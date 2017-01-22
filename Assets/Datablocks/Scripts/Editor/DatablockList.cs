using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Datablocks
{

    /// <summary>
    ///     Editor window for listing all tracked datablocks and allows filtering
    /// </summary>
    public class DatablockList : EditorWindow
    {
        private const char openingChar = '[';
        private const char seperator = ':';
        private const char endingChar = ']';
        private Texture2D addTexture;
        private Texture2D childTexture;
        private Texture2D clearTexture;
        private List<Type> datablockTypes;
        private string filterText;
        private int filterTypeIndex;
        private Texture2D removeTexture;
        private Vector2 scrollPos;
        private Texture2D siblingTexture;

        [MenuItem("Tools/Datablocks/View Datablocks")]
        public static void ViewDatablocks()
        {
            GetWindow(typeof (DatablockList));
        }

        [MenuItem("Tools/Datablocks/Online Help")]
        public static void ViewHelp()
        {
            Application.OpenURL("http://unitydatablocks.com/docs/manual");
        }

        public static List<Type> GetInstances(Type baseType)
        {
            var list = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                list.AddRange(from t in assembly.GetTypes() where t.IsSubclassOf(baseType) select t);
            }
            return list;
        }

        private void OnEnable()
        {
            datablockTypes = GetInstances(typeof (Datablock));
            clearTexture = Resources.Load("closeIcon") as Texture2D;
            childTexture = Resources.Load("child2") as Texture2D;
            removeTexture = Resources.Load("remove") as Texture2D;
            addTexture = Resources.Load("plus") as Texture2D;
            siblingTexture = Resources.Load("siblings2") as Texture2D;

            DatablockManager.Instance.RefreshAssets();
        }

        private void OnProjectChange()
        {
            DatablockManager.Instance.RefreshAssets();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Create new", EditorStyles.toolbarDropDown, GUILayout.Width(90)))
            {
                var menu = new GenericMenu();

                foreach (Type datablockType in datablockTypes)
                {
                    Type type = datablockType; // Copy to local value because of closure
                    string datablockName = datablockType.Name;
                    var category = type.GetAttribute<DatablockCategoryAttribute>();

                    if (category != null)
                    {
                        datablockName = category.category + "/" + datablockName;
                    }

                    menu.AddItem(new GUIContent(datablockName), false, () => CreateNewDatablock(type));
                }

                menu.ShowAsContext();
            }

            EditorGUIUtility.labelWidth = 50;
            filterText = EditorGUILayout.TextField("Filter:", filterText, EditorStyles.toolbarTextField, new[] {GUILayout.ExpandWidth(true)}) ?? "";

            Texture2D clearTextureToUse = filterText == "" ? null : clearTexture;

            var clearStyle = new GUIStyle(GUIStyle.none);
            clearStyle.padding = new RectOffset(1, 1, 3, 1);


            if (GUILayout.Button(clearTextureToUse, clearStyle, new[] {GUILayout.Width(15)}))
            {
                filterText = "";
                GUI.FocusControl(null);
            }

            var datablockNames = new List<string>();
            foreach (Type datablockType in datablockTypes)
            {
                string datablockName = datablockType.Name;
                datablockNames.Add(datablockName);
            }

            var typeList = new List<string>(datablockNames);
            typeList.Insert(0, "None");

            EditorGUIUtility.labelWidth = 80;

            string filterTypeText = GetParameterValue("T");
            if (filterTypeText != null)
            {
                filterTypeIndex = typeList.IndexOf(filterTypeText);
                if (filterTypeIndex < 0)
                    filterTypeIndex = 0;
            }
            else
            {
                filterTypeIndex = 0;
            }

            int tempFilterTypeIndex = EditorGUILayout.Popup("Type Filter:", filterTypeIndex, typeList.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(200));

            if (filterTypeIndex != tempFilterTypeIndex)
            {
                filterTypeIndex = tempFilterTypeIndex;
                if (filterTypeIndex != 0)
                {
                    SetFilterParameter("T", typeList[filterTypeIndex]);
                }
                else
                {
                    SetFilterParameter("T", null);
                }
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("ID", EditorStyles.boldLabel, GUILayout.Width(60f));
            GUILayout.Label("Name", EditorStyles.boldLabel, new[] {GUILayout.ExpandWidth(true), GUILayout.MinWidth(165)});
            GUILayout.Label("Type", EditorStyles.boldLabel, new[] {GUILayout.MaxWidth(170f), GUILayout.MinWidth(100)});
            GUILayout.Label("Parent", EditorStyles.boldLabel, GUILayout.MinWidth(150));
            GUILayout.Label("Actions", EditorStyles.boldLabel, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();


            List<Datablock> datablocks = GetFilteredDatablocks().ToList();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            var lineStyle = new GUIStyle(EditorStyles.toolbarTextField);
            lineStyle.fixedHeight = 23;
            //lineStyle.margin = new RectOffset(0, 0, 3, 7);
            lineStyle.padding = new RectOffset(0, 0, 3, 3);

            var nameStyle = new GUIStyle(EditorStyles.label);
            nameStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(.04f, .5f, 0.85f) : new Color(.02f, .1f, 0.65f);

            var toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
            toolbarButtonStyle.padding = new RectOffset(2, 2, 2, 2);


            if (datablocks.Count == 0)
            {
                GUILayout.Label("No results");
            }

            bool alternateColor = false;
            for (int i = 0; i < datablocks.Count; i++)
            {
                Datablock datablock = datablocks[i];

                if (!datablock)
                    continue;

                GUI.backgroundColor = alternateColor ? Color.white : new Color(0.8f, 0.8f, 0.85f);
                alternateColor = !alternateColor;

                EditorGUILayout.BeginHorizontal(lineStyle);

                // ID
                GUILayout.Label(datablock.DatablockId.ToString(), GUILayout.Width(60f));

                // Name
                if (GUILayout.Button(datablock.name, nameStyle, new[] {GUILayout.ExpandWidth(true), GUILayout.MinWidth(165)}))
                {
                    SelectDatablock(datablock);
                }

                // Type
                GUILayout.Label(datablock.GetType().Name, new[] {GUILayout.MaxWidth(170f), GUILayout.MinWidth(100)});

                // Parent
                if (datablock.Parent == null)
                {
                    GUILayout.Label("None", GUILayout.MinWidth(150));
                }
                else
                {
                    if (GUILayout.Button(datablock.Parent.name, nameStyle, GUILayout.MinWidth(150)))
                    {
                        SelectDatablock(datablock.Parent);
                    }
                }

                // Start toolbar buttons
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(110f));

                // Select children
                if (GUILayout.Button(new GUIContent(childTexture, "Select children"), toolbarButtonStyle, GUILayout.Width(25)))
                {
                    SetFilterParameter("P", datablock.DatablockId.ToString(), true);
                }

                // Select silblibngs
                if (GUILayout.Button(new GUIContent(siblingTexture, "Select siblings"), toolbarButtonStyle, GUILayout.Width(25)))
                {
                    if (datablock.Parent)
                    {
                        SetFilterParameter("S", datablock.Parent.DatablockId.ToString(), true);
                    }
                    else
                    {
                        SetFilterParameter("S", "0", true);
                    }
                }

                // New Child
                if (GUILayout.Button(new GUIContent(addTexture, "Create child from this datablock"), toolbarButtonStyle, GUILayout.Width(25)))
                {
                    CreateChildDatablock(datablock);
                }

                // Delete
                if (GUILayout.Button(new GUIContent(removeTexture, "Delete datablock"), toolbarButtonStyle, GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete datablock", "Are you sure you want to delete " + datablock.name + "?", "Delete", "Cancel"))
                    {
                        string pathToDelete = AssetDatabase.GetAssetPath(datablock);
                        AssetDatabase.DeleteAsset(pathToDelete);
                    }
                }

                EditorGUILayout.EndHorizontal();


                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void CreateChildDatablock(Datablock datablock)
        {
            var newDatablock = CreateInstance(datablock.GetType()) as Datablock;
            newDatablock.Parent = datablock;
            string pathName = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(datablock));
            AssetDatabase.CreateAsset(newDatablock, pathName);
            Selection.activeObject = newDatablock;
        }

        private void SelectDatablock(Datablock datablock)
        {
            Selection.activeObject = datablock;
        }

        /// <summary>
        ///     Get datablocks that match the filter
        /// </summary>
        /// <returns>List of matching datablocks</returns>
        private IEnumerable<Datablock> GetFilteredDatablocks()
        {
            string filterType = GetParameterValue("T");
            string filterParent = GetParameterValue("P");
            string filterSiblings = GetParameterValue("S");

            IEnumerable<Datablock> datablocks = null;
            if (filterType != null)
            {
                // Filter based on type
                Type type = datablockTypes.FirstOrDefault(t => t.Name == filterType);

                if (type != null)
                {
                    datablocks = DatablockManager.Instance.GetDatablocks(type);
                }
            }

            if (datablocks == null)
                datablocks = DatablockManager.Instance.GetDatablocks<Datablock>();

            if (filterParent != null)
            {
                // Filter based on parent
                int filterParentId;
                int.TryParse(filterParent, out filterParentId);
                datablocks = datablocks.Where(d => d.Parent != null && d.Parent.DatablockId == filterParentId);
            }

            if (filterSiblings != null)
            {
                // Filter based on sibling
                int filterSiblingId;
                int.TryParse(filterSiblings, out filterSiblingId);

                var parent = DatablockManager.Instance.GetDatablock<Datablock>(filterSiblingId);
                datablocks = datablocks.Where(d => d.Parent == parent);
            }

            MatchCollection matches = Regex.Matches(filterText, @"\s*(\[.*\])*(.*)");
            if (matches.Count != 0)
            {
                Match match = matches[0];

                if (match.Groups.Count > 2)
                {
                    string stringSearch = match.Groups[2].ToString();
                    datablocks = datablocks.Where(d => d.name.IndexOf(stringSearch, StringComparison.OrdinalIgnoreCase) != -1);
                }
            }

            return datablocks.OrderBy(d => d.DatablockId);
        }

        private string GetParameterValue(string parameter)
        {
            int existing = filterText.IndexOf(openingChar + parameter);
            if (existing == -1)
            {
                return null;
            }

            int end = filterText.IndexOf(endingChar, existing);
            if (end == -1)
            {
                return null;
            }

            return filterText.Substring(existing + parameter.Length + 2, end - (existing + parameter.Length + 2));
        }

        // Change the filter text to add or remove a paremeter
        private void SetFilterParameter(string parameter, string value, bool clearOtherFilter = false)
        {
            if (clearOtherFilter)
            {
                filterText = openingChar + parameter + seperator + value + endingChar;
                return;
            }

            int existing = filterText.IndexOf(openingChar + parameter);

            if (existing == -1)
            {
                // This parameter doesn't exist yet
                filterText = openingChar + parameter + seperator + value + endingChar + filterText;
            }
            else
            {
                // Parameter exists, change or delete it
                int end = filterText.IndexOf(endingChar, existing);
                if (end == -1)
                {
                    filterText = openingChar + parameter + seperator + value + endingChar;
                    return;
                }

                if (value == null)
                {
                    filterText = filterText.Substring(0, existing) + filterText.Substring(end + 1, filterText.Length - end - 1);
                    filterText = filterText.Trim();
                }
                else
                {
                    filterText = filterText.Substring(0, existing + parameter.Length + 2) + value + filterText.Substring(end, filterText.Length - end);
                }
            }
        }

        private void CreateNewDatablock(Type datablockType)
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

            string relativePath = path.Substring(assetsIndex + 1);
            MethodInfo method = typeof (DatablockEditor).GetMethod("CreateAsset", BindingFlags.Static | BindingFlags.Public);
            MethodInfo generic = method.MakeGenericMethod(datablockType);
            generic.Invoke(null, new object[] {DatablockManager.Instance.GetUniqueName(datablockType.Name, datablockType), relativePath});
        }
    }
}