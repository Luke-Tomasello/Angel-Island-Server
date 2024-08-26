/***************************************************************************
 *
 *   ZLR                     : May 1, 2007
 *   implementation          : (C) 2007-2023 Tara McGrew
 *   repository url          : https://foss.heptapod.net/zilf/zlr
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace ZLR.IFF
{
    [PublicAPI]
    public class IffFile
    {
        private uint formSubType;
        private readonly List<byte[]> blocks = new List<byte[]>();
        private readonly List<uint> types = new List<uint>();

        public IffFile([NotNull] string fileType)
        {
            formSubType = StringToTypeID(fileType);
        }

        public IffFile([NotNull] Stream fromStream)
        {
            ReadFromStream(fromStream);
        }

        [NotNull]
        public string FileType
        {
            get => TypeIDToString(formSubType);
            set => formSubType = StringToTypeID(value);
        }

        public int Length
        {
            get
            {
                // 12 bytes for FORM + file length + FORM sub-type
                var result = 12;

                // add up block lengths
                foreach (var block in blocks)
                {
                    // 8 bytes for type + length
                    result += 8;
                    // length of data
                    result += block.Length;
                    // padding byte for odd-length blocks
                    if (block.Length % 2 == 1)
                        result++;
                }

                return result;
            }
        }

        protected static uint StringToTypeID([NotNull] string type)
        {
            if (type.Length != 4)
                throw new ArgumentException("Wrong length for an IFF type", nameof(type));

            return (uint)(((byte)type[0] << 24) + ((byte)type[1] << 16) +
                          ((byte)type[2] << 8) + (byte)type[3]);
        }

        [NotNull]
        protected static string TypeIDToString(uint type)
        {
            var sb = new StringBuilder(4);

            sb.Append((char)(byte)(type >> 24));
            sb.Append((char)(byte)(type >> 16));
            sb.Append((char)(byte)(type >> 8));
            sb.Append((char)(byte)type);

            return sb.ToString();
        }

        public void AddBlock([NotNull] string type, [NotNull] byte[] data)
        {
            types.Add(StringToTypeID(type));
            blocks.Add(data);
        }

        [CanBeNull]
        public byte[]? GetBlock([NotNull] string type)
        {
            var index = types.IndexOf(StringToTypeID(type));
            return index == -1 ? null : blocks[index];
        }

        public async Task WriteToStreamAsync([NotNull] Stream stream, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // IFF header
            stream.Seek(0, SeekOrigin.Begin);
            stream.WriteByte((byte)'F');
            stream.WriteByte((byte)'O');
            stream.WriteByte((byte)'R');
            stream.WriteByte((byte)'M');

            // file length (not counting the IFF header or the length itself)
            var length = Length - 8;
            stream.WriteByte((byte)(length >> 24));
            stream.WriteByte((byte)(length >> 16));
            stream.WriteByte((byte)(length >> 8));
            stream.WriteByte((byte)length);

            // FORM sub-type
            stream.WriteByte((byte)(formSubType >> 24));
            stream.WriteByte((byte)(formSubType >> 16));
            stream.WriteByte((byte)(formSubType >> 8));
            stream.WriteByte((byte)formSubType);

            cancellationToken.ThrowIfCancellationRequested();

            // block data
            var sortedBlocks = new int[blocks.Count];
            for (var i = 0; i < sortedBlocks.Length; i++)
                sortedBlocks[i] = i;

            Array.Sort(sortedBlocks, (a, b) => CompareBlocks((types[a], blocks[a], a), (types[b], blocks[b], b)));

            foreach (var i in sortedBlocks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // block type
                var type = types[i];
                stream.WriteByte((byte)(type >> 24));
                stream.WriteByte((byte)(type >> 16));
                stream.WriteByte((byte)(type >> 8));
                stream.WriteByte((byte)type);

                // block data length
                var block = blocks[i];
                length = blocks[i].Length;
                stream.WriteByte((byte)(length >> 24));
                stream.WriteByte((byte)(length >> 16));
                stream.WriteByte((byte)(length >> 8));
                stream.WriteByte((byte)length);

                // block data
                await stream.WriteAsync(block, 0, length, cancellationToken).ConfigureAwait(false);

                // padding
                if (length % 2 == 1)
                    stream.WriteByte(0);
            }
        }

        // no sorting by default
        protected virtual int CompareBlocks((uint type, byte[] data, int index) block1,
            (uint type, byte[] data, int index) block2) => block1.index.CompareTo(block2.index);

        // load all blocks by default
        protected virtual bool WantBlock(uint type) => true;

        protected void ReadFromStream([NotNull] Stream stream)
        {
            types.Clear();
            blocks.Clear();

            stream.Seek(0, SeekOrigin.Begin);

            var br = new BinaryReader(stream);

            // IFF header
            if (br.ReadByte() != 'F' || br.ReadByte() != 'O' ||
                br.ReadByte() != 'R' || br.ReadByte() != 'M')
                throw new ArgumentException("Incorrect IFF header (FORM)", nameof(stream));

            // file length
            var fileLength = (br.ReadByte() << 24) + (br.ReadByte() << 16) +
                             (br.ReadByte() << 8) + br.ReadByte();

            fileLength += 8;

            // FORM sub-type
            formSubType = (uint)((br.ReadByte() << 24) + (br.ReadByte() << 16) +
                                 (br.ReadByte() << 8) + br.ReadByte());

            // blocks
            while (stream.Position < fileLength)
            {
                var typeID = (uint)((br.ReadByte() << 24) + (br.ReadByte() << 16) +
                                    (br.ReadByte() << 8) + br.ReadByte());

                var blockLength = (br.ReadByte() << 24) + (br.ReadByte() << 16) +
                                  (br.ReadByte() << 8) + br.ReadByte();

                if (WantBlock(typeID))
                {
                    // load it into memory
                    var block = br.ReadBytes(blockLength);
                    AddBlock(TypeIDToString(typeID), block);
                }
                else
                {
                    // skip it
                    stream.Seek(blockLength, SeekOrigin.Current);
                }

                // skip padding
                if (blockLength % 2 == 1)
                    br.ReadByte();
            }
        }
    }

    public class Blorb : IffFile
    {
        // IFF block types
        private const string BLORB_TYPE = "IFRS";
        private static readonly uint RIDX_TYPE_ID = StringToTypeID("RIdx");

        // Blorb resource types
        private static readonly uint EXEC_USAGE_ID = StringToTypeID("Exec");

        private struct Resource
        {
            public uint Usage;
            public uint Number;
            public uint Offset;
        }

        private readonly Stream stream;
        private readonly Resource[] resources;

        /// <summary>
        /// Initializes a new Blorb reader from a stream. The stream must be kept open
        /// while the Blorb reader is in use.
        /// </summary>
        /// <param name="fromStream">The stream to read.</param>
        public Blorb([NotNull] Stream fromStream)
            : base(fromStream)
        {
            if (FileType != BLORB_TYPE)
                throw new ArgumentException("Not a Blorb file", nameof(fromStream));

            stream = fromStream;

            var ridx = GetBlock("RIdx");
            if (ridx == null)
                throw new ArgumentException("Blorb file contains no resource index", nameof(fromStream));

            // load resource index
            var count = (ridx[0] << 24) + (ridx[1] << 16) + (ridx[2] << 8) + ridx[3];
            resources = new Resource[count];
            for (var i = 0; i < count; i++)
            {
                var pos = 4 + i * 12;
                resources[i].Usage = (uint)((ridx[pos] << 24) + (ridx[pos + 1] << 16) + (ridx[pos + 2] << 8) + ridx[pos + 3]);
                resources[i].Number = (uint)((ridx[pos + 4] << 24) + (ridx[pos + 5] << 16) + (ridx[pos + 6] << 8) + ridx[pos + 7]);
                resources[i].Offset = (uint)((ridx[pos + 8] << 24) + (ridx[pos + 9] << 16) + (ridx[pos + 10] << 8) + ridx[pos + 11]);
            }
        }

        // only load the resource index
        protected override bool WantBlock(uint type) => type == RIDX_TYPE_ID;

        protected override int CompareBlocks((uint type, byte[] data, int index) block1, (uint type, byte[] data, int index) block2)
        {
            // make sure RIdx is first, but leave other blocks in order
            if (block1.type == RIDX_TYPE_ID && block2.type != RIDX_TYPE_ID)
                return -1;
            if (block2.type == RIDX_TYPE_ID && block1.type != RIDX_TYPE_ID)
                return 1;
            return block1.index.CompareTo(block2.index);
        }

        [NotNull]
        private byte[] ReadBlock(uint offset, uint length)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            var result = new byte[length];
            var actual = stream.Read(result, 0, (int)length);
            if (actual < length)
                throw new Exception("Block ran past end of file");
            return result;
        }

#if HAVE_SPAN
        private void ReadSpan(uint offset, Span<byte> span)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            var actual = stream.Read(span);
            if (actual < span.Length)
                throw new Exception("Block ran past end of file");
        }
#endif

        private Resource? FindResource(uint usage, uint? num)
        {
            for (var i = 0; i < resources.Length; i++)
            {
                if (resources[i].Usage != usage)
                    continue;

                if (num == null || resources[i].Number == num.Value)
                    return resources[i];
            }

            return null;
        }

        /// <summary>
        /// Determines the type of the story file contained in this Blorb.
        /// </summary>
        /// <returns>A four-character string identifying the story file type,
        /// or null if no story resource is present.</returns>
        [CanBeNull]
        public string? GetStoryType()
        {
            var storyRes = FindResource(EXEC_USAGE_ID, null);

            if (storyRes == null)
                return null;

#if HAVE_SPAN
            return string.Create<object?>(4, null, (span, _) =>
            {
                Span<byte> typeBuffer = stackalloc byte[4];
                ReadSpan(storyRes.Value.Offset, typeBuffer);
                for (int i = 0; i < 4; i++)
                    span[i] = (char)typeBuffer[i];
            });
#endif
#if !HAVE_SPAN
            var type = ReadBlock(storyRes.Value.Offset, 4);
            var sb = new StringBuilder(4);
            sb.Append((char)type[0]);
            sb.Append((char)type[1]);
            sb.Append((char)type[2]);
            sb.Append((char)type[3]);
            return sb.ToString();
#endif
        }

        /// <summary>
        /// Obtains a stream for the story file data in this Blorb.
        /// </summary>
        /// <exception cref="InvalidOperationException">No story resource is present.</exception>
        /// <returns>A stream containing the story file data, or null if no
        /// story resource is present.</returns>
        public Stream GetStoryStream()
        {
            var storyRes = FindResource(EXEC_USAGE_ID, null);

            if (storyRes == null)
                throw new InvalidOperationException("No story resource is present");

            var lenBytes = ReadBlock(storyRes.Value.Offset + 4, 4);
            var len = (uint)((lenBytes[0] << 24) + (lenBytes[1] << 16) + (lenBytes[2] << 8) + lenBytes[3]);

            return new SubStream(stream, storyRes.Value.Offset + 8, len);
        }
    }

    internal class SubStream : Stream
    {
        private readonly Stream baseStream;
        private readonly long streamOffset, length;
        private long position;

        public SubStream([NotNull] Stream baseStream, long streamOffset, long length)
        {
            if (!baseStream.CanSeek)
                throw new ArgumentException("Base stream must be seekable", nameof(baseStream));

            this.baseStream = baseStream;
            this.streamOffset = streamOffset;
            this.length = length;
            position = 0;
        }

        public override bool CanRead => baseStream.CanRead;

        public override bool CanSeek => true;

        public override bool CanWrite => baseStream.CanWrite;

        public override void Flush()
        {
            baseStream.Flush();
        }

        public override long Length => length;

        public override long Position
        {
            get => position;
            set
            {
                if (value < 0 || value >= length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position + count > length)
                count = (int)(length - position);
            
            baseStream.Position = streamOffset + position;
            return baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                    break;

                case SeekOrigin.Current:
                    position += offset;
                    break;

                case SeekOrigin.End:
                    position = length + offset;
                    break;
            }

            if (position < 0)
                position = 0;
            else if (position > length)
                position = length;

            return position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (position + count > length)
                count = (int)(length - position);

            baseStream.Position = streamOffset + position;
            baseStream.Write(buffer, offset, count);
        }
    }
}