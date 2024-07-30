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

/* Scripts\Items\Weapons\Maces\FireworksWand.cs
 * Changelog:
 *  3/29/2024, Adam (ITriggerable)
 *      Plus some configuration parms
 *  7/28/06, Rhiannon
 *		Fixed displayed name so it says "a fireworks wand" instead of "A wand of fireworks."
 *  7/26/06, Rhiannon
 *		Added OnSingleClick() to show number of charges.
 *	7/1/05, Adam
 *		Changed LootType.Blessed to LootType.Regular
 */

using Server.Items.Triggers;
using Server.Network;
using System;
using System.Collections;

namespace Server.Items
{
    public class FireworksWand : MagicWand, ITriggerable
    {
        public override int LabelNumber { get { return 1041424; } } // a fireworks wand

        private int m_Charges;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get { return m_Charges; }
            set { m_Charges = value; InvalidateProperties(); }
        }

        [Constructable]
        public FireworksWand()
            : this(100)
        {
        }

        [Constructable]
        public FireworksWand(int charges)
        {
            m_Charges = charges;
            LootType = LootType.Regular;
        }

        public FireworksWand(Serial serial)
            : base(serial)
        {
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1060741, m_Charges.ToString()); // charges: ~1_val~
        }

        public override void OnDoubleClick(Mobile from)
        {
            BeginLaunch(from, true);
        }

        private double m_ScheduleIgnite;
        [CommandProperty(AccessLevel.GameMaster)]
        public double ScheduleIgnite
        {
            get
            {
                SendSystemMessage("ScheduleIgnite: Milliseconds until ignition");
                return m_ScheduleIgnite;
            }
            set
            {
                m_ScheduleIgnite = value;

            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Ignite
        {
            get { return false; }
            set
            {
                if (value == true)
                {
                    InvalidateProperties();
                    if (m_ScheduleIgnite > 0)
                        Timer.DelayCall(TimeSpan.FromMilliseconds(m_ScheduleIgnite), new TimerStateCallback(IgniteTick), new object[] { m_RepeatCount });
                }
            }
        }

        private int m_RepeatCount = 1;
        [CommandProperty(AccessLevel.GameMaster)]
        public int RepeatCount
        {
            get
            {
                return m_RepeatCount;
            }
            set
            {
                m_RepeatCount = value;
            }
        }
        private void IgniteTick(object state)
        {
            object[] aState = (object[])state;
            int repeat = (int)aState[0];

            Map map = this.Map;
            if (this.RootParent != null)
                if (this.RootParent is Mobile)
                    map = (this.RootParent as Mobile).Map;
                else
                    map = (this.RootParent as Item).Map;

            BeginLaunchInternal(GetWorldLocation(), map);

            if (repeat > 0)
                Timer.DelayCall(TimeSpan.FromMilliseconds(2000), new TimerStateCallback(IgniteTick), new object[] { --repeat });
        }
        public void BeginLaunch(Mobile from, bool useCharges)
        {
            Map map = from.Map;

            if (map == null || map == Map.Internal)
                return;

            if (useCharges)
            {
                if (Charges > 0)
                {
                    --Charges;
                }
                else
                {
                    from.SendLocalizedMessage(502412); // There are no charges left on that item.
                    return;
                }
            }

            from.SendLocalizedMessage(502615); // You launch a firework!

            BeginLaunchInternal(GetWorldLocation(), from.Map);
        }
        public void BeginLaunchInternal(Point3D loc, Map map)
        {
            if (map == null || map == Map.Internal)
                return;

            Point3D ourLoc = loc;

            Point3D startLoc = new Point3D(ourLoc.X, ourLoc.Y, ourLoc.Z + 10);
            Point3D endLoc = new Point3D(startLoc.X + Utility.RandomMinMax(-2, 2), startLoc.Y + Utility.RandomMinMax(-2, 2), startLoc.Z + 32);

            Effects.SendMovingEffect(new Entity(Serial.Zero, startLoc, map), new Entity(Serial.Zero, endLoc, map),
                0x36E4, 5, 0, false, false);

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(FinishLaunch), new object[] { null, endLoc, map });
        }
        private void FinishLaunch(object state)
        {
            object[] states = (object[])state;

            Mobile unused = (Mobile)states[0];
            Point3D endLoc = (Point3D)states[1];
            Map map = (Map)states[2];

            int hue = Utility.Random(40);

            if (hue < 8)
                hue = 0x66D;
            else if (hue < 10)
                hue = 0x482;
            else if (hue < 12)
                hue = 0x47E;
            else if (hue < 16)
                hue = 0x480;
            else if (hue < 20)
                hue = 0x47F;
            else
                hue = 0;

            if (Utility.RandomBool())
                hue = Utility.RandomList(0x47E, 0x47F, 0x480, 0x482, 0x66D);

            int renderMode = Utility.RandomList(0, 2, 3, 4, 5, 7);

            Effects.PlaySound(endLoc, map, Utility.Random(0x11B, 4));
            Effects.SendLocationEffect(endLoc, map, 0x373A + (0x10 * Utility.Random(4)), 16, 10, hue, renderMode);
        }
        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return true;
        }
        public void OnTrigger(Mobile from)
        {
            Ignite = true;
        }
        public override void OnSingleClick(Mobile from)
        {
            ArrayList attrs = new ArrayList();

            int num = 1041424;
            attrs.Add(new EquipInfoAttribute(num, m_Charges));

            int number;

            if (Name == null)
            {
                number = 1017085;
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            EquipmentInfo eqInfo = new EquipmentInfo(number, Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));

            from.Send(new DisplayEquipmentInfo(this, eqInfo));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write(m_ScheduleIgnite);

            // version 1
            writer.Write(m_RepeatCount);

            // version 0
            writer.Write((int)m_Charges);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_ScheduleIgnite = reader.ReadDouble();
                        goto case 1;
                    }
                case 1:
                    {
                        m_RepeatCount = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Charges = reader.ReadInt();
                        break;
                    }
            }
        }
    }
}