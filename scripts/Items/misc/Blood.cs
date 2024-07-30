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

/* CHANGELOG
 * 08/27/05, Taran Kain
 *		Added new constructor to specify the lifespan of the blood object.
 */

using System;

namespace Server.Items
{
    public class Blood : Item
    {
        [Constructable]
        public Blood()
            : this(0x1645)
        {
        }

        [Constructable]
        public Blood(int itemID)
            : this(itemID, 3.0 + (Utility.RandomDouble() * 3.0))
        {
        }

        [Constructable]
        public Blood(int itemID, double lifespan)
            : base(itemID)
        {
            Movable = false;

            new InternalTimer(this, TimeSpan.FromSeconds(lifespan)).Start();
        }

        public Blood(Serial serial)
            : base(serial)
        {
            new InternalTimer(this, TimeSpan.FromSeconds(3.0 + (Utility.RandomDouble() * 3.0))).Start();
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

        private class InternalTimer : Timer
        {
            private Item m_Blood;

            public InternalTimer(Item blood, TimeSpan lifespan)
                : base(lifespan)
            {
                Priority = TimerPriority.FiftyMS;

                m_Blood = blood;
            }

            protected override void OnTick()
            {
                m_Blood.Delete();
            }
        }
    }
}