using TMPro;
using UnityEngine;

public class BarrelResourceHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] BarrelHub barrelHub;

    [Header("Resource Text")]
    [SerializeField] TMP_Text slateText;
    [SerializeField] TMP_Text frostText;
    [SerializeField] TMP_Text basaltText;
    [SerializeField] TMP_Text abyssalText;
    [SerializeField] TMP_Text crystalText;

    void OnEnable()
    {
        if (barrelHub != null)
        {
            barrelHub.OnResourcesChanged += Refresh;
        }

        Refresh();
    }

    void OnDisable()
    {
        if (barrelHub != null)
        {
            barrelHub.OnResourcesChanged -= Refresh;
        }
    }

    void Refresh()
    {
        if (barrelHub == null)
            return;

        slateText.text =
            $"Slate: {barrelHub.SlateBarrels}";
            // $"[${barrelHub.GetPriceForFamily(AsteroidFamily.Slate)} each]";

        frostText.text =
            $"Frost: {barrelHub.FrostBarrels}";
            // $"[${barrelHub.GetPriceForFamily(AsteroidFamily.Frost)} each]";

        basaltText.text =
            $"Basalt: {barrelHub.BasaltBarrels}";
            // $"[${barrelHub.GetPriceForFamily(AsteroidFamily.Basalt)} each]";

        abyssalText.text =
            $"Abyssal: {barrelHub.AbyssalBarrels}";
            // $"[${barrelHub.GetPriceForFamily(AsteroidFamily.Abyssal)} each]";

        crystalText.text =
            $"Crystal: {barrelHub.CrystalBarrels}";
            // $"[${barrelHub.GetPriceForFamily(AsteroidFamily.Crystal)} each]";
    }
}