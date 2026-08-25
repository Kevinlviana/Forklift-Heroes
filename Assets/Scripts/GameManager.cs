using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float roundDuration = 120f;
    private float timeRemaining;
    private int score;

    public UnityEvent<PlayerRole> OnRoleChangedEvent;
    public UnityEvent<int> OnScoreChangedEvent;
    public UnityEvent<float> OnTimeChangedEvent;
    public UnityEvent OnRoundEndedEvent;

    private bool roundActive;

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
}
