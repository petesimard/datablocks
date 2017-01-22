using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Datablocks
{

    /// <summary>
    ///     Editor window for exporting the datablocks in CSV format
    /// </summary>
    public class ExportCSVSpreadsheet : DatablockExporter
    {
        private int datablockTypeIndex;
        private bool exportData = true;
        private StringBuilder outputStringBuilder;
        private FieldInfo[] datablockFields;
        private bool showExportOptions;
        private string fieldDelimiter = ",";

        private static char[] _charactersThatMustBeQuoted = {',', '"', '\n'};

        [MenuItem("Tools/Datablocks/Export/CSV File")]
        public static void ShowExportWindow()
        {
            GetWindow<ExportCSVSpreadsheet>("Export CSV Spreadsheet");
        }

        private void OnGUI()
        {
            DatablockManager datablockManager = DatablockManager.Instance;

            EditorGUILayout.HelpBox("Export your data in CSV format. Can be imported into a Google sheets, Excel, and other spreadsheet porgrams.", MessageType.None);
            List<Type> types = DatablockList.GetInstances(typeof (Datablock));

            if (types.Count == 0)
            {
                EditorGUILayout.LabelField("No datablock types found");
                return;
            }

            datablockTypeIndex = EditorGUILayout.Popup("Choose datablock type", datablockTypeIndex, types.Select(d => d.ToString()).ToArray());
            Type datablockType = types[datablockTypeIndex];

            string exportLabel = exportData ? "Export " + datablockManager.GetDatablocks(datablockType).Count() + " datablocks" : "Export schema";
            GUI.backgroundColor = new Color(0, 0.8f, 0);

            if (GUILayout.Button(exportLabel))
            {
                string saveLocation = EditorUtility.SaveFilePanel("Export datablocks", "", datablockType.Name.ToString() + "s" + ".csv", "csv");
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

                fieldDelimiter = EditorGUILayout.TextField("Field delimiter", fieldDelimiter);
                exportData = EditorGUILayout.Toggle("Export data", exportData);
                exportFullValues = EditorGUILayout.Toggle(new GUIContent("Include full values", "Exports the full value of a field even if it inherits its value."), exportFullValues);
            }
        }

        private void ExportDatablocks(Type datablockType, string saveLocation)
        {
            outputStringBuilder = new StringBuilder();
            var headers = new List<string>() {"Name", "Parent"};

            IEnumerable<Datablock> baseDatablocks = DatablockManager.Instance.GetDatablocks(datablockType).Where(d => d.Parent == null);

            datablockFields = Datablock.GetFields(datablockType);

            foreach (FieldInfo memberInfo in datablockFields)
            {
                headers.Add(memberInfo.Name);
            }


            // Add the headers
            outputStringBuilder.Append(String.Join(fieldDelimiter, headers.ToArray()) + "\r\n");

            // Process the datablocks from the base to their children
            foreach (var baseDatablock in baseDatablocks)
            {
                ProcessChildren(baseDatablock);
            }


            // Write the file
            File.WriteAllText(saveLocation, outputStringBuilder.ToString());

            Debug.Log("Datablocks exported to " + saveLocation);
        }

        protected override void ProcessDatablock(Datablock datablock)
        {
            AddColumn(datablock.name);

            AddColumn(datablock.Parent ? datablock.Parent.name : "");

            foreach (FieldInfo field in datablockFields)
            {
                var value = GetFieldValue(datablock, field).Replace("\"", "\"\"");

                AddColumn(value);
            }

            // strip triailing delimiter
            outputStringBuilder.Length--;

            outputStringBuilder.Append("\r\n");
        }

        private void AddColumn(string value)
        {
            if (value.IndexOfAny(_charactersThatMustBeQuoted) > -1)
                outputStringBuilder.Append("\"" + value + "\"" + fieldDelimiter);
            else
                outputStringBuilder.Append(value + fieldDelimiter);
        }
    }
}