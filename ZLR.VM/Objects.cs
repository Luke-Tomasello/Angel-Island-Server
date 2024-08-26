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
using JetBrains.Annotations;

namespace ZLR.VM
{
    public partial class ZMachine
    {
        internal ushort GetPropAddr(ushort obj, short prop)
        {
            if (obj == 0)
                return 0;

            if (zversion <= 3)
            {
                int propTable = (ushort)GetWord(GetObjectAddress(obj) + 7);

                // skip object name
                propTable += 2 * GetByte(propTable) + 1;

                var addr = propTable;
                var b = GetByte(addr++);
                while (b != 0)
                {
                    var num = b & 31;
                    var len = (b >> 5) + 1;

                    if (num == prop)
                        return (ushort)addr;
                    if (num < prop)
                        break;

                    addr += len;
                    b = GetByte(addr++);
                }
            }
            else
            {
                int propTable = (ushort)GetWord(GetObjectAddress(obj) + 12);

                // skip object name
                propTable += 2 * GetByte(propTable) + 1;

                var addr = propTable;
                var b = GetByte(addr++);
                while (b != 0)
                {
                    var num = b & 63;
                    int len;
                    if ((b & 128) == 0)
                    {
                        len = (b & 64) == 0 ? 1 : 2;
                    }
                    else
                    {
                        b = GetByte(addr++);
                        System.Diagnostics.Debug.Assert((b & 128) == 128);
                        len = b & 63;
                        if (len == 0)
                            len = 64;
                    }

                    if (num == prop)
                        return (ushort)addr;
                    if (num < prop)
                        break;

                    addr += len;
                    b = GetByte(addr++);
                }
            }

            return 0;
        }

#pragma warning disable 0169
        internal short GetNextProp(ushort obj, short prop)
        {
            if (obj == 0)
                return 0;

            if (zversion <= 3)
            {
                int propTable = (ushort)GetWord(GetObjectAddress(obj) + 7);

                // skip object name
                propTable += 2 * GetByte(propTable) + 1;

                var addr = propTable;
                var b = GetByte(addr++);
                while (b != 0)
                {
                    var num = b & 31;
                    var len = (b >> 5) + 1;

                    if (prop == 0 || num < prop)
                        return (short)num;

                    addr += len;
                    b = GetByte(addr++);
                }
            }
            else
            {
                int propTable = (ushort)GetWord(GetObjectAddress(obj) + 12);

                // skip object name
                propTable += 2 * GetByte(propTable) + 1;

                var addr = propTable;
                var b = GetByte(addr++);
                while (b != 0)
                {
                    var num = b & 63;
                    int len;
                    if ((b & 128) == 0)
                    {
                        len = (b & 64) == 0 ? 1 : 2;
                    }
                    else
                    {
                        b = GetByte(addr++);
                        System.Diagnostics.Debug.Assert((b & 128) == 128);
                        len = b & 63;
                        if (len == 0)
                            len = 64;
                    }

                    if (prop == 0 || num < prop)
                        return (short)num;

                    addr += len;
                    b = GetByte(addr++);
                }
            }

            return 0;
        }

        internal short GetPropValue(ushort obj, short prop)
        {
            int addr = GetPropAddr(obj, prop);

            if (addr == 0)
            {
                // not present, use default value
                return GetWord(objectTable + 2 * (prop - 1));
            }

            switch (GetPropLength((ushort)addr))
            {
                case 1:
                    return GetByte(addr);

                case 2:
                    return GetWord(addr);

                default:
                    throw new InvalidOperationException("Illegal get_prop on >2 byte property");
            }
        }

        internal void SetPropValue(ushort obj, short prop, short value)
        {
            int addr = GetPropAddr(obj, prop);

            if (addr != 0)
            {
                var len = GetPropLength((ushort)addr);
                if (len == 1)
                    SetByte(addr, (byte)value);
                else
                    SetWord(addr, value);
            }
        }
#pragma warning restore 0169

        internal short GetPropLength(ushort address)
        {
            if (address == 0)
                return 0;

            if (zversion <= 3)
            {
                var b = GetByte(address - 1);
                return (short)((b >> 5) + 1);
            }
            else
            {
                var b = GetByte(address - 1);
                if ((b & 128) == 0)
                {
                    return (b & 64) == 0 ? (short) 1 : (short) 2;
                }
                var len = (short)(b & 63);
                return len == 0 ? (short) 64 : len;
            }
        }

        internal ushort GetObjectParent(ushort obj)
        {
            if (obj == 0)
                return 0;

            if (zversion <= 3)
                return GetByte(GetObjectAddress(obj) + 4);

            return (ushort)GetWord(GetObjectAddress(obj) + 6);
        }

        internal ushort GetObjectSibling(ushort obj)
        {
            if (obj == 0)
                return 0;

            if (zversion <= 3)
                return GetByte(GetObjectAddress(obj) + 5);

            return (ushort)GetWord(GetObjectAddress(obj) + 8);
        }

        internal ushort GetObjectChild(ushort obj)
        {
            if (obj == 0)
                return 0;

            if (zversion <= 3)
                return GetByte(GetObjectAddress(obj) + 6);

            return (ushort)GetWord(GetObjectAddress(obj) + 10);
        }

        void SetObjectParent(ushort obj, ushort value)
        {
            if (obj != 0)
            {
                if (zversion <= 3)
                    SetByte(GetObjectAddress(obj) + 4, (byte)value);
                else
                    SetWord(GetObjectAddress(obj) + 6, (short)value);
            }
        }

        void SetObjectSibling(ushort obj, ushort value)
        {
            if (obj != 0)
            {
                if (zversion <= 3)
                    SetByte(GetObjectAddress(obj) + 5, (byte)value);
                else
                    SetWord(GetObjectAddress(obj) + 8, (short)value);
            }
        }

        void SetObjectChild(ushort obj, ushort value)
        {
            if (obj != 0)
            {
                if (zversion <= 3)
                    SetByte(GetObjectAddress(obj) + 6, (byte)value);
                else
                    SetWord(GetObjectAddress(obj) + 10, (short)value);
            }
        }

#pragma warning disable 0169
        internal void InsertObject(ushort obj, ushort dest)
        {
            if (obj == 0)
                return;

            var prevParent = GetObjectParent(obj);
            if (prevParent != 0)
            {
                var head = GetObjectChild(prevParent);
                if (head == obj)
                {
                    var prevSibling = GetObjectSibling(obj);
                    SetObjectChild(prevParent, prevSibling);
                }
                else
                {
                    var next = GetObjectSibling(head);
                    while (next != obj)
                    {
                        head = next;
                        next = GetObjectSibling(head);
                    }
                    SetObjectSibling(head, GetObjectSibling(obj));
                }
            }

            var prevChild = GetObjectChild(dest);
            SetObjectSibling(obj, prevChild);
            SetObjectParent(obj, dest);
            if (dest != 0)
                SetObjectChild(dest, obj);
        }
#pragma warning restore 0169

        private ushort GetObjectAddress(ushort obj)
        {
            if (zversion <= 3)
                return (ushort) (objectTable + 2 * 31 + 9 * (obj - 1));
            return (ushort) (objectTable + 2 * 63 + 14 * (obj - 1));
        }

#pragma warning disable 0169
        [NotNull]
        internal string GetObjectName(ushort obj)
        {
            if (obj == 0)
                return string.Empty;

            var propTable = (ushort)GetWord(GetObjectAddress(obj) + (zversion <= 3 ? 7 : 12));
            return DecodeString(propTable + 1);
        }

        internal bool GetObjectAttr(ushort obj, short attr)
        {
            if (obj == 0)
                return false;

            var bit = 128 >> (attr & 7);
            var offset = attr >> 3;
            var flags = GetByte(GetObjectAddress(obj) + offset);
            return (flags & bit) != 0;
        }

        internal void SetObjectAttr(ushort obj, short attr, bool value)
        {
            if (obj == 0)
                return;

            var bit = 128 >> (attr & 7);
            var address = GetObjectAddress(obj) + (attr >> 3);
            var flags = GetByte(address);
            if (value)
                flags |= (byte)bit;
            else
                flags &= (byte)~bit;
            SetByte(address, flags);
        }
#pragma warning restore 0169
    }
}