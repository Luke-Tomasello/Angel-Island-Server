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

/* Scripts\Engines\TerritoryControl\ContestedSpawner.Contributions.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */
#region Contributions & Accounting

using Server.Mobiles;
using System.Collections.Generic;
namespace Server.Engines.TerritoryControl
{
    public partial class ContestedSpawner
    {
        internal class ContributionData
        {
            public Dictionary<Serial, int> DamageCredit { get; } = new Dictionary<Serial, int>();
            public bool IsFinalized { get; set; }

            public void RecordDamage(BaseCreature killedCreature, List<DamageStore> lootingRights)
            {
                if (killedCreature == null || lootingRights == null || lootingRights.Count == 0)
                    return;

                var processed = BaseCreature.ProcessDamageStore(lootingRights);
                BaseCreature.ClipDamageStore(ref processed, killedCreature);

                // Do NOT clear — accumulate instead
                foreach (KeyValuePair<Mobile, int> kvp in processed)
                {
                    Mobile m = kvp.Key;
                    if (m != null && !m.Deleted && m.Serial.IsMobile)
                    {
                        Serial serial = m.Serial;
                        if (DamageCredit.ContainsKey(serial))
                            DamageCredit[serial] += kvp.Value;   // Add to existing total
                        else
                            DamageCredit[serial] = kvp.Value;
                    }
                }

                IsFinalized = false;
            }

            public void Rollback()
            {
                DamageCredit.Clear();
                IsFinalized = false;
            }

            public void FinalizeContributions()
            {
                IsFinalized = true;
            }
        }

        private ContributionData _contributions;

        public void InitializeContributions()
        {
            _contributions = new ContributionData();
        }

        /// <summary>
        /// Called whenever a creature spawned by this spawner dies.
        /// </summary>
        public void RecordKillContribution(BaseCreature killedCreature, List<DamageStore> lootingRights)
        {
            if (_contributions == null)
                InitializeContributions();

            _contributions.RecordDamage(killedCreature, lootingRights);
        }

        public void RollbackContributions()
        {
            _contributions?.Rollback();
        }

        public void FinalizeContributions()
        {
            _contributions?.FinalizeContributions();
        }

        public IReadOnlyDictionary<Serial, int> GetContributionData()
        {
            return _contributions != null
                ? _contributions.DamageCredit
                : new Dictionary<Serial, int>();
        }

        public bool HasContributions
        {
            get { return _contributions != null && _contributions.DamageCredit.Count > 0; }
        }

        internal ContributionData InternalContributions
        {
            get { return _contributions; }
        }
    }
}
#endregion Contributions & Accounting