using Datablocks;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Object = UnityEngine.Object;


/// <summary>
///     Base class all datablocks inherit from.
/// </summary>
public abstract class Datablock : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField] private StringStringDictionary assetPaths = new StringStringDictionary();

    private Dictionary<string, List<object>> originalLists = new Dictionary<string, List<object>>();
    [SerializeField] private StringBoolDictionary overrideParent = new StringBoolDictionary();
    [SerializeField] private int _datablockId;

    [SerializeField] private Datablock parent;

    /// <summary>
    /// Datablock parent
    /// </summary>
    public Datablock Parent
    {
        get { return parent; }
        set { parent = value; }
    }

    /// <summary>
    /// Internal datablock ID
    /// </summary>
    public int DatablockId
    {
        get { return _datablockId; }
        set { _datablockId = value; }
    }

    /// <summary>
    ///     Serialize asset paths
    /// </summary>
    public void OnBeforeSerialize()
    {
        FieldInfo[] members = GetFields(GetType());
        foreach (FieldInfo member in members)
        {
            Type fieldType = GetUnderlyingType(member);
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof (DatablockRef<>))
            {
                var dbref = GetFieldValue<IDatablockRef>(member);
                if (dbref != null)
                {
                    assetPaths[member.Name] = dbref.GetPath();
                }
            }
        }
    }

    /// <summary>
    ///     Restore asset paths
    /// </summary>
    public void OnAfterDeserialize()
    {
        FieldInfo[] members = GetFields(GetType());
        foreach (FieldInfo member in members)
        {
            Type fieldType = GetUnderlyingType(member);
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof (DatablockRef<>))
            {
                var dbref = GetFieldValue<IDatablockRef>(member);

                if (dbref != null && assetPaths.ContainsKey(member.Name))
                {
                    var constructor = member.FieldType.GetConstructor(new Type[]{ typeof (string)} );
                    object val = constructor.Invoke(new object[] { assetPaths[member.Name]});

                    member.SetValue(this, val);
                }
            }
        }
    }

    /// <summary>
    ///     Called when entering playmode for the first time. Assigns the fields of the datablock
    ///     based on their parent's values (If not set to override).
    /// </summary>
    public void AssignFieldsFromParent()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Can't assign fields while in the editor!");
            return;
        }

        originalLists.Clear();

        foreach (FieldInfo memberInfo in GetFields(GetType()))
        {
            FieldInfo field = memberInfo;
            if (field != null)
            {
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof (List<>) && !DoesOverridesParent(field))
                {
                    // Generate a combined list
                    Type elementType = field.FieldType.GetGenericArguments()[0];
                    MethodInfo method = typeof (Datablock).GetMethod("GetCombinedList", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo generic = method.MakeGenericMethod(elementType);
                    object combinedList = generic.Invoke(this, new object[] {field, null});
                    field.SetValue(this, combinedList);
                }
                else
                {
                    // Standard field
                    var val = GetFieldValue<object>(field);
                    field.SetValue(this, val);
                }
            }
        }
    }

    /// <summary>
    ///     Generate a complete list using the parents values at runtime
    /// </summary>
    /// <typeparam name="T">List type</typeparam>
    /// <param name="fieldInfo">The list field</param>
    /// <param name="theList">Reference to the list being worked on</param>
    /// <returns>Current combined list</returns>
    private List<T> GetCombinedList<T>(FieldInfo fieldInfo, List<T> theList) where T : class
    {
        bool copyList = false;
        if (theList == null)
        {
            copyList = true;
            theList = new List<T>();
        }

        var thisDatablockList = (List<T>) fieldInfo.GetValue(this);
        if (thisDatablockList != null)
        {
            theList.AddRange(thisDatablockList);
        }

        if (copyList)
        {
            List<object> boxedList = theList.Cast<object>().ToList();
            originalLists[fieldInfo.Name] = boxedList;
        }

        if (DoesOverridesParent(fieldInfo))
            return theList;

        return Parent.GetCombinedList(fieldInfo, theList);
    }

    /// <summary>
    ///     After returning from playmode, the editor will restore the lists value to its original state
    /// </summary>
    public void RestoreLists()
    {
        foreach (var originalListKV in originalLists)
        {
            FieldInfo field = GetType().GetField(originalListKV.Key);
            Type elementType = field.FieldType.GetGenericArguments()[0];
            MethodInfo method = typeof (Datablock).GetMethod("RestoreList", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo generic = method.MakeGenericMethod(elementType);
            generic.Invoke(this, new object[] {field, originalListKV.Value});
        }
    }

    private void RestoreList<T>(FieldInfo field, List<object> backedupList)
    {
        field.SetValue(this, backedupList.Cast<T>().ToList());
    }

    /// <summary>
    ///     Get a list of valid fields this datablock can access
    /// </summary>
    /// <returns>Array of valid fields</returns>
    public static FieldInfo[] GetFields(Type datablockType)
    {
        var fields = new List<FieldInfo>();
        foreach (FieldInfo field in datablockType.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!IsValidMemberType(field))
                continue;

            if (!(field.DeclaringType == typeof (Datablock) || field.DeclaringType.IsSubclassOf(typeof (Datablock))))
                continue;

            if (field.DeclaringType == typeof (Datablock))
                continue;

            fields.Add(field);
        }

        return fields.OrderBy(property => OrderAttribute.GetMemberOrder(property)).ToArray();
    }

    /// <summary>
    ///     Determines if the field is a valid datablock type
    /// </summary>
    /// <param name="member">Field to check</param>
    /// <returns>Field is valid or not</returns>
    private static bool IsValidMemberType(MemberInfo member)
    {
        Type fieldType = GetUnderlyingType(member);
        if (fieldType == null)
            return false;


        return (fieldType == typeof (int) ||
                fieldType == typeof (float) ||
                fieldType == typeof (double) ||
                fieldType == typeof (Vector2) ||
                fieldType == typeof (Vector3) ||
                fieldType == typeof (Vector4) ||
                fieldType == typeof (bool) ||
                fieldType == typeof (Color) ||
                fieldType == typeof (string) ||
                fieldType.IsSubclassOf(typeof (Object)) ||
                fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof (List<>) ||
                fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof (DatablockRef<>) ||
                fieldType.IsEnum);
    }

    /// <summary>
    ///     Helper function to get the type of the member
    /// </summary>
    /// <param name="member">Member to check</param>
    /// <returns>Type of the member</returns>
    private static Type GetUnderlyingType(MemberInfo member)
    {
        if (member.MemberType == MemberTypes.Field)
            return ((FieldInfo) member).FieldType;
        if (member.MemberType == MemberTypes.Property)
            return ((PropertyInfo) member).PropertyType;
        return null;
    }

    /// <summary>
    ///     Determine if the parent is valid for this datablock
    /// </summary>
    /// <param name="newParent">Parent to check</param>
    /// <returns>Parent is valid or not</returns>
    public bool IsParentValid(Datablock newParent)
    {
        if (newParent == this)
            return false;

        Datablock checkParent = newParent;
        while (checkParent != null)
        {
            if (checkParent == this)
                return false;

            checkParent = checkParent.Parent;
        }

        return true;
    }

    /// <summary>
    ///     Get the value of a field using its inheritance chain
    /// </summary>
    /// <typeparam name="T">Type of the field</typeparam>
    /// <param name="field">Field to get the value of</param>
    /// <returns>Field value</returns>
    public T GetFieldValue<T>(FieldInfo field)
    {
        if (DoesOverridesParent(field))
        {
            return (T) field.GetValue(this);
        }

        if (Parent == null)
        {
            return default(T);
        }

        return Parent.GetFieldValue<T>(field);
    }

    /// <summary>
    ///     Get the value of a field using its inheritance chain
    /// </summary>
    /// <param name="field">Field to get the value of</param>
    /// <returns>Field value</returns>
    public object GetFieldValue(FieldInfo field)
    {
        if (DoesOverridesParent(field))
        {
            return field.GetValue(this);
        }

        if (Parent == null)
        {
            return null;
        }

        return Parent.GetFieldValue(field);
    }

    /// <summary>
    ///     Get the value of a field using its inheritance chain
    /// </summary>
    /// <param name="fieldName">Field name to get the value of</param>
    /// <returns>Field value</returns>
    public T GetFieldValue<T>(string fieldName)
    {
        FieldInfo field = GetType().GetField(fieldName);
        return GetFieldValue<T>(field);
    }

    /// <summary>
    ///     Get the path to a resource used in a DatablockRef field
    /// </summary>
    /// <param name="fieldName">Name of the DatablockRef field</param>
    /// <returns>Resource path</returns>
    public string GetResourcePath(string fieldName)
    {
        string assetPath = "";
        assetPaths.TryGetValue(fieldName, out assetPath);

        return assetPath;
    }

    /// <summary>
    ///     Check if a field is set to override its parents value with its own.
    ///     If true, the datablock will use its own value, not inheriting its parent's value
    /// </summary>
    /// <param name="field">Field to check</param>
    /// <returns>True if no parent, or is set to override parent</returns>
    public bool DoesOverridesParent(FieldInfo field)
    {
        if (!parent)
            return true;

        bool overrides;
        overrideParent.TryGetValue(field.Name, out overrides);
        return overrides;
    }

    /// <summary>
    ///     Set the field to override its parent's value with its own
    /// </summary>
    /// <param name="field">Field to set</param>
    /// <param name="overrides">Override toggle</param>
    public void SetOverridesParent(FieldInfo field, bool overrides)
    {
        lock (overrideParent)
        {
            // We need to lock the thread because Unity's serializer is not run from the main thread.
            // This can cause issues when enumerating the dictionary for serialization.
            overrideParent[field.Name] = overrides;
        }
    }

    /// <summary>
    ///     Determine how many datablocks away in the inheritance chain a datablock is from this datablock.
    ///     For example, the parent of this datablock will have a depth = 1
    /// </summary>
    /// <param name="datablock">Parent datablock to check for</param>
    /// <returns>Depth of the parent. Returns -1 if the parent doesn't exist in the inheritance chain</returns>
    public int GetDepthOfParent(Datablock datablock)
    {
        return ParentDepthCheck(datablock);
    }

    private int ParentDepthCheck(Datablock datablock, int depth = 0)
    {
        if (datablock == this)
            return depth;

        if (Parent != null)
        {
            depth++;
            return Parent.ParentDepthCheck(datablock, depth);
        }
        return -1;
    }

    public bool IsChildOf(Datablock datablock)
    {
        return GetDepthOfParent(datablock) != -1;
    }


    /// <summary>
    ///     Called when level is first loaded. Assigns the fields of the datablock with their
    ///     proper value for fast access.
    /// </summary>
    public void ProcessChildren()
    {
        IEnumerable<Datablock> children = GetChildren();
        foreach (Datablock datablock in children)
        {
            datablock.ProcessChildren();
        }

        AssignFieldsFromParent();
    }

    /// <summary>
    ///     Get all the children of this datablock
    /// </summary>
    /// <returns>List of datablock children</returns>
    public IEnumerable<Datablock> GetChildren()
    {
        IEnumerable<Datablock> datablocks = DatablockManager.Instance.GetDatablocks<Datablock>().Where(d => d.Parent == this);
        return datablocks;
    }

    /// <summary>
    ///     Clear all the override flags, setting the values of all the fields to use the parent
    /// </summary>
    public void ClearParentOverrides()
    {
        assetPaths.Clear();
        overrideParent.Clear();
    }

    [Serializable]
    public class StringBoolDictionary : SerializableDictionary<string, bool>
    {
    }

    [Serializable]
    public class StringStringDictionary : SerializableDictionary<string, string>
    {
    }

    /// <summary>
    /// Returns the datablock that sets the value of this field. If the field is set to override or has no parent, it will return itsself
    /// </summary>
    /// <param name="field">Field to check for which parent defines it</param>
    /// <returns>Datablock that defined the field's value</returns>
    public Datablock DefiningParent(FieldInfo field)
    {
        if (DoesOverridesParent(field))
            return this;

        return Parent.DefiningParent(field);
    }
}