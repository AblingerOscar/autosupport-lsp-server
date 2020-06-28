using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Serialization.Annotation;
using System;
using System.Xml.Linq;
using static autosupport_lsp_server.Serialization.Annotation.AnnotationUtils;

namespace autosupport_lsp_server.Symbols.Impl
{
    [XLinqName("action")]
    public class Action : IAction
    {
        public string Command { get; private set; } = "";

        public Action() { }

        public Action(string command)
        {
            Command = command;
        }

        public string GetBaseCommand()
        {
            int idx = Command.IndexOf(' ');

            return idx == -1
                ? Command
                : Command.Substring(0, idx);
        }

        public string[] GetArguments()
        {
            int idx = Command.IndexOf(' ');

            if (idx == -1)
                return new string[0];
            else
                return Command.Substring(idx + 1).Split(' ');
        }

        public void Match(Action<ITerminal> terminal, Action<INonTerminal> nonTerminal, Action<IAction> action, Action<IOneOf> oneOf) =>
            action.Invoke(this);

        public R Match<R>(Func<ITerminal, R> terminal, Func<INonTerminal, R> nonTerminal, Func<IAction, R> action, Func<IOneOf, R> oneOf) =>
            action.Invoke(this);

        public XElement SerializeToXLinq()
        {
            return new XElement(annotation.ClassName(), Command);
        }

        public static IAction FromXLinq(XElement element, IInterfaceDeserializer interfaceDeserializer)
        {
            return new Action(element.Value);
        }

        private static readonly XLinqClassAnnotationUtil annotation = XLinqOf(typeof(Action));

        public override string? ToString()
        {
            return $"action({Command})";
        }
    }
}
