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

/* Scripts\Engines\Spawner\Spawner.TriggerSystem.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using Server.Diagnostics;
using Server.Items.Triggers;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        /// <summary>
        /// Notifies the spawner of this mobile, of its death.
        /// This is used by our special 'champ level' PushBackSpawners
        /// </summary>
        /// <param name="killed"></param>
        public virtual void SpawnedMobileKilled(Mobile killed)
        {
            #region Trigger System

            // first, process players responsible for the kill
            if (m_TriggerLinkPlayers != null)
            {
                if (TriggerSystem.CheckEvent(this))
                {
                    if (killed is BaseCreature bc)
                    {   // find legit damagers
                        List<DamageStore> list = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);

                        // divvy up points between party members
                        SortedList<Mobile, int> Results = BaseCreature.ProcessDamageStore(list);

                        // anti-cheezing: Players have found that healing a monster (or allowing it to heal itself,) can yield unlimited damage points.
                        //  We therefore limit the damage points to no more than the creature's HitsMax
                        BaseCreature.ClipDamageStore(ref Results, bc);

                        foreach (Mobile m in Results.Keys)
                            if (m is PlayerMobile pm)
                            {
                                if (TriggerSystem.CanTrigger(pm, m_TriggerLinkPlayers))
                                {
                                    if (pm.KillQueue == null)
                                        pm.KillQueue = new();

                                    pm.KillQueue.Enqueue(bc);

                                    TriggerSystem.CheckTrigger(pm, m_TriggerLinkPlayers);
                                }
                            }
                    }
                }
            }

            // now for the creature
            if (m_TriggerLinkCreature != null)
                if (TriggerSystem.CheckEvent(this))
                    if (killed is BaseCreature bc)
                    {
                        // find legit damagers
                        List<DamageStore> list = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);

                        // divvy up points between party members
                        SortedList<Mobile, int> Results = BaseCreature.ProcessDamageStore(list);

                        // anti-cheezing: Players have found that healing a monster (or allowing it to heal itself,) can yield unlimited damage points.
                        //  We therefore limit the damage points to no more than the creature's HitsMax
                        BaseCreature.ClipDamageStore(ref Results, bc);

                        foreach (Mobile m in Results.Keys)
                            if (m is PlayerMobile pm)
                            {
                                if (pm.KillQueue == null)
                                    pm.KillQueue = new();

                                if (!pm.KillQueue.Contains(bc))
                                    pm.KillQueue.Enqueue(bc);
                            }

                        TriggerSystem.CheckTrigger(bc, m_TriggerLinkCreature);
                    }

            #endregion Trigger System

            #region Goodies
            if (m_GoodiesRadius >= 0 && m_GoodiesTotalMin >= 0 && m_GoodiesTotalMax > 0 && m_GoodiesTotalMax >= m_GoodiesTotalMin)
            {
                int dropped = GoodiesTimer.DropGoodies(killed.Location, killed.Map, m_GoodiesRadius, m_GoodiesTotalMin, m_GoodiesTotalMax);

                LogHelper logger = new LogHelper("SpawnerGoodies.log", false, true);
                logger.Log(LogType.Item, this, string.Format("Mobile {0} dropped {1:N0} gold in goodies", killed, dropped));
            }
            #endregion

            DistributeArtifacts(killed);
        }
    }
}
