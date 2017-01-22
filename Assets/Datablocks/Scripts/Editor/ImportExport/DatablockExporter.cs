using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Collections;
using Object = UnityEngine.Object;

namespace Datablocks
{

    /// <summary>
    /// Base datablock exporter
    /// </summary>
    public abstract class DatablockExporter : EditorWindow
    {
        protected bool exportFullValues;

        private void OnProjectChange()
        {
            DatablockManager.Instance.RefreshAssets();
        }


        protected virtual void OnEnable()
        {
            DatablockManager.Instance.RefreshAssets();
        }

        protected string GetFieldValue(Datablock datablock, FieldInfo field)
        {
            // If the field uses its parent's value, return an empty string
            if (!datablock.DoesOverridesParent(field) && !exportFullValues)
                return "";

            if (field.FieldType == typeof (int))
            {
                return ((int) datablock.GetFieldValue(field)).ToString();
            }
            else if (field.FieldType == typeof (float))
            {
                return ((float) field.GetValue(datablock)).ToString();
            }
            else if (field.FieldType == typeof (bool))
            {
                return ((bool) field.GetValue(datablock)) ? "True" : "False";
            }
            else if (field.FieldType == typeof (double))
            {
                return ((double) field.GetValue(datablock)).ToString();
            }
            else if (field.FieldType == typeof (string))
            {
                var str = ((string) field.GetValue(datablock));
                if (string.IsNullOrEmpty(str))
                    return "(null)";
                return str;
            }
            else if (field.FieldType == typeof (Color))
            {
                var val = (Color) field.GetValue(datablock);
                return val.r.ToString() + ',' + val.g.ToString() + ',' + val.b.ToString() + ',' + val.a.ToString();
            }
            else if (field.FieldType == typeof (Vector2))
            {
                var val = (Vector2) field.GetValue(datablock);
                return val.x.ToString() + ',' + val.y.ToString();
            }
            else if (field.FieldType == typeof (Vector3))
            {
                var val = (Vector3) field.GetValue(datablock);
                return val.x.ToString() + ',' + val.y.ToString() + ',' + val.z.ToString();
            }
            else if (field.FieldType == typeof (Vector4))
            {
                var val = (Vector4) field.GetValue(datablock);
                return val.x.ToString() + ',' + val.y.ToString() + ',' + val.z.ToString() + ',' + val.w.ToString();
            }
            else if (field.FieldType.IsSubclassOf(typeof (Object)))
            {
                var obj = ((Object) field.GetValue(datablock));

                if (!obj)
                    return "";

                return obj.name;
            }
            else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof (DatablockRef<>))
            {
                return ((IDatablockRef) field.GetValue(datablock)).GetPath();
            }
            else if (field.FieldType.IsEnum)
            {
                return ((Enum) field.GetValue(datablock)).ToString();
            }
            return "";
        }

        protected void ProcessChildren(Datablock datablock)
        {
            ProcessDatablock(datablock);

            var children = datablock.GetChildren();

            foreach (var child in children)
            {
                ProcessChildren(child);
            }
        }

        protected abstract void ProcessDatablock(Datablock datablock);
    }
}