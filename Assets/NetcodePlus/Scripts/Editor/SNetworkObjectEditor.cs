using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NetcodePlus.Editor
{

    /// <summary>
    /// Just the text on the SNetworkObject component in Unity inspector
    /// </summary>

    [CustomEditor(typeof(SNetworkObject)), CanEditMultipleObjects]
    public class SNetworkObjectEditor : UnityEditor.Editor
    {

        public override void OnInspectorGUI()
        {
            SNetworkObject myScript = target as SNetworkObject;

            DrawDefaultInspector();

            EditorGUILayout.Space();

            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("Generate Network IDs"))
            {
                Undo.RecordObject(myScript, "Generate Network IDs");
                myScript.GenerateEditorID();
                EditorUtility.SetDirty(myScript);
            }

            EditorGUILayout.Space();
        }

    }

}
