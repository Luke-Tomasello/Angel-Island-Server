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

/* Scripts\Items\Misc\MarkTime.cs
 * ChangeLog:
 *	09/24/08, Adam
 *		Add a new system for calculating player movement speed.
 *		please see m_LastTimeMark in PlayerMobile.cs 
 */

using Server.Mobiles;
using System;

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class MarkTime : Item
    {
        [Constructable]
        public MarkTime()
            : base(0x1F14)
        {
            Weight = 1.0;
            Visible = false;
        }

        public MarkTime(Serial serial)
            : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m is PlayerMobile)
            {
                PlayerMobile pm = m as PlayerMobile;
                DateTime now = DateTime.UtcNow;
                TimeSpan ts = now - pm.LastTimeMark;
                pm.LastTimeMark = now;
                pm.SendMessage(string.Format("{0:00.00} seconds", ts.TotalSeconds));
            }
            return base.OnMoveOver(m);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}