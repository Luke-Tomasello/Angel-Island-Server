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

/* Items/SkillItems/Tools/BaseRunicTool.cs
 * CHANGELOG:
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *	3/5/05, Adam
 *		Fix the occational 'plain' instrument drop from GetRandomSlayer()
 *		if we have a "Silver", get out as Silvers have NO entries and will fail in the 
 *		else clause: if ( entries.Length == 0 ) return SlayerName.None;
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using System;
using System.Collections;

namespace Server.Items
{
    public abstract class BaseRunicTool : BaseTool
    {
        private CraftResource m_Resource;

        [CommandProperty(AccessLevel.GameMaster)]
        public new CraftResource Resource
        {
            get { return m_Resource; }
            set { m_Resource = value; Hue = CraftResources.GetHue(m_Resource); InvalidateProperties(); }
        }

        public BaseRunicTool(CraftResource resource, int itemID)
            : base(itemID)
        {
            m_Resource = resource;
        }

        public BaseRunicTool(CraftResource resource, int uses, int itemID)
            : base(uses, itemID)
        {
            m_Resource = resource;
        }

        public BaseRunicTool(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write((int)m_Resource);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Resource = (CraftResource)reader.ReadInt();
                        break;
                    }
            }
        }

        private static bool m_IsRunicTool = false;
        private static int m_LuckChance = 0;

        private static int Scale(int min, int max, int low, int high)
        {
            int percent;

            if (m_IsRunicTool)
            {
                percent = Utility.RandomMinMax(min, max);
            }
            else
            {
                // Behold, the worst system ever!
                int v = Utility.RandomMinMax(0, 10000);

                v = (int)Math.Sqrt(v);
                v = 100 - v;

                if (LootPack.CheckLuck(m_LuckChance))
                    v += 10; // TODO: is this right? who knows

                if (v < min)
                    v = min;
                else if (v > max)
                    v = max;

                percent = v;
            }

            int scaledBy = Math.Abs(high - low) + 1;

            if (scaledBy != 0)
                scaledBy = 10000 / scaledBy;

            percent *= (10000 + scaledBy);

            return low + (((high - low) * percent) / 1000001);
        }

        private static void ApplyAttribute(AosAttributes attrs, int min, int max, AosAttribute attr, int low, int high)
        {
            ApplyAttribute(attrs, min, max, attr, low, high, 1);
        }

        private static void ApplyAttribute(AosAttributes attrs, int min, int max, AosAttribute attr, int low, int high, int scale)
        {
            if (attr == AosAttribute.CastSpeed)
                attrs[attr] += Scale(min, max, low / scale, high / scale) * scale;
            else
                attrs[attr] = Scale(min, max, low / scale, high / scale) * scale;

            if (attr == AosAttribute.SpellChanneling)
                attrs[AosAttribute.CastSpeed] -= 1;
        }

        private static void ApplyAttribute(AosArmorAttributes attrs, int min, int max, AosArmorAttribute attr, int low, int high)
        {
            attrs[attr] = Scale(min, max, low, high);
        }

        private static void ApplyAttribute(AosArmorAttributes attrs, int min, int max, AosArmorAttribute attr, int low, int high, int scale)
        {
            attrs[attr] = Scale(min, max, low / scale, high / scale) * scale;
        }

        private static void ApplyAttribute(AosWeaponAttributes attrs, int min, int max, AosWeaponAttribute attr, int low, int high)
        {
            attrs[attr] = Scale(min, max, low, high);
        }

        private static void ApplyAttribute(AosWeaponAttributes attrs, int min, int max, AosWeaponAttribute attr, int low, int high, int scale)
        {
            attrs[attr] = Scale(min, max, low / scale, high / scale) * scale;
        }

        private static void ApplyAttribute(AosElementAttributes attrs, int min, int max, AosElementAttribute attr, int low, int high)
        {
            attrs[attr] = Scale(min, max, low, high);
        }

        private static void ApplyAttribute(AosElementAttributes attrs, int min, int max, AosElementAttribute attr, int low, int high, int scale)
        {
            attrs[attr] = Scale(min, max, low / scale, high / scale) * scale;
        }

        private static SkillName[] m_PossibleBonusSkills = new SkillName[]
            {
                SkillName.Swords,
                SkillName.Fencing,
                SkillName.Macing,
                SkillName.Archery,
                SkillName.Wrestling,
                SkillName.Parry,
                SkillName.Tactics,
                SkillName.Anatomy,
                SkillName.Healing,
                SkillName.Magery,
                SkillName.Meditation,
                SkillName.EvalInt,
                SkillName.MagicResist,
                SkillName.AnimalTaming,
                SkillName.AnimalLore,
                SkillName.Veterinary,
                SkillName.Musicianship,
                SkillName.Provocation,
                SkillName.Discordance,
                SkillName.Peacemaking,
				//SkillName.Chivalry,
				//SkillName.Focus,
				//SkillName.Necromancy,
				SkillName.Stealing,
                SkillName.Stealth,
                SkillName.SpiritSpeak
            };

        private static void ApplySkillBonus(AosSkillBonuses attrs, int min, int max, int index, int low, int high)
        {
            SkillName sk, check;
            double bonus;
            bool found;

            do
            {
                found = false;
                sk = m_PossibleBonusSkills[Utility.Random(m_PossibleBonusSkills.Length)];

                for (int i = 0; !found && i < 5; ++i)
                    found = (attrs.GetValues(i, out check, out bonus) && check == sk);
            } while (found);

            attrs.SetValues(index, sk, Scale(min, max, low, high));
        }

        private static void ApplyResistance(BaseArmor ar, int min, int max, ResistanceType res, int low, int high)
        {
            /*
			switch ( res )
			{
				case ResistanceType.Physical: ar.PhysicalBonus += Scale( min, max, low, high ); break;
				case ResistanceType.Fire: ar.FireBonus += Scale( min, max, low, high ); break;
				case ResistanceType.Cold: ar.ColdBonus += Scale( min, max, low, high ); break;
				case ResistanceType.Poison: ar.PoisonBonus += Scale( min, max, low, high ); break;
				case ResistanceType.Energy: ar.EnergyBonus += Scale( min, max, low, high ); break;
			}
			*/
        }

        private const int MaxProperties = 32;
        private static BitArray m_Props = new BitArray(MaxProperties);
        private static int[] m_Possible = new int[MaxProperties];

        public static int GetUniqueRandom(int count)
        {
            int avail = 0;

            for (int i = 0; i < count; ++i)
            {
                if (!m_Props[i])
                    m_Possible[avail++] = i;
            }

            if (avail == 0)
                return -1;

            int v = m_Possible[Utility.Random(avail)];

            m_Props.Set(v, true);

            return v;
        }

        public void ApplyAttributesTo(BaseWeapon weapon)
        {
            CraftResourceInfo resInfo = CraftResources.GetInfo(m_Resource);

            if (resInfo == null)
                return;

            CraftAttributeInfo attrs = resInfo.AttributeInfo;

            if (attrs == null)
                return;

            int attributeCount = Utility.RandomMinMax(attrs.RunicMinAttributes, attrs.RunicMaxAttributes);
            int min = attrs.RunicMinIntensity;
            int max = attrs.RunicMaxIntensity;

            ApplyAttributesTo(weapon, true, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(BaseWeapon weapon, int attributeCount, int min, int max)
        {
            ApplyAttributesTo(weapon, false, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(BaseWeapon weapon, bool isRunicTool, int luckChance, int attributeCount, int min, int max)
        {
            /*
			m_IsRunicTool = isRunicTool;
			m_LuckChance = luckChance;

			AosAttributes primary = weapon.Attributes;
			AosWeaponAttributes secondary = weapon.WeaponAttributes;

			m_Props.SetAll( false );

			if ( weapon is BaseRanged )
				m_Props.Set( 2, true ); // ranged weapons cannot be ubws or mageweapon

			for ( int i = 0; i < attributeCount; ++i )
			{
				int random = GetUniqueRandom( 24 );

				if ( random == -1 )
					break;

				switch ( random )
				{
					case 0:
					{
						switch ( Utility.Random( 5 ) )
						{
							case 0: ApplyAttribute( secondary, min, max, AosWeaponAttribute.HitPhysicalArea,2, 50, 2 ); break;
							case 1: ApplyAttribute( secondary, min, max, AosWeaponAttribute.HitFireArea,	2, 50, 2 ); break;
							case 2: ApplyAttribute( secondary, min, max, AosWeaponAttribute.HitColdArea,	2, 50, 2 ); break;
							case 3: ApplyAttribute( secondary, min, max, AosWeaponAttribute.HitPoisonArea,	2, 50, 2 ); break;
							case 4: ApplyAttribute( secondary, min, max, AosWeaponAttribute.HitEnergyArea,	2, 50, 2 ); break;
						}

						break;
					}
					case 1:
					{
						switch ( Utility.Random( 4 ) )
						{
							case 0: ApplyAttribute( secondary, min, max, AosWeaponAttribute.HitMagicArrow,	2, 50, 2 ); break;
							case 1: ApplyAttribute( secondary, min, max, AosWeaponAttribute.HitHarm,		2, 50, 2 ); break;
							case 2: ApplyAttribute( secondary, min, max, AosWeaponAttribute.HitFireball,	2, 50, 2 ); break;
							case 3: ApplyAttribute( secondary, min, max, AosWeaponAttribute.HitLightning,	2, 50, 2 ); break;
						}

						break;
					}
					case 2:
					{
						switch ( Utility.Random( 2 ) )
						{
							case 0: ApplyAttribute( secondary, min, max, AosWeaponAttribute.UseBestSkill,	1, 1 ); break;
							case 1: ApplyAttribute( secondary, min, max, AosWeaponAttribute.MageWeapon,		29, 20 ); break;
						}

						break;
					}
					case  3: ApplyAttribute( primary,	min, max, AosAttribute.WeaponDamage,				1, 50 ); break;
					case  4: ApplyAttribute( primary,	min, max, AosAttribute.DefendChance,				1, 15 ); break;
					case  5: ApplyAttribute( primary,	min, max, AosAttribute.CastSpeed,					1, 1 ); break;
					case  6: ApplyAttribute( primary,	min, max, AosAttribute.AttackChance,				1, 15 ); break;
					case  7: ApplyAttribute( primary,	min, max, AosAttribute.Luck,						1, 100 ); break;
					case  8: ApplyAttribute( primary,	min, max, AosAttribute.WeaponSpeed,					5, 30, 5 ); break;
					case  9: ApplyAttribute( primary,	min, max, AosAttribute.SpellChanneling,				1, 1 ); break;
					case 10: ApplyAttribute( secondary, min, max, AosWeaponAttribute.HitDispel,				2, 50, 2 ); break;
					case 11: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.HitLeechHits,			2, 50, 2 ); break;
					case 12: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.HitLowerAttack,		2, 50, 2 ); break;
					case 13: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.HitLowerDefend,		2, 50, 2 ); break;
					case 14: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.HitLeechMana,			2, 50, 2 ); break;
					case 15: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.HitLeechStam,			2, 50, 2 ); break;
					case 16: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.LowerStatReq,			10, 100, 10 ); break;
					case 17: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.ResistPhysicalBonus,	1, 15 ); break;
					case 18: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.ResistFireBonus,		1, 15 ); break;
					case 19: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.ResistColdBonus,		1, 15 ); break;
					case 20: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.ResistPoisonBonus,		1, 15 ); break;
					case 21: ApplyAttribute( secondary,	min, max, AosWeaponAttribute.ResistEnergyBonus,		1, 15 ); break;
					case 22: ApplyAttribute( secondary, min, max, AosWeaponAttribute.DurabilityBonus,		10, 100, 10 ); break;
					case 23: weapon.Slayer = GetRandomSlayer(); break;
				}
			}
		*/
        }

        public static SlayerName GetRandomSlayer()
        {
            // TODO: Check random algorithm on OSI

            SlayerGroup[] groups = SlayerGroup.Groups;

            if (groups.Length == 0)
                return SlayerName.None;

            SlayerGroup group = groups[Utility.Random(groups.Length)];
            SlayerEntry entry;

            // 10% chance to do super slayer
            // Adam: Also, if we have a "Silver", get out now as Silvers have NO entries
            // which will fail in the else clause: if ( entries.Length == 0 ) return SlayerName.None;
            if ((10 > Utility.Random(100)) || (group.Super.Name.ToString() == "Silver"))
            {
                entry = group.Super;
            }
            else
            {
                SlayerEntry[] entries = group.Entries;

                if (entries.Length == 0)
                    return SlayerName.None;

                entry = entries[Utility.Random(entries.Length)];
            }

            return entry.Name;
        }

        public void ApplyAttributesTo(BaseArmor armor)
        {
            CraftResourceInfo resInfo = CraftResources.GetInfo(m_Resource);

            if (resInfo == null)
                return;

            CraftAttributeInfo attrs = resInfo.AttributeInfo;

            if (attrs == null)
                return;

            int attributeCount = Utility.RandomMinMax(attrs.RunicMinAttributes, attrs.RunicMaxAttributes);
            int min = attrs.RunicMinIntensity;
            int max = attrs.RunicMaxIntensity;

            ApplyAttributesTo(armor, true, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(BaseArmor armor, int attributeCount, int min, int max)
        {
            ApplyAttributesTo(armor, false, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(BaseArmor armor, bool isRunicTool, int luckChance, int attributeCount, int min, int max)
        {/*
			m_IsRunicTool = isRunicTool;
			m_LuckChance = luckChance;

			AosAttributes primary = armor.Attributes;
			AosArmorAttributes secondary = armor.ArmorAttributes;

			m_Props.SetAll( false );

			bool isShield = ( armor is BaseShield );
			int baseCount = ( isShield ? 7 : 19 );
			int baseOffset = ( isShield ? 0 : 4 );

			if ( !isShield && armor.MeditationAllowance == ArmorMeditationAllowance.All )
				m_Props.Set( 3, true ); // remove mage armor from possible properties

			for ( int i = 0; i < attributeCount; ++i )
			{
				int random = GetUniqueRandom( baseCount );

				if ( random == -1 )
					break;

				random += baseOffset;

				switch ( random )
				{
						// Begin Sheilds 
					case  0: ApplyAttribute( primary,	min, max, AosAttribute.SpellChanneling,			1, 1 ); break;
					case  1: ApplyAttribute( primary,	min, max, AosAttribute.DefendChance,			1, 15 ); break;
					case  2: ApplyAttribute( primary,	min, max, AosAttribute.AttackChance,			1, 15 ); break;
					case  3: ApplyAttribute( primary,	min, max, AosAttribute.CastSpeed,				1, 1 ); break;
						// Begin Armor 
					case  4: ApplyAttribute( secondary,	min, max, AosArmorAttribute.LowerStatReq,		10, 100, 10 ); break;
					case  5: ApplyAttribute( secondary,	min, max, AosArmorAttribute.SelfRepair,			1, 5 ); break;
					case  6: ApplyAttribute( secondary,	min, max, AosArmorAttribute.DurabilityBonus,	10, 100, 10 ); break;
						// End Shields 
					case  7: ApplyAttribute( secondary,	min, max, AosArmorAttribute.MageArmor,			1, 1 ); break;
					case  8: ApplyAttribute( primary,	min, max, AosAttribute.RegenHits,				1, 2 ); break;
					case  9: ApplyAttribute( primary,	min, max, AosAttribute.RegenStam,				1, 3 ); break;
					case 10: ApplyAttribute( primary,	min, max, AosAttribute.RegenMana,				1, 2 ); break;
					case 11: ApplyAttribute( primary,	min, max, AosAttribute.BonusHits,				1, 5 ); break;
					case 12: ApplyAttribute( primary,	min, max, AosAttribute.BonusStam,				1, 8 ); break;
					case 13: ApplyAttribute( primary,	min, max, AosAttribute.BonusMana,				1, 8 ); break;
					case 14: ApplyAttribute( primary,	min, max, AosAttribute.LowerManaCost,			1, 8 ); break;
					case 15: ApplyAttribute( primary,	min, max, AosAttribute.LowerRegCost,			1, 20 ); break;
					case 16: ApplyAttribute( primary,	min, max, AosAttribute.Luck,					1, 100 ); break;
					case 17: ApplyAttribute( primary,	min, max, AosAttribute.ReflectPhysical,			1, 15 ); break;
					case 18: ApplyResistance( armor,	min, max, ResistanceType.Physical,				1, 15 ); break;
					case 19: ApplyResistance( armor,	min, max, ResistanceType.Fire,					1, 15 ); break;
					case 20: ApplyResistance( armor,	min, max, ResistanceType.Cold,					1, 15 ); break;
					case 21: ApplyResistance( armor,	min, max, ResistanceType.Poison,				1, 15 ); break;
					case 22: ApplyResistance( armor,	min, max, ResistanceType.Energy,				1, 15 ); break;
						// End Armor 
				}
			}
			*/
        }

        public static void ApplyAttributesTo(BaseHat hat, int attributeCount, int min, int max)
        {
            ApplyAttributesTo(hat, false, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(BaseHat hat, bool isRunicTool, int luckChance, int attributeCount, int min, int max)
        {
            /*m_IsRunicTool = isRunicTool;
			m_LuckChance = luckChance;

			AosAttributes primary = hat.Attributes;
			AosArmorAttributes secondary = hat.ClothingAttributes;
			AosElementAttributes resists = hat.Resistances;

			m_Props.SetAll(false);

			for (int i = 0; i < attributeCount; ++i)
			{
				int random = GetUniqueRandom(19);

				if (random == -1)
					break;

				switch (random)
				{
					case 0: ApplyAttribute(primary, min, max, AosAttribute.ReflectPhysical, 1, 15); break;
					case 1: ApplyAttribute(primary, min, max, AosAttribute.RegenHits, 1, 2); break;
					case 2: ApplyAttribute(primary, min, max, AosAttribute.RegenStam, 1, 3); break;
					case 3: ApplyAttribute(primary, min, max, AosAttribute.RegenMana, 1, 2); break;
					case 4: ApplyAttribute(primary, min, max, AosAttribute.NightSight, 1, 1); break;
					case 5: ApplyAttribute(primary, min, max, AosAttribute.BonusHits, 1, 5); break;
					case 6: ApplyAttribute(primary, min, max, AosAttribute.BonusStam, 1, 8); break;
					case 7: ApplyAttribute(primary, min, max, AosAttribute.BonusMana, 1, 8); break;
					case 8: ApplyAttribute(primary, min, max, AosAttribute.LowerManaCost, 1, 8); break;
					case 9: ApplyAttribute(primary, min, max, AosAttribute.LowerRegCost, 1, 20); break;
					case 10: ApplyAttribute(primary, min, max, AosAttribute.Luck, 1, 100); break;
					case 11: ApplyAttribute(secondary, min, max, AosArmorAttribute.LowerStatReq, 10, 100, 10); break;
					case 12: ApplyAttribute(secondary, min, max, AosArmorAttribute.SelfRepair, 1, 5); break;
					case 13: ApplyAttribute(secondary, min, max, AosArmorAttribute.DurabilityBonus, 10, 100, 10); break;
					case 14: ApplyAttribute(resists, min, max, AosElementAttribute.Physical, 1, 15); break;
					case 15: ApplyAttribute(resists, min, max, AosElementAttribute.Fire, 1, 15); break;
					case 16: ApplyAttribute(resists, min, max, AosElementAttribute.Cold, 1, 15); break;
					case 17: ApplyAttribute(resists, min, max, AosElementAttribute.Poison, 1, 15); break;
					case 18: ApplyAttribute(resists, min, max, AosElementAttribute.Energy, 1, 15); break;
				}
			}*/
        }

        public static void ApplyAttributesTo(BaseJewel jewelry, int attributeCount, int min, int max)
        {
            ApplyAttributesTo(jewelry, false, 0, attributeCount, min, max);
        }

        public static void ApplyAttributesTo(BaseJewel jewelry, bool isRunicTool, int luckChance, int attributeCount, int min, int max)
        {
            /*
				m_IsRunicTool = isRunicTool;
				m_LuckChance = luckChance;

				AosAttributes primary = jewelry.Attributes;
				AosElementAttributes resists = jewelry.Resistances;
				AosSkillBonuses skills = jewelry.SkillBonuses;

				m_Props.SetAll( false );

				for ( int i = 0; i < attributeCount; ++i )
				{
					int random = GetUniqueRandom( 23 );

					if ( random == -1 )
						break;

					switch ( random )
					{
						case  0: ApplyAttribute( resists,	min, max, AosElementAttribute.Physical,			1, 15 ); break;
						case  1: ApplyAttribute( resists,	min, max, AosElementAttribute.Fire,				1, 15 ); break;
						case  2: ApplyAttribute( resists,	min, max, AosElementAttribute.Cold,				1, 15 ); break;
						case  3: ApplyAttribute( resists,	min, max, AosElementAttribute.Poison,			1, 15 ); break;
						case  4: ApplyAttribute( resists,	min, max, AosElementAttribute.Energy,			1, 15 ); break;
						case  5: ApplyAttribute( primary,	min, max, AosAttribute.WeaponDamage,			1, 25 ); break;
						case  6: ApplyAttribute( primary,	min, max, AosAttribute.DefendChance,			1, 15 ); break;
						case  7: ApplyAttribute( primary,	min, max, AosAttribute.AttackChance,			1, 15 ); break;
						case  8: ApplyAttribute( primary,	min, max, AosAttribute.BonusStr,				1, 8 ); break;
						case  9: ApplyAttribute( primary,	min, max, AosAttribute.BonusDex,				1, 8 ); break;
						case 10: ApplyAttribute( primary,	min, max, AosAttribute.BonusInt,				1, 8 ); break;
						case 11: ApplyAttribute( primary,	min, max, AosAttribute.EnhancePotions,			5, 25, 5 ); break;
						case 12: ApplyAttribute( primary,	min, max, AosAttribute.CastSpeed,				1, 1 ); break;
						case 13: ApplyAttribute( primary,	min, max, AosAttribute.CastRecovery,			1, 3 ); break;
						case 14: ApplyAttribute( primary,	min, max, AosAttribute.LowerManaCost,			1, 8 ); break;
						case 15: ApplyAttribute( primary,	min, max, AosAttribute.LowerRegCost,			1, 20 ); break;
						case 16: ApplyAttribute( primary,	min, max, AosAttribute.Luck,					1, 100 ); break;
						case 17: ApplyAttribute( primary,	min, max, AosAttribute.SpellDamage,				1, 12 ); break;
						case 18: ApplySkillBonus( skills,	min, max, 0,									1, 15 ); break;
						case 19: ApplySkillBonus( skills,	min, max, 1,									1, 15 ); break;
						case 20: ApplySkillBonus( skills,	min, max, 2,									1, 15 ); break;
						case 21: ApplySkillBonus( skills,	min, max, 3,									1, 15 ); break;
						case 22: ApplySkillBonus( skills,	min, max, 4,									1, 15 ); break;
					}
				}
				*/
        }
    }
}