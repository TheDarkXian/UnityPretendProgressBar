using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(SimulateTaskTable))]
public class SimulateTaskTableEditor: Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
            var table= (SimulateTaskTable)target;
        if (GUILayout.Button("HandleTextArea"))
        {
            table.HandleTextArea();
        }
        if (GUILayout.Button("BuildTextAreaFromTasks"))
        {
            table.BuildTextAreaFromTasks();

        }

    }


}