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

/* Scripts\Engines\TerritoryControl\TerritoryJournal.cs
 * CHANGELOG:
 *   11/4/2026, Adam:
 *      - Created. The campaign journal: Logs/TerritoryCampaigns.log, one line per campaign event,
 *        "<utc> <campaign id>: <event>". The campaign id is TerritoryDistrict.CampaignID ("Jhelom@2026-11-04T17:02:13Z"):
 *        grep it for a whole campaign. Events: campaign begins (district created, recaptured) or first seen (a campaign
 *        already running when the journal began), zone conquered, Warlord placed or not placed (earned or conquest
 *        bonus, with the reason), district liberated, Warlord killed, campaign credit cleared at a recapture.
 *        Written for the 11/4/2026 Jhelom/Britain investigation: Warlord spawns went to the console only.
 */

using Server.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Engines.TerritoryControl
{
    public static class TerritoryJournal
    {
        public const string FileName = "TerritoryCampaigns.log";

        /// <summary>
        /// Appends the lines to the journal under the campaign id. Returns false if they could not be written (reported to
        /// the exception log); the journal never stops the game.
        /// </summary>
        public static bool Write(string campaign, IEnumerable<string> lines)
        {
            try
            {
                string stamp = DateTime.UtcNow.ToString("u");
                string path = Path.Combine(Core.LogsDirectory, FileName);
                Utility.EnsurePath(path);
                File.AppendAllLines(path, lines.Select(l => string.Format("{0} {1}: {2}", stamp, campaign, l)).ToList());
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return false;
            }
        }
        public static bool Write(string campaign, string line)
        {
            return Write(campaign, new[] { line });
        }
    }
}
