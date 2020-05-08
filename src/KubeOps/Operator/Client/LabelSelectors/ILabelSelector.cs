namespace KubeOps.Operator.Client.LabelSelectors
{
    public interface ILabelSelector
    {
        string ToExpression();
    }
}
