using System.Reflection;

using KubeOps.Abstractions.Entities;

namespace KubeOps.Cli.Transpilation;

internal record ValidatedEntity(TypeInfo Validator, EntityMetadata Metadata)
{
    private bool HasCreate => Validator.DeclaredMembers.Any(m => m.Name.StartsWith("Create"));

    private bool HasUpdate => Validator.DeclaredMembers.Any(m => m.Name.StartsWith("Update"));

    private bool HasDelete => Validator.DeclaredMembers.Any(m => m.Name.StartsWith("Delete"));

    public string ValidatorPath => $"/validate/{Validator.BaseType!.GenericTypeArguments[0].Name.ToLowerInvariant()}";

    public string[] GetOperations() =>
        new[] { HasCreate ? "CREATE" : null, HasUpdate ? "UPDATE" : null, HasDelete ? "DELETE" : null, }
            .Where(o => o is not null).ToArray()!;
}
