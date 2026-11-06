/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\TerritoryControl\TerritoryMessenger.cs
 * CHANGELOG:
 *   9/22/2026, Adam:
 *      - Added district challenged and district lost messaging.
 *      - Refactored district recipient selection so territorial messages
 *        share the same district + party notification scope.
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */

using Server.Engines.PartySystem;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Engines.TerritoryControl
{
    public static class TerritoryMessenger
    {
        public static void ReportKill(
            ContestedSpawner spawner,
            Mobile killed,
            IEnumerable<PlayerMobile> participants,
            bool extraKill = false)
        {
            if (spawner?.Web == null)
                return;

            var recipients = new HashSet<PlayerMobile>();

            foreach (PlayerMobile pm in TerritoryZone.GetPlayersInZone(spawner))
                AddRecipient(recipients, pm);

            if (participants != null)
            {
                foreach (PlayerMobile pm in participants)
                    AddRecipient(recipients, pm);
            }

            var output = new List<string>();

            if (Core.Debug)
            {
                TerritoryDistrict? district = spawner?.Web?.District;

                if (district != null)
                {
                    int stableKills = district.StableMiniBossKills;
                    int needed = TerritoryConfig.MiniBossKillInterval;

                    output.Add(
                        $"MiniBoss Credit: {district.TotalKills}/{stableKills}/{needed} kills");
                }
            }

            if (extraKill)
            {
                output.Add("Extra kill.");
            }
            else if (spawner.Web.IsComplete)
            {
                output.Add("Zone cleared!");
            }
            else if (spawner.Deactivated)
            {
                output.Add("Area cleared.");
            }
            else
            {
                output.Add($"{spawner.SpawnerKills}/{spawner.SpawnerKillsNeeded} kills needed for this area.");
                output.Add($"{spawner.Web.TotalKills}/{spawner.Web.WebKillsNeeded} kills needed for zone.");
            }

            foreach (PlayerMobile pm in recipients)
            {
                foreach (string line in output)
                    pm.SendMessage(line);
            }
        }

        public static void ReportDistrictConquered(string districtName, Map map)
        {
            foreach (PlayerMobile pm in GetDistrictRecipients(districtName, map))
            {
                pm.SendMessage(0x21, $"{districtName} has been reclaimed!");
                Effects.PlaySound(pm.Location, pm.Map, 0x2F3);
            }
        }

        public static void ReportDistrictChallenged(string districtName, Map map)
        {
            foreach (PlayerMobile pm in GetDistrictRecipients(districtName, map))
            {
                pm.SendMessage(0x21, $"{districtName} is being challenged!");
                Effects.PlaySound(pm.Location, pm.Map, 0x2F3);
            }
        }

        public static void ReportDistrictLost(string districtName, Map map)
        {
            foreach (PlayerMobile pm in GetDistrictRecipients(districtName, map))
            {
                pm.SendMessage(0x21, $"{districtName} has fallen to the Orcs!");
                Effects.PlaySound(pm.Location, pm.Map, 0x2F3);
            }
        }

        private static HashSet<PlayerMobile> GetDistrictRecipients(string districtName, Map map)
        {
            var recipients = new HashSet<PlayerMobile>();

            foreach (PlayerMobile pm in TerritoryDistrict.GetPlayersInDistrict(districtName, map))
            {
                AddRecipient(recipients, pm);

                Party? party = Party.Get(pm);

                if (party == null)
                    continue;

                foreach (PartyMemberInfo info in party.Members)
                {
                    if (info?.Mobile is PlayerMobile partyMember)
                        AddRecipient(recipients, partyMember);
                }
            }

            return recipients;
        }

        private static void AddRecipient(HashSet<PlayerMobile> recipients, PlayerMobile pm)
        {
            if (pm == null || pm.Deleted || pm.NetState == null)
                return;

            recipients.Add(pm);
        }
    }
}