using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField]
    string tutorialId;

    [TextArea(2, 4)]
    [SerializeField]
    string message;

    public void ShowTutorial()
    {
        if (TutorialPopup.Instance == null)
        {
            Debug.LogWarning(
                "[TutorialTrigger] TutorialPopup was not found.",
                gameObject
            );

            return;
        }

        string id =
            string.IsNullOrWhiteSpace(
                tutorialId
            )
            ? message
            : tutorialId;

        TutorialPopup.Instance.ShowOnce(
            id,
            message
        );
    }
}