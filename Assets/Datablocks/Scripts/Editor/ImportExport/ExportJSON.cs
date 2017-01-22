using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Datablocks
{

    /// <summary>
    ///     Editor window for exporting the datablocks in JSON format
    /// </summary>
    public class ExportJSON : DatablockExporter
    {
        private int datablockTypeIndex;
        private FieldInfo[] datablockFields;
        private bool showExportOptions;
        private JsonTextWriter writer;

        [MenuItem("Tools/Datablocks/Export/JSON File")]
        public static void ShowExportWindow()
        {
            GetWindow<ExportJSON>("Export JSON");
        }

        private void OnGUI()
        {
            DatablockManager datablockManager = DatablockManager.Instance;

            EditorGUILayout.HelpBox("Export your data in JSON format.", MessageType.None);
            List<Type> types = DatablockList.GetInstances(typeof (Datablock));

            if (types.Count == 0)
            {
                EditorGUILayout.LabelField("No datablock types found");
                return;
            }

            datablockTypeIndex = EditorGUILayout.Popup("Choose datablock type", datablockTypeIndex, types.Select(d => d.ToString()).ToArray());
            Type datablockType = types[datablockTypeIndex];

            string exportLabel = "Export " + datablockManager.GetDatablocks(datablockType).Count() + " datablocks";
            GUI.backgroundColor = new Color(0, 0.8f, 0);

            if (GUILayout.Button(exportLabel))
            {
                string saveLocation = EditorUtility.SaveFilePanel("Export datablocks", "", datablockType.Name.ToString() + "s" + ".json", "json");
                if (!string.IsNullOrEmpty(saveLocation))
                {
                    ExportDatablocks(datablockType, saveLocation);
                }
            }

            GUI.backgroundColor = new Color(1, 1, 1);

            EditorGUILayout.Separator();

            showExportOptions = EditorGUILayout.Foldout(showExportOptions, "Export options");

            if (showExportOptions)
            {
                EditorGUI.indentLevel = 1;

                exportFullValues = EditorGUILayout.Toggle(new GUIContent("Include full values", "Exports the full value of a field even if it inherits its value."), exportFullValues);
            }
        }

        private void ExportDatablocks(Type datablockType, string saveLocation)
        {
            IEnumerable<Datablock> baseDatablocks = DatablockManager.Instance.GetDatablocks(datablockType).Where(d => d.Parent == null);

            datablockFields = Datablock.GetFields(datablockType);

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            writer = new JsonTextWriter(sw);

            writer.WriteStartArray();

            // Process the datablocks from the base to their children
            foreach (var baseDatablock in baseDatablocks)
            {
                ProcessChildren(baseDatablock);
            }

            writer.WriteEndArray();

            File.WriteAllText(saveLocation, sb.ToString());

            Debug.Log("Datablocks exported to " + saveLocation);

            writer.Close();
        }

        protected override void ProcessDatablock(Datablock datablock)
        {
            writer.WriteStartObject();

            AddColumn("Name", datablock.name);
            AddColumn("Parent", datablock.Parent ? datablock.Parent.name : "");

            foreach (FieldInfo field in datablockFields)
            {
                var value = GetFieldValue(datablock, field);

                AddColumn(field.Name, value);
            }

            writer.WriteEndObject();
        }

        private void AddColumn(string field, string value)
        {
            writer.WritePropertyName(field);
            writer.WriteValue(value);
        }
    }
}