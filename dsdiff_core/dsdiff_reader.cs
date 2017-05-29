using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dsdiff_cross
{
    class DsdiffReader
    {
        private readonly Stream _inStream;

        private DsdChunk _frm8Chunk;

        private readonly List<DsdChunk> _chunks = new List<DsdChunk>();

        private int _channelsCountCached = 0;
        private Int64 _samplesSizeCached = 0, _samplesPosCached = 0;

        private Int64 _suffixPosition = 0;

        public DsdiffReader(Stream inStream)
        {
            _inStream = inStream;

            Parse();
        }

        private void Parse()
        {
            _frm8Chunk = new DsdChunk(_inStream);
            _frm8Chunk.MatchAndLoad(DsdChunk.IdType.FRM8);

            if (_frm8Chunk.FormTyped != DsdChunk.IdType.DSD)
                throw new Exception("DSDIFF File has invalid ID inside FRM8 chunk: " +
                    _frm8Chunk.FormTyped.ToString() + " instead of DSD");

            var remainingBytes = _frm8Chunk.CkDataSize;

            _inStream.Position = _frm8Chunk.DataPointer;

            while (remainingBytes > 4 + 8)
            {
                var chunk = new DsdChunk(_inStream);
                chunk.Load();
                _chunks.Add(chunk);

                if (chunk.CkId == DsdChunk.IdType.DSD)
                    _suffixPosition = _inStream.Position;

                remainingBytes -= 4 + 8;
                remainingBytes -= chunk.CkDataSize;
            }
        }

        public DsdChunk GetChunkById(DsdChunk.IdType id)
        {
            return _chunks.First(chunk => chunk.CkId == id);
        }

        public int ChannelsCount 
        {
            get
            {
                if (_channelsCountCached == 0)
                    _channelsCountCached = GetChunkById(DsdChunk.IdType.PROP).NumChannels;

                return _channelsCountCached;
            }
        }

        public UInt64 SampleRate
        {
            get { return GetChunkById(DsdChunk.IdType.PROP).SampleRate; }
        }

        public string CompressionType
        {
            get { return GetChunkById(DsdChunk.IdType.PROP).CompressionType; }
        }

        public Int64 SamplesPerChannel
        {
            get
            {
                _samplesSizeCached = GetChunkById(DsdChunk.IdType.DSD).CkDataSize;
                return _samplesSizeCached / ChannelsCount;
            }
        }

        public Int64 SamplesPosition
        {
            get 
            { 
                _samplesPosCached = GetChunkById(DsdChunk.IdType.DSD).DataPointer;
                return _samplesPosCached;
            }
        }

        public byte[] GetSamplesBlock(Int64 pos, int channel, Int64 size)
        {
            var samplesBuff = new byte[size];

            var channelsCount = ChannelsCount;

            var firstChannelPos = _samplesPosCached + pos*channelsCount;
            var indexedChannelPos = firstChannelPos + channel;

            // Set initial position
            _inStream.Position = indexedChannelPos;

            // Allocate buffer to read whole requested block for all channels
            var intermediateBuffer = new byte[size*channelsCount];
            _inStream.Read(intermediateBuffer, 0, intermediateBuffer.Length);

            var intermediatePosition = 0;

            for (var n = 0; n < size; n++)
            {
                samplesBuff[n] = intermediateBuffer[intermediatePosition];
                intermediatePosition += channelsCount;
            }

            return samplesBuff;
        }

        public void WriteSuffixToStream(Stream outStream)
        {
            _inStream.Position = _suffixPosition;

            var buff = new byte[_inStream.Length - _suffixPosition];
            _inStream.Read(buff, 0, buff.Length);
            outStream.Write(buff, 0, buff.Length);
        }

        public void ComposeSuffixToStream(Stream outStream)
        {
            WriteSuffixDiin(outStream);
            WriteSuffixComments(outStream);
            WriteId3(outStream);
        }

        private void WriteId3(Stream outStream)
        {
            var id3Source = GetChunkById(DsdChunk.IdType.ID3);

            if (id3Source != null && id3Source.ID3 != null)
            {
                DsdChunksContainer.WriteId(outStream, DsdChunk.IdType.ID3);
                DsdChunksContainer.WriteInt64(outStream, id3Source.ID3.Length);
                DsdChunksContainer.WriteBytes(outStream, id3Source.ID3);
            }
        }

        private void WriteSuffixDiin(Stream outStream)
        {
            var diinSource = GetChunkById(DsdChunk.IdType.DIIN);

            if (diinSource != null)
            {
                // Write DIIN
                var diinChunk = new DsdChunksContainer(DsdChunk.IdType.DIIN);

                // > MARK property
                // Ignore at this moment

                // > DIAR property
                if (!string.IsNullOrEmpty(diinSource.ArtistText))
                {
                    if ((diinSource.ArtistText.Length%2) != 0) diinSource.ArtistText += " ";

                    var diarChunk = new DsdChunksContainer(DsdChunk.DiinChunk.DIAR);
                    diarChunk.WriteInt32(diinSource.ArtistText.TrimEnd().Length);
                    diarChunk.WriteIdString(diinSource.ArtistText, diinSource.ArtistText.Length);
                    diinChunk.WriteChunk(diarChunk);
                }

                // > DITI property
                if (!string.IsNullOrEmpty(diinSource.TitleText))
                {
                    if ((diinSource.TitleText.Length % 2) != 0) diinSource.TitleText += " ";

                    var ditiChunk = new DsdChunksContainer(DsdChunk.DiinChunk.DITI);
                    ditiChunk.WriteInt32(diinSource.TitleText.TrimEnd().Length);
                    ditiChunk.WriteIdString(diinSource.TitleText, diinSource.TitleText.Length);
                    diinChunk.WriteChunk(ditiChunk);
                }

                // > EMID property
                if (!string.IsNullOrEmpty(diinSource.EMId))
                {
                    Console.WriteLine("EMID: " + diinSource.EMId + ", length: " + diinSource.EMId.Length);
                    Console.WriteLine("\n\n");
                    var emidChunk = new DsdChunksContainer(DsdChunk.DiinChunk.EMID);
                    emidChunk.WriteIdString(diinSource.EMId, diinSource.EMId.Length);
                    diinChunk.WriteChunk(emidChunk);
                }

                diinChunk.FlushToStream(outStream);
            }
        }

        private void PutCommentIntoChunk(DsdChunksContainer comtChunk, DsdChunk.DsdComment comment)
        {
            DsdChunksContainer.WriteUInt16(comtChunk.GetStream(), (ushort)comment.TimeStamp.Year);
            DsdChunksContainer.WriteUInt8(comtChunk.GetStream(), (byte)comment.TimeStamp.Month);
            DsdChunksContainer.WriteUInt8(comtChunk.GetStream(), (byte)comment.TimeStamp.Day);
            DsdChunksContainer.WriteUInt8(comtChunk.GetStream(), (byte)comment.TimeStamp.Hour);
            DsdChunksContainer.WriteUInt8(comtChunk.GetStream(), (byte)comment.TimeStamp.Minute);

            DsdChunksContainer.WriteUInt16(comtChunk.GetStream(), comment.Type);
            DsdChunksContainer.WriteUInt16(comtChunk.GetStream(), comment.Ref);

            if ((comment.Text.Length%2) != 0) comment.Text += " ";
            DsdChunksContainer.WriteInt32(comtChunk.GetStream(), comment.Text.Length);
            DsdChunksContainer.WriteIdString(comtChunk.GetStream(), comment.Text);
        }

        private void WriteSuffixComments(Stream outStream)
        {
            var comtChunk = new DsdChunksContainer(DsdChunk.IdType.COMT);
            var comtSource = GetChunkById(DsdChunk.IdType.COMT);

            var numComments = 3;
            if (comtSource != null) numComments += comtSource.Comments.Length;

            comtChunk.WriteUInt16((ushort)numComments);

            PutCommentIntoChunk(comtChunk, new DsdChunk.DsdComment
                {
                    Ref = 0,
                    Type = 3,
                    TimeStamp = DateTime.Now,
                    Text = "File was processed in DSDXOVER software crossover by dePhonica sound labs."
                });

            PutCommentIntoChunk(comtChunk, new DsdChunk.DsdComment
            {
                Ref = 0,
                Type = 3,
                TimeStamp = DateTime.Now,
                Text = "(C) 2010-" + DateTime.Now.Year + " dePhonica sound labs."
            });

            PutCommentIntoChunk(comtChunk, new DsdChunk.DsdComment
            {
                Ref = 0,
                Type = 3,
                TimeStamp = DateTime.Now,
                Text = "http://dephonica.com - dePhonica sound labs. official site"
            });

            if (comtSource != null)
            {
                foreach (var dsdComment in comtSource.Comments)
                    PutCommentIntoChunk(comtChunk, dsdComment);
            }

            comtChunk.FlushToStream(outStream);
        }
    }
}
