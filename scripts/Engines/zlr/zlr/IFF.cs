/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
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
using System.IO;
using System.Text;

namespace ZLR.IFF
{
    public class IffFile
    {
        private uint formSubType;
        private List<byte[]> blocks = new List<byte[]>();
        private List<uint> types = new List<uint>();

        public IffFile(string fileType)
        {
            formSubType = StringToTypeID(fileType);
        }

        public IffFile(Stream fromStream)
        {
            ReadFromStream(fromStream);
        }

        public string FileType
        {
            get { return TypeIDToString(formSubType); }
            set { formSubType = StringToTypeID(value); }
        }

        public int Length
        {
            get
            {
                // 12 bytes for FORM + file length + FORM sub-type
                int result = 12;

                // add up block lengths
                foreach (byte[] block in blocks)
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

        protected static uint StringToTypeID(string type)
        {
            if (type.Length != 4)
                throw new ArgumentException("Wrong length for an IFF type");

            return (uint)(((byte)type[0] << 24) + ((byte)type[1] << 16) +
                          ((byte)type[2] << 8) + (byte)type[3]);
        }

        protected static string TypeIDToString(uint type)
        {
            StringBuilder sb = new StringBuilder(4);

            sb.Append((char)(byte)(type >> 24));
            sb.Append((char)(byte)(type >> 16));
            sb.Append((char)(byte)(type >> 8));
            sb.Append((char)(byte)type);

            return sb.ToString();
        }

        public void AddBlock(string type, byte[] data)
        {
            types.Add(StringToTypeID(type));
            blocks.Add(data);
        }

        public byte[] GetBlock(string type)
        {
            int index = types.IndexOf(StringToTypeID(type));
            if (index == -1)
                return null;
            else
                return blocks[index];
        }

        public void WriteToStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            // IFF header
            stream.WriteByte((byte)'F');
            stream.WriteByte((byte)'O');
            stream.WriteByte((byte)'R');
            stream.WriteByte((byte)'M');

            // file length (not counting the IFF header or the length itself)
            int length = this.Length - 8;
            stream.WriteByte((byte)(length >> 24));
            stream.WriteByte((byte)(length >> 16));
            stream.WriteByte((byte)(length >> 8));
            stream.WriteByte((byte)length);

            // FORM sub-type
            stream.WriteByte((byte)(formSubType >> 24));
            stream.WriteByte((byte)(formSubType >> 16));
            stream.WriteByte((byte)(formSubType >> 8));
            stream.WriteByte((byte)formSubType);

            // block data
            int[] sortedBlocks = new int[blocks.Count];
            for (int i = 0; i < sortedBlocks.Length; i++)
                sortedBlocks[i] = i;

            Array.Sort(sortedBlocks, delegate (int a, int b)
            {
                return CompareBlocks(types[a], types[b], blocks[a], blocks[b], a, b);
            });

            foreach (int i in sortedBlocks)
            {
                // block type
                uint type = types[i];
                stream.WriteByte((byte)(type >> 24));
                stream.WriteByte((byte)(type >> 16));
                stream.WriteByte((byte)(type >> 8));
                stream.WriteByte((byte)type);

                // block data length
                byte[] block = blocks[i];
                length = blocks[i].Length;
                stream.WriteByte((byte)(length >> 24));
                stream.WriteByte((byte)(length >> 16));
                stream.WriteByte((byte)(length >> 8));
                stream.WriteByte((byte)length);

                // block data
                stream.Write(block, 0, length);

                // padding
                if (length % 2 == 1)
                    stream.WriteByte(0);
            }
        }

        protected virtual int CompareBlocks(uint type1, uint type2, byte[] data1, byte[] data2,
            int index1, int index2)
        {
            // no sorting by default
            return index1.CompareTo(index2);
        }

        protected virtual bool WantBlock(uint type)
        {
            // load all blocks by default
            return true;
        }

        protected void ReadFromStream(Stream stream)
        {
            types.Clear();
            blocks.Clear();

            stream.Seek(0, SeekOrigin.Begin);

            BinaryReader br = new BinaryReader(stream);

            // IFF header
            if (br.ReadByte() != 'F' || br.ReadByte() != 'O' ||
                br.ReadByte() != 'R' || br.ReadByte() != 'M')
                throw new ArgumentException("Incorrect IFF header (FORM)");

            // file length
            int fileLength = (br.ReadByte() << 24) + (br.ReadByte() << 16) +
                (br.ReadByte() << 8) + br.ReadByte();

            fileLength += 8;

            // FORM sub-type
            formSubType = (uint)((br.ReadByte() << 24) + (br.ReadByte() << 16) +
                (br.ReadByte() << 8) + br.ReadByte());

            // blocks
            while (stream.Position < fileLength)
            {
                uint typeID = (uint)((br.ReadByte() << 24) + (br.ReadByte() << 16) +
                    (br.ReadByte() << 8) + br.ReadByte());

                int blockLength = (br.ReadByte() << 24) + (br.ReadByte() << 16) +
                    (br.ReadByte() << 8) + br.ReadByte();

                if (WantBlock(typeID))
                {
                    // load it into memory
                    byte[] block = br.ReadBytes(blockLength);
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
        public Blorb(Stream fromStream)
            : base(fromStream)
        {
            if (FileType != BLORB_TYPE)
                throw new ArgumentException("Not a Blorb file");

            stream = fromStream;

            byte[] ridx = GetBlock("RIdx");
            if (ridx == null)
                throw new ArgumentException("Blorb file contains no resource index");

            // load resource index
            int count = (ridx[0] << 24) + (ridx[1] << 16) + (ridx[2] << 8) + ridx[3];
            resources = new Resource[count];
            for (int i = 0; i < count; i++)
            {
                int pos = 4 + i * 12;
                resources[i].Usage = (uint)((ridx[pos] << 24) + (ridx[pos + 1] << 16) + (ridx[pos + 2] << 8) + ridx[pos + 3]);
                resources[i].Number = (uint)((ridx[pos + 4] << 24) + (ridx[pos + 5] << 16) + (ridx[pos + 6] << 8) + ridx[pos + 7]);
                resources[i].Offset = (uint)((ridx[pos + 8] << 24) + (ridx[pos + 9] << 16) + (ridx[pos + 10] << 8) + ridx[pos + 11]);
            }
        }

        protected override bool WantBlock(uint type)
        {
            // only load the resource index
            return (type == RIDX_TYPE_ID);
        }

        protected override int CompareBlocks(uint type1, uint type2, byte[] data1, byte[] data2,
            int index1, int index2)
        {
            // make sure RIdx is first, but leave other blocks in order
            if (type1 == RIDX_TYPE_ID && type2 != RIDX_TYPE_ID)
                return -1;
            else if (type2 == RIDX_TYPE_ID && type1 != RIDX_TYPE_ID)
                return 1;
            else
                return index1.CompareTo(index2);
        }

        private byte[] ReadBlock(uint offset, uint length)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            byte[] result = new byte[length];
            int actual = stream.Read(result, 0, (int)length);
            if (actual < length)
                throw new Exception("Block ran past end of file");
            return result;
        }

        private Resource? FindResource(uint usage, uint? num)
        {
            for (int i = 0; i < resources.Length; i++)
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
        public string GetStoryType()
        {
            Resource? storyRes = FindResource(EXEC_USAGE_ID, null);

            if (storyRes == null)
                return null;

            byte[] type = ReadBlock(storyRes.Value.Offset, 4);
            StringBuilder sb = new StringBuilder(4);
            sb.Append((char)type[0]);
            sb.Append((char)type[1]);
            sb.Append((char)type[2]);
            sb.Append((char)type[3]);
            return sb.ToString();
        }

        /// <summary>
        /// Obtains a stream for the story file data in this Blorb.
        /// </summary>
        /// <returns>A stream containing the story file data, or null if no
        /// story resource is present.</returns>
        public Stream GetStoryStream()
        {
            Resource? storyRes = FindResource(EXEC_USAGE_ID, null);

            if (storyRes == null)
                return null;

            byte[] lenBytes = ReadBlock(storyRes.Value.Offset + 4, 4);
            uint len = (uint)((lenBytes[0] << 24) + (lenBytes[1] << 16) + (lenBytes[2] << 8) + lenBytes[3]);

            return new SubStream(stream, storyRes.Value.Offset + 8, len);
        }
    }

    internal class SubStream : Stream
    {
        private readonly Stream baseStream;
        private readonly long offset, length;
        private long position;

        public SubStream(Stream baseStream, long offset, long length)
        {
            if (!baseStream.CanSeek)
                throw new ArgumentException("Base stream must be seekable");

            this.baseStream = baseStream;
            this.offset = offset;
            this.length = length;
            this.position = 0;
        }

        public override bool CanRead
        {
            get { return baseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return baseStream.CanWrite; }
        }

        public override void Flush()
        {
            baseStream.Flush();
        }

        public override long Length
        {
            get { return length; }
        }

        public override long Position
        {
            get { return position; }
            set
            {
                if (value < 0 || value >= length)
                    throw new ArgumentOutOfRangeException();
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.position + count > this.length)
                count = (int)(this.length - this.position);

            baseStream.Position = this.offset + this.position;
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
                    position = this.length + offset;
                    break;
            }

            if (position < 0)
                position = 0;
            else if (position > this.length)
                position = this.length;

            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.position + count > this.length)
                count = (int)(this.length - this.position);

            baseStream.Position = this.offset + this.position;
            baseStream.Write(buffer, offset, count);
        }
    }
}