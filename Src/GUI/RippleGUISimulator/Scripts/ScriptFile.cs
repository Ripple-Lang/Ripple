using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Soap;
using System.Xml.Serialization;
using Ripple.Compilers.Options;

namespace Ripple.GUISimulator.Scripts
{
    class ScriptFile : IDisposable
    {
        public enum FileOpenMode
        {
            Read, Create,
        }

        public FileOpenMode Mode { get; private set; }

        private const string RippleSrcPath = @"RippleSrc." + Constants.FileExtensions.RippleSrcFile;
        private const string CompileOptionPath = @"CompileOption.xml";
        private const string VisualizationInfoPath = @"VisualizationInfo.xml";
        private const string VisualizationToolDataPath = @"VisualizationToolData";

        private ZipArchive zip;

        public ScriptFile(Stream stream, FileOpenMode mode)
        {
            this.Mode = mode;
            this.zip = new ZipArchive(stream, mode == FileOpenMode.Read ? ZipArchiveMode.Read : ZipArchiveMode.Create);
        }

        public void WriteScript(Script script)
        {
            using (var stream = zip.CreateEntry(RippleSrcPath).Open())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(script.RippleSrc);
            }

            using (var stream = zip.CreateEntry(CompileOptionPath).Open())
            {
                new XmlSerializer(typeof(CompileOption)).Serialize(stream, script.CompileOption);
            }

            using (var stream = zip.CreateEntry(VisualizationInfoPath).Open())
            {
                new SoapFormatter().Serialize(stream, script.VisualizationInfo);
            }
        }

        public Script ReadScript()
        {
            string rippleSrc;
            using (var stream = zip.GetEntry(RippleSrcPath).Open())
            using (var reader = new StreamReader(stream))
            {
                rippleSrc = reader.ReadToEnd();
            }

            CompileOption compileOption;
            using (var stream = zip.GetEntry(CompileOptionPath).Open())
            {
                compileOption = (CompileOption)new XmlSerializer(typeof(CompileOption)).Deserialize(stream);
            }

            VisualizationInfo visualizationInfo;
            using (var stream = zip.GetEntry(VisualizationInfoPath).Open())
            {
                visualizationInfo = (VisualizationInfo)new SoapFormatter().Deserialize(stream);
            }

            return new Script(rippleSrc, compileOption, visualizationInfo);
        }

        public Stream GetVisualizationToolDataStream()
        {
            return zip.GetEntry(VisualizationToolDataPath).Open();
        }

        public Stream CreateVisualizationToolDataStream()
        {
            return zip.CreateEntry(VisualizationToolDataPath).Open();
        }

        public void Dispose()
        {
            zip.Dispose();
        }
    }
}
