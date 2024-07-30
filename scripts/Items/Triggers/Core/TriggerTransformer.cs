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

/* Items/Triggers/Core/TriggerTransformer.cs
 * CHANGELOG:
 *  4/8/2024, Adam (Running)
 *      Design flaw: Disallow staff from setting Running. Setting this to true starts the timer with a 'null list'
 *          resulting in a crash the first time the timer ticks.
 *  12/28/23, Yoar
 *      Added Herder transform type
 *  12/14/23, Yoar
 *      Added ControlMaster transform type
 * 	3/7/23, Yoar
 * 		Initial version.
 */

using Server.Engines.PartySystem;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    public class TriggerTransformer : Item, ITriggerable, ITrigger
    {
        public enum _TransformType : byte
        {
            From,
            Party,
            InRange,
            InBounds,
            InRegion,
            ControlMaster,
            Herder,
            Controlled,
            Killers,
        }

        public override string DefaultName { get { return "Trigger Transformer"; } }
        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }
        private Item m_Link;

        private _TransformType m_TransformType;
        private int m_Range;
        private Rectangle2D m_Bounds;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public _TransformType TransformType
        {
            get { return m_TransformType; }
            set { m_TransformType = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D Bounds
        {
            get { return m_Bounds; }
            set { m_Bounds = value; }
        }

        [Constructable]
        public TriggerTransformer()
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
        public bool CanTrigger(Mobile from)
        {
            if (Running)
                return false;

            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            List<Mobile> list = new List<Mobile>();

            Transform(from, list);

            foreach (Mobile m in list)
            {
                if (TriggerSystem.CanTrigger(m, m_Link))
                    return true;
            }

            return false;
        }

        public void OnTrigger(Mobile from)
        {
            if (Running)
                return;

            List<Mobile> list = new List<Mobile>();

            Transform(from, list);

            if (list.Count != 0)
                StartTimer(list);
        }

        private void Transform(Mobile from, List<Mobile> list)
        {
            switch (m_TransformType)
            {
                case _TransformType.From:
                    {
                        if (from == null)
                            break;

                        list.Add(from);

                        break;
                    }
                case _TransformType.Party:
                    {
                        if (from == null)
                            break;

                        Party party = from.Party as Party;

                        if (party == null)
                            goto case _TransformType.From;

                        foreach (PartyMemberInfo mi in party.Members)
                        {
                            if (mi.Mobile == null)
                                continue;

                            list.Add(mi.Mobile);
                        }

                        break;
                    }
                case _TransformType.InRange:
                    {
                        foreach (Mobile m in GetMobilesInRange(m_Range))
                            list.Add(m);

                        break;
                    }
                case _TransformType.InBounds:
                    {
                        Map map = this.Map;

                        if (map == null)
                            break;

                        foreach (Mobile m in map.GetMobilesInBounds(m_Bounds))
                            list.Add(m);

                        break;
                    }
                case _TransformType.InRegion:
                    {
                        Map map = this.Map;

                        if (map == null)
                            break;

                        Region region = Region.Find(this.GetWorldLocation(), map);

                        if (region.IsDefault)
                            break;

                        foreach (Mobile m in region.Mobiles.Values)
                            list.Add(m);

                        break;
                    }
                case _TransformType.ControlMaster:
                    {
                        BaseCreature bc = from as BaseCreature;

                        if (bc == null || !bc.Controlled || bc.ControlMaster == null)
                            break;

                        list.Add(bc.ControlMaster);

                        break;
                    }
                case _TransformType.Herder:
                    {
                        BaseCreature bc = from as BaseCreature;

                        if (bc == null)
                            break;

                        Mobile herder = bc.GetHerder();

                        if (herder != null)
                            list.Add(herder);

                        break;
                    }
                case _TransformType.Controlled:
                    {
                        if (from == null)
                            break;

                        foreach (Mobile m in from.Followers)
                            if (m is BaseCreature pet && !pet.Deleted && pet.Controlled && pet.ControlMaster == from)
                                list.Add(m);

                        break;
                    }
                case _TransformType.Killers:
                    {
                        if (from is BaseCreature bc)
                        {
                            // find legit damagers
                            List<DamageStore> dsLlist = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);

                            // divvy up points between party members
                            SortedList<Mobile, int> Results = BaseCreature.ProcessDamageStore(dsLlist);

                            // anti-cheezing: Players have found that healing a monster (or allowing it to heal itself,) can yield unlimited damage points.
                            //  We therefore limit the damage points to no more than the creature's HitsMax
                            BaseCreature.ClipDamageStore(ref Results, bc);

                            list.AddRange(Results.Keys);
                        }

                        break;
                    }
            }
        }

        private InternalTimer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Running
        {
            get { return (m_Timer != null); }
            //set
            //{
            //    if (Running != value)
            //    {
            //        if (value)
            //            StartTimer(null);
            //        else
            //            StopTimer();
            //    }
            //}
        }

        private void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private void StartTimer(List<Mobile> list)
        {
            StopTimer();
            (m_Timer = new InternalTimer(this, list)).Start();
        }

        private void OnTick(List<Mobile> list)
        {
            StopTimer();
            foreach (Mobile m in list)
                TriggerSystem.CheckTrigger(m, m_Link);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        public override void OnAfterDelete()
        {
            StopTimer();
        }

        public TriggerTransformer(Serial serial)
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

            writer.Write((byte)m_TransformType);
            writer.Write((int)m_Range);
            writer.Write((Rectangle2D)m_Bounds);

            if (m_Timer != null)
            {
                writer.Write((bool)true);

                writer.WriteMobileList(m_Timer.List);
            }
            else
            {
                writer.Write((bool)false);
            }
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

                        m_TransformType = (_TransformType)reader.ReadByte();
                        m_Range = reader.ReadInt();
                        m_Bounds = reader.ReadRect2D();

                        if (reader.ReadBool())
                            StartTimer(reader.ReadStrongMobileList());

                        break;
                    }
            }
        }

        private class InternalTimer : Timer
        {
            private TriggerTransformer m_Trigger;
            private List<Mobile> m_List;

            public List<Mobile> List { get { return m_List; } }

            public InternalTimer(TriggerTransformer trigger, List<Mobile> list)
                : base(TimeSpan.Zero)
            {
                m_Trigger = trigger;
                m_List = list;
            }

            protected override void OnTick()
            {
                m_Trigger.OnTick(m_List);
            }
        }
    }
}