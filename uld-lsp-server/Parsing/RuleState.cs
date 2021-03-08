using uld.definition;
using uld.definition.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static uld.server.Parsing.RuleStateValueStoreKey;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace uld.server.Parsing
{
    internal class RuleState
    {
        private readonly Stack<Tuple<IRule, int>> ruleStates;
        private readonly Dictionary<string, Position> markers;
        private readonly List<Error> errors;

        private IRule? CurrentRule
            => ruleStates.TryPeek(out var tuple) ? tuple.Item1 : null;
        public ISymbol? CurrentSymbol {
            get {
                if (!ruleStates.TryPeek(out var tuple))
                    return null;

                var (rule, idx) = tuple;

                return idx >= rule.Symbols.Count
                    ? null
                    : rule.Symbols[idx];
            }
        }

        public bool IsFinished {
            get {
                return ruleStates.Count == 0;
            }
        }

        public IReadOnlyList<Error> Errors => errors;
        public IReadOnlyDictionary<string, Position> Markers => markers;
        public RuleStateValueStore ValueStore;

        public ISet<Identifier> Identifiers { get; private set; }

        public Range[]? ExplicitFoldingRanges
            => ValueStore.TryGetValue(RuleStateValueStoreKey.FoldingRanges, out var ranges)
                ? ranges.ToArray()
                : null;

        public RuleState(IRule rule, int position = 0)
        {
            if (position < 0)
                throw new ArgumentException("Position in rule may not be negative");
            if (rule == null)
                throw new ArgumentException("Rule may not be null");

            errors = new List<Error>();
            ruleStates = new Stack<Tuple<IRule, int>>();
            ruleStates.Push(new Tuple<IRule, int>(rule, position));
            markers = new Dictionary<string, Position>();
            Identifiers = Identifier.CreateIdentifierSet();
            ValueStore = new RuleStateValueStore();
        }

        private RuleState()
        {
            ruleStates = new Stack<Tuple<IRule, int>>(0);
            errors = new List<Error>();
            markers = new Dictionary<string, Position>();
            Identifiers = Identifier.CreateIdentifierSet();
            ValueStore = new RuleStateValueStore();
        }

        private RuleState(RuleState ruleState)
        {
            if (ruleState == null)
                throw new ArgumentException(nameof(ruleState) + " may not be null");

            ruleStates = ruleState.ruleStates.Clone();
            errors = new List<Error>(ruleState.errors);
            markers = new Dictionary<string, Position>(ruleState.markers);
            Identifiers = Identifier.CreateIdentifierSet(ruleState.Identifiers);
            ValueStore = new RuleStateValueStore(ruleState.ValueStore);
        }

        public IRuleStateBuilder Clone() => new RuleStateBuilder(this);

        public override string? ToString()
        {
            if (IsFinished)
                return $"finished";
            else
                return $"{CurrentSymbol?.ToString() ?? "<no symbol>"} in rule {CurrentRule?.ToString() ?? "<no rule>"}";
        }

        public override bool Equals(object? obj)
        {
            return obj is RuleState state &&
                   EqualityComparer<Stack<Tuple<IRule, int>>>.Default.Equals(ruleStates, state.ruleStates) &&
                   EqualityComparer<Dictionary<string, Position>>.Default.Equals(markers, state.markers) &&
                   EqualityComparer<RuleStateValueStore>.Default.Equals(ValueStore, state.ValueStore) &&
                   EqualityComparer<ISet<Identifier>>.Default.Equals(Identifiers, state.Identifiers);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ruleStates, markers, ValueStore, Identifiers);
        }

        internal interface IRuleStateBuilder
        {
            IRuleStateBuilder WithNextSymbol();
            IRuleStateBuilder WithNewRule(IRule rule);
            IRuleStateBuilder WithMarker(string markerName, Position position);
            IRuleStateBuilder WithoutMarker(string markerName);
            IRuleStateBuilder WithUpdatedValue<V>(RuleStateValueStoreKey<V> key, V value) where V : class;
            IRuleStateBuilder WithValue<V>(RuleStateValueStoreKey<V> key, V value) where V : class;
            IRuleStateBuilder WithValue(RuleStateValueStoreKey<NoValue> key);
            IRuleStateBuilder WithoutValue(IRuleStateValueStoreKey key);
            IRuleStateBuilder WithAdditionalErrors(IEnumerable<Error> errors);

            RuleState Build();
        }

        private class RuleStateBuilder : IRuleStateBuilder
        {
            private readonly RuleState ruleState;

            public RuleStateBuilder(RuleState ruleState)
            {
                this.ruleState = new RuleState(ruleState);
            }

            public IRuleStateBuilder WithNextSymbol()
            {
                if (ruleState.IsFinished)
                    return this;

                Tuple<IRule, int>? current;
                int newIndex;
                bool isEmpty;

                do
                {
                    isEmpty = !ruleState.ruleStates.TryPop(out current);
                    newIndex = current?.Item2 + 1 ?? 0;
                    // if new index is OOB do not push this state again, but
                    // instead move up a level
                } while (!isEmpty && newIndex >= current!.Item1.Symbols.Count);

                if (!isEmpty)
                    ruleState.ruleStates.Push(new Tuple<IRule, int>(current!.Item1, newIndex));

                return this;
            }

            public RuleState Build()
            {
                return ruleState;
            }

            public IRuleStateBuilder WithNewRule(IRule rule)
            {
                if (!ruleState.IsFinished)
                    ruleState.ruleStates.Push(new Tuple<IRule, int>(rule, 0));

                return this;
            }

            public IRuleStateBuilder WithMarker(string markerName, Position position)
            {
                if (!ruleState.IsFinished)
                    ruleState.markers.Add(markerName, new Position(position.Line, position.Character));

                return this;
            }

            public IRuleStateBuilder WithoutMarker(string markerName)
            {
                if (!ruleState.IsFinished)
                    ruleState.markers.Remove(markerName);

                return this;
            }

            public IRuleStateBuilder WithUpdatedValue<V>(RuleStateValueStoreKey<V> key, V value) where V : class
            {
                if (!ruleState.IsFinished)
                    ruleState.ValueStore.Update(key, value);

                return this;
            }

            public IRuleStateBuilder WithValue<T>(RuleStateValueStoreKey<T> key, T value) where T : class
            {
                if (!ruleState.IsFinished)
                    ruleState.ValueStore.Add(key, value);

                return this;
            }

            public IRuleStateBuilder WithValue(RuleStateValueStoreKey<NoValue> key)
            {
                if (!ruleState.IsFinished)
                    ruleState.ValueStore.Add(key);

                return this;
            }

            public IRuleStateBuilder WithoutValue(IRuleStateValueStoreKey key)
            {
                if (!ruleState.IsFinished)
                    ruleState.ValueStore.Remove(key);

                return this;
            }

            public IRuleStateBuilder WithAdditionalErrors(IEnumerable<Error> errors)
            {
                ruleState.errors.AddRange(errors);

                return this;
            }
        }
    }
}
