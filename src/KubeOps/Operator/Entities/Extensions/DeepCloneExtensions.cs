using System.Reflection;
using KubeOps.Operator.Comparing;
using ReferenceEqualityComparer = KubeOps.Operator.Comparing.ReferenceEqualityComparer;

namespace KubeOps.Operator.Entities.Extensions;

internal static class DeepCloneExtensions
{
    /// <summary>
    /// The Clone Method that will be recursively used for the deep clone.
    /// </summary>
    private static readonly MethodInfo CloneMethod = typeof(object).GetMethod(
                                                         "MemberwiseClone",
                                                         BindingFlags.NonPublic | BindingFlags.Instance) ??
                                                     throw new ArgumentNullException();

    /// <summary>
    /// Returns a Deep Clone / Deep Copy of an object of type T using a recursive
    /// call to the CloneMethod specified above.
    /// </summary>
    /// <param name="obj">The object to deeply clone.</param>
    /// <typeparam name="TEntity">Object type.</typeparam>
    /// <returns>A deep clone of a given object.</returns>
    internal static TEntity? DeepClone<TEntity>(this TEntity? obj)
    {
        if (obj == null)
        {
            return default;
        }

        return (TEntity)(DeepClone_Internal(
                             obj,
                             new Dictionary<object, object>(new ReferenceEqualityComparer())) ??
                         throw new InvalidCastException());
    }

    /// <summary>
    /// Returns TRUE if the type is a primitive one, FALSE otherwise.
    /// </summary>
    private static bool IsPrimitive(this Type type)
    {
        if (type == typeof(string))
        {
            return true;
        }

        return type.IsValueType && type.IsPrimitive;
    }

    private static object? DeepClone_Internal(object? obj, IDictionary<object, object> visited)
    {
        if (obj == null)
        {
            return null;
        }

        var typeToReflect = obj.GetType();
        if (IsPrimitive(typeToReflect))
        {
            return obj;
        }

        if (visited.ContainsKey(obj))
        {
            return visited[obj];
        }

        if (typeof(Delegate).IsAssignableFrom(typeToReflect))
        {
            return null;
        }

        var cloneObject = CloneMethod.Invoke(obj, null);
        if (typeToReflect.IsArray)
        {
            var arrayType = typeToReflect.GetElementType() ?? throw new ArgumentNullException();
            if (!IsPrimitive(arrayType))
            {
                var clonedArray = (Array)cloneObject!;
                clonedArray.ForEach(
                    (array, indices) => array.SetValue(
                        DeepClone_Internal(clonedArray.GetValue(indices), visited),
                        indices));
            }
        }

        visited.Add(obj, cloneObject!);
        CopyFields(obj, visited, cloneObject!, typeToReflect);
        RecursiveCopyBaseTypePrivateFields(obj, visited, cloneObject!, typeToReflect);
        return cloneObject;
    }

    private static void RecursiveCopyBaseTypePrivateFields(
        object originalObject,
        IDictionary<object, object> visited,
        object cloneObject,
        Type typeToReflect)
    {
        if (typeToReflect.BaseType == null)
        {
            return;
        }

        RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
        CopyFields(
            originalObject,
            visited,
            cloneObject,
            typeToReflect.BaseType,
            BindingFlags.Instance | BindingFlags.NonPublic,
            info => info.IsPrivate);
    }

    private static void CopyFields(
        object originalObject,
        IDictionary<object, object> visited,
        object cloneObject,
        IReflect typeToReflect,
        BindingFlags bindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy,
        Func<FieldInfo, bool>? filter = null)
    {
        foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
        {
            if (filter != null && !filter(fieldInfo))
            {
                continue;
            }

            if (IsPrimitive(fieldInfo.FieldType))
            {
                continue;
            }

            var originalFieldValue = fieldInfo.GetValue(originalObject);
            var clonedFieldValue = DeepClone_Internal(originalFieldValue, visited);
            fieldInfo.SetValue(cloneObject, clonedFieldValue);
        }
    }
}
