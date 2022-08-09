using System.Reflection;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Webhooks;

namespace KubeOps.Operator.Builder;

internal class AssemblyScanner : IAssemblyScanner
{
    private readonly IOperatorBuilder _operatorBuilder;
    private readonly List<(Type Type, MethodInfo RegistrationMethod)> _registrationDefinitions;

    public AssemblyScanner(IOperatorBuilder operatorBuilder)
    {
        _operatorBuilder = operatorBuilder;

        var operatorBuilderMethods = typeof(OperatorBuilderExtensions).GetMethods(
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        _registrationDefinitions =
            new (Type Type, string MethodName)[]
                {
                    new()
                    {
                        Type = typeof(IKubernetesObject<V1ObjectMeta>),
                        MethodName = nameof(OperatorBuilderExtensions.AddEntity),
                    },
                    new()
                    {
                        Type = typeof(IResourceController<>),
                        MethodName = nameof(OperatorBuilderExtensions.AddController),
                    },
                    new()
                    {
                        Type = typeof(IResourceFinalizer<>),
                        MethodName = nameof(OperatorBuilderExtensions.AddFinalizer),
                    },
                    new()
                    {
                        Type = typeof(IValidationWebhook<>),
                        MethodName = nameof(OperatorBuilderExtensions.AddValidationWebhook),
                    },
                    new()
                    {
                        Type = typeof(IMutationWebhook<>),
                        MethodName = nameof(OperatorBuilderExtensions.AddMutationWebhook),
                    },
                }
                .Select<(Type Type, string MethodName), (Type Type, MethodInfo RegistrationMethod)>(
                    t => new()
                    {
                        Type = t.Type,
                        RegistrationMethod = operatorBuilderMethods.Single(
                            m => m.Name == t.MethodName && m.GetGenericArguments().Length == 1),
                    })
                .ToList();
    }

    public IAssemblyScanner AddAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => (t.Attributes & TypeAttributes.Abstract) == 0);

        var registrationMethods = _registrationDefinitions.Join(
                types,
                _ => 1,
                _ => 1,
                (registrationDefinition, type) =>
                    new { RegistrationDefinition = registrationDefinition, ComponentType = type })
            .Where(
                t => t.ComponentType.GetInterfaces()
                    .Any(
                        i => (i.IsConstructedGenericType &&
                              i.GetGenericTypeDefinition().IsEquivalentTo(t.RegistrationDefinition.Type)) ||
                             (t.RegistrationDefinition.Type.IsConstructedGenericType &&
                              i.IsEquivalentTo(t.RegistrationDefinition.Type))))
            .Select(
                t => t.RegistrationDefinition.RegistrationMethod.MakeGenericMethod(t.ComponentType));

        foreach (var method in registrationMethods)
        {
            method.Invoke(null, new object[] { _operatorBuilder });
        }

        return this;
    }
}
