using System;
using System.Collections.Generic;
using System.Linq;

namespace autosupport_lsp_server.Parsing
{
    public class IdentifierType
    {
        public ISet<string> RawTypes;

        public bool IsCompatibleWithAllOf(IEnumerable<string>? types) =>
            types == null
            || RawTypes.Count == 0
            || types.All(RawTypes.Contains);

        public IdentifierType()
        {
            RawTypes = new HashSet<string>();
        }

        public IdentifierType(string? type)
        {
            RawTypes = new HashSet<string>(1);

            if (type != null)
                RawTypes.Add(type);
        }

        public IdentifierType(IEnumerable<string> types)
        {
            RawTypes = new HashSet<string>(types);
        }

        public IdentifierType(IdentifierType types) : this(types.RawTypes) { }

        public override bool Equals(object? obj)
            => obj is IdentifierType type && RawTypes.SetEquals(type.RawTypes);

        public override int GetHashCode() => HashCode.Combine(RawTypes.Count());
        public override string? ToString() => RawTypes.JoinToString();

        public static implicit operator IdentifierType(string type) => new IdentifierType(type);
    }
}
