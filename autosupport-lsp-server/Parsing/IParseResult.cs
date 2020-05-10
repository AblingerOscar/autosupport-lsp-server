using System;
using System.Collections.Generic;
using System.Text;

namespace autosupport_lsp_server.Parsing
{
    interface IParseResult
    {
        bool FinishedSuccessfully { get; }
    }
}
