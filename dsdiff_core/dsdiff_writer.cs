using System;
using System.IO;
using System.Linq;
using System.Text;

namespace dsdiff_cross
{
    class DsdiffWriter : IDisposable
    {
        private Int64 _totalDataSize = 0;
        private readonly Stream _outStream;
        private readonly ushort _channelsCount;

        private long _dsdLengthPosition = 0;
        private long _dsdDataLength = 0;

        private byte[] _interleavedBlock = null;

        public DsdiffWriter(Stream outStream, ushort channelsCount)
        {
            _outStream = outStream;
            _channelsCount = channelsCount;

            WriteHeader();
        }

        public void Dispose()
        {
            WriteFooter();
        }

        public Stream GetStream()
        {
            return _outStream;
        }

        private void WriteHeader()
        {
            // DSDIFF signature
            DsdChunksContainer.WriteId(_outStream, DsdChunk.IdType.FRM8);
            DsdChunksContainer.WriteInt64(_outStream, 0);  // Will write correct data size later
            DsdChunksContainer.WriteId(_outStream, DsdChunk.IdType.DSD);

            // File version
            DsdChunksContainer.WriteId(_outStream, DsdChunk.IdType.FVER);
            DsdChunksContainer.WriteInt64(_outStream, 4);
            DsdChunksContainer.WriteUInt32(_outStream, 0x01050000);

            // Properties composed chunk
            var propChunk = new DsdChunksContainer(DsdChunk.IdType.PROP);
            propChunk.WriteId(DsdChunk.IdType.SND);

            // > FS - frequency
            var fsChunk = new DsdChunksContainer(DsdChunk.PropChunk.FS);
            fsChunk.WriteUInt32(2822400);

            // > CHNL - channels
            var chnlChunk = new DsdChunksContainer(DsdChunk.PropChunk.CHNL);
            chnlChunk.WriteUInt16(_channelsCount);

            var chnlId = new[] {"SLFT", "SRGT", "MLFT", "MRGT", "LS", "RS", "C", "LFE", 
                "C000", "C001", "C002", "C003", "C004", "C005", "C006", "C007", "C008", "C009"};

            for (var n = 0; n < _channelsCount; n++)
                chnlChunk.WriteIdString(chnlId[n]);

            // > CMPR - compression
            var cmprChunk = new DsdChunksContainer(DsdChunk.PropChunk.CMPR);
            cmprChunk.WriteIdString("DSD");

            const string compressorName = "not compressed ";
            cmprChunk.WriteUInt8((byte)compressorName.Length);
            cmprChunk.WriteIdString(compressorName);

            // > ABSS - absolute start time chunk
            var abssChunk = new DsdChunksContainer(DsdChunk.PropChunk.ABSS);
            abssChunk.WriteUInt16(0);
            abssChunk.WriteUInt8(0);
            abssChunk.WriteUInt8(0);
            abssChunk.WriteUInt32(0);

            // > LSCO - loudspeaker configuration
            var lscoChunk = new DsdChunksContainer(DsdChunk.PropChunk.LSCO);
            lscoChunk.WriteUInt16((ushort)(_channelsCount <= 2 ? 0 : 4));

            // << Write all property chunks info container
            propChunk.WriteChunk(fsChunk);
            propChunk.WriteChunk(chnlChunk);
            propChunk.WriteChunk(cmprChunk);
            //propChunk.WriteChunk(abssChunk);
            propChunk.WriteChunk(lscoChunk);

            // Write property chunk info the file
            propChunk.FlushToStream(_outStream);

            // Begin DSD section
            DsdChunksContainer.WriteId(_outStream, DsdChunk.IdType.DSD);
            _dsdLengthPosition = _outStream.Position;
            DsdChunksContainer.WriteInt64(_outStream, 0);
        }

        private void WriteFooter()
        {
            var length = _outStream.Length - 12;

            _outStream.Seek(_dsdLengthPosition, SeekOrigin.Begin);
            DsdChunksContainer.WriteInt64(_outStream, _dsdDataLength);

            _outStream.Seek(4, SeekOrigin.Begin);
            DsdChunksContainer.WriteInt64(_outStream, length);
        }

        public void Write(byte[][] channelsData)
        {
            var channels = channelsData.Length;
            var samples = channelsData[0].Length;

            if (_interleavedBlock == null)
                _interleavedBlock = new byte[channels * samples];

            for (var c = 0; c < channels; c++)
            {
                var position = c;

                for (var s = 0; s < samples; s++, position += channels)
                {
                    _interleavedBlock[position] = channelsData[c][s];
                }
            }

            _outStream.Write(_interleavedBlock, 0, _interleavedBlock.Length);

            _dsdDataLength += _interleavedBlock.Length;
        }
    }
}
