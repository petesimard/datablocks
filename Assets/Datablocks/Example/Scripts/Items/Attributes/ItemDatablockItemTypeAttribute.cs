using System;

/// <summary>
///     Limit the display of a field vased on item type
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ItemDatablockItemTypeAttribute : Attribute
{
    public ItemDatablock.ItemType[] types;

    public ItemDatablockItemTypeAttribute(ItemDatablock.ItemType type)
    {
        types = new[] {type};
    }

    public ItemDatablockItemTypeAttribute(ItemDatablock.ItemType[] types)
    {
        this.types = types;
    }
}