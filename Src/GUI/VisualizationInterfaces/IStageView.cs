using System.IO;

namespace Ripple.VisualizationInterfaces
{
    public interface IStageView
    {
        void SerializeCurrentState(Stream stream);
    }
}
