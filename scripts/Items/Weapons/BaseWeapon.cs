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

/* Scripts/Items/Weapons/BaseWeapon.cs
 * ChangeLog:
 *  3/26/2024, Adam
 *      Prevent staff with AccessLevel < Owner from setting MaxRange, Speed, MinDamage, and MaxDamage
 *  8/29/2023, Adam (BaseWand)
 *      Wands actually do have an OldStrengthReq of 0, so stop the complaints on the console
 *  7/19/2023, Adam (Slayers)
 *      If the attacker is not a player, slayers work against tames.
 *  5/16/2023, Adam (Special Moves)
 *      ensure both "concussion blow" and "crushing blow" have the 30 second cooldown
 *  5/16/23, Yoar
 *      Concussion blow now properly applies the temporary INT debuff
 *  4/26/23, Yoar
 *      Reworked the poison hit chance formula. Now using an incremented
 *      probability for predictable randomness.
 *  4/25/23, Yoar
 *      - Refactored weapon wear code
 *      - Extended pre-AOS corrosion effect to ranged weapons as well,
 *        but with adjusted overhead message
 *  4/25/23, Yoar
 *      Added pre-AOS corrosion effect for melee weapons
 *  4/20/23, Yoar
 *      Reworked old-school naming
 *  4/16/23, Adam
 *      Only War Hammer gets crushing blow
 *      http://uo.stratics.com/content/skills/macefighting.shtml
 *  4/16/23, Yoar
 *      Implemented Pub13 changes to two-handed weapons' special hits.
 *  3/29/23, Yoar
 *      Added Pub5Moves setting
 *  3/26/23, Yoar
 *      Changed MagicCharges data type from ushort to int
 *  3/26/23, Yoar: Old name formatting
 *      Now splitting double suffix with "and" so that the weapon says "of X and Y" rather than "of X of Y".
 *  3/25/23, Yoar
 *      Changed SaveFlag data type from int to ulong
 *      Added MagicEffect, MagicCharges
 *  1/10/23, Yoar
 *      Added Ethic bless handles
 *  04/02/22, Yoar
 *      Added MakersMark struct that stored both the crafter's serial and name as string.
 *      This way, the crafter tag persists after the mobile has been deleted.
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *  11/9/21, Adam
 *      Fix a bug that was preventing non magic weapons to retain their hue
 *  10/23/21 (ScaleDamageOld)
 *      Adam: Cap quality bonus at Exceptional
 *  10/22/21, Yoar
 *      Added virtual double BaseCreature.GetAccuracyScalar().
 *  10/13/21, Adam (CorrodeWeapon())
 *      soft metals are corrosion-resistant materials
 *	3/2/11, Adam
 *		packInstinctBonus is now based upon the following calculation: if (PublishInfo.Publish >= 16 || Core.UOAI || Core.UOAR || Core.UOMO)
 *		Since Siege is Publish 15, Siege on't have a packInstinctBonus
 *	11/15/10, Adam
 *		Add comment explaining that there is no need to condition the SlayerBonus for SP shards since it will
 *		never get set in SpiritSpeak
 * 11/11/10, Pix
 *      For UOSP, revert back to 50% hit rate GM skill vs GM skill
 *	7/23/10, adam
 *		reduce blood timer in Slayer Damage system to 15 minutes from 24 hours.
 *	7/20/10, adam
 *		Add slayer damage bonus
 *		You have a 10 second window to pump up to ((m.Skills.SpiritSpeak.Value + (m.Dex * 2.0) + (m.Skills.Tactics.Value * 2.0)) / 5.0)
 *			bonus points of damage into the DamageAccumulator before it is transfered into the DamageCache
 *			where it is used by BaseWeapon. The DamageCache will hold that charge to 10 seconds.
 *			The DamageAccumulator cannot accept new bonus points until the DamageCache times-out
 *	6/27/10, Adam
 *		Add the notion of Smart Guards to defend against in-town griefing (exp pots, and melee attacks)
 *	6/25/10, Adam
 *		We introduce the notion of 'Interference' : CheckInterference(Mobile attacker, Mobile defender) 
 * 		Interference is where you have a guard on you and the guard is trying to stop you from killing a 
 * 		some poor citizen. The guard is trained in such things.
 *	6/10/10m, Adam
 *		o Add shoes as a clothing item that can take damage (same chance as handArmor)
 *		o rewrite the mess of clothing-takes-damage logic to fit with their armor-takes-damage logic
 *			(compatible with RunUO but adds some clothing pieces like cloaks and shoes)
 *		o add cloak damage (at the same time we are damaging robes)
 * 2010.05.22 - Pix
 *      weapon labels now use SlayerLabel.GetSlayerLabel()
 *	4/30/10, adam
 *		fix Pack Instincts, 
 *		o Fix the damage = AOS.Scale() logic (based it on Run 2.0) - our old code always returned a 0 bonus.
 *		o in GetPackInstinctBonus add chance based on pet loyality
 *		o in GetPackInstinctBonus factor in herding skill: pack = min(herding/20, pack)
 *  11/9/08, Adam
 *      Replace old MaxHits and Hits with MaxHitPoints and HitPoints (RunUO 2.0 compatibility)
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 * 5/2/08, Adam
 *		Update use of HideAttributes to be simpler.
 *	01/03/07, plasma
 *		Remove all duel challenge system code
 *	11/20/07, Adam
 *		Change damage bonus based on strength to take into account the new mobile.STRBonusCap.
 *		This new STRBonusCap allows playerMobiles to have super STR while 'capping' the STR bonus whereby preventing one-hit killing.
 *  6/1/07, Adam
 *      Add check for new item.HideAttributes bool for suppressing display attributes
 *	03/23/07, Pix
 *		Addressed the 'greyed out' on singleclick thing with oldschool labeled weapson.
 *		Re-added new type display of attributes for named weapons.
 *	03/19/07, Pix
 *		Modified single click to display poison and charges in an old-school manner
 *	01/03/07, Pix
 *		Changed CoreAI.RangedCorrosionModifier's effect to be extra chances to reduce poison corrosion on bows
 *	01/03/07, Pix
 *		Changed ranged corrosion message.
 *		Now only give ranged corrosion message 30% of the time (anti-spam!)
 *	01/02/07, Pix
 *		Added Corrosion to ranged weapons, with switches in CoreAI.
 *		Added logic for sealed bows to not corrode.
 *	01/21/06, Pix
 *		Removed test condition for new version of concussion.
 *	12/22/05, Pix
 *		Compilable first version of concussion for test.
 *	12/21/05, Adam
 *		Condition the Concussion changes on a temp variable 'CoreAI.TempInt'
 *		to allow us to merge into Prod without risk (will be turned on TC only)
 *	12/21/05, Pix
 *		First version of concussion for test.
 *	12/19/05, Pix
 *		Reverted concussion change for further review/testing.
 *	12/17/05, Pix
 *		Changed Concussion's effect to just halve mana.
 *  12/09/05, Kit
 *		Added check to OnHit() to check if creature has a weapon immunity and if so override damage
 *		cleanedup/removed some AOS code not used/needed.
 *	11/10/05, erlein
 *		Removed SaveHue property and added deserialization to pack out old data.
 *  11/09/05, Kit
 *		Added check to GetPoisonBasedOnSkillAndPoison function to always return -1 level poison
 *		if poison base skill is 0. Was returning full poison strenght 1% of time. 
 *  11/07/05, Kit
 *		Added check to allow monsters to deal damage based on weapon vs creature min/max damage.
 *		Allowed creatures to use concussion/crushing/para blow.
 *	10/16/05, Pix
 *		Changed how corrosion works with the poisoning skill.
 *		Added GetPoisonBasedOnSkillAndPoison function.
 *	10/2/05, erlein
 *		Added OnHit() code to handle clothing wear.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 11 lines removed.
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 *	3/7/05, mith
 *		CorrodeWeapon(): Added conditional so that if weapon is Ranged (bow/xbow) it doesn't corrode.
 *	1/12/05, mith
 *		OnMiss(), removed Ranged weapon corrosion.
 *	11/28/04, Adam
 *		Move the OnEquip() logic for bows to BaseRanged where it belongs.
 *	11/28/04, Adam
 *		1. Add TRY to OnEquip
 *		2. if (this.Type == WeaponType.Ranged && from is PlayerMobile)
 *  7/10/04, Old Salty
 * 		Changed ScaleDamageOld so that Lumberjacking gives 20% rather than 30% bonus at GM
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/28/04 smerX
 *		Modified Hit Chance
 *	5/25/04, mith
 *		Modified GetBaseDamage() to take into account the fact that
 *		all monsters use this routine to calculate how much damage they deal as well.
 * 5/12/04, smerX
 *	Changed Para Blow time from 2 seconds to 3 seconds
 * 4/25/04, smerX
 *	Added HasAbilityReady flag
 *	Added special ability functionality in OnHit
 * 4/20/04, pixie
 *  Added poison corrosion ...
 * 4/19/04, mith
 *	If player attempts to equip a bow with a PoisonCloth with > 0 charges on it,
 *		any spells they are casting or are waiting to target will be cancelled.
 * 3/25/04 changes by mith
 *	modified CheckSkill call to use max value of 100.0 instead of 120.0.
 */
using Server.Diagnostics;
using Server.Engines.Craft;
using Server.Engines.EventResources;
using Server.Factions;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.Crafting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public abstract class BaseWeapon : Item, IWeapon, IFactionItem, ICraftable/*, ISlayer, IDurability*/, IMagicItem
    {
        /* Publish 5
         * Two-handed Weapons
         * Any melee weapon that requires two hands to wield will gain a special
         * attack. The type of special attack will depend on the type of weapon
         * used. These special attacks will only work against player characters,
         * not against monsters or animals.
         * - Mace Weapon: Crushing blow, a hit for double damage. Only applies to
         *   true maces, not staves.
         * - Sword Weapon: Concussion blow, victim�s intelligence is halved for 30
         *   seconds. Note the effects of a concussion blow are not cumulative,
         *   once a target is the victim of a concussion blow, they cannot be hit
         *   in that manner again for 30 seconds.
         *   https://wiki.stratics.com/index.php?title=UO:Publish_Notes_from_2000-04-28
         * - Fencing Weapon: Paralyzing blow, victim is paralyzed for 4 seconds.
         *   Once paralyzed, the victim cannot fight back (s/he wont auto-defend)
         *   or cast spells, however s/he can still use potions and bandages. The
         *   paralysis will not break by any means, even if the victim takes
         *   damage. Once paralyzed, the victim cannot be paralyzed again with
         *   another special attack until the paralysis wears off.
         * Upon a successful hit, there will be a small chance to inflict one of
         * the special attacks. The base chance to inflict one of the special
         * attacks is 20%. A high intelligence will give a small bonus towards the
         * chance to execute a special attack up to a total chance of 30%.
         * --------------------------------------
         * The Mace Fighting skill is automatically used when you engage in combat holding a weapon that requires the Mace Fighting skill.
         * Mace Fighting has two advantages over other types of combat:
         * A mace does considerable damage to the armor of your opponent.
         * Mace fighters have a chance of "stunning" their opponent for a moment.
         * Upon a successful hit with a two handed Mace type weapon (excluding staves), there will be a small chance to perform a Crushing Blow, which is a hit for double damage. The base chance to inflict this special damage is your Anatomy skill level divided by 4. The only weapon that can be used for this special attack is the War Hammer.
         * http://uo.stratics.com/content/skills/macefighting.shtml
         */
        public static bool Pub5Moves { get { return (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisRules() && !Core.RuleSets.AOSRules() && PublishInfo.Publish >= 5 && PublishInfo.Publish < 18); } }

        protected bool DoPub5Move(Mobile attacker, Mobile defender)
        {
            // shard test
            if (!Pub5Moves)
                return false;

            // only works with two-handed weapons
            if (Layer != Layer.TwoHanded)
                return false;

            // only War Hammer gets crushing blow
            if (this is BaseBashing && this is not WarHammer)
                return false;

            double chance;

            if (PublishInfo.Publish >= 13.0)
            {
                // works for players and humanoid monsters
                if (!attacker.Player && !attacker.Body.IsHuman)
                    return false;

                chance = attacker.Skills[SkillName.Anatomy].Value / 400.0;
            }
            else
            {
                // only works in PVP
                if (!attacker.Player || !defender.Player)
                    return false;

                // calc the chance: anat bonus max 20% + int bonue max 10% = max total bonus 30%
                chance = (attacker.Skills[SkillName.Anatomy].Value / 500.0) + (attacker.Int / 1000.0);
            }

            return (Utility.RandomDouble() < chance);
        }

        #region Factions
        private FactionItem m_FactionState;

        public FactionItem FactionItemState
        {
            get { return m_FactionState; }
            set
            {
                m_FactionState = value;

                if (m_FactionState == null)
                    Hue = CraftResources.GetHue(Resource);

                LootType = (m_FactionState == null ? LootType.Regular : LootType.Blessed);
            }
        }
        #endregion

        #region Ethics
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime EthicBlessHero
        {
            get { return Ethics.EthicBless.GetExpireHero(this); }
            set { Ethics.EthicBless.SetExpireHero(this, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime EthicBlessEvil
        {
            get { return Ethics.EthicBless.GetExpireEvil(this); }
            set { Ethics.EthicBless.SetExpireEvil(this, value); }
        }
        #endregion

        /* Weapon internals work differently now (Mar 13 2003)
		 *
		 * The attributes defined below default to -1.
		 * If the value is -1, the corresponding virtual 'Aos/Old' property is used.
		 * If not, the attribute value itself is used. Here's the list:
		 *  - MinDamage
		 *  - MaxDamage
		 *  - Speed
		 *  - HitSound
		 *  - MissSound
		 *  - StrRequirement, DexRequirement, IntRequirement
		 *  - WeaponType
		 *  - WeaponAnimation
		 *  - MaxRange
		 */

        #region Var declarations
        // Instance values. These values must are unique to each weapon.
        private WeaponDamageLevel m_DamageLevel;
        private WeaponAccuracyLevel m_AccuracyLevel;
        private WeaponDurabilityLevel m_DurabilityLevel;
        private WeaponQuality m_Quality;
        private MakersMark m_Crafter;
        private Poison m_Poison;
        private int m_PoisonCharges;
        private int m_PoisonSkill;
        private byte m_Corrosion;
        private int m_WaxCharges;
        private bool m_Identified;
        private int m_Hits;
        private int m_MaxHits;
        private SlayerName m_Slayer;
        private SkillMod m_SkillMod, m_MageMod;
        private CraftResource m_Resource;
        private int m_DamageAbsorbed;   // not serialized: used to determine how much damage was absorbed in the last hit (diagnostics)
        private byte m_ImbueLevel;      // store the imbue level in the item

        // Overridable values. These values are provided to override the defaults which get defined in the individual weapon scripts.
        private int m_StrReq, m_DexReq, m_IntReq;
        private int m_MinDamage, m_MaxDamage;
        private int m_HitSound, m_MissSound;
        private int m_Speed;
        private int m_MaxRange;
        private SkillName m_Skill;
        private WeaponType m_Type;
        private WeaponAnimation m_Animation;
        // private int m_DieRolls, m_DieMax, m_AddConstant;
        #endregion

        #region Virtual Properties
        public virtual WeaponAbility PrimaryAbility { get { return null; } }
        public virtual WeaponAbility SecondaryAbility { get { return null; } }

        public virtual int DefMaxRange { get { return 1; } }
        public virtual int DefHitSound { get { return 0; } }
        public virtual int DefMissSound { get { return 0; } }
        public virtual SkillName DefSkill { get { return SkillName.Swords; } }
        public virtual WeaponType DefType { get { return WeaponType.Slashing; } }
        public virtual WeaponAnimation DefAnimation { get { return WeaponAnimation.Slash1H; } }

        public virtual int AosStrengthReq { get { return 0; } }
        public virtual int AosDexterityReq { get { return 0; } }
        public virtual int AosIntelligenceReq { get { return 0; } }
        public virtual int AosMinDamage { get { return 0; } }
        public virtual int AosMaxDamage { get { return 0; } }
        public virtual int AosSpeed { get { return 0; } }
        public virtual int AosMaxRange { get { return DefMaxRange; } }
        public virtual int AosHitSound { get { return DefHitSound; } }
        public virtual int AosMissSound { get { return DefMissSound; } }
        public virtual SkillName AosSkill { get { return DefSkill; } }
        public virtual WeaponType AosType { get { return DefType; } }
        public virtual WeaponAnimation AosAnimation { get { return DefAnimation; } }

        public abstract int OldStrengthReq { get; }
        public virtual int OldDexterityReq { get { return 0; } }
        public virtual int OldIntelligenceReq { get { return 0; } }
        public virtual int OldMinDamage { get { return 0; } }
        public virtual int OldMaxDamage { get { return 0; } }
        public virtual int OldSpeed { get { return 0; } }
        public virtual int OldMaxRange { get { return DefMaxRange; } }
        public virtual int OldHitSound { get { return DefHitSound; } }
        public virtual int OldMissSound { get { return DefMissSound; } }
        public virtual SkillName OldSkill { get { return DefSkill; } }
        public virtual WeaponType OldType { get { return DefType; } }
        public virtual WeaponAnimation OldAnimation { get { return DefAnimation; } }

        public virtual int OldDieRolls { get { return 0; } }
        public virtual int OldDieMax { get { return 0; } }
        public virtual int OldAddConstant { get { return 0; } }

        public virtual int InitMinHits { get { return 0; } }
        public virtual int InitMaxHits { get { return 0; } }
        #endregion

        private static Memory MessageGiven = new Memory();

        #region Getters & Setters
        /*
		[CommandProperty( AccessLevel.GameMaster )]
		public AosAttributes Attributes
		{
			get{ return m_AosAttributes; }
			set{}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public AosWeaponAttributes WeaponAttributes
		{
			get{ return m_AosWeaponAttributes; }
			set{}
		}
		*/

        /*
		[CommandProperty( AccessLevel.GameMaster )]
		public bool Cursed
		{
			get{ return m_Cursed; }
			set{ m_Cursed = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Consecrated
		{
			get{ return m_Consecrated; }
			set{ m_Consecrated = value; }
		}
		*/

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Identified
        {
            get { return m_Identified; }
            set { m_Identified = value; InvalidateProperties(); }
        }
        // not serialized: used to determine how much damage was absorbed in the last hit (diagnostics)
        public int DamageAbsorbed { get { return m_DamageAbsorbed; } set { m_DamageAbsorbed = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitPoints
        {
            get { return m_Hits; }
            set
            {
                if (m_Hits == value)
                    return;

                m_Hits = value;
                InvalidateProperties();

                if (m_Hits <= (m_MaxHits / 10))
                {
                    if (Parent is IEntity parent)
                        parent.Notify(notification: Notification.WeaponStatus, this, 1061121);    // Your equipment is severely damaged.
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxHitPoints
        {
            get { return m_MaxHits; }
            set { m_MaxHits = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonCharges
        {
            get { return m_PoisonCharges; }
            set { m_PoisonCharges = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WaxCharges
        {
            get { return m_WaxCharges; }
            set { m_WaxCharges = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Poison Poison
        {
            get { return m_Poison; }
            set { m_Poison = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonSkill
        {
            get { return m_PoisonSkill; }
            set { m_PoisonSkill = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public byte Corrosion
        {
            get { return m_Corrosion; }
            set { m_Corrosion = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MakersMark Crafter
        {
            get { return m_Crafter; }
            set { m_Crafter = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SlayerName Slayer
        {
            get { return m_Slayer; }
            set { m_Slayer = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set
            {
                EventResourceSystem.CheckRegistry(this, false);

                UnscaleDurability();

                m_Resource = value;

                // 9/28/23, Yoar: Weapons aren't normally colorable in earlier eras
                if (Core.RuleSets.AllShards)
                    Hue = CraftResources.GetHue(m_Resource);

                InvalidateProperties();

                ScaleDurability();

                EventResourceSystem.CheckRegistry(this, true);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponQuality Quality
        {
            get { return m_Quality; }
            set
            {   // make sure we are getting legitimate values 
                if (!Enum.IsDefined(typeof(WeaponQuality), value))
                {
                    LogHelper logger = new LogHelper("illegitimateMagicGear.log", false);
                    logger.Log(LogType.Item, this, string.Format("{0}", new System.Diagnostics.StackTrace().ToString()));
                    logger.Finish();
                    value = WeaponQuality.Regular;
                }
                UnscaleDurability(); m_Quality = value; ScaleDurability(); InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponDamageLevel DamageLevel
        {
            get { return m_DamageLevel; }
            set
            {
                if (!Enum.IsDefined(typeof(WeaponDamageLevel), value))
                {
                    LogHelper logger = new LogHelper("illegitimateMagicGear.log", false);
                    logger.Log(LogType.Item, this, string.Format("{0}", new System.Diagnostics.StackTrace().ToString()));
                    logger.Finish();
                    value = WeaponDamageLevel.Regular;
                }
                m_DamageLevel = value; InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponDurabilityLevel DurabilityLevel
        {
            get { return m_DurabilityLevel; }
            set
            {
                if (!Enum.IsDefined(typeof(WeaponDurabilityLevel), value))
                {
                    LogHelper logger = new LogHelper("illegitimateMagicGear.log", false);
                    logger.Log(LogType.Item, this, string.Format("{0}", new System.Diagnostics.StackTrace().ToString()));
                    logger.Finish();
                    value = WeaponDurabilityLevel.Regular;
                }
                UnscaleDurability(); m_DurabilityLevel = value; InvalidateProperties(); ScaleDurability();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponAccuracyLevel AccuracyLevel
        {
            get
            {
                return m_AccuracyLevel;
            }
            set
            {
                if (!Enum.IsDefined(typeof(WeaponAccuracyLevel), value))
                {
                    LogHelper logger = new LogHelper("illegitimateMagicGear.log", false);
                    logger.Log(LogType.Item, this, string.Format("{0}", new System.Diagnostics.StackTrace().ToString()));
                    logger.Finish();
                    value = WeaponAccuracyLevel.Regular;
                }

                if (m_AccuracyLevel != value)
                {
                    m_AccuracyLevel = value;

                    if (UseSkillMod)
                    {
                        if (m_AccuracyLevel == WeaponAccuracyLevel.Regular)
                        {
                            if (m_SkillMod != null)
                                m_SkillMod.Remove();

                            m_SkillMod = null;
                        }
                        else if (m_SkillMod == null && Parent is Mobile)
                        {
                            m_SkillMod = new DefaultSkillMod(AccuracySkill, true, (int)m_AccuracyLevel * 5);
                            ((Mobile)Parent).AddSkillMod(m_SkillMod);
                        }
                        else if (m_SkillMod != null)
                        {
                            m_SkillMod.Value = (int)m_AccuracyLevel * 5;
                        }
                    }

                    InvalidateProperties();
                }
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int MaxRange
        {
            get { return (m_MaxRange == -1 ? Core.RuleSets.AOSRules() ? AosMaxRange : OldMaxRange : m_MaxRange); }
            set { m_MaxRange = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponAnimation Animation
        {
            get { return (m_Animation == (WeaponAnimation)(-1) ? Core.RuleSets.AOSRules() ? AosAnimation : OldAnimation : m_Animation); }
            set { m_Animation = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponType Type
        {
            get { return (m_Type == (WeaponType)(-1) ? Core.RuleSets.AOSRules() ? AosType : OldType : m_Type); }
            set { m_Type = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill
        {
            get { return (m_Skill == (SkillName)(-1) ? Core.RuleSets.AOSRules() ? AosSkill : OldSkill : m_Skill); }
            set { m_Skill = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HitSound
        {
            get { return (m_HitSound == -1 ? Core.RuleSets.AOSRules() ? AosHitSound : OldHitSound : m_HitSound); }
            set { m_HitSound = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MissSound
        {
            get { return (m_MissSound == -1 ? Core.RuleSets.AOSRules() ? AosMissSound : OldMissSound : m_MissSound); }
            set { m_MissSound = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int MinDamage
        {
            get { return (m_MinDamage == -1 ? Core.RuleSets.AOSRules() ? AosMinDamage : OldMinDamage : m_MinDamage); }
            set { m_MinDamage = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int MaxDamage
        {
            get { return (m_MaxDamage == -1 ? Core.RuleSets.AOSRules() ? AosMaxDamage : OldMaxDamage : m_MaxDamage); }
            set { m_MaxDamage = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int Speed
        {
            get { return (m_Speed == -1 ? Core.RuleSets.AOSRules() ? AosSpeed : OldSpeed : m_Speed); }
            set { m_Speed = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StrRequirement
        {
            get
            {

                if ((m_StrReq == -1 ? Core.RuleSets.AOSRules() ? AosStrengthReq : OldStrengthReq : m_StrReq) == 0)
                    if (this is not BaseWand)
                        // Wands actually do have an OldStrengthReq of 0
                        Console.WriteLine("Strength requirement for {0} is {1}", this, (m_StrReq == -1 ? Core.RuleSets.AOSRules() ? AosStrengthReq : OldStrengthReq : m_StrReq));

                return (m_StrReq == -1 ? Core.RuleSets.AOSRules() ? AosStrengthReq : OldStrengthReq : m_StrReq);
            }
            set { m_StrReq = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DexRequirement
        {
            get { return (m_DexReq == -1 ? Core.RuleSets.AOSRules() ? AosDexterityReq : OldDexterityReq : m_DexReq); }
            set { m_DexReq = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int IntRequirement
        {
            get { return (m_IntReq == -1 ? Core.RuleSets.AOSRules() ? AosIntelligenceReq : OldIntelligenceReq : m_IntReq); }
            set { m_IntReq = value; }
        }
        #endregion

        public void UnscaleDurability()
        {
            int scale = 100 + GetDurabilityBonus();

            m_Hits = (m_Hits * 100) / scale;
            m_MaxHits = (m_MaxHits * 100) / scale;
            InvalidateProperties();
        }

        public void ScaleDurability()
        {
            int scale = 100 + GetDurabilityBonus();

            m_Hits = (m_Hits * scale) / 100;
            m_MaxHits = (m_MaxHits * scale) / 100;
            InvalidateProperties();
        }

        public int GetDurabilityBonus()
        {
            int bonus = 0;

            if (m_Quality == WeaponQuality.Exceptional)
                bonus += 20;

#if false
            if (Core.RuleSets.AllShards) // custom resource benefits
            {
                switch (m_Resource)
                {
                    case CraftResource.DullCopper: bonus += 5; break;
                    case CraftResource.ShadowIron: bonus += 10; break;
                    case CraftResource.Copper: bonus += 15; break;
                    case CraftResource.Bronze: bonus += 20; break;
                    case CraftResource.Gold: bonus += 50; break;
                    case CraftResource.Agapite: bonus += 70; break;
                    case CraftResource.Verite: bonus += 100; break;
                    case CraftResource.Valorite: bonus += 120; break;

                    case CraftResource.SpinedLeather: bonus += 20; break;
                    case CraftResource.HornedLeather: bonus += 40; break;
                    case CraftResource.BarbedLeather: bonus += 60; break;
                }
            }
#endif

            switch (m_DurabilityLevel)
            {
                case WeaponDurabilityLevel.Durable: bonus += 20; break;
                case WeaponDurabilityLevel.Substantial: bonus += 50; break;
                case WeaponDurabilityLevel.Massive: bonus += 70; break;
                case WeaponDurabilityLevel.Fortified: bonus += 100; break;
                case WeaponDurabilityLevel.Indestructible: bonus += 120; break;
            }

            EventResourceSystem ers = EventResourceSystem.Find(m_Resource);

            if (ers != null && ers.Validate(this))
                bonus += (ers.DurabilityScalar - 100);

            /*
			if ( Core.AOS )
			{
				bonus += m_AosWeaponAttributes.DurabilityBonus;

				CraftResourceInfo resInfo = CraftResources.GetInfo( m_Resource );
				CraftAttributeInfo attrInfo = null;

				if ( resInfo != null )
					attrInfo = resInfo.AttributeInfo;

				if ( attrInfo != null )
					bonus += attrInfo.WeaponDurability;
			}
			*/

            return bonus;
        }

        public int GetLowerStatReq()
        {
            if (!Core.RuleSets.AOSRules())
                return 0;
            return 0;
            /*
						int v = m_AosWeaponAttributes.LowerStatReq;

						CraftResourceInfo info = CraftResources.GetInfo( m_Resource );

						if ( info != null )
						{
							CraftAttributeInfo attrInfo = info.AttributeInfo;

							if ( attrInfo != null )
								v += attrInfo.WeaponLowerRequirements;
						}

						if ( v > 100 )
							v = 100;

						return v;
			*/
        }

        public static void BlockEquip(Mobile m, TimeSpan duration)
        {
            if (m.BeginAction(typeof(BaseWeapon)))
                new ResetEquipTimer(m, duration).Start();
        }

        private class ResetEquipTimer : Timer
        {
            private Mobile m_Mobile;

            public ResetEquipTimer(Mobile m, TimeSpan duration)
                : base(duration)
            {
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                m_Mobile.EndAction(typeof(BaseWeapon));
            }
        }

        public override bool CheckConflictingLayer(Mobile m, Item item, Layer layer)
        {
            if (base.CheckConflictingLayer(m, item, layer))
                return true;

            if (this.Layer == Layer.TwoHanded && layer == Layer.OneHanded)
                return true;
            else if (this.Layer == Layer.OneHanded && layer == Layer.TwoHanded && !(item is BaseShield) && !(item is BaseEquipableLight))
                return true;

            return false;
        }

        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            if (!Ethics.Ethic.CheckTrade(from, to, newOwner, this))
                return false;

            return base.AllowSecureTrade(from, to, newOwner, accepted);
        }

        public override bool CanEquip(Mobile from)
        {
            if (!Ethics.Ethic.CheckEquip(from, this))
                return false;

            if (from.Dex < DexRequirement)
            {
                from.SendMessage("You are not nimble enough to equip that.");
                return false;
            }
            else if (from.Str < AOS.Scale(StrRequirement, 100 - GetLowerStatReq()))
            {
                from.SendLocalizedMessage(500213); // You are not strong enough to equip that.
                return false;
            }
            else if (from.Int < IntRequirement)
            {
                from.SendMessage("You are not smart enough to equip that.");
                return false;
            }
            else if (!from.CanBeginAction(typeof(BaseWeapon)))
            {
                return false;
            }
            else
            {
                return base.CanEquip(from);
            }
        }

        public virtual bool UseSkillMod { get { return !Core.RuleSets.AOSRules(); } }
        public virtual SkillName AccuracySkill { get { return SkillName.Tactics; } }

        #region EquipSoundID
        private int m_EquipSoundID = 0;
        [CommandProperty(AccessLevel.GameMaster)]
        public int EquipSound { get { return m_EquipSoundID; } set { m_EquipSoundID = value; } }
        public virtual int GetEquipSound()
        {
            if (m_EquipSoundID != 0)
                return m_EquipSoundID;

            return -1;
        }
        #endregion EquipSoundID
        public override bool OnEquip(Mobile from)
        {

            try
            {
                from.PlaySound(GetEquipSound());

                /*
				int strBonus = m_AosAttributes.BonusStr;
				int dexBonus = m_AosAttributes.BonusDex;
				int intBonus = m_AosAttributes.BonusInt;

				if ((strBonus != 0 || dexBonus != 0 || intBonus != 0))
				{
					Mobile m = from;

					string modName = this.Serial.ToString();

					if (strBonus != 0)
						m.AddStatMod(new StatMod(StatType.Str, modName + "Str", strBonus, TimeSpan.Zero));

					if (dexBonus != 0)
						m.AddStatMod(new StatMod(StatType.Dex, modName + "Dex", dexBonus, TimeSpan.Zero));

					if (intBonus != 0)
						m.AddStatMod(new StatMod(StatType.Int, modName + "Int", intBonus, TimeSpan.Zero));
				}
				*/

                from.NextCombatTime = DateTime.UtcNow + GetDelay(from);

                if (UseSkillMod && m_AccuracyLevel != WeaponAccuracyLevel.Regular)
                {
                    if (m_SkillMod != null)
                        m_SkillMod.Remove();

                    m_SkillMod = new DefaultSkillMod(AccuracySkill, true, (int)m_AccuracyLevel * 5);
                    from.AddSkillMod(m_SkillMod);
                }

                /*
				if (Core.AOS && m_AosWeaponAttributes.MageWeapon != 0)
				{
					if (m_MageMod != null)
						m_MageMod.Remove();

					m_MageMod = new DefaultSkillMod(SkillName.Magery, true, -m_AosWeaponAttributes.MageWeapon);
					from.AddSkillMod(m_MageMod);
				}
				*/
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                if (from is PlayerMobile)
                {
                    PlayerMobile pm = (PlayerMobile)from;
                    Console.WriteLine("Exception" + ex + from.Name);
                }
                else
                    Console.WriteLine("Exception" + ex);
            }

            return true;
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile)
            {
                ((Mobile)parent).CheckStatTimers();
                ((Mobile)parent).Delta(MobileDelta.WeaponDamage);
            }
        }

        public override void OnRemoved(object parent)
        {
            if (parent is Mobile)
            {
                Mobile m = (Mobile)parent;
                BaseWeapon weapon = m.Weapon as BaseWeapon;

                string modName = this.Serial.ToString();

                m.RemoveStatMod(modName + "Str");
                m.RemoveStatMod(modName + "Dex");
                m.RemoveStatMod(modName + "Int");

                if (weapon != null)
                    m.NextCombatTime = DateTime.UtcNow + weapon.GetDelay(m);

                if (UseSkillMod && m_SkillMod != null)
                {
                    m_SkillMod.Remove();
                    m_SkillMod = null;
                }

                if (m_MageMod != null)
                {
                    m_MageMod.Remove();
                    m_MageMod = null;
                }

                m.CheckStatTimers();

                m.Delta(MobileDelta.WeaponDamage);
            }
        }

        public virtual SkillName GetUsedSkill(Mobile m, bool checkSkillAttrs)
        {
            SkillName sk;

            /*
			if ( checkSkillAttrs && m_AosWeaponAttributes.UseBestSkill != 0 )
			{
				double swrd = m.Skills[SkillName.Swords].Value;
				double fenc = m.Skills[SkillName.Fencing].Value;
				double arch = m.Skills[SkillName.Archery].Value;
				double mcng = m.Skills[SkillName.Macing].Value;
				double val;

				sk = SkillName.Swords;
				val = swrd;

				if ( fenc > val ){ sk = SkillName.Fencing; val = fenc; }
				if ( arch > val ){ sk = SkillName.Archery; val = arch; }
				if ( mcng > val ){ sk = SkillName.Macing; val = mcng; }
			}
			else if ( m_AosWeaponAttributes.MageWeapon != 0 )
			{
				sk = SkillName.Magery;
			}
			else*/
            {
                sk = Skill;

                if (sk != SkillName.Wrestling && !m.Player && !m.Body.IsHuman && m.Skills[SkillName.Wrestling].Value > m.Skills[sk].Value)
                    sk = SkillName.Wrestling;
            }

            return sk;
        }

        public virtual double GetAttackSkillValue(Mobile attacker, Mobile defender)
        {
            return attacker.Skills[GetUsedSkill(attacker, true)].Value;
        }

        public virtual double GetDefendSkillValue(Mobile attacker, Mobile defender)
        {
            return defender.Skills[GetUsedSkill(defender, false)].Value;
        }

        public virtual bool CheckHit(Mobile attacker, Mobile defender)
        {
            BaseWeapon atkWeapon = attacker.Weapon as BaseWeapon;
            BaseWeapon defWeapon = defender.Weapon as BaseWeapon;

            Skill atkSkill = attacker.Skills[atkWeapon.Skill];
            Skill defSkill = defender.Skills[defWeapon.Skill];

            double atkValue = atkWeapon.GetAttackSkillValue(attacker, defender);
            double defValue = defWeapon.GetDefendSkillValue(attacker, defender);

            //attacker.CheckSkill( atkSkill.SkillName, defValue - 20.0, 120.0 );
            //defender.CheckSkill( defSkill.SkillName, atkValue - 20.0, 120.0 );

            double ourValue, theirValue;

            int bonus = GetHitChanceBonus();

            if (Core.RuleSets.AOSRules())
            {
                if (atkValue <= -20.0)
                    atkValue = -19.9;

                if (defValue <= -20.0)
                    defValue = -19.9;

                bonus += AosAttributes.GetValue(attacker, AosAttribute.AttackChance);

                //if ( Spells.Chivalry.DivineFurySpell.UnderEffect( attacker ) )
                //bonus += 10; // attacker gets 10% bonus when they're under divine fury

                ourValue = (atkValue + 20.0) * (100 + bonus);

                bonus = AosAttributes.GetValue(defender, AosAttribute.DefendChance);

                //if ( Spells.Chivalry.DivineFurySpell.UnderEffect( defender ) )
                //bonus -= 20; // defender loses 20% bonus when they're under divine fury

                double discordanceScalar = 0.0;

                if (SkillHandlers.Discordance.GetScalar(attacker, ref discordanceScalar))
                    bonus += (int)(discordanceScalar * 100);

                theirValue = (defValue + 20.0) * (100 + bonus);

                bonus = 0;
            }
            else
            {
                if (atkValue <= -50.0)
                    atkValue = -49.9;

                if (defValue <= -50.0)
                    defValue = -49.9;

                ourValue = (atkValue + 50.0);
                theirValue = (defValue + 50.0);
            }

            double chance;
            if (Core.RuleSets.SiegeStyleRules())
            {
                chance = ourValue / (theirValue * 2.0);
            }
            else
            {
                chance = ourValue / (theirValue * 1.8);
            }

            chance *= 1.0 + ((double)bonus / 100);

            if (Core.RuleSets.AOSRules() && chance < 0.02)
                chance = 0.02;

            WeaponAbility ability = WeaponAbility.GetCurrentAbility(attacker);

            if (ability != null)
                chance *= ability.AccuracyScalar;

            if (attacker is BaseCreature)
                chance *= ((BaseCreature)attacker).GetAccuracyScalar();

            return attacker.CheckSkill(atkSkill.SkillName, chance, contextObj: new object[2] { defender, null });

            //return ( chance >= Utility.RandomDouble() );
        }

        public virtual TimeSpan GetDelay(Mobile m)
        {
            int speed = this.Speed;

            if (speed == 0)
                return TimeSpan.FromHours(1.0);

            double delayInSeconds;

            if (Core.RuleSets.AOSRules())
            {
                int v = (m.Stam + 100) * speed;

                int bonus = AosAttributes.GetValue(m, AosAttribute.WeaponSpeed);

                //if ( Spells.Chivalry.DivineFurySpell.UnderEffect( m ) )
                //bonus += 10;

                double discordanceScalar = 0.0;

                if (SkillHandlers.Discordance.GetScalar(m, ref discordanceScalar))
                    bonus += (int)(discordanceScalar * 100);

                v += AOS.Scale(v, bonus);

                if (v <= 0)
                    v = 1;

                delayInSeconds = Math.Floor(40000.0 / v) * 0.5;
            }
            else
            {
                int v = (m.Stam + 100) * speed;

                if (v <= 0)
                    v = 1;

                delayInSeconds = 15000.0 / v;
            }

            return TimeSpan.FromSeconds(delayInSeconds);
        }

        public virtual TimeSpan OnSwing(Mobile attacker, Mobile defender)
        {
            bool canSwing = true;

            if (Core.RuleSets.AOSRules())
            {
                canSwing = (!attacker.Paralyzed && !attacker.Frozen);

                if (canSwing)
                {
                    Spell sp = attacker.Spell as Spell;

                    canSwing = (sp == null || !sp.IsCasting || !sp.BlocksMovement);
                }
            }

            if (canSwing && MLDryad.UnderEffect(attacker))
                canSwing = false;

            #region Dueling
            if (attacker is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)attacker;

                if (pm.DuelContext != null && !pm.DuelContext.CheckItemEquip(attacker, this))
                    canSwing = false;
            }
            #endregion

            if (canSwing && attacker.HarmfulCheck(defender))
            {
                attacker.DisruptiveAction();

                if (attacker.NetState != null)
                    attacker.Send(new Swing(0, attacker, defender));

                if (attacker is BaseCreature)
                {
                    BaseCreature bc = (BaseCreature)attacker;
                    WeaponAbility ab = bc.GetWeaponAbility();

                    if (ab != null)
                    {
                        if (bc.WeaponAbilityChance > Utility.RandomDouble())
                            WeaponAbility.SetCurrentAbility(bc, ab);
                        else
                            WeaponAbility.ClearCurrentAbility(bc);
                    }
                }

                if (CheckHit(attacker, defender) && CheckInterference(attacker, defender) == false)
                {
                    OnHit(attacker, defender);
                    attacker.OnHit(attacker, defender);     // 7/2/21, Adam: notify the mobile base class
                    defender.OnHit(attacker, defender);

                }
                else
                {
                    OnMiss(attacker, defender);
                    attacker.OnMiss(attacker, defender);     // 7/2/21, Adam: notify the mobile base class
                    defender.OnMiss(attacker, defender);
                }
            }

            return GetDelay(attacker);
        }

        public virtual int GetHitAttackSound(Mobile attacker, Mobile defender)
        {
            int sound = attacker.GetAttackSound();

            if (sound == -1)
                sound = HitSound;

            return sound;
        }

        public virtual int GetHitDefendSound(Mobile attacker, Mobile defender)
        {
            return defender.GetHurtSound();
        }

        public virtual int GetMissAttackSound(Mobile attacker, Mobile defender)
        {
            if (attacker.GetAttackSound() == -1)
                return MissSound;
            else
                return -1;
        }

        public virtual int GetMissDefendSound(Mobile attacker, Mobile defender)
        {
            return -1;
        }

        public virtual int AbsorbDamageAOS(Mobile attacker, Mobile defender, int damage)
        {
            double positionChance = Utility.RandomDouble();
            BaseArmor armor;

            if (positionChance < 0.07)
                armor = defender.NeckArmor as BaseArmor;
            else if (positionChance < 0.14)
                armor = defender.HandArmor as BaseArmor;
            else if (positionChance < 0.28)
                armor = defender.ArmsArmor as BaseArmor;
            else if (positionChance < 0.43)
                armor = defender.HeadArmor as BaseArmor;
            else if (positionChance < 0.65)
                armor = defender.LegsArmor as BaseArmor;
            else
                armor = defender.ChestArmor as BaseArmor;

            if (armor != null)
                armor.OnHit(this, damage); // call OnHit to lose durability

            if (defender.Player || defender.Body.IsHuman)
            {
                BaseShield shield = defender.FindItemOnLayer(Layer.TwoHanded) as BaseShield;

                bool blocked = false;

                if (shield != null)
                {
                    double chance = (defender.Skills[SkillName.Parry].Value * 0.0030);

                    blocked = defender.CheckSkill(SkillName.Parry, chance, contextObj: new object[2]);
                }
                else if (!(defender.Weapon is Fists) && !(defender.Weapon is BaseRanged))
                {
                    double chance = (defender.Skills[SkillName.Parry].Value * 0.0015);

                    blocked = (chance > Utility.RandomDouble()); // Only skillcheck if wielding a shield
                }

                if (blocked)
                {
                    defender.FixedEffect(0x37B9, 10, 16);
                    damage = 0;

                    if (shield != null)
                    {
                        double halfArmor = shield.ArmorRating / 2.0;
                        int absorbed = (int)(halfArmor + (halfArmor * Utility.RandomDouble()));

                        if (absorbed < 2)
                            absorbed = 2;

                        if (Type == WeaponType.Bashing)
                            shield.HitPoints -= absorbed / 2;
                        else
                            shield.HitPoints -= Utility.Random(2);
                    }
                }
            }

            return damage;
        }

        public virtual int AbsorbDamage(Mobile attacker, Mobile defender, int damage)
        {   // 8/30/21, Adam: I want to revisit the ordering of what's hit first.
            // If you are wearing a robe (OuterTorso) it will take damage After damage to something like a a shirt.
            //  It would seem that clothes worn on the outside (the robe in this case,) should take damage first.

            // we sill save this value and report the delta to the attacker for damage adsorbed tracking.
            int old_damage = damage;

            if (Core.RuleSets.AOSRules())
                return AbsorbDamageAOS(attacker, defender, damage);

            double chance = Utility.RandomDouble();
            Item itemHit;

            if (chance < 0.07)
            {
                // armor: Layer.Neck
                itemHit = defender.NeckArmor;
            }
            else if (chance < 0.14)
            {
                // armor/clothing: Layer.Gloves
                itemHit = defender.HandArmor;

                // armor/clothing: also hits shoes
                if (itemHit == null)
                    itemHit = defender.FindItemOnLayer(Layer.Shoes);
            }
            else if (chance < 0.28)
            {
                // armor:  Layer.Arms
                itemHit = defender.ArmsArmor;

                // clothing: hits anything with arms
                if (itemHit == null)
                    itemHit = defender.FindItemOnLayer(Layer.Shirt);
                if (itemHit == null)
                    itemHit = defender.FindItemOnLayer(Layer.OuterTorso);
                if (itemHit == null)
                    itemHit = defender.FindItemOnLayer(Layer.MiddleTorso);
            }
            else if (chance < 0.43)
            {
                // armor/clothing: Layer.Helm
                itemHit = defender.HeadArmor;
            }
            else if (chance < 0.65)
            {
                // armor: Layer.InnerLegs, Layer.Pants
                itemHit = defender.LegsArmor;

                // clothing: hits anything with potential leg area
                if (itemHit == null)
                    itemHit = defender.FindItemOnLayer(Layer.OuterLegs);
                if (itemHit == null)
                    itemHit = defender.FindItemOnLayer(Layer.OuterTorso);
                if (itemHit == null)
                    itemHit = defender.FindItemOnLayer(Layer.Cloak);
            }
            else
            {
                // armor: Layer.InnerTorso, Layer.Shirt
                itemHit = defender.ChestArmor;

                // clothing: square in the chest - inner and outter
                if (itemHit == null)
                    itemHit = defender.FindItemOnLayer(Layer.OuterTorso);
                if (itemHit == null)
                    itemHit = defender.FindItemOnLayer(Layer.MiddleTorso);
            }

            if (itemHit is BaseArmor)
                damage = (itemHit as BaseArmor).OnHit(this, damage);

            if (itemHit is BaseClothing)
                damage = (itemHit as BaseClothing).OnHit(this, damage);

            BaseShield shield = defender.FindItemOnLayer(Layer.TwoHanded) as BaseShield;
            if (shield != null)
                damage = shield.OnHit(this, damage);

            int virtualArmor = defender.VirtualArmor + defender.VirtualArmorMod;

            if (virtualArmor > 0)
            {
                double scalar;

                if (chance < 0.14)
                    scalar = 0.07;
                else if (chance < 0.28)
                    scalar = 0.14;
                else if (chance < 0.43)
                    scalar = 0.15;
                else if (chance < 0.65)
                    scalar = 0.22;
                else
                    scalar = 0.35;

                int from = (int)(virtualArmor * scalar) / 2;
                int to = (int)(virtualArmor * scalar);

                damage -= Utility.Random(from, (to - from) + 1);
            }

            // record how much damage was absorbed (diagnostics)
            DamageAbsorbed = old_damage - damage;

            return damage;
        }

        public virtual int GetPackInstinctBonus(Mobile attacker, Mobile defender)
        {
            if (attacker.Player || defender.Player)
                return 0;

            BaseCreature bc = attacker as BaseCreature;

            if (bc == null || bc.PackInstinct == PackInstinct.None || (!bc.Controlled && !bc.Summoned))
                return 0;

            Mobile master = bc.ControlMaster;

            if (master == null)
                master = bc.SummonMaster;

            if (master == null)
                return 0;

            int inPack = 1;

            IPooledEnumerable eable = defender.GetMobilesInRange(1);
            foreach (Mobile m in eable)
            {
                if (m != attacker && m is BaseCreature)
                {
                    BaseCreature tc = (BaseCreature)m;

                    if ((tc.PackInstinct & bc.PackInstinct) == 0 || (!tc.Controlled && !tc.Summoned))
                        continue;

                    Mobile theirMaster = tc.ControlMaster;

                    if (theirMaster == null)
                        theirMaster = tc.SummonMaster;

                    // adam: add a % chance based on loyality
                    if (master == theirMaster && tc.Combatant == defender && Utility.RandomChance((int)tc.LoyaltyValue))
                        ++inPack;
                }
            }
            eable.Free();

            // adam: scaled against your herding skill
            inPack = Math.Min((int)master.Skills.Herding.Base / 20, inPack);

            if (inPack >= 5)
                return 100;
            else if (inPack >= 4)
                return 75;
            else if (inPack >= 3)
                return 50;
            else if (inPack >= 2)
                return 25;

            return 0;
        }

        private static bool m_InDoubleStrike;

        public static bool InDoubleStrike
        {
            get { return m_InDoubleStrike; }
            set { m_InDoubleStrike = value; }
        }

        public byte ImbueLevel
        {
            get { return m_ImbueLevel; }
            set { m_ImbueLevel = value; }
        }

        public int GetSpiritSpeakBonus(int damage, Mobile attacker, Mobile defender)
        {
            // will always be false if not Core.AngelIsland so no need to condition it here
            if (SkillHandlers.SpiritSpeak.SlayerDamageCache.Recall(attacker))
            {   // okay, we have a slayer strike queued up
                Memory.ObjectMemory om = SkillHandlers.SpiritSpeak.SlayerDamageCache.Recall(attacker as object);
                if (om != null && om.Context is SkillHandlers.SpiritSpeak.SlayerDamageContext)
                {
                    SkillHandlers.SpiritSpeak.SlayerDamageContext sdc = om.Context as SkillHandlers.SpiritSpeak.SlayerDamageContext;
                    if (sdc.SlayerDamageHP > 0)
                    {
                        if (sdc.SlayerDamageHP == sdc.SlayerDamageHPMax)
                        {   // nice red bolt for max hit
                            defender.BoltEffect(0x4EA); // colored bolt does not show up on my 4.0.9b client 

                            // extra blood
                            for (int i = 0; i < 3; i++)
                            {
                                Point3D p = new Point3D(attacker.Location);
                                p.X += Utility.RandomMinMax(-1, 1);
                                p.Y += Utility.RandomMinMax(-1, 1);
                                new Blood(Utility.Random(0x122A, 5), TimeSpan.FromMinutes(15).TotalSeconds).MoveToWorld(p, attacker.Map);
                            }

                            // should repair their weapon?
                            if (attacker.Skills.SpiritSpeak.Value / 100.0 >= Utility.RandomDouble())
                            {
                                BaseWeapon atkWeapon = attacker.Weapon as BaseWeapon;
                                if (atkWeapon.HitPoints < atkWeapon.MaxHitPoints)
                                {
                                    atkWeapon.HitPoints++;

                                    if (atkWeapon.HitPoints < atkWeapon.MaxHitPoints)
                                        switch (Utility.Random(3))
                                        {
                                            case 0: attacker.SendMessage("The energy repairs some damage to your weapon"); break;
                                            case 1: attacker.SendMessage("Your weapon grows stronger."); break;
                                            case 2: attacker.SendMessage("Your weapon regains some of it's gleam."); break;
                                        }
                                    else
                                        attacker.SendMessage("Your weapon is like new.");
                                }
                            }
                        }
                        else
                        {
                            defender.BoltEffect(0);
                            //attacker.SendMessage("Your slayer strike was less than perfection.");
                        }
                        damage += (int)sdc.SlayerDamageHP;
                        sdc.SlayerDamageHP = 0;
                    }
                    // else we are waiting for the timer to expire and cleanup this object
                }
            }

            return damage;
        }

        public virtual void OnHit(Mobile attacker, Mobile defender)
        {
            PlaySwingAnimation(attacker);
            PlayHurtAnimation(defender);

            attacker.PlaySound(GetHitAttackSound(attacker, defender));
            defender.PlaySound(GetHitDefendSound(attacker, defender));
            int damage = ComputeDamage(attacker, defender);

            // Check Slayer weapon: slayers only work against evil
            CheckSlayerResult cs = CheckSlayers(attacker, defender);

            // good pets are not attackable, check to see if this pet is evil
            // 7/19/2023, Adam. Exception: If the attacker is not a player, slayers work against tames.
            bool good;
            if (!attacker.Player)
                good = false;
            else
                good =
                    (defender is BaseCreature &&                            // base creature
                    (defender as BaseCreature).ControlMaster != null &&     // controlled
                                                                            // 8/10/22, Adam: I don't think Core.RedsInTown matters here
                    !(defender as BaseCreature).ControlMaster.Red)          // and my master is not a murderer
                    ? true : false;

            if (cs != CheckSlayerResult.None && !good)
            {
                if (cs == CheckSlayerResult.Slayer)
                    defender.FixedEffect(0x37B9, 10, 5);

                if (Core.RuleSets.AngelIslandRules())
                {   // special Slayer Template
                    damage = (int)((double)damage * (2.0 + (attacker.Skills.SpiritSpeak.Value / 100.0)));
                    damage = GetSpiritSpeakBonus(damage, attacker, defender);
                }
                else
                    damage = (int)((double)damage * 2.0);
            }
            else if (cs == CheckSlayerResult.Slayer)
            {   // tell the player this weapon doesn't work against good pets, but don't spam
                if (MessageGiven.Recall(attacker) == false)
                {
                    MessageGiven.Remember(attacker, 45);
                    attacker.SendMessage("Your weapon is only attuned to destroy evil.");
                }
            }

            // resource slayer
            if (defender is BaseCreature bcd && bcd.SusceptibleTo == this.Resource)
            {
                defender.FixedEffect(0x37B9, 10, 5);
                damage = (int)((double)damage * 2.0);
            }

            #region Ethics
            // Give double damage to Heroes wielding a holy weapon against Evil and undead
            if (Core.OldEthics)
            {
                if (attacker.Hero && this.HolyBlade)
                {   // see if we are fighting an undead with our holy weapon
                    SlayerEntry defSlayer = SlayerGroup.GetEntryByName(SlayerName.Silver);
                    bool defenderIsUndead = defSlayer.Slays(defender);

                    // see if we are fighting an Evil Player
                    bool defenderIsEvil = defender.Player && defender.Evil;

                    if (defenderIsEvil || defenderIsUndead)
                    {   // double damage against evil players & undead
                        damage *= 2;
                    }
                }
            }
            #endregion

            int packInstinctBonus = 0;
            if (PublishInfo.Publish >= 16 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
                packInstinctBonus = GetPackInstinctBonus(attacker, defender);

            // adam: limit to PvM only (for now)
            if (packInstinctBonus != 0 && defender is BaseCreature)
            {   // adam: new code, taken from runuo 2.0
                int percentageBonus = Math.Min(packInstinctBonus, 300);
                damage = AOS.Scale(damage, 100 + percentageBonus);

                // old code
                // damage += AOS.Scale(damage, packInstinctBonus);
            }


            if (attacker is BaseCreature)
                ((BaseCreature)attacker).AlterMeleeDamageTo(defender, ref damage);

            if (defender is BaseCreature)
                ((BaseCreature)defender).AlterMeleeDamageFrom(attacker, ref damage);

            WeaponAbility weaponAblity = WeaponAbility.GetCurrentAbility(attacker);

            damage = AbsorbDamage(attacker, defender, damage);

            if (damage < 1)
                damage = 1;

            AddBlood(attacker, defender, damage);

            // adam: always returns phys=100;
            int phys, fire, cold, pois, nrgy;
            GetDamageTypes(attacker, out phys, out fire, out cold, out pois, out nrgy);


            //check if creature has some immunity to weapon being used, if so reduce damage according to immunity.
            if (defender is BaseCreature)
            {
                BaseWeapon atkWeapon = attacker.Weapon as BaseWeapon;
                ((BaseCreature)defender).CheckWeaponImmunity(atkWeapon, damage, out damage);
            }

            int damageGiven = damage;

            AOS.ArmorIgnore = (weaponAblity is ArmorIgnore);
            damageGiven = AOS.Damage(defender, attacker, damage, phys, fire, cold, pois, nrgy, this);
            AOS.ArmorIgnore = false;

            DoWeaponWear(attacker, defender, true);

            if (attacker is VampireBatFamiliar)
            {
                BaseCreature bc = (BaseCreature)attacker;
                Mobile caster = bc.ControlMaster;

                if (caster == null)
                    caster = bc.SummonMaster;

                if (caster != null && caster.Map == bc.Map && caster.InRange(bc, 2))
                    caster.Hits += damage;
                else
                    bc.Hits += damage;
            }

            if (attacker is BaseCreature)
                ((BaseCreature)attacker).OnGaveMeleeAttack(defender);

            if (defender is BaseCreature)
                ((BaseCreature)defender).OnGotMeleeAttack(attacker);

            //if (weaponAblity != null) Console.WriteLine("************ START MODIFY (weaponAblity) DAMAGE ************");
            if (weaponAblity != null)
                weaponAblity.OnHit(attacker, defender, damage);
            //if (weaponAblity != null) Console.WriteLine("(14) OnHit.weaponAblity modified damage to: {0}", damage);
            //if (weaponAblity != null) Console.WriteLine("************ END MODIFY (weaponAblity) DAMAGE ************");

            #region AI Style Special Move
            if (Core.RuleSets.AllShards)
            {
                TimeSpan AbilityDelay = TimeSpan.FromSeconds(20.0);

                Mobile atkr = attacker;

                Item weapon = atkr.FindItemOnLayer(Layer.TwoHanded);

                bool SpecialMoveOk = Core.RuleSets.UseToggleSpecialAbility() ?
                    atkr.Mana >= 15 && DateTime.UtcNow >= atkr.NextAbilityTime :
                    DoPub5Move(attacker, defender) && Engines.ConPVP.DuelContext.AllowSpecialAbility(attacker,
                        weapon is BaseBashing ? "crushing blow" :
                        weapon is BasePoleArm ? "concussion blow" :
                        "paralyzing blow", false);

                // HasAbilityReady will always be true for shards that don't UseToggleSpecialAbility
                if (atkr.HasAbilityReady && SpecialMoveOk)
                {
                    atkr.HasAbilityReady = false;

                    if (weapon is BaseBashing)
                    {
                        if (atkr.Mana < 15 && Core.RuleSets.UseToggleSpecialAbility())
                        {
                            atkr.LocalOverheadMessage(MessageType.Emote, 0x3B2, false, "Insufficient mana.");
                            atkr.HasAbilityReady = false;

                        }
                        else
                        {
                            double crushbonus = damage * 0.5;
                            int crush = (int)crushbonus;

                            if (Core.RuleSets.UseToggleSpecialAbility())
                                atkr.Mana -= 15;
                            atkr.HasAbilityReady = false;
                            atkr.NextAbilityTime = DateTime.UtcNow + AbilityDelay;
                            atkr.SendLocalizedMessage(1060090); // You have delivered a crushing blow!

                            defender.SendLocalizedMessage(1060091); // You take extra damage from the crushing attack!
                            defender.PlaySound(0x1E1);
                            defender.FixedParticles(0, 1, 0, 9946, EffectLayer.Head);
                            defender.Damage(crush, atkr, this);       // brings dmg total up to 150%

                            Effects.SendMovingParticles(new Entity(Serial.Zero, new Point3D(defender.X, defender.Y, defender.Z + 50), defender.Map), new Entity(Serial.Zero, new Point3D(defender.X, defender.Y, defender.Z + 20), defender.Map), 0xFB4, 1, 0, false, false, 0, 3, 9501, 1, 0, EffectLayer.Head, 0x100);

                        }

                    }
                    else if (weapon is BasePoleArm || weapon is BaseAxe)
                    {   // https://wiki.stratics.com/index.php?title=UO:Publish_Notes_from_2000-04-28
                        if (defender.GetStatMod("Concussion") != null)
                            ;// do nothing, too soon
                        else if (atkr.Mana < 15 && Core.RuleSets.UseToggleSpecialAbility())
                        {
                            atkr.LocalOverheadMessage(MessageType.Emote, 0x3B2, false, "Insufficient mana.");
                            atkr.HasAbilityReady = false;
                        }
                        else
                        {
                            if (Core.RuleSets.UseToggleSpecialAbility())
                                atkr.Mana -= 15;
                            atkr.SendLocalizedMessage(1060165); // You have delivered a concussion!
                            atkr.HasAbilityReady = false;
                            atkr.NextAbilityTime = DateTime.UtcNow + AbilityDelay;

                            defender.SendLocalizedMessage(1060166); // You feel disoriented!
                            defender.AddStatMod(new StatMod(StatType.Int, "Concussion", -(defender.RawInt / 2), TimeSpan.FromSeconds(30.0)));
                            defender.PlaySound(0x213);
                            defender.FixedParticles(0x377A, 1, 32, 9949, 1153, 0, EffectLayer.Head);
                        }
                    }
                    else if (weapon is BaseSpear)
                    {
                        if (defender.Frozen == true)
                            ;// do nothing, still Paralyzed
                        else if (atkr.Mana < 15 && Core.RuleSets.UseToggleSpecialAbility())
                        {
                            atkr.LocalOverheadMessage(MessageType.Emote, 0x3B2, false, "Insufficient mana.");
                            atkr.HasAbilityReady = false;
                        }
                        else if (defender.BlockDamage == true)
                        {   // can't para BlockDamage mobs
                            string text = $"* {defender.SafeName} shrugs off your feeble attempt *";
                            if (defender.CheckString(text))
                                defender.Emote(text);
                            atkr.HasAbilityReady = false;
                        }
                        else
                        {
                            defender.SendMessage("You receive a paralyzing blow!");
                            defender.Freeze(TimeSpan.FromSeconds(4.0)); // paras defender
                            defender.FixedEffect(0x376A, 9, 32);
                            defender.PlaySound(0x204);

                            atkr.HasAbilityReady = false;
                            atkr.NextAbilityTime = DateTime.UtcNow + AbilityDelay;
                            atkr.SendMessage("You deliver a paralyzing blow!");
                            if (Core.RuleSets.UseToggleSpecialAbility())
                                atkr.Mana -= 15;
                            atkr.HasAbilityReady = false;
                        }
                    }
                    else
                    {
                        atkr.HasAbilityReady = false;
                    }
                }
            }
            #endregion

            if (HitMagicEffect && m_MagicEffect != MagicItemEffect.None)
                MagicItems.OnHit(attacker, defender, this);
        }

        public virtual double GetAosDamage(Mobile attacker, int min, int random, double div)
        {
            double scale = 1.0;

            scale += attacker.Skills[SkillName.Inscribe].Value * 0.001;

            if (attacker.Player)
            {
                scale += attacker.Int * 0.001;
                scale += AosAttributes.GetValue(attacker, AosAttribute.SpellDamage) * 0.01;
            }

            int baseDamage = min + (int)(attacker.Skills[SkillName.EvalInt].Value / div);

            double damage = Utility.RandomMinMax(baseDamage, baseDamage + random);

            return damage * scale;
        }

        public virtual void DoMagicArrow(Mobile attacker, Mobile defender)
        {
            if (!attacker.CanBeHarmful(defender, false))
                return;

            attacker.DoHarmful(defender);

            double damage = GetAosDamage(attacker, 3, 1, 10.0);

            attacker.MovingParticles(defender, 0x36E4, 5, 0, false, true, 3006, 4006, 0);
            attacker.PlaySound(0x1E5);

            SpellHelper.Damage(TimeSpan.FromSeconds(1.0), defender, attacker, damage, 0, 100, 0, 0, 0);
        }

        public virtual void DoHarm(Mobile attacker, Mobile defender)
        {
            if (!attacker.CanBeHarmful(defender, false))
                return;

            attacker.DoHarmful(defender);

            double damage = GetAosDamage(attacker, 6, 3, 6.5);

            if (!defender.InRange(attacker, 2))
                damage *= 0.25; // 1/4 damage at > 2 tile range
            else if (!defender.InRange(attacker, 1))
                damage *= 0.50; // 1/2 damage at 2 tile range

            defender.FixedParticles(0x374A, 10, 30, 5013, 1153, 2, EffectLayer.Waist);
            defender.PlaySound(0x0FC);

            SpellHelper.Damage(TimeSpan.Zero, defender, attacker, damage, 0, 0, 100, 0, 0);
        }

        public virtual void DoFireball(Mobile attacker, Mobile defender)
        {
            if (!attacker.CanBeHarmful(defender, false))
                return;

            attacker.DoHarmful(defender);

            double damage = GetAosDamage(attacker, 6, 3, 5.5);

            attacker.MovingParticles(defender, 0x36D4, 7, 0, false, true, 9502, 4019, 0x160);
            attacker.PlaySound(0x15E);

            SpellHelper.Damage(TimeSpan.FromSeconds(1.0), defender, attacker, damage, 0, 100, 0, 0, 0);
        }

        public virtual void DoLightning(Mobile attacker, Mobile defender)
        {
            if (!attacker.CanBeHarmful(defender, false))
                return;

            attacker.DoHarmful(defender);

            double damage = GetAosDamage(attacker, 6, 3, 5.0);

            defender.BoltEffect(0);

            SpellHelper.Damage(TimeSpan.Zero, defender, attacker, damage, 0, 0, 0, 0, 100);
        }

        public virtual void DoDispel(Mobile attacker, Mobile defender)
        {
            bool dispellable = false;

            if (defender is BaseCreature)
                dispellable = ((BaseCreature)defender).Summoned && !((BaseCreature)defender).IsAnimatedDead;

            if (!dispellable)
                return;

            if (!attacker.CanBeHarmful(defender, false))
                return;

            attacker.DoHarmful(defender);

            Spells.Spell sp = new Spells.Sixth.DispelSpell(attacker, null);

            if (sp.CheckResisted(defender))
            {
                defender.FixedEffect(0x3779, 10, 20);
            }
            else
            {
                Effects.SendLocationParticles(EffectItem.Create(defender.Location, defender.Map, EffectItem.DefaultDuration), 0x3728, 8, 20, 5042);
                Effects.PlaySound(defender, defender.Map, 0x201);

                defender.Delete();
            }
        }

        public virtual void DoAreaAttack(Mobile from, int sound, int hue, int phys, int fire, int cold, int pois, int nrgy)
        {
            Map map = from.Map;

            if (map == null)
                return;

            ArrayList list = new ArrayList();

            IPooledEnumerable eable = from.GetMobilesInRange(10);
            foreach (Mobile m in eable)
            {
                if (from != m && SpellHelper.ValidIndirectTarget(from, m) && from.CanBeHarmful(m, false) && from.InLOS(m))
                    list.Add(m);
            }
            eable.Free();

            if (list.Count == 0)
                return;

            Effects.PlaySound(from.Location, map, sound);

            // TODO: What is the damage calculation?

            for (int i = 0; i < list.Count; ++i)
            {
                Mobile m = (Mobile)list[i];

                double scalar = (11 - from.GetDistanceToSqrt(m)) / 10;

                if (scalar > 1.0)
                    scalar = 1.0;
                else if (scalar < 0.0)
                    continue;

                from.DoHarmful(m, true);
                m.FixedEffect(0x3779, 1, 15, hue, 0);
                AOS.Damage(m, from, (int)(GetBaseDamage(from, m) * scalar), phys, fire, cold, pois, nrgy, this);
            }
        }

        public virtual CheckSlayerResult CheckSlayers(Mobile attacker, Mobile defender)
        {
            BaseWeapon atkWeapon = attacker.Weapon as BaseWeapon;
            SlayerEntry atkSlayer = SlayerGroup.GetEntryByName(atkWeapon.Slayer);

            // BaseCreature now supports a Slayer property. Useful for testing (no production uses atm,) and has the effect that 
            //  Holy Water has on players (but does not expire.)
            if (atkSlayer == null && attacker is BaseCreature bc)
                atkSlayer = SlayerGroup.GetEntryByName(bc.Slayer);

            if (atkSlayer != null && atkSlayer.Slays(defender))
                return CheckSlayerResult.Slayer;

            if (HolyWater.Slays(attacker, defender))
                return CheckSlayerResult.Slayer;

            if (CoveBrew.Slays(attacker, defender))
                return CheckSlayerResult.Slayer;

            BaseWeapon defWeapon = defender.Weapon as BaseWeapon;
            SlayerEntry defSlayer = SlayerGroup.GetEntryByName(defWeapon.Slayer);

            if (defSlayer != null && defSlayer.Group.Opposition.Super.Slays(attacker))
                return CheckSlayerResult.Opposition;

            if (HolyWater.OppositionSuperSlays(attacker, defender))
                return CheckSlayerResult.Opposition;

            return CheckSlayerResult.None;
        }

        public virtual void AddBlood(Mobile attacker, Mobile defender, int damage)
        {
            if (damage <= 2)
                return;

            Direction d = defender.GetDirectionTo(attacker);

            int maxCount = damage / 15;

            if (maxCount < 1)
                maxCount = 1;
            else if (maxCount > 4)
                maxCount = 4;

            for (int i = 0; i < Utility.Random(1, maxCount); ++i)
            {
                int x = defender.X;
                int y = defender.Y;

                switch (d)
                {
                    case Direction.North:
                        x += Utility.Random(-1, 3);
                        y += Utility.Random(2);
                        break;
                    case Direction.East:
                        y += Utility.Random(-1, 3);
                        x += Utility.Random(-1, 2);
                        break;
                    case Direction.West:
                        y += Utility.Random(-1, 3);
                        x += Utility.Random(2);
                        break;
                    case Direction.South:
                        x += Utility.Random(-1, 3);
                        y += Utility.Random(-1, 2);
                        break;
                    case Direction.Up:
                        x += Utility.Random(2);
                        y += Utility.Random(2);
                        break;
                    case Direction.Down:
                        x += Utility.Random(-1, 2);
                        y += Utility.Random(-1, 2);
                        break;
                    case Direction.Left:
                        x += Utility.Random(2);
                        y += Utility.Random(-1, 2);
                        break;
                    case Direction.Right:
                        x += Utility.Random(-1, 2);
                        y += Utility.Random(2);
                        break;
                }

                new Blood().MoveToWorld(new Point3D(x, y, defender.Z), defender.Map);
            }
        }

        public virtual void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy)
        {
            /*
			if ( wielder is BaseCreature )
			{
				BaseCreature bc = (BaseCreature)wielder;

				phys = bc.PhysicalDamage;
				fire = bc.FireDamage;
				cold = bc.ColdDamage;
				pois = bc.PoisonDamage;
				nrgy = bc.EnergyDamage;
			}
			else
			*/
            {
                /*
				CraftResourceInfo resInfo = CraftResources.GetInfo( m_Resource );

				if ( resInfo != null )
				{
					CraftAttributeInfo attrInfo = resInfo.AttributeInfo;

					if ( attrInfo != null )
					{
						fire = attrInfo.WeaponFireDamage;
						cold = attrInfo.WeaponColdDamage;
						pois = attrInfo.WeaponPoisonDamage;
						nrgy = attrInfo.WeaponEnergyDamage;
						phys = 100 - fire - cold - pois - nrgy;
						return;
					}
				}
				*/

                phys = 100;
                fire = 0;
                cold = 0;
                pois = 0;
                nrgy = 0;
            }
        }

        public virtual void OnMiss(Mobile attacker, Mobile defender)
        {
            PlaySwingAnimation(attacker);
            attacker.PlaySound(GetMissAttackSound(attacker, defender));
            defender.PlaySound(GetMissDefendSound(attacker, defender));

            WeaponAbility ability = WeaponAbility.GetCurrentAbility(attacker);

            if (ability != null)
                ability.OnMiss(attacker, defender);

            //Only ranged weapons are corroded on misses too.
            if (this.Type == WeaponType.Ranged)
            {
                DoWeaponWear(attacker, defender, false);
            }
        }

        public virtual void GetBaseDamageRange(Mobile attacker, out int min, out int max)
        {
            if (attacker is BaseCreature)
            {
                BaseCreature c = (BaseCreature)attacker;

                if (c.DamageMin >= 0)
                {
                    min = c.DamageMin;
                    max = c.DamageMax;
                    return;
                }

                if (this is Fists && !attacker.Body.IsHuman)
                {
                    min = attacker.Str / 28;
                    max = attacker.Str / 28;
                    return;
                }
            }

            min = MinDamage;
            max = MaxDamage;
        }

        public virtual double GetBaseDamage(Mobile attacker, Mobile defender)
        {
            int min, max;
            double damage = 0;
            //only do this for mobs set to not use human weapon damage.
            if ((attacker is BaseCreature) && ((BaseCreature)attacker).UsesHumanWeapons == false)
            {
                GetBaseDamageRange(attacker, out min, out max);
                damage = Utility.RandomMinMax(min, max);
            }
            else
            {
                for (int i = 1; i <= OldDieRolls; i++)
                    damage += Utility.RandomMinMax(1, OldDieMax);

                damage += OldAddConstant;
            }

            return damage;

            //	return Utility.RandomMinMax( min, max );
        }
        public virtual double GetBonus(double value, double scalar, double threshold, double offset)
        {
            double bonus = value * scalar;

            if (value >= threshold)
                bonus += offset;

            return bonus / 100;
        }

        public virtual int GetHitChanceBonus()
        {
            if (!Core.RuleSets.AOSRules())
                return 0;

            int bonus = 0;

            switch (m_AccuracyLevel)
            {
                case WeaponAccuracyLevel.Accurate: bonus += 02; break;
                case WeaponAccuracyLevel.Surpassingly: bonus += 04; break;
                case WeaponAccuracyLevel.Eminently: bonus += 06; break;
                case WeaponAccuracyLevel.Exceedingly: bonus += 08; break;
                case WeaponAccuracyLevel.Supremely: bonus += 10; break;
            }

            return bonus;
        }

        public virtual int GetDamageBonus()
        {
            int bonus = VirtualDamageBonus;

            switch (m_Quality)
            {
                case WeaponQuality.Low: bonus -= 20; break;
                case WeaponQuality.Exceptional: bonus += 20; break;
            }

            switch (m_DamageLevel)
            {
                case WeaponDamageLevel.Ruin: bonus += 15; break;
                case WeaponDamageLevel.Might: bonus += 20; break;
                case WeaponDamageLevel.Force: bonus += 25; break;
                case WeaponDamageLevel.Power: bonus += 30; break;
                case WeaponDamageLevel.Vanquishing: bonus += 35; break;
            }

            return bonus;
        }

        public virtual void GetStatusDamage(Mobile from, out int min, out int max)
        {
            int baseMin, baseMax;

            GetBaseDamageRange(from, out baseMin, out baseMax);

            if (Core.RuleSets.AOSRules())
            {
                min = (int)ScaleDamageAOS(from, baseMin, false, false);
                max = (int)ScaleDamageAOS(from, baseMax, false, false);
            }
            else
            {
                min = (int)ScaleDamageOld(from, baseMin, false, false);
                max = (int)ScaleDamageOld(from, baseMax, false, false);
            }

            if (min < 1)
                min = 1;

            if (max < 1)
                max = 1;
        }

        public virtual double ScaleDamageAOS(Mobile attacker, double damage, bool checkSkills, bool checkAbility)
        {
            if (checkSkills)
            {
                attacker.CheckSkill(SkillName.Tactics, 0.0, 100.0, contextObj: new object[2]); // Passively check tactics for gain
                attacker.CheckSkill(SkillName.Anatomy, 0.0, 100.0, contextObj: new object[2]); // Passively check Anatomy for gain

                if (Type == WeaponType.Axe)
                    attacker.CheckSkill(SkillName.Lumberjacking, 0.0, 100.0, contextObj: new object[2]); // Passively check Lumberjacking for gain
            }

            double strengthBonus = GetBonus(attacker.Str, 0.300, 100.0, 5.00);
            double anatomyBonus = GetBonus(attacker.Skills[SkillName.Anatomy].Value, 0.500, 100.0, 5.00);
            double tacticsBonus = GetBonus(attacker.Skills[SkillName.Tactics].Value, 0.625, 100.0, 6.25);
            double lumberBonus = GetBonus(attacker.Skills[SkillName.Lumberjacking].Value, 0.200, 100.0, 10.00);

            if (Type != WeaponType.Axe)
                lumberBonus = 0.0;

            double totalBonus = strengthBonus + anatomyBonus + tacticsBonus + lumberBonus + ((double)(GetDamageBonus() + AosAttributes.GetValue(attacker, AosAttribute.WeaponDamage)) / 100);

            //if ( TransformationSpell.UnderTransformation( attacker, typeof( HorrificBeastSpell ) ) )
            //totalBonus += 0.25;

            //if ( Spells.Chivalry.DivineFurySpell.UnderEffect( attacker ) )
            //totalBonus += 0.1;

            double discordanceScalar = 0.0;

            if (SkillHandlers.Discordance.GetScalar(attacker, ref discordanceScalar))
                totalBonus += discordanceScalar * 2;

            damage += (damage * totalBonus);

            WeaponAbility a = WeaponAbility.GetCurrentAbility(attacker);

            if (checkAbility && a != null)
                damage *= a.DamageScalar;

            return damage;
        }

        public virtual int VirtualDamageBonus { get { return 0; } }

        public virtual int ComputeDamageAOS(Mobile attacker, Mobile defender)
        {
            return (int)ScaleDamageAOS(attacker, GetBaseDamage(attacker, defender), true, true);
        }

        private int QualityLookup(WeaponQuality quality)
        {
            switch (quality)
            {
                case WeaponQuality.Low: return (int)WeaponQuality.Low;
                case WeaponQuality.Regular: return (int)WeaponQuality.Regular;
                case WeaponQuality.Exceptional: return (int)WeaponQuality.Exceptional;
                default: return (int)WeaponQuality.Exceptional;
            }
        }

        public virtual double ScaleDamageOld(Mobile attacker, double damage, bool checkSkills, bool checkAbility)
        {
            double baseDamage = damage;

            if (checkSkills)
            {
                attacker.CheckSkill(SkillName.Tactics, 0.0, 100.0, contextObj: new object[2]); // Passively check tactics for gain
                attacker.CheckSkill(SkillName.Anatomy, 0.0, 100.0, contextObj: new object[2]); // Passively check Anatomy for gain

                if (Type == WeaponType.Axe)
                    attacker.CheckSkill(SkillName.Lumberjacking, 0.0, 100.0, contextObj: new object[2]); // Passively check Lumberjacking for gain
            }

            /* Compute tactics modifier
			 * :   0.0 = 50% loss
			 * :  50.0 = unchanged
			 * : 100.0 = 50% bonus
			 */
            double tacticsBonus = (attacker.Skills[SkillName.Tactics].Value - 50.0) / 100.0;

            /* Compute strength modifier
			 * : 1% bonus for every 5 strength
			 */
            int tempStr = (attacker.STRBonusCap > 0 && attacker.Str > attacker.STRBonusCap) ? attacker.STRBonusCap : attacker.Str;
            double strBonus = (tempStr / 5.0) / 100.0;

            /* Compute anatomy modifier
			 * : 1% bonus for every 5 points of anatomy
			 * : +10% bonus at Grandmaster or higher
			 */
            double anatomyValue = attacker.Skills[SkillName.Anatomy].Value;
            double anatomyBonus = (anatomyValue / 5.0) / 100.0;

            if (anatomyValue >= 100.0)
                anatomyBonus += 0.1;

            /* Compute lumberjacking bonus 
			 */
            double lumberBonus;

            if (Type == WeaponType.Axe)
            {
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
                {   // AI style bonus 
                    // The bonus is 20% of the lumberjack skill
                    /* Compute lumberjacking bonus
					 * : 1% bonus for every 5 points of lumberjacking
					 * : +10% bonus at Grandmaster or higher
					 */
                    double lumberValue = attacker.Skills[SkillName.Lumberjacking].Value;
                    lumberBonus = (lumberValue / 5.0) / 100.0;
                }
                else if (PublishInfo.Publish >= 13)
                {   // publish 13
                    // Lumberjack Damage Bonus
                    // The lumberjack skill will now add a damage bonus to axe weapons using the same formula as described for the anatomy damage bonus.
                    // The bonus is 20% of the lumberjack skill until 99.9 skill and 30% for 100 skill.
                    /* Compute lumberjacking bonus
					 * : 1% bonus for every 5 points of lumberjacking
					 * : +10% bonus at Grandmaster or higher
					 */
                    double lumberValue = attacker.Skills[SkillName.Lumberjacking].Value;

                    lumberBonus = (lumberValue / 5.0) / 100.0;

                    if (lumberValue >= 100.0)
                        lumberBonus += 0.1;
                }
                else if (PublishInfo.Publish >= 5 && PublishInfo.Publish < 13)
                {   // publish 5
                    // Lumberjacking and Axes
                    // The lumberjacking skill will provide a bonus to damage when the player is using one of the following axes. 
                    //	The higher the characters lumberjacking skill the more damage they will do up to a 25% bonus for 99.9 lumberjacking. 
                    //	At Grandmaster lumberjacking, the damage bonus is 35%.
                    /* Compute lumberjacking bonus
					 * : 1% bonus for every 4 points of lumberjacking
					 * : +10% bonus at Grandmaster or higher
					 */
                    double lumberValue = attacker.Skills[SkillName.Lumberjacking].Value;

                    lumberBonus = (lumberValue / 4.0) / 100.0;

                    if (lumberValue >= 100.0)
                        lumberBonus += 0.1;
                }
                else
                    lumberBonus = 0.0;
            }
            else
            {
                lumberBonus = 0.0;
            }

            // New quality bonus:
#if false
            //double qualityBonus = ((int)m_Quality - 1) * 0.2;
            // Adam: Cap quality bonus at Exceptional
            double qualityBonus = (QualityLookup(m_Quality) - 1) * 0.2;
#else
            double qualityBonus = 0.0;
#endif

            // Apply bonuses
            damage += (damage * tacticsBonus) + (damage * strBonus) + (damage * anatomyBonus) + (damage * lumberBonus) + (damage * qualityBonus) + ((damage * VirtualDamageBonus) / 100);

            // Old quality bonus:
#if true
            /* Apply quality offset
			 * : Low         : -4
			 * : Regular     :  0
			 * : Exceptional : +4
			 * https://web.archive.org/web/20010801151717fw_/http://uo.stratics.com/content/arms-armor/arms.shtml
			 */
            damage += ((int)m_Quality - 1) * 4.0;
#endif

            /* Apply damage level offset
			 * : Regular : 0
			 * : Ruin    : 1
			 * : Might   : 3
			 * : Force   : 5
			 * : Power   : 7
			 * : Vanq    : 9
			 * https://web.archive.org/web/20010801154352fw_/http://uo.stratics.com/content/arms-armor/magicarmsarmor.shtml
			 */
            if (m_DamageLevel != WeaponDamageLevel.Regular)
                damage += (2.0 * (int)m_DamageLevel) - 1.0;

            EventResourceSystem ers = EventResourceSystem.Find(m_Resource);

            if (ers != null && ers.Validate(this))
                damage += ers.DamageBonus;

            // Halve the computed damage and return
            // 7/13/21, Adam: This was a bug, and has been fixed.
            //  See the caller of this function where we correctly scale the damage based on what you're fighting.
            // damage /= 2.0;

            WeaponAbility a = WeaponAbility.GetCurrentAbility(attacker);

            if (checkAbility && a != null)
                damage *= a.DamageScalar;

            return (int)damage;
        }

        public virtual int ComputeDamage(Mobile attacker, Mobile defender)
        {
            if (Core.RuleSets.AOSRules())
                return ComputeDamageAOS(attacker, defender);

            int damage = (int)ScaleDamageOld(attacker, GetBaseDamage(attacker, defender), true, true);

            // pre-AOS, halve damage if the defender is a player or the attacker is not a player
            if (PublishInfo.Publish < 16)
                if (defender is PlayerMobile || !(attacker is PlayerMobile))
                    damage = (int)(damage / 2.0);

            return damage;
        }

        public virtual void PlayHurtAnimation(Mobile from)
        {
            int action;
            int frames;

            switch (from.Body.Type)
            {
                case BodyType.Sea:
                case BodyType.Animal:
                    {
                        action = 7;
                        frames = 5;
                        break;
                    }
                case BodyType.Monster:
                    {
                        action = 10;
                        frames = 4;
                        break;
                    }
                case BodyType.Human:
                    {
                        action = 20;
                        frames = 5;
                        break;
                    }
                default: return;
            }

            if (from.Mounted)
                return;

            from.Animate(action, frames, 1, true, false, 0);
        }

        public virtual void PlaySwingAnimation(Mobile from)
        {
            int action;

            switch (from.Body.Type)
            {
                case BodyType.Sea:
                case BodyType.Animal:
                    {
                        action = Utility.Random(5, 2);
                        break;
                    }
                case BodyType.Monster:
                    {
                        switch (Animation)
                        {
                            default:
                            case WeaponAnimation.Wrestle:
                            case WeaponAnimation.Bash1H:
                            case WeaponAnimation.Pierce1H:
                            case WeaponAnimation.Slash1H:
                            case WeaponAnimation.Bash2H:
                            case WeaponAnimation.Pierce2H:
                            case WeaponAnimation.Slash2H: action = Utility.Random(4, 3); break;
                            case WeaponAnimation.ShootBow: return; // 7
                            case WeaponAnimation.ShootXBow: return; // 8
                        }

                        break;
                    }
                case BodyType.Human:
                    {
                        if (!from.Mounted)
                        {
                            action = (int)Animation;
                        }
                        else
                        {
                            switch (Animation)
                            {
                                default:
                                case WeaponAnimation.Wrestle:
                                case WeaponAnimation.Bash1H:
                                case WeaponAnimation.Pierce1H:
                                case WeaponAnimation.Slash1H: action = 26; break;
                                case WeaponAnimation.Bash2H:
                                case WeaponAnimation.Pierce2H:
                                case WeaponAnimation.Slash2H: action = 29; break;
                                case WeaponAnimation.ShootBow: action = 27; break;
                                case WeaponAnimation.ShootXBow: action = 28; break;
                            }
                        }

                        break;
                    }
                default: return;
            }

            from.Animate(action, 7, 1, true, false, 0);
        }

        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
        {
            if (setIf)
                flags |= toSet;
        }

        private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet)
        {
            return ((flags & toGet) != 0);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)14); // version

            SaveFlag flags = SaveFlag.None;

            SetSaveFlag(ref flags, SaveFlag.DamageLevel, m_DamageLevel != WeaponDamageLevel.Regular);
            SetSaveFlag(ref flags, SaveFlag.AccuracyLevel, m_AccuracyLevel != WeaponAccuracyLevel.Regular);
            SetSaveFlag(ref flags, SaveFlag.DurabilityLevel, m_DurabilityLevel != WeaponDurabilityLevel.Regular);
            SetSaveFlag(ref flags, SaveFlag.Quality, m_Quality != WeaponQuality.Regular);
            SetSaveFlag(ref flags, SaveFlag.Hits, m_Hits != 0);
            SetSaveFlag(ref flags, SaveFlag.MaxHits, m_MaxHits != 0);
            SetSaveFlag(ref flags, SaveFlag.Slayer, m_Slayer != SlayerName.None);
            SetSaveFlag(ref flags, SaveFlag.Poison, m_Poison != null);
            SetSaveFlag(ref flags, SaveFlag.PoisonCharges, m_PoisonCharges != 0);
            SetSaveFlag(ref flags, SaveFlag.Crafter, !m_Crafter.IsEmpty);
            SetSaveFlag(ref flags, SaveFlag.Identified, m_Identified != false);
            SetSaveFlag(ref flags, SaveFlag.StrReq, m_StrReq != -1);
            SetSaveFlag(ref flags, SaveFlag.DexReq, m_DexReq != -1);
            SetSaveFlag(ref flags, SaveFlag.IntReq, m_IntReq != -1);
            SetSaveFlag(ref flags, SaveFlag.MinDamage, m_MinDamage != -1);
            SetSaveFlag(ref flags, SaveFlag.MaxDamage, m_MaxDamage != -1);
            SetSaveFlag(ref flags, SaveFlag.HitSound, m_HitSound != -1);
            SetSaveFlag(ref flags, SaveFlag.MissSound, m_MissSound != -1);
            SetSaveFlag(ref flags, SaveFlag.Speed, m_Speed != -1);
            SetSaveFlag(ref flags, SaveFlag.MaxRange, m_MaxRange != -1);
            SetSaveFlag(ref flags, SaveFlag.Skill, m_Skill != (SkillName)(-1));
            SetSaveFlag(ref flags, SaveFlag.Type, m_Type != (WeaponType)(-1));
            SetSaveFlag(ref flags, SaveFlag.Animation, m_Animation != (WeaponAnimation)(-1));
            SetSaveFlag(ref flags, SaveFlag.Resource, m_Resource != CraftResource.Iron);
            SetSaveFlag(ref flags, SaveFlag.xAttributes, false); // turned off in version 8
            SetSaveFlag(ref flags, SaveFlag.EquipSound, m_Slayer != SlayerName.None);
            SetSaveFlag(ref flags, SaveFlag.xWeaponAttributes, false); // turned off in version 8
            //			SetSaveFlag( ref flags, SaveFlag.OldDieRolls,		OldDieRolls != -1 );
            //			SetSaveFlag( ref flags, SaveFlag.OldDieMax,			OldDieMax != -1 );
            //			SetSaveFlag( ref flags, SaveFlag.OldAddConstant,	OldAddConstant != -1 );
            SetSaveFlag(ref flags, SaveFlag.WaxCharges, m_WaxCharges != 0);
            SetSaveFlag(ref flags, SaveFlag.MagicEffect, m_MagicEffect != MagicItemEffect.None);
            SetSaveFlag(ref flags, SaveFlag.MagicCharges, m_MagicCharges != 0);
            SetSaveFlag(ref flags, SaveFlag.Corrosion, m_Corrosion != 0);
            SetSaveFlag(ref flags, SaveFlag.PoisonSkill, m_PoisonSkill != 0);

            writer.Write((ulong)flags);

            writer.Write(m_ImbueLevel);

            if (GetSaveFlag(flags, SaveFlag.PoisonSkill))
                writer.WriteEncodedInt(m_PoisonSkill);

            if (GetSaveFlag(flags, SaveFlag.Corrosion))
                writer.Write((byte)m_Corrosion);

            if (GetSaveFlag(flags, SaveFlag.MagicEffect))
                writer.Write((sbyte)m_MagicEffect);

            if (GetSaveFlag(flags, SaveFlag.MagicCharges))
                writer.Write((int)m_MagicCharges);

            if (GetSaveFlag(flags, SaveFlag.WaxCharges))
                writer.Write((int)m_WaxCharges);

            if (GetSaveFlag(flags, SaveFlag.EquipSound))
                writer.Write((int)m_EquipSoundID);

            if (GetSaveFlag(flags, SaveFlag.DamageLevel))
                writer.Write((int)m_DamageLevel);

            if (GetSaveFlag(flags, SaveFlag.AccuracyLevel))
                writer.Write((int)m_AccuracyLevel);

            if (GetSaveFlag(flags, SaveFlag.DurabilityLevel))
                writer.Write((int)m_DurabilityLevel);

            if (GetSaveFlag(flags, SaveFlag.Quality))
                writer.Write((int)m_Quality);

            if (GetSaveFlag(flags, SaveFlag.Hits))
                writer.Write((int)m_Hits);

            if (GetSaveFlag(flags, SaveFlag.MaxHits))
                writer.Write((int)m_MaxHits);

            if (GetSaveFlag(flags, SaveFlag.Slayer))
                writer.Write((int)m_Slayer);

            if (GetSaveFlag(flags, SaveFlag.Poison))
                Poison.Serialize(m_Poison, writer);

            if (GetSaveFlag(flags, SaveFlag.PoisonCharges))
                writer.Write((int)m_PoisonCharges);

            if (GetSaveFlag(flags, SaveFlag.Crafter))
                m_Crafter.Serialize(writer);

            if (GetSaveFlag(flags, SaveFlag.StrReq))
                writer.Write((int)m_StrReq);

            if (GetSaveFlag(flags, SaveFlag.DexReq))
                writer.Write((int)m_DexReq);

            if (GetSaveFlag(flags, SaveFlag.IntReq))
                writer.Write((int)m_IntReq);

            if (GetSaveFlag(flags, SaveFlag.MinDamage))
                writer.Write((int)m_MinDamage);

            if (GetSaveFlag(flags, SaveFlag.MaxDamage))
                writer.Write((int)m_MaxDamage);

            if (GetSaveFlag(flags, SaveFlag.HitSound))
                writer.Write((int)m_HitSound);

            if (GetSaveFlag(flags, SaveFlag.MissSound))
                writer.Write((int)m_MissSound);

            if (GetSaveFlag(flags, SaveFlag.Speed))
                writer.Write((int)m_Speed);

            if (GetSaveFlag(flags, SaveFlag.MaxRange))
                writer.Write((int)m_MaxRange);

            if (GetSaveFlag(flags, SaveFlag.Skill))
                writer.Write((int)m_Skill);

            if (GetSaveFlag(flags, SaveFlag.Type))
                writer.Write((int)m_Type);

            if (GetSaveFlag(flags, SaveFlag.Animation))
                writer.Write((int)m_Animation);

            if (GetSaveFlag(flags, SaveFlag.Resource))
                writer.Write((int)m_Resource);

            // turned off in version 8
            //if ( GetSaveFlag( flags, SaveFlag.xAttributes ) )
            //m_AosAttributes.Serialize( writer );

            //if ( GetSaveFlag( flags, SaveFlag.xWeaponAttributes ) )
            //m_AosWeaponAttributes.Serialize( writer );

            //			if ( GetSaveFlag( flags, SaveFlag.OldDieRolls ) )
            //				writer.Write( OldDieRolls );
            //
            //			if ( GetSaveFlag( flags, SaveFlag.OldDieMax ) )
            //				writer.Write( OldDieMax );
            //
            //			if ( GetSaveFlag( flags, SaveFlag.OldAddConstant ) )
            //				writer.Write( OldAddConstant );
        }

        [Flags]
        private enum SaveFlag : ulong
        {
            None = 0x00000000,
            DamageLevel = 0x00000001,
            AccuracyLevel = 0x00000002,
            DurabilityLevel = 0x00000004,
            Quality = 0x00000008,
            Hits = 0x00000010,
            MaxHits = 0x00000020,
            Slayer = 0x00000040,
            Poison = 0x00000080,
            PoisonCharges = 0x00000100,
            Crafter = 0x00000200,
            Identified = 0x00000400,
            StrReq = 0x00000800,
            DexReq = 0x00001000,
            IntReq = 0x00002000,
            MinDamage = 0x00004000,
            MaxDamage = 0x00008000,
            HitSound = 0x00010000,
            MissSound = 0x00020000,
            Speed = 0x00040000,
            MaxRange = 0x00080000,
            Skill = 0x00100000,
            Type = 0x00200000,
            Animation = 0x00400000,
            Resource = 0x00800000,
            xAttributes = 0x01000000,
            xWeaponAttributes = 0x02000000,
            EquipSound = 0x04000000,        // 10/28/21. Adam: used for slayer weapons
            OldDieRolls = 0x08000000,
            OldDieMax = 0x10000000,
            OldAddConstant = 0x20000000,
            WaxCharges = 0x40000000,
            MagicEffect = 0x80000000,
            MagicCharges = 0x100000000,
            Corrosion = 0x200000000,
            PoisonSkill = 0x400000000,
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            SaveFlag flags;

            if (version >= 12)
                flags = (SaveFlag)reader.ReadULong();
            else
                flags = (SaveFlag)reader.ReadInt();

            switch (version)
            {
                case 14:
                    {
                        m_ImbueLevel = reader.ReadByte();
                        goto case 13;
                    }
                case 13:
                case 12:
                    {
                        if (GetSaveFlag(flags, SaveFlag.PoisonSkill))
                            m_PoisonSkill = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Corrosion))
                            m_Corrosion = reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.MagicEffect))
                            m_MagicEffect = (MagicItemEffect)reader.ReadSByte();

                        if (GetSaveFlag(flags, SaveFlag.MagicCharges))
                        {
                            if (version >= 13)
                                m_MagicCharges = reader.ReadInt();
                            else
                                m_MagicCharges = reader.ReadUShort();
                        }

                        goto case 11;
                    }
                case 11:
                case 10:
                    {
                        if (GetSaveFlag(flags, SaveFlag.WaxCharges))
                            m_WaxCharges = reader.ReadInt();
                        goto case 9;
                    }
                case 9:
                    {
                        if (GetSaveFlag(flags, SaveFlag.EquipSound))
                            m_EquipSoundID = reader.ReadInt();
                        goto case 8;
                    }
                case 8:
                    {
                        // turnned off AOS attributes
                        goto case 7;
                    }
                case 7:
                    {
                        goto case 6;
                    }
                case 6:
                    {
                        goto case 5;
                    }
                case 5:
                    {
                        if (GetSaveFlag(flags, SaveFlag.DamageLevel))
                            m_DamageLevel = (WeaponDamageLevel)reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.AccuracyLevel))
                            m_AccuracyLevel = (WeaponAccuracyLevel)reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.DurabilityLevel))
                            m_DurabilityLevel = (WeaponDurabilityLevel)reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.Quality))
                            m_Quality = (WeaponQuality)reader.ReadInt();
                        else
                            m_Quality = WeaponQuality.Regular;

                        if (m_Quality > WeaponQuality.Exceptional)
                            m_Quality = WeaponQuality.Exceptional;

                        if (GetSaveFlag(flags, SaveFlag.Hits))
                            m_Hits = reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.MaxHits))
                            m_MaxHits = reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.Slayer))
                            m_Slayer = (SlayerName)reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.Poison))
                            m_Poison = Poison.Deserialize(reader);

                        if (GetSaveFlag(flags, SaveFlag.PoisonCharges))
                            m_PoisonCharges = reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.Crafter))
                        {
                            if (version >= 11)
                                m_Crafter.Deserialize(reader);
                            else
                                m_Crafter = reader.ReadMobile();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Identified))
                            m_Identified = (version >= 6 || reader.ReadBool());

                        if (GetSaveFlag(flags, SaveFlag.StrReq))
                            m_StrReq = reader.ReadInt();
                        else
                            m_StrReq = -1;

                        if (GetSaveFlag(flags, SaveFlag.DexReq))
                            m_DexReq = reader.ReadInt();
                        else
                            m_DexReq = -1;

                        if (GetSaveFlag(flags, SaveFlag.IntReq))
                            m_IntReq = reader.ReadInt();
                        else
                            m_IntReq = -1;

                        if (GetSaveFlag(flags, SaveFlag.MinDamage))
                            m_MinDamage = reader.ReadInt();
                        else
                            m_MinDamage = -1;

                        if (GetSaveFlag(flags, SaveFlag.MaxDamage))
                            m_MaxDamage = reader.ReadInt();
                        else
                            m_MaxDamage = -1;

                        if (GetSaveFlag(flags, SaveFlag.HitSound))
                            m_HitSound = reader.ReadInt();
                        else
                            m_HitSound = -1;

                        if (GetSaveFlag(flags, SaveFlag.MissSound))
                            m_MissSound = reader.ReadInt();
                        else
                            m_MissSound = -1;

                        if (GetSaveFlag(flags, SaveFlag.Speed))
                            m_Speed = reader.ReadInt();
                        else
                            m_Speed = -1;

                        if (GetSaveFlag(flags, SaveFlag.MaxRange))
                            m_MaxRange = reader.ReadInt();
                        else
                            m_MaxRange = -1;

                        if (GetSaveFlag(flags, SaveFlag.Skill))
                            m_Skill = (SkillName)reader.ReadInt();
                        else
                            m_Skill = (SkillName)(-1);

                        if (GetSaveFlag(flags, SaveFlag.Type))
                            m_Type = (WeaponType)reader.ReadInt();
                        else
                            m_Type = (WeaponType)(-1);

                        if (GetSaveFlag(flags, SaveFlag.Animation))
                            m_Animation = (WeaponAnimation)reader.ReadInt();
                        else
                            m_Animation = (WeaponAnimation)(-1);

                        if (GetSaveFlag(flags, SaveFlag.Resource))
                            m_Resource = (CraftResource)reader.ReadInt();
                        else
                            m_Resource = CraftResource.Iron;

                        // obsolete from version 8 on
                        if (version < 8)
                        {
                            AosAttributes dmy_AosAttributes;
                            AosWeaponAttributes dmy_AosWeaponAttributes;

                            if (GetSaveFlag(flags, SaveFlag.xAttributes))
                                dmy_AosAttributes = new AosAttributes(this, reader);
                            //else
                            //dmy_AosAttributes = new AosAttributes( this );

                            if (GetSaveFlag(flags, SaveFlag.xWeaponAttributes))
                                dmy_AosWeaponAttributes = new AosWeaponAttributes(this, reader);
                            //else
                            //dmy_AosWeaponAttributes = new AosWeaponAttributes( this );
                        }

                        if (UseSkillMod && m_AccuracyLevel != WeaponAccuracyLevel.Regular && Parent is Mobile)
                        {
                            m_SkillMod = new DefaultSkillMod(AccuracySkill, true, (int)m_AccuracyLevel * 5);
                            ((Mobile)Parent).AddSkillMod(m_SkillMod);
                        }

                        /*if ( Core.AOS && m_AosWeaponAttributes.MageWeapon != 0 && Parent is Mobile )
						{
							m_MageMod = new DefaultSkillMod( SkillName.Magery, true, -m_AosWeaponAttributes.MageWeapon );
							((Mobile)Parent).AddSkillMod( m_MageMod );
						}*/

                        // erl: made obsolete by PlayerCrafted in version 9
                        //if (version < 9)
                        //{
                        //if ( GetSaveFlag( flags, SaveFlag.SaveHue ) )
                        //PlayerCrafted = true;
                        //}

                        break;
                    }
                case 4:
                    {
                        m_Slayer = (SlayerName)reader.ReadInt();

                        goto case 3;
                    }
                case 3:
                    {
                        m_StrReq = reader.ReadInt();
                        m_DexReq = reader.ReadInt();
                        m_IntReq = reader.ReadInt();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Identified = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        m_MaxRange = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version == 0)
                            m_MaxRange = 1; // default

                        if (version < 5)
                        {
                            m_Resource = CraftResource.Iron;
                            //m_AosAttributes = new AosAttributes( this );
                            //m_AosWeaponAttributes = new AosWeaponAttributes( this );
                        }

                        m_MinDamage = reader.ReadInt();
                        m_MaxDamage = reader.ReadInt();

                        m_Speed = reader.ReadInt();

                        m_HitSound = reader.ReadInt();
                        m_MissSound = reader.ReadInt();

                        m_Skill = (SkillName)reader.ReadInt();
                        m_Type = (WeaponType)reader.ReadInt();
                        m_Animation = (WeaponAnimation)reader.ReadInt();
                        m_DamageLevel = (WeaponDamageLevel)reader.ReadInt();
                        m_AccuracyLevel = (WeaponAccuracyLevel)reader.ReadInt();
                        m_DurabilityLevel = (WeaponDurabilityLevel)reader.ReadInt();
                        m_Quality = (WeaponQuality)reader.ReadInt();

                        m_Crafter = reader.ReadMobile();

                        m_Poison = Poison.Deserialize(reader);
                        m_PoisonCharges = reader.ReadInt();

                        if (m_StrReq == OldStrengthReq)
                            m_StrReq = -1;

                        if (m_DexReq == OldDexterityReq)
                            m_DexReq = -1;

                        if (m_IntReq == OldIntelligenceReq)
                            m_IntReq = -1;

                        if (m_MinDamage == OldMinDamage)
                            m_MinDamage = -1;

                        if (m_MaxDamage == OldMaxDamage)
                            m_MaxDamage = -1;

                        if (m_HitSound == OldHitSound)
                            m_HitSound = -1;

                        if (m_MissSound == OldMissSound)
                            m_MissSound = -1;

                        if (m_Speed == OldSpeed)
                            m_Speed = -1;

                        if (m_MaxRange == OldMaxRange)
                            m_MaxRange = -1;

                        if (m_Skill == OldSkill)
                            m_Skill = (SkillName)(-1);

                        if (m_Type == OldType)
                            m_Type = (WeaponType)(-1);

                        if (m_Animation == OldAnimation)
                            m_Animation = (WeaponAnimation)(-1);

                        if (UseSkillMod && m_AccuracyLevel != WeaponAccuracyLevel.Regular && Parent is Mobile)
                        {
                            m_SkillMod = new DefaultSkillMod(AccuracySkill, true, (int)m_AccuracyLevel * 5);
                            ((Mobile)Parent).AddSkillMod(m_SkillMod);
                        }

                        break;
                    }
            }
            /*
						int strBonus = m_AosAttributes.BonusStr;
						int dexBonus = m_AosAttributes.BonusDex;
						int intBonus = m_AosAttributes.BonusInt;

						if ( this.Parent is Mobile && (strBonus != 0 || dexBonus != 0 || intBonus != 0) )
						{
							Mobile m = (Mobile)this.Parent;

							string modName = this.Serial.ToString();

							if ( strBonus != 0 )
								m.AddStatMod( new StatMod( StatType.Str, modName + "Str", strBonus, TimeSpan.Zero ) );

							if ( dexBonus != 0 )
								m.AddStatMod( new StatMod( StatType.Dex, modName + "Dex", dexBonus, TimeSpan.Zero ) );

							if ( intBonus != 0 )
								m.AddStatMod( new StatMod( StatType.Int, modName + "Int", intBonus, TimeSpan.Zero ) );
						}
			*/
            if (Parent is Mobile)
                ((Mobile)Parent).CheckStatTimers();

            if (m_Hits <= 0 && m_MaxHits <= 0)
            {
                m_Hits = m_MaxHits = Utility.RandomMinMax(InitMinHits, InitMaxHits);
            }

            //if ( version < 6 )
            //PlayerCrafted = true; // we don't know, so, assume it's crafted

            EventResourceSystem.CheckRegistry(this, true);

            BaseCraftableItem.PatchResourceHue(this, m_Resource);
        }

        public override void OnAfterDelete()
        {
            EventResourceSystem.CheckRegistry(this, false);
        }

        public BaseWeapon(int itemID)
            : base(itemID)
        {
            Layer = (Layer)ItemData.Quality;

            m_Quality = WeaponQuality.Regular;
            m_StrReq = -1;
            m_DexReq = -1;
            m_IntReq = -1;
            m_MinDamage = -1;
            m_MaxDamage = -1;
            m_HitSound = -1;
            m_MissSound = -1;
            m_Speed = -1;
            m_MaxRange = -1;
            m_Skill = (SkillName)(-1);
            m_Type = (WeaponType)(-1);
            m_Animation = (WeaponAnimation)(-1);

            m_Hits = m_MaxHits = Utility.RandomMinMax(InitMinHits, InitMaxHits);

            m_Resource = CraftResource.Iron;

            //m_AosAttributes = new AosAttributes( this );
            //m_AosWeaponAttributes = new AosWeaponAttributes( this );
        }

        public BaseWeapon(Serial serial)
            : base(serial)
        {
        }

        private string GetNameString()
        {
            string name = this.Name;

            if (name == null)
                name = String.Format("#{0}", LabelNumber);

            return name;
        }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get { return base.Hue; }
            set { base.Hue = value; InvalidateProperties(); }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            int oreType;

            if (Hue == 0)
            {
                oreType = 0;
            }
            else
            {
                switch (m_Resource)
                {
                    case CraftResource.DullCopper: oreType = 1053108; break; // dull copper
                    case CraftResource.ShadowIron: oreType = 1053107; break; // shadow iron
                    case CraftResource.Copper: oreType = 1053106; break; // copper
                    case CraftResource.Bronze: oreType = 1053105; break; // bronze
                    case CraftResource.Gold: oreType = 1053104; break; // golden
                    case CraftResource.Agapite: oreType = 1053103; break; // agapite
                    case CraftResource.Verite: oreType = 1053102; break; // verite
                    case CraftResource.Valorite: oreType = 1053101; break; // valorite
                    case CraftResource.SpinedLeather: oreType = 1061118; break; // spined
                    case CraftResource.HornedLeather: oreType = 1061117; break; // horned
                    case CraftResource.BarbedLeather: oreType = 1061116; break; // barbed
                    case CraftResource.RedScales: oreType = 1060814; break; // red
                    case CraftResource.YellowScales: oreType = 1060818; break; // yellow
                    case CraftResource.BlackScales: oreType = 1060820; break; // black
                    case CraftResource.GreenScales: oreType = 1060819; break; // green
                    case CraftResource.WhiteScales: oreType = 1060821; break; // white
                    case CraftResource.BlueScales: oreType = 1060815; break; // blue
                    default: oreType = 0; break;
                }
            }

            if (oreType != 0)
                list.Add(1053099, "#{0}\t{1}", oreType, GetNameString()); // ~1_oretype~ ~2_armortype~
            else if (Name == null)
                list.Add(LabelNumber);
            else
                list.Add(Name);
        }

        public override bool AllowEquipedCast(Mobile from)
        {
            if (base.AllowEquipedCast(from))
                return true;

            //return ( m_AosAttributes.SpellChanneling != 0 );
            return false;
        }

        public virtual int ArtifactRarity
        {
            get { return 0; }
        }

        public virtual int GetLuckBonus()
        {
            CraftResourceInfo resInfo = CraftResources.GetInfo(m_Resource);

            if (resInfo == null)
                return 0;

            CraftAttributeInfo attrInfo = resInfo.AttributeInfo;

            if (attrInfo == null)
                return 0;

            return attrInfo.WeaponLuck;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Crafter != null)
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~

            #region Factions
            if (m_FactionState != null)
                list.Add(1041350); // faction item
            #endregion

            if (m_Quality == WeaponQuality.Exceptional)
                list.Add(1060636); // exceptional


            if (ArtifactRarity > 0)
                list.Add(1061078, ArtifactRarity.ToString()); // artifact rarity ~1_val~

            if (this is IUsesRemaining && ((IUsesRemaining)this).ShowUsesRemaining)
                list.Add(1060584, ((IUsesRemaining)this).UsesRemaining.ToString()); // uses remaining: ~1_val~

            if (m_Poison != null && m_PoisonCharges > 0)
                list.Add(1062412 + m_Poison.Level, m_PoisonCharges.ToString());

            if (m_Slayer != SlayerName.None)
                list.Add(1017383 + (int)m_Slayer);


            int prop;
            /*
						if ( (prop = m_AosWeaponAttributes.UseBestSkill) != 0 )
							list.Add( 1060400 ); // use best weapon skill

						if ( (prop = (GetDamageBonus() + m_AosAttributes.WeaponDamage)) != 0 )
							list.Add( 1060401, prop.ToString() ); // damage increase ~1_val~%

						if ( (prop = m_AosAttributes.DefendChance) != 0 )
							list.Add( 1060408, prop.ToString() ); // defense chance increase ~1_val~%

						if ( (prop = m_AosAttributes.BonusDex) != 0 )
							list.Add( 1060409, prop.ToString() ); // dexterity bonus ~1_val~

						if ( (prop = m_AosAttributes.EnhancePotions) != 0 )
							list.Add( 1060411, prop.ToString() ); // enhance potions ~1_val~%

						if ( (prop = m_AosAttributes.CastRecovery) != 0 )
							list.Add( 1060412, prop.ToString() ); // faster cast recovery ~1_val~

						if ( (prop = m_AosAttributes.CastSpeed) != 0 )
							list.Add( 1060413, prop.ToString() ); // faster casting ~1_val~

						if ( (prop = (GetHitChanceBonus() + m_AosAttributes.AttackChance)) != 0 )
							list.Add( 1060415, prop.ToString() ); // hit chance increase ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitColdArea) != 0 )
							list.Add( 1060416, prop.ToString() ); // hit cold area ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitDispel) != 0 )
							list.Add( 1060417, prop.ToString() ); // hit dispel ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitEnergyArea) != 0 )
							list.Add( 1060418, prop.ToString() ); // hit energy area ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitFireArea) != 0 )
							list.Add( 1060419, prop.ToString() ); // hit fire area ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitFireball) != 0 )
							list.Add( 1060420, prop.ToString() ); // hit fireball ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitHarm) != 0 )
							list.Add( 1060421, prop.ToString() ); // hit harm ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitLeechHits) != 0 )
							list.Add( 1060422, prop.ToString() ); // hit life leech ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitLightning) != 0 )
							list.Add( 1060423, prop.ToString() ); // hit lightning ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitLowerAttack) != 0 )
							list.Add( 1060424, prop.ToString() ); // hit lower attack ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitLowerDefend) != 0 )
							list.Add( 1060425, prop.ToString() ); // hit lower defense ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitMagicArrow) != 0 )
							list.Add( 1060426, prop.ToString() ); // hit magic arrow ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitLeechMana) != 0 )
							list.Add( 1060427, prop.ToString() ); // hit mana leech ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitPhysicalArea) != 0 )
							list.Add( 1060428, prop.ToString() ); // hit physical area ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitPoisonArea) != 0 )
							list.Add( 1060429, prop.ToString() ); // hit poison area ~1_val~%

						if ( (prop = m_AosWeaponAttributes.HitLeechStam) != 0 )
							list.Add( 1060430, prop.ToString() ); // hit stamina leech ~1_val~%

						if ( (prop = m_AosAttributes.BonusHits) != 0 )
							list.Add( 1060431, prop.ToString() ); // hit point increase ~1_val~

						if ( (prop = m_AosAttributes.BonusInt) != 0 )
							list.Add( 1060432, prop.ToString() ); // intelligence bonus ~1_val~

						if ( (prop = m_AosAttributes.LowerManaCost) != 0 )
							list.Add( 1060433, prop.ToString() ); // lower mana cost ~1_val~%

						if ( (prop = m_AosAttributes.LowerRegCost) != 0 )
							list.Add( 1060434, prop.ToString() ); // lower reagent cost ~1_val~%
			*/
            if ((prop = GetLowerStatReq()) != 0)
                list.Add(1060435, prop.ToString()); // lower requirements ~1_val~%

            //if ( (prop = (GetLuckBonus() + m_AosAttributes.Luck)) != 0 )
            //list.Add( 1060436, prop.ToString() ); // luck ~1_val~
            /*
						if ( (prop = m_AosWeaponAttributes.MageWeapon) != 0 )
							list.Add( 1060438, prop.ToString() ); // mage weapon -~1_val~ skill

						if ( (prop = m_AosAttributes.BonusMana) != 0 )
							list.Add( 1060439, prop.ToString() ); // mana increase ~1_val~

						if ( (prop = m_AosAttributes.RegenMana) != 0 )
							list.Add( 1060440, prop.ToString() ); // mana regeneration ~1_val~

						if ( (prop = m_AosAttributes.ReflectPhysical) != 0 )
							list.Add( 1060442, prop.ToString() ); // reflect physical damage ~1_val~%

						if ( (prop = m_AosAttributes.RegenStam) != 0 )
							list.Add( 1060443, prop.ToString() ); // stamina regeneration ~1_val~

						if ( (prop = m_AosAttributes.RegenHits) != 0 )
							list.Add( 1060444, prop.ToString() ); // hit point regeneration ~1_val~

						if ( (prop = m_AosWeaponAttributes.SelfRepair) != 0 )
							list.Add( 1060450, prop.ToString() ); // self repair ~1_val~

						if ( (prop = m_AosAttributes.SpellChanneling) != 0 )
							list.Add( 1060482 ); // spell channeling

						if ( (prop = m_AosAttributes.SpellDamage) != 0 )
							list.Add( 1060483, prop.ToString() ); // spell damage increase ~1_val~%

						if ( (prop = m_AosAttributes.BonusStam) != 0 )
							list.Add( 1060484, prop.ToString() ); // stamina increase ~1_val~

						if ( (prop = m_AosAttributes.BonusStr) != 0 )
							list.Add( 1060485, prop.ToString() ); // strength bonus ~1_val~

						if ( (prop = m_AosAttributes.WeaponSpeed) != 0 )
							list.Add( 1060486, prop.ToString() ); // swing speed increase ~1_val~%
			*/
            /*
						int phys, fire, cold, pois, nrgy;

						GetDamageTypes( null, out phys, out fire, out cold, out pois, out nrgy );

						if ( phys != 0 )
							list.Add( 1060403, phys.ToString() ); // physical damage ~1_val~%

						if ( fire != 0 )
							list.Add( 1060405, fire.ToString() ); // fire damage ~1_val~%

						if ( cold != 0 )
							list.Add( 1060404, cold.ToString() ); // cold damage ~1_val~%

						if ( pois != 0 )
							list.Add( 1060406, pois.ToString() ); // poison damage ~1_val~%

						if ( nrgy != 0 )
							list.Add( 1060407, nrgy.ToString() ); // energy damage ~1_val~%
			*/
            list.Add(1061168, "{0}\t{1}", MinDamage.ToString(), MaxDamage.ToString()); // weapon damage ~1_val~ - ~2_val~
            list.Add(1061167, Speed.ToString()); // weapon speed ~1_val~

            if (MaxRange > 1)
                list.Add(1061169, MaxRange.ToString()); // range ~1_val~

            int strReq = AOS.Scale(StrRequirement, 100 - GetLowerStatReq());

            if (strReq > 0)
                list.Add(1061170, strReq.ToString()); // strength requirement ~1_val~

            if (Layer == Layer.TwoHanded)
                list.Add(1061171); // two-handed weapon
            else
                list.Add(1061824); // one-handed weapon

            //if ( m_AosWeaponAttributes.UseBestSkill == 0 && m_AosWeaponAttributes.MageWeapon == 0 )
            {
                switch (Skill)
                {
                    case SkillName.Swords: list.Add(1061172); break; // skill required: swordsmanship
                    case SkillName.Macing: list.Add(1061173); break; // skill required: mace fighting
                    case SkillName.Fencing: list.Add(1061174); break; // skill required: fencing
                    case SkillName.Archery: list.Add(1061175); break; // skill required: archery
                }
            }

            if (m_Hits > 0 && m_MaxHits > 0)
                list.Add(1060639, "{0}\t{1}", m_Hits, m_MaxHits); // durability ~1_val~ / ~2_val~
        }

        public override void OnSingleClick(Mobile from)
        {
            if (this.HideAttributes == true || (Name == null && UseOldNames))
            {
                base.OnSingleClick(from);
                return;
            }

            ArrayList attrs = new ArrayList();

            if (DisplayLootType)
            {
                if (GetFlag(LootType.Blessed))
                    attrs.Add(new EquipInfoAttribute(1038021)); // blessed
                else if (GetFlag(LootType.Cursed))
                    attrs.Add(new EquipInfoAttribute(1049643)); // cursed
            }

            #region Factions
            if (m_FactionState != null)
                attrs.Add(new EquipInfoAttribute(1041350)); // faction item
            #endregion

            Ethics.EthicBless.AddEquipmentInfoAttribute(this, attrs);

            if (m_Quality == WeaponQuality.Exceptional)
                attrs.Add(new EquipInfoAttribute(1018305 - (int)m_Quality));

            if (m_Identified)
            {
                if (m_Slayer != SlayerName.None)
                    attrs.Add(new EquipInfoAttribute(1017383 + (int)m_Slayer));

                if (m_DurabilityLevel != WeaponDurabilityLevel.Regular)
                    attrs.Add(new EquipInfoAttribute(1038000 + (int)m_DurabilityLevel));

                if (m_DamageLevel != WeaponDamageLevel.Regular)
                    attrs.Add(new EquipInfoAttribute(1038015 + (int)m_DamageLevel));

                if (m_AccuracyLevel != WeaponAccuracyLevel.Regular)
                    attrs.Add(new EquipInfoAttribute(1038010 + (int)m_AccuracyLevel));

                if (m_MagicEffect != MagicItemEffect.None)
                    attrs.Add(new EquipInfoAttribute(MagicItems.GetLabel(m_MagicEffect), m_MagicCharges));
            }
            else if (m_Slayer != SlayerName.None || m_DurabilityLevel != WeaponDurabilityLevel.Regular || m_DamageLevel != WeaponDamageLevel.Regular || m_AccuracyLevel != WeaponAccuracyLevel.Regular || m_MagicEffect != MagicItemEffect.None)
            {
                attrs.Add(new EquipInfoAttribute(1038000)); // Unidentified
            }

            if (m_Poison != null && m_PoisonCharges > 0)
                attrs.Add(new EquipInfoAttribute(1017383, m_PoisonCharges));

            int number;

            if (Name == null)
            {
                number = LabelNumber;
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            if (attrs.Count == 0 && Crafter == null && Name != null)
                return;

            EquipmentInfo eqInfo = new EquipmentInfo(number, m_Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));
            from.Send(new DisplayEquipmentInfo(this, eqInfo));
        }

        public override string GetOldPrefix(ref Article article)
        {
            string prefix = "";

            if (m_Poison != null && m_PoisonCharges > 0)
            {
                if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    article = Article.A;

                prefix += "poisoned ";
            }

            if (!HideAttributes && m_Quality == WeaponQuality.Exceptional)
            {
                if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    article = Article.An;

                prefix += "exceptional ";
            }

            if (m_Identified)
            {
                if (!HideAttributes && m_Slayer == SlayerName.Silver)
                {
                    if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                        article = Article.A;

                    prefix += "silver ";
                }

                if (!HideAttributes && m_AccuracyLevel != WeaponAccuracyLevel.Regular)
                {
                    if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    {
                        if (m_AccuracyLevel == WeaponAccuracyLevel.Accurate || m_AccuracyLevel == WeaponAccuracyLevel.Eminently || m_AccuracyLevel == WeaponAccuracyLevel.Exceedingly)
                            article = Article.An;
                        else
                            article = Article.A;
                    }

                    if (m_AccuracyLevel == WeaponAccuracyLevel.Accurate)
                        prefix += m_AccuracyLevel.ToString().ToLower() + " ";
                    else
                        prefix += m_AccuracyLevel.ToString().ToLower() + " accurate ";
                }

                if (!HideAttributes && m_DurabilityLevel != WeaponDurabilityLevel.Regular)
                {
                    if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    {
                        if (m_DurabilityLevel == WeaponDurabilityLevel.Indestructible)
                            article = Article.An;
                        else
                            article = Article.A;
                    }

                    prefix += m_DurabilityLevel.ToString().ToLower() + " ";
                }
            }
            else if (!HideAttributes && (m_Slayer != SlayerName.None
                || m_DurabilityLevel != WeaponDurabilityLevel.Regular
                || m_DamageLevel != WeaponDamageLevel.Regular
                || m_AccuracyLevel != WeaponAccuracyLevel.Regular
                || m_MagicEffect != MagicItemEffect.None))
            {
                if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    article = Article.A;

                prefix += "magic ";
            }

            if (EventResourceSystem.Find(m_Resource) != null)
            {
                CraftResourceInfo info = CraftResources.GetInfo(m_Resource);

                if (info != null)
                {
                    if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                        article = info.Article;

                    prefix += String.Concat(info.Name.ToLower(), " ");
                }
            }

            return prefix;
        }

        public override string GetOldSuffix()
        {
            string suffix = "";

            if (m_Identified)
            {
                if (!HideAttributes && m_DamageLevel != WeaponDamageLevel.Regular)
                {
                    if (suffix.Length == 0)
                        suffix += " of ";
                    else
                        suffix += " and ";

                    suffix += m_DamageLevel.ToString().ToLower();
                }

                if (!HideAttributes && m_Slayer != SlayerName.None && m_Slayer != SlayerName.Silver)
                {
                    if (suffix.Length == 0)
                        suffix += " of ";
                    else
                        suffix += " and ";

                    suffix += SlayerLabel.GetSuffix(m_Slayer).ToLower();
                }

                if (!HideAttributes && m_MagicEffect != MagicItemEffect.None)
                {
                    if (suffix.Length == 0)
                        suffix += " of ";
                    else
                        suffix += " and ";

                    suffix += MagicItems.GetOldSuffix(m_MagicEffect, m_MagicCharges);
                }
            }

            if (m_Crafter != null)
                suffix += " crafted by " + m_Crafter.Name;

            if (m_Poison != null && m_PoisonCharges > 0)
                suffix += String.Format(" (poison charges: {0})", m_PoisonCharges);

            return suffix;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!HitMagicEffect && m_MagicEffect != MagicItemEffect.None)
                MagicItems.OnUse(from, this);
        }

        private static BaseWeapon m_Fists; // This value holds the default--fist--weapon

        public static BaseWeapon Fists
        {
            get { return m_Fists; }
            set { m_Fists = value; }
        }

        public Poison MutatePoison(Mobile mob, Poison poisonOnWeapon)
        {
            //CURRENT METHOD
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.MeleePoisonSkillFactor) || this is BaseRanged)
            {
                int poisonSkill = (int)mob.Skills[SkillName.Poisoning].Base;

                if ((Utility.Random(100) <= poisonSkill) && (poisonSkill != 0))
                {
                    return poisonOnWeapon;
                }
                else
                {
                    return Poison.GetPoison(poisonOnWeapon.Level - 1);
                }
            }
            else
            {
                return poisonOnWeapon;
            }

            //ALTERNATE METHOD: With min/max skilllevels per poison level
            //			double min = 0; //10% chance to poison at level
            //			double max = 100; //90% chance to poison at level
            //			switch(poisonOnWeapon.Level)
            //			{
            //				case 0: //lesser
            //					min = 0;
            //					max = 40;
            //					break;
            //				case 1: //regular
            //					min = 20;
            //					max = 60;
            //					break;
            //				case 2: //greater
            //					min = 40;
            //					max = 80;
            //					break;
            //				case 3: //deadly
            //					min = 60;
            //					max = 100;
            //					break;
            //				case 4: //lethal -- should never happen
            //					min = 80;
            //					max = 120;
            //					break;
            //				default:
            //					break;
            //			}
            //			double poisonSkill = mob.Skills[SkillName.Poisoning].Value;
            //			double chanceToPoisonCurrentLevel = 0;
            //			if( poisonSkill < min )
            //				chanceToPoisonCurrentLevel = 10.0;
            //			else if( poisonSkill > max )
            //				chanceToPoisonCurrentLevel = 90.0;
            //			else
            //				chanceToPoisonCurrentLevel = 10 + (80 * (poisonSkill-min)/(max-min));
            //
            //			if( Utility.Random(100) <= chanceToPoisonCurrentLevel )
            //				return poisonOnWeapon;
            //			else
            //				return Poison.GetPoison( poisonOnWeapon.Level - 1 );
        }

        private double m_HitPoisonChance; // not serialized

        public bool CheckHitPoison(Mobile attacker)
        {
#if RunUO
            // in RunUO, poison lands 50% of the time
            return Utility.RandomBool();
#else
            double poisonSkill = m_PoisonSkill / 10.0;

            if (poisonSkill < 30.0)
                poisonSkill = 30.0; // arbitrary lower bound

            if (poisonSkill > 100.0)
                poisonSkill = 100.0;

            double increment = CoreAI.HitPoisonChanceIncr * poisonSkill / 100.0;

            m_HitPoisonChance += increment;

            //attacker.SendMessage("Hit poison: {0:F2}", m_HitPoisonChance);

            if (Utility.RandomDouble() < m_HitPoisonChance)
            {
                m_HitPoisonChance = 0.0;
                return true;
            }

            return false;
#endif
        }

        public void OnHitPoison(Mobile attacker)
        {
            if (m_Poison == null)
                return; // sanity

            int corrosion;

            if (m_Poison.Level > 1)
            {
                double poisonSkill = attacker.Skills[SkillName.Poisoning].Value;

                if (poisonSkill >= 99.0)
                    corrosion = m_Poison.Level - 1;
                else if (poisonSkill >= 50.0)
                    corrosion = m_Poison.Level;
                else
                    corrosion = m_Poison.Level + 1;
            }
            else
            {
                corrosion = 1;
            }

            if (corrosion > 0)
            {
                if (m_Corrosion == 0)
                {
                    if (this is BaseRanged)
                        attacker.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "The poison soaks into the bow, weakening your weapon.");
                    else
                        attacker.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1005537); // Blood mixes with poison and begins to corrode your weapon.
                }

                m_Corrosion = (byte)corrosion;
            }
        }

        // soft metals are corrosion-resistant materials
        private static readonly CraftResource[] m_SoftMetals = new CraftResource[] { CraftResource.DullCopper, CraftResource.Copper, CraftResource.Gold, CraftResource.Bronze };

        private Memory m_dwBloodScars = new Memory();
        private Memory m_dwSeverelyDamaged = new Memory();
        private Memory m_dwResistsAcidBlood = new Memory();
        private Memory m_dwResistsCorrosiveEffect = new Memory();

        public void DoWeaponWear(Mobile attacker, Mobile defender, bool onHit)
        {
            // nothing to damage
            if (m_MaxHits <= 0)
                return;

            // exclude these cases
            bool weaponWearsOut = (attacker is BaseCreature bc && bc.AIObject != null && bc.AIObject.WeaponWearsOut(this, defender, attacker));
            if ((!attacker.Player && !weaponWearsOut) || this is Fists)
                return;

            bool isSoftMetal = (Array.IndexOf(m_SoftMetals, Resource) != -1); // soft metals are corrosion-resistant materials        
            bool IsSealed = (m_WaxCharges > 0); // wax coated weapons (wood) are immune to corrosive agents

            int wear = 0;

            #region 1. Wear & Tear by acid blood
            if (MaxRange <= 1 && (defender is Slime || defender is ToxicElemental))
            {
                bool acidScars = true;

                #region Soft Metals & Wax Coatings (AI/MO)
                if ((Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()) && acidScars && isSoftMetal)
                {
                    acidScars = false;
                    if (m_dwResistsAcidBlood.Recall("ResistsAcidBlood") == null)
                    {   // anti-spam
                        attacker.LocalOverheadMessage(MessageType.Regular, 0x3B2, true, "Your weapon resists the acid blood of this creature.");
                        m_dwResistsAcidBlood.Remember("ResistsAcidBlood", 7);
                    }
                }

                if ((Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()) && acidScars && IsSealed)
                {
                    acidScars = false;
                    if (m_dwResistsAcidBlood.Recall("ResistsAcidBlood") == null)
                    {   // anti-spam
                        attacker.LocalOverheadMessage(MessageType.Regular, 0x3B2, true, "Your weapon resists the acid blood of this creature.");
                        m_dwResistsAcidBlood.Remember("ResistsAcidBlood", 7);
                    }

                    if (--m_WaxCharges == 0)
                        attacker.SendAsciiMessage("The wax coating has worn off your weapon.");
                }
                #endregion

                if (acidScars)
                {
                    wear++;

                    if (m_dwBloodScars.Recall("BloodScars") == null)
                    {   // anti-spam
                        attacker.LocalOverheadMessage(MessageType.Regular, 0x3B2, 500263); // *Acid blood scars your weapon!*
                        m_dwBloodScars.Remember("BloodScars", 7);
                    }
                }
            }
            #endregion 1. Wear & Tear by acid blood

            #region 2. Normal Wear & Tear
            // only normal wear & tear if we don't have acid blood
            if (wear == 0)
            {
                // Stratics says 50% chance, seems more like 4%..
                if (Utility.Random(25) == 0)
                    wear++;
            }
            #endregion 2. Normal Wear & Tear

            #region 3. Wear & Tear by corrosion
            if (m_Corrosion > 0)
            {
                int corrodeWeapon = m_Corrosion;

                #region Soft Metals & Wax Coatings (AI/MO)
                if ((Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()) && corrodeWeapon > 0 && isSoftMetal)
                {
                    corrodeWeapon = 0;
                    if (m_dwResistsCorrosiveEffect.Recall("CorrosiveEffect") == null)
                    {   // anti-spam
                        attacker.LocalOverheadMessage(MessageType.Regular, 0x3B2, true, "Your weapon resists the corrosive effect of the poison.");
                        m_dwResistsCorrosiveEffect.Remember("CorrosiveEffect", 7);
                    }
                }

                if ((Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()) && corrodeWeapon > 0 && IsSealed)
                {
                    corrodeWeapon = 0;
                    if (--m_WaxCharges == 0)
                        attacker.SendAsciiMessage("The wax coating has worn off your weapon.");
                    if (m_dwResistsCorrosiveEffect.Recall("CorrosiveEffect") == null)
                    {   // anti-spam
                        attacker.LocalOverheadMessage(MessageType.Regular, 0x3B2, true, "Your weapon resists the corrosive effect of the poison.");
                        m_dwResistsCorrosiveEffect.Remember("CorrosiveEffect", 7);
                    }
                }
                #endregion

                if (corrodeWeapon > 0)
                    wear += corrodeWeapon;
            }
            #endregion 3. Wear & Tear by corrosion

            // do the damage
            Damage(wear);
        }

        public void Damage(int damage)
        {
            if (damage <= 0)
                return;

            if (m_Hits > damage)
            {
                HitPoints -= damage;

                if (HitPoints <= 10 && Parent is Mobile)
                {
                    if (m_dwSeverelyDamaged.Recall("SeverelyDamaged") == null)
                    {   // anti-spam
                        m_dwSeverelyDamaged.Remember("SeverelyDamaged", 4);

                        if (Parent is IEntity parent)
                            parent.Notify(notification: Notification.WeaponStatus, this, 1061121);    // Your equipment is severely damaged.);
                    }
                }
            }
            else
            {
                if (Parent is IEntity parent)
                    parent.Notify(notification: Notification.Destroyed, this);
                Delete();
            }
        }

        protected Mobile IsGuardAttacking(Mobile m)
        {
            List<AggressorInfo> aggressors = m.Aggressed;

            if (aggressors.Count > 0)
            {
                for (int i = 0; i < aggressors.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)aggressors[i];
                    Mobile defender = info.Defender;

                    if (defender != null && !defender.Deleted && defender.GetDistanceToSqrt(m) <= 1)
                    {
                        if (defender is Mobiles.BaseGuard)
                            return defender;
                    }
                }
            }
            return null;
        }

        // adam: We introduce the notion of 'Interference'
        //	Interference is where you have a guard on you and the guard is trying to stop you from killing a 
        //	some poor citizen. The guard is trained in such things
        protected bool CheckInterference(Mobile attacker, Mobile defender)
        {
            // attacker skill
            Skill atkSkill = attacker.Skills[(attacker.Weapon as BaseWeapon).Skill];

            // no Interference if we're not in town with a guard whacking us.
            //	also, interference doesn't kick in untill we've taken some damage
            Mobile guard = null;
            if (attacker.Region is Regions.GuardedRegion && (attacker.Region as Regions.GuardedRegion).IsGuarded && (attacker.Region as Regions.GuardedRegion).IsSmartGuards && attacker.Hits < attacker.HitsMax && (guard = IsGuardAttacking(attacker)) != null)
            {
                double chance = (((atkSkill.Value - 80.0) * 5.0) + ((attacker.Dex - 50.0) * 2.0)) / 2.66;
                chance /= 100.0;
                if (chance >= Utility.RandomDouble())
                    // cool, just hit as per normal
                    return false;
                else
                {
                    // Interference!
                    switch (Utility.Random(5))
                    {
                        case 0:
                            attacker.SendMessage("{0} parrys your blow directed at {1}.", guard.Name, defender.Name);
                            break;

                        case 1:
                            attacker.SendMessage("{0} parrys your attack.", guard.Name);
                            break;

                        case 2:
                            if (chance < 0.3)
                            {
                                guard.Say("You're really bad at this {0}.", attacker.Name);
                                attacker.SendMessage("Parried.");
                            }
                            else if (chance < 0.5)
                            {
                                guard.Say("You have some skill {0}, but you are no match for me!", attacker.Name);
                                attacker.SendMessage("Parried.");
                            }
                            else
                            {
                                guard.Say("I won't allow you to harm {0}!", defender.Name);
                                attacker.SendMessage("Parried.");
                            }
                            break;

                        default:
                            attacker.SendMessage("Your attack is parried.");
                            break;

                    }
                    return true;
                }
            }

            return false;
        }

        #region ICraftable Members

        public virtual int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            Quality = (WeaponQuality)quality;

            if (makersMark)
                Crafter = from;

            Type resourceType = typeRes;

            if (resourceType == null)
                resourceType = craftItem.Resources.GetAt(0).ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            CraftContext context = craftSystem.GetContext(from);

            if (context != null && context.DoNotColor)
                Hue = 0;

            #region Magic Craft
            // Angel Island player crafter magic weapons
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.MagicCraftSystem) == true)
                if (tool is TenjinsHammer || tool is TenjinsSaw)
                {
                    //context = craftSystem.GetContext(from);
                    bool doNotColor = (context != null && context.DoNotColor) ? true : false;
                    bool allRequiredSkills = true;
                    double chance = craftItem.GetSuccessChance(from, typeRes, craftSystem, false, ref allRequiredSkills);
                    Spell spell = new CraftWeaponSpell(from, chance, this, Resource, doNotColor);
                    spell.Cast();
                }
            #endregion Magic Craft

            return quality;
        }
        #endregion ICraftable Members

        #region Dynamic Resources
#if false
        public static bool DynamicResource(Mobile from)
        {
            if (from != null)
            {
                if (from.Backpack != null)
                {
                    foreach (Item item in from.Backpack.Items)
                    {
                        if (item is CrystallinePowder && (item as CrystallinePowder).UsesRemaining > 0)
                            return true;
                    }
                }
            }
            return false;
        }
        public static CraftResource GetDynamicResource(Mobile from)
        {
            if (from != null)
            {
                if (from.Backpack != null)
                {
                    foreach (Item item in from.Backpack.Items)
                    {
                        if (item is CrystallinePowder && (item as CrystallinePowder).UsesRemaining > 0)
                        {
                            (item as CrystallinePowder).UsesRemaining--;
                            return (item as CrystallinePowder).Resource;
                        }
                    }
                }
            }
            return CraftResource.None;
        }
#endif
        #endregion Dynamic Resources

        #region Magic Effects
        public void SetRandomMagicEffect(int MinLevel, int MaxLevel)
        {
            if (MinLevel < 1 || MaxLevel > 3)
                return;

            m_MagicEffect = Loot.WeaponEnchantments[Utility.Random(Loot.WeaponEnchantments.Length)];

            int NewLevel = Utility.RandomMinMax(MinLevel, MaxLevel);
            switch (NewLevel)
            {
                case 1:
                    m_MagicCharges = Utility.Random(1, 5);
                    break;
                case 2:
                    m_MagicCharges = Utility.Random(4, 11);
                    break;
                case 3:
                    m_MagicCharges = Utility.Random(9, 20);
                    break;
                default:
                    // should never happen
                    m_MagicCharges = 0;
                    break;
            }
            Identified = false;
        }
        private MagicItemEffect m_MagicEffect = MagicItemEffect.None;
        private int m_MagicCharges;

        [CommandProperty(AccessLevel.GameMaster)]
        public MagicItemEffect MagicEffect
        {
            get { return m_MagicEffect; }
            set { m_MagicEffect = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MagicCharges
        {
            get { return m_MagicCharges; }
            set { m_MagicCharges = value; }
        }

        public virtual bool HitMagicEffect { get { return true; } }

        #endregion
    }



    public enum CheckSlayerResult
    {
        None,
        Slayer,
        Opposition
    }
}