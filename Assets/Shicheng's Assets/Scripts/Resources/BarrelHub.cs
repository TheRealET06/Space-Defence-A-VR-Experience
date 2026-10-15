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

    [Header("Economy")]
    [SerializeField] int creditsPerBarrel = 10;

    [SerializeField] CurrencyManager currency;

    [Header("Testing")]
    [SerializeField] bool pressSToSellForTesting = true;

    readonly List<GameObject> barrelSlots =
        new List<GameObject>();

    int barrelCount;

    public int BarrelCount => barrelCount;

    public int Capacity =>
        Mathf.Clamp(
            unlockedRepositoryCount,
            1,
            repositories.Length
        ) * 4;

    public int FreeSlots =>
        Capacity - barrelCount;

    public bool IsFull =>
        barrelCount >= Capacity;

    void Awake()
    {
        BuildBarrelSlotList();

        barrelCount = 0;

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
            $"[BarrelHub] Found {barrelSlots.Count} barrel visual slots.",
            gameObject
        );
    }

    public bool TryStoreOneBarrel()
    {
        if (IsFull)
        {
            Debug.Log(
                "[BarrelHub] Storage full.",
                gameObject
            );

            return false;
        }

        barrelCount++;

        RefreshVisuals();

        Debug.Log(
            $"[BarrelHub] Barrel stored: " +
            $"{barrelCount}/{Capacity}",
            gameObject
        );

        return true;
    }

    public void SellAll()
    {
        if (barrelCount <= 0)
        {
            Debug.Log(
                "[BarrelHub] Sell ignored. No barrels stored.",
                gameObject
            );

            return;
        }

        int sold =
            barrelCount;

        int earnedCredits =
            sold * creditsPerBarrel;

        barrelCount = 0;

        if (currency != null)
        {
            currency.Add(
                earnedCredits
            );
        }

        RefreshVisuals();

        Debug.Log(
            $"[BarrelHub] SELL ALL\n" +
            $"Barrels sold = {sold}\n" +
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
                i < unlockedRepositoryCount
            );
        }

        for (
            int i = 0;
            i < barrelSlots.Count;
            i++
        )
        {
            barrelSlots[i].SetActive(
                i < barrelCount
            );
        }
    }
}