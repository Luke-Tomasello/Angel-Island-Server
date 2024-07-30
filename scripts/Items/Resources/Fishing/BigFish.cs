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

/* items/Resources/Fishing/BigFish.cs
 * CHANGELOG:
 *  11/30/21, Adam (LunarInfluence)
 *      Add LunarInfluence() to our weight calc
 *  11/29/21, Yoar
 *      Big fish now have random weights.
 *      Big fish now have one of two hues (blue or green).
 *      Added m_Fisher in order to display the fisher.
 *      Added m_Caught to keep track of when the fish was caught. Useful for fishing events.
 */

using Server.Diagnostics;
using System;

namespace Server.Items
{
    public class BigFish : Item, ICarvable
    {
        private string m_Fisher;
        private DateTime m_Caught;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Fisher
        {
            get { return m_Fisher; }
            set { m_Fisher = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime Caught
        {
            get { return m_Caught; }
            set { m_Caught = value; InvalidateProperties(); }
        }

        public override int LabelNumber { get { return 1041112; } } // a big fish

        [Constructable]
        public BigFish()
            : base(0x09CC)
        {
            Weight = RandomWeight();
            Hue = Utility.RandomBool() ? 0x847 : 0x58C; // blue or green
        }

        // this is needed 'cause grandfathered big fish may still be stackable
        public override Item Dupe(int amount)
        {
            return base.Dupe(new BigFish(amount), amount);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Weight >= 20)
            {
                if (m_Fisher != null)
                    list.Add(1070857, m_Fisher); // Caught by ~1_fisherman~

                list.Add(1070858, ((int)Weight).ToString()); // ~1_weight~ stones
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (Weight >= 20)
            {
                if (m_Fisher != null)
                    LabelTo(from, 1070857, m_Fisher); // Caught by ~1_fisherman~

                LabelTo(from, 1070858, ((int)Weight).ToString()); // ~1_weight~ stones
            }
        }

        private int GetLunarOffset()
        {   // note: Even though x/y here are 0 (internal map), it doesn't appear to affect our calcs
            //  It seems OSI was using X to resolve subserver differences.
            return 1008146 + (int)Clock.GetMoonPhase(Map.Felucca, this.X, this.Y);
        }
        private double LunarInfluence()
        {
            switch (GetLunarOffset())
            {
                default:
                case 1008147: return 225.0 - 75;    // "a waxing crescent moon";
                case 1008148: return 225.0 - 50;    // "in the first quarter";
                case 1008149: return 225.0 - 25;    // "waxing gibbous";
                case 1008146: return 225.0;         // "a new moon";
                case 1008150: return 225.0;         // "a full moon";
                case 1008151: return 225.0 - 25;    // "waning gibbous";
                case 1008152: return 225.0 - 50;    // "in its last quarter";
                case 1008153: return 225.0 - 75;    // "a waning crescent";
            }
        }
        public double RandomWeight()
        {
#if RunUO
			return Utility.RandomMinMax( 3, 200 );
#else
            double result = Math.Max(1.0, LunarInfluence() * (100.0 - Math.Sqrt(Utility.RandomMinMax(0, 10000))) / 100.0);
            LogHelper logger = new LogHelper("bigfish weight.log", false, true);
            logger.Log(LogType.Text, string.Format("A {1} stone big fish was caught during: '{0}'", Server.Text.Cliloc.Lookup[GetLunarOffset()], result));
            logger.Finish();
            return result;
#endif
        }

        public void Carve(Mobile from, Item item)
        {
            base.ScissorHelper(item as Scissors, from, new RawFishSteak(), Math.Max(16, (int)Weight) / 2, false);
        }

        public BigFish(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((string)m_Fisher);
            writer.Write((DateTime)m_Caught);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Fisher = Utility.Intern(reader.ReadString());
                        m_Caught = reader.ReadDateTime();
                        break;
                    }
            }
        }
    }
}