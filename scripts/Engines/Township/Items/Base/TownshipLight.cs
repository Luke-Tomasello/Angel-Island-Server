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

/* Engines/Township/Items/Base/TownshipLight.cs
 * CHANGELOG:
 *  9/7/2023, Adam (OnDoubleClick)
 *      Now call TownshipItemHelper.LikelyDamager() before invoking all the damage logic which also includes AFK checks.
 * 3/17/22, Adam
 *  Update OnBuild to use new parm list
 * 11/23/21, Yoar
 *	    Initial version.
 */

using Server.Items;
using System;
using System.Collections;

namespace Server.Township
{
    public abstract class TownshipLight : BaseLight, ITownshipItem, IChopable
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

        public TownshipLight(int itemID)
            : base(itemID)
        {
            Movable = false;

            TownshipItemHelper.Register(this);
        }

        public virtual void OnBuild(Mobile from)
        {
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

        public TownshipLight(Serial serial)
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

                            ValidationQueue<TownshipLight>.Enqueue(this, new object[] { owner, placed });
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