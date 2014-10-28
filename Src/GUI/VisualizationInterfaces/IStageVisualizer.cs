using System.Collections.Generic;
using System.IO;

namespace Ripple.VisualizationInterfaces
{
    public interface IStageVisualizer
    {
        IStageView CreateView(IEnumerable<IStageContainer> stages);
        IStageView CreateView(IEnumerable<IStageContainer> stages, Stream stream);
        string Name { get; }
        string Information { get; }
        string Constraints { get; }
    }
}
