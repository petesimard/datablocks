using System;

/// <summary>
///     Limit display of a field based on the equipment slot
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ItemDatablockEquipmentSlotAttribute : Attribute
{
    public ItemDatablock.Slot[] slots;

    public ItemDatablockEquipmentSlotAttribute(ItemDatablock.Slot slot)
    {
        slots = new[] {slot};
    }

    public ItemDatablockEquipmentSlotAttribute(ItemDatablock.Slot[] slots)
    {
        this.slots = slots;
    }
}