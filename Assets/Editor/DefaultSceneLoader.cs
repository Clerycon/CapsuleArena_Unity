using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DefaultSceneLoader
{
    static DefaultSceneLoader()
    {
        string [] guids = AssetDatabase.FindAssets($"t:Scene {Consts.SceneNames.BOOTSTRAP_SCENE}");

        if(guids.Length > 0)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            SceneAsset bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            EditorSceneManager.playModeStartScene = bootstrapScene;
        }
        else
        {
            Debug.LogError($"[DefaultSceneLoader] '{Consts.SceneNames.BOOTSTRAP_SCENE}' Is Not Found!");
        }
    }
}
