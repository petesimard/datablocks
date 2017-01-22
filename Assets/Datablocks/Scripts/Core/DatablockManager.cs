using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Datablocks
{


    /// <summary>
    ///     Singleton datablock manager. The gameobject this is attached to will be marked
    ///     as DontDestroyOnLoad. The gameobject should be created in your first scene and not
    ///     created again.
    /// </summary>
    [ExecuteInEditMode]
    public class DatablockManager : MonoBehaviour
    {
        [SerializeField] [HideInInspector] private List<Datablock> datablocks = new List<Datablock>();

        private Dictionary<string, Datablock> datablockDictionary;

        private static DatablockManager instance;

        public static DatablockManager Instance
        {
            get
            {
                EnsureInitilized();
                return instance;
            }
        }

        public bool searchEntireProject = true;
        public string[] customSearchPaths = {"Datablocks\\Example\\Game Data"};

        /// <summary>
        ///     Ensure the manager has been created
        /// </summary>
        public static void EnsureInitilized()
        {
            if (!instance)
            {
                instance = FindObjectOfType<DatablockManager>();
                if (!instance)
                {
                    var manager = new GameObject("Datablock Manager", typeof (DatablockManager));
                    instance = manager.GetComponent<DatablockManager>();
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        ///     Restore the lists to their original value when leaving playmode
        /// </summary>
        private void RestoreLists()
        {
            foreach (Datablock datablock in datablocks)
            {
                datablock.RestoreLists();
            }
        }

        /// <summary>
        ///     Search the project for datablocks. Generates a list of all datablocks
        /// </summary>
        public void RefreshAssets()
        {
            datablocks.Clear();
            string sDataPath = Application.dataPath;

            if (searchEntireProject)
            {
                // Search the entire asset library for datablocks
                AddDatablocksInPath(sDataPath);
            }
            else
            {
                // Search only the specified paths
                foreach (string datablockSearchPath in customSearchPaths)
                {
                    string sFolderPath = sDataPath.Substring(0, sDataPath.Length - 6) + "Assets\\" + datablockSearchPath;
                    AddDatablocksInPath(sFolderPath);
                }
            }


            foreach (Datablock datablock in datablocks)
            {
                int ct = datablocks.Count(d => d.DatablockId == datablock.DatablockId);
                if (datablock.DatablockId == 0 || ct > 1)
                {
                    for (int dbId = 1; dbId < ushort.MaxValue; dbId++)
                    {
                        bool existing = datablocks.Any(d => d.DatablockId == dbId);
                        if (existing)
                            continue;

                        datablock.DatablockId = dbId;
                        EditorUtility.SetDirty(datablock);
                        break;
                    }
                }
            }

            // If the datablock manager is part of a prefab, update the prefab
            Object prefab = PrefabUtility.GetPrefabParent(gameObject);
            if (prefab)
            {
                PrefabUtility.ReplacePrefab(gameObject, prefab, ReplacePrefabOptions.ConnectToPrefab);
                EditorUtility.SetDirty(prefab);
            }

            //Debug.Log("Datablocks refreshed - " + datablocks.Count + " Datablocks"); 
        }

        /// <summary>
        ///     Add the datablocks in a path
        /// </summary>
        /// <param name="sFolderPath">Path</param>
        private void AddDatablocksInPath(string sFolderPath)
        {
            string sDataPath = Application.dataPath;

            var directories = new List<string>(Directory.GetDirectories(sFolderPath));

            foreach (string directory in directories)
            {
                AddDatablocksInPath(directory);
            }

            // get the system file paths of all the files in the asset folder
            string[] aFilePaths = Directory.GetFiles(sFolderPath, "*.asset");

            // enumerate through the list of files loading the assets they represent and getting their type		
            foreach (string sFilePath in aFilePaths)
            {
                string sAssetPath = sFilePath.Substring(sDataPath.Length - 6);

                var datablock = AssetDatabase.LoadAssetAtPath(sAssetPath, typeof (Datablock)) as Datablock;
                if (datablock == null)
                    continue;

                datablocks.Add(datablock);
            }
        }
#endif

        private void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
#endif
        }

#if UNITY_EDITOR

        private void OnDisable()
        {
            EditorApplication.playmodeStateChanged -= PlaymodeStateChanged;
        }

        private void PlaymodeStateChanged()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                Instance.RefreshAssets();
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying)
            {
                Instance.RestoreLists();
            }
        }
#endif

        private void Awake()
        {
            // If there is already a DatablockManager, destroy this one
            if (instance)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (Application.isPlaying)
            {
                ProcessDatablocks();
                DontDestroyOnLoad(gameObject);

            }
            else
            {
#if UNITY_EDITOR
                Instance.RefreshAssets();
#endif
            }
        }

        /// <summary>
        ///     Process the datablocks on first load
        /// </summary>
        private void ProcessDatablocks()
        {
            IEnumerable<Datablock> baseLevelDatblocks = datablocks.Where(d => d.Parent == null);
            foreach (Datablock datablock in baseLevelDatblocks)
            {
                datablock.ProcessChildren();
            }

            // For fast lookup of datablocks by name
            datablockDictionary = new Dictionary<string, Datablock>();
            foreach (Datablock datablock in datablocks)
            {
                try
                {
                    datablockDictionary.Add(datablock.GetType() + datablock.name, datablock);
                }
                catch (Exception)
                {
                    Debug.LogError("Error adding datablock '" + datablock.name + " (" + datablock.GetType() + ") to dictionary. Possible duplicate name");
                }
            }

            Debug.Log("Processed datablocks");
        }

        /// <summary>
        ///     Get a datablock with the specified ID
        /// </summary>
        /// <typeparam name="T">Datablock type</typeparam>
        /// <param name="id">Datablock id</param>
        /// <returns>Datablock</returns>
        public T GetDatablock<T>(int id) where T : Datablock
        {
            return (T) datablocks.FirstOrDefault(d => d.DatablockId == id);
        }

        /// <summary>
        ///     Get a datablock with the specified name
        /// </summary>
        /// <typeparam name="T">Type of the datablock</typeparam>
        /// <param name="datablockName">Datablock name</param>
        /// <param name="caseInsensitive">Search case insensitive</param>
        /// <returns>Datablock</returns>
        public T GetDatablock<T>(string datablockName, bool caseInsensitive = false) where T : Datablock
        {
            Datablock d;

            if (caseInsensitive)
            {
                d = datablockDictionary.Values.FirstOrDefault(db => db is T && db.name.Equals(datablockName, StringComparison.InvariantCultureIgnoreCase));
                return (T) d;
            }

            if (!datablockDictionary.TryGetValue(typeof (T) + datablockName, out d))
                return null;

            if (d is T)
                return (T) d;

            return null;
        }

        /// <summary>
        ///     Get a datablock with the specified name
        /// </summary>
        /// <param name="datablockName">Datablock name</param>
        /// <returns>Datablock</returns>
        public Datablock GetDatablock(string datablockName)
        {
            return datablocks.FirstOrDefault(d => d.name == datablockName);
        }

        /// <summary>
        ///     Get a datablock with the specified name
        /// </summary>
        /// <param name="datablockName">Datablock name</param>
        /// <returns>Datablock</returns>
        public Datablock GetDatablock(string datablockName, bool caseInsensitive = false)
        {
            if (caseInsensitive)
                return datablocks.FirstOrDefault(d => d.name.Equals(datablockName, StringComparison.InvariantCultureIgnoreCase));
            else
                return datablocks.FirstOrDefault(d => d.name == datablockName);
        }

        /// <summary>
        ///     Get a datablock with the specified name
        /// </summary>
        /// <param name="datablockName">Datablock name</param>
        /// <param name="datablockType">Type of the datablock</param>
        /// <param name="caseInsensitive">Search case insensitive</param>
        /// <returns></returns>
        public Datablock GetDatablock(string datablockName, Type datablockType, bool caseInsensitive = false)
        {
            if (caseInsensitive)
                return datablocks.FirstOrDefault(d => d.name.Equals(datablockName, StringComparison.InvariantCultureIgnoreCase) && d.GetType() == datablockType);
            return datablocks.FirstOrDefault(d => d.name == datablockName && d.GetType() == datablockType);
        }

        /// <summary>
        ///     Get all datablocks of the specified type
        /// </summary>
        /// <typeparam name="T">Datablock type</typeparam>
        /// <returns>List of all datablocks of the specified type</returns>
        public IEnumerable<T> GetDatablocks<T>() where T : Datablock
        {
            return datablocks.Where(d => d is T).Cast<T>();
        }

        /// <summary>
        ///     Get all datablocks of the specified type
        /// </summary>
        /// <param name="datablockType">Datablock type</param>
        /// <returns>List of all datablocks of the specified type</returns>
        public IEnumerable<Datablock> GetDatablocks(Type datablockType)
        {
            return datablocks.Where(d => d.GetType() == datablockType);
        }

        /// <summary>
        ///     Get the total count of all tracked datablocks
        /// </summary>
        /// <returns>Number of tracked datablocks</returns>
        public int Count()
        {
            return datablocks.Count;
        }

        /// <summary>
        /// Get a name for a datablock that is unique to that type
        /// </summary>
        /// <param name="datablockName">Desired name</param>
        /// <param name="datablockType">Datablock type</param>
        /// <returns>Unique name</returns>
        public string GetUniqueName(string datablockName, Type datablockType)
        {
            var existing = GetDatablocks(datablockType).Any(d => d.name == datablockName);
            if (!existing)
                return datablockName;

            for (var x = 1; x < 1000; x++)
            {
                var newDatablockname = datablockName + " " + x;
                existing = GetDatablocks(datablockType).Any(d => d.name == newDatablockname);
                if (!existing)
                    return newDatablockname;
            }

            Debug.LogError("Unable to find a unique name for " + datablockName);
            return datablockName;
        }
    }
}