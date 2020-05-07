namespace Dos.Operator.Client.LabelSelectors
{
    public class ExistsSelector : ILabelSelector
    {
        public ExistsSelector(string label)
        {
            Label = label;
        }

        public string Label { get; set; }

        public string ToExpression() => $"{Label}";
    }
}
