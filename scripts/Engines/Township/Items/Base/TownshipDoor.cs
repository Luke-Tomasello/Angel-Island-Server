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

/* Engines/Township/Items/Base/TownshipDoor.cs
 * CHANGELOG:
 *  9/7/2023, Adam (OnDoubleClick)
 *      Now call TownshipItemHelper.LikelyDamager() before invoking all the damage logic which also includes AFK checks.
 *  7/5/23, Yoar
 *      Doors are now chopable
 * 3/23/22, Yoar
 *	    Initial version.
 */

using Server.Items;
using System;
using System.Collections;

namespace Server.Township
{
    public class TownshipDoor : LockpickableDoor, ITownshipItem, IChopable
    {
        private int m_HitsMax;
        private int m_Hits;
        private DateTime m_LastDamage;
        private DateTime m_LastRepair;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int HitsMax
        {
            get { return m_HitsMax; }
            set
            {
                if (value < 0)
                    value = 0;

                if (m_HitsMax != value)
                {
                    int perc;

                    if (m_HitsMax > 0)
                        perc = Math.Max(0, Math.Min(100, 100 * m_Hits / m_HitsMax));
                    else
                        perc = 100;

                    m_HitsMax = value;

                    this.Hits = perc * m_HitsMax / 100;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Hits
        {
            get { return m_Hits; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > m_HitsMax)
                    value = m_HitsMax;

                if (m_Hits != value)
                {
                    int hitsOld = m_Hits;

                    m_Hits = value;

                    OnHitsChanged(hitsOld);
                }
            }
        }

        protected void SetHits(int hits)
        {
            m_HitsMax = m_Hits = hits;
        }

        public virtual void OnHitsChanged(int hitsOld)
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DateTime LastDamage
        {
            get { return m_LastDamage; }
            set { m_LastDamage = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public TimeSpan LastDamageAgo
        {
            get
            {
                TimeSpan ts = DateTime.UtcNow - m_LastDamage;

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
            set { this.LastDamage = DateTime.UtcNow - value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DateTime LastRepair
        {
            get { return m_LastRepair; }
            set { m_LastRepair = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public TimeSpan LastRepairAgo
        {
            get
            {
                TimeSpan ts = DateTime.UtcNow - m_LastRepair;

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
            set { this.LastRepair = DateTime.UtcNow - value; }
        }

        [Constructable]
        public TownshipDoor(DoorType doorType, DoorFacing facing)
            : this(DoorHelper.GetInfo(doorType), facing)
        {
        }

        private TownshipDoor(DoorHelper.DoorInfo info, DoorFacing facing)
            : this(info.BaseItemID, info.BaseItemID + 1, info.BaseSoundID, info.BaseSoundID + 7, facing)
        {
        }

        [Constructable]
        public TownshipDoor(int closedID, int openedID, int openedSound, int closedSound, DoorFacing facing)
            : base(closedID, openedID, openedSound, closedSound, facing)
        {
            TownshipItemHelper.Register(this);
        }

        public virtual void OnBuild(Mobile m)
        {
            int hits = GetInitialHits(m);

            this.HitsMax = hits;
            this.Hits = hits;
            this.LastDamage = DateTime.UtcNow;
            this.LastRepair = DateTime.UtcNow;
        }

        protected virtual int GetInitialHits(Mobile m)
        {
            int carp = (int)m.Skills[SkillName.Carpentry].Value;
            int tink = (int)m.Skills[SkillName.Tinkering].Value;
            int mine = (int)m.Skills[SkillName.Mining].Value;
            int jack = (int)m.Skills[SkillName.Lumberjacking].Value;

            int smit = (int)m.Skills[SkillName.Blacksmith].Value;
            int alch = (int)m.Skills[SkillName.Alchemy].Value;
            int item = (int)m.Skills[SkillName.ItemID].Value;
            int mace = (int)m.Skills[SkillName.Macing].Value;
            int scrb = (int)m.Skills[SkillName.Inscribe].Value;
            int dtct = (int)m.Skills[SkillName.DetectHidden].Value;
            int cart = (int)m.Skills[SkillName.Cartography].Value;

            int baseHits = 100;

            //"main" skills add the most
            baseHits += carp / 4; //+25 @ GM
            baseHits += tink / 4; //+25 @ GM
            baseHits += mine / 4; //+25 @ GM
            baseHits += jack / 4; //+25 @ GM

            //"support" skills add some more
            baseHits += smit / 10;//+10 @ GM
            baseHits += alch / 10;//+10 @ GM
            baseHits += item / 10;//+10 @ GM
            baseHits += mace / 10;//+10 @ GM
            baseHits += scrb / 10;//+10 @ GM
            baseHits += dtct / 10;//+10 @ GM
            baseHits += cart / 10;//+10 @ GM

            baseHits /= 2;

            return baseHits;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            TownshipItemHelper.OnLocationChange(this, oldLocation);
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            TownshipItemHelper.OnMapChange(this);
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (HitsMax > 0)
                TownshipItemHelper.Inspect(from, this);
        }

        public override void OnDoubleClick(Mobile from)
        {
            // 9/8/23, Yoar: Disabled double-click attack
#if false
            if (HitsMax > 0 && from.Warmode && TownshipItemHelper.LikelyDamager(from, this))
            {
                if (!from.InRange(this.GetWorldLocation(), 2))
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                else 
                    TownshipItemHelper.BeginDamageDelayed(from, this);
            }
            else
#endif
            {
                base.OnDoubleClick(from);
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            TownshipItemHelper.AddContextMenuEntries(this, from, list);
        }

        public void OnChop(Mobile from)
        {
            TownshipItemHelper.OnChop(this, from);
        }

        public virtual bool CanDestroy(Mobile m)
        {
            return TownshipItemHelper.IsOwner(this, m);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            TownshipItemHelper.Unregister(this);
        }

        public TownshipDoor(Serial serial)
            : base(serial)
        {
            TownshipItemHelper.Register(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_HitsMax);
            writer.Write((int)m_Hits);
            writer.Write((DateTime)m_LastDamage);
            writer.Write((DateTime)m_LastRepair);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        if (version < 1)
                        {
                            Mobile owner = reader.ReadMobile();
                            DateTime placed = reader.ReadDateTime();

                            ValidationQueue<TownshipDoor>.Enqueue(this, new object[] { owner, placed });
                        }

                        m_HitsMax = reader.ReadInt();
                        m_Hits = reader.ReadInt();
                        m_LastDamage = reader.ReadDateTime();
                        m_LastRepair = reader.ReadDateTime();

                        break;
                    }
            }
        }

        private void Validate(object state)
        {
            object[] array = (object[])state;

            Mobile owner = (Mobile)array[0];
            DateTime placed = (DateTime)array[1];

            TownshipItemHelper.SetOwnership(this, owner, placed);
        }
    }
}