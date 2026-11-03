using UnityEngine;
//using static Unity.VisualScripting.Round<TInput, TOutput>;
using Random = UnityEngine.Random;

namespace Brian
{
    /// <summary>
    /// A planet that slowly builds up gas, and is also a build slot: it needs a TowerNode
    /// set to Node Type = Gas, and a GasExtractor can only be built on it.
    /// </summary>
    [RequireComponent(typeof(TowerNode))]
    public class GasPlanet : MonoBehaviour
    {
        [SerializeField] private float min = 5f;
        [SerializeField] private float max = 24f;
        [SerializeField] private float speed = 2f;
        [SerializeField] private float maxBrightness = 3f;
        private double gas = 0;

        private Renderer targetRenderer;
        private Material material;
        private Color originalEmission;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>Gas currently built up and available to extract.</summary>
        public double Stored => gas;
        public bool Running { get; private set; }

        void Start()
        {
            targetRenderer = GetComponent<Renderer>();
            // Use GetComponentInChildren<Renderer>() if the renderer is on a child object

            if (targetRenderer == null)
            {
                Debug.LogError($"{name}: No Renderer found for GasPlanet.", this);
                enabled = false;
                return;
            }

            material = targetRenderer.material; // per-object instance
            material.EnableKeyword("_EMISSION");

            originalEmission = material.GetColor(EmissionColorId);

            // If the material's emission color is black, there's nothing to scale,
            // so fall back to the base color so the pulse is still visible.
            if (originalEmission.maxColorComponent <= 0f)
            {
                originalEmission = material.color;
            }

            speed += Random.Range(-1, 1);
        }

        void Update()
        {
            float oscillationRange = (max - min) / 2f;
            float oscillationOffset = oscillationRange + min;
            float wave = Mathf.Sin(Time.time * speed);


            float intensity = ((wave + 1f) / 2f) * maxBrightness;
            material.SetColor(EmissionColorId, originalEmission * intensity);

            var director = WaveDirector.Instance;
            var gm = GameManager.Instance;
            Running = director != null && director.WaveRunning && (gm == null || gm.IsPlaying);
            if(!Running) { return; } // run only when there is a wave

            gas += Time.deltaTime * (oscillationOffset + wave * oscillationRange);
        }

        void OnDestroy()
        {
            // .material creates an instance, so clean it up
            if (material != null) Destroy(material);
        }

        /// <summary>Takes all the gas that has built up and resets the pool to zero.</summary>
        public double Extract()
        {
            double extracted = gas;
            gas = 0;
            return extracted;
        }
    }
}