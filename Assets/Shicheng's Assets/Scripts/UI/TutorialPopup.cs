using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialPopup : MonoBehaviour
{
    public static TutorialPopup Instance
    {
        get;
        private set;
    }

    [Header("UI")]
    [SerializeField]
    TMP_Text tutorialText;

    [SerializeField]
    CanvasGroup canvasGroup;

    [Header("Timing")]
    [SerializeField]
    float displaySeconds = 3f;

    readonly HashSet<string> shownTutorials =
        new HashSet<string>();

    Coroutine activeRoutine;

    void Awake()
    {
        Instance = this;

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        HideImmediately();
    }

    public void ShowOnce(
        string tutorialId,
        string message
    )
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (shownTutorials.Contains(tutorialId))
            return;

        shownTutorials.Add(tutorialId);

        Show(message);
    }

    public void Show(
        string message
    )
    {
        if (
            tutorialText == null ||
            canvasGroup == null
        )
        {
            return;
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine =
            StartCoroutine(
                ShowRoutine(message)
            );
    }

    IEnumerator ShowRoutine(
        string message
    )
    {
        tutorialText.text = message;

        canvasGroup.alpha = 1f;

        yield return
            new WaitForSecondsRealtime(
                displaySeconds
            );

        canvasGroup.alpha = 0f;

        activeRoutine = null;
    }

    void HideImmediately()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}