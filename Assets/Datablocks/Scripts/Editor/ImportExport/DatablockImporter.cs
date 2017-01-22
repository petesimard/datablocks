using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Datablocks
{

    /// <summary>
    /// Base datablock importer
    /// </summary>
    public abstract class DatablockImporter : EditorWindow
    {
        protected string newDatablockDir;
        protected List<DatablockDetectionInfo> detectedTypes;

        protected virtual void OnEnable()
        {
            newDatablockDir = EditorPrefs.GetString("newDatablockDir", "/Assets");
            DatablockManager.Instance.RefreshAssets();
        }

        private void OnProjectChange()
        {
            DatablockManager.Instance.RefreshAssets();
        }

        /// <summary>
        /// Container to store the field analysis of a datablock type on imported data
        /// </summary>
        protected class DatablockDetectionInfo
        {
            public readonly List<FieldInfo> fields = new List<FieldInfo>();
            public Type datablockType;
        }

        /// <summary>
        ///     Attempt to determine the type of datablock based on the spreadsheets headers
        /// </summary>
        /// <param name="headers">1st row headers</param>
        /// <returns></returns>
        protected List<DatablockDetectionInfo> AutoDetectDatablockType(List<string> headers)
        {
            List<Type> types = DatablockList.GetInstances(typeof (Datablock));
            if (types.Count == 0)
            {
                Debug.LogError("No datablocks defined!");
                return null;
            }

            var typeMatches = new List<DatablockDetectionInfo>();

            foreach (Type datablockType in types)
            {
                var detectionInfo = new DatablockDetectionInfo
                                    {
                                        datablockType = datablockType
                                    };

                typeMatches.Add(detectionInfo);

                foreach (string header in headers)
                {
                    FieldInfo field = datablockType.GetField(header, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (field != null)
                        detectionInfo.fields.Add(field);
                }
            }

            // Not a single field matched
            if (typeMatches.Sum(t => t.fields.Count) == 0)
                return null;

            return typeMatches;
        }

        protected void SetField(Datablock datablock, FieldInfo field, string rawValue)
        {
            object newVal = null;

            if (field.FieldType == typeof (int))
            {
                int intVal;
                int.TryParse(rawValue, out intVal);
                newVal = intVal;
            }
            else if (field.FieldType == typeof (float))
            {
                float floatVal;
                float.TryParse(rawValue, out floatVal);
                newVal = floatVal;
            }
            else if (field.FieldType == typeof (bool))
            {
                if (rawValue.Equals("True", StringComparison.InvariantCultureIgnoreCase) ||
                    rawValue.Equals("T", StringComparison.InvariantCultureIgnoreCase) ||
                    rawValue.Equals("Yes", StringComparison.InvariantCultureIgnoreCase))
                {
                    newVal = true;
                }
                else
                {
                    newVal = false;
                }
            }
            else if (field.FieldType == typeof (double))
            {
                double doubleVal;
                double.TryParse(rawValue, out doubleVal);
                newVal = doubleVal;
            }
            else if (field.FieldType == typeof (string))
            {
                if (rawValue.Equals("(null)", StringComparison.OrdinalIgnoreCase))
                {
                    newVal = null;
                }
                else
                {
                    newVal = rawValue;
                }
            }
            else if (field.FieldType == typeof (Color))
            {
                Color colorVal = Color.white;

                try
                {
                    string[] splitColor = rawValue.Split(',');
                    if (splitColor.Length == 3)
                        colorVal = new Color(float.Parse(splitColor[0].Trim()), float.Parse(splitColor[1].Trim()), float.Parse(splitColor[2].Trim()));
                    else if (splitColor.Length == 4)
                        colorVal = new Color(float.Parse(splitColor[0].Trim()), float.Parse(splitColor[1].Trim()), float.Parse(splitColor[2].Trim()), float.Parse(splitColor[3].Trim()));
                }
                catch
                {
                }

                newVal = colorVal;
            }
            else if (field.FieldType == typeof (Vector2))
            {
                Vector2 vector = Vector2.zero;

                try
                {
                    string[] splitVec = rawValue.Split(',');
                    vector = new Vector2(float.Parse(splitVec[0].Trim()), float.Parse(splitVec[1].Trim()));
                }
                catch
                {
                }

                newVal = vector;
            }
            else if (field.FieldType == typeof (Vector3))
            {
                Vector3 vector = Vector3.zero;

                try
                {
                    string[] splitVec = rawValue.Split(',');
                    vector = new Vector3(float.Parse(splitVec[0].Trim()), float.Parse(splitVec[1].Trim()), float.Parse(splitVec[2].Trim()));
                }
                catch
                {
                }

                newVal = vector;
            }
            else if (field.FieldType == typeof (Vector4))
            {
                Vector4 vector = Vector4.zero;

                try
                {
                    string[] splitVec = rawValue.Split(',');
                    vector = new Vector4(float.Parse(splitVec[0].Trim()), float.Parse(splitVec[1].Trim()), float.Parse(splitVec[2].Trim()), float.Parse(splitVec[3].Trim()));
                }
                catch
                {
                }

                newVal = vector;
            }
            else if (field.FieldType.IsSubclassOf(typeof (Object)))
            {
                string[] matchingAssets = AssetDatabase.FindAssets(rawValue + " t:" + field.FieldType.Name);

                if (!matchingAssets.Any())
                {
                    Debug.LogWarning("Unable to find object: " + rawValue + " for " + field.Name + " on datablock " + datablock.name);
                    return;
                }

                Object obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(matchingAssets[0]), field.FieldType);
                newVal = obj;
            }
            else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof (DatablockRef<>))
            {
                Object obj = Resources.Load(rawValue);
                if (!obj)
                {
                    Debug.LogWarning("Unable to load resource: " + rawValue + " for " + field.Name + " on datablock " + datablock.name);
                    return;
                }

                newVal = Activator.CreateInstance(field.FieldType, new object[] {obj});
            }
            else if (field.FieldType.IsEnum)
            {
                try
                {
                    newVal = Enum.Parse(field.FieldType, rawValue);
                }
                catch (Exception)
                {
                    Debug.LogError("Invalid enum vlue " + rawValue + " for " + field.Name + " on datablock " + datablock.name);
                }
            }

            field.SetValue(datablock, newVal);
            datablock.SetOverridesParent(field, true);
        }

        protected Datablock GetNamedDatablock(DatablockDetectionInfo datablockDetectionInfo, string datablockName)
        {
            Datablock datablock = DatablockManager.Instance.GetDatablock(datablockName, datablockDetectionInfo.datablockType, true);

            if (datablock == null)
            {
                datablock = CreateInstance(datablockDetectionInfo.datablockType) as Datablock;
                string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(newDatablockDir + "/" + datablockName + ".asset");
                AssetDatabase.CreateAsset(datablock, assetPathAndName);
            }
            return datablock;
        }

        protected void ProcessRawField(DatablockDetectionInfo datablockDetectionInfo, string fieldName, Datablock datablock,
            string fieldValue)
        {
            if (fieldName.Equals("parent", StringComparison.OrdinalIgnoreCase))
            {
                datablock.Parent = DatablockManager.Instance.GetDatablock(fieldValue, datablockDetectionInfo.datablockType);
                return;
            }

            FieldInfo field =
                datablockDetectionInfo.fields.FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            if (field == null)
                return;

            if (string.IsNullOrEmpty(fieldValue))
            {
                datablock.SetOverridesParent(field, false);
                return;
            }

            SetField(datablock, field, fieldValue);
        }
    }
}