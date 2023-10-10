using System.Collections.ObjectModel;
using System.Reflection;

namespace KubeOps.Transpiler;

public static class Utilities
{
    public static CustomAttributeData? GetCustomAttributeData<TAttribute>(this Type type)
        where TAttribute : Attribute
        => CustomAttributeData
            .GetCustomAttributes(type)
            .FirstOrDefault(a => a.AttributeType.Name == typeof(TAttribute).Name);

    public static CustomAttributeData? GetCustomAttributeData<TAttribute>(this PropertyInfo prop)
        where TAttribute : Attribute
        => CustomAttributeData
            .GetCustomAttributes(prop)
            .FirstOrDefault(a => a.AttributeType.Name == typeof(TAttribute).Name);

    public static IEnumerable<CustomAttributeData> GetCustomAttributesData<TAttribute>(this Type type)
        where TAttribute : Attribute
        => CustomAttributeData
            .GetCustomAttributes(type)
            .Where(a => a.AttributeType.Name == typeof(TAttribute).Name);

    public static T? GetCustomAttributeNamedArg<T>(this CustomAttributeData attr, MetadataLoadContext ctx, string name) =>
        attr.NamedArguments.FirstOrDefault(a => a.MemberName == name).TypedValue.ArgumentType == ctx.GetContextType<T>()
            ? (T)attr.NamedArguments.FirstOrDefault(a => a.MemberName == name).TypedValue.Value!
            : default;

    public static IList<string> GetCustomAttributeNamedStringArrayArg(this CustomAttributeData attr, string name) =>
        attr.NamedArguments.FirstOrDefault(a => a.MemberName == name).TypedValue.Value is
            ReadOnlyCollection<CustomAttributeTypedArgument> value
            ? value.Select(v => v.Value?.ToString() ?? string.Empty).ToList()
            : new List<string>();

    public static T? GetCustomAttributeCtorArg<T>(this CustomAttributeData attr, MetadataLoadContext ctx, int index) =>
        attr.ConstructorArguments.Count >= index + 1 &&
        attr.ConstructorArguments[index].ArgumentType == ctx.GetContextType<T>()
            ? (T)attr.ConstructorArguments[index].Value!
            : default;

    public static Type GetContextType<T>(this MetadataLoadContext context)
        => context.GetContextType(typeof(T));

    public static Type GetContextType(this MetadataLoadContext context, Type type)
    {
        foreach (var assembly in context.GetAssemblies())
        {
            if (assembly.GetType(type.FullName!) is { } t)
            {
                return t;
            }
        }

        var newAssembly = context.LoadFromAssemblyPath(type.Assembly.Location);
        return newAssembly.GetType(type.FullName!)!;
    }
}
