using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Brian
{
    public class Outline : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] Material material;
        [SerializeField, ColorUsage(false, true)] Color color = new Color(0.6f, 0.45f, 0.9f);
        [SerializeField] float thickness = 0.04f;
        [SerializeField] int skinnedUpdateInterval = 3;

        struct SkinnedShell
        {
            public SkinnedMeshRenderer source;
            public Mesh baked;
            public Transform shell;
        }

        readonly List<SkinnedShell> skinned = new List<SkinnedShell>();
        MaterialPropertyBlock block;
        int frame;

        void Start()
        {
            if (material == null)
            {
                enabled = false;
                return;
            }

            block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, Color.black);
            block.SetColor(EmissionId, color);

            foreach (var filter in GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>() == null) continue;
                MakeShell(filter.transform, filter.sharedMesh);
            }

            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null) continue;
                var baked = new Mesh();
                smr.BakeMesh(baked, false);
                var shell = MakeShell(transform, baked);
                var s = new SkinnedShell { source = smr, baked = baked, shell = shell };
                skinned.Add(s);
                FollowSkinned(s);
            }

            if (skinned.Count == 0) enabled = false;
        }

        void LateUpdate()
        {
            bool bake = ++frame % Mathf.Max(1, skinnedUpdateInterval) == 0;
            foreach (var s in skinned)
            {
                if (s.source == null || s.shell == null) continue;
                if (bake) s.source.BakeMesh(s.baked, false);
                FollowSkinned(s);
            }
        }

        void FollowSkinned(SkinnedShell s)
        {
            Transform src = s.source.transform;
            Vector3 offset = src.rotation * (-s.baked.bounds.center * thickness);
            s.shell.SetPositionAndRotation(src.position + offset, src.rotation);
            float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
            s.shell.localScale = Vector3.one * (1f + thickness) / parentScale;
        }

        void OnDestroy()
        {
            foreach (var s in skinned)
                if (s.baked != null) Destroy(s.baked);
        }

        Transform MakeShell(Transform parent, Mesh mesh)
        {
            var go = new GameObject("Outline");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            var mats = new Material[Mathf.Max(1, mesh.subMeshCount)];
            for (int i = 0; i < mats.Length; i++) mats[i] = material;
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.SetPropertyBlock(block);
            Place(go.transform, mesh);
            return go.transform;
        }

        void Place(Transform shell, Mesh mesh)
        {
            shell.localRotation = Quaternion.identity;
            shell.localScale = Vector3.one * (1f + thickness);
            shell.localPosition = -mesh.bounds.center * thickness;
        }
    }
}
