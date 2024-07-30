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

/* Items/Misc/FishController.cs
 * CHANGELOG:
 *  3/26/2024, Adam
 *      Percolate construction errors up to the controller for display to staff.
 *      These will often be AccessLevel errors (setting props you do not have sufficient rights to set.)
 *	12/28/23, Yoar
 *		Implemented IEventController
 *  12/28/23, Yoar
 *      Initial version.
 *      
 *      Can be used in event areas for players to fish up special items.
 */

using Server.Engines;
using Server.Mobiles;
using System;

namespace Server.Items
{
    public class FishController : Item
    {
        public static int FishRange = 4;

        public override string DefaultName { get { return "Fish Controller"; } }

        public override bool Decays { get { return (Spawner != null); } }
        public override TimeSpan DecayTime { get { return TimeSpan.FromDays(1.0); } }

        private string m_SpawnStr;

        private int m_MaxQuantity;
        private int m_CurQuantity;

        private double m_ReqSkill;
        private double m_MinSkill;
        private double m_MaxSkill;

        private string m_SuccessMessage;
        private string m_FailureMessage;

        private TimeSpan m_RestockDelay;
        private DateTime m_NextRestock;

        private Event m_FishingEvent;

        [CommandProperty(AccessLevel.GameMaster)]
        public string SpawnStr
        {
            get { return m_SpawnStr; }
            set { m_SpawnStr = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxQuantity
        {
            get { return m_MaxQuantity; }
            set { m_MaxQuantity = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurQuantity
        {
            get { return m_CurQuantity; }
            set { m_CurQuantity = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ReqSkill
        {
            get { return m_ReqSkill; }
            set { m_ReqSkill = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double MinSkill
        {
            get { return m_MinSkill; }
            set { m_MinSkill = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double MaxSkill
        {
            get { return m_MaxSkill; }
            set { m_MaxSkill = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string SuccessMessage
        {
            get { return m_SuccessMessage; }
            set { m_SuccessMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string FailureMessage
        {
            get { return m_FailureMessage; }
            set { m_FailureMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan RestockDelay
        {
            get { return m_RestockDelay; }
            set { m_RestockDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextRestock
        {
            get { return m_NextRestock; }
            set { m_NextRestock = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Event Event
        {
            get { return m_FishingEvent; }
            set { }
        }

        public bool IsActive { get { return (Spawner is EventSpawner || m_FishingEvent.EventRunning); } }

        [Constructable]
        public FishController()
            : this(null, 4)
        {
        }

        [Constructable]
        public FishController(string spawnStr)
            : this(spawnStr, 4)
        {
        }

        [Constructable]
        public FishController(string spawnStr, int quantity)
            : base(0x09CC)
        {
            Movable = false;
            Visible = false;

            m_SpawnStr = spawnStr;

            m_MaxQuantity = m_CurQuantity = quantity;

            m_ReqSkill = 0.0;
            m_MinSkill = 0.0;
            m_MaxSkill = 200.0;

            m_RestockDelay = TimeSpan.FromHours(2.0);

            m_FishingEvent = new Event(this);
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (!m_FishingEvent.EventRunning)
            {
                LabelTo(from, "event not running");
                if (m_FishingEvent.Countdown > TimeSpan.Zero)
                    LabelTo(from, "{0} activating in {1} seconds", this.GetType().Name, string.Format("{0:N2}", m_FishingEvent.Countdown.TotalSeconds));

                LabelTo(from, "(inactive)");
            }
            else
            {
                LabelTo(from, "event running");
                LabelTo(from, "(active)");
            }
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public static FishController Find(Mobile from, Point3D loc)
        {
            Map map = from.Map;

            if (map == null)
                return null;

            foreach (Item item in map.GetItemsInRange(loc, FishRange))
            {
                if (item is FishController)
                {
                    FishController controller = (FishController)item;

                    if (!controller.IsActive)
                        continue;

                    controller.CheckRestock();

                    if (controller.CurQuantity > 0)
                        return controller;
                }
            }

            return null;
        }

        public void OnFish(Mobile from, out Item fish, out string mssg)
        {
            if (from.Skills[SkillName.Fishing].Value >= m_ReqSkill && from.CheckSkill(SkillName.Fishing, m_MinSkill, m_MaxSkill, contextObj: new object[2]))
            {
                string reason = string.Empty;
                fish = SpawnEngine.Build<Item>(m_SpawnStr, ref reason);

                if (reason != string.Empty)
                    this.SendSystemMessage(string.Format("{0}: {1}", m_SpawnStr, reason));

                if (fish != null)
                {
                    if (fish is BigFish)
                    {
                        ((BigFish)fish).Fisher = Utility.Intern(Misc.Titles.FormatShort(from));
                        ((BigFish)fish).Caught = DateTime.UtcNow;
                    }

                    mssg = m_SuccessMessage;

                    ConsumeFish();

                    return;
                }
            }

            fish = null;
            mssg = m_FailureMessage;
        }

        private void CheckRestock()
        {
            if (Spawner == null && DateTime.UtcNow >= m_NextRestock)
            {
                m_CurQuantity = m_MaxQuantity;
                m_NextRestock = DateTime.MinValue;
            }
        }

        private void ConsumeFish()
        {
            m_CurQuantity--;

            if (Spawner != null)
            {
                if (m_CurQuantity <= 0)
                    Delete();
            }
            else if (m_NextRestock == DateTime.MinValue && m_CurQuantity < m_MaxQuantity)
            {
                m_NextRestock = DateTime.UtcNow + m_RestockDelay;
            }
        }

        public FishController(Serial serial)
            : base(serial)
        {
            m_FishingEvent = new Event(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 2
            //writer.Write(m_Event);

            // version 1
            m_FishingEvent.Serialize(writer);

            writer.Write((string)m_SpawnStr);

            writer.Write((int)m_MaxQuantity);
            writer.Write((int)m_CurQuantity);

            writer.Write((double)m_ReqSkill);
            writer.Write((double)m_MinSkill);
            writer.Write((double)m_MaxSkill);

            writer.Write((string)m_SuccessMessage);
            writer.Write((string)m_FailureMessage);

            writer.Write((TimeSpan)m_RestockDelay);
            writer.WriteDeltaTime(m_NextRestock);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        //m_Event = reader.ReadString();

                        goto case 1;
                    }
                case 1:
                    {
                        m_FishingEvent.Deserialize(reader);

                        goto case 0;
                    }
                case 0:
                    {
                        m_SpawnStr = reader.ReadString();

                        m_MaxQuantity = reader.ReadInt();
                        m_CurQuantity = reader.ReadInt();

                        m_ReqSkill = reader.ReadDouble();
                        m_MinSkill = reader.ReadDouble();
                        m_MaxSkill = reader.ReadDouble();

                        m_SuccessMessage = reader.ReadString();
                        m_FailureMessage = reader.ReadString();

                        m_RestockDelay = reader.ReadTimeSpan();
                        m_NextRestock = reader.ReadDeltaTime();

                        break;
                    }
            }
        }
    }
}