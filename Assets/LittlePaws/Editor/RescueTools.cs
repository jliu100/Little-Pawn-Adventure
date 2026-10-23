using UnityEditor;
using UnityEditor.SceneManagement;
using LittlePaws.Rescue;

public static class RescueTools
{
    [MenuItem("Little Paws/Open Rescue the egg")]
    public static void Open()
    {
        if(!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Scenes/RescueEgg.unity");
    }

    [MenuItem("Little Paws/Run Rescue the egg checks")]
    public static void RunChecks()
    {
        RescueCombatChecks.Run();
    }
}
