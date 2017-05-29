using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using dsdiff_cross;

namespace dsdiff_cross_ui_wpf
{
    class RuntimeWrap
    {
        private readonly string RuntimeFileName = "dsdiff_cross.exe";

        public RuntimeWrap()
        {
            RuntimeFileName = Path.GetDirectoryName(Application.ExecutablePath) + 
                "\\" + RuntimeFileName;
        }

        public Process StartFiltering(string sourceFile, string targetFile, string configFile)
        {
            var p = new Process
                {
                    StartInfo =
                        {
                            FileName = RuntimeFileName,
                            Arguments =
                                "--config \"" + configFile + "\" --input \"" + sourceFile +
                                "\" --output \"" + targetFile + "\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                        },
                    EnableRaisingEvents = true
                };

            return p;
        }

        public List<Tuple<double, double>> GetFileResponse(string dsdFile, ref Dictionary<string, string> fileInfo)
        {
            var infoFile = Path.GetTempFileName();
            var logFile = Path.GetTempFileName();

            using (var p = new Process
                {
                    StartInfo =
                        {
                            FileName = RuntimeFileName,
                            Arguments =
                                "--input_log \"" + logFile + "\" --log_only true --file_info \"" + infoFile + "\" --config dummy_cfg.json --input \"" + dsdFile +
                                "\" --output dummy_cross.dff",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            StandardOutputEncoding = new UTF8Encoding()
                        }
                })
            {

                p.Start();
                if (p.WaitForExit(10000) == false)
                    throw new Exception("Unable to execute DSD Crossover runtime");

                File.WriteAllText("cross_result.txt", p.StandardOutput.ReadToEnd());
            }

            var infoText = File.ReadAllLines(infoFile);
            File.Delete(infoFile);
            foreach (var s in infoText)
            {
                var items = s.Replace(" ", "").Split(':');
                fileInfo.Add(items[0], items[1]);
            }

            var resultText = File.ReadAllLines(logFile);
            File.Delete(logFile);

            return resultText.Select(s => 
                s.Replace(',', '.').Split(';')).
                Select(items => 
                    new Tuple<double, double>(
                        double.Parse(items[0], CultureInfo.InvariantCulture), 
                        double.Parse(items[1], CultureInfo.InvariantCulture))).ToList();
        }
    }
}
