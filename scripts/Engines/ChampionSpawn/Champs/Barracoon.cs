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

/* Scripts\Engines\ChampionSpawn\Champs\Barracoon.cs
 * CHANGELOG
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  01/05/07, plasma
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *  12/03/06 Taran Kain
 *      Set Female = false. No trannies!
 *  8/16/06, Rhiannon
 *		Added speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *  3/23/04, mith
 *		Removed spawn of gold items in pack.
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
    public class Barracoon : BaseChampion
    {
        public override ChampionSkullType SkullType { get { return ChampionSkullType.Greed; } }

        [Constructable]
        public Barracoon()
            : base(AIType.AI_Melee, 0.175, 0.350)
        {
            Name = "Barracoon";
            Title = "the piper";
            Female = false;
            Body = 0x190;
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

            AddItem(new FancyShirt(Utility.RandomGreenHue()));
            AddItem(new LongPants(Utility.RandomYellowHue()));
            AddItem(new JesterHat(Utility.RandomPinkHue()));
            AddItem(new Cloak(Utility.RandomPinkHue()));
            AddItem(new Sandals());

            AddItem(new ShortHair(148));
        }
        #region DraggingMitigation

        public override List<Mobile> GetDraggingMitigationHelpers()
        {
            List<Mobile> helpers = new List<Mobile>();
            // okay, now we take action against this bothersome individual!
            for (int ix = 0; ix < 2; ix++)
            {   // these will be out helpers
                if (Utility.RandomBool())
                    helpers.Add(new RatmanArcher());
                else
                    helpers.Add(new SilverSerpent());
            }
            return helpers;
        }
        #endregion DraggingMitigation
        public override void GenerateLoot()
        {
            if (IsChampion)
            {
                if (!Core.RuleSets.AngelIslandRules())
                {
                    AddLoot(LootPack.UltraRich, 3);
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

                m.BodyMod = 42;
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

        public void SpawnRatmen(Mobile target)
        {
            Map map = this.Map;

            if (map == null)
                return;

            int rats = 0;

            IPooledEnumerable eable = this.GetMobilesInRange(10);
            foreach (Mobile m in eable)
            {
                if (m is Ratman || m is RatmanArcher || m is RatmanMage)
                    ++rats;
            }
            eable.Free();

            if (rats < 16)
            {
                int newRats = Utility.RandomMinMax(3, 6);

                try
                {
                    for (int i = 0; i < newRats; ++i)
                    {
                        BaseCreature rat;

                        switch (Utility.Random(5))
                        {
                            default:
                            case 0:
                            case 1: rat = new Ratman(); break;
                            case 2:
                            case 3: rat = new RatmanArcher(); break;
                            case 4: rat = new RatmanMage(); break;
                        }

                        rat.Team = this.Team;

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

                        rat.MoveToWorld(loc, map);

                        rat.Combatant = target;
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    Console.WriteLine("Exception (non-fatal) caught at Barracoon.Damage: " + e.Message);
                }
            }
        }

        public void DoSpecialAbility(Mobile target)
        {
            if (target != null && target is PlayerMobile)
            {
                if (0.6 >= Utility.RandomDouble()) // 60% chance to polymorph attacker into a ratman
                    Polymorph(target);
                else if (0.2 >= Utility.RandomDouble()) // 20% chance to more ratmen
                    SpawnRatmen(target);
            }

            if (Hits < 500 && !IsBodyMod) // Baracoon is low on life, polymorph into a ratman
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

        public Barracoon(Serial serial)
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