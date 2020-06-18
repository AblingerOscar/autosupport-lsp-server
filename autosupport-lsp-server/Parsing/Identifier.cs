using autosupport_lsp_server.LSP;
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
        public CompletionItemKind Kind { get; set; } = CompletionItemKind.Variable;

        public IdentifierType Types { get; set; } = new IdentifierType();

        public DeclarationReference? Declaration { get; set; }
        public Position? Implementation { get; set; }
        public List<Reference> References { get; set; } = new List<Reference>();

        public Identifier() { }

        public Identifier(Identifier other)
        {
            Name = other.Name;
            Documentation = other.Documentation;
            Environment = other.Environment;
            Kind = other.Kind;
            UseBeforeDeclare = other.UseBeforeDeclare;
            Types = new IdentifierType(other.Types);
            if (other.Declaration != null)
                Declaration = new DeclarationReference(other.Declaration);
            if (other.Implementation != null)
                Implementation = other.Implementation.Clone();
            References = new List<Reference>(other.References);
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
                            existingIdentifier.References = identifier.References;
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
            {
                unchecked
                {
                    int hash = (int)2166136261;
                    hash = (hash * 16777619) ^ obj.Name.GetHashCode();
                    hash = (hash * 16777619) ^ obj.Environment.GetHashCode();
                    hash = (hash * 16777619) ^ obj.Types.GetHashCode();
                    return hash;
                }
            }
        }

    }
}
