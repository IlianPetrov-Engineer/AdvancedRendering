using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChannelGuide))]
public class CustomEditors : Editor
{
    public override void OnInspectorGUI()
    {
        ChannelGuide script = (ChannelGuide)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Place notes"))
        {
            script.CleanUp();
            GameObject createdNote = script.Node();
            Selection.activeGameObject = createdNote;
        }
    }
}
