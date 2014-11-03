using System.Collections.Generic;

namespace Ripple.Compilers.Symbols
{
    internal interface ITypeInferable : ISyntaxNode
    {
        void InferType(List<ISyntaxNode> undecidedNodes);
    }
}
