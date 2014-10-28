using System;
using System.Collections.Generic;
using System.Text;
using Ripple.Compilers.ConstantValues;
using Ripple.Compilers.Options;
using Ripple.Compilers.Symbols;
using Ripple.Compilers.Types;

namespace Ripple.Compilers.CodeGenerations.CSharp
{
    internal abstract class TypeMember : ICSharp
    {
        public string Name { get; private set; }
        public ICSharp Enclosing { get; private set; }
        public AccessLevel AccessLevel { get; private set; }
        public TypeData EvaluatedType { get; private set; }
        public bool IsStatic { get; private set; }

        public TypeMember(string name, ICSharp enclosingType, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic)
        {
            this.Name = name;
            this.Enclosing = enclosingType;
            this.AccessLevel = accessLevel;
            this.EvaluatedType = evaluatedType;
            this.IsStatic = isStatic;
        }

        public abstract string ToCSharpCode(CompileOption option);

        public string CSharpLocation
        {
            get { return CSharpLocation + Name; }
        }
    }

    internal class Field : TypeMember
    {
        public VariableSymbol Variable { get; private set; }

        public Field(string name, ICSharp enclosingType, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, VariableSymbol variable)
            : base(name, enclosingType, accessLevel, evaluatedType, isStatic)
        {
            this.Variable = variable;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return AccessLevel.ToCSharpCode(option)
                + " " + (IsStatic ? "static" : "")
                + " " + EvaluatedType.ToCSharpCode(option)
                + " @" + Variable.ToCSharpName(option) + ";";
        }
    }

    internal class AutoImplementedProperty : TypeMember
    {
        public AccessLevel GetterAccessLevel { get; private set; }
        public AccessLevel SetterAccessLevel { get; private set; }

        public AutoImplementedProperty(string name, ICSharp enclosingType, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, AccessLevel getterAccessLevel, AccessLevel setterAccessLevel)
            : base(name, enclosingType, accessLevel, evaluatedType, isStatic)
        {
            this.GetterAccessLevel = getterAccessLevel;
            this.SetterAccessLevel = setterAccessLevel;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return AccessLevel.ToCSharpCode(option)
                + " " + (IsStatic ? "static" : "")
                + " " + EvaluatedType.ToCSharpCode(option)
                + " @" + Name
                + " { " + GetterAccessLevel.ToCSharpCode(option) + " get; "
                + SetterAccessLevel.ToCSharpCode(option) + " set; }";
        }
    }

    internal class ReadonlyProperty : TypeMember
    {
        public AccessLevel GetterAccessLevel { get; private set; }
        private readonly BlockStatement body;

        public ReadonlyProperty(string name, ICSharp enclosingType, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, AccessLevel getterAccessLevel, BlockStatement body)
            : base(name, enclosingType, accessLevel, evaluatedType, isStatic)
        {
            this.GetterAccessLevel = getterAccessLevel;
            this.body = body;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return AccessLevel.ToCSharpCode(option) + " " + EvaluatedType.ToCSharpCode(option) + " @" + Name + "{"
                + Environment.NewLine + "get"
                + Environment.NewLine + body.ToCSharpCode(option)
                + Environment.NewLine + "}";
        }

    }

    internal class Constructor : TypeMember
    {
        public string MaxTimeArgumentName { get; private set; }
        public string ContentCSharpCode { get; private set; }

        public Constructor(ICSharp enclosingType, AccessLevel accessLevel, string maxTimeArgumentName, string contentCSharpCode)
            : base("@<Constructor>", enclosingType, accessLevel, BuiltInNumericType.Nothing, false)
        {
            this.MaxTimeArgumentName = maxTimeArgumentName;
            this.ContentCSharpCode = contentCSharpCode;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return AccessLevel.ToCSharpCode(option) + " " + Enclosing.Name
                + "(" + Constants.TimeType.ToCSharpCode(option) + " " + MaxTimeArgumentName + ")"
                + Environment.NewLine + ContentCSharpCode;
        }
    }

    internal class TextBasedMethod : TypeMember
    {
        private readonly IList<FunctionParameterSymbol> parameters;
        private readonly string contentCSharpCode;

        public TextBasedMethod(string name, ICSharp enclosingType, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, IList<FunctionParameterSymbol> parameters, string contentCSharpCode)
            : base(name, enclosingType, accessLevel, evaluatedType, isStatic)
        {
            this.parameters = parameters;
            this.contentCSharpCode = contentCSharpCode;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(AccessLevel.ToCSharpCode(option)
                + " " + (IsStatic ? "static" : "")
                + " " + EvaluatedType.ToCSharpCode(option)
                + " @" + Name + "(");

            for (int i = 0; i < parameters.Count; i++)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(parameters[i].Type.ToCSharpCode(option) + " @" + parameters[i].ToCSharpName(option));
            }

            sb.AppendLine(")");

            sb.Append(contentCSharpCode);

            return sb.ToString();
        }
    }

    internal class Method : TypeMember
    {
        public IMethodDefnition Defnition { get; private set; }

        public Method(string name, ICSharp enclosingType, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, IMethodDefnition defnition)
            : base(name, enclosingType, accessLevel, evaluatedType, isStatic)
        {
            this.Defnition = defnition;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return new TextBasedMethod(Name, Enclosing, AccessLevel, EvaluatedType, IsStatic,
                 Defnition.Parameters, Defnition.Body.ToCSharpCode(option)).ToCSharpCode(option);
        }
    }

    internal class Delegate : TypeMember
    {
        public string ParametersString { get; set; }

        public Delegate(string name, ICSharp enclosingType, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, string parametersString)
            : base(name, enclosingType, accessLevel, evaluatedType, isStatic)
        {
            this.ParametersString = parametersString;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return AccessLevel.ToCSharpCode(option) + " "
                + "delegate "
                + EvaluatedType.ToCSharpCode(option) + " "
                + Name + " "
                + ParametersString + ";";
        }
    }

    internal class Event : TypeMember
    {
        public string HandlerTypeName { get; private set; }

        public Event(string name, ICSharp enclosingType, AccessLevel accessLevel, TypeData evaluatedType, bool isStatic, string handlerTypeName)
            : base(name, enclosingType, accessLevel, evaluatedType, isStatic)
        {
            this.HandlerTypeName = handlerTypeName;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return AccessLevel.ToCSharpCode(option) + " "
                + "event "
                + HandlerTypeName + " "
                + Name + ";";
        }
    }
}
