using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GroupProject.Elevator;
public static class PreparationNavigation
{
    [MenuItem("Tools/Training Rooms/Select Changing and Shower Rooms")]
    static void Preparation(){Show("02_Preparation_Interactive");}
    [MenuItem("Tools/Training Rooms/Select Cleaning Station")]
    static void Cleaning(){Show("03_Cleaning_Adham");}
    static void Show(string name)
    {
        if(EditorApplication.isPlaying){Debug.LogWarning("Exit Play Mode before editing the rooms.");return;}
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var target=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==name);
        if(!target){Debug.LogWarning("Open Assets/Project/Scenes/01_Opening.unity first.");return;}
        var root=target;while(root.parent)root=root.parent;root.gameObject.SetActive(true);
        Selection.activeGameObject=target.gameObject;
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.FrameSelected();
    }
}
