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

/* Scripts\Engines\AI\AI\GuardAI.cs
 * ChangeLog
 * 8/21/21, Adam:
 * 	Add special callback �AcquireFocusMobCallback()� to BaseAI and override it here. This callback allows special AIs like GuardAI to perform highly specific �Acquire Focus Mob� processing 
 * 	    that otherwise does not fit into the normal mobile acquisition model. 
 * 	For instance, guards now dispatch monsters when they are in a guarded region, they are not controlled, and they have an aggressive AI. 
 * 	    Most of this could be covered by the standard AI with the exception of the inside/outside region constraints. Also the target FightMode may also be clumsy to specify. 
 * 	    It�s not that these things can�t be tested and acted upon in AcquireFocusMob(), but rather AcquireFocusMob doesn�t have the FightMode flags to specify such a specific target.
 * 	    Therefore, we override the �AcquireFocusMobCallback()� here in the GuardAI to facilitate such a check.
 * 8/11/21, Adam
 *      override AcquireFocusMobWorker to better handle a hidden focus mob.
 *      The base logic doesn't know about the guards 'FocusMob', and so goes on to try to Acquire a new 'Focus'. Eventually,
 *      We get back to the guard behavior of trying to seek out our 'FocusMob'.. this causes the Guard to switch in and out of DoActionWander,
 *      Causing the guard to run back and forth like a goof. This fix 'pins' the guard to it's 'FocusMob' inbetween Think() ticks.
 *  6/30/10, Adam
 *		Initial creation
 *		Guards now do some basic heals and will cast the new NPC spell "InvisibleShield", "Kal Sanct Grav" on suicide bombers. (Kal (summon), Sanct (Protection), Grav (field))
 */

using Server.Spells;
using Server.Spells.Fifth;
using System;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Mobiles
{
    public class GuardAI : HybridAI
    {
        public GuardAI(BaseCreature guard)
            : base(guard)
        {

        }
        public override bool StayAngryAtAggressed
        { get { return false; } }
        public override Spell ChooseSpell(Mobile target)
        {
            if (m_Mobile.InRange(target, 5) && target.SuicideBomber && !target.InvisibleShield)
                return new InvisibleShieldSpell(m_Mobile, null);
            else
                return base.ChooseSpell(target);
        }

        public override void WalkRandomInHome(int iChanceToNotMove, int iChanceToDir, int iSteps, bool evenIfStationed = false)
        {
            base.WalkRandomInHome(iChanceToNotMove, iChanceToDir, iSteps, evenIfStationed);
        }

        public override bool DoActionCombat(MobileInfo info)
        {
            bool dac = base.DoActionCombat(info: info);

            if (m_Mobile.Spell == null && DateTime.UtcNow >= m_Mobile.NextSpellTime)
            {
                if (m_Mobile.Spell == null || !(m_Mobile.Spell as Spell).Cast())
                    EquipWeapon();
            }
            else if (m_Mobile.Spell is Spell && ((Spell)m_Mobile.Spell).State == SpellState.Sequencing)
                EquipWeapon();

            return dac;
        }

        public override bool AcquireFocusMobWorker(int iRange, FightMode acqType, FightMode acqFlags, bool bPlayerOnly, bool bFacFriend, bool bFacFoe, List<Mobile> ignore = null)
        {
            if (m_Mobile.Deleted)
                return false;
            // CASE 1:
            // if we already have a focus nearby, just go with them.
            // I added this to prevent the AI from switching back and forth from action.Combat to action.Wander
            // Guards: When they are on someone, no need to fool around with other targets.
            if (m_Mobile.Combatant != null && m_Mobile is WarriorGuard guard && guard.Focus != null && guard.Focus.Alive)
                if (m_Mobile.Combatant == guard.Focus && guard.Focus.Deleted == false)  // maybe not needed?
                    if (guard.GetDistanceToSqrt(guard.Focus) <= guard.RangePerception)
                    {
                        // we already have a good Combatant, we'll stick with him
                        m_Mobile.FocusMob = m_Mobile.Combatant;
                        return true;
                    }

            return base.AcquireFocusMobWorker(iRange, acqType, acqFlags, bPlayerOnly, bFacFriend, bFacFoe);
        }
        // CASE 2:
        // AcquireFocusMobWorker doesn't understand subtleties, like guard region, or IsGuarded.
        //  Rather than try to overly expand AcquireFocusMob, we will handle it here as a special case (which it is.)
        public override bool AIFocusMobCallback(Mobile m)
        {
            if (!(m_Mobile.Region is Regions.GuardedRegion && m_Mobile.Region.IsGuarded) && (m_Mobile is BritannianRanger))
                return UnGuardedRegionEvilMonsterRule(m_Mobile, m_Mobile.Region, m);
            else
                // called from BaseAI (GuardedRegionEvilMonsterRule is also IsGuardCandidate)
                return GuardedRegionEvilMonsterRule(m_Mobile, m_Mobile.Region, m);
        }
        public static bool UnGuardedRegionEvilMonsterRule(Mobile checking, Region r, Mobile beingChecked)
        {   // special guard logic

            // the guard is not in a guarded region
            if ((r is Regions.GuardedRegion && r.IsGuarded))
                return false;

            // and the mobile is not in a guarded region
            if ((beingChecked.Region is Regions.GuardedRegion && beingChecked.Region.IsGuarded))
                return false;

            // and we are in the same region
            //if ((r == beingChecked.Region))
            //return false;

            // and the mobile is a creature
            if (!(beingChecked is BaseCreature))
                return false;

            // and the creature is uncontrolled, no control master, and has menacing AI, and is summoned,
            // then it falls under the GuardedRegionEvilMonsterRule()
            if ((beingChecked as BaseCreature).Controlled == false && (beingChecked as BaseCreature).ControlMaster == null && (beingChecked as BaseCreature).Summoned && beingChecked.Alive)
            {
                if ((beingChecked as BaseCreature).FightMode.HasFlag(FightMode.All))
                    return true;
                else if (checking != null && checking is BaseCreature)
                    (checking as BaseCreature).DebugSay(DebugFlags.AI, "I have detected {0}, but {1} seems non aggressive.", beingChecked.Name, beingChecked.Female ? "she" : "he");
            }
            else
            {
                if (checking != null && checking is BaseCreature)
                    (checking as BaseCreature).DebugSay(DebugFlags.AI, "I have detected {0}, but {1} seems cool.", beingChecked.Name, beingChecked.Female ? "she" : "he");
            }

            return false;
        }
        public static bool GuardedRegionEvilMonsterRule(Mobile checking, Region r, Mobile beingChecked)
        {   // special guard logic
            // called from BaseAI and also IsGuardCandidate

            // the guard is in a guarded region
            if (!(r is Regions.GuardedRegion && r.IsGuarded))
                return false;

            // and the mobile is in a guarded region
            if (!(beingChecked.Region is Regions.GuardedRegion && beingChecked.Region.IsGuarded))
                return false;

            // and we are in the same region
            if (!(r == beingChecked.Region))
                return false;

            // and the mobile is a creature
            if (!(beingChecked is BaseCreature bc))
                return false;

            // and the creature is uncontrolled, no control master, and has menacing AI, then it falls under the GuardedRegionEvilMonsterRule()
            if (bc.Controlled == false && bc.ControlMaster == null && bc.Alive)
            {
                if (bc.FightMode.HasFlag(FightMode.All) && !bc.GuardIgnore)
                    return true;
                if (checking != null && bc.FightMode.HasFlag(FightMode.All) && bc.GuardIgnore)
                    (checking as BaseCreature).DebugSay(DebugFlags.AI, "I have detected {0}, but creature listed as GuardIgnore.", bc.Name);
                else if (checking != null)
                    (checking as BaseCreature).DebugSay(DebugFlags.AI, "I have detected {0}, but {1} seems non aggressive.", bc.Name, bc.Female ? "she" : "he");
            }
            else
            {
                if (checking != null)
                    (checking as BaseCreature).DebugSay(DebugFlags.AI, "I have detected {0}, but {1} seems cool.", bc.Name, bc.Female ? "she" : "he");
            }

            return false;
        }

        public override bool DoActionWander()
        {
            bool daw = base.DoActionWander();

            if (m_Mobile.Spell == null && DateTime.UtcNow >= m_Mobile.NextSpellTime)
            {
                if (m_Mobile.Spell == null || !(m_Mobile.Spell as Spell).Cast())
                    EquipWeapon();
            }
            else if (m_Mobile.Spell is Spell && ((Spell)m_Mobile.Spell).State == SpellState.Sequencing)
                EquipWeapon();

            return daw;
        }

        public override bool PreferMagic()
        {
            DateTime m_FightMode = DateTime.UtcNow;

            if (IsAllowed(FightStyle.Melee) && (double)m_Mobile.Mana < m_Mobile.ManaMax * .30)
                return false;   // low on mana use melee
            else if (m_Mobile.Combatant is BaseCreature)
                return false;   // destroy pets with our weapon
            if (m_Mobile.InRange(m_Mobile.Combatant, 5) && m_Mobile.Combatant.SuicideBomber && !m_Mobile.Combatant.InvisibleShield)
                return true;    // we need to cast InvisibleShield

            // - finally -
            else if ((m_FightMode.Second >= 0 && m_FightMode.Second <= 15) || (m_FightMode.Second >= 30 && m_FightMode.Second <= 45))
                return false;   // prefer weapon for the first 15 seconds and the 3rd 15 seconds out of each minute
            else
                return true;    // use magic
        }

        #region Serialize
        private SaveFlags m_flags;

        [Flags]
        private enum SaveFlags
        {   // 0x00 - 0x800 reserved for version
            unused = 0x1000
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;                                // current version (up to 4095)
            m_flags = m_flags | (SaveFlags)version;         // save the version and flags
            writer.Write((int)m_flags);

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            m_flags = (SaveFlags)reader.ReadInt();              // grab the version an flags
            int version = (int)(m_flags & (SaveFlags)0xFFF);    // maskout the version

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
            switch (version)
            {
                default: break;
            }

        }
        #endregion Serialize
    }
}