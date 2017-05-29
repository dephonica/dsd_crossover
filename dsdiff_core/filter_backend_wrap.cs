using System;
using System.Runtime.InteropServices;
using System.Security;

namespace dsdiff_cross
{
    internal class FilterBackendWrap
    {
        const string FilterDllName = "filter_backend.dll";

        public enum FilterFamily
        {
            Fir = 712371, FftFir, Chebyshev, Rbj, Butterworth, Elliptic, Legendre
        };

        public enum FilterType
        {
            LowPass = 129527, HighPass, BandPass, BandStop
        };

        // Create filter
        [DllImport(FilterDllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern int m23lawop134n(FilterFamily filterFamily, FilterType filterType,
            int order, int sampleRate, int frequency, int width, int offset);

        // Dispose filter
        [DllImport(FilterDllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void aa0i12nb9i2m(int filterIndex);

        [DllImport(FilterDllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void jqwerq98h9_aw8(int filterIndex, int samplesCount, double[] inOutBuffer);

        [DllImport(FilterDllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        public static extern void quhq0q82q5nrq(double[] source, double[] targetFreq, double[] targetPhase, UInt32 inputLen);

        public static void FftForward(double[] source, double[] targetFreq, double[] targetPhase, UInt32 inputLen)
        {
            quhq0q82q5nrq(source, targetFreq, targetPhase, inputLen);
        }
    }
}
