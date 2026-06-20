#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class AutoSave
{
    private static double lastSaveTime;
    private const double saveInterval = 300; // 5 minutes in seconds

    static AutoSave()
    {
        lastSaveTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (EditorApplication.timeSinceStartup - lastSaveTime >= saveInterval)
        {
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                Debug.Log($"[AutoSave] Saved at {System.DateTime.Now:HH:mm:ss}");
            }
            lastSaveTime = EditorApplication.timeSinceStartup;
        }
    }
}
#endif