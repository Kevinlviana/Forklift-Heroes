using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// remove só o ambiente original (objeto Environment) da cena aberta
public static class RemoveOriginalEnvironment
{
    [MenuItem("Forklift Heroes/Remove Original Environment")]
    public static void Run()
    {
        var env = GameObject.Find("Environment");
        if (env == null)
        {
            EditorUtility.DisplayDialog("Forklift Heroes", "No 'Environment' object in the open scene (already removed, or open Phase1_Treino).", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Forklift Heroes",
            "Remove the original environment ('Environment': giant floor and walls) from the open scene?\n\nYou can undo with Ctrl+Z. Remember to save the scene.", "Remove", "Cancel"))
            return;

        Undo.DestroyObjectImmediate(env);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Ambiente original removido. Salve a cena.");
    }
}
