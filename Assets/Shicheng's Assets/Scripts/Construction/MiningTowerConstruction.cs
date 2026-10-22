using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MiningTowerBuildStep
{
    public string stepName;

    [Tooltip("All objects constructed during this step.")]
    public GameObject[] objects;

    [Min(0.05f)]
    public float duration = 1f;
}

public class MiningTowerConstruction : MonoBehaviour
{
    [Header("Construction")]
    [SerializeField] MiningTowerBuildStep[] buildSteps;

    [SerializeField] Material constructionMaterial;

    [Range(0f, 1f)]
    [SerializeField] float startingAlpha = 0.05f;

    [SerializeField] float delayBetweenSteps = 0.1f;

    [Header("Future Upgrade Objects")]
    [Tooltip("These objects remain hidden after initial construction.")]
    [SerializeField] GameObject[] futureUpgradeObjects;

    [Header("Gameplay")]
    [Tooltip("These behaviours are enabled only after construction is complete.")]
    [SerializeField] Behaviour[] gameplayBehaviours;

    [Header("Startup")]
    [SerializeField] bool buildAutomaticallyOnStart = true;

    bool isBuilding;
    bool isComplete;

    readonly List<RendererInfo> rendererInfos =
        new List<RendererInfo>();

    readonly List<ColliderInfo> colliderInfos =
        new List<ColliderInfo>();

    public bool IsBuilding => isBuilding;
    public bool IsComplete => isComplete;

    public event Action OnConstructionCompleted;

    class RendererInfo
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public bool originalEnabled;
    }

    class ColliderInfo
    {
        public Collider collider;
        public bool originalEnabled;
    }

    void Awake()
    {
        CacheInitialState();

        HideInitialConstruction();

        HideFutureUpgradeObjects();

        SetGameplayEnabled(false);
    }

    IEnumerator Start()
    {
        if (!buildAutomaticallyOnStart)
            yield break;

        // Wait one frame so the placed prefab is fully initialized.
        yield return null;

        BeginConstruction();
    }

    void CacheInitialState()
    {
        rendererInfos.Clear();
        colliderInfos.Clear();

        HashSet<Renderer> cachedRenderers =
            new HashSet<Renderer>();

        HashSet<Collider> cachedColliders =
            new HashSet<Collider>();

        foreach (MiningTowerBuildStep step in buildSteps)
        {
            if (step.objects == null)
                continue;

            foreach (GameObject obj in step.objects)
            {
                if (obj == null)
                    continue;

                Renderer[] renderers =
                    obj.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer renderer in renderers)
                {
                    if (
                        renderer == null ||
                        cachedRenderers.Contains(renderer)
                    )
                    {
                        continue;
                    }

                    cachedRenderers.Add(renderer);

                    RendererInfo info =
                        new RendererInfo
                        {
                            renderer = renderer,
                            originalMaterials =
                                renderer.sharedMaterials,
                            originalEnabled =
                                renderer.enabled
                        };

                    rendererInfos.Add(info);
                }

                Collider[] colliders =
                    obj.GetComponentsInChildren<Collider>(true);

                foreach (Collider col in colliders)
                {
                    if (
                        col == null ||
                        cachedColliders.Contains(col)
                    )
                    {
                        continue;
                    }

                    cachedColliders.Add(col);

                    ColliderInfo info =
                        new ColliderInfo
                        {
                            collider = col,
                            originalEnabled =
                                col.enabled
                        };

                    colliderInfos.Add(info);
                }
            }
        }
    }

    void HideInitialConstruction()
    {
        foreach (RendererInfo info in rendererInfos)
        {
            if (info.renderer != null)
            {
                info.renderer.enabled = false;
            }
        }

        foreach (ColliderInfo info in colliderInfos)
        {
            if (info.collider != null)
            {
                info.collider.enabled = false;
            }
        }
    }

    void HideFutureUpgradeObjects()
    {
        if (futureUpgradeObjects == null)
            return;

        foreach (GameObject obj in futureUpgradeObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    public bool BeginConstruction()
    {
        if (isBuilding || isComplete)
            return false;

        StartCoroutine(
            ConstructionRoutine()
        );

        return true;
    }

    IEnumerator ConstructionRoutine()
    {
        isBuilding = true;

        Debug.Log(
            "[MiningTowerConstruction] Construction started.",
            gameObject
        );

        for (
            int i = 0;
            i < buildSteps.Length;
            i++
        )
        {
            MiningTowerBuildStep step =
                buildSteps[i];

            Debug.Log(
                $"[MiningTowerConstruction] Building: {step.stepName}",
                gameObject
            );

            yield return
                BuildStep(step);

            if (
                delayBetweenSteps > 0f &&
                i < buildSteps.Length - 1
            )
            {
                yield return
                    new WaitForSeconds(
                        delayBetweenSteps
                    );
            }
        }

        isBuilding = false;
        isComplete = true;

        SetGameplayEnabled(true);

        Debug.Log(
            "[MiningTowerConstruction] Construction complete. Tower operational.",
            gameObject
        );

        OnConstructionCompleted?.Invoke();
    }

    IEnumerator BuildStep(
        MiningTowerBuildStep step
    )
    {
        if (
            step.objects == null ||
            step.objects.Length == 0
        )
        {
            yield break;
        }

        List<Renderer> stepRenderers =
            new List<Renderer>();

        List<Collider> stepColliders =
            new List<Collider>();

        Dictionary<Renderer, Material[]>
            originalMaterials =
                new Dictionary<Renderer, Material[]>();

        Dictionary<Renderer, Material[]>
            ghostMaterials =
                new Dictionary<Renderer, Material[]>();

        foreach (GameObject obj in step.objects)
        {
            if (obj == null)
                continue;

            obj.SetActive(true);

            Renderer[] renderers =
                obj.GetComponentsInChildren<Renderer>(
                    true
                );

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (stepRenderers.Contains(renderer))
                    continue;

                stepRenderers.Add(renderer);

                originalMaterials[renderer] =
                    renderer.sharedMaterials;

                int materialCount =
                    Mathf.Max(
                        1,
                        renderer.sharedMaterials.Length
                    );

                Material[] materials =
                    new Material[materialCount];

                for (
                    int i = 0;
                    i < materialCount;
                    i++
                )
                {
                    Material ghost =
                        new Material(
                            constructionMaterial
                        );

                    SetMaterialAlpha(
                        ghost,
                        startingAlpha
                    );

                    materials[i] =
                        ghost;
                }

                ghostMaterials[renderer] =
                    materials;

                renderer.sharedMaterials =
                    materials;

                renderer.enabled = true;
            }

            Collider[] colliders =
                obj.GetComponentsInChildren<Collider>(
                    true
                );

            foreach (Collider col in colliders)
            {
                if (col == null)
                    continue;

                if (stepColliders.Contains(col))
                    continue;

                stepColliders.Add(col);

                col.enabled = false;
            }
        }

        float elapsed = 0f;

        float duration =
            Mathf.Max(
                0.05f,
                step.duration
            );

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float alpha =
                Mathf.Lerp(
                    startingAlpha,
                    1f,
                    progress
                );

            foreach (
                KeyValuePair<Renderer, Material[]>
                pair in ghostMaterials
            )
            {
                foreach (Material material in pair.Value)
                {
                    if (material != null)
                    {
                        SetMaterialAlpha(
                            material,
                            alpha
                        );
                    }
                }
            }

            yield return null;
        }

        foreach (Renderer renderer in stepRenderers)
        {
            if (renderer == null)
                continue;

            renderer.sharedMaterials =
                originalMaterials[renderer];

            renderer.enabled = true;

            Material[] temporaryMaterials =
                ghostMaterials[renderer];

            foreach (Material material
                     in temporaryMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }

        foreach (Collider col in stepColliders)
        {
            if (col != null)
            {
                ColliderInfo originalInfo =
                    colliderInfos.Find(
                        x => x.collider == col
                    );

                col.enabled =
                    originalInfo != null
                    ? originalInfo.originalEnabled
                    : true;
            }
        }
    }

    void SetGameplayEnabled(
        bool enabledState
    )
    {
        if (gameplayBehaviours == null)
            return;

        foreach (
            Behaviour behaviour
            in gameplayBehaviours
        )
        {
            if (behaviour != null)
            {
                behaviour.enabled =
                    enabledState;
            }
        }
    }

    void SetMaterialAlpha(
        Material material,
        float alpha
    )
    {
        if (material == null)
            return;

        if (
            material.HasProperty(
                "_BaseColor"
            )
        )
        {
            Color color =
                material.GetColor(
                    "_BaseColor"
                );

            color.a = alpha;

            material.SetColor(
                "_BaseColor",
                color
            );
        }

        if (
            material.HasProperty(
                "_Color"
            )
        )
        {
            Color color =
                material.GetColor(
                    "_Color"
                );

            color.a = alpha;

            material.SetColor(
                "_Color",
                color
            );
        }
    }
}