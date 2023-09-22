using k8s.Models;

namespace Operator.Entities;

public class V1TestEntitySpec
{
    public string Spec { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public IntstrIntOrString StringOrInteger { get; set; } = 42;
}
