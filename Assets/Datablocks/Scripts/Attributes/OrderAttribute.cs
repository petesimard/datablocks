using System;
using System.Reflection;

/// <summary>
///     Specify the order fields should be shown in the inspector
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class OrderAttribute : Attribute
{
    public int order;

    public OrderAttribute(int order)
    {
        this.order = order;
    }

    public static int GetMemberOrder(MemberInfo memberInfo)
    {
        var attr = memberInfo.GetAttribute<OrderAttribute>();
        if (attr != null)
        {
            return attr.order;
        }

        // return max val if no order specified
        return int.MaxValue;
    }
}