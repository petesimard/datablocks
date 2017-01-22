using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Dictinary that can be serialized by Unity
/// </summary>
/// <typeparam name="TKey">Key type</typeparam>
/// <typeparam name="TValue">Value type</typeparam>
[Serializable]
public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
    [SerializeField] private List<TKey> keys = new List<TKey>();

    [SerializeField] private List<TValue> values = new List<TValue>();

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return dictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable) dictionary).GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> kv)
    {
        dictionary.Add(kv.Key, kv.Value);
    }

    public void Clear()
    {
        dictionary.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return dictionary.ContainsKey(item.Key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        Copy(this, array, arrayIndex);
    }

    private static void Copy<T>(ICollection<T> source, T[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException("array");

        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException("arrayIndex");

        if ((array.Length - arrayIndex) < source.Count)
            throw new ArgumentException("Destination array is not large enough. Check array.Length and arrayIndex.");

        foreach (T item in source)
            array[arrayIndex++] = item;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return dictionary.Remove(item.Key);
    }

    public int Count
    {
        get { return dictionary.Count; }
    }

    public bool IsReadOnly
    {
        get { return false; }
    }

    public void Add(TKey key, TValue value)
    {
        dictionary.Add(key, value);
    }

    public bool ContainsKey(TKey key)
    {
        return dictionary.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        return dictionary.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return dictionary.TryGetValue(key, out value);
    }

    public TValue this[TKey key]
    {
        get { return dictionary[key]; }
        set { dictionary[key] = value; }
    }

    public ICollection<TKey> Keys
    {
        get { return dictionary.Keys; }
    }

    public ICollection<TValue> Values
    {
        get { return dictionary.Values; }
    }

    public void OnBeforeSerialize()
    {
        lock (this)
        {
            keys = new List<TKey>(Keys);
            values = new List<TValue>(Values);
        }
    }

    // load dictionary from lists
    public void OnAfterDeserialize()
    {
        Clear();

        if (keys.Count != values.Count)
            throw new Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));

        for (int i = 0; i < keys.Count; i++)
            Add(keys[i], values[i]);
    }

    public bool Contains(TValue item)
    {
        return dictionary.ContainsValue(item);
    }
}