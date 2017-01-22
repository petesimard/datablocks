using System;

/// <summary>
///     Sets the category for creating a new datablock
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DatablockCategoryAttribute : Attribute
{
    public string category;

    public DatablockCategoryAttribute(string category)
    {
        this.category = category;
    }
}