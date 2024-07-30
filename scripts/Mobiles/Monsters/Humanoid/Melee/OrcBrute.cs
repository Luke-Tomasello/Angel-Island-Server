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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/OrcBrute.cs
 * ChangeLog
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	1/14/05, Adam
 *		set chance to 20% to spawn an orc lord when hit by magic
 *		have him immune to >= Poison.Deadly instead of Leathal
 *			This will give Rotting Corpses a chance to poison
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  12/09/04, Jade
 *      Changed body type and hue.
 *  11/10/04, Froste
 *      Implemented new random IOB drop system and changed drop change to 12%
 *	11/05/04, Pigpen
 *		Made changes for Implementation of IOBSystem. Changes include:
 *		Removed IsEnemy and Aggressive Action Checks. These are now handled in BaseCreature.cs
 *		Set Creature IOBAlignment to Orcish.
 *  9/16/04, Pigpen
 * 		Added IOB Functionality to item OrcishKinHelm
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an orcish corpse")]
    public class OrcBrute : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Orc }); } }

        [Constructable]
        public OrcBrute()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Body = 17;
            Hue = 0x8A4;
            BardImmune = true;

            Name = "an orc brute";
            BaseSoundID = 0x45A;
            IOBAlignment = IOBAlignment.Orcish;
            ControlSlots = 5;

            SetStr(767, 945);
            SetDex(66, 75);
            SetInt(46, 70);

            SetHits(476, 552);

            SetDamage(20, 25);

            SetSkill(SkillName.Macing, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 125.1, 140.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 50;
        }

        public override Poison PoisonImmune { get { return Poison.Deadly; } }
        public override int Meat { get { return 2; } }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.SavagesAndOrcs; }
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
                if (m.Player && m.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
                    return false;

            return base.IsEnemy(m, filter);
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            base.AggressiveAction(aggressor, criminal);

            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
            {
                Item item = aggressor.FindItemOnLayer(Layer.Helm);

                if (item is OrcishKinMask)
                {
                    AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0, item);
                    item.Delete();
                    aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                    aggressor.PlaySound(0x307);
                }
            }
        }

        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }

        public override void OnDamagedBySpell(Mobile caster)
        {
            if (caster == this)
                return;

            // Adam: 12% chance to spawn an orc lord
            if (0.12 > Utility.RandomDouble())
                SpawnOrcLord(caster);
        }

        public void SpawnOrcLord(Mobile target)
        {
            Map map = target.Map;

            if (map == null)
                return;

            int orcs = 0;

            IPooledEnumerable eable = this.GetMobilesInRange(10);
            foreach (Mobile m in eable)
            {
                if (m is OrcishLord)
                    ++orcs;
            }
            eable.Free();

            if (orcs < 10)
            {
                BaseCreature orc = new SpawnedOrcishLord();

                orc.Team = this.Team;
                orc.Map = map;

                bool validLocation = false;

                for (int j = 0; !validLocation && j < 10; ++j)
                {
                    int x = target.X + Utility.Random(3) - 1;
                    int y = target.Y + Utility.Random(3) - 1;
                    int z = map.GetAverageZ(x, y);

                    if (validLocation = Utility.CanFit(map, x, y, this.Z, 16, Utility.CanFitFlags.requireSurface))
                        orc.Location = new Point3D(x, y, Z);
                    else if (validLocation = Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.requireSurface))
                        orc.Location = new Point3D(x, y, z);
                }

                if (!validLocation)
                    orc.Location = target.Location;

                orc.Combatant = target;
            }
        }

        public OrcBrute(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(600, 700);
                PackItem(new ShadowIronOre(25));
                PackItem(new IronIngot(10));
                PackMagicEquipment(1, 3, 0.80, 0.80);
                PackMagicEquipment(1, 3, 0.10, 0.10);

                // Froste: 12% random IOB drop
                if (0.12 > Utility.RandomDouble())
                {
                    Item iob = Loot.RandomIOB();
                    PackItem(iob);
                }

                if (0.2 > Utility.RandomDouble())
                    PackItem(new BolaBall());

                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);

                if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
                {
                    // 30% boost to gold
                    PackGold(base.GetGold() / 3);
                }
            }
            else
            {
                // http://web.archive.org/web/20020221205654/uo.stratics.com/hunters/orcbrute.shtml
                // 50 -120 Gold, 25 shadow ore, 10 ingots, a war mace, orc helm, orc mask, bola ball

                if (Core.RuleSets.AllShards)
                {
                    if (Spawning)
                    {
                        PackGold(50, 120);
                    }
                    else
                    {
                        PackItem(new ShadowIronOre(25));
                        PackItem(new IronIngot(10));
                        PackItem(new WarMace());
                        if (Utility.RandomBool())               // TODO: no idea about the drop rates here
                            PackItem(new OrcHelm());
                        else
                            PackItem(typeof(OrcishMask), .1);

                        // 1. http://www.uoguide.com/Savage_Empire
                        // 2. http://uo.stratics.com/secrets/archive/orcsavage.shtml
                        // 3. Bola balls have appeared as loot on Orc Bombers. Balls on Bombers are rather common, around a 50/50% chance of getting a ball or not. They are only appearing as loot on bombers.
                        // 4. http://web.archive.org/web/20020221205654/uo.stratics.com/hunters/orcbrute.shtml
                        // 5. 50 -120 Gold, 25 shadow ore, 10 ingots, a war mace, orc helm, orc mask, bola ball
                        if (PublishInfo.PublishDate >= Core.EraSAVE)   // enable due to above #4&5 above
                            if (0.2 > Utility.RandomDouble())
                                PackItem(new BolaBall());
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        PackItem(new ShadowIronOre(25));
                        PackItem(new IronIngot(10));
                        PackItem(new WarMace());

                        // http://www.uoguide.com/Savage_Empire
                        // http://uo.stratics.com/secrets/archive/orcsavage.shtml
                        if (PublishInfo.PublishDate >= Core.EraSAVE)
                            if (0.05 > Utility.RandomDouble())
                                PackItem(new OrcishKinMask());

                        // 1. http://www.uoguide.com/Savage_Empire
                        // 2. http://uo.stratics.com/secrets/archive/orcsavage.shtml
                        // 3. Bola balls have appeared as loot on Orc Bombers. Balls on Bombers are rather common, around a 50/50% chance of getting a ball or not. They are only appearing as loot on bombers.
                        // 4. http://web.archive.org/web/20020221205654/uo.stratics.com/hunters/orcbrute.shtml
                        // 5. 50 -120 Gold, 25 shadow ore, 10 ingots, a war mace, orc helm, orc mask, bola ball
                        if (PublishInfo.PublishDate >= Core.EraSAVE)   // enable due to above #4&5 above
                            if (0.2 > Utility.RandomDouble())
                                PackItem(new BolaBall());
                    }

                    AddLoot(LootPack.FilthyRich);
                    AddLoot(LootPack.Rich);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}