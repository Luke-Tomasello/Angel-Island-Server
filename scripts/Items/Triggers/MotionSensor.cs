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

/* Items/Triggers/MotionSensor.cs
 * CHANGELOG:
 *  3/23/2024, Adam (EventSink_OnBoatMoving)
 *      Add and capture boat moving events. 
 *      In this way, we can simulate mobile OnMovement()
 * 	3/7/23, Yoar
 * 		Initial version.
 */

using System.Collections;

namespace Server.Items.Triggers
{
    public class MotionSensor : Item, ITrigger
    {
        public static void Initialize()
        {
            EventSink.BoatMoving += EventSink_OnBoatMoving;
        }

        public enum MSMotionType : byte
        {
            Move,
            Enter,
            Leave,
        }

        public override string DefaultName { get { return "Motion Sensor"; } }
        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }
        private Item m_Link;

        private MSMotionType m_MotionType;
        private int m_Range;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MSMotionType MotionType
        {
            get { return m_MotionType; }
            set { m_MotionType = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }

        [Constructable]
        public MotionSensor()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public override bool HandlesOnMovement { get { return true; } }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m.Hidden && m.AccessLevel > AccessLevel.Player)
                return;

            bool doTrigger = false;

            switch (m_MotionType)
            {
                case MSMotionType.Move:
                    {
                        doTrigger = CheckRange(m.Location);
                        break;
                    }
                case MSMotionType.Enter:
                    {
                        doTrigger = (!CheckRange(oldLocation) && CheckRange(m.Location));
                        break;
                    }
                case MSMotionType.Leave:
                    {
                        doTrigger = (CheckRange(oldLocation) && !CheckRange(m.Location));
                        break;
                    }
            }

            if (doTrigger)
                TriggerSystem.CheckTrigger(m, m_Link);
        }

        private bool CheckRange(Point3D loc)
        {
            return (this.Z >= loc.Z - 8 && this.Z < loc.Z + 16) && Utility.InRange(this.GetWorldLocation(), loc, m_Range);
        }
        private static void EventSink_OnBoatMoving(BoatMovingEventArgs e)
        {
            foreach (object o in e.Boat.GetMobilesOnDeck())
                if (o is Mobile m && m.Map != null && m.Map != Map.Internal)
                {
                    IPooledEnumerable eable = m.Map.GetItemsInRange(m.Location, range: 13);
                    foreach (Item thing in eable)
                        if (thing is MotionSensor ms)
                            ms.OnMovement(m, oldLocation: e.OldLocation);
                    eable.Free();
                }
        }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
                list.Add(new TriggerSystem.LinkCME());
        }

        public MotionSensor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(m_Event);

            // version 0
            writer.Write((Item)m_Link);

            writer.Write((byte)m_MotionType);
            writer.Write((int)m_Range);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Event = reader.ReadString();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Link = reader.ReadItem();

                        m_MotionType = (MSMotionType)reader.ReadByte();
                        m_Range = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}