using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.GData.Client;
using Google.GData.Spreadsheets;
using UnityEditor;
using UnityEngine;

namespace Datablocks
{

    /// <summary>
    ///     Editor window for importing spreadsheets from Google sheets
    /// </summary>
    public class ImportGoogleSpreadsheet : DatablockImporter
    {
        private readonly SheetsAPI sheetsAPI = new SheetsAPI();
        private readonly List<SpreadsheetEntry> spreadsheetEntries = new List<SpreadsheetEntry>();

        private bool canBeImported;
        private bool fieldsFoldout;
        private int importDatablockTypeIndex;
        private int nameColumnIndex = -1;
        private int spreadsheetIndex;
        private AtomEntryCollection spreadsheetRows;

        private string tempAuthtoken;

        [MenuItem("Tools/Datablocks/Import/Google Spreadsheet")]
        public static void ShowImportWindow()
        {
            GetWindow<ImportGoogleSpreadsheet>("Import Google Spreadsheet");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            sheetsAPI.Initilize();
        }

        private void OnGUI()
        {
            if (sheetsAPI.HasAccessToken())
                ShowOAuthPanel();
            else
            {
                ShowSpreadsheetList();
            }
        }

        private void ShowSpreadsheetList()
        {
            EditorGUILayout.HelpBox("Refresh spreadsheets to retrieve a list of sheets associated with the connected Google account. Only the first" +
                                    " sheet in a workbook will be loaded and the first row should contain the field names.", MessageType.None);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0, 0.8f, .8f);

            if (GUILayout.Button("Refresh Spreesheets"))
            {
                spreadsheetEntries.Clear();

                var query = new SpreadsheetQuery();

                SpreadsheetFeed feed = null;
                try
                {
                    feed = sheetsAPI.Service.Query(query);
                }
                catch (Exception)
                {
                    Debug.LogError("OAuth error");
                    sheetsAPI.ClearOAuthToken();
                    throw;
                }

                // Iterate through all of the spreadsheets returned
                foreach (SpreadsheetEntry entry in feed.Entries)
                {
                    spreadsheetEntries.Add(entry);
                }
            }

            GUI.backgroundColor = new Color(1f, 0.2f, .4f);

            if (GUILayout.Button("Clear OAuth token"))
            {
                sheetsAPI.ClearOAuthToken();
            }

            GUI.backgroundColor = Color.white;


            EditorGUILayout.EndHorizontal();

            var options = new List<string>(spreadsheetEntries.Select(s => s.Title.Text));

            if (spreadsheetEntries.Count == 0)
            {
                options.Insert(0, "No spreadsheets found");
            }
            else
            {
                options.Insert(0, "Select");
            }

            if (spreadsheetIndex >= options.Count)
                spreadsheetIndex = 0;

            int tempSpreadsheetIndex = EditorGUILayout.Popup("Select spreedsheet", spreadsheetIndex, options.ToArray());

            if (tempSpreadsheetIndex != spreadsheetIndex)
            {
                canBeImported = false;

                spreadsheetIndex = tempSpreadsheetIndex;

                if (spreadsheetIndex != 0)
                {
                    DatablockManager.Instance.RefreshAssets();

                    SpreadsheetEntry spreadsheet = spreadsheetEntries.FirstOrDefault(s => s.Title.Text == options[spreadsheetIndex]);
                    detectedTypes = ParseSpreadsheet(spreadsheet);

                    if (detectedTypes == null)
                    {
                        Debug.LogError("No data detected in sheet");
                        canBeImported = false;
                        return;
                    }

                    if (nameColumnIndex == -1)
                    {
                        Debug.LogError("Sheet must have a header field named 'Name'");
                        canBeImported = false;
                        return;
                    }

                    importDatablockTypeIndex = detectedTypes.IndexOf(detectedTypes.OrderByDescending(d => d.fields.Count).First());

                    canBeImported = true;
                }
            }

            if (spreadsheetIndex != 0 && canBeImported)
            {
                importDatablockTypeIndex = EditorGUILayout.Popup("Choose datablock type", importDatablockTypeIndex, detectedTypes.Select(d => d.datablockType.ToString()).ToArray());

                DatablockDetectionInfo datablockDetectionInfo = detectedTypes[importDatablockTypeIndex];

                fieldsFoldout = EditorGUILayout.Foldout(fieldsFoldout, datablockDetectionInfo.fields.Count + " detected fields");
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

                int existingCount = 0;
                int newCount = 0;
                foreach (ListEntry spreadsheetRow in spreadsheetRows)
                {
                    string name = spreadsheetRow.Elements[nameColumnIndex].Value;
                    if (DatablockManager.Instance.GetDatablock(name, datablockDetectionInfo.datablockType, true) != null)
                        existingCount++;
                    else
                        newCount++;
                }

                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox("Ready to import " + newCount + " new and " + existingCount + " existing datablocks.", MessageType.Info);
                EditorGUILayout.Separator();

                GUI.backgroundColor = new Color(0, 0.8f, 0);
                if (GUILayout.Button("Import!"))
                {
                    ImportRows(datablockDetectionInfo);
                    DatablockManager.Instance.RefreshAssets();
                }
            }
        }


        private void ImportRows(DatablockDetectionInfo datablockDetectionInfo)
        {
            foreach (ListEntry spreadsheetRow in spreadsheetRows)
            {
                string name = spreadsheetRow.Elements[nameColumnIndex].Value;

                Datablock datablock = GetNamedDatablock(datablockDetectionInfo, name);

                foreach (ListEntry.Custom element in spreadsheetRow.Elements)
                {
                    string fieldName = element.LocalName;
                    string fieldValue = element.Value;

                    ProcessRawField(datablockDetectionInfo, fieldName, datablock, fieldValue);
                }

                EditorUtility.SetDirty(datablock);
            }

            AssetDatabase.SaveAssets();

            Debug.Log(spreadsheetRows.Count + " datablocks imported");
        }

        private List<DatablockDetectionInfo> ParseSpreadsheet(SpreadsheetEntry spreadsheet)
        {
            WorksheetFeed wsFeed = spreadsheet.Worksheets;

            if (wsFeed.Entries.Count == 0)
                return null;

            var worksheet = (WorksheetEntry) wsFeed.Entries[0];

            if (worksheet.Rows < 2)
                return null;

            // Define the URL to request the list feed of the worksheet.
            AtomLink listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

            // Fetch the list feed of the worksheet.
            var listQuery = new ListQuery(listFeedLink.HRef.ToString());
            ListFeed listFeed = sheetsAPI.Service.Query(listQuery);

            var headers = new List<string>();
            spreadsheetRows = listFeed.Entries;
            nameColumnIndex = -1;

            var row = (ListEntry) spreadsheetRows[0];
            for (int index = 0; index < row.Elements.Count; index++)
            {
                ListEntry.Custom element = row.Elements[index];

                if (element.LocalName.Equals("name", StringComparison.OrdinalIgnoreCase))
                    nameColumnIndex = index;

                headers.Add(element.LocalName);
            }

            List<DatablockDetectionInfo> datablockTypes = AutoDetectDatablockType(headers);
            if (datablockTypes == null)
            {
                Debug.Log("Unable to auto detect datablock type");
                return null;
            }

            return datablockTypes;
        }

        private void ShowOAuthPanel()
        {
            EditorGUILayout.HelpBox("To import a spreadsheet you need to obtain a token from Google to allow access to your spreadsheets. Click the button below and paste the code Google provides into the textbox.", MessageType.Info);

            EditorGUILayout.Separator();

            if (GUILayout.Button("Request token"))
            {
                string authorizationUrl = sheetsAPI.AuthURL();
                Application.OpenURL(authorizationUrl);
            }

            EditorGUILayout.Separator();

            EditorGUIUtility.labelWidth = 85;

            EditorGUILayout.BeginHorizontal();
            tempAuthtoken = EditorGUILayout.TextField("OAuth Token:", tempAuthtoken);


            if (GUILayout.Button("OK", EditorStyles.miniButton, GUILayout.MaxWidth(50)))
            {
                sheetsAPI.SetAccessCode(tempAuthtoken);

                Debug.Log("Access token recieved");
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}