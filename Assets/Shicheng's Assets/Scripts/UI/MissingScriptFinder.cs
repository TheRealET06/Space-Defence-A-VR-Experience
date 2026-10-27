using UnityEditor;
using UnityEngine;

public static class MissingScriptFinder
{
    
    [MenuItem("Tools/Find Missing Scripts In Selection")]
    public static void FindMissingScriptsInSelection()
    {
        GameObject root = Selection.activeGameObject;

        if (root == null)
        {
            Debug.LogWarning("Select a GameObject or Prefab root first.");
            return;
        }

        Transform[] transforms =
            root.GetComponentsInChildren<Transform>(true);

        int totalMissing = 0;

        foreach (Transform t in transforms)
        {
            int missingCount =
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                    t.gameObject
                );

            if (missingCount > 0)
            {
                totalMissing += missingCount;

                Debug.LogError(
                    $"Missing Script x{missingCount}: {GetFullPath(t)}",
                    t.gameObject
                );
            }
        }

        Debug.Log(
            totalMissing == 0
                ? "No missing scripts found."
                : $"Found {totalMissing} missing script component(s)."
        );
    }

    [MenuItem("Tools/Remove Missing Scripts From Selection")]
    public static void RemoveMissingScriptsFromSelection()
    {
        GameObject root = Selection.activeGameObject;

        if (root == null)
        {
            Debug.LogWarning("Select a GameObject or Prefab root first.");
            return;
        }

        Transform[] transforms =
            root.GetComponentsInChildren<Transform>(true);

        int totalRemoved = 0;

        foreach (Transform t in transforms)
        {
            int missingCount =
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                    t.gameObject
                );

            if (missingCount <= 0)
                continue;

            Undo.RegisterCompleteObjectUndo(
                t.gameObject,
                "Remove Missing Scripts"
            );

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(
                t.gameObject
            );

            totalRemoved += missingCount;

            EditorUtility.SetDirty(t.gameObject);

            Debug.Log(
                $"Removed {missingCount} missing script(s): " +
                GetFullPath(t),
                t.gameObject
            );
        }

        Debug.Log(
            $"Removed {totalRemoved} missing script component(s)."
        );
    }

    static string GetFullPath(Transform t)
    {
        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }
    
}