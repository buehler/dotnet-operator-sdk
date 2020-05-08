namespace KubeOps.Operator.Entities
{
    internal readonly struct CustomEntityDefinition
    {
        public readonly string Kind;

        public readonly string ListKind;

        public readonly string Group;

        public readonly string Version;

        public readonly string Singular;

        public readonly string Plural;

        public readonly EntityScope Scope;

        public CustomEntityDefinition(
            string kind,
            string listKind,
            string @group,
            string version,
            string singular,
            string plural,
            EntityScope scope)
        {
            Kind = kind;
            ListKind = listKind;
            Group = @group;
            Version = version;
            Singular = singular;
            Plural = plural;
            Scope = scope;
        }
    }
}
