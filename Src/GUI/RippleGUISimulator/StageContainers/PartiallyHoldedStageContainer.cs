using System;
using System.Collections.Generic;
using System.Linq;
using Ripple.VisualizationInterfaces;

namespace Ripple.GUISimulator.StageContainers
{
    internal class PartiallyHoldedStageContainer : IStageContainer
    {
        public Array Stages { get; private set; }
        public string Name { get; private set; }
        private readonly int maxTime;

        public PartiallyHoldedStageContainer(Array stages, int maxTime, string name)
        {
            this.Stages = stages;
            this.Name = name;
            this.maxTime = maxTime;
        }

        public IEnumerable<int> SupportedTime
        {
            get { return Enumerable.Range(maxTime - Stages.Length + 1, Stages.Length); }
        }

        public int GetArrayIndex(int time)
        {
            return time % Stages.Length;
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return Stages.GetEnumerator();
        }
    }
}
