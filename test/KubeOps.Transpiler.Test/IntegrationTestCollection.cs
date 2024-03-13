using System.Reflection;

namespace KubeOps.Transpiler.Test;

[CollectionDefinition(Name, DisableParallelization = true)]
public class TranspilerTestCollection : ICollectionFixture<MlcProvider>
{
    public const string Name = "Transpiler Tests";
}

[Collection(TranspilerTestCollection.Name)]
public abstract class TranspilerTestBase(MlcProvider provider)
{
    protected readonly MetadataLoadContext _mlc = provider.Mlc;
}
