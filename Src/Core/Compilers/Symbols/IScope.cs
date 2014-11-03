using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ripple.Compilers.ErrorsAndWarnings;
using Ripple.Compilers.Exceptions;
using Ripple.Compilers.LexicalAnalysis;

namespace Ripple.Compilers.Symbols
{
    internal interface IScope
    {
        IScope Enclosing { get; }
        void Define(Symbol symbol);
        Symbol Resolve(string name);
    }

    internal class GlobalScope : IScope, ISyntaxNode
    {
        private Dictionary<string, Symbol> symbols;
        public string CSharpLocation { get; private set; }

        public GlobalScope(string csharpLocation)
        {
            this.symbols = new Dictionary<string, Symbol>();
            this.CSharpLocation = csharpLocation;
        }

        public IDictionary<string, Symbol> SymbolsDictionary
        {
            get { return new ReadOnlyDictionary<string, Symbol>(symbols); }
        }

        public IEnumerable<Symbol> Symbols
        {
            get { return symbols.Values; }
        }

        public IEnumerable<ISyntaxNode> Children
        {
            get { return Symbols; }
        }

        public IScope Enclosing
        {
            get { return null; }
        }

        public void Define(Symbol symbol)
        {
            try
            {
                symbols.Add(symbol.Name, symbol);
            }
            catch (ArgumentException e)
            {
                throw new SymbolAlreadyExistsException(symbol.Name, e);
            }
        }

        public Symbol Resolve(string name)
        {
            try
            {
                return symbols[name];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public bool InferChildlensType(ErrorsAndWarningsContainer errorsAndWarnings)
        {
            for (; ; )
            {
                var undecidedNodes = new List<ISyntaxNode>();

                if (!InferType(this, undecidedNodes, errorsAndWarnings))
                {
                    return false;
                }

                if (undecidedNodes.Count == 0)
                {
                    break;
                }
            }

            return true;
        }

        private static bool InferType(ISyntaxNode node, List<ISyntaxNode> undecidedNodes, ErrorsAndWarningsContainer errorsAndWarnings)
        {
            bool state = true;

            foreach (var child in node.Children.Where(c => c != null))
            {
                state = state && InferType(child, undecidedNodes, errorsAndWarnings);
            }

            if (!state)
            {
                return false;
            }

            var asInferable = node as ITypeInferable;

            if (asInferable != null)
            {
                if (undecidedNodes.Contains(node))
                {
                    // TODO : 適切なエラー作成
                    errorsAndWarnings.AddError(new Error(CharPosition.InvalidPosition, "型推論できない"));
                    return false;
                }
                else
                {
                    asInferable.InferType(undecidedNodes);
                }
            }

            return true;
        }
    }

}
