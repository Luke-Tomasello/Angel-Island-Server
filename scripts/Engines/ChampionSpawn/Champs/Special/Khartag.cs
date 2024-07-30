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

/* Scripts\Engines\ChampionSpawn\Champs\Special\Khartag.cs
 * CHANGELOG
 *	3/30/2024, Adam
 *	    created - based on Barracoon
 */

using Server.Diagnostics;
using Server.Engines.ChampionSpawn;
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
        public override ChampionSkullType SkullType { get { return ChampionSkullType.None; } }

        [Constructable]
        public Khartag()
            : base(AIType.AI_Melee, 0.175, 0.350)
        {
            BaseSoundID = 0x45A;
            IOBAlignment = IOBAlignment.Orcish;

            Name = Utility.RandomList("Baloth Bloodtusk", "Thulgeg", "Gortwog gro-Nagorm", "Khartag", "Torug gro-Igron", "Orcus gro-Kurl", "Yashnag gro-Yazgu", "Kurog gro-Bagrakh");
            Title = "Warlord";
            Female = false;
            Body = 0x07;
            Hue = 0x83EC;
            BardImmune = true;

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
        public override void GenerateLoot()
        {
            if (IsChampion)
            {
                if (!Core.RuleSets.AngelIslandRules())
                {
                    if (Spawning)
                        return;

                    //AddLoot(LootPack.UltraRich, 3);
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
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
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
                Item disarm = m.FindItemOnLayer(Layer.OneHanded);

                if (disarm != null && disarm.Movable)
                    m.AddToBackpack(disarm);

                disarm = m.FindItemOnLayer(Layer.TwoHanded);

                if (disarm != null && disarm.Movable)
                    m.AddToBackpack(disarm);

                m.BodyMod = 17; // orc
                m.HueMod = 0;

                new ExpirePolymorphTimer(m).Start();
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
                        BaseCreature orc;

                        switch (Utility.Random(5))
                        {
                            default:
                            case 0:
                            case 1: orc = new Orc(); break;
                            case 2:
                            case 3: orc = new OrcCaptain(); break;
                            case 4: orc = new OrcishLord(); break;
                        }

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

        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            base.Damage(amount, from, source_weapon);

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

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}