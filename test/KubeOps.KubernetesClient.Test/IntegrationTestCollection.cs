namespace KubeOps.KubernetesClient.Test;

[CollectionDefinition(Name, DisableParallelization = true)]
public class IntegrationTestCollection
{
    public const string Name = "Integration Tests";
}

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase;
