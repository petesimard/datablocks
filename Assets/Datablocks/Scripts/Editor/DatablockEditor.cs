using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Rotorz.ReorderableList;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Datablocks
{
    /// <summary>
    ///     Custom inspector for Datablocks
    /// </summary>
    [CustomEditor(typeof (Datablock), true)]
    public class DatablockEditor : Editor
    {
        private static bool showHierarchy;
        public static Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        protected Datablock datablock;

        private MemberInfo[] members;
        private string renameAssetTempName;


        public virtual void OnEnable()
        {
            datablock = (Datablock) target;

            renameAssetTempName = datablock.name;

            members = Datablock.GetFields(datablock.GetType());
            DatablockManager.EnsureInitilized();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("NewNameField");
            renameAssetTempName = EditorGUILayout.TextField("Name", renameAssetTempName);

            bool doChange = false;
            if (Event.current.isKey)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        if (GUI.GetNameOfFocusedControl() == "NewNameField")
                            doChange = true;
                        break;
                }
            }

            if (GUILayout.Button("Change", GUILayout.MaxWidth(55)) || doChange)
            {
                string path = AssetDatabase.GetAssetPath(datablock);
                AssetDatabase.RenameAsset(path, renameAssetTempName);
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();

            var newParent = EditorGUILayout.ObjectField("Parent Datablock", datablock.Parent, datablock.GetType(), false) as Datablock;
            if (newParent != datablock.Parent)
            {
                if (newParent != null && newParent.GetType() != datablock.GetType())
                {
                    Debug.LogError("Parent is not the same type.");
                }
                else if (newParent != null && !datablock.IsParentValid(newParent))
                {
                    Debug.LogError("Parent would create a circular loop.");
                }
                else
                {
                    datablock.Parent = newParent;
                }
            }
            EditorGUILayout.Space();
            DrawFields();


            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create new child"))
            {
                var newDatablock = CreateInstance(datablock.GetType()) as Datablock;
                newDatablock.Parent = datablock;
                string name = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(datablock));
                AssetDatabase.CreateAsset(newDatablock, name);
                Selection.activeObject = newDatablock;
                return;
            }
            if (GUILayout.Button("Create copy"))
            {
                Datablock newDatablock = CreateInstance(datablock.GetType()) as Datablock;
                newDatablock.Parent = datablock.Parent;
                string name = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(datablock));
                AssetDatabase.CreateAsset(newDatablock, name);
                Selection.activeObject = newDatablock;
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (datablock.Parent != null)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset to parent"))
                {
                    datablock.ClearParentOverrides();
                }

                EditorGUILayout.EndHorizontal();


                showHierarchy = EditorGUILayout.Foldout(showHierarchy, "Hierarchy");
                if (showHierarchy)
                {
                    EditorGUILayout.BeginVertical();

                    Datablock parent = datablock.Parent;
                    while (parent != null)
                    {
                        if (GUILayout.Button(parent.name, EditorStyles.miniButton))
                        {
                            Selection.activeObject = parent;
                        }
                        parent = parent.Parent;
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            if (!Application.isPlaying)
            {
                // Run some extra logic on changes
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(datablock);
                }
            }
        }

        /// <summary>
        ///     Draw a field
        /// </summary>
        /// <param name="field">Field to draw</param>
        /// <param name="indent">Indent the display of the field</param>
        protected virtual void DrawField(FieldInfo field, int indent = 0)
        {
            bool overrideParentValue = datablock.DoesOverridesParent(field) || datablock.Parent == null;

            Type fieldType = field.FieldType;
            object val = null;

            bool isDatablockReference = false;
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof (DatablockRef<>))
            {
                fieldType = fieldType.GetGenericArguments()[0];
                val = datablock.GetFieldValue<IDatablockRef>(field).GetObject();
                isDatablockReference = true;
            }
            else
            {
                val = datablock.GetFieldValue(field);
            }

            // Set the indent
            EditorGUI.indentLevel = indent;

            // Field overrides parent, or doesn't have a parent
            if (fieldType == typeof (int))
            {
                EditorGUILayout.BeginHorizontal();

                //var val = (int) field.GetValue(datablock);
                int newVal = EditorGUILayout.IntField(FieldLabel(field), (int) val);

                if ((int) val != newVal)
                {
                    SetFieldValue(field, newVal);
                }
            }
            else if (fieldType == typeof (float))
            {
                EditorGUILayout.BeginHorizontal();

                float newVal = EditorGUILayout.FloatField(FieldLabel(field), (float) val);

                if ((float) val != newVal)
                {
                    SetFieldValue(field, newVal);
                }
            }
#if UNITY_5_0
        else if (fieldType == typeof (double))
        {
            EditorGUILayout.BeginHorizontal();

            double newVal = EditorGUILayout.DoubleField(FieldLabel(field), (double) val);

            if ((double) val != newVal)
            {
                SetFieldValue(field, newVal);
            }
        }
#endif
            else if (fieldType == typeof (bool))
            {
                EditorGUILayout.BeginHorizontal();

                bool newVal = EditorGUILayout.Toggle(FieldLabel(field), (bool) val);

                if ((bool) val != newVal)
                {
                    SetFieldValue(field, newVal);
                }
            }
            else if (fieldType == typeof (Vector2))
            {
                EditorGUILayout.BeginHorizontal();

                Vector2 newVal = EditorGUILayout.Vector2Field(FieldLabel(field), (Vector2) val);

                if ((Vector2) val != newVal)
                {
                    SetFieldValue(field, newVal);
                }
            }
            else if (fieldType == typeof (Vector3))
            {
                EditorGUILayout.BeginHorizontal();

                Vector3 newVal = EditorGUILayout.Vector3Field(FieldLabel(field), (Vector3) val);

                if ((Vector3) val != newVal)
                {
                    SetFieldValue(field, newVal);
                }
            }
            else if (fieldType == typeof (Vector4))
            {
                EditorGUILayout.BeginHorizontal();

                Vector4 newVal = EditorGUILayout.Vector4Field(FormatName(field.Name), (Vector4) val);

                if ((Vector4) val != newVal)
                {
                    SetFieldValue(field, newVal);
                }
            }
            else if (fieldType == typeof (Color))
            {
                EditorGUILayout.BeginHorizontal();

                Color newVal = EditorGUILayout.ColorField(FieldLabel(field), (Color) val);

                if ((Color) val != newVal)
                {
                    SetFieldValue(field, newVal);
                }
            }
            else if (fieldType.IsSubclassOf(typeof (Object)))
            {
                EditorGUILayout.BeginHorizontal();

                Object newVal = EditorGUILayout.ObjectField(FieldLabel(field), (Object) val, fieldType, false);
                if ((Object) val != newVal)
                {
                    if (isDatablockReference)
                    {
                        object val2 = Activator.CreateInstance(field.FieldType, new object[] {newVal});
                        SetFieldValue(field, val2);
                    }
                    else
                    {
                        SetFieldValue(field, newVal);
                    }
                }
            }
            else if (fieldType == typeof (string))
            {
                EditorGUILayout.BeginHorizontal();

                string newVal = EditorGUILayout.TextField(FieldLabel(field), (string) val);

                if ((string) val != newVal)
                {
                    SetFieldValue(field, newVal);
                }
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof (List<>))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();

                serializedObject.Update();
                SerializedProperty listProperty = serializedObject.FindProperty(field.Name);

                try
                {
                    ReorderableListGUI.Title(field.Name);
                    ReorderableListGUI.ListField(listProperty);
                }
                catch (ExitGUIException)
                {
                    // suppress unity bug
                }

                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.EndVertical();
            }
            else if (fieldType.IsEnum)
            {
                EditorGUILayout.BeginHorizontal();

                Enum newVal;

                if (Attribute.IsDefined(fieldType, typeof (FlagsAttribute)))
                {
                    newVal = EditorGUILayout.EnumMaskField(FieldLabel(field), (Enum) val);
                }
                else
                {
                    newVal = EditorGUILayout.EnumPopup(FieldLabel(field), (Enum) val);
                }

                if (!Equals((Enum) val, newVal))
                {
                    SetFieldValue(field, newVal);
                }

            }
            else
            {
                return;
            }

            EditorGUI.indentLevel = 0;
            EditorGUI.BeginDisabledGroup(datablock.Parent == null);
            {
                bool useParentValPrev = overrideParentValue;
                overrideParentValue = EditorGUILayout.Toggle("", overrideParentValue, GUILayout.MaxWidth(20));
                if (overrideParentValue != useParentValPrev)
                {
                    datablock.SetOverridesParent(field, overrideParentValue);

                    if (overrideParentValue)
                    {
                        field.SetValue(datablock, GetDefaultValue(fieldType));
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof (List<>))
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
        }

        private GUIContent FieldLabel(FieldInfo field)
        {
            return new GUIContent(FormatName(field.Name), "Set by " + datablock.DefiningParent(field).name);
        }


        private void SetFieldValue(FieldInfo field, object newVal)
        {
            field.SetValue(datablock, newVal);
            datablock.SetOverridesParent(field, true);
        }

        private string FormatName(string name)
        {
            if (name.Length > 1)
                return char.ToUpper(name[0]) + name.Substring(1);

            return name.ToUpper();
        }

        private void DrawFields()
        {
            string currentFoldoutGroup = null;
            var foldoutOpen = false;

            foreach (MemberInfo memberData in members)
            {
                if (!ShouldShowField(memberData))
                    continue;

                var field = memberData as FieldInfo;
                if (field != null)
                {
                    var foldoutName = GetFoldoutGroup(field);

                    if (foldoutName != null)
                    {
                        if (foldoutName == "")
                        {
                            currentFoldoutGroup = null;
                        }
                        else
                        {
                            if (foldoutStates.ContainsKey(foldoutName))
                                foldoutOpen = foldoutStates[foldoutName];

                            foldoutStates[foldoutName] = EditorGUILayout.Foldout(foldoutOpen, foldoutName);
                            currentFoldoutGroup = foldoutName;
                        }
                    }

                    if (currentFoldoutGroup == null || (currentFoldoutGroup != null && foldoutOpen))
                        DrawField(field, (currentFoldoutGroup == null ? 0 : 1));
                }
            }
        }

        /// <summary>
        /// Returns the name of the foldout assigned to this field, null if no attribute found.
        /// </summary>
        /// <param name="field">Field info</param>
        /// <returns>Foldout name</returns>
        private string GetFoldoutGroup(FieldInfo field)
        {
            var foldoutAttribute = field.GetAttribute<DatablockFoldoutAttribute>();
            if (foldoutAttribute != null)
                return foldoutAttribute.groupName;

            return null;
        }

        /// <summary>
        ///     Determine if the field should be drawn or not
        /// </summary>
        /// <param name="memberData">Member to check</param>
        /// <returns>True if field should be drawn</returns>
        protected virtual bool ShouldShowField(MemberInfo memberData)
        {
            var boolLimitAttr = memberData.GetAttribute<DatablockBoolAttribute>();
            if (boolLimitAttr != null)
            {
                FieldInfo slotField = datablock.GetType().GetField(boolLimitAttr.fieldName);
                var val = datablock.GetFieldValue<bool>(slotField);

                if (!val)
                    return false;
            }

            var nullLimitAttr = memberData.GetAttribute<DatablockNullAttribute>();
            if (nullLimitAttr != null)
            {
                FieldInfo nullField = datablock.GetType().GetField(nullLimitAttr.fieldName);
                var val = datablock.GetFieldValue<object>(nullField);

                if (val == null)
                    return false;
            }


            var enumAttr = memberData.GetAttribute<DatablockEnumAttribute>();
            if (enumAttr != null)
            {
                FieldInfo checkField = datablock.GetType().GetField(enumAttr.fieldName);
                var val = datablock.GetFieldValue<Enum>(checkField);

                if (enumAttr.reverse && val.Equals(enumAttr.enumValue))
                    return false;

                if (!enumAttr.reverse && !val.Equals(enumAttr.enumValue))
                    return false;
            }


            return true;
        }

        protected void MarkDirty()
        {
            EditorUtility.SetDirty(datablock);
        }

        /// <summary>
        ///     Create a scriptable object and save it
        /// </summary>
        /// <typeparam name="T">Scriptableobject type</typeparam>
        /// <param name="name">Asset name</param>
        /// <param name="path">Path to save to</param>
        /// <returns>Newly created Scriptableobject</returns>
        public static T CreateAsset<T>(string name = "", string path = "") where T : ScriptableObject
        {
            var asset = CreateInstance<T>();

            string assetPathAndName = "";

            if (name == "")
            {
                if (path == "")
                {
                    path = AssetDatabase.GetAssetPath(Selection.activeObject);
                    if (path == "")
                    {
                        path = "Assets";
                    }
                    else if (Path.GetExtension(path) != "")
                    {
                        path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
                    }
                }

                assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof (T) + ".asset");
            }
            else
            {
                assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + name + ".asset");
            }
            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();

            Selection.activeObject = asset;
            return asset;
        }

        /// <summary>
        ///     Get the default value of a type
        /// </summary>
        /// <param name="t">Type</param>
        /// <returns>Default value</returns>
        public static object GetDefaultValue(Type t)
        {
            if (t.IsValueType && Nullable.GetUnderlyingType(t) == null)
                return Activator.CreateInstance(t);
            return null;
        }
    }
}