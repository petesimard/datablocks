using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
///     Store a reference to a Unity Object located in the resources folder.
///     Prevents Unity from including it in your scene until you access it explicitly.
/// </summary>
/// <typeparam name="T">The type of Unity Object</typeparam>
public struct DatablockRef<T> : IDatablockRef where T : Object
{
    private T value;

    private string resourcePath;

    public DatablockRef(T unityObject)
    {
        value = null;
        resourcePath = null;
        SetNewValue(unityObject);
    }

    public DatablockRef(string path)
    {
        value = null;
        resourcePath = path;
    }

    public void SetNewValue(T unityObject)
    {
        value = unityObject;

        resourcePath = "";
#if UNITY_EDITOR
        if (unityObject != null)
        {
            resourcePath = GetResourcePath(unityObject);
            if (resourcePath == null)
            {
                Debug.Log("Invalid resource: " + unityObject.name);
            }
        }
#endif
    }

#if UNITY_EDITOR
    public static string GetResourcePath(Object prefab)
    {
        string assetPath;
        assetPath = AssetDatabase.GetAssetPath(prefab);

        int resourceFolderPos = assetPath.LastIndexOf("/Resources");
        if (resourceFolderPos == -1)
        {
            Debug.LogError("Prefab must be placed in the Resouces folder! " + assetPath);
            return null;
        }

        assetPath = Path.GetDirectoryName(assetPath.Substring(resourceFolderPos + 11)) + "/" +
                    Path.GetFileNameWithoutExtension(assetPath);
        if (assetPath[0] == '/')
            assetPath = assetPath.Substring(1);
        return assetPath;
    }
#endif

    public Object GetObject()
    {
        return GetValue();
    }

    public string GetPath()
    {
        return resourcePath;
    }

    private T GetValue()
    {
        if (resourcePath == null)
            return null;

        if (value == null)
        {
            value = (T) Resources.Load(resourcePath);
        }

        return value;
    }

    /// <summary>
    ///     Access the Object referenced by the DatablockRef
    /// </summary>
    public T Value
    {
        get { return GetValue(); }
    }

    public void SetPath(string path)
    {
        resourcePath = path;
    }
}

// Helper interface for accessing paths in a non-generic manner
public interface IDatablockRef
{
    void SetPath(string path);
    Object GetObject();
    string GetPath();
}