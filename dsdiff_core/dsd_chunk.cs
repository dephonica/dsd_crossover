using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace dsdiff_cross
{
    public class DsdChunk
    {
        private const bool DisplayInfo = false;

        public class DsdComment
        {
            public DateTime TimeStamp;
            public UInt16 Type;
            public UInt16 Ref;
            public string Text;
        };

        private readonly Stream _inStream;

        public enum IdType { FRM8, DSD, FVER, PROP, COMT, DST, DSTI, DIIN, MANF, SND, ID3, NONE };
        public enum PropChunk { FS, CHNL, CMPR, ABSS, LSCO, DSD, ID3 };
        public enum DiinChunk { EMID, DIAR, DITI, MANF, MARK };

        public Int64 SelfPointer, DataPointer;

        public IdType CkId;
        public Int64 CkDataSize;

        // FRM8 Chunk
        public IdType FormTyped;

        // FVER
        public string Version;

        // COMT
        public int NumComments;
        public DsdComment[] Comments;

        // DIIN
        //   EMID
        public string EMId;
        //   DIAR
        public string ArtistText;
        //   DITI
        public string TitleText;

        // PROP - FS
        public UInt64 SampleRate;

        // PROP - CHNL
        public int NumChannels;
        public string[] ChId;

        // PROP - CMPR
        public string CompressionType;
        public string CompressionName;

        // PROP - ABSS
        public int StartHours;
        public int StartMinutes;
        public int StartSeconds;
        public UInt64 StartSamples;

        // PROP - LSCO
        public int SpeakersConfig;

        // ID3 data
        public byte[] ID3;

        /// ///////////////

        public DsdChunk(Stream inStream)
        {
            _inStream = inStream;
        }

        public void Load()
        {
            MatchAndLoad(IdType.NONE);
        }

        public void MatchAndLoad(IdType chunkType)
        {
            if (!_inStream.CanSeek && chunkType != IdType.NONE)
                throw new Exception("Unable to use MatchAndLoad on stream, that can't seek");

            SelfPointer = _inStream.Position;

            // Read chunk id
            CkId = ReadId();

            if (CkId != chunkType && chunkType != IdType.NONE)
            {
                _inStream.Seek(SelfPointer, SeekOrigin.Begin);
                throw new Exception("Expected chunk type " + chunkType + " instead of " + CkId);
            }

            CkDataSize = ReadInt64();
            if (DisplayInfo)
                Console.WriteLine("Readed chunk with type {0} and size {1}", CkId, CkDataSize);

            ParseChunk();

            DataPointer = _inStream.Position;
            _inStream.Position = SelfPointer + CkDataSize + 12;

            if (DisplayInfo)
                Console.WriteLine();
        }

        private void ParseChunk()
        {
            if (CkId == IdType.FRM8)
            {
                FormTyped = ReadId();
                if (DisplayInfo)
                    Console.WriteLine("FRM8: Form type: " + FormTyped);
            }
            else if (CkId == IdType.FVER)
            {
                Version = _inStream.ReadByte().ToString(CultureInfo.InvariantCulture) + "." +
                          _inStream.ReadByte().ToString(CultureInfo.InvariantCulture) + "." +
                          _inStream.ReadByte().ToString(CultureInfo.InvariantCulture) + "." +
                          _inStream.ReadByte().ToString(CultureInfo.InvariantCulture);

                if (DisplayInfo)
                    Console.WriteLine("File version: {0}", Version);
            }
            else if (CkId == IdType.PROP)
            {
                var id = ReadId();
                if (id != IdType.SND)
                    throw new Exception("Invalid property chunk in DSDIFF file");

                ParseProperties();
            }
            else if (CkId == IdType.COMT)
            {
                ParseComments();
            }
            else if (CkId == IdType.DIIN)
            {
                ParseMasterInfo();
            }
            else if (CkId == IdType.ID3)
            {
                ID3 = ReadBytes((int)CkDataSize);

                if (DisplayInfo)
                    Console.Write("ID3 tag was readed, len: " + CkDataSize);
            }
        }

        private void ParseMasterInfo()
        {
            var remainingBytes = CkDataSize;

            while (remainingBytes > 0)
            {
                var position = _inStream.Position;

                var chunkId = ReadDiinId();
                var dataSize = ReadInt64();

                if (chunkId == DiinChunk.EMID)
                {
                    EMId = ReadIdString((int)dataSize);
                    if (DisplayInfo)
                        Console.WriteLine("EMID: {0}", EMId);
                }
                else if (chunkId == DiinChunk.MARK)
                {
                    // Ignore markers
                }
                else if (chunkId == DiinChunk.DIAR)
                {
                    var artistSize = ReadInt32();
                    ArtistText = ReadIdString(artistSize);
                    if (DisplayInfo)
                        Console.WriteLine("Artist: {0}", ArtistText);
                }
                else if (chunkId == DiinChunk.DITI)
                {
                    var titleSize = ReadInt32();
                    TitleText = ReadIdString(titleSize);
                    if (DisplayInfo)
                        Console.WriteLine("Title: {0}", TitleText);
                }
                else
                {
                    if (DisplayInfo)
                        Console.WriteLine("DIIN chunk not handled: {0}", chunkId);
                }

                _inStream.Position = position + dataSize + 12;
                remainingBytes -= dataSize + 12;
            }
        }

        private void ParseComments()
        {
            NumComments = ReadUInt16();
            Comments = new DsdComment[NumComments];

            if (DisplayInfo)
                Console.WriteLine("Comments in DSDIFF file: {0}", NumComments);

            for (var n = 0; n < NumComments; n++)
            {
                var year = ReadUInt16();
                var month = ReadUInt8() + 1;
                var day = ReadUInt8();
                var hour = ReadUInt8();
                var minute = ReadUInt8();

                if (DisplayInfo)
                    Console.WriteLine("Date info: {0}-{1}-{2} {3}:{4}:{5}",
                        year, month, day, hour, minute, 0);

                var commentTimestamp = new DateTime(year, month, day, hour, minute, 0);

                var commentType = ReadUInt16();
                var commentRef = ReadUInt16();

                var textLength = ReadInt32();

                var nextPosition = _inStream.Position + textLength;
                if (textLength % 2 != 0) nextPosition++;

                var text = ReadIdString((int)textLength);

                Comments[n] = new DsdComment
                    {
                        TimeStamp = commentTimestamp,
                        Type = commentType,
                        Ref = commentRef,
                        Text = text
                    };

                if (DisplayInfo)
                    Console.WriteLine("{0} - {1}", Comments[n].TimeStamp.ToLongDateString(), Comments[n].Text);

                _inStream.Position = nextPosition;
            }
        }

        private void ParseProperties()
        {
            var remainingBytes = CkDataSize;

            while (remainingBytes >= 8 + 4)
            {
                var propPosition = _inStream.Position;

                var id = ReadPropId();
                var dataSize = ReadInt64();

                if (id == PropChunk.ID3)
                {
                    ID3 = ReadBytes((int)dataSize);

                    if (DisplayInfo)
                        Console.Write("PROP.ID3 tag was readed, len: " + dataSize);
                }
                else if (id == PropChunk.FS)
                {
                    SampleRate = ReadUInt32();

                    if (DisplayInfo)
                        Console.WriteLine("Stream sample rate: {0}", SampleRate);
                }
                else if (id == PropChunk.CHNL)
                {
                    NumChannels = ReadUInt16();
                    ChId = new string[NumChannels];

                    for (var n = 0; n < NumChannels; n++)
                        ChId[n] = ReadIdString();

                    if (DisplayInfo)
                        Console.WriteLine("Channels ({0}): {1}", NumChannels, string.Join(",", ChId));
                }
                else if (id == PropChunk.CMPR)
                {
                    CompressionType = ReadIdString();

                    var nameSize = ReadUInt8();
                    CompressionName = ReadIdString(nameSize);

                    if (DisplayInfo)
                        Console.WriteLine("Compression type: {0}, name: {1}", CompressionType, CompressionName);
                }
                else if (id == PropChunk.LSCO)
                {
                    SpeakersConfig = ReadUInt16();

                    var speakSetup = "Reserved for future use";
                    if (SpeakersConfig == 0)
                        speakSetup = "2-channel stereo set-up";
                    else if (SpeakersConfig == 3)
                        speakSetup = "5-channel set-up";
                    else if (SpeakersConfig == 4)
                        speakSetup = "6-channel set-up";
                    else if (SpeakersConfig == 65535)
                        speakSetup = "Undefined set-up";

                    if (DisplayInfo)
                        Console.WriteLine("Speakers setup: {0}", speakSetup);
                }
                else
                {
                    if (DisplayInfo)
                        Console.WriteLine("Property tag: {0}, data size: {1}, remaining bytes: {2}",
                            id, dataSize, remainingBytes);
                }

                remainingBytes -= dataSize + 12;

                _inStream.Position = propPosition + dataSize + 12;
            }
        }

        private byte[] ReadBytes(int byteLen)
        {
            var idBytes = new byte[byteLen];
            _inStream.Read(idBytes, 0, byteLen);

            return idBytes;
        }

        private string ReadIdString(int textLen = 4)
        {
            return Encoding.ASCII.GetString(ReadBytes(textLen));
        }

        private IdType ReadId()
        {
            var idString = ReadIdString();

            IdType idType;
            if (!Enum.TryParse(idString, true, out idType))
                throw new Exception("Unable to parse DSDIFF Tag: " + idString);

            return idType;
        }

        private PropChunk ReadPropId()
        {
            var idString = ReadIdString();

            PropChunk idType;
            if (!Enum.TryParse(idString, true, out idType))
                throw new Exception("Unable to parse PROP Tag: " + idString);

            return idType;
        }

        private DiinChunk ReadDiinId()
        {
            var idString = ReadIdString();

            DiinChunk idType;
            if (!Enum.TryParse(idString, true, out idType))
                throw new Exception("Unable to parse DIIN Tag: " + idString);

            return idType;
        }

        private byte ReadUInt8()
        {
            var sizeBytes = new byte[1];
            _inStream.Read(sizeBytes, 0, 1);

            return sizeBytes[0];
        }

        private UInt16 ReadUInt16()
        {
            var sizeBytes = new byte[2];
            _inStream.Read(sizeBytes, 0, 2);
            if (BitConverter.IsLittleEndian)
                sizeBytes = sizeBytes.Reverse().ToArray();

            return BitConverter.ToUInt16(sizeBytes, 0);
        }

        private Int32 ReadInt32()
        {
            var sizeBytes = new byte[4];
            _inStream.Read(sizeBytes, 0, 4);
            if (BitConverter.IsLittleEndian)
                sizeBytes = sizeBytes.Reverse().ToArray();

            return BitConverter.ToInt32(sizeBytes, 0);
        }

        private UInt64 ReadUInt32()
        {
            var sizeBytes = new byte[4];
            _inStream.Read(sizeBytes, 0, 4);
            if (BitConverter.IsLittleEndian)
                sizeBytes = sizeBytes.Reverse().ToArray();

            return BitConverter.ToUInt32(sizeBytes, 0);
        }

        private Int64 ReadInt64()
        {
            var sizeBytes = new byte[8];
            _inStream.Read(sizeBytes, 0, 8);
            if (BitConverter.IsLittleEndian)
                sizeBytes = sizeBytes.Reverse().ToArray();

            return BitConverter.ToInt64(sizeBytes, 0);
        }
    }
}
