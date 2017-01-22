using System;

/// <summary>
///     Limit the display of the field based on the enum value of another field
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class DatablockEnumAttribute : Attribute
{
    public Enum enumValue;
    public string fieldName;
    public bool reverse;

    /// <summary>
    ///     Limit the display of the field based on the enum value of another field
    /// </summary>
    /// <param name="field">Field to check for enum value</param>
    /// <param name="value">Enum value other field must have</param>
    /// <param name="reverse">If reversed, will only display if the other field's value does not match</param>
    public DatablockEnumAttribute(string field, object value, bool reverse = false)
    {
        fieldName = field;
        enumValue = (Enum)value;
        this.reverse = reverse;
    }
}