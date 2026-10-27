using System;
using UnityEngine;

public class OrbitalAnchorProcessor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] BarrelHub barrelHub;

    [Header("Processing")]
    [SerializeField] float barrelsPerSecond = 1f;

    [Header("Runtime Stat")]
    [SerializeField] float processingProgress;

    MiningCargo activeCargo;
    AsteroidFamily activeFamily;

    int totalBarrels;
    int producedBarrels;

    bool isProcessing;

    Action completionCallback;

    public float ProcessingProgress =>
        processingProgress;

    public bool IsProcessing =>
        isProcessing;
    
    public float BarrelsPerSecond =>
        barrelsPerSecond * SpeedMultiplier;

    public float SpeedMultiplier { get; set; } = 1f;

    public void BeginProcessing(
        MiningCargo cargo,
        Action onComplete
    )
    {
        if (isProcessing)
        {
            Debug.LogWarning(
                "[AnchorProcessor] Processing already active.",
                gameObject
            );

            return;
        }

        if (
            cargo == null ||
            !cargo.IsLoaded
        )
        {
            Debug.LogWarning(
                "[AnchorProcessor] Cannot process empty cargo.",
                gameObject
            );

            return;
        }

        activeCargo =
            cargo;

        totalBarrels =
            cargo.StoredBarrelYield;
        activeFamily =
            cargo.StoredFamily;

        producedBarrels = 0;
        processingProgress = 0f;

        completionCallback =
            onComplete;

        isProcessing = true;

        activeCargo.SetProcessingProgress01(
            0f
        );

        Debug.Log(
            $"[AnchorProcessor] PROCESSING START\n" +
            $"Resource = {activeFamily}\n" +
            $"Target barrel count = {totalBarrels}",
            gameObject
        );
    }

    void Update()
    {
        if (!isProcessing)
            return;

        if (
            barrelHub == null ||
            activeCargo == null
        )
        {
            AbortProcessing(
                "Missing BarrelHub or MiningCargo."
            );

            return;
        }

        /*
         * Processing pauses automatically when the active
         * repositories have no free barrel slots.
         */
        if (barrelHub.IsFull)
            return;

        /*
         * Euler integration:
         * progress(t + dt) =
         * progress(t) + rate * dt
         */
        float nextProgress =
            processingProgress +
            barrelsPerSecond *
            SpeedMultiplier *
            Time.deltaTime;

        nextProgress =
            Mathf.Min(
                nextProgress,
                totalBarrels
            );

        int targetProducedCount =
            Mathf.FloorToInt(
                nextProgress
            );

        while (
            producedBarrels <
            targetProducedCount
        )
        {
            bool stored =
                barrelHub.TryStoreOneBarrel(
                    activeFamily
                );

            if (!stored)
            {
                /*
                 * Freeze progress at the last successfully
                 * produced barrel when storage becomes full.
                 */
                processingProgress =
                    producedBarrels;

                UpdateCargoVisual();

                return;
            }

            producedBarrels++;
        }

        processingProgress =
            nextProgress;

        UpdateCargoVisual();

        if (
            producedBarrels >=
            totalBarrels &&
            processingProgress >=
            totalBarrels
        )
        {
            CompleteProcessing();
        }
    }

    void UpdateCargoVisual()
    {
        if (
            activeCargo == null ||
            totalBarrels <= 0
        )
        {
            return;
        }

        float progress01 =
            processingProgress /
            totalBarrels;

        activeCargo.SetProcessingProgress01(
            progress01
        );
    }

    void CompleteProcessing()
    {
        isProcessing = false;

        activeCargo.FinishProcessing();

        Debug.Log(
            $"[AnchorProcessor] PROCESSING COMPLETE\n" +
            $"Barrels produced = {producedBarrels}",
            gameObject
        );

        MiningCargo finishedCargo =
            activeCargo;

        activeCargo = null;

        Action callback =
            completionCallback;

        completionCallback = null;

        callback?.Invoke();
    }

    void AbortProcessing(
        string reason
    )
    {
        Debug.LogError(
            $"[AnchorProcessor] Processing aborted.\n" +
            $"Reason = {reason}",
            gameObject
        );

        isProcessing = false;
        activeCargo = null;
        completionCallback = null;
    }

    public void IncreaseProcessingSpeed(
        float amount
    )
    {
        barrelsPerSecond =
            Mathf.Max(
                0.1f,
                barrelsPerSecond + amount
            );

        Debug.Log(
            $"[OrbitalAnchorProcessor] Processing speed = {barrelsPerSecond:F1} barrels/s.",
            gameObject
        );
    }
}