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

/* Server\Serialization.cs
 * Changelog
 *  9/15/22, Yoar
 *      Added Read/Write Rectangle3D
 * 9/13/22, Adam
 *  Add WriteRectangle2DList / ReadRectangle2DList
 * 8/31/22, Adam (DateTime - HasPatchedTime)
 *      1.properly accounted for patch state HasPatchedTime
 *      2. added handling for both Read/Write Delta time given our new understanding of DateTime.MinValue.
 *          2a. Handle ReadDeltaTime (DateTime.MaxValue) exceptions.
 *              I.e., in the past, if you wrote DeltaTime(DateTime.MaxValue), you would always throw an exception on ReadDeltaTime.
 * 8/31/22, Yoar (DateTime.MinValue fix)
 *  DateTime.MinValue was incorrectly (de)serialized due to the local time/UTC conversion.
 *  Applied the following workaround:
 *  https://stackoverflow.com/questions/4025851/why-can-datetime-minvalue-not-be-serialized-in-timezones-ahead-of-utc
 * 8/18/22, Adam(Utc)
 * Convert all read/write of times to handle UTC (the server format.)
 *  All DateTime objects are written as UTC.
 *  All DateTime objects are read as UTC and returned as LOCAL.
 *  Why: This insures that a Saves folder moved from production will run correctly on a local (developers) machine.
 *  Example: (old system) The AI server computer runs at UTC, but time is saved in 'local' format. This works fine as long as the world files are saved and loaded
 *          on the server. But if you then move the world files to a local developer machine running, say Pacific Time, then when you read the world files all dateTime 
 *          objects will be off by 7-8 hours, depending on DST
 *  The above change fixes this problem by ensuring all DateTime objects are always written as UTC and converted to local on read.
 *  8/18/22, Adam (ReadDeltaTime)
 *  The problem with the code 'old code' is that if someone writes DateTime.MaxValue, when we read it back
 *  it will throw an exception every time. The 'old code' (isn't really clear) and doesn't catch the case it is intended to.
 *  (DateTime.MaxValue - NOW [in write]) will always be > DateTime.MaxValue [in read] (ticks + NOW [in read])
 *  In read we always add NOW, which causes us to exceed DateTime.MaxValue.
 *  The 'new code' catches, and patches this case clearly without the need for exception-catching.
 *  8/15/22, Adam
 *      remove old, unused code.
 *	7/18/09, Adam
 *		Remove iPack32Lib software dongle - no longer needed for open source
 *	1/1/09, Adam
 *		Add new SerializableObject for classes you want Serialized.
 *		This SerializableObject is checked in the ScriptCompiler to insure there are both
 * 			Serialize() and Deserialize() methods
 * 11/15/08, plasma
 *			Add  generic list overloads and methods for read/write item, guild and mob lists
 *  7/13/07, Adam
 *      trap encoding error in BinaryFileReader.End()
 *      http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=127647&SiteID=1
 *      This error was surfacing in the ResourcePool which has since been rewritten to eliminage
 *      BinaryFileReader.End() as the means for detecting the EOF.
 *  3/28/06 Taran Kain
 *		Change IPAddress.Address (deprecated) references to use GetAddressBytes()
 *		Added GenericWriter.Write(byte[], int, int)
 *	12/17/05 Taran Kain
 *		Changed BinaryFileWriter to accept any Stream, instead of just FileStream
 *		Added Seek, Close and Position
 */

using Server.Guilds;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Server
{
    public abstract class GenericReader
    {
        public GenericReader() { }

        public abstract string ReadString();
        public abstract DateTime ReadDateTime();
        public abstract TimeSpan ReadTimeSpan();
        public abstract DateTime ReadDeltaTime();
        public abstract decimal ReadDecimal();
        public abstract long ReadLong();
        public abstract ulong ReadULong();
        public abstract int ReadInt();
        public abstract uint ReadUInt();
        public abstract short ReadShort();
        public abstract ushort ReadUShort();
        public abstract double ReadDouble();
        public abstract float ReadFloat();
        public abstract char ReadChar();
        public abstract byte ReadByte();
        public abstract sbyte ReadSByte();
        public abstract bool ReadBool();
        public abstract int ReadEncodedInt();
        public abstract IPAddress ReadIPAddress();

        public abstract Point3D ReadPoint3D();
        public abstract Point2D ReadPoint2D();
        public abstract Rectangle2D ReadRect2D();
        public abstract Rectangle3D ReadRect3D();
        public abstract Map ReadMap();

        public abstract Item ReadItem();
        public abstract Mobile ReadMobile();
        public abstract BaseGuild ReadGuild();

        public abstract List<Rectangle2D> ReadRectangle2DList();

        public abstract ArrayList ReadItemList();
        public abstract List<T> ReadItemList<T>() where T : Item;
        public abstract ArrayList ReadMobileList();
        public abstract List<T> ReadMobileList<T>() where T : Mobile;
        public abstract ArrayList ReadGuildList();
        public abstract List<T> ReadGuildList<T>() where T : BaseGuild;

        public abstract List<Item> ReadStrongItemList();
        public abstract List<T> ReadStrongItemList<T>() where T : Item;

        public abstract List<Mobile> ReadStrongMobileList();
        public abstract List<T> ReadStrongMobileList<T>() where T : Mobile;

        public abstract bool End();
        public abstract long Seek(long position, SeekOrigin origin);
        public abstract void Close();

        public abstract long Position { get; }
    }

    public abstract class GenericWriter
    {
        public GenericWriter() { }

        public abstract void Close();

        public abstract long Position { get; }

        public abstract void Write(string value);
        public abstract void Write(DateTime value);
        public abstract void Write(TimeSpan value);
        public abstract void Write(decimal value);
        public abstract void Write(long value);
        public abstract void Write(ulong value);
        public abstract void Write(int value);
        public abstract void Write(uint value);
        public abstract void Write(short value);
        public abstract void Write(ushort value);
        public abstract void Write(double value);
        public abstract void Write(float value);
        public abstract void Write(char value);
        public abstract void Write(byte value);
        public abstract void Write(sbyte value);
        public abstract void Write(bool value);
        public abstract void WriteEncodedInt(int value);
        public abstract void Write(IPAddress value);

        public abstract void WriteDeltaTime(DateTime value);

        public abstract void Write(Point3D value);
        public abstract void Write(Point2D value);
        public abstract void Write(Rectangle2D value);
        public abstract void Write(Rectangle3D value);
        public abstract void Write(Map value);

        public abstract void Write(Item value);
        public abstract void Write(Mobile value);
        public abstract void Write(BaseGuild value);

        public abstract void Write(byte[] buf, int offset, int length);

        public abstract void WriteRectangle2DList(List<Rectangle2D> list);

        public abstract void WriteItemList(ArrayList list);
        public abstract void WriteItemList(ArrayList list, bool tidy);
        public abstract void WriteItemList<T>(List<T> list) where T : Item;
        public abstract void WriteItemList<T>(List<T> list, bool tidy) where T : Item;

        public abstract void WriteMobileList(ArrayList list);
        public abstract void WriteMobileList(ArrayList list, bool tidy);
        public abstract void WriteMobileList<T>(List<T> list) where T : Mobile;
        public abstract void WriteMobileList<T>(List<T> list, bool tidy) where T : Mobile;

        public abstract void WriteGuildList(ArrayList list);
        public abstract void WriteGuildList(ArrayList list, bool tidy);
        public abstract void WriteGuildList<T>(List<T> list) where T : BaseGuild;
        public abstract void WriteGuildList<T>(List<T> list, bool tidy) where T : BaseGuild;

    }

    public class BinaryFileWriter : GenericWriter
    {
        private bool PrefixStrings;
        //private BinaryWriter m_Bin;
        private Stream m_File;

        private const int BufferSize = 4096;

        private byte[] m_Buffer;
        private int m_Index;

        private Encoding m_Encoding;

        public BinaryFileWriter(Stream strm, bool prefixStr)
        {
            PrefixStrings = prefixStr;
            //m_Bin = new BinaryWriter( strm, Utility.UTF8 ); 
            m_Encoding = Utility.UTF8;
            m_Buffer = new byte[BufferSize];
            m_File = strm;
        }

        public BinaryFileWriter(string filename, bool prefixStr)
        {
            PrefixStrings = prefixStr;
            m_Buffer = new byte[BufferSize];
            m_File = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            m_Encoding = Utility.UTF8WithEncoding;
            //m_Bin = new BinaryWriter( m_File, Utility.UTF8WithEncoding );
        }

        public void Flush()
        {
            if (m_Index > 0)
            {
                m_File.Write(m_Buffer, 0, m_Index);
                m_Index = 0;
            }
        }

        public override long Position
        {
            get
            {
                Flush();
                return m_File.Position;
            }
        }

        public Stream UnderlyingStream
        {
            get
            {
                Flush();
                return m_File;
            }
        }

        public override void Close()
        {
            Flush();
            m_File.Close();
        }

        public override void WriteEncodedInt(int value)
        {
            uint v = (uint)value;

            while (v >= 0x80)
            {
                if ((m_Index + 1) > BufferSize)
                    Flush();

                m_Buffer[m_Index++] = (byte)(v | 0x80);
                v >>= 7;
            }

            if ((m_Index + 1) > BufferSize)
                Flush();

            m_Buffer[m_Index++] = (byte)v;
        }

        private byte[] m_CharacterBuffer;
        private int m_MaxBufferChars;
        private const int LargeByteBufferSize = 256;

        internal void InternalWriteString(string value)
        {
            int length = m_Encoding.GetByteCount(value);

            WriteEncodedInt(length);

            if (m_CharacterBuffer == null)
            {
                m_CharacterBuffer = new byte[LargeByteBufferSize];
                m_MaxBufferChars = LargeByteBufferSize / m_Encoding.GetMaxByteCount(1);
            }

            if (length > LargeByteBufferSize)
            {
                int current = 0;
                int charsLeft = value.Length;

                while (charsLeft > 0)
                {
                    int charCount = (charsLeft > m_MaxBufferChars) ? m_MaxBufferChars : charsLeft;
                    int byteLength = m_Encoding.GetBytes(value, current, charCount, m_CharacterBuffer, 0);

                    if ((m_Index + byteLength) > BufferSize)
                        Flush();

                    Buffer.BlockCopy(m_CharacterBuffer, 0, m_Buffer, m_Index, byteLength);
                    m_Index += byteLength;

                    current += charCount;
                    charsLeft -= charCount;
                }
            }
            else
            {
                int byteLength = m_Encoding.GetBytes(value, 0, value.Length, m_CharacterBuffer, 0);

                if ((m_Index + byteLength) > BufferSize)
                    Flush();

                Buffer.BlockCopy(m_CharacterBuffer, 0, m_Buffer, m_Index, byteLength);
                m_Index += byteLength;
            }
        }

        public override void Write(string value)
        {
            if (PrefixStrings)
            {
                if (value == null)
                {
                    if ((m_Index + 1) > BufferSize)
                        Flush();

                    m_Buffer[m_Index++] = 0;
                }
                else
                {
                    if ((m_Index + 1) > BufferSize)
                        Flush();

                    m_Buffer[m_Index++] = 1;

                    InternalWriteString(value);
                }
            }
            else
            {
                InternalWriteString(value);
            }
        }

        public override void Write(DateTime value)
        {
            // MinValue workaround
            if (value == DateTime.MinValue)
                value = DateTime.MinValue.ToUniversalTime();
            else
                value = value.ToUniversalTime();
            // all saves are in UTC
            Write(value.Ticks);
        }

        public override void WriteDeltaTime(DateTime value)
        {   // all saves are in UTC
            long ticks = value.Ticks;
            long now = DateTime.UtcNow.Ticks;

            TimeSpan d;

            try { d = new TimeSpan(ticks - now); }
            catch { if (ticks < now) d = TimeSpan.MaxValue; else d = TimeSpan.MaxValue; }

            Write(d);
        }

        public override void Write(IPAddress value)
        {
            Write(value.GetAddressBytes(), 0, 4);
        }

        public override void Write(byte[] buf, int offset, int length)
        {
            if (offset < 0 || length <= 0 || (length + offset) > buf.Length)
                return;

            if (m_Index + length > BufferSize)
                Flush();

            for (int i = 0; i < length / BufferSize; i++)
            {
                Array.Copy(buf, offset, m_Buffer, m_Index, BufferSize);
                offset += BufferSize;
                m_Index += BufferSize;
                Flush();
            }
            Array.Copy(buf, offset, m_Buffer, m_Index, length);
            m_Index += length;
        }

        public override void Write(TimeSpan value)
        {
            Write(value.Ticks);
        }

        public override void Write(decimal value)
        {
            int[] bits = Decimal.GetBits(value);

            for (int i = 0; i < bits.Length; ++i)
                Write(bits[i]);
        }

        public override void Write(long value)
        {
            if ((m_Index + 8) > BufferSize)
                Flush();

            m_Buffer[m_Index] = (byte)value;
            m_Buffer[m_Index + 1] = (byte)(value >> 8);
            m_Buffer[m_Index + 2] = (byte)(value >> 16);
            m_Buffer[m_Index + 3] = (byte)(value >> 24);
            m_Buffer[m_Index + 4] = (byte)(value >> 32);
            m_Buffer[m_Index + 5] = (byte)(value >> 40);
            m_Buffer[m_Index + 6] = (byte)(value >> 48);
            m_Buffer[m_Index + 7] = (byte)(value >> 56);
            m_Index += 8;
        }

        public override void Write(ulong value)
        {
            if ((m_Index + 8) > BufferSize)
                Flush();

            m_Buffer[m_Index] = (byte)value;
            m_Buffer[m_Index + 1] = (byte)(value >> 8);
            m_Buffer[m_Index + 2] = (byte)(value >> 16);
            m_Buffer[m_Index + 3] = (byte)(value >> 24);
            m_Buffer[m_Index + 4] = (byte)(value >> 32);
            m_Buffer[m_Index + 5] = (byte)(value >> 40);
            m_Buffer[m_Index + 6] = (byte)(value >> 48);
            m_Buffer[m_Index + 7] = (byte)(value >> 56);
            m_Index += 8;
        }

        public override void Write(int value)
        {
            if ((m_Index + 4) > BufferSize)
                Flush();

            m_Buffer[m_Index] = (byte)value;
            m_Buffer[m_Index + 1] = (byte)(value >> 8);
            m_Buffer[m_Index + 2] = (byte)(value >> 16);
            m_Buffer[m_Index + 3] = (byte)(value >> 24);
            m_Index += 4;
        }

        public override void Write(uint value)
        {
            if ((m_Index + 4) > BufferSize)
                Flush();

            m_Buffer[m_Index] = (byte)value;
            m_Buffer[m_Index + 1] = (byte)(value >> 8);
            m_Buffer[m_Index + 2] = (byte)(value >> 16);
            m_Buffer[m_Index + 3] = (byte)(value >> 24);
            m_Index += 4;
        }

        public override void Write(short value)
        {
            if ((m_Index + 2) > BufferSize)
                Flush();

            m_Buffer[m_Index] = (byte)value;
            m_Buffer[m_Index + 1] = (byte)(value >> 8);
            m_Index += 2;
        }

        public override void Write(ushort value)
        {
            if ((m_Index + 2) > BufferSize)
                Flush();

            m_Buffer[m_Index] = (byte)value;
            m_Buffer[m_Index + 1] = (byte)(value >> 8);
            m_Index += 2;
        }

        public unsafe override void Write(double value)
        {
            if ((m_Index + 8) > BufferSize)
                Flush();

            fixed (byte* pBuffer = m_Buffer)
                *((double*)(&pBuffer[m_Index])) = value;

            m_Index += 8;
        }

        public unsafe override void Write(float value)
        {
            if ((m_Index + 4) > BufferSize)
                Flush();

            fixed (byte* pBuffer = m_Buffer)
                *((float*)(&pBuffer[m_Index])) = value;

            m_Index += 4;
        }

        private char[] m_SingleCharBuffer = new char[1];

        public override void Write(char value)
        {
            if ((m_Index + 8) > BufferSize)
                Flush();

            m_SingleCharBuffer[0] = value;

            int byteCount = m_Encoding.GetBytes(m_SingleCharBuffer, 0, 1, m_Buffer, m_Index);
            m_Index += byteCount;
        }

        public override void Write(byte value)
        {
            if ((m_Index + 1) > BufferSize)
                Flush();

            m_Buffer[m_Index++] = value;
        }

        public override void Write(sbyte value)
        {
            if ((m_Index + 1) > BufferSize)
                Flush();

            m_Buffer[m_Index++] = (byte)value;
        }

        public override void Write(bool value)
        {
            if ((m_Index + 1) > BufferSize)
                Flush();

            m_Buffer[m_Index++] = (byte)(value ? 1 : 0);
        }

        public override void Write(Point3D value)
        {
            Write(value.m_X);
            Write(value.m_Y);
            Write(value.m_Z);
        }

        public override void Write(Point2D value)
        {
            Write(value.m_X);
            Write(value.m_Y);
        }

        public override void Write(Rectangle2D value)
        {
            Write(value.Start);
            Write(value.End);
        }

        public override void Write(Rectangle3D value)
        {
            Write(value.Start);
            Write(value.End);
        }

        public override void Write(Map value)
        {
            if (value != null)
                Write((byte)value.MapIndex);
            else
                Write((byte)0xFF);
        }

        public override void Write(Item value)
        {
            if (value == null || value.Deleted)
                Write(Serial.MinusOne);
            else
                Write(value.Serial);
        }

        public override void Write(Mobile value)
        {
            if (value == null || value.Deleted)
                Write(Serial.MinusOne);
            else
                Write(value.Serial);
        }

        public override void Write(BaseGuild value)
        {
            if (value == null)
                Write(0);
            else
                Write(value.Id);
        }

        public override void WriteMobileList<T>(List<T> list)
        {
            WriteMobileList<T>(list, false);
        }

        public override void WriteMobileList<T>(List<T> list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (((Mobile)list[i]).Deleted)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write((Mobile)list[i]);
        }

        public override void WriteMobileList(ArrayList list)
        {
            WriteMobileList(list, false);
        }

        public override void WriteMobileList(ArrayList list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (((Mobile)list[i]).Deleted)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write((Mobile)list[i]);
        }
        public override void WriteRectangle2DList(List<Rectangle2D> list)
        {
            Write(list.Count);
            for (int i = 0; i < list.Count; ++i)
                Write(list[i]);
        }

        public override void WriteItemList<T>(List<T> list)
        {
            WriteItemList<T>(list, false);
        }

        public override void WriteItemList<T>(List<T> list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (((Item)list[i]).Deleted)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write((Item)list[i]);
        }

        public override void WriteItemList(ArrayList list)
        {
            WriteItemList(list, false);
        }

        public override void WriteItemList(ArrayList list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (((Item)list[i]).Deleted)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write((Item)list[i]);
        }

        public override void WriteGuildList<T>(List<T> list)
        {
            WriteGuildList<T>(list, false);
        }

        public override void WriteGuildList<T>(List<T> list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (((BaseGuild)list[i]).Disbanded)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write((BaseGuild)list[i]);
        }

        public override void WriteGuildList(ArrayList list)
        {
            WriteGuildList(list, false);
        }

        public override void WriteGuildList(ArrayList list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (((BaseGuild)list[i]).Disbanded)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write((BaseGuild)list[i]);
        }
    }

    public class BinaryFileReader : GenericReader
    {
        private BinaryReader m_File;

        public BinaryFileReader(BinaryReader br) { m_File = br; }

        public override void Close()
        {
            m_File.Close();
        }

        public override long Position
        {
            get
            {
                return m_File.BaseStream.Position;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_File.BaseStream.Seek(offset, origin);
        }

        public override string ReadString()
        {
            if (ReadByte() != 0)
                return m_File.ReadString();
            else
                return null;
        }

        public override DateTime ReadDeltaTime()
        {
            long ticks = m_File.ReadInt64();
            long now;
            if (CoreAI.IsDynamicPatchSet(CoreAI.PatchIndex.HasPatchedTime) == false)
            {
                now = DateTime.UtcNow.Ticks;
                try { return new DateTime(now + ticks); }
                catch { if (ticks > 0) return DateTime.MaxValue; else return DateTime.MinValue; }
            }

            now = DateTime.UtcNow.Ticks;

            if (ticks > 0 && (ticks + now) < 0)
                return DateTime.MaxValue;
            else if (ticks < 0 && (ticks + now) < 0)
                return DateTime.MinValue;
            else if (ticks + now > DateTime.MaxValue.Ticks)
                return DateTime.MaxValue;

            try { return new DateTime(now + ticks); }
            catch { if (ticks > 0) return DateTime.MaxValue; else return DateTime.MinValue; }
        }

        public override IPAddress ReadIPAddress()
        {
            return new IPAddress(m_File.ReadInt64());
        }

        public override int ReadEncodedInt()
        {
            int v = 0, shift = 0;
            byte b;

            do
            {
                b = m_File.ReadByte();
                v |= (b & 0x7F) << shift;
                shift += 7;
            } while (b >= 0x80);

            return v;
        }

        public override DateTime ReadDateTime()
        {
            long ticks = m_File.ReadInt64();
            DateTime value = new DateTime(ticks);
            if (CoreAI.IsDynamicPatchSet(CoreAI.PatchIndex.HasPatchedTime) == false)
                return value;

            // MinValue workaround
            if (value == DateTime.MinValue.ToUniversalTime())
                return DateTime.MinValue;

            // we wrote in UTC, we will read the UTC and return Local
            return new DateTime(ticks).ToLocalTime();
        }

        public override TimeSpan ReadTimeSpan()
        {
            return new TimeSpan(m_File.ReadInt64());
        }

        public override decimal ReadDecimal()
        {
            return m_File.ReadDecimal();
        }

        public override long ReadLong()
        {
            return m_File.ReadInt64();
        }

        public override ulong ReadULong()
        {
            return m_File.ReadUInt64();
        }

        public override int ReadInt()
        {
            return m_File.ReadInt32();
        }

        public override uint ReadUInt()
        {
            return m_File.ReadUInt32();
        }

        public override short ReadShort()
        {
            return m_File.ReadInt16();
        }

        public override ushort ReadUShort()
        {
            return m_File.ReadUInt16();
        }

        public override double ReadDouble()
        {
            return m_File.ReadDouble();
        }

        public override float ReadFloat()
        {
            return m_File.ReadSingle();
        }

        public override char ReadChar()
        {
            return m_File.ReadChar();
        }

        public override byte ReadByte()
        {
            return m_File.ReadByte();
        }

        public override sbyte ReadSByte()
        {
            return m_File.ReadSByte();
        }

        public override bool ReadBool()
        {
            return m_File.ReadBoolean();
        }

        public override Point3D ReadPoint3D()
        {
            return new Point3D(ReadInt(), ReadInt(), ReadInt());
        }

        public override Point2D ReadPoint2D()
        {
            return new Point2D(ReadInt(), ReadInt());
        }

        public override Rectangle2D ReadRect2D()
        {
            return new Rectangle2D(ReadPoint2D(), ReadPoint2D());
        }

        public override Rectangle3D ReadRect3D()
        {
            return new Rectangle3D(ReadPoint3D(), ReadPoint3D());
        }

        public override Map ReadMap()
        {
            return Map.Maps[ReadByte()];
        }

        public override Item ReadItem()
        {
            return World.FindItem(ReadInt());
        }

        public override Mobile ReadMobile()
        {
            return World.FindMobile(ReadInt());
        }

        public override BaseGuild ReadGuild()
        {
            return BaseGuild.Find(ReadInt());
        }

        public override List<Rectangle2D> ReadRectangle2DList()
        {
            int count = ReadInt();

            List<Rectangle2D> list = new List<Rectangle2D>(count);

            for (int i = 0; i < count; ++i)
                list.Add(ReadRect2D());

            return list;
        }

        public override List<T> ReadItemList<T>()
        {
            int count = ReadInt();

            List<T> list = new List<T>(count);

            for (int i = 0; i < count; ++i)
            {
                T item = ReadItem() as T;

                if (item != null)
                    list.Add(item);
            }

            return list;

        }

        public override ArrayList ReadItemList()
        {
            int count = ReadInt();

            ArrayList list = new ArrayList(count);

            for (int i = 0; i < count; ++i)
            {
                Item item = ReadItem();

                if (item != null)
                    list.Add(item);
            }

            return list;
        }

        public override List<T> ReadMobileList<T>()
        {
            int count = ReadInt();

            List<T> list = new List<T>(count);

            for (int i = 0; i < count; ++i)
            {
                T m = ReadMobile() as T;

                if (m != null)
                    list.Add(m);
            }

            return list;
        }

        public override ArrayList ReadMobileList()
        {
            int count = ReadInt();

            ArrayList list = new ArrayList(count);

            for (int i = 0; i < count; ++i)
            {
                Mobile m = ReadMobile();

                if (m != null)
                    list.Add(m);
            }

            return list;
        }

        public override List<T> ReadGuildList<T>()
        {
            int count = ReadInt();

            List<T> list = new List<T>(count);

            for (int i = 0; i < count; ++i)
            {
                T g = ReadGuild() as T;

                if (g != null)
                    list.Add(g);
            }

            return list;
        }

        public override ArrayList ReadGuildList()
        {
            int count = ReadInt();

            ArrayList list = new ArrayList(count);

            for (int i = 0; i < count; ++i)
            {
                BaseGuild g = ReadGuild();

                if (g != null)
                    list.Add(g);
            }

            return list;
        }

        public override List<Item> ReadStrongItemList()
        {
            return ReadStrongItemList<Item>();
        }

        public override List<T> ReadStrongItemList<T>()
        {
            int count = ReadInt();

            if (count > 0)
            {
                List<T> list = new List<T>(count);

                for (int i = 0; i < count; ++i)
                {
                    T item = ReadItem() as T;

                    if (item != null)
                    {
                        list.Add(item);
                    }
                }

                return list;
            }
            else
            {
                return new List<T>();
            }
        }

        public override List<Mobile> ReadStrongMobileList()
        {
            return ReadStrongMobileList<Mobile>();
        }

        public override List<T> ReadStrongMobileList<T>()
        {
            int count = ReadInt();

            if (count > 0)
            {
                List<T> list = new List<T>(count);

                for (int i = 0; i < count; ++i)
                {
                    T m = ReadMobile() as T;

                    if (m != null)
                    {
                        list.Add(m);
                    }
                }

                return list;
            }
            else
            {
                return new List<T>();
            }
        }

        public override bool End()
        {
            try { return m_File.PeekChar() == -1; }
            catch (ArgumentException ae)
            {
                // trap encoding error
                // http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=127647&SiteID=1
                Console.WriteLine(ae.Message);
                return false;
            }
            catch { return true; }
        }
    }

    public class AsyncWriter : GenericWriter
    {
        private static int m_ThreadCount = 0;
        public static int ThreadCount { get { return m_ThreadCount; } }


        private int BufferSize;

        private long m_LastPos, m_CurPos;
        private bool m_Closed;
        private bool PrefixStrings;

        private MemoryStream m_Mem;
        private BinaryWriter m_Bin;
        private FileStream m_File;

        private Queue m_WriteQueue;
        private Thread m_WorkerThread;

        public AsyncWriter(string filename, bool prefix)
            : this(filename, 1048576, prefix)//1 mb buffer
        {
        }

        public AsyncWriter(string filename, int buffSize, bool prefix)
        {
            PrefixStrings = prefix;
            m_Closed = false;
            m_WriteQueue = Queue.Synchronized(new Queue());
            BufferSize = buffSize;

            m_File = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            m_Mem = new MemoryStream(BufferSize + 1024);
            m_Bin = new BinaryWriter(m_Mem, Utility.UTF8WithEncoding);
        }

        private void Enqueue(MemoryStream mem)
        {
            m_WriteQueue.Enqueue(mem);

            if (m_WorkerThread == null || !m_WorkerThread.IsAlive)
            {
                m_WorkerThread = new Thread(new ThreadStart(new WorkerThread(this).Worker));
                m_WorkerThread.Priority = ThreadPriority.BelowNormal;
                m_WorkerThread.Start();
            }
        }

        private class WorkerThread
        {
            private AsyncWriter m_Owner;

            public WorkerThread(AsyncWriter owner)
            {
                m_Owner = owner;
            }

            public void Worker()
            {
                AsyncWriter.m_ThreadCount++;
                while (m_Owner.m_WriteQueue.Count > 0)
                {
                    MemoryStream mem = (MemoryStream)m_Owner.m_WriteQueue.Dequeue();

                    if (mem != null && mem.Length > 0)
                        mem.WriteTo(m_Owner.m_File);
                }

                if (m_Owner.m_Closed)
                    m_Owner.m_File.Close();
                AsyncWriter.m_ThreadCount--;
            }
        }

        private void OnWrite()
        {
            long curlen = m_Mem.Length;
            m_CurPos += curlen - m_LastPos;
            m_LastPos = curlen;
            if (curlen >= BufferSize)
            {
                Enqueue(m_Mem);
                m_Mem = new MemoryStream(BufferSize + 1024);
                m_Bin = new BinaryWriter(m_Mem, Utility.UTF8WithEncoding);
                m_LastPos = 0;
            }
        }

        public MemoryStream MemStream
        {
            get
            {
                return m_Mem;
            }
            set
            {
                if (m_Mem.Length > 0)
                    Enqueue(m_Mem);

                m_Mem = value;
                m_Bin = new BinaryWriter(m_Mem, Utility.UTF8WithEncoding);
                m_LastPos = 0;
                m_CurPos = m_Mem.Length;
                m_Mem.Seek(0, SeekOrigin.End);
            }
        }

        public override void Close()
        {
            Enqueue(m_Mem);
            m_Closed = true;
        }

        public override long Position
        {
            get
            {
                return m_CurPos;
            }
        }

        public override void Write(IPAddress value)
        {
            m_Bin.Write(value.GetAddressBytes(), 0, 4);
            OnWrite();
        }

        public override void Write(string value)
        {
            if (PrefixStrings)
            {
                if (value == null)
                {
                    m_Bin.Write((byte)0);
                }
                else
                {
                    m_Bin.Write((byte)1);
                    m_Bin.Write(value);
                }
            }
            else
            {
                m_Bin.Write(value);
            }
            OnWrite();
        }

        public override void WriteDeltaTime(DateTime value)
        {
            // all saves are in UTC
            value = value.ToUniversalTime();
            long ticks = value.Ticks;
            long now = DateTime.UtcNow.ToUniversalTime().Ticks;

            TimeSpan d;

            try { d = new TimeSpan(ticks - now); }
            catch { if (ticks < now) d = TimeSpan.MaxValue; else d = TimeSpan.MaxValue; }

            Write(d);
        }

        public override void Write(DateTime value)
        {
            // MinValue workaround
            if (value == DateTime.MinValue)
                value = DateTime.MinValue.ToUniversalTime();
            // all saves are in UTC
            value = value.ToUniversalTime();
            m_Bin.Write(value.Ticks);
            OnWrite();
        }

        public override void Write(TimeSpan value)
        {
            m_Bin.Write(value.Ticks);
            OnWrite();
        }

        public override void Write(decimal value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(long value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(ulong value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void WriteEncodedInt(int value)
        {
            uint v = (uint)value;

            while (v >= 0x80)
            {
                m_Bin.Write((byte)(v | 0x80));
                v >>= 7;
            }

            m_Bin.Write((byte)v);
            OnWrite();
        }

        public override void Write(int value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(uint value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(short value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(ushort value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(double value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(float value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(char value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(byte value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(sbyte value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(bool value)
        {
            m_Bin.Write(value);
            OnWrite();
        }

        public override void Write(Point3D value)
        {
            Write(value.m_X);
            Write(value.m_Y);
            Write(value.m_Z);
        }

        public override void Write(Point2D value)
        {
            Write(value.m_X);
            Write(value.m_Y);
        }

        public override void Write(Rectangle2D value)
        {
            Write(value.Start);
            Write(value.End);
        }

        public override void Write(Rectangle3D value)
        {
            Write(value.Start);
            Write(value.End);
        }

        public override void Write(Map value)
        {
            if (value != null)
                Write((byte)value.MapIndex);
            else
                Write((byte)0xFF);
        }

        public override void Write(Item value)
        {
            if (value == null || value.Deleted)
                Write(Serial.MinusOne);
            else
                Write(value.Serial);
        }

        public override void Write(Mobile value)
        {
            if (value == null || value.Deleted)
                Write(Serial.MinusOne);
            else
                Write(value.Serial);
        }

        public override void Write(BaseGuild value)
        {
            if (value == null)
                Write(0);
            else
                Write(value.Id);
        }

        public override void Write(byte[] buf, int offset, int length)
        {
            m_Bin.Write(buf, offset, length);
        }

        public override void WriteMobileList<T>(List<T> list)
        {
            WriteMobileList<T>(list, false);
        }

        public override void WriteMobileList<T>(List<T> list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (list[i].Deleted)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write(list[i]);

        }

        public override void WriteMobileList(ArrayList list)
        {
            WriteMobileList(list, false);
        }

        public override void WriteMobileList(ArrayList list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (((Mobile)list[i]).Deleted)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write((Mobile)list[i]);
        }
        public override void WriteRectangle2DList(List<Rectangle2D> list)
        {
            Write(list.Count);
            for (int i = 0; i < list.Count; ++i)
                Write(list[i]);
        }

        public override void WriteItemList<T>(List<T> list)
        {
            WriteItemList<T>(list, false);
        }

        public override void WriteItemList<T>(List<T> list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (list[i].Deleted)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write(list[i]);
        }

        public override void WriteItemList(ArrayList list)
        {
            WriteItemList(list, false);
        }

        public override void WriteItemList(ArrayList list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (((Item)list[i]).Deleted)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write((Item)list[i]);
        }

        public override void WriteGuildList<T>(List<T> list)
        {
            WriteGuildList<T>(list, false);
        }

        public override void WriteGuildList<T>(List<T> list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (list[i].Disbanded)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write(list[i]);
        }

        public override void WriteGuildList(ArrayList list)
        {
            WriteGuildList(list, false);
        }

        public override void WriteGuildList(ArrayList list, bool tidy)
        {
            if (tidy)
            {
                for (int i = 0; i < list.Count;)
                {
                    if (((BaseGuild)list[i]).Disbanded)
                        list.RemoveAt(i);
                    else
                        ++i;
                }
            }

            Write(list.Count);

            for (int i = 0; i < list.Count; ++i)
                Write((BaseGuild)list[i]);
        }
    }

    public class SerializableObject
    {
        public virtual void Serialize(GenericWriter writer)
        {
        }
        public virtual void Deserialize(GenericReader reader)
        {
        }
    }
}