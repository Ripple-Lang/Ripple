using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ripple.Compilers.CodeGenerations;
using Ripple.Components;

namespace Ripple.Compilers.Tools.Simulations
{
    public class Simulator
    {
        public delegate void OnTimeChangedEventHandler(object sender, int time);
        public event OnTimeChangedEventHandler OnTimeChanged;

        public ISimulation CreateInstance(CompilationResult compilationResult, int maxTime)
        {
            var option = compilationResult.CompileOption;

            var assembly = compilationResult.CSharpCompilerResults.CompiledAssembly;
            var compiledClass = assembly.GetType(option.NameSpaceName + "." + option.ClassName);
            var constructor = compiledClass.GetConstructor(new Type[] { });

            return (ISimulation)constructor.Invoke(new object[] { });
        }

        public Task SimulateAsync(ISimulation instance, int maxTime, IEnumerable<StageInitInfo> stageInitInfos = null)
        {
            return Task.Run(() =>
            {
                var d = OnTimeChanged;
                __OnTimeChangedEventHandler handler = d != null ? (_, time) => { d(this, time); } : (__OnTimeChangedEventHandler)null;

                if (handler != null)
                {
                    instance.__OnTimeChanged += handler;
                }

                instance.__Initialize(maxTime);

                if (stageInitInfos != null)
                {
                    foreach (var info in stageInitInfos)
                    {
                        dynamic stageField = instance.GetType().GetField(info.StageName).GetValue(instance);
                        stageField[0] = (dynamic)info.Initializer();
                    }
                }

                instance.__Run(maxTime);

                if (handler != null)
                {
                    instance.__OnTimeChanged -= handler;
                }
            });
        }
    }

    public class StageInitInfo
    {
        public string StageName { get; private set; }
        public Func<object> Initializer { get; private set; }

        public StageInitInfo(string stageName, Func<object> initializer)
        {
            this.StageName = stageName;
            this.Initializer = initializer;
        }
    }
}
