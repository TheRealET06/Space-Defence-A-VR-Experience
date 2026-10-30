using UnityEngine;
using UnityEngine.Events;

namespace Brian
{
    /// <summary>
    /// A finite pool of gems that is also a build slot: it needs a TowerNode set to
    /// Node Type = Gem, and a GemExtractor can only be built on it.
    /// </summary>
    [RequireComponent(typeof(TowerNode))]
    public class CrystalAsteroid : MonoBehaviour
    {
        public double gems = 1000;
        public bool depleated = false;                  // (sic) kept so existing references keep working
        public UnityEvent gemsGone;

        public bool Depleted => depleated;
        public double Remaining => gems;

        void Awake() => depleated = gems <= 0;

        /// <summary>Removes up to 'amount' gems and returns how many were actually taken.</summary>
        public double Extract(double amount)
        {
            if (depleated || amount <= 0) return 0;

            double taken = System.Math.Min(amount, gems);
            gems -= taken;

            if (gems <= 0)
            {
                gems = 0;
                depleated = true;
                gemsGone.Invoke();
            }
            return taken;
        }
    }
}