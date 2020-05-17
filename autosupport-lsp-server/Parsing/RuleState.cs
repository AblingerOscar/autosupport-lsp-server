using autosupport_lsp_server.Symbols;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace autosupport_lsp_server.Parsing
{
    internal class RuleState
    {
        private Stack<Tuple<IRule, int>> ruleStates;

        public IRule CurrentRule => ruleStates.Peek().Item1;
        public ISymbol? CurrentSymbol => ruleStates.Peek().Item2 >= CurrentRule.Symbols.Count
            ? null
            : CurrentRule.Symbols[ruleStates.Peek().Item2];

        public bool IsFinished {
            get {
                return ruleStates.Count == 0;
            }
        }

        public RuleState(IRule rule, int position = 0)
        {
            if (position < 0)
                throw new ArgumentException("Position in rule may not be negative");
            if (rule == null)
                throw new ArgumentException("Rule may not be null");

            ruleStates = new Stack<Tuple<IRule, int>>();
            ruleStates.Push(new Tuple<IRule, int>(rule, position));
        }

        /// <summary>
        /// Returns a RuleState that indicates a finished rule (aka no CurrentRule and IsFinished is true)
        /// The only valid next symbol for this state is EOF
        /// </summary>
        public static RuleState FinishedRuleState => new RuleState();

        private RuleState()
        {
            ruleStates = new Stack<Tuple<IRule, int>>(0);

        }

        private RuleState(RuleState ruleState)
        {
            if (ruleState == null)
                throw new ArgumentException(nameof(ruleState) + " may not be null");
            ruleStates = ruleState.ruleStates.Clone();
        }

        public IConcreteRuleStateBuilder Clone() => new RuleStateBuilder(this);

        internal interface IRuleStateBuilder {
            INullableRuleStateBuilder WithNextSymbol();
        }

        internal interface IConcreteRuleStateBuilder : IRuleStateBuilder
        {
            RuleState Build();
            IConcreteRuleStateBuilder WithNewRule(IRule rule);
        }

        internal interface INullableRuleStateBuilder : IRuleStateBuilder
        {
            RuleState? TryBuild();
            INullableRuleStateBuilder WithNewRule(IRule rule);
        }

        internal class RuleStateBuilder : IConcreteRuleStateBuilder, INullableRuleStateBuilder
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

            public RuleStateBuilder WithNewRule(IRule rule)
            {
                if (ruleState == null)
                    return this;

                ruleState.ruleStates.Push(new Tuple<IRule, int>(rule, 0));
                return this;
            }

            IConcreteRuleStateBuilder IConcreteRuleStateBuilder.WithNewRule(IRule rule) => WithNewRule(rule);
            INullableRuleStateBuilder INullableRuleStateBuilder.WithNewRule(IRule rule) => WithNewRule(rule);
        }
    }
}
