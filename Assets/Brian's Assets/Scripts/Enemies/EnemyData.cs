using UnityEngine;

namespace Brian
{
    [CreateAssetMenu(menuName = "Brian/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public string displayName = "Enemy";
        public Enemy prefab;
        public float maxHealth = 100f;
        public float maxSpeed = 0.6f;
        [Range(0.05f, 1f)] public float minSpeedFraction = 0.35f;
        public int homeDamage = 1;
    }
}
