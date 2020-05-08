using autosupport_lsp_server.Symbols;
using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Parser
{
    internal class RuleState
    {
        private Stack<Tuple<IRule, int>> ruleStates;

        public IRule CurrentRule => ruleStates.Peek().Item1;
        public ISymbol? CurrentSymbol => ruleStates.Peek().Item2 >= CurrentRule.Symbols.Count
            ? null
            : CurrentRule.Symbols[ruleStates.Peek().Item2];

        public bool IsEmpty => ruleStates.Count == 0;

        public RuleState(IRule rule, int position = 0)
        {
            if (position < 0)
                throw new ArgumentException("Position in rule may not be negative");
            if (rule == null)
                throw new ArgumentException("Rule may not be null");

            ruleStates = new Stack<Tuple<IRule, int>>();
            ruleStates.Push(new Tuple<IRule, int>(rule, position));
        }

        private RuleState(RuleState ruleState)
        {
            ruleStates = ruleState.ruleStates.Clone();
        }

        public RuleStateBuilder Clone() => new RuleStateBuilder(this);

        internal class RuleStateBuilder
        {
            private RuleState? ruleState;

            internal RuleStateBuilder(RuleState ruleState)
            {
                this.ruleState = new RuleState(ruleState);
            }

            internal RuleStateBuilder WithNextSymbol()
            {
                if (ruleState == null)
                    return this;

                Tuple<IRule, int>? current;
                int newIndex;
                bool isEmpty;

                do
                {
                    isEmpty = ruleState.ruleStates.TryPop(out current);
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

            internal RuleState? TryBuild()
            {
                return ruleState;
            }
        }
    }
}
