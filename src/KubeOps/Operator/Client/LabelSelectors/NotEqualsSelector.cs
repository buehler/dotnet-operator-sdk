using System.Collections.Generic;

namespace Dos.Operator.Client.LabelSelectors
{
    public class NotEqualsSelector : ILabelSelector
    {
        public NotEqualsSelector(string label, params string[] values)
        {
            Label = label;
            Values = values;
        }

        public string Label { get; set; }

        public IEnumerable<string> Values { get; set; }

        public string ToExpression() => $"{Label} notin ({string.Join(',', Values)})";
    }
}
