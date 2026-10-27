using UnityEngine;

public class MiningCargoTransferController : MonoBehaviour
{
    public enum TransferState
    {
        Idle,
        Lowering,
        Processing,
        Raising
    }

    [Header("References")]
    [SerializeField] Transform storage;
    [SerializeField] MiningCargo cargo;
    [SerializeField] OrbitalAnchorProcessor processor;

    [Header("Movement")]
    [SerializeField] float processingLocalY = 0f;
    [SerializeField] float moveSpeed = 0.75f;

    TransferState state =
        TransferState.Idle;

    Vector3 defaultStorageLocalPosition;

    public TransferState State =>
        state;

    public float SpeedMultiplier { get; set; } = 1f;

    public bool IsBusy =>
        state != TransferState.Idle;

    void Awake()
    {
        defaultStorageLocalPosition =
            storage.localPosition;
    }

    void Update()
    {
        switch (state)
        {
            case TransferState.Lowering:
                UpdateLowering();
                break;

            case TransferState.Raising:
                UpdateRaising();
                break;
        }
    }

    public bool TryStartUnload()
    {
        if (state != TransferState.Idle)
            return false;

        if (
            cargo == null ||
            !cargo.IsLoaded
        )
        {
            Debug.Log(
                "[CargoTransfer] No cargo to unload.",
                gameObject
            );

            return false;
        }

        state =
            TransferState.Lowering;

        Debug.Log(
            "[CargoTransfer] UNLOAD START",
            gameObject
        );

        return true;
    }

    void UpdateLowering()
    {
        Vector3 position =
            storage.localPosition;

        position.y =
            Mathf.MoveTowards(
                position.y,
                processingLocalY,
                moveSpeed *
                SpeedMultiplier *
                Time.deltaTime
            );

        storage.localPosition =
            position;

        if (
            Mathf.Abs(
                position.y -
                processingLocalY
            ) > 0.001f
        )
        {
            return;
        }

        position.y =
            processingLocalY;

        storage.localPosition =
            position;

        state =
            TransferState.Processing;

        Debug.Log(
            "[CargoTransfer] Storage reached processing position.",
            gameObject
        );

        processor.BeginProcessing(
            cargo,
            OnProcessingComplete
        );
    }

    void OnProcessingComplete()
    {
        state =
            TransferState.Raising;

        Debug.Log(
            "[CargoTransfer] Processing complete. Returning storage.",
            gameObject
        );
    }

    void UpdateRaising()
    {
        Vector3 position =
            storage.localPosition;

        position.y =
            Mathf.MoveTowards(
                position.y,
                defaultStorageLocalPosition.y,
                moveSpeed *
                SpeedMultiplier *
                Time.deltaTime
            );

        storage.localPosition =
            position;

        if (
            Mathf.Abs(
                position.y -
                defaultStorageLocalPosition.y
            ) > 0.001f
        )
        {
            return;
        }

        storage.localPosition =
            defaultStorageLocalPosition;

        state =
            TransferState.Idle;

        Debug.Log(
            "[CargoTransfer] Storage returned to default position.",
            gameObject
        );
    }
}