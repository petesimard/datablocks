using System;

[AttributeUsage(AttributeTargets.Field)]
public class DatablockFoldoutAttribute : Attribute
{
    public string groupName;

    public DatablockFoldoutAttribute(string groupName)
    {
        this.groupName = groupName;
    }
}
