using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ripple.Compilers.Options;
using Ripple.Compilers.Symbols;
using Ripple.Compilers.Types;

namespace Ripple.Compilers.CodeGenerations.CSharp
{
    internal class Type : TypeMember, ICSharp
    {
        public TypeKind Kind { get; private set; }
        private IList<string> extendsOrImplements;

        private List<Delegate> delegates = new List<Delegate>();
        private List<Event> events = new List<Event>();
        private List<Type> types = new List<Type>();
        private List<Field> fields = new List<Field>();
        private List<AutoImplementedProperty> autoProperties = new List<AutoImplementedProperty>();
        private List<ReadonlyProperty> readonlyProperties = new List<ReadonlyProperty>();
        private List<Constructor> constructors = new List<Constructor>();
        private List<TextBasedMethod> textbasedMethods = new List<TextBasedMethod>();
        private List<Method> methods = new List<Method>();

        private IEnumerable<TypeMember> AllMembers
        {
            get
            {
                return delegates
                    .Concat<TypeMember>(events)
                    .Concat(types)
                    .Concat(fields)
                    .Concat(autoProperties)
                    .Concat(readonlyProperties)
                    .Concat(constructors)
                    .Concat(textbasedMethods)
                    .Concat(methods);
            }
        }

        public Type(string name, ICSharp enclosing, TypeKind kind, AccessLevel accessLevel, IList<string> extendsOrImplements = null)
            : base(name, enclosing, accessLevel, null, false)   // TODO : 適当
        {
            this.Kind = kind;
            this.extendsOrImplements = extendsOrImplements;
        }

        public Type DefineType(string name, TypeKind kind, AccessLevel accessLevel, IList<string> extendsOrImplements = null)
        {
            Type type = new Type(name, this, kind, accessLevel, extendsOrImplements);
            types.Add(type);
            return type;
        }

        public Field DefineField(string name, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, VariableSymbol variable)
        {
            Field field = new Field(name, this, accessLevel, evaluatedType, isStatic, variable);
            fields.Add(field);
            return field;
        }

        public AutoImplementedProperty DefineAutoImplementedProperty(string name, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, AccessLevel getterAccessLevel, AccessLevel setterAccessLevel)
        {
            AutoImplementedProperty prop = new AutoImplementedProperty(name, this, accessLevel, evaluatedType, isStatic, getterAccessLevel, setterAccessLevel);
            autoProperties.Add(prop);
            return prop;
        }

        public ReadonlyProperty DefineReadonlyProperty(string name, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, AccessLevel getterAccessLevel, BlockStatement body)
        {
            ReadonlyProperty property = new ReadonlyProperty(name, this, accessLevel, evaluatedType, isStatic, getterAccessLevel, body);
            readonlyProperties.Add(property);
            return property;
        }

        public Constructor DefineConstructor(AccessLevel accessLevel, string maxTimeArgumentName, string bodyCSharpCode)
        {
            Constructor constructor = new Constructor(this, accessLevel, maxTimeArgumentName, bodyCSharpCode);
            constructors.Add(constructor);
            return constructor;
        }

        public TextBasedMethod DefineTextBasedMethod(string name, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, IList<FunctionParameterSymbol> parameters, string contentCSharpCode)
        {
            TextBasedMethod method = new TextBasedMethod(name, this, accessLevel, evaluatedType, isStatic, parameters, contentCSharpCode);
            textbasedMethods.Add(method);
            return method;
        }

        public Method DefineMethod(string name, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, IMethodDefnition defnition)
        {
            Method method = new Method(name, this, accessLevel, evaluatedType, isStatic, defnition);
            methods.Add(method);
            return method;
        }

        public Delegate DefineDelegate(string name, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, string parametersString)
        {
            Delegate d = new Delegate(name, this, accessLevel, evaluatedType, isStatic, parametersString);
            delegates.Add(d);
            return d;
        }

        public Event DefineEvent(string name, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, string handlerTypeName)
        {
            Event e = new Event(name, this, accessLevel, evaluatedType, isStatic, handlerTypeName);
            events.Add(e);
            return e;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(AccessLevel.ToCSharpCode(option) + " " + Kind.ToCSharpCode(option) + " " + Name);
            if (extendsOrImplements != null && extendsOrImplements.Count() > 0)
            {
                sb.Append(" : ");
                for (int i = 0; i < extendsOrImplements.Count; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(extendsOrImplements[i]);
                }
            }

            sb.AppendLine();
            sb.AppendLine("{");

            foreach (var member in AllMembers)
            {
                sb.AppendLine(member.ToCSharpCode(option));
                sb.AppendLine();
            }

            sb.Append("}");

            return sb.ToString();
        }
    }

    internal class TypeKind
    {
        private enum Kind
        {
            Class, Struct,
        }

        private readonly Kind kind;

        private TypeKind(Kind kind)
        {
            this.kind = kind;
        }

        public static readonly TypeKind Class = new TypeKind(Kind.Class);
        public static readonly TypeKind Struct = new TypeKind(Kind.Struct);

        public string ToCSharpCode(CompileOption option)
        {
            if (kind == Kind.Class)
            {
                return "class";
            }
            else
            {
                return "struct";
            }
        }
    }
}
