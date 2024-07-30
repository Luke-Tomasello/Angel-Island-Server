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

/* Scripts/Engines/Factions/Town.cs
 * ChangeLog
 *  1/2/22, Yoar
 *      Added "Squalor" town expense. This prevents the town from
 *      accumulating an infinite amount of silver.
 */

using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Factions
{
    [CustomEnum(new string[] { "None", "Britain", "Magincia", "Minoc", "Moonglow", "Skara Brae", "Trinsic", "Vesper", "Yew" })]
    [Parsable]
    public abstract class Town : IComparable
    {
        private TownDefinition m_Definition;
        private TownState m_State;

        public TownDefinition Definition
        {
            get { return m_Definition; }
            set { m_Definition = value; }
        }

        public TownState State
        {
            get { return m_State; }
            set { m_State = value; ConstructGuardLists(); }
        }

        public int Silver
        {
            get { return m_State.Silver; }
            set { m_State.Silver = value; }
        }

        public Faction Owner
        {
            get { return m_State.Owner; }
            set { m_State.Owner = value; }
        }

        public Mobile Sheriff
        {
            get { return m_State.Sheriff; }
            set { m_State.Sheriff = value; }
        }

        public Mobile Finance
        {
            get { return m_State.Finance; }
            set { m_State.Finance = value; }
        }

        public int Tax
        {
            get { return m_State.Tax; }
            set { m_State.Tax = value; }
        }

        public DateTime LastTaxChange
        {
            get { return m_State.LastTaxChange; }
            set { m_State.LastTaxChange = value; }
        }

        public bool TaxChangeReady
        {
            get { return (m_State.LastTaxChange + FactionConfig.TownTaxChangePeriod) < DateTime.UtcNow; }
        }

        public static Town FromRegion(Region reg)
        {
            if (reg.Map != Faction.Facet)
                return null;

            List<Town> towns = Towns;

            for (int i = 0; i < towns.Count; ++i)
            {
                Town town = towns[i];

                if (reg.IsPartOf(town.Definition.Region))
                    return town;
            }

            return null;
        }

        public int FinanceUpkeep
        {
            get
            {
                List<VendorList> vendorLists = VendorLists;
                int upkeep = 0;

                for (int i = 0; i < vendorLists.Count; ++i)
                    upkeep += vendorLists[i].Vendors.Count * vendorLists[i].Definition.Upkeep;

                return upkeep;
            }
        }

        public int SheriffUpkeep
        {
            get
            {
                List<GuardList> guardLists = GuardLists;
                int upkeep = 0;

                for (int i = 0; i < guardLists.Count; ++i)
                    upkeep += guardLists[i].Guards.Count * guardLists[i].Definition.Upkeep;

                return upkeep;
            }
        }

        public int DailyIncome
        {
            get { return (10000 * (100 + m_State.Tax)) / 100; }
        }

        public int Squalor
        {
            get { return (FactionConfig.TownSqualorEnabled ? Math.Max(0, (int)(Silver * (40000.0 / FactionConfig.TownSoftSilverCap))) : 0); }
        }

        public int NetCashFlow
        {
            get { return DailyIncome - FinanceUpkeep - SheriffUpkeep - Squalor; }
        }

        public TownMonolith Monolith
        {
            get
            {
                List<BaseMonolith> monoliths = BaseMonolith.Monoliths;

                foreach (BaseMonolith monolith in monoliths)
                {
                    if (monolith is TownMonolith)
                    {
                        TownMonolith townMonolith = (TownMonolith)monolith;

                        if (townMonolith.Town == this)
                            return townMonolith;
                    }
                }

                return null;
            }
        }

        public DateTime LastIncome
        {
            get { return m_State.LastIncome; }
            set { m_State.LastIncome = value; }
        }

        public void BeginOrderFiring(Mobile from)
        {
            bool isFinance = IsFinance(from);
            bool isSheriff = IsSheriff(from);
            string type = null;

            // NOTE: Messages not OSI-accurate, intentional
            if (isFinance && isSheriff) // GM only
                type = "vendor or guard";
            else if (isFinance)
                type = "vendor";
            else if (isSheriff)
                type = "guard";

            from.SendMessage("Target the {0} you wish to dismiss.", type);
            from.BeginTarget(12, false, TargetFlags.None, new TargetCallback(EndOrderFiring));
        }

        public void EndOrderFiring(Mobile from, object obj)
        {
            bool isFinance = IsFinance(from);
            bool isSheriff = IsSheriff(from);
            string type = null;

            if (isFinance && isSheriff) // GM only
                type = "vendor or guard";
            else if (isFinance)
                type = "vendor";
            else if (isSheriff)
                type = "guard";

            if (obj is BaseFactionVendor)
            {
                BaseFactionVendor vendor = (BaseFactionVendor)obj;

                if (vendor.Town == this && isFinance)
                    vendor.Delete();
            }
            else if (obj is BaseFactionGuard)
            {
                BaseFactionGuard guard = (BaseFactionGuard)obj;

                if (guard.Town == this && isSheriff)
                    guard.Delete();
            }
            else
            {
                from.SendMessage("That is not a {0}!", type);
            }
        }

        public void CheckIncome()
        {
            if ((LastIncome + FactionConfig.TownIncomePeriod) > DateTime.UtcNow || Owner == null)
                return;

            ProcessIncome();
        }

        public void ProcessIncome()
        {
            LastIncome = DateTime.UtcNow;

            int flow = NetCashFlow;

            if ((Silver + flow) < 0)
            {
                ArrayList toDelete = BuildFinanceList();

                while ((Silver + flow) < 0 && toDelete.Count > 0)
                {
                    int index = Utility.Random(toDelete.Count);
                    Mobile mob = (Mobile)toDelete[index];

                    mob.Delete();

                    toDelete.RemoveAt(index);
                    flow = NetCashFlow;
                }
            }

            Silver += flow;
        }

        public ArrayList BuildFinanceList()
        {
            ArrayList list = new ArrayList();

            List<VendorList> vendorLists = VendorLists;

            for (int i = 0; i < vendorLists.Count; ++i)
                list.AddRange(vendorLists[i].Vendors);

            List<GuardList> guardLists = GuardLists;

            for (int i = 0; i < guardLists.Count; ++i)
                list.AddRange(guardLists[i].Guards);

            return list;
        }

        private List<VendorList> m_VendorLists;
        private List<GuardList> m_GuardLists;

        public List<VendorList> VendorLists
        {
            get { return m_VendorLists; }
            set { m_VendorLists = value; }
        }

        public List<GuardList> GuardLists
        {
            get { return m_GuardLists; }
            set { m_GuardLists = value; }
        }

        public void ConstructGuardLists()
        {
            GuardDefinition[] defs = (Owner == null ? new GuardDefinition[0] : Owner.Definition.Guards);

            m_GuardLists = new List<GuardList>();

            for (int i = 0; i < defs.Length; ++i)
                m_GuardLists.Add(new GuardList(defs[i]));
        }

        public GuardList FindGuardList(Type type)
        {
            List<GuardList> guardLists = GuardLists;

            for (int i = 0; i < guardLists.Count; ++i)
            {
                GuardList guardList = guardLists[i];

                if (guardList.Definition.Type == type)
                    return guardList;
            }

            return null;
        }

        public void ConstructVendorLists()
        {
            VendorDefinition[] defs = VendorDefinition.Definitions;

            m_VendorLists = new List<VendorList>();

            for (int i = 0; i < defs.Length; ++i)
                m_VendorLists.Add(new VendorList(defs[i]));
        }

        public VendorList FindVendorList(Type type)
        {
            List<VendorList> vendorLists = VendorLists;

            for (int i = 0; i < vendorLists.Count; ++i)
            {
                VendorList vendorList = vendorLists[i];

                if (vendorList.Definition.Type == type)
                    return vendorList;
            }

            return null;
        }

        public bool RegisterGuard(BaseFactionGuard guard)
        {
            if (guard == null)
                return false;

            GuardList guardList = FindGuardList(guard.GetType());

            if (guardList == null)
                return false;

            guardList.Guards.Add(guard);
            return true;
        }

        public bool UnregisterGuard(BaseFactionGuard guard)
        {
            if (guard == null)
                return false;

            GuardList guardList = FindGuardList(guard.GetType());

            if (guardList == null)
                return false;

            if (!guardList.Guards.Contains(guard))
                return false;

            guardList.Guards.Remove(guard);
            return true;
        }

        public bool RegisterVendor(BaseFactionVendor vendor)
        {
            if (vendor == null)
                return false;

            VendorList vendorList = FindVendorList(vendor.GetType());

            if (vendorList == null)
                return false;

            vendorList.Vendors.Add(vendor);
            return true;
        }

        public bool UnregisterVendor(BaseFactionVendor vendor)
        {
            if (vendor == null)
                return false;

            VendorList vendorList = FindVendorList(vendor.GetType());

            if (vendorList == null)
                return false;

            if (!vendorList.Vendors.Contains(vendor))
                return false;

            vendorList.Vendors.Remove(vendor);
            return true;
        }

        public static void Initialize()
        {
            if (Core.Factions)
            {
                List<Town> towns = Towns;

                for (int i = 0; i < towns.Count; ++i)
                {
                    towns[i].Sheriff = towns[i].Sheriff;
                    towns[i].Finance = towns[i].Finance;
                }

                CommandSystem.Register("GrantTownSilver", AccessLevel.Administrator, new CommandEventHandler(GrantTownSilver_OnCommand));
            }
        }

        public Town()
        {
            m_State = new TownState(this);
            ConstructVendorLists();
            ConstructGuardLists();
        }

        public bool IsMember(Mobile mob)
        {
            if (mob == null || mob.Deleted)
                return false;

            return (mob.AccessLevel >= AccessLevel.GameMaster || (Owner != null && Owner.IsMember(mob)));
        }

        public bool IsSheriff(Mobile mob)
        {
            if (mob == null || mob.Deleted)
                return false;

            return (mob.AccessLevel >= AccessLevel.GameMaster || mob == Sheriff);
        }

        public bool IsFinance(Mobile mob)
        {
            if (mob == null || mob.Deleted)
                return false;

            return (mob.AccessLevel >= AccessLevel.GameMaster || mob == Finance);
        }

        public bool IsCommander(Mobile mob)
        {
            if (mob == null || mob.Deleted)
                return false;

            return (mob.AccessLevel >= AccessLevel.GameMaster || (m_State.Owner != null && mob == m_State.Owner.Commander));
        }

        public static List<Town> Towns { get { return Reflector.Towns; } }

        public int CompareTo(object obj)
        {
            return m_Definition.Sort - ((Town)obj).m_Definition.Sort;
        }

        public override string ToString()
        {
            return m_Definition.FriendlyName;
        }

        public static void WriteReference(GenericWriter writer, Town town)
        {
            int idx = Towns.IndexOf(town);

            writer.WriteEncodedInt((int)(idx + 1));
        }

        public static Town ReadReference(GenericReader reader)
        {
            int idx = reader.ReadEncodedInt() - 1;

            if (idx >= 0 && idx < Towns.Count)
                return Towns[idx];

            return null;
        }

        public static Town Parse(string name)
        {
            List<Town> towns = Towns;

            for (int i = 0; i < towns.Count; ++i)
            {
                Town town = towns[i];

                if (Insensitive.Equals(town.Definition.FriendlyName, name))
                    return town;
            }

            return null;
        }

        public static void GrantTownSilver_OnCommand(CommandEventArgs e)
        {
            Town town = FromRegion(e.Mobile.Region);

            if (town == null)
                e.Mobile.SendMessage("You are not in a faction town.");
            else if (e.Length == 0)
                e.Mobile.SendMessage("Format: GrantTownSilver <amount>");
            else
            {
                town.Silver += e.GetInt32(0);
                e.Mobile.SendMessage("You have granted {0:N0} silver to the town. It now has {1:N0} silver.", e.GetInt32(0), town.Silver);
            }
        }
    }
}