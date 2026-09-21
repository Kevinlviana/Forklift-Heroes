using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }

    [SerializeField] private int maxMistakes = 3;
    [SerializeField] private float roundDuration = 120f;

    public UnityEvent<int, int> OnSectionProgressChanged;
    public UnityEvent<int> OnScoreChangedEvent;
    public UnityEvent<float> OnTimeChangedEvent;
    public UnityEvent<PlayerRole> OnRoleChangedEvent;
    public UnityEvent OnRoundEndedEvent;
    public UnityEvent OnSectionCompletedEvent;
    public UnityEvent OnMistakeMadeEvent;
 
    private int itemsPlacedCorrectly;
    private int totalItemsInSection;
    private int mistakesMade;
    private int score;

    private bool roundActive;

    public string currentSectionName;

    private float timeRemaining;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartRound();
        StartSection("Fruits", 5);
    }

    public void StartRound()
    {
        timeRemaining = roundDuration;
        score = 0;
        roundActive = true;
        OnScoreChangedEvent?.Invoke(score);
        OnTimeChangedEvent?.Invoke(timeRemaining);
    }

    private void Update()
    {
        if (!roundActive) return;

        timeRemaining -= Time.deltaTime;
        OnTimeChangedEvent?.Invoke(timeRemaining);

        if (timeRemaining <= 0f)
        {
            EndRound();
        }
    }

    public void OnRoleChanged(PlayerRole newRole)
    {
        OnRoleChangedEvent?.Invoke(newRole);
    }

    public void AddScore(int amount)
    {
        score += amount;
        OnScoreChangedEvent?.Invoke(score);
    }

    public void ReduceTime(float seconds)
    {
        timeRemaining = Mathf.Max(0f, timeRemaining - seconds);
    }

    private void EndRound()
    {
        roundActive = false;
        OnRoundEndedEvent?.Invoke();

        int highscore = PlayerPrefs.GetInt("PlayerHighscore", 0);
        if (score > highscore)
        {
            PlayerPrefs.SetInt("PlayerHighscore", score);
        }
    }

    public void StartSection(string sectionName, int totalItems)
    {
        currentSectionName = sectionName;
        totalItemsInSection = totalItems;
        itemsPlacedCorrectly = 0;
        mistakesMade = 0;

        Debug.Log($"Seção '{sectionName}' iniciada — {totalItems} itens para completar.");
        OnSectionProgressChanged?.Invoke(itemsPlacedCorrectly, totalItemsInSection);
    }

    public void OnItemPlacedCorrectly()
    {
        itemsPlacedCorrectly++;
        Debug.Log($"Progresso da seção: {itemsPlacedCorrectly}/{totalItemsInSection}");

        OnSectionProgressChanged?.Invoke(itemsPlacedCorrectly, totalItemsInSection);

        if (itemsPlacedCorrectly >= totalItemsInSection)
        {
            CompleteSection();
        }
    }

    public void OnItemPlacedWrong()
    {
        mistakesMade++;
        Debug.Log($"Erro registrado. Total de erros: {mistakesMade}/{maxMistakes}");
        OnMistakeMadeEvent?.Invoke();
    }

    private void CompleteSection()
    {
        Debug.Log($"=== SEÇÃO '{currentSectionName}' CONCLUÍDA === Erros cometidos: {mistakesMade}");
        OnSectionCompletedEvent?.Invoke();
    }
}