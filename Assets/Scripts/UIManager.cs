using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
   
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2f;

    private float messageTimer;

    private void Awake()
    {
        if (targetText != null) targetText.text = "";
        if (messageText != null) messageText.text = "";
    }

    private void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f && messageText != null)
            {
                messageText.text = "";
            }
        }
    }

    public void ShowTarget(string sectionName, string slotId)
    {
        if (targetText == null) return;
        targetText.text = $"Levar para: {sectionName} — {slotId}";
    }

    public void ClearTarget()
    {
        if (targetText == null) return;
        targetText.text = "";
    }

    public void UpdateProgress(int current, int total)
    {
        if (progressText == null) return;
        progressText.text = $"Progresso: {current}/{total}";
    }

    public void ShowMistake()
    {
        if (messageText == null) return;
        messageText.text = "Item errado! Tente novamente.";
        messageTimer = messageDuration;
    }

    public void ShowSectionComplete()
    {
        if (messageText == null) return;
        messageText.text = "Seção concluída!";
        messageTimer = messageDuration;
    }
}