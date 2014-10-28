using System.Collections.Generic;

namespace Ripple.Compilers.Symbols
{
    internal interface ISyntaxNode
    {
        IEnumerable<ISyntaxNode> Children { get; }
    }
}
