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

namespace Server.HuePickers
{
    public class HuePicker
    {
        private static int m_NextSerial = 1;

        private int m_Serial;
        private int m_ItemID;

        public int Serial
        {
            get
            {
                return m_Serial;
            }
        }

        public int ItemID
        {
            get
            {
                return m_ItemID;
            }
        }

        public HuePicker(int itemID)
        {
            do
            {
                m_Serial = m_NextSerial++;
            } while (m_Serial == 0);

            m_ItemID = itemID;
        }

        public virtual void OnResponse(int hue)
        {
        }

        public void SendTo(NetState state)
        {
            state.Send(new DisplayHuePicker(this));
            state.AddHuePicker(this);
        }
    }
}