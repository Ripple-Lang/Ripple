using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Ripple.VisualizationInterfaces;

namespace Ripple.Plugins.ArrayToTextVisualizer
{
    [Export(typeof(IStageVisualizer))]
    class ArrayToTextVisualizer : IStageVisualizer
    {
        public IStageView CreateView(IEnumerable<IStageContainer> stages)
        {
            return new ViewControl()
            {
                DataContext = new ViewModel(stages.First().Stages)
            };
        }

        public IStageView CreateView(IEnumerable<IStageContainer> stages, System.IO.Stream stream)
        {
            throw new NotImplementedException();
        }

        public string Name
        {
            get { return this.GetType().ToString(); }
        }

        public string Information
        {
            get { throw new NotImplementedException(); }
        }

        public string Constraints
        {
            get { throw new NotImplementedException(); }
        }
    }
}
