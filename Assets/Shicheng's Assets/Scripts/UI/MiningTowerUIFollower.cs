using UnityEngine;

public class MiningTowerUIFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform towerCenter;
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private RectTransform actionCanvas;
    [SerializeField] private RectTransform feedbackCanvas;

    [Header("Action Canvas")]
    [SerializeField] private float actionDistance = 4f;
    [SerializeField] private float actionHeight = 1.8f;

    [Header("Feedback Canvas")]
    [SerializeField] private float feedbackDistance = 4.25f;
    [SerializeField] private float feedbackHeight = 2.8f;

    private void Awake()
    {
        if (towerCenter == null)
            towerCenter = transform.parent;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (towerCenter == null || cameraTransform == null)
            return;

        Vector3 horizontalDirection =
            cameraTransform.position - towerCenter.position;

        horizontalDirection.y = 0f;

        if (horizontalDirection.sqrMagnitude < 0.001f)
            horizontalDirection = towerCenter.forward;

        horizontalDirection.Normalize();

        UpdateCanvas(
            actionCanvas,
            actionDistance,
            actionHeight,
            horizontalDirection);

        UpdateCanvas(
            feedbackCanvas,
            feedbackDistance,
            feedbackHeight,
            horizontalDirection);
    }

    private void UpdateCanvas(
        RectTransform canvas,
        float distance,
        float height,
        Vector3 direction)
    {
        if (canvas == null)
            return;

        canvas.position =
            towerCenter.position
            + direction * distance
            + Vector3.up * height;

        Vector3 lookDirection =
            cameraTransform.position - canvas.position;

        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            canvas.rotation =
                Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }
}