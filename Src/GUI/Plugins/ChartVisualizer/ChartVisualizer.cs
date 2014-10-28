using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml.Serialization;
using Ripple.VisualizationInterfaces;

namespace Ripple.Plugins.ChartVisualizer
{
    [Export(typeof(IStageVisualizer))]
    public class ChartVisualizer : IStageVisualizer
    {
        public IStageView CreateView(IEnumerable<IStageContainer> stages)
        {
            return new View(stages);
        }

        public IStageView CreateView(IEnumerable<IStageContainer> stages, System.IO.Stream stream)
        {
            return new View(stages, (SeriesChartType)new XmlSerializer(typeof(SeriesChartType)).Deserialize(stream));
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
