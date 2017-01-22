using UnityEditor;
using UnityEngine;

namespace Datablocks
{

    /// <summary>
    ///     Custom inspector for the DatablockManager
    /// </summary>
    [CustomEditor(typeof (DatablockManager))]
    public class DatablockManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DatablockManager datablockManager = DatablockManager.Instance;

            EditorGUIUtility.labelWidth = 160;
            datablockManager.searchEntireProject = !EditorGUILayout.Toggle("Use custom search paths", !datablockManager.searchEntireProject);

            if (!datablockManager.searchEntireProject)
            {
                SerializedProperty searchPaths = serializedObject.FindProperty("customSearchPaths");
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(searchPaths, true);
            }

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("View Datablocks", GUILayout.Width(200)))
            {
                DatablockList.ViewDatablocks();
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Tracking " + datablockManager.Count() + " datablocks", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}