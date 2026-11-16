#nullable enable
using System;

namespace HERMESLANG.Parser
{
    public class ParserException : Exception
    {
        public ParserException(string message)
            : base(message)
        {
        }
    }
}
