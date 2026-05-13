using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class GameEditor
{
    private const string sceneFolder = "Assets/CarDrift/Scenes/";
    [MenuItem("GameEditor/Scenes/LoadingScene")]
    static void OpenLoadingScene()
    {
        OpenScene(sceneFolder + "LoadingScene.unity");
    }
    [MenuItem("GameEditor/Scenes/InGameScene")]
    static void OpenInGameScene()
    {
        OpenScene(sceneFolder + "InGameScene.unity");
    }
    [MenuItem("GameEditor/Scenes/MenuScene")]
    static void OpenMenuScene()
    {
        OpenScene(sceneFolder + "MenuScene.unity");
    }

    static void OpenScene(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
        }
    }
}