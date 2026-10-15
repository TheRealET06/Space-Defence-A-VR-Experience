using UnityEngine;

public class MiningTowerTargetSelector : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] float captureRange = 8f;

    [Tooltip("Origin used for distance checks. If empty, this object is used.")]
    [SerializeField] Transform scanOrigin;

    [Tooltip("Arm pivot used to determine whether a target is inside the vertical aiming range.")]
    [SerializeField] Transform aimOrigin;

    [Header("Arm Vertical Limits")]
    [SerializeField] float minArmAngleX = -90f;
    [SerializeField] float maxArmAngleX = 45f;

    [Header("Debug")]
    [SerializeField] bool autoSearchOnStart = false;
    [SerializeField] bool drawCaptureRange = true;
    [SerializeField] bool logRejectedTargets = false;

    ResourceAsteroid currentTarget;

    public float CaptureRange => captureRange;
    public ResourceAsteroid CurrentTarget => currentTarget;

    void Start()
    {
        if (autoSearchOnStart)
        {
            FindBestTarget();
        }
    }

    public ResourceAsteroid FindBestTarget()
    {
        ResourceAsteroid[] allAsteroids =
            FindObjectsByType<ResourceAsteroid>(
                FindObjectsSortMode.None
            );

        Vector3 rangeOrigin =
            scanOrigin != null
                ? scanOrigin.position
                : transform.position;

        Vector3 verticalAimOrigin =
            aimOrigin != null
                ? aimOrigin.position
                : rangeOrigin;

        ResourceAsteroid bestTarget = null;

        int bestValue = int.MinValue;
        float bestDistance = float.MaxValue;

        foreach (ResourceAsteroid asteroid in allAsteroids)
        {
            if (asteroid == null)
                continue;

            // Chunks are not initial mining targets.
            if (asteroid.IsChunk)
                continue;

            float distance =
                Vector3.Distance(
                    rangeOrigin,
                    asteroid.transform.position
                );

            if (distance > captureRange)
            {
                if (logRejectedTargets)
                {
                    Debug.Log(
                        $"Rejected {asteroid.name}: outside capture range.",
                        asteroid.gameObject
                    );
                }

                continue;
            }

            float requiredArmAngleX =
                CalculateRequiredArmAngleX(
                    verticalAimOrigin,
                    asteroid.transform.position
                );

            bool canAim =
                requiredArmAngleX >= minArmAngleX &&
                requiredArmAngleX <= maxArmAngleX;

            if (!canAim)
            {
                if (logRejectedTargets)
                {
                    Debug.Log(
                        $"Rejected {asteroid.name}: " +
                        $"required Arm X = {requiredArmAngleX:F1}°, " +
                        $"allowed range = " +
                        $"{minArmAngleX:F1}° to {maxArmAngleX:F1}°.",
                        asteroid.gameObject
                    );
                }

                continue;
            }

            int value = asteroid.PriorityValue;

            bool higherValue =
                value > bestValue;

            bool sameValueButCloser =
                value == bestValue &&
                distance < bestDistance;

            if (higherValue || sameValueButCloser)
            {
                bestTarget = asteroid;
                bestValue = value;
                bestDistance = distance;
            }
        }

        currentTarget = bestTarget;

        if (currentTarget != null)
        {
            float requiredArmX =
                CalculateRequiredArmAngleX(
                    verticalAimOrigin,
                    currentTarget.transform.position
                );

            Debug.Log(
                $"Mining Target Selected: {currentTarget.name}\n" +
                $"Family: {currentTarget.Family}\n" +
                $"Yield: {currentTarget.BarrelYield} barrels\n" +
                $"Distance: {bestDistance:F2} m\n" +
                $"Required Arm X: {requiredArmX:F1}°",
                currentTarget.gameObject
            );
        }
        else
        {
            Debug.Log(
                $"MiningTower: No reachable asteroid found within " +
                $"{captureRange:F1} m.",
                gameObject
            );
        }

        return currentTarget;
    }

    float CalculateRequiredArmAngleX(
        Vector3 origin,
        Vector3 targetPosition
    )
    {
        Vector3 direction =
            targetPosition - origin;

        float horizontalDistance =
            new Vector2(
                direction.x,
                direction.z
            ).magnitude;

        float elevationAngle =
            Mathf.Atan2(
                direction.y,
                horizontalDistance
            ) * Mathf.Rad2Deg;

        // The model aims upward using negative local X rotation.
        return -elevationAngle;
    }

    public void ClearTarget()
    {
        currentTarget = null;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawCaptureRange)
            return;

        Vector3 origin =
            scanOrigin != null
                ? scanOrigin.position
                : transform.position;

        Gizmos.DrawWireSphere(
            origin,
            captureRange
        );
    }
}