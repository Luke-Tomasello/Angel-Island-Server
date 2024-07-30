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

/* scripts\Engines\BetaRewardSystem\BetaRewardSystem.cs
 * 
 * ChangeLog
 * Created 4/10/23 by Adam
 *  Very simple system for rewarding BETA testers. No levels, just a reward for helping out.
 *  We won't announce this until the shard is wiped for Production, we don't want players logging in just to get a reward.
 */

using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.BetaRewardSystem
{
    public class BetaRewardSystem
    {
        public static bool Enabled = false; // false to record beta testers, true to pay out

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }
        private static List<uint> RewardGuids = new List<uint>();
        public static void RecordBetaLogin(PlayerMobile pm, Account acct)
        {
            if (pm.AccessLevel > AccessLevel.Player || acct.AccessLevel > AccessLevel.Player)
                return;

            // rewards only for siege, and during the beta phase
            if (!Core.RuleSets.SiegeStyleRules() || Core.ReleasePhase == ReleasePhase.Production)
                return;

            if (!RewardGuids.Contains(pm.GUID))
                RewardGuids.Add(pm.GUID);
        }
        public static void RewardBetaTester(PlayerMobile pm, Account acct)
        {
            if (pm.AccessLevel > AccessLevel.Player || acct.AccessLevel > AccessLevel.Player)
                return;

            if (Core.RuleSets.SiegeStyleRules() && Core.ReleasePhase == ReleasePhase.Production)
            {
                if (RewardGuids.Contains(pm.GUID))
                {
                    RewardGuids.Remove(pm.GUID);
                    LeatherArmorDyeTub item = new LeatherArmorDyeTub();
                    item.UsesRemaining = 25;
                    item.LootType = LootType.Newbied;
                    pm.AddToBackpack(item);
                    pm.SendMessage("You have received a beta tester reward. Thank you!");
                }
            }
        }

        #region Serialization
        public static void Load()
        {
            if (!Directory.Exists("./Saves"))
                return;

            string[] check = Directory.GetFiles("Saves", "BetaRewards*.bin");
            if (check.Length == 0)
                return;

            Console.WriteLine("Beta Rewards Loading...");
            try
            {
                string[] files = Directory.GetFiles("Saves", "BetaRewards*.bin");
                if (files.Length == 0)
                {
                    Utility.PushColor(ConsoleColor.Red);
                    Console.WriteLine("Error reading BetaRewards*.bin, using default values:");
                    Utility.PopColor();
                    return;
                }
                // we merge the BetaRewards*.bin from both Test Center and Siege
                for (int i = 0; i < files.Length; ++i)
                {
                    BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(files[i], FileMode.Open, FileAccess.Read)));
                    int version = reader.ReadInt();

                    switch (version)
                    {
                        case 1:
                            {
                                int count = reader.ReadInt();
                                for (int ix = 0; ix < count; ix++)
                                {
                                    uint guid = reader.ReadUInt();
                                    if (!RewardGuids.Contains(guid))
                                        RewardGuids.Add(guid);
                                    else
                                        ;
                                }
                                break;
                            }
                        default:
                            {
                                throw new Exception("Invalid BetaRewards*.bin savefile version.");
                            }
                    }

                    reader.Close();
                }
            }
            catch
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Error reading BetaRewards*.bin, using default values:");
                Utility.PopColor();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Beta Rewards Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/BetaRewards.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(RewardGuids.Count);
                            foreach (uint ux in RewardGuids)
                                writer.Write(ux);
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing BetaRewards.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization

    }
}