using System.Collections.Generic;
using System.Text;
using Ripple.Compilers.Options;

namespace Ripple.Compilers.CodeGenerations.CSharp
{
    internal class NameSpace : ICSharp
    {
        public string Name { get; private set; }
        private List<Type> types;

        public NameSpace(string name)
        {
            this.Name = name;
            this.types = new List<Type>();
        }

        public Type DefineType(string name, TypeKind kind, AccessLevel accessLevel, IList<string> extendsOrImplements = null)
        {
            Type type = new Type(name, this, kind, accessLevel, extendsOrImplements);
            types.Add(type);
            return type;
        }

        public string ToCSharpCode(CompileOption option)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("namespace " + Name);
            sb.AppendLine("{");

            foreach (var type in types)
            {
                sb.AppendLine(type.ToCSharpCode(option));
            }

            sb.Append("}");

            return sb.ToString();
        }

        public string CSharpLocation
        {
            get { return Name; }
        }
    }
}
