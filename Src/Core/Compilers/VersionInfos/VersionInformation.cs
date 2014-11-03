using System;
using System.Reflection;

namespace Ripple.Compilers.VersionInfos
{
    public static class VersionInformation
    {
        public static readonly string ProductName;
        public static readonly Version Version;

        static VersionInformation()
        {
            var asm = Assembly.GetExecutingAssembly();
            ProductName = GetAttribute<AssemblyTitleAttribute>(asm).Title;
            Version = asm.GetName().Version;
        }

        private static T GetAttribute<T>(Assembly assembly) where T : class
        {
            return Attribute.GetCustomAttribute(assembly, typeof(T)) as T;
        }
    }
}
