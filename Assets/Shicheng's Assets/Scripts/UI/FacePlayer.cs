using UnityEngine;

public class FacePlayer : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform playerCamera;

    [Header("Settings")]
    [SerializeField] bool keepUpright = true;
    [SerializeField] bool flip180 = false;

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (playerCamera == null)
            return;

        Vector3 direction =
            playerCamera.position -
            transform.position;

        if (keepUpright)
        {
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );

        if (flip180)
        {
            transform.Rotate(
                0f,
                180f,
                0f
            );
        }
    }
}