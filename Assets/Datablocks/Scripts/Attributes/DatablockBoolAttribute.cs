using System;

/// <summary>
///     Display field if the referenced bool field is true
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class DatablockBoolAttribute : Attribute
{
    public string fieldName;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="fieldName">Field to check for true value</param>
    public DatablockBoolAttribute(string fieldName)
    {
        this.fieldName = fieldName;
    }
}