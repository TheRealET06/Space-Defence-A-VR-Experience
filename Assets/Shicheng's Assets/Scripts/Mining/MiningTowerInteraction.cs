using UnityEngine;
using UnityEngine.InputSystem;

public class MiningTowerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] MiningTowerController miningController;
    [SerializeField] MiningCargo cargo;
    [SerializeField] MiningCargoTransferController cargoTransfer;

    [Header("Testing")]
    [SerializeField] bool pressEToInteractForTesting = true;

    void Update()
    {
        if (
            pressEToInteractForTesting &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame
        )
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (cargoTransfer.IsBusy)
        {
            Debug.Log(
                "[MiningTowerInteraction] Interaction ignored. Cargo transfer is busy.",
                gameObject
            );

            return;
        }

        if (cargo.IsLoaded)
        {
            Debug.Log(
                "[MiningTowerInteraction] Unloading cargo.",
                gameObject
            );

            cargoTransfer.TryStartUnload();

            return;
        }

        Debug.Log(
            "[MiningTowerInteraction] Starting mining cycle.",
            gameObject
        );

        miningController.StartMiningCycle();
    }
}