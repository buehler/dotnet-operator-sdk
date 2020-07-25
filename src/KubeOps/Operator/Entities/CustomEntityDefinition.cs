using System.Collections.Generic;

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

        public readonly IList<string>? ShortNames;

        public CustomEntityDefinition(
            string kind,
            string listKind,
            string @group,
            string version,
            string singular,
            string plural,
            EntityScope scope,
            IList<string>? shortNames = null)
        {
            Kind = kind;
            ListKind = listKind;
            Group = @group;
            Version = version;
            Singular = singular;
            Plural = plural;
            Scope = scope;
            ShortNames = shortNames;
        }
    }
}
