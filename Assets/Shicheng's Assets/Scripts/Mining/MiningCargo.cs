using UnityEngine;

public class MiningCargo : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] GameObject meteorVisual;

    bool isLoaded;
    int storedBarrelYield;

    Vector3 defaultMeteorScale;

    public bool IsLoaded => isLoaded;

    // This value also represents the CargoLoad stat.
    public int StoredBarrelYield => storedBarrelYield;

    void Awake()
    {
        if (meteorVisual != null)
        {
            defaultMeteorScale =
                meteorVisual.transform.localScale;

            meteorVisual.SetActive(false);
        }
    }

    public bool TryLoad(int barrelYield)
    {
        if (isLoaded)
        {
            Debug.LogWarning(
                "[MiningCargo] Load rejected. Cargo is already full.",
                gameObject
            );

            return false;
        }

        storedBarrelYield =
            Mathf.Clamp(barrelYield, 1, 64);

        isLoaded = true;

        if (meteorVisual != null)
        {
            meteorVisual.transform.localScale =
                defaultMeteorScale;

            meteorVisual.SetActive(true);
        }

        Debug.Log(
            $"[MiningCargo] LOAD SUCCESS\n" +
            $"CargoLoad = {storedBarrelYield}",
            gameObject
        );

        return true;
    }

    public void SetProcessingProgress01(float progress01)
    {
        if (meteorVisual == null)
            return;

        progress01 =
            Mathf.Clamp01(progress01);

        Vector3 scale =
            defaultMeteorScale;

        scale.y =
            defaultMeteorScale.y *
            (1f - progress01);

        meteorVisual.transform.localScale =
            scale;
    }

    public void FinishProcessing()
    {
        storedBarrelYield = 0;
        isLoaded = false;

        if (meteorVisual != null)
        {
            meteorVisual.transform.localScale =
                defaultMeteorScale;

            meteorVisual.SetActive(false);
        }

        Debug.Log(
            "[MiningCargo] Processing complete. Cargo is empty.",
            gameObject
        );
    }
}