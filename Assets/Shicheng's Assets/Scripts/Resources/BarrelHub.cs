using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BarrelHub : MonoBehaviour
{
    [Header("Repositories")]
    [SerializeField] Transform[] repositories;

    [Range(1, 8)]
    [SerializeField] int unlockedRepositoryCount = 1;

    [Header("Barrel Materials")]
    [SerializeField] Material slateMaterial;
    [SerializeField] Material frostMaterial;
    [SerializeField] Material basaltMaterial;
    [SerializeField] Material abyssalMaterial;
    [SerializeField] Material crystalMaterial;

    [Header("Barrel Prices")]
    [SerializeField] int slateCreditsPerBarrel = 5;
    [SerializeField] int frostCreditsPerBarrel = 6;
    [SerializeField] int basaltCreditsPerBarrel = 8;
    [SerializeField] int abyssalCreditsPerBarrel = 10;
    [SerializeField] int crystalCreditsPerBarrel = 12;

    [Header("Economy")]
    [SerializeField] CurrencyManager currency;

    [Header("Testing")]
    [SerializeField] bool pressSToSellForTesting = true;

    readonly List<GameObject> barrelSlots =
        new List<GameObject>();

    readonly List<AsteroidFamily> storedBarrels =
        new List<AsteroidFamily>();

    public event Action OnResourcesChanged;

    public int UnlockedRepositoryCount =>
        unlockedRepositoryCount;

    public int MaxRepositoryCount =>
        repositories != null
            ? repositories.Length
            : 0;

    public int BarrelCount =>
        storedBarrels.Count;

    public int Capacity =>
        Mathf.Clamp(
            unlockedRepositoryCount,
            1,
            repositories.Length
        ) * 4;

    public int FreeSlots =>
        Capacity - BarrelCount;

    public bool IsFull =>
        BarrelCount >= Capacity;

    public int SlateBarrels =>
        GetResourceCount(AsteroidFamily.Slate);

    public int FrostBarrels =>
        GetResourceCount(AsteroidFamily.Frost);

    public int BasaltBarrels =>
        GetResourceCount(AsteroidFamily.Basalt);

    public int AbyssalBarrels =>
        GetResourceCount(AsteroidFamily.Abyssal);

    public int CrystalBarrels =>
        GetResourceCount(AsteroidFamily.Crystal);

    public int TotalStoredValue =>
        CalculateStoredValue();

    void Awake()
    {
        if (currency == null)
        {
            currency =
                FindFirstObjectByType<CurrencyManager>();

            if (currency == null)
            {
                Debug.LogWarning(
                    "[BarrelHub] CurrencyManager was not found.",
                    gameObject
                );
            }
        }

        BuildBarrelSlotList();

        storedBarrels.Clear();

        RefreshVisuals();
    }

    void Update()
    {
        if (
            pressSToSellForTesting &&
            Keyboard.current != null &&
            Keyboard.current.sKey.wasPressedThisFrame
        )
        {
            SellAll();
        }
    }

    void BuildBarrelSlotList()
    {
        barrelSlots.Clear();

        if (repositories == null)
            return;

        foreach (Transform repository in repositories)
        {
            if (repository == null)
                continue;

            for (
                int i = 0;
                i < repository.childCount;
                i++
            )
            {
                Transform child =
                    repository.GetChild(i);

                if (
                    child.name.StartsWith(
                        "barrel",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    barrelSlots.Add(
                        child.gameObject
                    );
                }
            }
        }

        Debug.Log(
            $"[BarrelHub] Found {barrelSlots.Count} barrel slots.",
            gameObject
        );
    }

    public bool TryStoreOneBarrel(
        AsteroidFamily family
    )
    {
        if (IsFull)
        {
            Debug.Log(
                "[BarrelHub] Storage full.",
                gameObject
            );

            return false;
        }

        storedBarrels.Add(
            family
        );

        RefreshVisuals();

        OnResourcesChanged?.Invoke();

        Debug.Log(
            $"[BarrelHub] STORED\n" +
            $"Type = {family}\n" +
            $"Unit Value = {GetPriceForFamily(family)}\n" +
            $"Storage = {BarrelCount}/{Capacity}\n" +
            $"Total Stored Value = {TotalStoredValue}",
            gameObject
        );

        return true;
    }

    public int GetResourceCount(
        AsteroidFamily family
    )
    {
        int count = 0;

        foreach (
            AsteroidFamily storedFamily
            in storedBarrels
        )
        {
            if (
                storedFamily ==
                family
            )
            {
                count++;
            }
        }

        return count;
    }

    public int GetPriceForFamily(
        AsteroidFamily family
    )
    {
        switch (family)
        {
            case AsteroidFamily.Slate:
                return slateCreditsPerBarrel;

            case AsteroidFamily.Frost:
                return frostCreditsPerBarrel;

            case AsteroidFamily.Basalt:
                return basaltCreditsPerBarrel;

            case AsteroidFamily.Abyssal:
                return abyssalCreditsPerBarrel;

            case AsteroidFamily.Crystal:
                return crystalCreditsPerBarrel;

            default:
                return 0;
        }
    }

    int CalculateStoredValue()
    {
        int totalValue = 0;

        foreach (
            AsteroidFamily family
            in storedBarrels
        )
        {
            totalValue +=
                GetPriceForFamily(family);
        }

        return totalValue;
    }

    public void SellAll()
    {
        if (BarrelCount <= 0)
        {
            Debug.Log(
                "[BarrelHub] Sell ignored. No barrels stored.",
                gameObject
            );

            return;
        }

        int slateCount =
            SlateBarrels;

        int frostCount =
            FrostBarrels;

        int basaltCount =
            BasaltBarrels;

        int abyssalCount =
            AbyssalBarrels;

        int crystalCount =
            CrystalBarrels;

        int sold =
            BarrelCount;

        int earnedCredits =
            CalculateStoredValue();

        storedBarrels.Clear();

        if (currency != null)
        {
            currency.AddMoney(
                earnedCredits
            );
        }

        RefreshVisuals();

        OnResourcesChanged?.Invoke();

        Debug.Log(
            $"[BarrelHub] SELL ALL\n" +
            $"Slate = {slateCount} x {slateCreditsPerBarrel}\n" +
            $"Frost = {frostCount} x {frostCreditsPerBarrel}\n" +
            $"Basalt = {basaltCount} x {basaltCreditsPerBarrel}\n" +
            $"Abyssal = {abyssalCount} x {abyssalCreditsPerBarrel}\n" +
            $"Crystal = {crystalCount} x {crystalCreditsPerBarrel}\n" +
            $"Total barrels sold = {sold}\n" +
            $"Credits earned = {earnedCredits}",
            gameObject
        );
    }

    public bool UnlockNextRepository()
    {
        if (
            unlockedRepositoryCount >=
            repositories.Length
        )
        {
            return false;
        }

        unlockedRepositoryCount++;

        RefreshVisuals();

        Debug.Log(
            $"[BarrelHub] Repository unlocked. " +
            $"Capacity = {Capacity}",
            gameObject
        );

        return true;
    }

    void RefreshVisuals()
    {
        for (
            int i = 0;
            i < repositories.Length;
            i++
        )
        {
            if (repositories[i] == null)
                continue;

            repositories[i].gameObject.SetActive(
                i <
                unlockedRepositoryCount
            );
        }

        for (
            int i = 0;
            i < barrelSlots.Count;
            i++
        )
        {
            bool shouldBeVisible =
                i < storedBarrels.Count;

            barrelSlots[i].SetActive(
                shouldBeVisible
            );

            if (!shouldBeVisible)
                continue;

            AsteroidFamily family =
                storedBarrels[i];

            Material material =
                GetMaterialForFamily(
                    family
                );

            ApplyMaterial(
                barrelSlots[i],
                material
            );
        }
    }

    Material GetMaterialForFamily(
        AsteroidFamily family
    )
    {
        switch (family)
        {
            case AsteroidFamily.Slate:
                return slateMaterial;

            case AsteroidFamily.Frost:
                return frostMaterial;

            case AsteroidFamily.Basalt:
                return basaltMaterial;

            case AsteroidFamily.Abyssal:
                return abyssalMaterial;

            case AsteroidFamily.Crystal:
                return crystalMaterial;

            default:
                return slateMaterial;
        }
    }

    void ApplyMaterial(
        GameObject barrelObject,
        Material material
    )
    {
        if (
            barrelObject == null ||
            material == null
        )
        {
            return;
        }

        Renderer[] renderers =
            barrelObject.GetComponentsInChildren<Renderer>(
                true
            );

        foreach (
            Renderer barrelRenderer
            in renderers
        )
        {
            barrelRenderer.material =
                material;
        }
    }

    
}