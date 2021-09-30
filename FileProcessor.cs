using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace FixAudiobookTitle
{
    public class FileProcessor
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Options _opts;

        public FileProcessor(Options opts)
        {
            _opts = opts;
        }

        public void Run()
        {
            if (!_opts.Exists)
                log.Error($"File Not Found [{_opts.InputFile}]");
            else if (_opts.IsDirectory)
                ProcessDirectory(_opts.InputFile);
            else if (_opts.IsM4b)
                ProcessFile(_opts.InputFile);
            else
                log.Info("Nothing to Do");
        }

        public void ProcessDirectory(string directory)
        {
            log.Info($"Processing directory [{directory}]");
            string[] files = Directory.GetFiles(directory, "*.m4b");
            if (null != files && 0 < files.Length)
            {
                foreach (string f in files)
                {
                    ProcessFile(f);
                }
            }
            else
            {
                log.Info($"No files found to process in [{directory}]");
            }
        }

        public void ProcessFile(string filename)
        {
            log.Info($"Processing file [{filename}]");
            Dictionary<string, string> metadata = ExtractMetadata(filename);
            if (metadata.ContainsKey("album"))
            {
                string album = metadata["album"];
                string title = metadata["title"];
                if (title.CompareTo(album) != 0)
                {
                    log.Info($"Album = {album}, Title = {title}");
                    SetTitle(filename, metadata["album"]);
                }
                else
                {
                    log.Info("Nothing to Do");
                }
            }
        }

        private Dictionary<string, string> ExtractMetadata(string filename)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string logFile = filename.Replace(".m4b", "_log.txt");

            try
            {
                List<string> args = new List<string>();
                args.Add(string.Format(@"& '{0}'", _opts.ExecFfmpeg));
                args.Add("-i");
                args.Add(filename);
                args.Add($"> {logFile}");
                args.Add("2>&1");
                string sArgs = String.Join(" ", args.ToArray());

                using (PowerShell psi = PowerShell.Create())
                {
                    psi.AddScript(sArgs);
                    psi.Invoke();
                }

                if (File.Exists(logFile))
                {
                    string line;
                    string key;
                    string value;
                    bool isMetadata = false;
                    using (var sr = new StreamReader(logFile))
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.StartsWith("  Metadata:"))
                            {
                                isMetadata = true;
                            }
                            else if (isMetadata)
                            {
                                if (line.StartsWith("    "))
                                {
                                    int idx = line.IndexOf(":");
                                    if (0 < idx)
                                    {
                                        key = line.Substring(0, idx).Trim();
                                        value = line.Substring(idx + 1).Trim();
                                        result.Add(key, value);
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"EXCEPTION extracting metadata : {e.Message}", e);
            }
            finally
            {
                if (File.Exists(logFile))
                {
                    try
                    {
                        log.Debug($"Deleting temporary log file [{logFile}]");
                        File.Delete(logFile);
                    }
                    catch (Exception) { }
                }
            }

            log.Debug($"METADATA [{filename}] {{");
            foreach (KeyValuePair<string, string> kvp in result)
            {
                log.Debug($"    {kvp.Key} = {kvp.Value},");
            }
            log.Debug("}");

            return result;
        }

        private void SetTitle(string filename, string title)
        {
            string tmpFile = filename.Replace(".m4b", "-tmp.m4b");

            try
            {
                List<string> args = new List<string>();
                args.Add(string.Format(@"& '{0}'", _opts.ExecFfmpeg));
                args.Add("-i");
                args.Add(filename);
                args.Add("-metadata");
                args.Add($"title=\"{title}\"");
                args.Add("-codec");
                args.Add("copy");
                args.Add(tmpFile);
                string sArgs = String.Join(" ", args.ToArray());

                using (PowerShell psi = PowerShell.Create())
                {
                    psi.AddScript(sArgs);
                    psi.Invoke();
                }

                if (File.Exists(tmpFile))
                {
                    FileInfo fi1 = new FileInfo(filename);
                    FileInfo fi2 = new FileInfo(tmpFile);
                    long length_one = fi1.Length;
                    long length_two = fi2.Length;
                    long diff;
                    if (length_one > length_two) diff = length_one - length_two;
                    else diff = length_two - length_one;

                    // Make sure the file size difference is less than 1%
                    if (((diff / length_one) * 100) < 1)
                    {
                        log.Debug($"Deleting original file [{filename}]");
                        fi1.Delete();
                        log.Debug($"Copying temporary file [{tmpFile}] to [{filename}]");
                        fi2.CopyTo(filename, true);
                    }
                    else
                    {
                        log.Error("FileSize variance is greater than 1%. Cancelling operation.");
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"EXCEPTION extracting metadata : {e.Message}", e);
            }
            finally
            {
                if (File.Exists(tmpFile))
                {
                    try
                    {
                        log.Debug($"Deleting temporary file [{tmpFile}]");
                        File.Delete(tmpFile);
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
