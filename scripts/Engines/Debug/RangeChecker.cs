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

namespace Server.Engines
{
    public class RangeChecker : Item
    {
        [Constructable]
        public RangeChecker()
            : base(0x14F5)  // spyglass graphic
        {
            Movable = false;
            Timer.DelayCall(TimeSpan.FromSeconds(5), new TimerStateCallback(Tick), new object[] { null });
        }

        public RangeChecker(Serial serial)
            : base(serial)
        {
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
        private void Tick(object state)
        {
            object[] aState = (object[])state;

            {   // first, just notify the tester how far they are from the test beacon
                IPooledEnumerable eable = Map.Felucca.GetMobilesInRange(this.Location, 128);
                foreach (Mobile m in eable)
                {
                    if (m != null && m.Player && m.AccessLevel > AccessLevel.Player)
                    {
                        m.SendMessage(string.Format("Looking for mobiles within range 16. You are at {0}", m.GetDistanceToSqrt(this)));
                    }
                }
                eable.Free();
            }

            {   // notify the tester that they are within range
                IPooledEnumerable eable = Map.Felucca.GetMobilesInRange(this.Location, 16);
                foreach (Mobile m in eable)
                {
                    if (m != null && m.Hidden == false && m.Player)
                    {
                        m.SendMessage(string.Format("Found {0} at {1}. RangeExit: {2}", m, m.Location, m.GetDistanceToSqrt(this)));
                    }
                }
                eable.Free();
            }

            Timer.DelayCall(TimeSpan.FromSeconds(5), new TimerStateCallback(Tick), new object[] { null });
        }


    }
}