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

/* Changelog
 *  11/21/2023, Adam (DegenerateFactions)
 *      1. Clear the list of towns.
 *          Even though the system will rebuild this list on next server up, it will be guaranteed to be regenerated with 
 *          all default values, like Tax for instance.
 *      2. Remove FactionWarHorse. These guys are essentially the power of a Drake at one control slot.
 *          Too much to give as a gift, especially considering the circumstances under which Factions are being removed.
 *  11/18/23, Yoar (DegenerateFactions)
 *      Joined the item loops into one
 *      Added various elements to wipe
 *  4/14/23, Adam (DegenerateFactions)
 *      Add a command to wipe all notion of factions
 */

using System;
using System.Collections.Generic;

namespace Server.Factions
{
    public class Generator
    {
        public static void Initialize()
        {
            CommandSystem.Register("GenerateFactions", AccessLevel.Administrator, new CommandEventHandler(GenerateFactions_OnCommand));
            CommandSystem.Register("DegenerateFactions", AccessLevel.Administrator, new CommandEventHandler(DegenerateFactions_OnCommand));
        }

        public static void GenerateFactions_OnCommand(CommandEventArgs e)
        {
            new FactionPersistence();

            List<Faction> factions = Faction.Factions;

            foreach (Faction faction in factions)
                Generate(faction);

            List<Town> towns = Town.Towns;

            foreach (Town town in towns)
                Generate(town);
        }

        public static void Generate(Town town)
        {
            Map facet = Faction.Facet;

            TownDefinition def = town.Definition;

            if (!CheckExistance(def.Monolith, facet, typeof(TownMonolith)))
            {
                TownMonolith mono = new TownMonolith(town);
                mono.MoveToWorld(def.Monolith, facet);
                mono.Sigil = new Sigil(town);
            }

            if (!CheckExistance(def.TownStone, facet, typeof(TownStone)))
                new TownStone(town).MoveToWorld(def.TownStone, facet);
        }

        public static void Generate(Faction faction)
        {
            Map facet = Faction.Facet;

            List<Town> towns = Town.Towns;

            StrongholdDefinition stronghold = faction.Definition.Stronghold;

            if (!CheckExistance(stronghold.JoinStone, facet, typeof(JoinStone)))
                new JoinStone(faction).MoveToWorld(stronghold.JoinStone, facet);

            if (!CheckExistance(stronghold.FactionStone, facet, typeof(FactionStone)))
                new FactionStone(faction).MoveToWorld(stronghold.FactionStone, facet);

            for (int i = 0; i < stronghold.Monoliths.Length; ++i)
            {
                Point3D monolith = stronghold.Monoliths[i];

                if (!CheckExistance(monolith, facet, typeof(StrongholdMonolith)))
                    new StrongholdMonolith(towns[i], faction).MoveToWorld(monolith, facet);
            }
        }

        private static bool CheckExistance(Point3D loc, Map facet, Type type)
        {
            foreach (Item item in facet.GetItemsInRange(loc, 0))
            {
                if (type.IsAssignableFrom(item.GetType()))
                    return true;
            }

            return false;
        }

        public static void DegenerateFactions_OnCommand(CommandEventArgs e)
        {
            DegenerateResult result = DegenerateFactions();

            e.Mobile.SendMessage("{0} faction elements wiped.", result.TotalCount());
        }

        public static DegenerateResult DegenerateFactions()
        {
            DegenerateResult result = new DegenerateResult();

            foreach (Faction f in Faction.Factions)
            {
                List<PlayerState> members = f.State.Members;

                for (int i = members.Count - 1; i >= 0; i--)
                {
                    if (i < members.Count)
                    {
                        Mobile m = members[i].Mobile;
                        f.RemoveMember(m);
#if false
                        Ethics.Ethic.Leave(m);
#endif
                        result.KickCount++;
                    }
                }

                f.StrongholdRegion.Unregister();
                result.UnregisterCount++;
            }

            foreach (Item item in World.Items.Values)
            {
                if (item is IFactionItem)
                {
                    IFactionItem fi = (IFactionItem)item;

                    if (fi.FactionItemState != null)
                    {
                        fi.FactionItemState.Detach();
                        result.DetachCount++;
                    }
                }

#if false
                if ((item.SavedFlags & 0x100) != 0)
                {
                    if (item.Hue == Ethics.Ethic.Hero.Definition.PrimaryHue)
                        item.Hue = 0;

                    item.SavedFlags &= ~0x100;
                    result.DetachCount++;
                }

                if ((item.SavedFlags & 0x200) != 0)
                {
                    if (item.Hue == Ethics.Ethic.Evil.Definition.PrimaryHue)
                        item.Hue = 0;

                    item.SavedFlags &= ~0x200;
                    result.DetachCount++;
                }
#endif

                if (item is BaseMonolith || item is BaseFactionTrap || item is BaseFactionTrapDeed || item is FactionStone || item is JoinStone || item is TownStone)
                {
                    item.Delete();
                    result.DeleteCount++;
                }
                else if (item is Sigil)
                {
                    ((Sigil)item).ForceDelete();
                    result.DeleteCount++;
                }
            }

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is BaseFactionGuard || m is BaseFactionVendor || m is FactionWarHorse)
                {
                    m.Delete();
                    result.DeleteCount++;
                }
            }

            Town.Towns.Clear();

            return result;
        }

        public struct DegenerateResult
        {
            public int KickCount;
            public int DetachCount;
            public int DeleteCount;
            public int UnregisterCount;

            public int TotalCount()
            {
                return (KickCount + DetachCount + DeleteCount + UnregisterCount);
            }
        }
    }
}