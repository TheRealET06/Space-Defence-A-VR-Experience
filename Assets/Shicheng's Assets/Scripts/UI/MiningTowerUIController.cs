using UnityEngine;
using TMPro;
using System.Collections;

public class MiningTowerUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject actionCanvas;
    [SerializeField] private GameObject feedbackCanvas;
    [SerializeField] private GameObject popupPanel;

    [Header("Feedback")]
    [SerializeField] private TMP_Text popupTitle;
    [SerializeField] private TMP_Text popupMessage;
    [SerializeField] private TMP_Text objectiveText;

    [SerializeField] private float popupDuration = 3f;

    private Coroutine popupCoroutine;

    public bool IsActionVisible =>
        actionCanvas != null && actionCanvas.activeSelf;

    private void Awake()
    {
        if (actionCanvas != null)
            actionCanvas.SetActive(false);

        if (feedbackCanvas != null)
            feedbackCanvas.SetActive(true);

        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    public bool ToggleActionCanvas()
    {
        if (actionCanvas == null)
            return false;

        bool newState = !actionCanvas.activeSelf;
        actionCanvas.SetActive(newState);

        return newState;
    }

    public void ShowActionCanvas()
    {
        if (actionCanvas != null)
            actionCanvas.SetActive(true);
    }

    public void HideActionCanvas()
    {
        if (actionCanvas != null)
            actionCanvas.SetActive(false);
    }

    public void SetObjective(string message)
    {
        if (objectiveText != null)
            objectiveText.text = message;
    }

    public void ShowLesson(string title, string message)
    {
        ShowPopup(title, message);
    }

    public void ShowError(string title, string message)
    {
        ShowPopup(title, message);
    }

    public void ShowUnlock(string title, string message)
    {
        ShowPopup(title, message);
    }

    private void ShowPopup(string title, string message)
    {
        if (popupPanel == null)
            return;

        if (popupCoroutine != null)
            StopCoroutine(popupCoroutine);

        popupCoroutine =
            StartCoroutine(PopupRoutine(title, message));
    }

    private IEnumerator PopupRoutine(
        string title,
        string message)
    {
        if (popupTitle != null)
            popupTitle.text = title;

        if (popupMessage != null)
            popupMessage.text = message;

        popupPanel.SetActive(true);

        yield return new WaitForSecondsRealtime(popupDuration);

        popupPanel.SetActive(false);

        popupCoroutine = null;
    }
}