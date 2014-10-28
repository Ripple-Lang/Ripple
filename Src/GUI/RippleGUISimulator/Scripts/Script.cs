using System;
using Ripple.Compilers.Options;

namespace Ripple.GUISimulator.Scripts
{
    [Serializable]
    class Script
    {
        public string RippleSrc { get; private set; }
        public CompileOption CompileOption { get; private set; }
        public VisualizationInfo VisualizationInfo { get; private set; }

        public Script(string rippleSrc, CompileOption compileOption, VisualizationInfo visualizationInfo)
        {
            this.RippleSrc = rippleSrc;
            this.CompileOption = compileOption;
            this.VisualizationInfo = visualizationInfo;
        }
    }

    [Serializable]
    class VisualizationInfo
    {
        public string[] Stages { get; private set; }
        public string VisualizationToolsName { get; private set; }

        public VisualizationInfo(string[] stages, string visualizationToolsName)
        {
            this.Stages = stages;
            this.VisualizationToolsName = visualizationToolsName;
        }
    }
}
