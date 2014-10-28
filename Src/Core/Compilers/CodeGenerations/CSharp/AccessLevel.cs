using System.Diagnostics;

namespace Ripple.Compilers.CodeGenerations.CSharp
{
    internal class AccessLevel
    {
        private enum Kind
        {
            Public, Internal, /* ProtectedInternal, */Private, NoSpecified
        }

        private readonly Kind value;

        private AccessLevel(Kind value)
        {
            this.value = value;
        }

        public static readonly AccessLevel Public = new AccessLevel(Kind.Public);
        public static readonly AccessLevel Internal = new AccessLevel(Kind.Internal);
        public static readonly AccessLevel Private = new AccessLevel(Kind.Private);
        public static readonly AccessLevel NoSpecified = new AccessLevel(Kind.NoSpecified);

        public string ToCSharpCode(Options.CompileOption option)
        {
            switch (value)
            {
                case Kind.Public:
                    return "public";
                case Kind.Internal:
                    return "internal";
                case Kind.Private:
                    return "private";
                case Kind.NoSpecified:
                    return "";
                default:
                    Debug.Assert(false);
                    return null;
            }
        }
    }
}
