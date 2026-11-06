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

/* Scripts\Engines\ChampionSpawn\Champs\Special\Khartag.cs
 * CHANGELOG
 *	11/4/2026, Adam
 *	    TerritoryCampaign (saved, version 2): the TerritoryControl campaign that placed this Warlord
 *	        (TerritoryDistrict.CampaignID). Its death is written to the campaign journal (Logs/TerritoryCampaigns.log):
 *	        time alive, where, damagers and the top damager. Empty for Warlords not placed by a campaign ([Nuke).
 *	3/30/2024, Adam
 *	    created - based on Barracoon
 */

using Server.Diagnostics;
using Server.Engines.Alignment;
using Server.Engines.ChampionSpawn;
using Server.Engines.DataRecorder;
using Server.Items;
using Server.Spells.Fifth;
using Server.Spells.Seventh;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("a dead orcish warlord")]
    public class Khartag : BaseChampion
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Orc }); } }
        private bool m_allowReinforcements = true;
        public override ChampionSkullType SkullType { get { return ChampionSkullType.None; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public override DefensiveSlayer SlayersDefensive { get; set; } = DefensiveSlayer.Repond | DefensiveSlayer.OrcSlaying;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AllowReinforcements
        {
            get => m_allowReinforcements && SpawnerHandle is ChampEngine;
            set => m_allowReinforcements = value;
        }
        // the TerritoryControl campaign that placed this Warlord (TerritoryDistrict.CampaignID); empty otherwise
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public string TerritoryCampaign { get; set; } = string.Empty;

        public override void OnDeath(Container c)
        {
            if (!string.IsNullOrEmpty(TerritoryCampaign))
            {   // before the base: the damage entries are intact
                List<DamageStore> rights = GetLootingRights(DamageEntries, HitsMax);
                DamageStore? top = null;
                foreach (DamageStore ds in rights)
                    if (top == null || ds.m_Damage > top.m_Damage)
                        top = ds;
                TimeSpan alive = DateTime.UtcNow - Created;
                Engines.TerritoryControl.TerritoryJournal.Write(TerritoryCampaign, string.Format(
                    "Warlord 0x{0:X8} {1} killed at {2} after {3}d {4}h {5}m; {6} damager(s){7}",
                    Serial.Value, Name, Location, alive.Days, alive.Hours, alive.Minutes, rights.Count,
                    top == null ? "" : string.Format(", top {0} (0x{1:X8}) {2} damage", top.m_Mobile?.Name ?? "unknown", top.m_Mobile?.Serial.Value ?? 0, top.m_Damage)));
            }

            base.OnDeath(c);
        }

        [Constructable]
        public Khartag()
            : base(AIType.AI_Melee, 0.175, 0.350)
        {
            BaseSoundID = 0x45A;
            IOBAlignment = IOBAlignment.Orcish;

            Name = Utility.RandomList("Baloth Bloodtusk", "Thulgeg", "Gortwog gro-Nagorm", "Khartag", "Torug gro-Igron", "Orcus gro-Kurl", "Yashnag gro-Yazgu", "Kurog gro-Bagrakh");
            Title = Utility.RandomList("Warlord", "War Chief", "Clan Chief", "Battlelord", "Horde Chief", "Warboss", "High Chieftain", "Blood Chief");
            Female = false;
            Body = 0x07;
            Hue = 0x83EC;
            BardImmune = (Core.RuleSets.SeasonsRules(2, 3) || Core.RuleSets.UpheavalRules()) ? false : true;

            SetStr(305, 425);
            SetDex(72, 150);
            SetInt(505, 750);

            SetHits(4200);
            SetStam(102, 300);

            SetDamage(25, 35);

            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 70;
        }
        #region DraggingMitigation

        public override List<Mobile> GetDraggingMitigationHelpers()
        {
            List<Mobile> helpers = new List<Mobile>();
            // okay, now we take action against this bothersome individual!
            for (int ix = 0; ix < 2; ix++)
            {   // these will be our helpers
                if (Utility.RandomBool())
                    helpers.Add(new OrcishLord());
                else
                    helpers.Add(new OrcishMage());
            }
            return helpers;
        }
        #endregion DraggingMitigation

        public override void OnDeathLeaderBoards(List<DamageStore> list)
        {
            AwardDeathRewards(
                list,
                awardTownship: true,
                RecorderDomain.PvMPoints,
                RecorderDomain.LaunchEventPoints);
        }
        public override void DistributeLoot()
        {
            if (IsChampion)
            {
                if (this.Map != null)
                {   // give harrower loot, but in champ numbers
                    GiveMagicItems(this, magicItems: ChampLootPack.GetHarrowerMagicItems(), specialRewards: ChampLootPack.GetHarrowerSpecialRewards());
                    DoGoodies(this);
                }
            }
        }
        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            bool useMask = (Core.RuleSets.SeasonsRules(2, 3) || Core.RuleSets.UpheavalRules());

            if (useMask ||
                (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled))
            {
                if (m.Player &&
                    m.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
                {
                    return false;
                }
            }

            return base.IsEnemy(m, filter);
        }
        public override void AggressiveAction(Mobile aggressor, bool criminal, object? source = null)
        {
            base.AggressiveAction(aggressor, criminal);

            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
            {
                Item? item = aggressor.FindItemOnLayer(Layer.Helm);

                if (item is OrcishKinMask)
                {
                    AOS.Damage(aggressor, 50, new DamageContext { Source = this });
                    item.Delete();
                    aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                    aggressor.PlaySound(0x307);
                }
            }
        }
        public override void GenerateLoot()
        {
            if (IsChampion)
            {
                if (Spawning) { }
                else
                {
                    switch (Utility.Random(3))
                    {
                        case 0:
                            {
                                Item deadOrc = new Item(0x3D64);
                                deadOrc.Name = Server.Items.Corpse.GetCorpseName(this);
                                PackItem(deadOrc);
                                break;
                            }
                        case 1:
                            {

                                PackItem(new XmlAddonDeed("a skull of the orc champion"));
                                break;
                            }
                        case 2:
                            {

                                PackItem(new CannonDeed());
                                break;
                            }

                    }
                }
            }
        }
        public override bool AlwaysMurderer { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(this); } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool ClickTitle { get { return false; } }

        public void Polymorph(Mobile m)
        {
            if (!m.CanBeginAction(typeof(PolymorphSpell)) || !m.CanBeginAction(typeof(IncognitoSpell)) || m.IsBodyMod)
                return;

            IMount mount = m.Mount;

            if (mount != null)
                mount.Rider = null;

            if (m.Mounted)
                return;

            if (m.BeginAction(typeof(PolymorphSpell)))
            {
                Item? disarm = m.FindItemOnLayer(Layer.OneHanded);

                if (disarm != null && disarm.Movable)
                    m.AddToBackpack(disarm);

                disarm = m.FindItemOnLayer(Layer.TwoHanded);

                if (disarm != null && disarm.Movable)
                    m.AddToBackpack(disarm);

                m.BodyMod = 17; // orc
                m.HueMod = 0;

                PolymorphSpell.StartTimer(m, new ExpirePolymorphTimer(m));
            }
        }

        private class ExpirePolymorphTimer : Timer
        {
            private Mobile m_Owner;

            public ExpirePolymorphTimer(Mobile owner)
                : base(TimeSpan.FromMinutes(3.0))
            {
                m_Owner = owner;

                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                if (!m_Owner.CanBeginAction(typeof(PolymorphSpell)))
                {
                    m_Owner.BodyMod = 0;
                    m_Owner.HueMod = -1;
                    m_Owner.EndAction(typeof(PolymorphSpell));
                }
            }
        }

        public void SpawnOrcs(Mobile target)
        {
            // too tough? no reinforcements
            if (!AllowReinforcements)
                return;

            Map map = this.Map;

            if (map == null)
                return;

            int orcs = 0;

            IPooledEnumerable eable = this.GetMobilesInRange(10);
            foreach (Mobile m in eable)
            {
                if (m is Orc || m is OrcCaptain || m is OrcishLord || m is OrcBomber || m is OrcBrute || m is OrcishMage)
                    ++orcs;
            }
            eable.Free();

            if (orcs < 16)
            {
                int newOrcs = Utility.RandomMinMax(3, 6);

                try
                {
                    for (int i = 0; i < newOrcs; ++i)
                    {
                        // switch to SpawnedOrcishLord() to prevent 'loot milking'
                        BaseCreature orc = new SpawnedOrcishLord();

                        orc.Team = this.Team;

                        bool validLocation = false;
                        Point3D loc = this.Location;

                        for (int j = 0; !validLocation && j < 10; ++j)
                        {
                            int x = target.X + Utility.Random(3) - 1;
                            int y = target.Y + Utility.Random(3) - 1;
                            int z = map.GetAverageZ(x, y);

                            if (validLocation = Utility.CanFit(map, x, y, target.Z, 16, Utility.CanFitFlags.requireSurface))
                                loc = new Point3D(x, y, Z);
                            else if (validLocation = Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.requireSurface))
                                loc = new Point3D(x, y, z);
                        }

                        orc.MoveToWorld(loc, map);
                        orc.OnAfterSpawn();

                        orc.Combatant = target;
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    Console.WriteLine("Exception (non-fatal) caught at Khartag.Damage: " + e.Message);
                }
            }
        }

        public void DoSpecialAbility(Mobile target)
        {
            if (target != null && target is PlayerMobile)
            {
                if (0.6 >= Utility.RandomDouble()) // 60% chance to polymorph attacker into a orc
                    Polymorph(target);
                else if (0.2 >= Utility.RandomDouble()) // 20% chance to more orc
                    SpawnOrcs(target);
            }

            if (Hits < 500 && !IsBodyMod) // Khartag is low on life, polymorph into a orc
                Polymorph(this);
        }

        public override void Damage(int amount, Mobile from, DamageContext context)
        {
            base.Damage(amount, from, context);

            DoSpecialAbility(from);
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            DoSpecialAbility(defender);
        }

        public Khartag(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write(TerritoryCampaign ?? string.Empty);

            // version 1
            writer.Write(m_allowReinforcements);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        TerritoryCampaign = reader.ReadString() ?? string.Empty;
                        goto case 1;
                    }
                case 1:
                    {
                        m_allowReinforcements = reader.ReadBool();
                        break;
                    }

                case 0:
                    {
                        m_allowReinforcements = true;
                        break;
                    }
            }
        }
    }
}