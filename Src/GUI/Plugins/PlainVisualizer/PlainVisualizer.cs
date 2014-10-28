using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml.Serialization;
using Ripple.VisualizationInterfaces;

namespace Ripple.Plugins.PlainVisualizer
{
    [Export(typeof(IStageVisualizer))]
    public class PlainVisualizer : IStageVisualizer
    {
        public IStageView CreateView(IEnumerable<IStageContainer> stages)
        {
            return new ViewPage(stages.First());
        }

        public IStageView CreateView(IEnumerable<IStageContainer> stages, System.IO.Stream stream)
        {
            // 保存された状態をデシリアライズ
            var state = (State)new XmlSerializer(typeof(State)).Deserialize(stream);
            //var state = (State)new SoapFormatter().Deserialize(stream);

            return new ViewPage(stages.First(), state);
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
