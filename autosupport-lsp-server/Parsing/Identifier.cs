using autosupport_lsp_server.Shared;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace autosupport_lsp_server.Parsing
{
    internal class Identifier
    {
        public string Name { get; set; } = "";
        public string Documentation { get; set; } = "";
        public string Environment { get; set; } = "";
        public CompletionItemKind Kind { get; set; } = CompletionItemKind.Variable;

        public Either<string, IdentifierType> Type { get; set; } = IdentifierType.Any;

        public Position? Definition { get; set; }
        public Position? Implementation { get; set; }
        public List<Position> References { get; set; } = new List<Position>();

        internal enum IdentifierType
        {
            Any
        }

        public static string GlobalEnvironment => "global|";

        public static HashSet<Identifier> CreateIdentifierSet()
        {
            return new HashSet<Identifier>(new IdentifierComparer());
        }
        public static HashSet<Identifier> CreateIdentifierSet(IEnumerable<Identifier> baseSet)
        {
            return new HashSet<Identifier>(baseSet, new IdentifierComparer());
        }

        internal class IdentifierComparer : EqualityComparer<Identifier>
        {
            public override bool Equals([AllowNull] Identifier x, [AllowNull] Identifier y)
            {
                if (x == null)
                    return y == null;
                else if (y == null)
                    return false;

                return x.Name == y.Name && x.Environment == y.Environment && x.Type == y.Type;
            }

            public override int GetHashCode([DisallowNull] Identifier obj)
            {
                unchecked
                {
                    int hash = (int)2166136261;
                    hash = (hash * 16777619) ^ obj.Name.GetHashCode();
                    hash = (hash * 16777619) ^ obj.Environment.GetHashCode();
                    hash = (hash * 16777619) ^ obj.Type.GetHashCode();
                    return hash;
                }
            }
        }

    }
}
