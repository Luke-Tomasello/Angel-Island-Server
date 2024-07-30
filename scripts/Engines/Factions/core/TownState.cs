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
using System.Collections.Generic;

namespace Server.Factions
{
    [PropertyObject]
    public class TownState
    {
        private Town m_Town;
        private Faction m_Owner;

        private Mobile m_Sheriff;
        private Mobile m_Finance;

        private int m_Silver;
        private int m_Tax;

        private DateTime m_LastTaxChange;
        private DateTime m_LastIncome;

        [CommandProperty(AccessLevel.GameMaster)]
        public Town Town
        {
            get { return m_Town; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Faction Owner
        {
            get { return m_Owner; }
            set
            {
                if (m_Owner == value)
                    return;

                if (m_Owner == null) // going from unowned to owned
                {
                    LastIncome = DateTime.UtcNow;
                    value.Silver += FactionConfig.TownCaptureSilver;
                }
                else if (value == null) // going from owned to unowned
                {
                    LastIncome = DateTime.MinValue;
                }
                else // otherwise changing hands, income timer doesn't change
                {
                    value.Silver += FactionConfig.TownCaptureSilver;
                }

                m_Owner = value;

                Sheriff = null;
                Finance = null;

                TownMonolith monolith = m_Town.Monolith;

                if (monolith != null)
                    monolith.Faction = value;

                List<VendorList> vendorLists = m_Town.VendorLists;

                for (int i = 0; i < vendorLists.Count; ++i)
                {
                    VendorList vendorList = vendorLists[i];
                    List<BaseFactionVendor> vendors = vendorList.Vendors;

                    for (int j = vendors.Count - 1; j >= 0; --j)
                        vendors[j].Delete();
                }

                List<GuardList> guardLists = m_Town.GuardLists;

                for (int i = 0; i < guardLists.Count; ++i)
                {
                    GuardList guardList = guardLists[i];
                    List<BaseFactionGuard> guards = guardList.Guards;

                    for (int j = guards.Count - 1; j >= 0; --j)
                        guards[j].Delete();
                }

                m_Town.ConstructGuardLists();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Sheriff
        {
            get { return m_Sheriff; }
            set
            {
                if (m_Sheriff != null)
                {
                    PlayerState pl = PlayerState.Find(m_Sheriff);

                    if (pl != null)
                        pl.Sheriff = null;
                }

                m_Sheriff = value;

                if (m_Sheriff != null)
                {
                    PlayerState pl = PlayerState.Find(m_Sheriff);

                    if (pl != null)
                        pl.Sheriff = m_Town;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Finance
        {
            get { return m_Finance; }
            set
            {
                if (m_Finance != null)
                {
                    PlayerState pl = PlayerState.Find(m_Finance);

                    if (pl != null)
                        pl.Finance = null;
                }

                m_Finance = value;

                if (m_Finance != null)
                {
                    PlayerState pl = PlayerState.Find(m_Finance);

                    if (pl != null)
                        pl.Finance = m_Town;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Silver
        {
            get { return m_Silver; }
            set { m_Silver = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Tax
        {
            get { return m_Tax; }
            set { m_Tax = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastTaxChange
        {
            get { return m_LastTaxChange; }
            set { m_LastTaxChange = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastIncome
        {
            get { return m_LastIncome; }
            set { m_LastIncome = value; }
        }

        public TownState(Town town)
        {
            m_Town = town;
        }

        public TownState(GenericReader reader)
        {
            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 3:
                    {
                        m_LastIncome = reader.ReadDateTime();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Tax = reader.ReadEncodedInt();
                        m_LastTaxChange = reader.ReadDateTime();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Silver = reader.ReadEncodedInt();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Town = Town.ReadReference(reader);
                        m_Owner = Faction.ReadReference(reader);

                        m_Sheriff = reader.ReadMobile();
                        m_Finance = reader.ReadMobile();

                        m_Town.State = this;

                        break;
                    }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt((int)3); // version

            writer.Write((DateTime)m_LastIncome);

            writer.WriteEncodedInt((int)m_Tax);
            writer.Write((DateTime)m_LastTaxChange);

            writer.WriteEncodedInt((int)m_Silver);

            Town.WriteReference(writer, m_Town);
            Faction.WriteReference(writer, m_Owner);

            writer.Write((Mobile)m_Sheriff);
            writer.Write((Mobile)m_Finance);
        }

        public override string ToString()
        {
            return "...";
        }
    }
}