using UnityEngine;

public class TextureAnimationSignifier : MonoBehaviour
{
    public enum SignifierMode
    {
        WhileProcessing,
        WhileCargoLoaded,
        WhileObjectActive
    }

    public enum PatternType
    {
        Stripes,
        Checker,
        Diagonal
    }

    [Header("Mode")]
    [SerializeField]
    SignifierMode mode;

    [Header("References")]
    [SerializeField]
    Renderer targetRenderer;

    [SerializeField]
    OrbitalAnchorProcessor processor;

    [SerializeField]
    MiningCargo cargo;

    [Header("Animation")]
    [SerializeField]
    Vector2 scrollSpeed =
        new Vector2(0f, 0.5f);

    [SerializeField]
    Vector2 textureTiling =
        new Vector2(4f, 4f);

    [Header("Procedural Texture")]
    [SerializeField]
    PatternType patternType =
        PatternType.Stripes;

    [SerializeField]
    int textureSize = 64;

    [SerializeField]
    int patternScale = 8;

    [SerializeField]
    Color patternColorA =
        Color.white;

    [SerializeField]
    Color patternColorB =
        new Color(
            0.35f,
            0.35f,
            0.35f,
            1f
        );

    Material runtimeMaterial;
    Texture2D generatedTexture;

    Vector2 baseOffset;

    static readonly int BaseMap =
        Shader.PropertyToID("_BaseMap");

    static readonly int MainTex =
        Shader.PropertyToID("_MainTex");

    void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer =
                GetComponent<Renderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogWarning(
                "[TextureAnimationSignifier] No Renderer found.",
                gameObject
            );

            return;
        }

        // Create a private material instance.
        runtimeMaterial =
            targetRenderer.material;

        generatedTexture =
            CreateProceduralTexture();

        ApplyTexture(
            generatedTexture
        );

        SetTextureScale(
            textureTiling
        );

        baseOffset =
            GetTextureOffset();
    }

    void Update()
    {
        if (runtimeMaterial == null)
            return;

        if (!ShouldAnimate())
        {
            SetTextureOffset(
                baseOffset
            );

            return;
        }

        Vector2 offset =
            baseOffset +
            scrollSpeed *
            Time.time;

        SetTextureOffset(
            offset
        );
    }

    bool ShouldAnimate()
    {
        switch (mode)
        {
            case SignifierMode.WhileProcessing:
                return
                    processor != null &&
                    processor.IsProcessing;

            case SignifierMode.WhileCargoLoaded:
                return
                    cargo != null &&
                    cargo.IsLoaded;

            case SignifierMode.WhileObjectActive:
                return
                    gameObject.activeInHierarchy;
        }

        return false;
    }

    Texture2D CreateProceduralTexture()
    {
        int size =
            Mathf.Max(
                8,
                textureSize
            );

        int scale =
            Mathf.Max(
                1,
                patternScale
            );

        Texture2D texture =
            new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );

        texture.wrapMode =
            TextureWrapMode.Repeat;

        texture.filterMode =
            FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool useA =
                    GetPatternValue(
                        x,
                        y,
                        scale
                    );

                texture.SetPixel(
                    x,
                    y,
                    useA
                        ? patternColorA
                        : patternColorB
                );
            }
        }

        texture.Apply();

        return texture;
    }

    bool GetPatternValue(
        int x,
        int y,
        int scale
    )
    {
        switch (patternType)
        {
            case PatternType.Stripes:
                return
                    (x / scale) % 2 == 0;

            case PatternType.Checker:
                return
                    (
                        (x / scale) +
                        (y / scale)
                    ) % 2 == 0;

            case PatternType.Diagonal:
                return
                    (
                        (x + y) / scale
                    ) % 2 == 0;
        }

        return true;
    }

    void ApplyTexture(
        Texture texture
    )
    {
        if (
            runtimeMaterial.HasProperty(
                BaseMap
            )
        )
        {
            runtimeMaterial.SetTexture(
                BaseMap,
                texture
            );
        }

        if (
            runtimeMaterial.HasProperty(
                MainTex
            )
        )
        {
            runtimeMaterial.SetTexture(
                MainTex,
                texture
            );
        }
    }

    void SetTextureScale(
        Vector2 scale
    )
    {
        if (
            runtimeMaterial.HasProperty(
                BaseMap
            )
        )
        {
            runtimeMaterial.SetTextureScale(
                BaseMap,
                scale
            );
        }

        if (
            runtimeMaterial.HasProperty(
                MainTex
            )
        )
        {
            runtimeMaterial.SetTextureScale(
                MainTex,
                scale
            );
        }
    }

    Vector2 GetTextureOffset()
    {
        if (
            runtimeMaterial.HasProperty(
                BaseMap
            )
        )
        {
            return
                runtimeMaterial.GetTextureOffset(
                    BaseMap
                );
        }

        if (
            runtimeMaterial.HasProperty(
                MainTex
            )
        )
        {
            return
                runtimeMaterial.GetTextureOffset(
                    MainTex
                );
        }

        return Vector2.zero;
    }

    void SetTextureOffset(
        Vector2 offset
    )
    {
        if (
            runtimeMaterial.HasProperty(
                BaseMap
            )
        )
        {
            runtimeMaterial.SetTextureOffset(
                BaseMap,
                offset
            );
        }

        if (
            runtimeMaterial.HasProperty(
                MainTex
            )
        )
        {
            runtimeMaterial.SetTextureOffset(
                MainTex,
                offset
            );
        }
    }

    void OnDestroy()
    {
        if (generatedTexture != null)
        {
            Destroy(
                generatedTexture
            );
        }

        if (runtimeMaterial != null)
        {
            Destroy(
                runtimeMaterial
            );
        }
    }
}