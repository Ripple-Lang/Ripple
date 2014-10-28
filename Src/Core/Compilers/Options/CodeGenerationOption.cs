using System;

namespace Ripple.Compilers.Options
{
    public class CompileOption : ICloneable
    {
        public bool AddUniqueNoToVariable { get; set; }
        public bool UseBuiltinMethods { get; set; }
        public bool ProhibitOverloadingOfVariable { get; set; }

        public ParallelizationOption ParallelizationOption { get; set; }

        public bool Optimize { get; set; }
        public bool CacheStages { get; set; }
        public bool CacheParameters { get; set; }

        public string NameSpaceName { get; set; }
        public string ClassName { get; set; }

        public bool GenerateInMemory { get; set; }
        public string OutputAssembly { get; set; }

        /// <summary>
        /// 既定のコンパイルオプションが設定されたインスタンスを生成します。
        /// </summary>
        public CompileOption()
        {
            AddUniqueNoToVariable = true;
            UseBuiltinMethods = true;
            ProhibitOverloadingOfVariable = false;

            ParallelizationOption = Options.ParallelizationOption.InParallelSpecifiedCode;

            Optimize = true;
            CacheStages = true;
            CacheParameters = true;

            NameSpaceName = "__N1";
            ClassName = "__C1";

            GenerateInMemory = true;
            OutputAssembly = null;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    [Flags]
    public enum ParallelizationOption
    {
        None = 0,
        InInitializingArray = 1,
        InParallelSpecifiedCode = 2,
        All = 3,
    }
}
