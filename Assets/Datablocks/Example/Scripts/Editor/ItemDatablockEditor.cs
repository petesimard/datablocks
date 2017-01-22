using System.Reflection;
using Datablocks;
using UnityEditor;

/// <summary>
///     Custom editor for Item Datablocks
/// </summary>
[CustomEditor(typeof (ItemDatablock), true)]
public class ItemDatablockEditor : DatablockEditor
{
    private ItemDatablock itemDatablock;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var sellPrice = itemDatablock.GetFieldValue<int>("sellPrice");
        EditorGUILayout.HelpBox("Sell Price: " + new Money(sellPrice), MessageType.Info);
    }

    public override void OnEnable()
    {
        base.OnEnable();

        itemDatablock = (ItemDatablock) target;
    }

    /// <summary>
    ///     Additional checks for slot & item type
    /// </summary>
    /// <param name="memberData">Member info</param>
    /// <returns>True if the field should be shown</returns>
    protected override bool ShouldShowField(MemberInfo memberData)
    {
        if (!base.ShouldShowField(memberData))
            return false;

        var limitSlotsAttr = memberData.GetAttribute<ItemDatablockEquipmentSlotAttribute>();
        if (limitSlotsAttr != null)
        {
            FieldInfo slotField = itemDatablock.GetType().GetField("equipmentSlot");
            var datablockSlot = itemDatablock.GetFieldValue<ItemDatablock.Slot>(slotField);
            foreach (ItemDatablock.Slot slot in limitSlotsAttr.slots)
            {
                if (datablockSlot == slot)
                    return true;
            }

            return false;
        }

        var limitTypeAttr = memberData.GetAttribute<ItemDatablockItemTypeAttribute>();
        if (limitTypeAttr != null)
        {
            FieldInfo itemTypeField = itemDatablock.GetType().GetField("itemType");
            var datablockType = itemDatablock.GetFieldValue<ItemDatablock.ItemType>(itemTypeField);
            foreach (ItemDatablock.ItemType type in limitTypeAttr.types)
            {
                if (datablockType == type)
                    return true;
            }

            return false;
        }

        return true;
    }
}