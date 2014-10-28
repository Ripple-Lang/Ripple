using System;

namespace Ripple.Compilers.Exceptions
{
    public class SymbolAlreadyExistsException : Exception
    {
        public string SymbolName { get; private set; }

        public SymbolAlreadyExistsException(string symbolName)
            : this(symbolName, null)
        { }

        public SymbolAlreadyExistsException(string symbolName, Exception innerException)
            : base("シンボル\"" + symbolName + "\"はすでに定義されています。", innerException)
        {
            this.SymbolName = symbolName;
        }
    }
}
