using System;
using System.Collections;
using System.Collections.Generic;

namespace Ripple.VisualizationInterfaces
{
    public interface IStageContainer : IEnumerable
    {
        Array Stages { get; }
        string Name { get; }
        int GetArrayIndex(int time);
        IEnumerable<int> SupportedTime { get; }
    }
}
