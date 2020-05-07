using System;
using System.Collections.Generic;
using System.Reflection;
using Dos.Operator.Comparing;
using k8s;
using k8s.Models;

namespace Dos.Operator.Entities
{
    internal static class EntityExtensions
    {
        internal static CustomEntityDefinition CreateResourceDefinition(
            this IKubernetesObject<V1ObjectMeta> kubernetesEntity) =>
            CreateResourceDefinition(kubernetesEntity.GetType());

        internal static CustomEntityDefinition CreateResourceDefinition<TResource>()
            where TResource : IKubernetesObject<V1ObjectMeta> =>
            CreateResourceDefinition(typeof(TResource));

        internal static CustomEntityDefinition CreateResourceDefinition(Type resourceType)
        {
            var attribute = resourceType.GetCustomAttribute<KubernetesEntityAttribute>();
            if (attribute == null)
            {
                throw new ArgumentException($"The Type {resourceType} does not have the kubernetes entity attribute.");
            }

            var scopeAttribute = resourceType.GetCustomAttribute<EntityScopeAttribute>();
            var kind = string.IsNullOrWhiteSpace(attribute.Kind) ? resourceType.Name : attribute.Kind;

            return new CustomEntityDefinition(
                kind,
                $"{kind}List",
                attribute.Group,
                attribute.ApiVersion,
                kind.ToLower(),
                string.IsNullOrWhiteSpace(attribute.PluralName) ? $"{kind.ToLower()}s" : attribute.PluralName,
                scopeAttribute?.Scope ?? default);
        }

        /// <summary>
        /// The Clone Method that will be recursively used for the deep clone.
        /// </summary>
        private static readonly MethodInfo CloneMethod = typeof(object).GetMethod(
                                                             "MemberwiseClone",
                                                             BindingFlags.NonPublic | BindingFlags.Instance) ??
                                                         throw new ArgumentNullException();

        /// <summary>
        /// Returns TRUE if the type is a primitive one, FALSE otherwise.
        /// </summary>
        private static bool IsPrimitive(this Type type)
        {
            if (type == typeof(string)) return true;
            return (type.IsValueType & type.IsPrimitive);
        }

        /// <summary>
        /// Returns a Deep Clone / Deep Copy of an object of type T using a recursive call to the CloneMethod specified above.
        /// </summary>
        internal static TResource DeepClone<TResource>(this TResource obj)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            return (TResource) (DeepClone_Internal(
                                    obj,
                                    new Dictionary<object, object>(new ReferenceEqualityComparer())) ??
                                throw new InvalidCastException());
        }

        private static object? DeepClone_Internal(object? obj, IDictionary<object, object> visited)
        {
            if (obj == null) return null;
            var typeToReflect = obj.GetType();
            if (IsPrimitive(typeToReflect)) return obj;
            if (visited.ContainsKey(obj)) return visited[obj];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            var cloneObject = CloneMethod.Invoke(obj, null);
            if (typeToReflect.IsArray)
            {
                var arrayType = typeToReflect.GetElementType() ?? throw new ArgumentNullException();
                if (IsPrimitive(arrayType) == false)
                {
                    var clonedArray = (Array) cloneObject!;
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
            if (typeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
                CopyFields(
                    originalObject,
                    visited,
                    cloneObject,
                    typeToReflect.BaseType,
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    info => info.IsPrivate);
            }
        }

        private static void CopyFields(
            object originalObject,
            IDictionary<object, object> visited,
            object cloneObject,
            Type typeToReflect,
            BindingFlags bindingFlags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy,
            Func<FieldInfo, bool>? filter = null)
        {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                var clonedFieldValue = DeepClone_Internal(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }
    }
}
