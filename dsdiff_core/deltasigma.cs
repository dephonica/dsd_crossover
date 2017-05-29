using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsdiff_cross
{
    class Deltasigma
    {
        private double _dsOutPrevious = 1;
        private double _adderValue1 = 0;
        private double _adderValue2 = 0;

        public void Modulate(double[] blockData, ref byte[] deltaSigmaData)
        {
            byte outByte = 0;
            var outPosition = 0;

            byte mask = 128;

            foreach (var v in blockData)
            {
                // Diff summ
                var diff = v - _dsOutPrevious;

                // Integrator
                _adderValue1 += diff;

                // Diff summ 2
                var diff2 = _adderValue1 - _dsOutPrevious;

                // Integrator 2
                _adderValue2 += diff2;

                // Comparator & 1-bit DAC
                if (_adderValue2 >= 0)
                {
                    //bit = 1;
                    _dsOutPrevious = 1;
                    outByte |= mask;
                }
                else
                {
                    //bit = 0;
                    _dsOutPrevious = -1;                    
                }

                if (mask == 1)
                {
                    // Write output byte
                    deltaSigmaData[outPosition] = outByte;
                    outPosition++;

                    mask = 128;
                    outByte = 0;
                } else
                    mask /= 2;
            }

            if (mask != 128)
                deltaSigmaData[outPosition] = outByte;
        }
    }
}
