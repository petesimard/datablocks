using System;
using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
///     Utility functions
/// </summary>
public static class Util
{
    /// <summary>
    ///     Get an attribute of a class member
    /// </summary>
    /// <typeparam name="TAttributeType">Attribute type</typeparam>
    /// <param name="memberInfo">Member Info</param>
    /// <returns>Attribute, or null if it doesn't exist on this member</returns>
    public static TAttributeType GetAttribute<TAttributeType>(this MemberInfo memberInfo) where TAttributeType : Attribute
    {
        return (TAttributeType) Attribute.GetCustomAttribute(memberInfo, typeof (TAttributeType));
    }


    /// <summary>
    ///     Conver camcel case to spaces
    /// </summary>
    /// <param name="str">String to convert</param>
    /// <returns>String with spaces</returns>
    public static string CamelToSpaces(string str)
    {
        return Regex.Replace(str, @"(?<a>(?<!^)((?:[A-Z][a-z])|(?:(?<!^[A-Z]+)[A-Z0-9]+(?:(?=[A-Z][a-z])|$))|(?:[0-9]+)))", @" ${a}");
    }
}