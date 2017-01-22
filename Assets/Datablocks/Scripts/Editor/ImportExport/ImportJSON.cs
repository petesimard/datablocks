using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ideafixxxer.CsvParser;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Datablocks
{

    /// <summary>
    ///     Editor window for importing JSON file
    /// </summary>
    public class ImportJSON : DatablockImporter
    {
        private string jsonFilePath;
        private bool fieldsFoldout;
        private int importDatablockTypeIndex;
        private bool showImportOptions;

        [MenuItem("Tools/Datablocks/Import/JSON File")]
        public static void ShowImportWindow()
        {
            GetWindow<ImportJSON>("Import JSON File");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Import datablocks from JSON file.", MessageType.None);

            GUI.backgroundColor = new Color(0, 0.8f, .8f);

            if (GUILayout.Button("Choose JSON file"))
            {
                jsonFilePath = EditorUtility.OpenFilePanel("Load JSON file", "", "*");

                if (!string.IsNullOrEmpty(jsonFilePath))
                {
                    ParseJSONFile();

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

        }

        private void ImportRows(DatablockDetectionInfo datablockDetectionInfo)
        {
            var myFile = new StreamReader(jsonFilePath);
            var jsonStr = myFile.ReadToEnd();
            myFile.Close();

            var ct = 0;

            var datablockValues = new Dictionary<string, string>();

            var reader = new JsonTextReader(new StringReader(jsonStr));
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    // New datablock
                    datablockValues = new Dictionary<string, string>();
                }
                else if (reader.TokenType == JsonToken.PropertyName)
                {
                    var fieldName = reader.Value as string;
                    reader.Read();

                    datablockValues[fieldName] = reader.Value as string;
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    if (!datablockValues.ContainsKey("Name"))
                    {
                        Debug.LogWarning("Datalock missing Name field");
                        continue;
                    }
                    var datablockName = datablockValues["Name"];

                    Datablock datablock = GetNamedDatablock(datablockDetectionInfo, datablockName);

                    foreach (var datablockValue in datablockValues)
                    {
                        var fieldName = datablockValue.Key;
                        var fieldValue = datablockValue.Value;

                        if (fieldName == "Name")
                            continue;

                        ProcessRawField(datablockDetectionInfo, fieldName, datablock, fieldValue);
                    }

                    ct++;
                }
            }

            Debug.Log("Imported " + ct + " datablocks.");

        }

        private void ParseJSONFile()
        {
            var myFile = new StreamReader(jsonFilePath);
            var jsonStr = myFile.ReadToEnd();
            myFile.Close();

            var reader = new JsonTextReader(new StringReader(jsonStr));

            // Read object start
            if (!reader.Read())
            {
                detectedTypes = new List<DatablockDetectionInfo>();
                Debug.LogError("Empty file");
                return;
            }


            var headers = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    detectedTypes = AutoDetectDatablockType(headers);
                    if (detectedTypes == null)
                    {
                        Debug.Log("Unable to auto detect datablock type");
                        return;
                    }

                    return;
                }

                if (reader.TokenType == JsonToken.PropertyName)
                    headers.Add(reader.Value as string);
            }
        }
    }
}