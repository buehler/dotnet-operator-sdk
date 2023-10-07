﻿using System.Reflection;

namespace KubeOps.Transpiler;

internal static class Utilities
{
    public static CustomAttributeData? GetCustomAttributeData<TAttribute>(this Type type)
        where TAttribute : Attribute
        => CustomAttributeData
            .GetCustomAttributes(type)
            .FirstOrDefault(a => a.AttributeType.Name == typeof(TAttribute).Name);

    public static IEnumerable<CustomAttributeData> GetCustomAttributesData<TAttribute>(this Type type)
        where TAttribute : Attribute
        => CustomAttributeData
            .GetCustomAttributes(type)
            .Where(a => a.AttributeType.Name == typeof(TAttribute).Name);

    public static T? GetCustomAttributeNamedArg<T>(this CustomAttributeData attr, string name) =>
        attr.NamedArguments?.FirstOrDefault(a => a.MemberName == name).TypedValue.Value is T value
            ? value
            : default;
}
