using UnityEngine;

public class MiningTowerUIFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform towerCenter;

    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private RectTransform actionCanvas;

    [Header("Placement")]
    [SerializeField]
    private float horizontalDistance = 4f;

    [SerializeField]
    private float height = 1.8f;

    private void Awake()
    {
        if (towerCenter == null)
        {
            towerCenter =
                transform.parent;
        }

        if (
            cameraTransform == null &&
            Camera.main != null
        )
        {
            cameraTransform =
                Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (
            towerCenter == null ||
            cameraTransform == null ||
            actionCanvas == null
        )
        {
            return;
        }

        Vector3 horizontalDirection =
            cameraTransform.position -
            towerCenter.position;

        horizontalDirection.y = 0f;

        if (
            horizontalDirection.sqrMagnitude <
            0.001f
        )
        {
            horizontalDirection =
                towerCenter.forward;
        }

        horizontalDirection.Normalize();

        actionCanvas.position =
            towerCenter.position +
            horizontalDirection *
            horizontalDistance +
            Vector3.up *
            height;

        Vector3 lookDirection =
            cameraTransform.position -
            actionCanvas.position;

        lookDirection.y = 0f;

        if (
            lookDirection.sqrMagnitude >
            0.001f
        )
        {
            actionCanvas.rotation =
                Quaternion.LookRotation(
                    lookDirection.normalized,
                    Vector3.up
                );
        }
    }
}