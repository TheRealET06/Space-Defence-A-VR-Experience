using TMPro;
using UnityEngine;

namespace Brian
{
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] float distance = 1.2f;
        [SerializeField] float metersPerPixel = 0.0012f;

        RectTransform root;
        TextMeshProUGUI title;

        void Start()
        {
            root = UIKit.Canvas("End Screen", transform, new Vector2(600f, 300f), metersPerPixel);
            title = UIKit.Text(root, "", 56, 0, 50, 600, 90, TextAlignmentOptions.Center);
            UIKit.Button(root, "Play again", 26, 200, 180, 200, 70, () => GameManager.Instance.Restart());
            root.gameObject.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnState;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnState;
        }

        void OnState(GameState state)
        {
            if (state != GameState.Won && state != GameState.Lost) return;
            title.text = state == GameState.Won ? "<color=#9fffb0>YOU WIN</color>" : "<color=#ff8a80>HOME DESTROYED</color>";
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 forward = cam.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                root.position = cam.transform.position + forward.normalized * distance;
                UIKit.FaceCamera(root);
            }
            root.gameObject.SetActive(true);
        }
    }
}
