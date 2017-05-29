using System;
using System.Collections.Generic;
using System.IO;

namespace dsdiff_cross
{
    public class Analysis
    {
        public Analysis()
        {
        }

        public static void InitBitTranslateTable(ref byte[][] table)
        {
            for (var n = 0; n < 256; n++)
            {
                table[n] = new byte[8];

                for (int i = 0, mask = 128; i < 8; i++, mask /= 2)
                {
                    table[n][i] = (byte)((n & mask) > 0 ? 0 : 1);
                }
            }
        }

        public static List<Tuple<double, double>> GetFileResponse(string inputFile, string infoFile, int downsampleToPoints)
        {
            var bitTranslateTable = new byte[256][];
            InitBitTranslateTable(ref bitTranslateTable);

            var inFileStream = File.Open(inputFile, FileMode.Open);

            var reader = new DsdiffReader(inFileStream);

            if (reader.CompressionType != "DSD ")
                throw new Exception("Invalid compression type - only DSD files supported");

            var channels = reader.ChannelsCount;
            var samplesPerChannel = (ulong)reader.SamplesPerChannel;

            ////////////////////////////////////////////////////////
            try
            {
                var outText = "channels: " + channels +
                              "\nsamples_per_channel: " + samplesPerChannel +
                              "\nsamplerate: " + reader.SampleRate;
                File.WriteAllText(infoFile, outText);
            }
            catch {}
            ////////////////////////////////////////////////////////

            const int fftBlockSize = 512 * 1024;

            var inputs = new double[channels][];

            // Fill inputs with data
            for (var c = 0; c < channels; c++)
            {
                inputs[c] = new double[fftBlockSize * 8];

                var bitSamples = reader.GetSamplesBlock(
                    reader.SamplesPosition + (long)(samplesPerChannel / 2), c, fftBlockSize);

                for (var m = 0; m < fftBlockSize; m++)
                {
                    var bits = bitSamples[m];
                    var byteSamples = bitTranslateTable[bits];

                    for (var j = 0; j < 8; j++)
                        inputs[c][m*8 + j] = byteSamples[j] == 0 ? -1 : 1;
                }
            }

            inFileStream.Close();

            var targetFreq = new double[fftBlockSize*4+1];
            var targetPhase = new double[fftBlockSize * 4 + 1];
            var targetAverageFreq = new double[fftBlockSize*4+1];
            var targetAveragePhase = new double[fftBlockSize * 4 + 1];

            // Convert channels
            for (var c = 0; c < channels; c++)
            {
                FilterBackendWrap.FftForward(inputs[c], targetFreq, targetPhase, fftBlockSize*8);
                for (var n = 0; n < fftBlockSize*4; n++)
                {
                    targetAverageFreq[n] += targetFreq[n];
                    targetAveragePhase[n] += targetFreq[n];
                }
            }

            for (var n = 0; n < fftBlockSize*4; n++)
            {
                targetAverageFreq[n] /= channels;
                targetAveragePhase[n] /= channels;
            }

            // Downsample
            var preResult = new double[downsampleToPoints];

            var samplesPerPoint = fftBlockSize*4/downsampleToPoints;
            var waitForPoint = samplesPerPoint;

            var accFreq = 0.0;
            var accPhase = 0.0;
            var pointNumber = 0;

            for (var n = 0; n < fftBlockSize*4; n++)
            {
                accFreq += targetAverageFreq[n];
                accPhase += targetAveragePhase[n];

                if (n >= waitForPoint)
                {
                    waitForPoint += samplesPerPoint;

                    accFreq /= samplesPerPoint;
                    accPhase /= samplesPerPoint;

                    accFreq = Math.Sqrt(Math.Pow(accFreq, 2) + Math.Pow(accPhase, 2));
                    preResult[pointNumber++] = accFreq;

                    accFreq = 0.0;
                    accPhase = 0.0;
                }
            }

            accFreq /= samplesPerPoint;
            accPhase /= samplesPerPoint;
            accFreq = Math.Sqrt(Math.Pow(accFreq, 2) + Math.Pow(accPhase, 2));
            preResult[pointNumber] = accFreq;

            // Filter result
            var result = new List<Tuple<double, double>>();

            var average = 0.0;
            const int filterSize = 100;

            for (var n = 0; n < preResult.Length + filterSize; n++)
            {
                var value = 0.0;
                value = n >= preResult.Length ? preResult[preResult.Length - 1] : preResult[n];

                average += value;

                if (n >= filterSize)
                {
                    var output = average/filterSize;
                    result.Add(new Tuple<double, double>(n - filterSize, output));

                    average -= preResult[n - filterSize];
                }
            }

            return result;
        }
    }
}
