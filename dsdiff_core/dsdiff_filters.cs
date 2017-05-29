using System;
using System.Collections.Generic;
using System.Linq;
using Jayrock.Json;
using Jayrock.Json.Conversion;
using System.IO;

namespace dsdiff_cross
{
    class DsdiffFilters : IDisposable
    {
        private const int MaxFilterFreq = 96000;

        private readonly int _sampleRate;
        private readonly List<FilterInstance> _filterSet = new List<FilterInstance>();
        private readonly List<int> _channelSet = new List<int>();

        public int Count { get { return _channelSet.Count; } }

        public DsdiffFilters(string descFileName, int sampleRate)
        {
            _sampleRate = sampleRate;

            LoadConfiguration(descFileName);
        }

        public void Dispose()
        {
            foreach (var filterInstance in _filterSet)
            {
                filterInstance.Dispose();
            }
        }

        private void LoadConfiguration(string descFileName)
        {
            try
            {
                var jsonConfig = (JsonObject)JsonConvert.Import(File.ReadAllText(descFileName));

                foreach (var jsonChannel in ((JsonArray)jsonConfig["channels"]).Cast<JsonObject>())
                {
                    var source = jsonChannel["source"].ToString().ToLower() == "left" ? 0 : 1;
                    var filterJson = (JsonObject)jsonChannel["filter"];

                    FilterBackendWrap.FilterType filterType;
                    Enum.TryParse(filterJson["type"].ToString(), true, out filterType);

                    Console.WriteLine("Filter type: {0}", filterType);

                    var order = int.Parse(filterJson["order"].ToString());

                    if (filterType == FilterBackendWrap.FilterType.HighPass ||
                        filterType == FilterBackendWrap.FilterType.LowPass)
                    {
                        var freq = int.Parse(filterJson["freq"].ToString());

                        if (filterType == FilterBackendWrap.FilterType.LowPass)
                        {
                            var filter = new FilterInstance(FilterBackendWrap.FilterFamily.FftFir,
                                                            filterType, order, _sampleRate, freq, 0);

                            _filterSet.Add(filter);
                        }
                        else
                        {
                            var lofreq = freq;
                            var hifreq = MaxFilterFreq;

                            var width = hifreq - lofreq;
                            var center = lofreq + width / 2;

                            var filter = new FilterInstance(FilterBackendWrap.FilterFamily.FftFir,
                                                            FilterBackendWrap.FilterType.BandPass, 
                                                            order, _sampleRate, center, width);

                            _filterSet.Add(filter);
                        }
                    }
                    else
                    {
                        var lofreq = int.Parse(filterJson["lofreq"].ToString());
                        var hifreq = int.Parse(filterJson["hifreq"].ToString());

                        if (filterType == FilterBackendWrap.FilterType.BandPass)
                            hifreq = Math.Min(MaxFilterFreq, hifreq);

                        var width = hifreq - lofreq;
                        var center = lofreq + width/2;

                        var filter = new FilterInstance(FilterBackendWrap.FilterFamily.FftFir,
                                                        filterType, order, _sampleRate, center, width);

                        _filterSet.Add(filter);
                    }

                    _channelSet.Add(source);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in configuration file: {0}", ex.Message);
            }
        }

        public int GetFilterSource(int filterIndex)
        {
            return _channelSet[filterIndex];
        }

        public void Process(int filterIndex, int samplesCount, double[] samplesData)
        {
            _filterSet[filterIndex].Process(samplesCount, samplesData);
        }
    }
}
