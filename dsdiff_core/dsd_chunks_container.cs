using System;
using System.IO;
using System.Linq;
using System.Text;

namespace dsdiff_cross
{
    class DsdChunksContainer
    {
        private readonly Enum _containerType;

        private readonly MemoryStream _memStream = new MemoryStream();

        public DsdChunksContainer(Enum containerType)
        {
            _containerType = containerType;
        }

        public MemoryStream GetStream()
        {
            return _memStream;
        }

        public void FlushToStream(Stream outStream)
        {
            WriteId(outStream, _containerType);
            WriteInt64(outStream, _memStream.Length);

            _memStream.WriteTo(outStream);
        }

        public void WriteChunk(DsdChunksContainer chunk)
        {
            chunk.FlushToStream(_memStream);
        }

        public static void WriteBytes(Stream outStream, byte []buffer)
        {
            outStream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteIdString(Stream outStream, string idString, int textLen = 4)
        {
            while (idString.Length < textLen)
                idString += " ";

            var idBytes = Encoding.ASCII.GetBytes(idString);
            outStream.Write(idBytes, 0, idBytes.Length);
        }

        public static void WriteId(Stream outStream, Enum type)
        {
            WriteIdString(outStream, type.ToString());
        }

        public static void WriteId(Stream outStream, DsdChunk.IdType type)
        {
            WriteIdString(outStream, type.ToString());
        }

        public static void WritePropId(Stream outStream, DsdChunk.PropChunk idType)
        {
            WriteIdString(outStream, idType.ToString());
        }

        public static void WriteDiinId(Stream outStream, DsdChunk.DiinChunk idType)
        {
            WriteIdString(outStream, idType.ToString());
        }

        public static void WriteUInt8(Stream outStream, byte b)
        {
            var sizeBytes = new[] { b };
            outStream.Write(sizeBytes, 0, 1);
        }

        public static void WriteUInt16(Stream outStream, UInt16 v)
        {
            var sizeBytes = BitConverter.GetBytes(v);

            if (BitConverter.IsLittleEndian)
                sizeBytes = sizeBytes.Reverse().ToArray();

            outStream.Write(sizeBytes, 0, 2);
        }

        public static void WriteInt32(Stream outStream, Int32 v)
        {
            var sizeBytes = BitConverter.GetBytes(v);

            if (BitConverter.IsLittleEndian)
                sizeBytes = sizeBytes.Reverse().ToArray();

            outStream.Write(sizeBytes, 0, 4);
        }

        public static void WriteUInt32(Stream outStream, UInt32 v)
        {
            var sizeBytes = BitConverter.GetBytes(v);

            if (BitConverter.IsLittleEndian)
                sizeBytes = sizeBytes.Reverse().ToArray();

            outStream.Write(sizeBytes, 0, 4);
        }

        public static void WriteInt64(Stream outStream, Int64 v)
        {
            var sizeBytes = BitConverter.GetBytes(v);

            if (BitConverter.IsLittleEndian)
                sizeBytes = sizeBytes.Reverse().ToArray();

            outStream.Write(sizeBytes, 0, 8);
        }

//////////////////////////////////
        
        public void WriteIdString(string idString, int textLen = 4)
        {
            WriteIdString(_memStream, idString, textLen);
        }

        public void WriteId(Enum type)
        {
            WriteIdString(_memStream, type.ToString());
        }

        public void WriteUInt8(byte b)
        {
            WriteUInt8(_memStream, b);
        }

        public void WriteUInt16(UInt16 v)
        {
            WriteUInt16(_memStream, v);
        }

        public void WriteInt32(Int32 v)
        {
            WriteInt32(_memStream, v);
        }

        public void WriteUInt32(UInt32 v)
        {
            WriteUInt32(_memStream, v);
        }

        public void WriteInt64(Int64 v)
        {
            WriteInt64(_memStream, v);
        }
    }
}
