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

/* Scripts/Mobiles/Monsters/AOS/DemonKnight.cs
 * ChangeLog
 *	7/27/05, Adam
 *		Remove Artifacts
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 10 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("a daemon knight corpse")]
    public class DemonKnight : BaseCreature
    {
        /*
		private static Type[] m_ArtifactRarity10 = new Type[]
			{
				typeof( LegacyOfTheDreadLord ),
				typeof( TheDragonSlayer ),
				typeof( TheTaskmaster )
			};

		private static Type[] m_ArtifactRarity11 = new Type[]
			{
				typeof( ArmorOfFortune ),
				typeof( GauntletsOfNobility ),
				typeof( HelmOfInsight ),
				typeof( HolyKnightsBreastplate ),
				typeof( JackalsCollar ),
				typeof( LeggingsOfBane ),
				typeof( MidnightBracers ),
				typeof( OrnateCrownOfTheHarrower ),
				typeof( ShadowDancerLeggings ),
				typeof( TunicOfFire ),
				typeof( VoiceOfTheFallenKing ),
				typeof( BraceletOfHealth ),
				typeof( OrnamentOfTheMagician ),
				typeof( RingOfTheElements ),
				typeof( RingOfTheVile ),
				typeof( Aegis ),
				typeof( ArcaneShield ),
				typeof( AxeOfTheHeavens ),
				typeof( BladeOfInsanity ),
				typeof( BoneCrusher ),
				typeof( BreathOfTheDead ),
				typeof( Frostbringer ),
				typeof( SerpentsFang ),
				typeof( StaffOfTheMagi ),
				typeof( TheBeserkersMaul )
			};
*/
        public static Item CreateRandomArtifact()
        {
            if (!Core.RuleSets.AOSRules())
                return null;

            return null;
            /*
						int count = ( m_ArtifactRarity10.Length * 5 ) + ( m_ArtifactRarity11.Length * 4 );
						int random = Utility.Random( count );
						Type type;

						if ( random < ( m_ArtifactRarity10.Length * 5 ) )
						{
							type = m_ArtifactRarity10[random / 5];
						}
						else
						{
							random -= m_ArtifactRarity10.Length * 5;
							type = m_ArtifactRarity11[random / 4];
						}

						return Loot.Construct( type );
						*/
        }

        public static Mobile FindRandomPlayer(BaseCreature creature)
        {
            List<DamageStore> rights = BaseCreature.GetLootingRights(creature.DamageEntries, creature.HitsMax);

            for (int i = rights.Count - 1; i >= 0; --i)
            {
                DamageStore ds = (DamageStore)rights[i];

                if (!ds.m_HasRight)
                    rights.RemoveAt(i);
            }

            if (rights.Count > 0)
                return ((DamageStore)rights[Utility.Random(rights.Count)]).m_Mobile;

            return null;
        }

        public static void DistributeArtifact(BaseCreature creature)
        {
            DistributeArtifact(creature, CreateRandomArtifact());
        }

        public static void DistributeArtifact(BaseCreature creature, Item artifact)
        {
            DistributeArtifact(FindRandomPlayer(creature), artifact);
        }

        public static void DistributeArtifact(Mobile to)
        {
            DistributeArtifact(to, CreateRandomArtifact());
        }

        public static void DistributeArtifact(Mobile to, Item artifact)
        {
            if (to == null || artifact == null)
                return;

            Container pack = to.Backpack;

            if (pack == null || !pack.TryDropItem(to, artifact, false))
                to.BankBox.DropItem(artifact);

            to.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
        }

        public static int GetArtifactChance(Mobile boss)
        {
            if (!Core.RuleSets.AOSRules())
                return 0;

            int luck = LootPack.GetLuckChanceForKiller(boss);
            int chance;

            if (boss is DemonKnight)
                chance = 1500 + (luck / 5);
            else
                chance = 750 + (luck / 10);

            return chance;
        }

        public static bool CheckArtifactChance(Mobile boss)
        {
            return GetArtifactChance(boss) > Utility.Random(100000);
        }

        public override WeaponAbility GetWeaponAbility()
        {
            switch (Utility.Random(3))
            {
                default:
                case 0: return WeaponAbility.DoubleStrike;
                case 1: return WeaponAbility.WhirlwindAttack;
                case 2: return WeaponAbility.CrushingBlow;
            }
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (!Summoned && !NoKillAwards && DemonKnight.CheckArtifactChance(this))
                DemonKnight.DistributeArtifact(this);
        }

        [Constructable]
        public DemonKnight()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = NameList.RandomName("demon knight");
            Title = "the Dark Father";
            Body = 318;
            BaseSoundID = 0x165;
            BardImmune = true;

            SetStr(500);
            SetDex(100);
            SetInt(1000);

            SetHits(30000);
            SetMana(5000);

            SetDamage(17, 21);

            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Magery, 100.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.MagicResist, 150.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 120.0);

            Fame = 28000;
            Karma = -28000;

            VirtualArmor = 64;
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }

        private static bool m_InHere;

        public override void OnDamage(int amount, Mobile from, bool willKill, object source_weapon)
        {
            if (from != null && from != this && !m_InHere)
            {
                m_InHere = true;
                AOS.Damage(from, this, Utility.RandomMinMax(8, 20), 100, 0, 0, 0, 0, this);

                MovingEffect(from, 0xECA, 10, 0, false, false, 0, 0);
                PlaySound(0x491);

                if (0.05 > Utility.RandomDouble())
                    Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(CreateBones_Callback), from);

                m_InHere = false;
            }
        }

        public virtual void CreateBones_Callback(object state)
        {
            Mobile from = (Mobile)state;
            Map map = from.Map;

            if (map == null)
                return;

            int count = Utility.RandomMinMax(1, 3);

            for (int i = 0; i < count; ++i)
            {
                int x = from.X + Utility.RandomMinMax(-1, 1);
                int y = from.Y + Utility.RandomMinMax(-1, 1);
                int z = from.Z;

                if (!Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.checkMobiles))
                {
                    z = map.GetAverageZ(x, y);

                    if (z != from.Z && !Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.checkMobiles))
                        continue;
                }

                UnholyBone bone = new UnholyBone();

                bone.Hue = 0;
                bone.Name = "unholy bones";
                bone.ItemID = Utility.Random(0xECA, 9);

                bone.MoveToWorld(new Point3D(x, y, z), map);
            }
        }

        public DemonKnight(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            PackGem();
            PackGold(3500, 4700);
            PackMagicEquipment(3, 3, 1.0, 1.0);
            PackMagicEquipment(3, 3, 1.0, 1.0);
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