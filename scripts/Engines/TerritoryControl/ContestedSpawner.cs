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

/* Scripts\Engines\TerritoryControl\ContestedSpawner.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */

using Server.Engines.DataRecorder;
using Server.Engines.TerritoryControl.Events;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using TextCopy;
using static Server.Accounting.Account;

namespace Server.Engines.TerritoryControl
{
    public partial class ContestedSpawner : Spawner
    {
        private Timer? m_LevelTimer;
        private bool _deactivated;

        public GuardianWeb? Web { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnerKillsNeeded { get; set; } = TerritoryConfig.DefaultSpawnerKillsNeeded;

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnerKills { get; set; }

        public bool IsComplete => SpawnerKills >= SpawnerKillsNeeded;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Deactivated
        {
            get => _deactivated;
            set
            {
                if (_deactivated == value)
                    return;

                _deactivated = value;

                if (value)                    // Whenever we deactivate, stop the timer
                {
                    StopLevelTimer();
                    Running = false;
                }
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan LevelRestartDelay { get; set; } = TimeSpan.FromMinutes(5);

        [CommandProperty(AccessLevel.GameMaster)]
        public string? Clan { get; set; } = "Invader";

        /// <summary>
        /// Constructor used by OrcSpawnGenerator
        /// </summary>
        /// <summary>
        /// Constructor used by OrcSpawnGenerator to create themed Orc guardian spawners
        /// </summary>
        public ContestedSpawner(OrcSpawnGenerator.OrcSpawnInfo info) : base()
        {
            if (info == null || info.CreatureCounts.Count == 0)
                return;

            // reset the creature content (should be empty already)
            this.Entries.Clear();

            // Apply configuration / template settings first
            this.Concentric = info.Concentric;
            this.HomeRange = info.HomeRange;
            this.Distro = info.Distro;
            this.LevelRestartDelay = info.LevelRestartDelay;
            this.MinDelay = info.MinDelay;
            this.MaxDelay = info.MaxDelay;
            this.Clan = info.Clan;

            #region Team

            // make sure this team setting matches our mini boss or else our orcs will fight the mini boss
            // setting this to anythig other than 0 will cause our spawn to kill all nearby base creatures
            this.Team = (Core.RuleSets.SeasonsRules(3) || Core.RuleSets.UpheavalRules()) ? 0 : info.Team;

            #endregion Team


            // expensive, but prevents creatures from spawning behind walls
            this.SpawnerFlags = this.SpawnerFlags | Utility.SpawnFlags.ClearPath;

            // Only set Count for legacy (non-Multi) spawner modes
            if (!this.GetFlag(SpawnerAttribs.ModeNeruns) && !this.GetFlag(SpawnerAttribs.ModeMulti))
                this.Count = info.Count;

            // Finally, populate the creatures (ModeMulti style)
            foreach (var kvp in info.CreatureCounts)
            {
                this.Entries.Add(new SpawnerEntry(kvp.Key, kvp.Value));
            }
        }

        protected override void OnAfterMobileSpawn(Mobile m)
        {
            base.OnAfterMobileSpawn(m);
            // Give the orc a proper name
            m.Name = NameList.RandomName("orc");

            // Prevent guardians from looting gold from high-value corpses (anti-farming exploit)
            if (m is BaseCreature bc)
                bc.CorpseLootExclusions.Add(typeof(Gold));

            // players need bandages an' stuff!
            PackInvasionOrcLoot(m);
        }
        public override bool HandlesOnSingleClick
        {
            get { return true; }
        }

        /// <summary>
        /// Custom single-click handling for orcs spawned by this contested spawner.
        /// Formats as:
        ///     Grok the Brutal
        ///     [Grak'thar]
        /// </summary>
        public override void OnSingleClickSpawned(Mobile from, BaseCreature orc)
        {
            if (orc == null || from == null)
                return;

            string name = !string.IsNullOrEmpty(orc.Name) ? orc.Name : "an orc";
            string clanLine = !string.IsNullOrEmpty(Clan) ? $"[{Clan}]" : "";

            // First line: Name (with proper hue)
            int hue = Notoriety.GetHue(Notoriety.Compute(from, orc));
            orc.PrivateOverheadMessage(MessageType.Label, hue, true, name, from.NetState);

            // Second line: Clan tag (same hue)
            if (!string.IsNullOrEmpty(clanLine))
                orc.PrivateOverheadMessage(MessageType.Label, hue, true, clanLine, from.NetState);

            return;
        }
        public ContestedSpawner() : base()
        {
        }

        public ContestedSpawner(Serial serial) : base(serial)
        {
        }

        public override void SpawnedMobileKilled(Mobile killed)
        {
            if (killed is not BaseCreature bc)
                return;

            var lootingRights = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);
            if (lootingRights == null || lootingRights.Count == 0)
                return;

            var participants = BaseCreature.GetQualifiedParticipants(bc.DamageEntries, bc.HitsMax);

            // killing orcs from this spawner makes you an enemy of orcs (can't wear kin mask)
            foreach (PlayerMobile pm in participants)
            {
                if (pm.Account is Accounting.Account acct)
                {
                    bool alreadyEnemy = acct.IsOrcEnemy;

                    TimeSpan duration = Core.Debug
                        ? TimeSpan.FromMinutes(5)
                        : TimeSpan.FromHours(72);

                    acct.SetSeason2OrcEnemy(duration);

                    if (!alreadyEnemy)
                    {
                        pm.SendMessage(
                            0x35,
                            "You have been deemed an enemy of the occupation forces for {0}.",
                            FormatOrcEnemyDuration(duration));
                    }
                }
            }

            SpawnerKills++;

            if (SpawnerKills == 1 && m_LevelTimer == null)
                StartLevelTimer();

            if (Deactivated)
            {
                TerritoryMessenger.ReportKill(this, killed, participants, true);
                return;
            }

            RecordKillContribution(bc, lootingRights);

            if (IsComplete)
                OnSpawnerCleared();            // deactivate, FinalizeContributions

            Web?.NotifyKill(this);      // Web manages web completion

            TerritoryMessenger.ReportKill(this, killed, participants);
        }
        public void OnSpawnerCleared()
        {
            Deactivated = true;
            FinalizeContributions();
            AwardSpawnerBonus();
        }
        public void AwardSpawnerBonus()
        {
            if (!HasContributions)
                return;

            double totalDamage = GetContributionData().Values.Sum();
            double bonus = (totalDamage * 0.20);   // 20%

            // Record bonus into DataRecorder and provide township tax subsidies and fame
            foreach (var kvp in GetContributionData())
            {
                Mobile? player = World.FindMobile(kvp.Key);
                if (player != null)
                {
                    DataRecorder.DataRecorder.Add(player, RecorderDomain.LaunchEventPoints, bonus);
                    TerritorySystem.UpdateTownshipForPlayer(player, bonus);
                }
            }
        }
        private void StartLevelTimer()
        {
            StopLevelTimer();
            m_LevelTimer = Timer.DelayCall(LevelRestartDelay, OnLevelTimerExpired);
        }

        private void StopLevelTimer()
        {
            if (m_LevelTimer != null)
            {
                m_LevelTimer.Stop();
                m_LevelTimer = null;
            }
        }

        private void OnLevelTimerExpired()
        {
            if (!Deactivated && SpawnerKills > 0 && SpawnerKills < SpawnerKillsNeeded)
            {
                // Rollback our local contribution tracking (clear player damage credits)
                RollbackContributions();

                SpawnerKills = 0;

                // Subtle immersive feedback using clan name
                if (!string.IsNullOrEmpty(Clan) && Web?.Zone != null)
                {
                    string message = $"The [{Clan}] clan has regained some ground.";
                    Effects.PlaySound(Location, Map, 0x5D); // fizzle

                    // Send to nearby players only
                    foreach (Mobile m in GetPlayersInRange(18))
                    {
                        if (m is PlayerMobile)
                            m.SendMessage(0x22, message); // dark red / ominous
                    }
                }
            }

            m_LevelTimer = null;
        }

        public override void OnSingleClick(Mobile from)
        {
            if (from.AccessLevel < AccessLevel.GameMaster)
            {
                base.OnSingleClick(from);
                return;
            }

            string statusText = Deactivated ? "Deactivated" : (!Running || Entries.Count == 0) ? "ERROR" : "Active";
            int hue = (statusText == "ERROR") ? 0x22 : 0x3F; // red for error, white otherwise

            PrivateOverheadMessage(MessageType.Label, 0x55, true, "Orc Invasion", from.NetState);
            PrivateOverheadMessage(MessageType.Label, hue, true, $"Status: {statusText}", from.NetState);
            PrivateOverheadMessage(MessageType.Label, 0x3F, true, $"Spawner Kills: {SpawnerKills} / {SpawnerKillsNeeded}", from.NetState);

            if (Web != null)
            {
                PrivateOverheadMessage(MessageType.Label, 0x3F, true,
                    $"Web Kills: {Web.TotalKills} / {Web.WebKillsNeeded}", from.NetState);
            }

            // Optional debug clipboard
            if (Core.Debug && false)
            {
                var lines = new List<string>
                {
                    "Orc Invasion",
                    $"Status: {statusText}",
                    $"Spawner Kills: {SpawnerKills} / {SpawnerKillsNeeded}",
                    Web != null ? $"Web Kills: {Web.TotalKills} / {Web.WebKillsNeeded}" : "No Web"
                };

                string clipboardText = string.Join(Environment.NewLine, lines);
                Clipboard clipboard = new Clipboard();
                clipboard.SetText(clipboardText);
            }
        }

        public override void Delete()
        {
            StopLevelTimer();
            base.Delete();
        }

        #region Serialization

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            writer.Write(SpawnerKillsNeeded);
            writer.Write(SpawnerKills);
            writer.Write(Deactivated);
            writer.Write(Clan);

            // Serialize Contribution Data
            writer.Write(_contributions != null);

            if (_contributions != null)
            {
                writer.Write(_contributions.DamageCredit.Count);

                foreach (KeyValuePair<Serial, int> kvp in _contributions.DamageCredit)
                {
                    writer.Write(kvp.Key);      // Serial
                    writer.Write(kvp.Value);    // Damage
                }

                writer.Write(_contributions.IsFinalized);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        SpawnerKillsNeeded = reader.ReadInt();
                        SpawnerKills = reader.ReadInt();
                        Deactivated = reader.ReadBool();
                        Clan = reader.ReadString();

                        // Deserialize Contribution Data
                        bool hasContributions = reader.ReadBool();

                        if (hasContributions)
                        {
                            _contributions = new ContributionData();

                            int count = reader.ReadInt();

                            for (int i = 0; i < count; i++)
                            {
                                Serial serial = reader.ReadInt();
                                int damage = reader.ReadInt();

                                if (serial.IsMobile)
                                    _contributions.DamageCredit[serial] = damage;
                            }

                            _contributions.IsFinalized = reader.ReadBool();
                        }
                        else
                        {
                            _contributions = null;
                        }

                        // Restart timer if there is partial progress
                        if (!Deactivated && SpawnerKills > 0 && SpawnerKills < SpawnerKillsNeeded)
                            StartLevelTimer();

                        break;
                    }
            }
        }

        #endregion Serialization

        #region tools
        private static void PackInvasionOrcLoot(Mobile m)
        {
            if (m is not BaseCreature bc)
                return;

            PackInvasionOrcFood(bc);

            if (bc.GetType() == typeof(OrcishMage))
                PackInvasionOrcMagePotion(bc);
            else
                PackInvasionOrcBandages(bc);
        }

        private static void PackInvasionOrcFood(BaseCreature bc)
        {
            switch (Utility.Random(4))
            {
                case 0:
                    bc.PackItem(new CheeseSlice());
                    break;
                case 1:
                    bc.PackItem(new Carrot());
                    break;
                case 2:
                    bc.PackItem(new Onion());
                    break;
                case 3:
                    bc.PackItem(new ChickenLeg());
                    break;
            }
        }

        private static void PackInvasionOrcMagePotion(BaseCreature bc)
        {
            switch (Utility.Random(3))
            {
                case 0:
                    bc.PackItem(new LesserHealPotion());
                    break;
                case 1:
                    bc.PackItem(new HealPotion());
                    break;
                case 2:
                    bc.PackItem(new GreaterHealPotion());
                    break;
            }
        }

        private static void PackInvasionOrcBandages(BaseCreature bc)
        {
            int count = Utility.RandomDouble() < 0.80 ? 1 : 2;

            bc.PackItem(new Bandage(count));
        }
        public List<Mobile> GetPlayersInRange(int range)
        {
            List<Mobile> mobiles = new();

            if (Map == null || Map == Map.Internal)
                return mobiles;

            IPooledEnumerable eable = Map.GetMobilesInRange(Location, range);

            try
            {
                foreach (Mobile m in eable)
                {
                    if (m != null && !m.Deleted)
                        mobiles.Add(m);
                }
            }
            finally
            {
                eable.Free();
            }

            return mobiles;
        }
        #endregion
    }
}