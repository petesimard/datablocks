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
    ///     Editor window for exporting to Google Sheets
    /// </summary>
    public class ExportGoogleSpreadsheet : DatablockExporter
    {
        private readonly SheetsAPI sheetsAPI = new SheetsAPI();
        private readonly List<SpreadsheetEntry> spreadsheetEntries = new List<SpreadsheetEntry>();
        private CellFeed batchRequest;
        private bool canBeExported;
        private CellFeed cellFeed;
        private int currentCellIndex;
        private FieldInfo[] datablockFields;
        private int datablockTypeIndex;
        private bool exportData = true;
        private ListFeed listFeed;
        private bool showExportOptions;

        private SpreadsheetEntry spreadsheet;
        private int spreadsheetIndex;
        private string tempAuthtoken;

        [MenuItem("Tools/Datablocks/Export/Google Spreadsheet")]
        public static void ShowExportWindow()
        {
            GetWindow<ExportGoogleSpreadsheet>("Export Google Spreadsheet");
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
                ShowExportUI();
            }
        }


        private void ShowOAuthPanel()
        {
            EditorGUILayout.HelpBox("To export a spreadsheet you need to obtain a token from Google to allow access to your spreadsheets. Click the button below and paste the code Google provides into the textbox.", MessageType.Info);

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

        private void ShowExportUI()
        {
            DatablockManager datablockManager = DatablockManager.Instance;

            EditorGUILayout.HelpBox("Export datablocks of the selected type to a Google sheet. Select the worksheet you want to export to. Note: All exiting data on the first sheet will be overwritten.", MessageType.None);

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
                canBeExported = false;

                spreadsheetIndex = tempSpreadsheetIndex;

                if (spreadsheetIndex != 0)
                {
                    DatablockManager.Instance.RefreshAssets();

                    spreadsheet = spreadsheetEntries.FirstOrDefault(s => s.Title.Text == options[spreadsheetIndex]);

                    if (spreadsheet.Worksheets.Entries.Count == 0)
                    {
                        Debug.LogError("Spreadsheet has no worksheet. Please add a worksheet to use this spreadsheet.");
                        return;
                    }

                    canBeExported = true;
                }
            }

            if (spreadsheetIndex != 0 && canBeExported)
            {
                List<Type> types = DatablockList.GetInstances(typeof (Datablock));

                datablockTypeIndex = EditorGUILayout.Popup("Choose datablock type", datablockTypeIndex, types.Select(d => d.ToString()).ToArray());
                Type datablockType = types[datablockTypeIndex];


                GUI.backgroundColor = new Color(0, 0.8f, 0);
                string exportLabel = exportData ? "Export " + datablockManager.GetDatablocks(datablockType).Count() + " datablocks" : "Export schema";

                if (GUILayout.Button(exportLabel))
                {
                    ExportDatablocks(datablockType);
                }

                GUI.backgroundColor = Color.white;
                EditorGUILayout.HelpBox("All data on the first sheet will be overwritten.", MessageType.Warning);
            }

            showExportOptions = EditorGUILayout.Foldout(showExportOptions, "Export options");

            if (showExportOptions)
            {
                EditorGUI.indentLevel = 1;

                exportData = EditorGUILayout.Toggle("Export data", exportData);
                exportFullValues = EditorGUILayout.Toggle(new GUIContent("Include full values", "Exports the full value of a field even if it inherits its value."), exportFullValues);
            }
        }

        private void ExportDatablocks(Type datablockType)
        {
            var worksheet = (WorksheetEntry) spreadsheet.Worksheets.Entries[0];
            IEnumerable<Datablock> datablocks = DatablockManager.Instance.GetDatablocks(datablockType);
            datablockFields = Datablock.GetFields(datablockType);
            List<string> headers = datablockFields.Select(f => f.Name).ToList();
            headers.Insert(0, "Parent");
            headers.Insert(0, "Name");

            // Set the worksheet to a single row for our headers
            worksheet.Cols = (uint) headers.Count;
            worksheet.Rows = (exportData ? (uint) datablocks.Count() : 0) + 1;
            worksheet.Update();

            if (exportData)
            {
                // Fetch the cell feed of the worksheet.
                var cellQuery = new CellQuery(worksheet.CellFeedLink);
                cellQuery.ReturnEmpty = ReturnEmptyCells.yes;
                cellFeed = sheetsAPI.Service.Query(cellQuery);

                batchRequest = new CellFeed(cellQuery.Uri, sheetsAPI.Service);

                currentCellIndex = 0;
                // Set headers
                for (int index = 0; index < headers.Count; index++)
                {
                    string cellValue = headers[index];

                    SetNextCellValue(cellValue);
                }

                currentCellIndex = headers.Count;

                IEnumerable<Datablock> baseDatablocks = datablocks.Where(d => d.Parent == null);

                // Process the datablocks from the base to their children
                foreach (Datablock baseDatablock in baseDatablocks)
                {
                    ProcessChildren(baseDatablock);
                }

                sheetsAPI.Service.Batch(batchRequest, new Uri(cellFeed.Batch));
            }

            Debug.Log("Datablocks saved to " + spreadsheet.Title.Text);
        }

        private void SetNextCellValue(string cellValue)
        {
            var cell = (CellEntry) cellFeed.Entries[currentCellIndex];

            cell.InputValue = cellValue;

            cell.BatchData = new GDataBatchEntryData(string.Format("R{0}C{1}", cell.Row, cell.Column),
                GDataBatchOperationType.update);
            batchRequest.Entries.Add(cell);

            currentCellIndex++;
        }

        protected override void ProcessDatablock(Datablock datablock)
        {
            SetNextCellValue(datablock.name);
            SetNextCellValue(datablock.Parent ? datablock.Parent.name : "");

            foreach (FieldInfo field in datablockFields)
            {
                string value = GetFieldValue(datablock, field);
                SetNextCellValue(value);
            }
        }
    }
}