using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2f;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private bool useFallbackHud = true;

    private float messageTimer;
    private GameManager gm;

    private int shownScore;
    private float shownTime;
    private int comboMult = 1;
    private PlayerRole shownRole = PlayerRole.Driver;
    private bool roundOver;
    private int lastTimerSecond = -1;
    private GUIStyle hudStyle, bigStyle, smallStyle;
    private string fallbackMessage = "";
    private bool showDebug;

    private void Awake()
    {
        if (targetText != null) targetText.text = "";
        if (messageText != null) messageText.text = "";
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    private void Start()
    {
        gm = GameManager.Instance;
        if (gm == null) return;

        gm.OnScoreChangedEvent.AddListener(UpdateScore);
        gm.OnTimeChangedEvent.AddListener(UpdateTime);
        gm.OnRoleChangedEvent.AddListener(UpdateRole);
        gm.OnComboChangedEvent.AddListener(UpdateCombo);
        gm.OnRoundEndedEvent.AddListener(ShowRoundEnd);
        gm.OnCargoBrokenEvent.AddListener(ShowCargoBroken);
        gm.OnObstacleHitEvent.AddListener(ShowObstacleHit);
        gm.OnSectionDoneEvent.AddListener(ShowSectionDone);
        gm.OnStageStartedEvent.AddListener(ShowStageStarted);

        UpdateScore(gm.Score);
        UpdateTime(gm.TimeRemaining);
        UpdateCombo(gm.ComboMultiplier);
        UpdateRole(shownRole);
    }

    private void OnDestroy()
    {
        if (gm == null) return;
        gm.OnScoreChangedEvent.RemoveListener(UpdateScore);
        gm.OnTimeChangedEvent.RemoveListener(UpdateTime);
        gm.OnRoleChangedEvent.RemoveListener(UpdateRole);
        gm.OnComboChangedEvent.RemoveListener(UpdateCombo);
        gm.OnRoundEndedEvent.RemoveListener(ShowRoundEnd);
        gm.OnCargoBrokenEvent.RemoveListener(ShowCargoBroken);
        gm.OnObstacleHitEvent.RemoveListener(ShowObstacleHit);
        gm.OnSectionDoneEvent.RemoveListener(ShowSectionDone);
        gm.OnStageStartedEvent.RemoveListener(ShowStageStarted);
    }

    private void Update()
    {
        var kbd = Keyboard.current;
        if (kbd != null && kbd.f3Key.wasPressedThisFrame) showDebug = !showDebug;

        if (messageTimer > 0f)
        {
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f)
            {
                fallbackMessage = "";
                if (messageText != null) messageText.text = "";
            }
        }
    }

    public void ShowTarget(string sectionName, string slotId)
    {
        if (targetText == null) return;
        targetText.text = $"Deliver to: {SectionInfo.Display(sectionName)} / {slotId}";
    }

    public void ClearTarget()
    {
        if (targetText == null) return;
        targetText.text = "";
    }

    public void UpdateProgress(int current, int total)
    {
        if (progressText == null) return;
        progressText.text = $"Progress: {current}/{total}";
    }

    public void ShowMistake()
    {
        if (gm == null) { ShowMessage("Wrong slot! Try again."); return; }
        ShowMessage($"Wrong slot! -{gm.MistakeTimePenalty:0}s  ({gm.MistakesMade}/{gm.MaxMistakes})");
    }

    public void ShowSectionComplete()
    {
        ShowMessage("Section complete!");
    }


    public void ShowMessage(string text)
    {
        fallbackMessage = text;
        messageTimer = messageDuration;
        if (messageText != null) messageText.text = text;
    }

    private void ShowCargoBroken(CargoItem cargo)
    {
        float p = gm != null ? gm.BrokenCargoTimePenalty : 0f;
        ShowMessage($"Fragile cargo broken! -{p:0}s");
    }

    private void ShowSectionDone(string section)
    {
        int bonus = gm != null ? gm.SectionBonus : 0;
        ShowMessage($"{SectionInfo.Display(section)} organized! +{bonus}");
    }

    private void ShowStageStarted(string section)
    {
        ShowMessage($"{SectionInfo.Display(section)} cargo has arrived!");
    }

    private void ShowObstacleHit()
    {
        float p = gm != null ? gm.ObstaclePenalty : 0f;
        ShowMessage($"Watch out for the customers! -{p:0}s");
    }

    public void UpdateScore(int score)
    {
        shownScore = score;
        if (scoreText != null) scoreText.text = $"Score: {RunState.BankedScore + score}";
    }

    public void UpdateTime(float seconds)
    {
        shownTime = seconds;
        if (timerText == null) return;

        int whole = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        if (whole == lastTimerSecond) return;
        lastTimerSecond = whole;

        timerText.text = $"{whole / 60:00}:{whole % 60:00}";
        timerText.color = whole <= 10 ? Color.red : Color.white;
    }

    public void UpdateRole(PlayerRole role)
    {
        shownRole = role;
        if (roleText != null)
            roleText.text = role == PlayerRole.Driver ? "MODE: DRIVE" : "MODE: FORKS";
    }

    public void UpdateCombo(int multiplier)
    {
        comboMult = multiplier;
        if (comboText != null) comboText.text = multiplier > 1 ? $"COMBO x{multiplier}" : "";
    }

    public void ShowRoundEnd()
    {
        roundOver = true;
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = BuildResultString();
    }

    private string BuildResultString()
    {
        if (gm == null) return "";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(gm.CampaignComplete ? "YOU ARE THE FORKLIFT HERO!" : gm.RoundWon ? "STOCK ORGANIZED!" : "GAME OVER");
        sb.AppendLine(gm.EndReason);
        sb.AppendLine();
        sb.AppendLine($"Stage score: {gm.Score}" + (gm.RoundWon ? $"  (bonus +{gm.TimeBonusAwarded})" : ""));
        sb.AppendLine($"Total: {RunState.BankedScore}");
        if (gm.RoundWon)
        {
            sb.AppendLine($"Time: {RunState.FormatTime(gm.TimeUsed)}" + (gm.NewTimeRecord ? "  NEW RECORD!" : $"  (best {RunState.FormatTime(RunState.BestTime(RunState.Level))})"));
        }
        sb.AppendLine();
        if (gm.CampaignComplete) sb.Append("[Enter] Play again from stage 1");
        else if (gm.RoundWon) sb.Append($"[Enter] Next section ({RunState.Level + 1}/{RunState.MaxLevel})");
        else sb.Append("[Enter] or [R] Try again");
        return sb.ToString();
    }


    private void DrawDim(float alpha)
    {
        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void OnGUI()
    {
        if (!useFallbackHud) return;

        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
            bigStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        }
        float w = Screen.width, h = Screen.height;
        hudStyle.fontSize = Mathf.Max(14, Screen.height / 28);
        bigStyle.fontSize = Mathf.Max(20, Screen.height / 18);
        smallStyle.fontSize = Mathf.Max(13, Screen.height / 34);
        float line = hudStyle.fontSize * 1.4f;

        var phase = gm != null ? gm.Phase : GamePhase.Playing;

        if (phase == GamePhase.Menu) { DrawMenu(w, h); return; }

        if (timerText == null)
        {
            int whole = Mathf.CeilToInt(Mathf.Max(0f, shownTime));
            hudStyle.alignment = TextAnchor.UpperCenter;
            hudStyle.normal.textColor = whole <= 10 ? Color.red : Color.white;
            GUI.Label(new Rect(0, 8, w, line), $"{whole / 60:00}:{whole % 60:00}", hudStyle);
        }

        hudStyle.normal.textColor = Color.white;
        hudStyle.alignment = TextAnchor.UpperRight;
        float y = 8f;
        if (scoreText == null) { GUI.Label(new Rect(0, y, w - 12, line), $"Score: {RunState.BankedScore + shownScore}", hudStyle); y += line; }
        if (comboText == null && comboMult > 1) { GUI.Label(new Rect(0, y, w - 12, line), $"COMBO x{comboMult}", hudStyle); y += line; }
        if (roleText == null)
            GUI.Label(new Rect(0, y, w - 12, line), shownRole == PlayerRole.Driver ? "MODE: DRIVE [Tab]" : "MODE: FORKS [Tab]", hudStyle);

        hudStyle.alignment = TextAnchor.UpperLeft;
        GUI.Label(new Rect(12, 8, w * 0.5f, line), $"Stage {RunState.Level}/{RunState.MaxLevel}: {LevelManager.Current.name}", hudStyle);

        var lm = LevelManager.Instance;
        if (lm != null)
        {
            float yy = 8f + line;
            foreach (var sec in lm.ActiveSections)
            {
                hudStyle.normal.textColor = SectionInfo.ColorOf(sec);
                GUI.Label(new Rect(12, yy, w * 0.5f, line), $"{SectionInfo.Display(sec)} {lm.SectionDone(sec)}/{lm.SectionTotal(sec)}", hudStyle);
                yy += line;
            }
            hudStyle.normal.textColor = Color.white;
        }

        if (phase == GamePhase.Playing || phase == GamePhase.Paused)
        {
            string mode = OrbitCamera.Instance != null ? OrbitCamera.Instance.ModeLabel : "Free";
            smallStyle.alignment = TextAnchor.MiddleLeft;
            smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(12, h - smallStyle.fontSize * 2.4f, w * 0.7f, smallStyle.fontSize * 2f),
                $"Camera {mode} [V]  ·  right mouse + drag to orbit  ·  wheel = zoom  ·  Z/X orbit", smallStyle);
            smallStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (messageText == null && fallbackMessage.Length > 0)
        {
            bigStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            GUI.Label(new Rect(0, h * 0.18f, w, bigStyle.fontSize * 1.6f), fallbackMessage, bigStyle);
        }

        if (phase == GamePhase.Playing && ForkInteraction.Instance != null && ForkInteraction.Instance.HintText.Length > 0)
        {
            smallStyle.normal.textColor = Color.white;
            GUI.Box(new Rect(w * 0.3f, h * 0.86f, w * 0.4f, smallStyle.fontSize * 2f), GUIContent.none);
            GUI.Label(new Rect(w * 0.3f, h * 0.86f, w * 0.4f, smallStyle.fontSize * 2f), ForkInteraction.Instance.HintText, smallStyle);
        }

        if (phase == GamePhase.Countdown)
        {
            bigStyle.fontSize = Mathf.Max(40, Screen.height / 7);
            bigStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(0, 0, w, h), gm.CountdownNumber.ToString(), bigStyle);
        }

        if (showDebug) DrawDebug(w, h);

        if (phase == GamePhase.StageClear && gm != null)
        {
            DrawDim(0.45f);
            bigStyle.fontSize = Mathf.Max(20, Screen.height / 20);
            bigStyle.normal.textColor = Color.white;
            int nextIdx = Mathf.Min(RunState.Level, LevelManager.Levels.Length - 1);
            string next = LevelManager.Levels[nextIdx].name;
            GUI.Label(new Rect(w * 0.15f, h * 0.15f, w * 0.7f, h * 0.7f),
                $"{gm.EndReason}\n\nSection score: {gm.Score}  (bonus +{gm.TimeBonusAwarded})\nTotal: {RunState.BankedScore}\n\nNext delivery: {next} in {Mathf.CeilToInt(gm.StageClearSecondsLeft)} s   [Enter] skip", bigStyle);
        }

        if (phase == GamePhase.Paused)
        {
            DrawDim(0.6f);
            bigStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(0, h * 0.06f, w, h * 0.3f), "PAUSED\n\n[Esc] Resume     [R] Retry section", bigStyle);
            DrawMenu(w, h, true);
        }

        if (roundOver && phase == GamePhase.Over && resultText == null)
        {
            DrawDim(0.55f);
            bigStyle.fontSize = Mathf.Max(20, Screen.height / 24);
            bigStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(w * 0.15f, h * 0.1f, w * 0.7f, h * 0.8f), BuildResultString(), bigStyle);
        }
    }

    private void DrawDebug(float w, float h)
    {
        var lm = LevelManager.Instance;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("DIAGNOSTICS (F3)");
        if (gm != null) sb.AppendLine($"Placed: {lm?.Cargos.FindAll(c => c != null && c.IsPlaced).Count ?? 0}   mistakes: {gm.MistakesMade}/{gm.MaxMistakes}   phase: {gm.Phase}");
        if (lm != null)
        {
            foreach (var c in lm.Cargos)
            {
                if (c == null) continue;
                string st = c.IsPlaced ? "ON SHELF " + (c.PlacedSlot != null ? c.PlacedSlot.slotId : "?")
                          : c.IsCarried ? "carrying" : "on floor";
                if (c.IsBroken) st += " (broken)";
                sb.AppendLine($"{c.name,-18} target {c.targetSlotId,-5} {st}");
            }
        }
        sb.AppendLine("-- events --");
        foreach (var l in PlacementLog.Lines) sb.AppendLine(l);

        var st2 = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = Mathf.Max(12, Screen.height / 42) };
        st2.normal.textColor = new Color(0.8f, 1f, 0.8f);
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(8, h * 0.18f, w * 0.5f, h * 0.62f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(14, h * 0.18f + 4, w * 0.5f - 12, h * 0.62f), sb.ToString(), st2);
    }

    private void DrawMenu(float w, float h, bool asHelp = false)
    {
        if (!asHelp)
        {
            DrawDim(0.8f);
            bigStyle.fontSize = Mathf.Max(30, Screen.height / 10);
            bigStyle.normal.textColor = new Color(1f, 0.82f, 0.1f);
            GUI.Label(new Rect(0, h * 0.06f, w, h * 0.15f), "FORKLIFT HEROES", bigStyle);
        }

        smallStyle.normal.textColor = Color.white;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Organize the stock before time runs out!");
        sb.AppendLine();
        sb.AppendLine("W A S D / left stick  Drive        Shift / right trigger  Rush");
        sb.AppendLine("Tab / Y  Switch Drive <-> Forks        Space / X  Brake");
        sb.AppendLine("Arrows / right stick  Forks (lift/tilt)");
        sb.AppendLine("E / A  Pick up / drop cargo      Esc  Pause      M  Mute      R  Retry section");
        sb.AppendLine("Free camera: right mouse + drag (or Z/X) to orbit  ·  mouse wheel = zoom  ·  V = free/follow");
        sb.AppendLine("F3  Cargo diagnostics");
        if (ModelCatalog.Has("forklift")) sb.AppendLine("Forklift model: K toggles  ·  J flips the front");
        sb.AppendLine();
        sb.AppendLine("Line the forklift up with the cargo and match the fork height to pick it up.");
        sb.AppendLine("Fragile cargo breaks with fast fork tilts and impacts. Hitting customers costs time.");
        sb.AppendLine();
        sb.AppendLine("BEST TIMES");
        for (int i = 1; i <= RunState.MaxLevel; i++)
        {
            float bt = RunState.BestTime(i);
            sb.AppendLine($"Stage {i} - {LevelManager.Levels[i - 1].name}:  {(bt > 0f ? RunState.FormatTime(bt) : "--:--")}");
        }
        var top = RunState.TopScores();
        sb.AppendLine();
        sb.AppendLine("CAMPAIGN RANKING: " + string.Join("  ", System.Array.ConvertAll(top, t => t.ToString())));
        GUI.Label(new Rect(w * 0.1f, asHelp ? h * 0.38f : h * 0.2f, w * 0.8f, h * 0.6f), sb.ToString(), smallStyle);

        if (asHelp) return;
        bigStyle.fontSize = Mathf.Max(22, Screen.height / 18);
        bigStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0, h * 0.86f, w, h * 0.1f), "[Enter] PLAY", bigStyle);
    }
}
