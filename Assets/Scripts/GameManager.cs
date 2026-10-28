using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GamePhase { Menu, Countdown, Playing, Paused, StageClear, Over }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int maxMistakes = 3;
    [SerializeField] private float roundDuration = 180f;
    [SerializeField] private int pointsPerBaseScore = 100;
    [SerializeField] private int maxCombo = 3;
    [SerializeField] private float timeBonusPerSecond = 5f;
    [SerializeField] private int perfectRunBonus = 200;
    [SerializeField] private int sectionBonus = 150;
    [SerializeField] private float mistakeTimePenalty = 5f;
    [SerializeField] private float brokenCargoTimePenalty = 8f;
    [SerializeField] private float obstaclePenalty = 3f;
    [SerializeField] private bool failOnMaxMistakes = true;
    [SerializeField] private bool skipMenu = true;
    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private float stageClearSeconds = 4f;
    [SerializeField] private string startSectionName = "Fruits";
    [SerializeField] private bool endRoundWhenSectionCompletes = true;

    public UnityEvent<int, int> OnSectionProgressChanged;
    public UnityEvent<int> OnScoreChangedEvent;
    public UnityEvent<float> OnTimeChangedEvent;
    public UnityEvent<PlayerRole> OnRoleChangedEvent;
    public UnityEvent OnRoundEndedEvent;
    public UnityEvent OnSectionCompletedEvent;
    public UnityEvent OnMistakeMadeEvent;
    public UnityEvent<int> OnComboChangedEvent;
    public UnityEvent<CargoItem> OnCargoBrokenEvent;
    public UnityEvent OnObstacleHitEvent;
    public UnityEvent<string> OnSectionDoneEvent;
    public UnityEvent<string> OnStageStartedEvent;

    private int itemsPlacedCorrectly;
    private int totalItemsInSection;
    private int mistakesMade;
    private int brokenCount;
    private int comboCount;
    private int score;
    private float levelDuration;
    private float countdownTimer;
    private int lastCountdownInt;
    private float overSince;
    private float stageClearTimer;
    private readonly HashSet<string> sectionsRewarded = new HashSet<string>();

    public string currentSectionName;

    private float timeRemaining;

    public GamePhase Phase { get; private set; } = GamePhase.Menu;
    public bool RoundActive => Phase == GamePhase.Playing;
    public bool RoundWon { get; private set; }
    public bool NewTimeRecord { get; private set; }
    public bool CampaignComplete { get; private set; }
    public string EndReason { get; private set; } = "";
    public int TimeBonusAwarded { get; private set; }
    public int Score => score;
    public float TimeRemaining => timeRemaining;
    public float TimeUsed => Mathf.Max(0f, levelDuration - timeRemaining);
    public int MistakesMade => mistakesMade;
    public int MaxMistakes => maxMistakes;
    public int CountdownNumber => Mathf.Max(1, Mathf.CeilToInt(countdownTimer));
    public int ComboMultiplier => Mathf.Clamp(comboCount, 1, Mathf.Max(1, maxCombo));
    public float MistakeTimePenalty => mistakeTimePenalty;
    public float BrokenCargoTimePenalty => brokenCargoTimePenalty;
    public float ObstaclePenalty => obstaclePenalty;
    public int SectionBonus => sectionBonus;
    public float StageClearSecondsLeft => Mathf.Max(0f, stageClearTimer);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; Time.timeScale = 1f; }
    }

    private void Start()
    {
        var lm = FindFirstObjectByType<LevelManager>();
        if (lm != null) lm.Prepare();

        levelDuration = lm != null ? LevelManager.Current.time : roundDuration;
        timeRemaining = levelDuration;
        score = 0;
        currentSectionName = StageSection();

        StartSection(currentSectionName, CountCargoTotal());
        NotifyHud(1);

        if (RunState.MenuShown || skipMenu) BeginCountdown();
        else Phase = GamePhase.Menu;
    }

    private int PlacedCount()
    {
        var lm = LevelManager.Instance;
        if (lm == null) return itemsPlacedCorrectly;
        int n = 0;
        foreach (var c in lm.Cargos) if (c != null && c.IsPlaced) n++;
        return n;
    }

    private void SyncProgress()
    {
        int n = PlacedCount();
        if (n == itemsPlacedCorrectly) return;
        itemsPlacedCorrectly = n;
        OnSectionProgressChanged?.Invoke(itemsPlacedCorrectly, totalItemsInSection);
    }

    private string StageSection()
    {
        var loads = LevelManager.Current.loads;
        return loads != null && loads.Length > 0 ? loads[0].section : startSectionName;
    }

    private int CountCargoTotal()
    {
        var lm = LevelManager.Instance;
        if (lm != null && lm.Cargos.Count > 0) return lm.Cargos.Count;
        return FindObjectsByType<CargoItem>(FindObjectsSortMode.None).Length;
    }

    private void BeginCountdown()
    {
        RunState.MenuShown = true;
        Phase = GamePhase.Countdown;
        countdownTimer = countdownSeconds;
        lastCountdownInt = Mathf.CeilToInt(countdownTimer) + 1;
    }

    public void StartRound()
    {
        ResetRound();
        Phase = GamePhase.Playing;
        NotifyHud(ComboMultiplier);
    }

    private void ResetRound()
    {
        timeRemaining = levelDuration;
        score = 0;
        comboCount = 0;
        brokenCount = 0;
        RoundWon = false;
        NewTimeRecord = false;
        EndReason = "";
        TimeBonusAwarded = 0;
    }

    private void NotifyHud(int combo)
    {
        OnScoreChangedEvent?.Invoke(score);
        OnTimeChangedEvent?.Invoke(timeRemaining);
        OnComboChangedEvent?.Invoke(combo);
    }

    public void RestartRound()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static bool ConfirmPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;
        var gp = Gamepad.current;
        return gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame);
    }

    private static bool PausePressed()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) return true;
        var gp = Gamepad.current;
        return gp != null && gp.startButton.wasPressedThisFrame;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame && Phase != GamePhase.Menu) RetryStage();

        switch (Phase)
        {
            case GamePhase.Menu:
                if (ConfirmPressed()) BeginCountdown();
                break;

            case GamePhase.Countdown:
                countdownTimer -= Time.deltaTime;
                int n = Mathf.CeilToInt(countdownTimer);
                if (n != lastCountdownInt && n > 0) { Feedback.PlaySfx(Sfx.Tick); }
                lastCountdownInt = n;
                if (countdownTimer <= 0f)
                {
                    StartRound();
                    Feedback.PlaySfx(Sfx.Go);
                }
                break;

            case GamePhase.Playing:
                if (PausePressed()) { SetPaused(true); break; }
                SyncProgress();
                if (totalItemsInSection > 0 && itemsPlacedCorrectly >= totalItemsInSection) { CompleteSection(); break; }
                timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
                OnTimeChangedEvent?.Invoke(timeRemaining);
                if (timeRemaining <= 0f) EndRound(false, "Time's up!");
                else if (timeRemaining <= 10f && Mathf.CeilToInt(timeRemaining) != Mathf.CeilToInt(timeRemaining + Time.deltaTime))
                    Feedback.PlaySfx(Sfx.Tick);
                break;

            case GamePhase.Paused:
                if (PausePressed() || ConfirmPressed()) SetPaused(false);
                break;

            case GamePhase.StageClear:
                stageClearTimer -= Time.deltaTime;
                if (stageClearTimer <= 0f || (Time.unscaledTime - overSince > 0.8f && ConfirmPressed())) AdvanceStage();
                break;

            case GamePhase.Over:
                if (Time.unscaledTime - overSince > 1f && ConfirmPressed()) ContinueAfterRound();
                break;
        }
    }

    private void SetPaused(bool paused)
    {
        Phase = paused ? GamePhase.Paused : GamePhase.Playing;
        Time.timeScale = paused ? 0f : 1f;
    }

    private void ContinueAfterRound()
    {
        if (CampaignComplete)
        {
            RunState.ResetCampaign();
            RestartRound();
        }
        else RetryStage();
    }
    private void AdvanceStage()
    {
        RunState.Level++;
        BeginStageInScene(false);
        var drv = FindFirstObjectByType<DriverController>();
        if (drv != null) Feedback.SpawnBurst(drv.transform.position + Vector3.up * 2f, FxKind.Sparkle);
        OnStageStartedEvent?.Invoke(StageSection());
    }
    public void RetryStage()
    {
        Time.timeScale = 1f;
        BeginStageInScene(true);
    }

    private void BeginStageInScene(bool clearPrevious)
    {
        var lm = LevelManager.Instance;
        if (lm != null) lm.BeginStage(clearPrevious);

        levelDuration = lm != null ? LevelManager.Current.time : roundDuration;
        ResetRound();
        sectionsRewarded.Clear();
        CampaignComplete = false;
        currentSectionName = StageSection();

        StartSection(currentSectionName, CountCargoTotal());
        NotifyHud(1);
        BeginCountdown();
    }

    public void OnRoleChanged(PlayerRole newRole)
    {
        OnRoleChangedEvent?.Invoke(newRole);
    }

    public void AddScore(int amount)
    {
        if (!RoundActive) return;
        score += amount;
        OnScoreChangedEvent?.Invoke(score);
    }

    public void ReduceTime(float seconds)
    {
        if (!RoundActive) return;
        timeRemaining = Mathf.Max(0f, timeRemaining - seconds);
        OnTimeChangedEvent?.Invoke(timeRemaining);
        if (timeRemaining <= 0f) EndRound(false, "Time's up!");
    }

    private void EndRound(bool won, string reason)
    {
        if (!RoundActive) return;

        if (won)
        {
            TimeBonusAwarded = Mathf.RoundToInt(timeRemaining * timeBonusPerSecond);
            if (mistakesMade == 0 && brokenCount == 0) TimeBonusAwarded += perfectRunBonus;
            score += TimeBonusAwarded;
            OnScoreChangedEvent?.Invoke(score);

            NewTimeRecord = RunState.RegisterTime(RunState.Level, TimeUsed);
            RunState.BankedScore += score;

            if (RunState.Level >= RunState.MaxLevel)
            {
                CampaignComplete = true;
                RunState.AddTopScore(RunState.BankedScore);
            }
        }

        bool stageCleared = won && !CampaignComplete;
        Phase = stageCleared ? GamePhase.StageClear : GamePhase.Over;
        stageClearTimer = stageClearSeconds;
        overSince = Time.unscaledTime;
        Time.timeScale = 1f;
        RoundWon = won;
        EndReason = reason;

        int highscore = PlayerPrefs.GetInt("PlayerHighscore", 0);
        if (score > highscore)
        {
            PlayerPrefs.SetInt("PlayerHighscore", score);
            PlayerPrefs.Save();
        }

        if (won)
        {
            Feedback.PlaySfx(Sfx.Win);
            var drv = FindFirstObjectByType<DriverController>();
            Vector3 p = drv != null ? drv.transform.position + Vector3.up * 1.5f : Vector3.up * 3f;
            Feedback.SpawnBurst(p + Vector3.left * 2f, FxKind.Confetti);
            Feedback.SpawnBurst(p, FxKind.Confetti);
            Feedback.SpawnBurst(p + Vector3.right * 2f, FxKind.Confetti);
        }
        else Feedback.PlaySfx(Sfx.Lose);

        OnRoundEndedEvent?.Invoke();
    }

    public void StartSection(string sectionName, int totalItems)
    {
        currentSectionName = sectionName;
        totalItemsInSection = totalItems;
        itemsPlacedCorrectly = 0;
        mistakesMade = 0;

        Debug.Log($"Seção '{sectionName}' iniciada: {totalItems} caixas");
        OnSectionProgressChanged?.Invoke(itemsPlacedCorrectly, totalItemsInSection);
    }

    public void RegisterPlacement(CargoItem cargo)
    {
        if (!RoundActive) return;

        int basePoints = cargo != null ? cargo.GetScoreValue() * pointsPerBaseScore : 0;
        if (basePoints > 0) comboCount++;
        else comboCount = 0;
        OnComboChangedEvent?.Invoke(ComboMultiplier);

        int pts = basePoints * (basePoints > 0 ? ComboMultiplier : 1);
        if (cargo != null) cargo.AwardedPoints = pts;
        AddScore(pts);

        var lm = LevelManager.Instance;
        if (cargo != null && lm != null && !sectionsRewarded.Contains(cargo.sectionName)
            && lm.IsSectionComplete(cargo.sectionName) && itemsPlacedCorrectly + 1 < totalItemsInSection)
        {
            sectionsRewarded.Add(cargo.sectionName);
            AddScore(sectionBonus);
            OnSectionDoneEvent?.Invoke(cargo.sectionName);
            Feedback.PlaySfx(Sfx.Win);
        }

        OnItemPlacedCorrectly();
    }

    public void UnregisterPlacement(CargoItem cargo)
    {
        if (!RoundActive) return;
        itemsPlacedCorrectly = PlacedCount();
        if (cargo != null)
        {
            score = Mathf.Max(0, score - cargo.AwardedPoints);
            cargo.AwardedPoints = 0;
        }
        OnScoreChangedEvent?.Invoke(score);
        OnSectionProgressChanged?.Invoke(itemsPlacedCorrectly, totalItemsInSection);
    }

    public void OnItemPlacedCorrectly()
    {
        itemsPlacedCorrectly = LevelManager.Instance != null ? PlacedCount() : itemsPlacedCorrectly + 1;
        Debug.Log($"Progresso: {itemsPlacedCorrectly}/{totalItemsInSection}");

        OnSectionProgressChanged?.Invoke(itemsPlacedCorrectly, totalItemsInSection);

        if (itemsPlacedCorrectly >= totalItemsInSection)
        {
            CompleteSection();
        }
    }

    public void OnItemPlacedWrong()
    {
        if (!RoundActive) return;

        mistakesMade++;
        comboCount = 0;
        OnComboChangedEvent?.Invoke(ComboMultiplier);
        OnMistakeMadeEvent?.Invoke();

        ReduceTime(mistakeTimePenalty);

        if (failOnMaxMistakes && mistakesMade >= maxMistakes)
        {
            EndRound(false, "Too many mistakes!");
        }
    }

    public void OnCargoBroken(CargoItem cargo)
    {
        if (!RoundActive) return;

        brokenCount++;
        comboCount = 0;
        OnComboChangedEvent?.Invoke(ComboMultiplier);
        OnCargoBrokenEvent?.Invoke(cargo);
        ReduceTime(brokenCargoTimePenalty);
    }

    public void OnObstacleHit()
    {
        if (!RoundActive) return;

        comboCount = 0;
        OnComboChangedEvent?.Invoke(ComboMultiplier);
        OnObstacleHitEvent?.Invoke();
        ReduceTime(obstaclePenalty);
    }

    private void CompleteSection()
    {
        Debug.Log($"Seção '{currentSectionName}' concluída, erros: {mistakesMade}");
        OnSectionCompletedEvent?.Invoke();

        if (endRoundWhenSectionCompletes)
        {
            EndRound(true, SectionInfo.Display(currentSectionName) + " organized!");
        }
    }
}
