using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChannelGuide))]
public class CustomEditors : Editor
{
    public override void OnInspectorGUI()
    {
        ChannelGuide script = (ChannelGuide)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Place nodes"))
        {
            //Removes any null entries before adding a new node
            script.CleanUp();

            //Creates a new node and immediately select it so the user can position it
            GameObject createdNode = script.Node();
            Selection.activeGameObject = createdNode;
        }
    }
}
