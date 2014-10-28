using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ripple.Compilers.Symbols
{
    internal interface ITypeInferable : ISyntaxNode
    {
        void InferType(List<ISyntaxNode> undecidedNodes);
    }
}
