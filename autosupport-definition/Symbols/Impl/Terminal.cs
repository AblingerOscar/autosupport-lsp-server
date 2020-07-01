using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using autosupport_lsp_server.Symbols.Impl.Terminals;
using Sprache;
using System;
using System.Xml.Linq;

namespace autosupport_lsp_server.Symbols.Impl
{
    public abstract class Terminal : Symbol, ITerminal
    {
        protected Terminal() { /* do nothing */ }

        protected abstract Parser<string> Parser { get; }

        public abstract int MinimumNumberOfCharactersToParse { get; }

        public abstract string[] PossibleContent { get; }

        public override void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal, Action<IAction> action, Action<IOneOf> oneOf)
        {
            terminal.Invoke(this);
        }

        public override R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal, Func<IAction, R> action, Func<IOneOf, R> oneOf)
        {
            return terminal.Invoke(this);
        }

        public override XElement SerializeToXLinq()
        {
            return base.SerializeToXLinq();
        }

        public static ITerminal FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            var name = element.Name.ToString();
            var elementType = AnnotationUtils.FindTypeWithName(name);

            if (elementType == null)
                throw new ArgumentException($"Type for '{name}' does not exist, is not an ITerminal or does not have a default constructor");


            if (typeof(StringTerminal).IsAssignableFrom(elementType))
            {
                var result = new StringTerminal(element.Value);
                AddSymbolValuesFromXLinq(result, element, interfaceDeserializer);
                return result;
            }
            else if (typeof(Terminal).IsAssignableFrom(elementType))
            {
                if (elementType.GetConstructor(new Type[0])?.Invoke(null) is Terminal result)
                {
                    AddSymbolValuesFromXLinq(result, element, interfaceDeserializer);
                    return result;
                }
            }

            throw new ArgumentException($"Type '{name}' does not exist, is not an ITerminal or does not have a default constructor");
        }

        public bool TryParse(string str)
        {
            var parseResult = Parser.TryParse(str);
            return parseResult.WasSuccessful;
        }
    }
}
