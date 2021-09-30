using CommandLine;
using log4net;
using System;
using System.Configuration;
using System.IO;

namespace FixAudiobookTitle
{
    public class Options
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [Option('i', "input", Required = true, HelpText = "Input file OR directory to process. If it is a directory, it will process ALL m4b files within the directory.")]
        public string InputFile
        {
            get
            {
                return _InputFile;
            }
            set
            {
                _InputFile = value;
                if (!string.IsNullOrWhiteSpace(_InputFile))
                {
                    if (File.Exists(_InputFile) || Directory.Exists(_InputFile))
                    {
                        Exists = true;
                        try
                        {
                            FileAttributes fa = File.GetAttributes(_InputFile);
                            IsDirectory = ((fa & FileAttributes.Directory) == FileAttributes.Directory);
                            IsM4b = _InputFile.Trim().ToLower().EndsWith(".m4b");
                        }
                        catch (Exception e)
                        {
                            log.Error($"EXCEPTION setting InputFile option : {e.Message}", e);
                        }
                    }
                }
            }
        }
        private string _InputFile;

        public bool IsDirectory { get; set; }

        public bool IsM4b { get; set; }

        public bool Exists { get; set; }

        public string ExecFfmpeg
        {
            get
            {
                return ConfigurationManager.AppSettings["ExecFfmpeg"];
            }
        }
    }
}
