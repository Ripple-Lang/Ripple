using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Ripple.VisualizationInterfaces;

namespace Ripple.GUISimulator
{
    public class ImportedPlugins
    {
        [ImportMany(typeof(IStageVisualizer))]
        public IEnumerable<Lazy<IStageVisualizer>> Visualizers { get; set; }
    }
}
