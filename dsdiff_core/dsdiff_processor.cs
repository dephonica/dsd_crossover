using System;
using System.IO;
using System.Threading;

namespace dsdiff_cross
{
    class DsdiffProcessor : IDisposable
    {
        private readonly EventWaitHandle _terminateEvent = new EventWaitHandle(false, EventResetMode.ManualReset, "dsdiffCrossoverTerminateEvent");

        private readonly DsdiffReader _dsdiffReader;
        private readonly DsdiffWriter _dsdiffWriter;
        private readonly DsdiffFilters _dsdiffFilters;

        private readonly byte[][] _bitTranslateTable = new byte[256][];

        public DsdiffProcessor(DsdiffReader reader, DsdiffWriter writer, DsdiffFilters filters)
        {
            _dsdiffReader = reader;
            _dsdiffWriter = writer;
            _dsdiffFilters = filters;

            InitBitTranslator();
        }

        public void Dispose()
        {
            
        }

        private void InitBitTranslator()
        {
            for (var n = 0; n < 256; n++)
            {
                _bitTranslateTable[n] = new byte[8];

                for (int i = 0, mask = 128; i < 8; i++, mask /= 2)
                {
                    _bitTranslateTable[n][i] = (byte)((n & mask) > 0 ? 0 : 1);
                }
            }
        }

        public void TerminateCtrlC(object sender, ConsoleCancelEventArgs e)
        {
            if (e != null)
                e.Cancel = true;

            Console.Write("\n\nTerminating application\n");

            _terminateEvent.Set();
        }

        public void Go()
        {
            Console.CancelKeyPress += TerminateCtrlC;

            var channels = _dsdiffReader.ChannelsCount;
            var samplesPerChannel = (ulong)_dsdiffReader.SamplesPerChannel;

            var duration = TimeSpan.FromSeconds(samplesPerChannel*8/_dsdiffReader.SampleRate);
            Console.WriteLine("> File duration: {0}", duration);

            ////
            const int samplesBlockSize = 4096;

            var inputs = new double[channels][];
            for (var n = 0; n < channels; n++)
                inputs[n] = new double[samplesBlockSize * 8];

            var outputs = new double[_dsdiffFilters.Count][];
            for (var n = 0; n < _dsdiffFilters.Count; n++)
                outputs[n] = new double[samplesBlockSize * 8];

            var outputsDeltasigma = new byte[_dsdiffFilters.Count][];
            for (var n = 0; n < _dsdiffFilters.Count; n++)
                outputsDeltasigma[n] = new byte[samplesBlockSize];

            var deltaModulators = new Deltasigma[_dsdiffFilters.Count];
            for (var n = 0; n < _dsdiffFilters.Count; n++)
                deltaModulators[n] = new Deltasigma();

            var writtenDisplayTrigger = 0;

            for (var n = (ulong) _dsdiffReader.SamplesPosition;
                 n < samplesPerChannel;
                 n += samplesBlockSize)
            {
                if (_terminateEvent.WaitOne(0)) break;

                // Fill inputs with data
                for (var c = 0; c < channels; c++)
                {
                    var bitSamples = _dsdiffReader.GetSamplesBlock((long) n, c, samplesBlockSize);

                    for (var m = 0; m < samplesBlockSize; m++)
                    {
                        var bits = bitSamples[m];
                        var byteSamples = _bitTranslateTable[bits];

                        for (var j = 0; j < 8; j++)
                            inputs[c][m * 8 + j] = byteSamples[j] == 0 ? -1 : 1;
                    }
                }

                // Fill outputs with data and filter
                for (var c = 0; c < _dsdiffFilters.Count; c++)
                {
                    var source = _dsdiffFilters.GetFilterSource(c);
                    inputs[source].CopyTo(outputs[c], 0);
                    _dsdiffFilters.Process(c, outputs[c].Length, outputs[c]);

                    deltaModulators[c].Modulate(outputs[c], ref outputsDeltasigma[c]);
                }

                // Write output file
                _dsdiffWriter.Write(outputsDeltasigma);

                // Print progress
                writtenDisplayTrigger++;

                if (writtenDisplayTrigger > 100)
                    Console.Write("Written: {0} %\r", n * 100 / samplesPerChannel);
            }

            Console.CancelKeyPress -= TerminateCtrlC;

            _dsdiffWriter.GetStream().Seek(0, SeekOrigin.End);
            //_dsdiffReader.WriteSuffixToStream(_dsdiffWriter.GetStream());
            _dsdiffReader.ComposeSuffixToStream(_dsdiffWriter.GetStream());
        }
    }
}
