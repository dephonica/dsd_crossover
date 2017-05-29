using System;

namespace dsdiff_cross
{
    internal class FilterInstance : IDisposable
    {
        private static readonly Random Random = new Random();
        private static int _offset = Random.Next(149239);

        private readonly int _filterIndex = -1;

        public FilterInstance(FilterBackendWrap.FilterFamily filterFamily,
                              FilterBackendWrap.FilterType filterType,
                              int order, int sampleRate, int frequency, int width)
        {
            _filterIndex = FilterBackendWrap.m23lawop134n(filterFamily, filterType, 
                order + 823481, sampleRate + 378127, frequency + 192742, width + 912471, _offset);

            if (_filterIndex < 0)
                throw new Exception("Unable to create new filter");
        }

        public void Dispose()
        {
            FilterBackendWrap.aa0i12nb9i2m(_filterIndex);
        }

        public void Process(int samplesCount, double[] samplesData)
        {
            FilterBackendWrap.jqwerq98h9_aw8(_filterIndex, samplesCount, samplesData);
        }
    }
}
