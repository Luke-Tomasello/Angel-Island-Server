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

/* Scripts\Spells\Crafting\CraftingMagic.cs
 * 	ChangeLog:
 * 	12/16/21, Adam (Resurce)
 * 	    Add weapon Resource to the logging:
 * 	    weapon.DamageLevel.ToString(),                         
 * 	    weapon.AccuracyLevel.ToString(),                         
 * 	    weapon.DurabilityLevel.ToString(),                        
 * 	    weapon.Resource.ToString(),                        
 * 	    Caster));
 * 	    Also add logging of failures so we can calc success chances.
 * 	10/13/21, Adam
 * 	    All level upgrades for damage, accuracy, and protection.
 * 	    Durability remains unaffected. 
 *	7/7/21, adam
 *	    First time checkin.
 *	    These spells are implicit - not cast - when a crafter is attempting to craft the 'magical ores'
 *	    The magical ores are everything but iron.
 */

using Server.Diagnostics;
using Server.Items;
using System.Collections.Generic;

namespace Server.Spells.Crafting
{
    public class CraftSpell : Spell
    {
        private Stack<int> m_hueStack = new Stack<int>();
        CraftResource m_resource;
        CraftResource Resource { get { return m_resource; } }
        Item m_item;
        private List<CraftResource> softMetals = new List<CraftResource>() { CraftResource.DullCopper, CraftResource.Copper, CraftResource.Gold };
        private List<CraftResource> moderatelySoftMetals = new List<CraftResource>() { CraftResource.Bronze };
        private Dictionary<CraftResource, SpellCircle> CircleLookup = new Dictionary<CraftResource, SpellCircle>()
        { 
            // metals                                               woods                                                leather
            { CraftResource.DullCopper, SpellCircle.First },        { CraftResource.RegularWood, SpellCircle.First },   { CraftResource.RegularLeather, SpellCircle.First },
            { CraftResource.Iron, SpellCircle.First },              { CraftResource.OakWood, SpellCircle.First },
            { CraftResource.ShadowIron, SpellCircle.Second },
            { CraftResource.Copper, SpellCircle.Third },            { CraftResource.AshWood, SpellCircle.Third },
            { CraftResource.Bronze, SpellCircle.Fourth },           { CraftResource.YewWood, SpellCircle.Fourth },      { CraftResource.SpinedLeather, SpellCircle.Fourth },
            { CraftResource.Gold, SpellCircle.Fifth },
            { CraftResource.Agapite, SpellCircle.Sixth },           { CraftResource.Heartwood, SpellCircle.Sixth },
            { CraftResource.Verite, SpellCircle.Seventh },          { CraftResource.Bloodwood, SpellCircle.Seventh },   { CraftResource.HornedLeather, SpellCircle.Seventh },
            { CraftResource.Valorite, SpellCircle.Eighth },         { CraftResource.Frostwood, SpellCircle.Eighth },    { CraftResource.BarbedLeather, SpellCircle.Eighth },
        };
        private Dictionary<CraftResource, CraftResource> LevelFromResource = new Dictionary<CraftResource, CraftResource>()
        { 
            // metals                                               woods                                                    leather
            { CraftResource.DullCopper, CraftResource.DullCopper }, { CraftResource.RegularWood, CraftResource.DullCopper }, { CraftResource.RegularLeather,  CraftResource.DullCopper},
            { CraftResource.Iron, CraftResource.Iron },             { CraftResource.OakWood, CraftResource.DullCopper },
            { CraftResource.ShadowIron, CraftResource.ShadowIron},
            { CraftResource.Copper, CraftResource.Copper},          { CraftResource.AshWood, CraftResource.Copper },
            { CraftResource.Bronze, CraftResource.Bronze},          { CraftResource.YewWood, CraftResource.Bronze },        { CraftResource.SpinedLeather, CraftResource.Bronze },
            { CraftResource.Gold, CraftResource.Gold},
            { CraftResource.Agapite, CraftResource.Agapite},        { CraftResource.Heartwood, CraftResource.Agapite },
            { CraftResource.Verite, CraftResource.Verite},          { CraftResource.Bloodwood, CraftResource.Verite },      { CraftResource.HornedLeather, CraftResource.Verite },
            { CraftResource.Valorite, CraftResource.Valorite},      { CraftResource.Frostwood, CraftResource.Valorite },    { CraftResource.BarbedLeather, CraftResource.Valorite },
        };
        double MySkill { get { return Caster.Skills[CastSkill].Value; } }
        List<CraftResource> SoftMetals { get { return softMetals; } }
        public Item Item { get { return m_item; } set { m_item = value; } }
        private bool m_doNotColor = false;
        public bool DoNotColor { get { return m_doNotColor; } }
        private double m_chance = 0.0;
        public double SuccessChance { get { return m_chance; } }
        private static SpellInfo m_Info = new SpellInfo(null, null, (SpellCircle)0, 4,/*no animation*/0, true, Reagent.Garlic, Reagent.MandrakeRoot);

        public CraftSpell(Mobile caster, double chance, Item item, CraftResource resource, bool doNotColor)
            : base(caster, null, m_Info)
        {
            m_item = item;
            m_resource = resource;
            m_doNotColor = doNotColor;
            Circle = CircleLookup[resource];
            m_chance = chance;
        }

        public override bool CheckCast()
        {
            if (!base.CheckCast())
                return false;
            return true;
        }

        public override void OnCast()
        {
        }

        public WeaponDurabilityLevel WeaponDurabilitySelector(bool downgrade)
        {
            double minSkill, maxSkill;

            GetCastSkills(out minSkill, out maxSkill);
            CraftResource Level = LevelFromResource[Resource];
            if (Utility.RandomChance(25.0))  // 25% chance at a level upgrade
                Level = LevelUpgrade();

            // pick the best the caster can afford.
            // If we're downgrading, fail the first level you are skilled enough to achieve
            //  then reset downgrade.
            if (MySkill >= minSkill && !SoftMetals.Contains(Level) && !moderatelySoftMetals.Contains(Level) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponDurabilityLevel.Indestructible;

            }
            if (MySkill >= minSkill && !SoftMetals.Contains(Level) && !moderatelySoftMetals.Contains(Level) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level, CraftResource.Verite))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponDurabilityLevel.Fortified;
            }
            if (MySkill >= minSkill && !SoftMetals.Contains(Level) && !moderatelySoftMetals.Contains(Level) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level, CraftResource.Agapite))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponDurabilityLevel.Massive;
            }
            if (MySkill >= minSkill && !SoftMetals.Contains(Level) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel())
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponDurabilityLevel.Substantial;
            }
            if (MySkill >= minSkill && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel())
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponDurabilityLevel.Durable;
            }
            return WeaponDurabilityLevel.Regular;
        }
        public WeaponAccuracyLevel WeaponAccuracySelector(bool downgrade)
        {
            double minSkill, maxSkill;

            GetCastSkills(out minSkill, out maxSkill);
            CraftResource Level = LevelFromResource[Resource];
            if (Utility.RandomChance(25.0))  // 25% chance at a level upgrade
                Level = LevelUpgrade();

            // pick the best the caster can afford.
            if (MySkill >= minSkill && (Level == CraftResource.Valorite) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponAccuracyLevel.Supremely;
            }
            if (MySkill >= minSkill && (Level >= CraftResource.Verite) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponAccuracyLevel.Exceedingly;
            }
            if (MySkill >= minSkill && (Level >= CraftResource.Agapite) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponAccuracyLevel.Eminently;
            }
            if (MySkill >= minSkill && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel())
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponAccuracyLevel.Surpassingly;
            }
            if (MySkill >= minSkill && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel())
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponAccuracyLevel.Accurate;
            }
            return WeaponAccuracyLevel.Regular;
        }
        public WeaponDamageLevel WeaponDamageSelector(bool downgrade)
        {
            double minSkill, maxSkill;

            GetCastSkills(out minSkill, out maxSkill);
            CraftResource Level = LevelFromResource[Resource];
            if (Utility.RandomChance(25.0))  // 25% chance at a level upgrade
                Level = LevelUpgrade();

            // pick the best the caster can afford.
            if (MySkill >= minSkill && (Level == CraftResource.Valorite) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponDamageLevel.Vanquishing;
            }
            if (MySkill >= minSkill && (Level >= CraftResource.Verite) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level, CraftResource.Verite))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponDamageLevel.Power;
            }
            if (MySkill >= minSkill && (Level >= CraftResource.Agapite) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level, CraftResource.Agapite))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponDamageLevel.Force;
            }
            if (MySkill >= minSkill && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel())
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponDamageLevel.Might;
            }
            if (MySkill >= minSkill && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel())
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return WeaponDamageLevel.Ruin;
            }
            return WeaponDamageLevel.Regular;
        }
        private CraftResource LevelUpgrade()
        {
            // no upgrade from Valorite
            if (LevelFromResource[Resource] == CraftResource.Valorite)
                return LevelFromResource[Resource];

            // look for an upgrade
            for (int ix = 0; ix < 10; ix++)
            {
                CraftResource randomLevel = (CraftResource)Utility.RandomMinMax((int)CraftResource.Iron, (int)CraftResource.Valorite);
                if (randomLevel > LevelFromResource[Resource])
                    return randomLevel;
            }

            return LevelFromResource[Resource];
        }

        private bool ChanceAtLevel()
        {   // our default metal that has a high chance of being selected
            return ChanceAtLevel(CraftResource.Gold);
        }
        private bool ChanceAtLevel(CraftResource level)
        {
            return ChanceAtLevel(level, level);
        }
        private bool ChanceAtLevel(CraftResource level, CraftResource newLevel)
        {
            if (!(SuccessChance >= Utility.RandomDouble()))
                return false; // no chance

            double chance = 0.0;
            switch (level)
            {
                case CraftResource.Valorite: chance = 0.22; break;
                case CraftResource.Verite: chance = 0.19; break;
                case CraftResource.Agapite: chance = 0.29; break;
                case CraftResource.Gold: chance = 0.8; break;
                case CraftResource.Bronze: chance = 0.8; break;
                case CraftResource.Copper: chance = 0.8; break;
                case CraftResource.ShadowIron: chance = 0.8; break;
                case CraftResource.DullCopper: chance = 0.8; break;
                default: chance = 0.0; break;
            }

            // special downgrade cases
            if (level == CraftResource.Valorite && newLevel == CraftResource.Verite)
                return (chance * .5) >= Utility.RandomDouble();
            if (level == CraftResource.Valorite && newLevel == CraftResource.Agapite)
                return (chance * .3) >= Utility.RandomDouble();
            if (level == CraftResource.Verite && newLevel == CraftResource.Agapite)
                return (chance * .2) >= Utility.RandomDouble();

            // for all other cases, including upgrades, chance stays as it is.
            return chance >= Utility.RandomDouble();
        }

        public ArmorDurabilityLevel ArmorDurabilitySelector(bool downgrade)
        {
            double minSkill, maxSkill;

            GetCastSkills(out minSkill, out maxSkill);
            CraftResource Level = LevelFromResource[Resource];
            if (Utility.RandomChance(25.0))  // 25% chance at a level upgrade
                Level = LevelUpgrade();

            // pick the best the caster can afford.
            if (MySkill >= minSkill && !SoftMetals.Contains(Level) && !moderatelySoftMetals.Contains(Level) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return ArmorDurabilityLevel.Indestructible;
            }
            if (MySkill >= minSkill && !SoftMetals.Contains(Level) && !moderatelySoftMetals.Contains(Level) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level, CraftResource.Verite))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return ArmorDurabilityLevel.Fortified;
            }
            if (MySkill >= minSkill && !SoftMetals.Contains(Level) && !moderatelySoftMetals.Contains(Level) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level, CraftResource.Agapite))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return ArmorDurabilityLevel.Massive;
            }
            if (MySkill >= minSkill && !SoftMetals.Contains(Level) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel())
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return ArmorDurabilityLevel.Substantial;
            }
            if (MySkill >= minSkill && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel())
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return ArmorDurabilityLevel.Durable;
            }
            return ArmorDurabilityLevel.Regular;
        }
        public ArmorProtectionLevel ArmorProtectionSelector(bool downgrade)
        {
            double minSkill, maxSkill;

            GetCastSkills(out minSkill, out maxSkill);
            CraftResource Level = LevelFromResource[Resource];
            if (Utility.RandomChance(25.0))  // 25% chance at a level upgrade
                Level = LevelUpgrade();

            // pick the best the caster can afford.
            if (MySkill >= minSkill && (Level == CraftResource.Valorite) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return ArmorProtectionLevel.Invulnerability;
            }
            if (MySkill >= minSkill && (Level >= CraftResource.Verite) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level, CraftResource.Verite))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return ArmorProtectionLevel.Fortification;
            }
            if (MySkill >= minSkill && (Level >= CraftResource.Agapite) && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel(Level, CraftResource.Agapite))
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return ArmorProtectionLevel.Hardening;
            }
            if (MySkill >= minSkill && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel())
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return ArmorProtectionLevel.Guarding;
            }
            if (MySkill >= minSkill && Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]) && ChanceAtLevel())
            {
                if (downgrade == true)
                    downgrade = false;
                else
                    return ArmorProtectionLevel.Defense;
            }
            return ArmorProtectionLevel.Regular;
        }
        public bool IsMagical()
        {
            Server.Items.BaseWeapon weapon = (Item as Server.Items.BaseWeapon);
            Server.Items.BaseArmor armor = (Item as Server.Items.BaseArmor);
            Server.Items.BaseShield shield = (Item as Server.Items.BaseShield);

            if (weapon != null)
            {
                if (weapon.DurabilityLevel == WeaponDurabilityLevel.Regular && weapon.AccuracyLevel == WeaponAccuracyLevel.Regular && weapon.DamageLevel == WeaponDamageLevel.Regular)
                    return false;
            }
            else if (armor != null)
            {
                if (armor.DurabilityLevel == ArmorDurabilityLevel.Regular && armor.ProtectionLevel == ArmorProtectionLevel.Regular)
                    return false;
            }
            else if (shield != null)
            {
                if (shield.DurabilityLevel == ArmorDurabilityLevel.Regular && shield.ProtectionLevel == ArmorProtectionLevel.Regular)
                    return false;
            }

            return true;
        }
    }

    public class CraftWeaponSpell : CraftSpell
    {
        double m_chance;
        public CraftWeaponSpell(Mobile caster, double chance, Item item, CraftResource resource, bool doNotColor)
            : base(caster, chance, item, resource, doNotColor)
        {
            m_chance = chance;
        }

        public override void OnCast()
        {
            Server.Items.BaseWeapon weapon = (Item as Server.Items.BaseWeapon);
            if (DoNotColor)
                weapon.Hue = 0;
            weapon.Identified = true;

            if (CheckSequence())
            {
                if (m_chance >= Utility.RandomDouble())
                {   // this is what we want
                    int damageLevelVanq = 0;
                    int damageLevelPower = 0;
                    int damageLevelForce = 0;
                    //for (int ix = 0; ix < 10000; ix++)
                    {
                        weapon.DamageLevel = WeaponDamageSelector(false);
                        weapon.AccuracyLevel = WeaponAccuracySelector(false);
                        weapon.DurabilityLevel = WeaponDurabilitySelector(false);

                        if (weapon.DamageLevel == WeaponDamageLevel.Vanquishing)
                            damageLevelVanq++;
                        if (weapon.DamageLevel == WeaponDamageLevel.Power)
                            damageLevelPower++;
                        if (weapon.DamageLevel == WeaponDamageLevel.Force)
                            damageLevelForce++;
                    }

                    if ((weapon.Quality = (IsMagical()) ? WeaponQuality.Exceptional : weapon.Quality) == WeaponQuality.Exceptional)
                        Effects.PlaySound(Caster.Location, Caster.Map, 0x1E6);
                }
                else
                {   // drop down a level if it is not exceptional
                    weapon.DamageLevel = WeaponDamageSelector(weapon.Quality != WeaponQuality.Exceptional);
                    weapon.AccuracyLevel = WeaponAccuracySelector(weapon.Quality != WeaponQuality.Exceptional);
                    weapon.DurabilityLevel = WeaponDurabilitySelector(weapon.Quality != WeaponQuality.Exceptional);
                    if ((weapon.Quality = (IsMagical()) ? WeaponQuality.Exceptional : weapon.Quality) == WeaponQuality.Exceptional)
                        Effects.PlaySound(Caster.Location, Caster.Map, 0x299);
                }
                if (weapon.Quality == WeaponQuality.Exceptional)
                {
                    Caster.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You consecrate this weapon.");
                    Caster.FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
                }
                // show the player what they made
                weapon.OnSingleClick(Caster);
                // logging
                if (weapon.Quality == WeaponQuality.Exceptional)
                {
                    LogHelper logger = new LogHelper("MagicGearCreation.log", false, true);
                    logger.Log(LogType.Item, weapon,
                        string.Format("SUCCESS: From: {3}: DamageLevel: {0}, AccuracyLevel: {1}, DurabilityLevel: {2}, Resource: {4}",
                        weapon.DamageLevel.ToString(),
                        weapon.AccuracyLevel.ToString(),
                        weapon.DurabilityLevel.ToString(),
                        weapon.Resource.ToString(),
                        Caster));
                    logger.Finish();
                }
                else
                {
                    LogHelper logger = new LogHelper("MagicGearCreation.log", false, true);
                    logger.Log(LogType.Item, weapon,
                        string.Format("FAIL: From: {3}: DamageLevel: {0}, AccuracyLevel: {1}, DurabilityLevel: {2}, Resource: {4}",
                        weapon.DamageLevel.ToString(),
                        weapon.AccuracyLevel.ToString(),
                        weapon.DurabilityLevel.ToString(),
                        weapon.Resource.ToString(),
                        Caster));
                    logger.Finish();
                }
            }

            FinishSequence();
        }
    }
    public class CraftArmorOrShieldSpell : CraftSpell
    {
        double m_chance;
        public CraftArmorOrShieldSpell(Mobile caster, double chance, Item item, CraftResource resource, bool doNotColor)
            : base(caster, chance, item, resource, doNotColor)
        {
            m_chance = chance;
        }

        public override void OnCast()
        {
            Server.Items.BaseArmor armor = (Item as Server.Items.BaseArmor);
            Server.Items.BaseShield shield = (Item as Server.Items.BaseShield);
            if (armor != null)
            {
                if (DoNotColor)
                    armor.Hue = 0;
                armor.Identified = true;
            }
            else
            {
                if (DoNotColor)
                    shield.Hue = 0;
                shield.Identified = true;
            }

            if (CheckSequence())
            {
                if (m_chance >= Utility.RandomDouble())
                {   // this is what we want
                    if (armor != null)
                    {

                        int protectionLevelInvulnerability = 0;
                        int protectionLevelFortification = 0;
                        int protectionLevelHardening = 0;
                        //for (int ix = 0; ix < 10000; ix++)
                        {
                            armor.ProtectionLevel = ArmorProtectionSelector(false);
                            armor.DurabilityLevel = ArmorDurabilitySelector(false);
                            if (armor.ProtectionLevel == ArmorProtectionLevel.Invulnerability)
                                protectionLevelInvulnerability++;
                            if (armor.ProtectionLevel == ArmorProtectionLevel.Fortification)
                                protectionLevelFortification++;
                            if (armor.ProtectionLevel == ArmorProtectionLevel.Hardening)
                                protectionLevelHardening++;
                        }

                        if ((armor.Quality = (IsMagical()) ? ArmorQuality.Exceptional : armor.Quality) == ArmorQuality.Exceptional)
                            Effects.PlaySound(Caster.Location, Caster.Map, 0x1E6);
                    }
                    else
                    {
                        shield.ProtectionLevel = ArmorProtectionSelector(false);
                        shield.DurabilityLevel = ArmorDurabilitySelector(false);
                        if ((shield.Quality = (IsMagical()) ? ArmorQuality.Exceptional : shield.Quality) == ArmorQuality.Exceptional)
                            Effects.PlaySound(Caster.Location, Caster.Map, 0x1E6);
                    }
                }
                else
                {   // drop down a level
                    if (armor != null)
                    {
                        armor.ProtectionLevel = ArmorProtectionSelector(armor.Quality != ArmorQuality.Exceptional);
                        armor.DurabilityLevel = ArmorDurabilitySelector(armor.Quality != ArmorQuality.Exceptional);
                        if ((armor.Quality = (IsMagical()) ? ArmorQuality.Exceptional : armor.Quality) == ArmorQuality.Exceptional)
                            Effects.PlaySound(Caster.Location, Caster.Map, 0x299);
                    }
                    else
                    {
                        shield.ProtectionLevel = ArmorProtectionSelector(armor.Quality != ArmorQuality.Exceptional);
                        shield.DurabilityLevel = ArmorDurabilitySelector(armor.Quality != ArmorQuality.Exceptional);
                        if ((shield.Quality = (IsMagical()) ? ArmorQuality.Exceptional : shield.Quality) == ArmorQuality.Exceptional)
                            Effects.PlaySound(Caster.Location, Caster.Map, 0x299);
                    }
                }
                if (armor != null)
                {
                    if (armor.Quality == ArmorQuality.Exceptional)
                    {
                        Caster.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You consecrate this armor.");
                        Caster.FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
                    }
                }
                else
                {
                    if (shield.Quality == ArmorQuality.Exceptional)
                    {
                        Caster.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You consecrate this shield.");
                        Caster.FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
                    }
                }
                // logging
                if (armor != null)
                {
                    // show the player what they made
                    armor.OnSingleClick(Caster);
                    if (armor.Quality == ArmorQuality.Exceptional)
                    {
                        LogHelper logger = new LogHelper("MagicGearCreation.log", false);
                        logger.Log(LogType.Item, armor, string.Format("From: {2}: Protection: {0}, DurabilityLevel: {1}", armor.ProtectionLevel.ToString(), armor.DurabilityLevel.ToString(), Caster));
                        logger.Finish();
                    }
                }
                else
                {
                    // show the player what they made
                    shield.OnSingleClick(Caster);
                    if (shield.Quality == ArmorQuality.Exceptional)
                    {
                        LogHelper logger = new LogHelper("MagicGearCreation.log", false);
                        logger.Log(LogType.Item, shield, string.Format("From: {2}: Protection: {0}, DurabilityLevel: {1}", shield.ProtectionLevel.ToString(), shield.DurabilityLevel.ToString(), Caster));
                        logger.Finish();
                    }
                }


            }

            FinishSequence();
        }
    }
}