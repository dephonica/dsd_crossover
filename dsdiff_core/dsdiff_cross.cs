using System;
using System.IO;
using loggerlib;
using rempacklib;

namespace dsdiff_cross
{
    public class DsdiffCross : IDisposable
    {
        public string InputFile { get; set; }
        public string ConfigFile { get; set; }
        public string OutputFile { get; set; }
        public string InputLogFile { get; set; }
        public string InfoFile { get; set; }
        public string LogOnly { get; set; }

        //private static DsdiffProcessor _globalProcessor;

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            using (var runtime = new DsdiffCross())
            {
                try
                {
                    runtime.Init(args);
                }
                catch (Exception ex)
                {
                    File.WriteAllText("dsdiff_cross.error","Exception: " + ex.Message + "\n" +
                        ex.StackTrace);
                    Console.WriteLine("Exception: {0}", ex.Message);
                    "Exception: {0}"._Log(ex.Message);
                }
            }
        }

        public void Dispose()
        {

        }

        private void Init(string[] args)
        {
            InputFile = "";
            ConfigFile = "";
            OutputFile = "";
            InputLogFile = "";
            InfoFile = "";
            LogOnly = "";

            using (var moduleBase = new CmdModuleBase(true))
            {
                moduleBase.CommandLineOptions.AddRange(new[]
                    {
                        new CmdOptionDesc
                            {
                                Name = "input_log",
                                Description = "Save analysis log for input DSDIFF file",
                                Parameters =
                                    new[]
                                        {
                                            new Tuple<string, string>("name", "File path/name")
                                        },
                                HandleDelegate = new CmdParamToPropertyMapper(this, "InputLogFile").Handler,
                                Required = false
                            },
                        new CmdOptionDesc
                            {
                                Name = "file_info",
                                Description = "Save file info for input DSDIFF file",
                                Parameters =
                                    new[]
                                        {
                                            new Tuple<string, string>("name", "File path/name")
                                        },
                                HandleDelegate = new CmdParamToPropertyMapper(this, "InfoFile").Handler,
                                Required = false
                            },
                        new CmdOptionDesc
                            {
                                Name = "log_only",
                                Description = "Only write analytic logs without real processing",
                                Parameters =
                                    new[]
                                        {
                                            new Tuple<string, string>("option", "true/false value")
                                        },
                                HandleDelegate = new CmdParamToPropertyMapper(this, "LogOnly").Handler,
                                Required = false
                            },
                        new CmdOptionDesc
                            {
                                Name = "config",
                                Description = "Description of crossover's configuration (JSON format)",
                                Parameters =
                                    new[]
                                        {
                                            new Tuple<string, string>("name", "File path/name")
                                        },
                                HandleDelegate = new CmdParamToPropertyMapper(this, "ConfigFile").Handler,
                                Required = true
                            },
                        new CmdOptionDesc
                            {
                                Name = "input",
                                Description = "Input file with DSD stream inside (DSDIFF format)",
                                Parameters =
                                    new[]
                                        {
                                            new Tuple<string, string>("name", "File path/name")
                                        },
                                HandleDelegate = new CmdParamToPropertyMapper(this, "InputFile").Handler,
                                Required = true
                            },
                        new CmdOptionDesc
                            {
                                Name = "output",
                                Description = "Output file to store processed DSD data (DSDIFF format)",
                                Parameters =
                                    new[]
                                        {
                                            new Tuple<string, string>("name", "File path/name")
                                        },
                                HandleDelegate = new CmdParamToPropertyMapper(this, "OutputFile").Handler,
                                Required = true
                            }
                    });

                if (moduleBase.Parse(args) == false)
                {
                    return;
                }

                Process();
            }
        }

        public void Process()
        {
            Console.WriteLine("> Processing DSD file: {0}", Path.GetFileName(InputFile));

            try
            {
                if (InputLogFile != "")
                {
                    try
                    {
                        Console.WriteLine("> Writing log for input file into: {0}", InputLogFile);
                        var result = Analysis.GetFileResponse(InputFile, InfoFile, 1024);

                        var logOutFile = new StreamWriter(InputLogFile);

                        foreach (var r in result)
                            logOutFile.Write("{0};{1}\n", r.Item1, r.Item2);

                        logOutFile.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: Unable to write input file log - " + ex.Message + "\n" + ex.StackTrace);
                    }
                }

                if (LogOnly.ToLower() == "true") return;

                // Reader
                var inFileStream = File.Open(InputFile, FileMode.Open);

                var reader = new DsdiffReader(inFileStream);

                if (reader.CompressionType != "DSD ")
                    throw new Exception("Invalid compression type - only DSD files supported");

                if (reader.ChannelsCount != 2)
                    throw new Exception(
                        string.Format("Invalid number of channels in DSDIFF file: {0}. Only 2-channel files supported", 
                        reader.ChannelsCount));

                // Writer
                var outFileStream = File.Open(OutputFile, FileMode.Create);

                using (var filters = new DsdiffFilters(ConfigFile, 2822400))
                using (var writer = new DsdiffWriter(outFileStream, (ushort)filters.Count))
                using (var processor = new DsdiffProcessor(reader, writer, filters))
                {
                    //_globalProcessor = processor;
                    processor.Go();
                }

                //_globalProcessor = null;

                inFileStream.Close();
                inFileStream.Dispose();

                outFileStream.Close();
                outFileStream.Dispose();

                Console.WriteLine("Successfully shutted down\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }
    }
}
