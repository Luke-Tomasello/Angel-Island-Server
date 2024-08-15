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

using Server.Network;
using System;

namespace Server.Engines.Chat
{
    public sealed class ChatMessagePacket : Packet
    {
        public ChatMessagePacket(Mobile who, int number, string param1, string param2)
            : base(0xB2)
        {
            if (param1 == null)
                param1 = string.Empty;

            if (param2 == null)
                param2 = string.Empty;

            EnsureCapacity(13 + ((param1.Length + param2.Length) * 2));

            m_Stream.Write((ushort)(number - 20));

            if (who != null)
                m_Stream.WriteAsciiFixed(who.Language, 4);
            else
                m_Stream.Write((int)0);

            m_Stream.WriteBigUniNull(param1);
            m_Stream.WriteBigUniNull(param2);
        }
    }
}