using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace Ripple.GUISimulator.Windows.Windows
{
    class CSharpCodeFormatter
    {
        public static Task<string> FormatAsync(string code)
        {
            return Task<string>.Run(() =>
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var customWorkSpace = new CustomWorkspace();
                var options = customWorkSpace.GetOptions();
                var formattedNode = Formatter.Format(syntaxTree.GetRoot(), customWorkSpace, options);
                return formattedNode.ToFullString();
            });
        }
    }
}
