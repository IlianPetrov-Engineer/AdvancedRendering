using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChannelGuide))]
public class CustomEditors : Editor
{
    public override void OnInspectorGUI()
    {
        ChannelGuide script = (ChannelGuide)target;
        script.CleanUp();

        DrawDefaultInspector();

        if (GUILayout.Button("Place notes"))
        {
            GameObject createdNote = script.Test();
            Selection.activeGameObject = createdNote;
        }
    }
}
