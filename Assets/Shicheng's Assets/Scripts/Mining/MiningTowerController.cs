using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MiningTowerController : MonoBehaviour
{
    public enum TowerState
    {
        Idle,
        HorizontalAligning,
        VerticalAligning,
        WaitingForPulse,
        ChunkHorizontalAligning,
        ChunkVerticalAligning,
        Extending,
        Retracting,
        VerticalReturning,
        Returning
    }

    [Header("References")]
    [SerializeField] MiningTowerTargetSelector targetSelector;
    [SerializeField] MiningCargo cargo;
    [SerializeField] Transform towerRoot;
    [SerializeField] Transform arm;
    [SerializeField] Transform trackSupport;
    [SerializeField] Transform armHead;
    [SerializeField] Transform grabPoint;
    [SerializeField] Transform firePoint;

    [Header("Start Control")]
    [SerializeField] bool startOnPlay = true;

    [Tooltip("Optional input action used to start a mining cycle.")]
    [SerializeField] InputActionReference startAction;

    [Header("Tower Rotation")]
    [SerializeField] float towerRotationSpeed = 90f;
    [SerializeField] float towerYawOffset = 0f;
    [SerializeField] float horizontalAimTolerance = 2f;

    [Header("Arm Rotation")]
    [SerializeField] float armRotationSpeed = 90f;
    [SerializeField] float minArmAngleX = -90f;
    [SerializeField] float maxArmAngleX = 45f;
    [SerializeField] float verticalAimTolerance = 2f;

    [Header("Arm Extension")]
    [SerializeField] float extensionSpeed = 4f;

    [Header("Capture")]
    [SerializeField] float reachDistance = 0.3f;

    [Header("Mining Pulse")]
    [SerializeField] GameObject miningPulsePrefab;

    [Tooltip("Use a large value during testing so every full asteroid shatters in one hit.")]
    [SerializeField] float miningPulseDamage = 999f;

    TowerState state = TowerState.Idle;

    ResourceAsteroid currentTarget;
    ResourceAsteroid carriedChunk;

    Quaternion defaultTowerLocalRotation;
    Quaternion defaultArmLocalRotation;

    Vector3 defaultTrackScale;
    Vector3 defaultArmHeadPosition;

    public TowerState State => state;
    public ResourceAsteroid CurrentTarget => currentTarget;

    void Awake()
    {
        if (towerRoot == null)
            towerRoot = transform;

        defaultTowerLocalRotation =
            towerRoot.localRotation;

        defaultArmLocalRotation =
            arm.localRotation;

        defaultTrackScale =
            trackSupport.localScale;

        defaultArmHeadPosition =
            armHead.localPosition;
    }

    void OnEnable()
    {
        if (startAction != null)
        {
            startAction.action.performed +=
                OnStartAction;
        }
    }

    void OnDisable()
    {
        if (startAction != null)
        {
            startAction.action.performed -=
                OnStartAction;
        }
    }

    IEnumerator Start()
    {
        if (!startOnPlay)
            yield break;

        // Wait one frame for runtime asteroid spawning.
        yield return null;

        StartMiningCycle();
    }

    void Update()
    {
        switch (state)
        {
            case TowerState.HorizontalAligning:
                UpdateHorizontalAligning();
                break;

            case TowerState.VerticalAligning:
                UpdateVerticalAligning();
                break;

            case TowerState.WaitingForPulse:
                UpdateWaitingForPulse();
                break;

            case TowerState.ChunkHorizontalAligning:
                UpdateChunkHorizontalAligning();
                break;

            case TowerState.ChunkVerticalAligning:
                UpdateChunkVerticalAligning();
                break;

            case TowerState.Extending:
                UpdateExtending();
                break;

            case TowerState.Retracting:
                UpdateRetraction();
                break;

            case TowerState.VerticalReturning:
                UpdateVerticalReturn();
                break;

            case TowerState.Returning:
                UpdateReturnToDefault();
                break;
        }
    }

    void OnStartAction(
        InputAction.CallbackContext context
    )
    {
        StartMiningCycle();
    }

    public void StartMiningCycle()
    {
        if (state != TowerState.Idle)
        {
            Debug.Log(
                $"[MiningTower] Start ignored. " +
                $"Current state = {state}",
                gameObject
            );

            return;
        }
        if (cargo != null && cargo.IsLoaded)
        {
            Debug.Log(
                "[MiningTower] Mining blocked. Cargo is full.",
                gameObject
            );

            return;
        }

        currentTarget =
            targetSelector.FindBestTarget();

        if (currentTarget == null)
        {
            Debug.Log(
                "[MiningTower] No valid target found.",
                gameObject
            );

            return;
        }

        state =
            TowerState.HorizontalAligning;

        Debug.Log(
            $"[MiningTower] Capture START\n" +
            $"Target = {currentTarget.name}\n" +
            $"Yield = {currentTarget.BarrelYield}",
            gameObject
        );
    }

    void UpdateHorizontalAligning()
    {
        if (!ValidateLockedTarget())
            return;

        float yawError =
            UpdateTowerYaw();

        if (
            yawError <=
            horizontalAimTolerance
        )
        {
            state =
                TowerState.VerticalAligning;

            Debug.Log(
                "[MiningTower] Horizontal alignment complete.",
                gameObject
            );
        }
    }

    void UpdateVerticalAligning()
    {
        if (!ValidateLockedTarget())
            return;

        // Horizontal tracking continues during vertical alignment.
        UpdateTowerYaw();

        float requiredArmX =
            CalculateRequiredArmAngleX();

        float armError =
            UpdateArmPitch(
                requiredArmX
            );

        if (
            armError <=
            verticalAimTolerance
        )
        {
            FireMiningPulse();
        }
    }

    void FireMiningPulse()
    {
        if (state != TowerState.VerticalAligning)
            return;

        if (
            miningPulsePrefab == null ||
            firePoint == null ||
            currentTarget == null
        )
        {
            FailCapture(
                "MiningPulse configuration is incomplete."
            );

            return;
        }

        GameObject pulseObject =
            Instantiate(
                miningPulsePrefab,
                firePoint.position,
                firePoint.rotation
            );

        MiningPulse pulse =
            pulseObject.GetComponent<MiningPulse>();

        if (pulse == null)
        {
            Destroy(pulseObject);

            FailCapture(
                "MiningPulse prefab has no MiningPulse component."
            );

            return;
        }

        state =
            TowerState.WaitingForPulse;

        pulse.Initialize(
            currentTarget,
            this,
            miningPulseDamage
        );

        Debug.Log(
            $"[MiningTower] FIRE\n" +
            $"Target = {currentTarget.name}",
            gameObject
        );
    }

    void UpdateWaitingForPulse()
    {
        if (!ValidateLockedTarget())
            return;

        // Continue tracking while the projectile is flying.
        UpdateTowerYaw();

        float requiredArmX =
            CalculateRequiredArmAngleX();

        UpdateArmPitch(
            requiredArmX
        );
    }

    public void OnPulseHit(
        ResourceAsteroid spawnedChunk
    )
    {
        if (
            state !=
            TowerState.WaitingForPulse
        )
        {
            return;
        }

        if (spawnedChunk == null)
        {
            FailCapture(
                "Projectile hit, but no harvestable chunk was created."
            );

            return;
        }

        currentTarget =
            spawnedChunk;

        state =
            TowerState.ChunkHorizontalAligning;

        Debug.Log(
            $"[MiningTower] SHATTER SUCCESS\n" +
            $"Chunk = {spawnedChunk.name}\n" +
            $"Chunk yield = {spawnedChunk.BarrelYield}",
            gameObject
        );
    }

    public void OnPulseFailed(
        string reason
    )
    {
        if (
            state ==
            TowerState.WaitingForPulse
        )
        {
            FailCapture(
                reason
            );
        }
    }

    void UpdateChunkHorizontalAligning()
    {
        if (!ValidateLockedTarget())
            return;

        float yawError =
            UpdateTowerYaw();

        if (
            yawError <=
            horizontalAimTolerance
        )
        {
            state =
                TowerState.ChunkVerticalAligning;
        }
    }

    void UpdateChunkVerticalAligning()
    {
        if (!ValidateLockedTarget())
            return;

        // Horizontal tracking remains active.
        UpdateTowerYaw();

        float requiredArmX =
            CalculateRequiredArmAngleX();

        float armError =
            UpdateArmPitch(
                requiredArmX
            );

        if (
            armError <=
            verticalAimTolerance
        )
        {
            state =
                TowerState.Extending;

            Debug.Log(
                "[MiningTower] Chunk aligned. Extending arm.",
                gameObject
            );
        }
    }

    void UpdateExtending()
    {
        if (!ValidateLockedTarget())
            return;

        // Both axes continue tracking during extension.
        UpdateTowerYaw();

        float requiredArmX =
            CalculateRequiredArmAngleX();

        UpdateArmPitch(
            requiredArmX
        );

        float grabDistance =
            Vector3.Distance(
                grabPoint.position,
                currentTarget.transform.position
            );

        if (
            grabDistance <=
            reachDistance
        )
        {
            CaptureSuccess(
                grabDistance
            );

            return;
        }

        float currentArmReach =
            Vector3.Distance(
                arm.position,
                grabPoint.position
            );

        /*
         * The sensor range is also used as the world-space
         * mechanical arm reach limit.
         */
        float maxArmReach =
            targetSelector.CaptureRange;

        if (
            currentArmReach >=
            maxArmReach
        )
        {
            FailCapture(
                $"Arm reached world-space limit without contact. " +
                $"Arm reach = {currentArmReach:F2} m, " +
                $"Limit = {maxArmReach:F2} m, " +
                $"Grab distance = {grabDistance:F2} m"
            );

            return;
        }

        ExtendArm(
            maxArmReach
        );
    }

    void ExtendArm(
        float maxArmReach
    )
    {
        Vector3 previousScale =
            trackSupport.localScale;

        Vector3 previousHeadPosition =
            armHead.localPosition;

        Vector3 scale =
            trackSupport.localScale;

        scale.y +=
            extensionSpeed *
            Time.deltaTime;

        trackSupport.localScale =
            scale;

        /*
         * Mechanical mapping:
         * +2 TrackSupport Scale Y
         * corresponds to
         * +1 ArmHead local Z.
         */
        float scaleExtension =
            scale.y -
            defaultTrackScale.y;

        Vector3 headPosition =
            armHead.localPosition;

        headPosition.z =
            defaultArmHeadPosition.z +
            scaleExtension * 0.5f;

        armHead.localPosition =
            headPosition;

        float newWorldReach =
            Vector3.Distance(
                arm.position,
                grabPoint.position
            );

        if (
            newWorldReach >
            maxArmReach
        )
        {
            trackSupport.localScale =
                previousScale;

            armHead.localPosition =
                previousHeadPosition;
        }
    }

    float UpdateTowerYaw()
    {
        Vector3 direction =
            currentTarget.transform.position -
            towerRoot.position;

        direction.y = 0f;

        if (
            direction.sqrMagnitude <
            0.001f
        )
        {
            return 0f;
        }

        Quaternion lookRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );

        Quaternion desiredRotation =
            lookRotation *
            Quaternion.Euler(
                0f,
                towerYawOffset,
                0f
            );

        towerRoot.rotation =
            Quaternion.RotateTowards(
                towerRoot.rotation,
                desiredRotation,
                towerRotationSpeed *
                Time.deltaTime
            );

        return Quaternion.Angle(
            towerRoot.rotation,
            desiredRotation
        );
    }

    float CalculateRequiredArmAngleX()
    {
        Vector3 direction =
            currentTarget.transform.position -
            arm.position;

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

        // Negative local X points the arm upward.
        return -elevationAngle;
    }

    float UpdateArmPitch(
        float desiredArmX
    )
    {
        float clampedArmX =
            Mathf.Clamp(
                desiredArmX,
                minArmAngleX,
                maxArmAngleX
            );

        float currentX =
            NormalizeAngle(
                arm.localEulerAngles.x
            );

        float newX =
            Mathf.MoveTowardsAngle(
                currentX,
                clampedArmX,
                armRotationSpeed *
                Time.deltaTime
            );

        Vector3 currentEuler =
            arm.localEulerAngles;

        arm.localRotation =
            Quaternion.Euler(
                newX,
                currentEuler.y,
                currentEuler.z
            );

        return Mathf.Abs(
            Mathf.DeltaAngle(
                newX,
                clampedArmX
            )
        );
    }

    bool ValidateLockedTarget()
    {
        if (currentTarget == null)
        {
            FailCapture(
                "Target was destroyed or lost."
            );

            return false;
        }

        float targetDistance =
            Vector3.Distance(
                towerRoot.position,
                currentTarget.transform.position
            );

        if (
            targetDistance >
            targetSelector.CaptureRange
        )
        {
            FailCapture(
                $"Target escaped capture range. " +
                $"Distance = {targetDistance:F2} m, " +
                $"Range = {targetSelector.CaptureRange:F2} m"
            );

            return false;
        }

        float requiredArmX =
            CalculateRequiredArmAngleX();

        if (
            requiredArmX <
            minArmAngleX ||
            requiredArmX >
            maxArmAngleX
        )
        {
            FailCapture(
                $"Target entered vertical blind zone. " +
                $"Required Arm X = {requiredArmX:F1}°, " +
                $"Allowed = {minArmAngleX:F1}° to {maxArmAngleX:F1}°"
            );

            return false;
        }

        return true;
    }

    void CaptureSuccess(float grabDistance)
    {
        if (currentTarget == null)
        {
            FailCapture(
                "Capture succeeded geometrically, but target was lost."
            );

            return;
        }

        if (!currentTarget.IsChunk)
        {
            FailCapture(
                "Mining arm can only capture a harvestable chunk."
            );

            return;
        }

        carriedChunk =
            currentTarget;

        carriedChunk.SetCaptured(true);

        /*
        * Keep the chunk's current world transform while parenting it
        * to the GrabPoint. It will now follow the mining arm.
        */
        carriedChunk.transform.SetParent(
            grabPoint,
            true
        );

        Debug.Log(
            $"[MiningTower] Capture SUCCESS\n" +
            $"Chunk = {carriedChunk.name}\n" +
            $"Yield = {carriedChunk.BarrelYield}\n" +
            $"Grab distance = {grabDistance:F3} m",
            gameObject
        );

        state =
            TowerState.Retracting;
    }

    void FailCapture(
        string reason
    )
    {
        string targetName =
            currentTarget != null
                ? currentTarget.name
                : "None";

        Debug.LogWarning(
            $"[MiningTower] Capture FAILED\n" +
            $"Target = {targetName}\n" +
            $"Reason = {reason}",
            gameObject
        );

        state =
            TowerState.Retracting;
    }

    void UpdateRetraction()
    {
        Vector3 scale =
            trackSupport.localScale;

        scale.y =
            Mathf.MoveTowards(
                scale.y,
                defaultTrackScale.y,
                extensionSpeed *
                Time.deltaTime
            );

        trackSupport.localScale =
            scale;

        float scaleExtension =
            scale.y -
            defaultTrackScale.y;

        Vector3 headPosition =
            armHead.localPosition;

        headPosition.z =
            defaultArmHeadPosition.z +
            scaleExtension * 0.5f;

        armHead.localPosition =
            headPosition;

        bool extensionReturned =
            Mathf.Abs(
                scale.y -
                defaultTrackScale.y
            ) < 0.01f;

        if (!extensionReturned)
            return;

        trackSupport.localScale =
            defaultTrackScale;

        armHead.localPosition =
            defaultArmHeadPosition;

        state =
            TowerState.VerticalReturning;

        Debug.Log(
            "[MiningTower] Arm retracted.",
            gameObject
        );
    }

    void UpdateVerticalReturn()
    {
        float currentArmX =
            NormalizeAngle(
                arm.localEulerAngles.x
            );

        float defaultArmX =
            NormalizeAngle(
                defaultArmLocalRotation.eulerAngles.x
            );

        float newArmX =
            Mathf.MoveTowardsAngle(
                currentArmX,
                defaultArmX,
                armRotationSpeed *
                Time.deltaTime
            );

        Vector3 armEuler =
            arm.localEulerAngles;

        arm.localRotation =
            Quaternion.Euler(
                newArmX,
                armEuler.y,
                armEuler.z
            );

        bool returned =
            Mathf.Abs(
                Mathf.DeltaAngle(
                    newArmX,
                    defaultArmX
                )
            ) < 1f;

        if (!returned)
            return;

        arm.localRotation =
            defaultArmLocalRotation;

        state =
            TowerState.Returning;

        Debug.Log(
            "[MiningTower] Arm vertical angle returned.",
            gameObject
        );
    }

    void UpdateReturnToDefault()
    {
        towerRoot.localRotation =
            Quaternion.RotateTowards(
                towerRoot.localRotation,
                defaultTowerLocalRotation,
                towerRotationSpeed *
                Time.deltaTime
            );

        float remainingAngle =
            Quaternion.Angle(
                towerRoot.localRotation,
                defaultTowerLocalRotation
            );

        if (remainingAngle > 1f)
            return;

        towerRoot.localRotation =
            defaultTowerLocalRotation;

        if (carriedChunk != null)
        {
            int yield =
                carriedChunk.BarrelYield;

            AsteroidFamily family =
                carriedChunk.Family;

            cargo.TryLoad(
                yield,
                family
            );

            Destroy(
                carriedChunk.gameObject
            );

            carriedChunk = null;
        }
        
        currentTarget = null;

        targetSelector.ClearTarget();

        state =
            TowerState.Idle;

        Debug.Log(
            "[MiningTower] Returned to IDLE.",
            gameObject
        );
    }

    float NormalizeAngle(
        float angle
    )
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}