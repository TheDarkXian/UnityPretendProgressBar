using UnityEditor;
using UnityEngine;
namespace PretendProgressBar{
[CustomEditor(typeof(PretendProgressBarSo))]
public class PretendProgressEditor: Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("StartPretend"))
        {
            
            PretendProgressBarHandle.HandleSo((PretendProgressBarSo)target);

        }
    }
    }
}