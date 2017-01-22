using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ideafixxxer.CsvParser;
using UnityEditor;
using UnityEngine;

namespace Datablocks
{

    /// <summary>
    ///     Editor window for importing a CSV file
    /// </summary>
    public class ImportCSVSpreadsheet : DatablockImporter
    {
        private string csvFilePath;
        private string fieldDelimiter = ",";
        private bool fieldsFoldout;
        private List<string> headers;
        private int importDatablockTypeIndex;
        private int nameIndex;
        private bool showImportOptions;

        [MenuItem("Tools/Datablocks/Import/CSV File")]
        public static void ShowImportWindow()
        {
            GetWindow<ImportCSVSpreadsheet>("Import CSV Spreadsheet");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Import data in CSV format.", MessageType.None);

            GUI.backgroundColor = new Color(0, 0.8f, .8f);

            if (GUILayout.Button("Choose CSV file"))
            {
                csvFilePath = EditorUtility.OpenFilePanel("Load CSV file", "", "csv");

                if (!string.IsNullOrEmpty(csvFilePath))
                {
                    ParseCSVFile();

                    if (detectedTypes != null)
                    {
                        importDatablockTypeIndex = detectedTypes.IndexOf(detectedTypes.OrderByDescending(d => d.fields.Count).First());
                    }
                }
            }

            GUI.backgroundColor = Color.white;

            if (detectedTypes != null)
            {
                importDatablockTypeIndex = EditorGUILayout.Popup("Choose datablock type", importDatablockTypeIndex,
                    detectedTypes.Select(d => d.datablockType.ToString()).ToArray());

                DatablockDetectionInfo datablockDetectionInfo = detectedTypes[importDatablockTypeIndex];

                fieldsFoldout = EditorGUILayout.Foldout(fieldsFoldout,
                    datablockDetectionInfo.fields.Count + " detected fields");
                if (fieldsFoldout)
                {
                    foreach (FieldInfo field in datablockDetectionInfo.fields)
                    {
                        EditorGUILayout.LabelField(field.Name);
                    }
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("New datablock path: \"" + newDatablockDir + "\"");

                if (GUILayout.Button("Change"))
                {
                    string path = EditorUtility.OpenFolderPanel("Select path to save new datablock", "Assets", "");
                    if (String.IsNullOrEmpty(path))
                        return;

                    int assetsIndex = path.IndexOf("/Assets", StringComparison.Ordinal);
                    if (assetsIndex == -1)
                    {
                        Debug.LogError("Path must be in the Assets folder");
                        return;
                    }

                    newDatablockDir = path.Substring(assetsIndex + 1);
                    EditorPrefs.SetString("newDatablockDir", newDatablockDir);
                }
                EditorGUILayout.EndHorizontal();


                GUI.backgroundColor = new Color(0, 0.8f, 0);
                if (GUILayout.Button("Import!"))
                {
                    ImportRows(datablockDetectionInfo);
                    DatablockManager.Instance.RefreshAssets();
                }

                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.Separator();

            showImportOptions = EditorGUILayout.Foldout(showImportOptions, "Import options");

            if (showImportOptions)
            {
                EditorGUI.indentLevel = 1;
                fieldDelimiter = EditorGUILayout.TextField("Field delimiter", fieldDelimiter);
            }
        }

        private void ImportRows(DatablockDetectionInfo datablockDetectionInfo)
        {
            using (TextReader reader = File.OpenText(csvFilePath))
            {
                var parser = new CsvParser2();
                parser.TrimTrailingEmptyLines = true;
                string[][] parsed = parser.Parse(reader);

                for (int i = 1; i < parsed.GetLength(0); i++)
                {
                    string[] row = parsed[i];

                    string datablockName = row[nameIndex];
                    Datablock datablock = GetNamedDatablock(datablockDetectionInfo, datablockName);

                    for (int index = 0; index < parsed[i].Length; index++)
                    {
                        if (index == headers.Count)
                            break;

                        string fieldValue = parsed[i][index];
                        string fieldName = headers[index];

                        ProcessRawField(datablockDetectionInfo, fieldName, datablock, fieldValue);
                    }
                }

                Debug.Log("Imported " + (parsed.GetLength(0) - 1) + " datablocks.");
            }
        }

        private void ParseCSVFile()
        {
            var file = new StreamReader(csvFilePath);

            using (file)
            {
                string headerLine = file.ReadLine();
                if (headerLine == null)
                {
                    detectedTypes = new List<DatablockDetectionInfo>();
                    Debug.LogError("Empty file");
                    return;
                }

                headers = headerLine.Split(new[] {fieldDelimiter}, StringSplitOptions.None).ToList();

                detectedTypes = AutoDetectDatablockType(headers);
                if (detectedTypes == null)
                {
                    Debug.Log("Unable to auto detect datablock type");
                    return;
                }

                nameIndex = headers.FindIndex(h => h.Equals("name", StringComparison.InvariantCultureIgnoreCase));

                if (nameIndex == -1)
                {
                    detectedTypes = new List<DatablockDetectionInfo>();
                    Debug.LogError("Missing name column in header");
                }
            }
        }
    }
}