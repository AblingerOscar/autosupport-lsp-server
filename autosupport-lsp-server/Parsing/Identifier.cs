using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace autosupport_lsp_server.Parsing
{
    public class Identifier
    {
        public string Name { get; set; } = "";
        public string Documentation { get; set; } = "";
        public string Environment { get; set; } = "";
        public CompletionItemKind? Kind { get; set; } = null;

        /// <summary>
        /// If true the identifier can be used before it is declared.
        /// In C# an example would be class methods or classes themselves
        /// </summary>
        public bool AllowsUseBeforeDeclared { get; set; } = false;
        public IdentifierType Types { get; set; } = new IdentifierType();

        public IReferenceWithEnclosingRange? Declaration { get; set; }
        public IReferenceWithEnclosingRange? Implementation { get; set; }
        public List<IReference> References { get; set; } = new List<IReference>();

        public Identifier() { }

        public Identifier(Identifier other)
        {
            Name = other.Name;
            Documentation = other.Documentation;
            Environment = other.Environment;
            Kind = other.Kind;
            AllowsUseBeforeDeclared = other.AllowsUseBeforeDeclared;
            Types = new IdentifierType(other.Types);
            if (other.Declaration != null)
                Declaration = new ReferenceWithEnclosingRange(other.Declaration);
            if (other.Implementation != null)
                Implementation = new ReferenceWithEnclosingRange(other.Implementation);
            References = new List<IReference>(other.References);
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

        internal static Identifier[] MergeIdentifiers(params IEnumerable<Identifier>[] identifiers)
        {
            var mergedIdentifiers = CreateIdentifierSet();

            foreach (var identifiersInDocument in identifiers)
            {
                foreach (var identifier in identifiersInDocument)
                {
                    if (mergedIdentifiers.TryGetValue(identifier, out var existingIdentifier))
                    {
                        if (existingIdentifier.Declaration == null)
                            existingIdentifier.Declaration = identifier.Declaration;

                        if (existingIdentifier.References == null)
                            existingIdentifier.References = new List<IReference>(identifier.References);
                        else
                            existingIdentifier.References.AddRange(identifier.References);

                        if (existingIdentifier.Documentation.Trim() == "")
                            existingIdentifier.Documentation = identifier.Documentation;

                        if (existingIdentifier.Implementation == null)
                            existingIdentifier.Implementation = identifier.Implementation;
                    }
                    else
                    {
                        mergedIdentifiers.Add(new Identifier(identifier));
                    }
                }
            }

            return mergedIdentifiers.ToArray();
        }

        internal class IdentifierComparer : EqualityComparer<Identifier>
        {
            public override bool Equals([AllowNull] Identifier x, [AllowNull] Identifier y)
            {
                if (x == null)
                    return y == null;
                else if (y == null)
                    return false;

                return x.Name == y.Name && x.Environment == y.Environment && x.Types.Equals(y.Types) && x.Kind == y.Kind;
            }

            public override int GetHashCode([DisallowNull] Identifier obj)
                => HashCode.Combine(obj.Name, obj.Environment, obj.Types, obj.Kind);
        }

    }
}
