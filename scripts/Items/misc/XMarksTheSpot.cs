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

/* Items/Misc/XMarksTheSpot.cs
 * ChangeLog:
 *  12/12/21: Yoar
 *		Added "m.NetState != null" check.
 *  11/14/21m Adam (too far)
 *      Add a check to prevent players drom digging up treasure more than 3 tiles away
 *      Have Bury(Item item, int level) understand that this is a level 6 chest. (we were defaulting to the minimum skill requirements)
 *      Decrease m_MobileIgnore from 7 seconds to 2.5 seconds
 *  11/13/21, Adam (OnMovement)
 *      if (m.Player == false) return;
 *  11/08/21: Yoar
 *		Initial version.
 */

using Server.Diagnostics;
using Server.Network;
using Server.SkillHandlers;
using System;

namespace Server.Items
{
    public class XMarksTheSpot : BaseAddon, IDetectable, IPassiveDetectable
    {
        private int m_ReqSkillFixed, m_MinSkillFixed, m_MaxSkillFixed;
        private double m_PassiveScalar;
        private Item m_BuriedItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public int ReqSkillFixed
        {
            get { return m_ReqSkillFixed; }
            set { m_ReqSkillFixed = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MinSkillFixed
        {
            get { return m_MinSkillFixed; }
            set { m_MinSkillFixed = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxSkillFixed
        {
            get { return m_MaxSkillFixed; }
            set { m_MaxSkillFixed = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double PassiveScalar
        {
            get { return m_PassiveScalar; }
            set { m_PassiveScalar = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item BuriedItem
        {
            get { return m_BuriedItem; }
            set { m_BuriedItem = value; }
        }

        [Constructable]
        public XMarksTheSpot()
            : base()
        {
            AddComponent(new InternalComponent(0x1B11, 33), 0, 0, 0);
            AddComponent(new InternalComponent(0x1B12, 33), 0, 0, 0);
            m_MaxSkillFixed = 1000;
            m_PassiveScalar = 0.33;
        }

        public static void Bury(Item item)
        {
            Bury(item, 1);
        }

        public static Item Bury(Item item, int level)
        {
            XMarksTheSpot x = new XMarksTheSpot();

            int reqFixed;

            switch (level)
            {
                default:
                case 0: reqFixed = 300; break;
                case 1: reqFixed = 360; break;
                case 2: reqFixed = 760; break;
                case 3: reqFixed = 840; break;
                case 4: reqFixed = 920; break;
                case 5:
                case 6: // new chests are level 6
                    reqFixed = 1000; break;
            }

            x.ReqSkillFixed = reqFixed;
            x.MinSkillFixed = reqFixed - 250;
            x.MaxSkillFixed = reqFixed + 250;
            x.BuriedItem = item;

            x.MoveToWorld(item.GetWorldLocation(), item.Map);

            item.MoveToIntStorage();

            return x;
        }

        public bool Revealed
        {
            get { return Components.Count != 0 && ((Item)Components[0]).Visible; }
        }

        public bool OnDetect(Mobile m)
        {
            return CheckDetect(m, 1.0, true);
        }

        public bool OnPassiveDetect(Mobile m)
        {
            if (!m.Player)
                return false;

            double dist = m.GetDistanceToSqrt(this);

            if (dist >= 6.0)
                return false;

            double rangeScalar = 1.0 - (dist / 6.0);

            return CheckDetect(m, Math.Sqrt(rangeScalar) * m_PassiveScalar, false);
        }

        private static Memory m_MobileIgnore = new Memory();

        private bool CheckDetect(Mobile m, double scalar, bool skillUsed)
        {
            if (Revealed || !m.Player)
                return false;

            int skillFixed = m.Skills[SkillName.DetectHidden].Fixed;

            if (skillFixed < m_ReqSkillFixed)
                return false;

            double chance = (double)(skillFixed - m_MinSkillFixed) / (m_MaxSkillFixed - m_MinSkillFixed);

            if (scalar != 1.0)
                chance *= scalar;

            if (Utility.RandomDouble() < chance)
            {
                if (skillUsed)
                {
                    Reveal(m);
                }
                else if (!m_MobileIgnore.Recall(m))
                {
                    m_MobileIgnore.Remember(m, 2.5);

                    if (m.NetState != null)
                        m.NetState.Send(new AsciiMessage(m.Serial, m.Body, MessageType.Regular, 0, 3, m.Name, "*You sense something may be buried nearby.*"));
                }

                return true;
            }

            return false;
        }

        private void Reveal(Mobile m)
        {
            foreach (Item c in Components)
                c.Visible = true;

            if (m.NetState != null && Components.Count != 0)
            {
                Item c = (Item)Components[0];

                m.NetState.Send(new AsciiMessage(c.Serial, c.ItemID, MessageType.Regular, 0, 3, c.Name, "*You have uncovered buried treasure!*"));

                LogHelper logger = new LogHelper("Level6DungeonChestOpened.log", World.GetAdminAcct(), false, true);
                logger.Log(LogType.Mobile, m, string.Format("*You have uncovered buried treasure!* {0}.", this));
                logger.Finish();
            }

            StartHideTimer();
        }

        private Timer m_HideTimer;

        private void StartHideTimer()
        {
            StopHideTimer();

            (m_HideTimer = new HideTimer(this)).Start();
        }

        private void StopHideTimer()
        {
            if (m_HideTimer != null)
            {
                m_HideTimer.Stop();
                m_HideTimer = null;
            }
        }

        private class HideTimer : Timer
        {
            private XMarksTheSpot m_XItem;

            public HideTimer(XMarksTheSpot x)
                : base(TimeSpan.FromSeconds(10.0))
            {
                m_XItem = x;
            }

            protected override void OnTick()
            {
                m_XItem.StopHideTimer();

                foreach (Item c in m_XItem.Components)
                    c.Visible = false;
            }
        }

        public static bool BeginDig(Mobile m, Item tool, object targeted)
        {
            if (!m.CanBeginAction(typeof(XMarksTheSpot)))
                return false;

            Map map = m.Map;

            if (map == null || map == Map.Internal)
                return false;

            if (!(targeted is IPoint3D))
                return false;

            Point3D loc = new Point3D((IPoint3D)targeted);

            XMarksTheSpot found = null;

            foreach (Item item in map.GetItemsInRange(loc, 0))
            {
                if (item is XMarksTheSpot && m.GetDistanceToSqrt(item) <= 3)
                {
                    XMarksTheSpot spot = (XMarksTheSpot)item;

                    if (spot.Revealed && m.BeginAction(typeof(XMarksTheSpot)))
                    {
                        found = spot;
                        break;
                    }
                }
            }

            if (found != null)
            {

                if (m.Mounted)
                {
                    m.SendLocalizedMessage(501864); // You can't dig while riding or flying.
                }
                else if (m.GetDistanceToSqrt(loc) > 3)
                {
                    m.SendLocalizedMessage(501860);  // "That is too far away.";
                }
                else if (m.IsBodyMod && !m.Body.IsHuman)
                {
                    m.SendLocalizedMessage(501865); // You can't mine while polymorphed.
                }
                else
                {
                    if (found.BuriedItem != null && !found.BuriedItem.Deleted)
                        found.BuriedItem.MoveToIntStorage();

                    found.StopHideTimer();
                    found.StartDigTimer(m, tool);
                }

                return true;
            }

            return false;
        }

        private DigTimer m_DigTimer;

        private void StartDigTimer(Mobile m, Item tool)
        {
            StopDigTimer();

            (m_DigTimer = new DigTimer(this, m, tool)).Start();
        }

        private void StopDigTimer()
        {
            if (m_DigTimer != null)
            {
                m_DigTimer.From.EndAction(typeof(XMarksTheSpot));

                m_DigTimer.Stop();
                m_DigTimer = null;
            }
        }

        private class DigTimer : Timer
        {
            private XMarksTheSpot m_XItem;
            private Mobile m_From;
            private Item m_Tool;
            private long m_NextSkillTime;
            private DateTime m_NextSpellTime;
            private long m_NextActionTime;
            private DateTime m_LastMoveTime;
            private int m_Count;
            private DirtAddon m_Dirt;

            public Mobile From { get { return m_From; } }

            public DigTimer(XMarksTheSpot x, Mobile from, Item tool)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                m_XItem = x;
                m_From = from;
                m_Tool = tool;
                m_NextSkillTime = from.NextSkillTime;
                m_NextSpellTime = from.NextSpellTime;
                m_NextActionTime = from.NextActionTime;
                m_LastMoveTime = from.LastMoveTime;
            }

            protected override void OnTick()
            {
                if (!Validate())
                {
                    m_From.EndAction(typeof(XMarksTheSpot));

                    if (m_Dirt != null)
                        m_Dirt.Delete();

                    if (m_XItem.BuriedItem != null)
                        m_XItem.BuriedItem.MoveToIntStorage();

                    m_XItem.StopDigTimer();
                    m_XItem.StartHideTimer();

                    return;
                }

                Point3D loc = m_XItem.Location;
                Map map = m_XItem.Map;
                Item toDig = m_XItem.BuriedItem;

                m_From.RevealingAction();
                m_From.Direction = m_From.GetDirectionTo(loc);

                m_Count++;

                bool done = false;

                if (m_Count == 2)
                {
                    m_Dirt = new DirtAddon();
                    m_Dirt.SetStage(0);
                    m_Dirt.MoveToWorld(loc, map);
                }
                else if (m_Count == 5)
                {
                    m_Dirt.SetStage(1);
                }
                else if (m_Count == 10)
                {
                    m_Dirt.SetStage(2);

                    if (toDig == null || toDig.Deleted)
                    {
                        done = true;
                    }
                    else if (m_Count == 10)
                    {
                        FlipChest(toDig);

                        toDig.RetrieveItemFromIntStorage(new Point3D(loc.X, loc.Y, loc.Z - 15), map);
                    }
                }
                else if (m_Count > 10)
                {
                    if (toDig == null || toDig.Deleted)
                        done = true;
                    else
                        done = (++toDig.Z >= loc.Z);
                }

                if (done)
                {
                    m_From.EndAction(typeof(XMarksTheSpot));

                    if (toDig != null)
                        m_XItem.BuriedItem = null;

                    m_XItem.Delete();
                }
                else
                {
                    if (m_From.Body.IsHuman && !m_From.Mounted)
                        m_From.Animate(11, 5, 1, true, false, 0);

                    new SoundTimer(m_From, 0x125 + (m_Count % 2)).Start();
                }
            }

            private bool Validate()
            {
                Map map = m_XItem.Map;

                if (map == null || map == Map.Internal || m_XItem.Deleted || !m_From.CheckAlive(false) || !m_Tool.IsChildOf(m_From.Backpack))
                    return false;

                if (m_From.Mounted)
                {
                    m_From.SendLocalizedMessage(501864); // You can't dig while riding or flying.
                    return false;
                }
                else if (m_From.IsBodyMod && !m_From.Body.IsHuman)
                {
                    m_From.SendLocalizedMessage(501865); // You can't mine while polymorphed.
                    return false;
                }
                else if (m_NextSkillTime != m_From.NextSkillTime || m_NextSpellTime != m_From.NextSpellTime || m_NextActionTime != m_From.NextActionTime)
                {
                    // TODO: Message?
                    return false;
                }
                else if (m_LastMoveTime != m_From.LastMoveTime)
                {
                    m_From.SendLocalizedMessage(503023); // You cannot move around while digging up treasure. You will need to start digging anew.
                    return false;
                }

                int z = m_XItem.Z;
                Item toDig = m_XItem.BuriedItem;

                if (toDig != null && !toDig.Deleted && toDig.Map != null && toDig.Map != Map.Internal)
                {
                    int itemTop = toDig.Z + toDig.ItemData.CalcHeight;

                    if (itemTop > z)
                        z = itemTop;
                }

                if (!Utility.CanFit(map, m_XItem.X, m_XItem.Y, z, 16, Utility.CanFitFlags.checkBlocksFit | Utility.CanFitFlags.checkMobiles))
                {
                    m_From.SendLocalizedMessage(503024); // You stop digging because something is directly on top of the treasure chest.
                    return false;
                }

                return true;
            }

            private class DirtAddon : BaseAddon
            {
                private Timer m_Timer;

                public DirtAddon()
                    : base()
                {
                    AddComponent(new AddonComponent(0x1), 0, 0, 0);
                    AddComponent(new AddonComponent(0x1), 0, -1, 0);
                    m_Timer = Timer.DelayCall(TimeSpan.FromMinutes(1.0), new TimerCallback(Delete));
                }

                public void SetStage(int stage)
                {
                    switch (stage)
                    {
                        default:
                        case 0:
                            {
                                ((Item)Components[0]).ItemID = 0x912;
                                ((Item)Components[1]).ItemID = 0x912;
                                break;
                            }
                        case 1:
                            {
                                ((Item)Components[0]).ItemID = 0x913;
                                ((Item)Components[1]).ItemID = 0x912;
                                break;
                            }
                        case 2:
                            {
                                ((Item)Components[0]).ItemID = 0x914;
                                ((Item)Components[1]).ItemID = 0x914;
                                break;
                            }
                    }
                }

                public DirtAddon(Serial serial)
                    : base(serial)
                {
                }

                public override void Serialize(GenericWriter writer)
                {
                    base.Serialize(writer);

                    writer.WriteEncodedInt(0); // version
                }

                public override void Deserialize(GenericReader reader)
                {
                    base.Deserialize(reader);

                    int version = reader.ReadEncodedInt();

                    Delete();
                }
            }

            private static void FlipChest(Item item)
            {
                switch (item.ItemID)
                {
                    case 0x9AB: item.ItemID = 0xE7C; break; // Metal chest
                    case 0xE41: item.ItemID = 0xE40; break; // Metal golden chest
                    case 0xe43: item.ItemID = 0xe42; break; // Wooden chest
                }
            }

            private class SoundTimer : Timer
            {
                private Mobile m_From;
                private int m_SoundID;

                public SoundTimer(Mobile from, int soundID)
                    : base(TimeSpan.FromSeconds(0.9))
                {
                    Priority = TimerPriority.TenMS;
                    m_From = from;
                    m_SoundID = soundID;
                }

                protected override void OnTick()
                {
                    m_From.PlaySound(m_SoundID);
                }
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_BuriedItem != null)
                m_BuriedItem.Delete();

            StopHideTimer();
            StopDigTimer();
        }

        public XMarksTheSpot(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_ReqSkillFixed);
            writer.Write((int)m_MinSkillFixed);
            writer.Write((int)m_MaxSkillFixed);
            writer.Write((double)m_PassiveScalar);
            writer.Write((Item)m_BuriedItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_ReqSkillFixed = reader.ReadInt();
            m_MinSkillFixed = reader.ReadInt();
            m_MaxSkillFixed = reader.ReadInt();
            m_PassiveScalar = reader.ReadDouble();
            m_BuriedItem = reader.ReadItem();
        }

        private class InternalComponent : AddonComponent
        {
            public InternalComponent(int itemID, int hue)
                : base(itemID)
            {
                Name = "X marks the spot";
                Hue = hue;
                Visible = false;
            }

            public InternalComponent(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.WriteEncodedInt(0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadEncodedInt();

                Visible = false;
            }
        }
    }
}