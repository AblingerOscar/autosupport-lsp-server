using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Parsing.Impl
{
    internal class ParseResult : IParseResult
    {
        public ParseResult(bool finished, string[] possibleContinuations, IError[] errors)
        {
            Finished = finished;
            PossibleContinuations = possibleContinuations;
            Errors = errors;
        }

        public bool Finished { get; set; }

        public string[] PossibleContinuations { get; set; }

        public IError[] Errors { get; set; }
    }
}
