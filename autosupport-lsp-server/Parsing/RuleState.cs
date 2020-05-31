using autosupport_lsp_server.Symbols;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;

namespace autosupport_lsp_server.Parsing
{
    internal class RuleState
    {
        private Stack<Tuple<IRule, int>> ruleStates;
        private Dictionary<string, Position> markers;

        public IRule CurrentRule => ruleStates.Peek().Item1;
        public ISymbol? CurrentSymbol => ruleStates.Peek().Item2 >= CurrentRule.Symbols.Count
            ? null
            : CurrentRule.Symbols[ruleStates.Peek().Item2];

        public bool IsFinished {
            get {
                return ruleStates.Count == 0;
            }
        }

        public IReadOnlyDictionary<string, Position> Markers => markers;

        public ISet<Identifier> Identifiers { get; private set; }

        public RuleState(IRule rule, int position = 0)
        {
            if (position < 0)
                throw new ArgumentException("Position in rule may not be negative");
            if (rule == null)
                throw new ArgumentException("Rule may not be null");

            ruleStates = new Stack<Tuple<IRule, int>>();
            ruleStates.Push(new Tuple<IRule, int>(rule, position));
            markers = new Dictionary<string, Position>();
            Identifiers = Identifier.CreateIdentifierSet();
        }

        /// <summary>
        /// Returns a RuleState that indicates a finished rule (aka no CurrentRule and IsFinished is true)
        /// The only valid next symbol for this state is EOF
        /// </summary>
        public static RuleState FinishedRuleState => new RuleState();

        private RuleState()
        {
            ruleStates = new Stack<Tuple<IRule, int>>(0);
            markers = new Dictionary<string, Position>();
            Identifiers = Identifier.CreateIdentifierSet();
        }

        private RuleState(RuleState ruleState)
        {
            if (ruleState == null)
                throw new ArgumentException(nameof(ruleState) + " may not be null");
            ruleStates = ruleState.ruleStates.Clone();
            markers = new Dictionary<string, Position>(ruleState.markers);
            Identifiers = Identifier.CreateIdentifierSet(ruleState.Identifiers);
        }

        public IConcreteRuleStateBuilder Clone() => new RuleStateBuilder(this);

        public override string? ToString()
        {
            return $"{CurrentSymbol?.ToString() ?? "<no symbol>"} in rule {CurrentRule.Name} with {ruleStates.Count} rulestates";
        }

        internal interface IRuleStateBuilder<T>
        {
            INullableRuleStateBuilder WithNextSymbol();
            T WithNewRule(IRule rule);
            T WithMarker(string markerName, Position position);
            T WithoutMarker(string markerName, Position? position = null);
        }

        internal interface IConcreteRuleStateBuilder : IRuleStateBuilder<IConcreteRuleStateBuilder>
        {
            RuleState Build();
        }

        internal interface INullableRuleStateBuilder : IRuleStateBuilder<INullableRuleStateBuilder>
        {
            RuleState? TryBuild();
        }

        private class RuleStateBuilder : IConcreteRuleStateBuilder, INullableRuleStateBuilder
        {
            private RuleState? ruleState;

            public RuleStateBuilder(RuleState ruleState)
            {
                this.ruleState = new RuleState(ruleState);
            }

            public INullableRuleStateBuilder WithNextSymbol()
            {
                if (ruleState == null)
                    return this;

                Tuple<IRule, int>? current;
                int newIndex;
                bool isEmpty;

                do
                {
                    isEmpty = !ruleState.ruleStates.TryPop(out current);
                    newIndex = current == null
                        ? 0
                        : current.Item2 + 1;
                    // if new index is OOB do not push this state again, but
                    // instead move up a level
                } while (!isEmpty && newIndex >= current!.Item1.Symbols.Count);

                if (isEmpty)
                    ruleState = null;
                else
                    ruleState.ruleStates.Push(new Tuple<IRule, int>(current!.Item1, newIndex));

                return this;
            }

            public RuleState Build()
            {
                if (ruleState == null)
                    throw new ArgumentException("When using Build() ruleState may never be null… use TryBuild instead");

                return ruleState;
            }

            public RuleState? TryBuild()
            {
                return ruleState;
            }

            RuleStateBuilder WithNewRule(IRule rule)
            {
                if (ruleState == null)
                    return this;

                ruleState.ruleStates.Push(new Tuple<IRule, int>(rule, 0));
                return this;
            }

            IConcreteRuleStateBuilder IRuleStateBuilder<IConcreteRuleStateBuilder>.WithNewRule(IRule rule) => WithNewRule(rule);
            INullableRuleStateBuilder IRuleStateBuilder<INullableRuleStateBuilder>.WithNewRule(IRule rule) => WithNewRule(rule);

            private RuleStateBuilder WithMarker(string markerName, Position position)
            {
                if (ruleState != null)
                    ruleState.markers.Add(markerName, position);

                return this;
            }

            IConcreteRuleStateBuilder IRuleStateBuilder<IConcreteRuleStateBuilder>.WithMarker(string markerName, Position position) => WithMarker(markerName, position);
            INullableRuleStateBuilder IRuleStateBuilder<INullableRuleStateBuilder>.WithMarker(string markerName, Position position) => WithMarker(markerName, position);

            private RuleStateBuilder WithoutMarker(string markerName, Position? position)
            {
                if (ruleState != null
                    && (position == null
                        || (ruleState.markers.TryGetValue(markerName, out var actualPosition) && actualPosition == position)))
                    ruleState.markers.Remove(markerName);

                return this;
            }

            IConcreteRuleStateBuilder IRuleStateBuilder<IConcreteRuleStateBuilder>.WithoutMarker(string markerName, Position? position) => WithoutMarker(markerName, position);
            INullableRuleStateBuilder IRuleStateBuilder<INullableRuleStateBuilder>.WithoutMarker(string markerName, Position? position) => WithoutMarker(markerName, position);
        }
    }
}
