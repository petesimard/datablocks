using System;

/// <summary>
///     Don't display the field if the referenced field is null
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class DatablockNullAttribute : Attribute
{
    public string fieldName;

    /// <summary>
    ///     Don't display the field if the referenced field is null
    /// </summary>
    /// <param name="fieldName">Referenced field. If null, this field will be hidden</param>
    public DatablockNullAttribute(string fieldName)
    {
        this.fieldName = fieldName;
    }
}