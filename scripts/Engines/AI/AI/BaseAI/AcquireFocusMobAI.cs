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

/* Scripts\Engines\AI\AI\BaseAI\AcquireFocusMobAI.cs
 * CHANGELOG
 *  4/12/2024, Adam
 *      We don't acquire mobiles that are inaccessible. 
 *      Problem: We use Spawner.ClearPath() to make this determination, but Spawner.ClearPath only works for pathing on land.
 *      (Now renamed to Spawner.ClearPathLand())
 *      For water-mobs, we simply check to see if they are on water, and if so, assume accessibility. 
 *      What we really need is Spawner.ClearPathWater(). TBD
 *  10/15/2023, Adam
 *      EVs and BSs don't attack other EVs and BSs from the same summon master.
 *      Sort of an AI special, not sure how it works on OSI - but it's intuitive!
 *  8/30/2023, Adam (attack master)
 *      Add FightMode.Master which directs AcquireFocusMob to allow attacking the master.
 *      Both Energyvortex and BladeSpirits have this flag. Keep in mind, both of these summons
 *      will prefer higher INT targets (Energyvortex) or higher STR targets (BladeSpirits) regardless of their master.
 *  4/15/23, Yoar
 *      Added support for new Alignment system.
 *  4/9/23, Adam (Inaccessible mobiles)
 *      we don't want to include Inaccessible mobiles in our sort, too expensive. Instead, we will enqueue them and only
 *          use them as a last resort. Most AIs will ignore Inaccessible mobiles, but Champs wont and neither will guards.
 *  3/25/23, Adam (range perception)
 *      Siege folks are complaining that monsters are aggroing from too far away.
 *      The RunUO code used to have the default range perception at 10, then they changed it to 16
 *      I can only assume that 10'ish was right for the Siege era, and this is the problem.
 *      Rather than change/condition 100 source files, we'll just reduce the aggro range here by 70%
 *      i.e., iRange = (int)((double)iRange * 0.7);
 *  1/25/23, Adam
 *      Add a new signature for AcquireFocusMob: Mobile AcquireFocusMob(Mobile want = null, bool killFocus = false)
 *      This version allows you to specify a mobile of interest, or if no mobile of interest is specified, 
 *          any appropriate mobile is returned.
 *      If killFocusis specified, the function just returns what would have been the focus mobile.
 *  1/9/22, Adam
 *      First time checkin.
 *      Break out AcquireFocusMob into its own 'partial' class
 */

/*  ** total rewrite of AcquireFocusMob() **
    * Description:  
    * We split the FightMode into two different bitmasks: 
    * (1) The TYPE of creature to focus on (murderer, Evil, Aggressor, etc.)
    * (2) The SELECTION parameters (closest, smartest, strongest, etc.)
    * We then enumerate each value contained in the TYPE bitmask and pass each one to the
    * AcquireFocusMobWorker() function along with the SELECTION mask.
    * AcquireFocusMobWorker() will perform a similar enumeration over the SELECTION mask
    * to build a sorted list of compound selection criteria, for instance Closest and Strongest. 
    * Differences from OSI: Most creatures will act the same as on OSI; and if they don’t, we probably
    * set the FightMode flags wrong for that creature. The real difference is the flexibility to do things
    * not supported on OSI like creating compound aggression formulas like: 
    * “Focus on all Evil and Criminal players while attacking the Weakest first with the highest Intelligence”
*/

using Server.Diagnostics;
using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Server.Utility;

namespace Server.Mobiles
{
    public abstract partial class BaseAI : SerializableObject
    {
        /// <summary>
        /// Add a new signature for AcquireFocusMob: Mobile AcquireFocusMob(Mobile want = null, bool killFocus = false)
        /// This version allows you to specify a mobile of interest, or if no mobile of interest is specified any appropriate mobile is returned.
        /// If killFocus is specified, the function just returns what would have been the focus mobile.
        /// </summary>
        /// <param name="want">Mobile you wish to test or null for any mobile</param>
        /// <param name="killFocus">Unset the FocusMob</param>
        /// <returns>Mobile of interest</returns>
        public Mobile AcquireFocusMob(Mobile want = null, bool bPlayerOnly = true, bool bFacFriend = false, bool bFacFoe = true, bool killFocus = false)
        {   // we're looking to attack a specific mobile or any mobile
            List<Mobile> ignore = new();
            while (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, bPlayerOnly, bFacFriend, bFacFoe, ignore))
            {
                if (want == null || m_Mobile.FocusMob == want)
                {   // we may only want the mobile that WOULD be the focus. Here we unset
                    //  the FocusMob if so instructed
                    Mobile focus = m_Mobile.FocusMob;
                    if (killFocus)
                        m_Mobile.FocusMob = null;
                    return focus;
                }
                else
                {   // ignore this guy
                    if (!ignore.Contains(m_Mobile.FocusMob))
                        ignore.Add(m_Mobile.FocusMob);
                    m_Mobile.FocusMob = null;
                    // keep looking
                    continue;
                }
            }

            return null;
        }
        private static bool AnyAggressors(Mobile m)
        {
            if (m.Aggressors == null || m.Aggressors.Count == 0)
                return false;

            List<AggressorInfo> list = m.Aggressors;
            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo ai = (AggressorInfo)list[i];

                if (ai.Attacker.Alive && m.CanSee(ai.Attacker))
                    return true;
            }

            return false;
        }
        private static bool AggressionFilter(Mobile m, FightMode acqSelect)
        {
            if (acqSelect == 0)             // not actively looking for trouble
                if (!AnyAggressors(m))      // and we have no aggressors
                    return true;            // filter

            return false;                   // okay to process aggressors
        }
        public bool AcquireFocusMob(int iRange, FightMode acqTypeRaw, bool bPlayerOnly, bool bFacFriend, bool bFacFoe, List<Mobile> ignore = null)
        {

            // separate the flags from the target types
            FightMode acqSelect = (FightMode)((uint)acqTypeRaw & 0xFFFF0000);
            FightMode acqType = (FightMode)((uint)acqTypeRaw & 0x0000FFFF);

            // optimization - don't bother with all this processing if we're not aggressive and we have not been aggressed.
            if (AggressionFilter(m_Mobile, acqSelect))
                // not looking for trouble
                return false;

            //Use a redefined priority list if one is present
            /*IList sourcePriority = m_FightModeValues;
			if (m_Mobile != null && m_Mobile is BaseCreature && !m_Mobile.Deleted && ((BaseCreature)m_Mobile).FightModePriority != null)
				sourcePriority = ((BaseCreature)m_Mobile).FightModePriority;*/

            #region DEBUG
            // If this fires it's because you likely have a RunUO legacy FightMode such as FightMode.Closest
            //	In the GMN model, FightMode.Closest is simply a sort option, you need an aggression specification such as:
            //	FightMode.All. E.g., FightMode.All | FightMode.Closest
            // it's okay to not have an "aggression specification" as something like attack Aggressors is totally fine.
            //  We filter for trammel here since our New Player Starting Area has some specially abstracted mobs that don't fit this model.
            if (acqType == 0 && m_Mobile.Map != Map.Trammel && false)
                Utility.ConsoleWriteLine(string.Format("Mobile({0}) {1}", m_Mobile, "has no acquire type."), ConsoleColor.Red);
            #endregion DEBUG

            try
            {   // if we have something to acquire
                if (acqType > 0)
                {
                    // for each enum value, check the passed-in acqType for a match
                    for (int ix = 0; ix < m_FightModeValues.Count; ix++)
                    {   // if this fight-mode-value exists in the acqType
                        if (((int)acqType & (int)m_FightModeValues[ix]) >= 1)
                            if (AcquireFocusMobWorker(iRange, (FightMode)m_FightModeValues[ix], acqSelect, bPlayerOnly, bFacFriend, bFacFoe))
                                return true;
                    }
                }
                else
                {   // 10/24/22, Adam: Update. We're testing allowing FightMode None - although we still complain
                    //Utility.ConsoleOut(string.Format("Mobile({0}) {1}", m_Mobile, "has no fightmode."), ConsoleColor.Yellow);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {   // we moved the delay setting code out of AcquireFocusMobWorker(AFM) since we need to call it once for each acqType.
                // If we were to leave it in AFM then we would only be able to call AFM the first time through the loop.
                // the new timing model is where are allowed to process ALL acquire types before being forced to wait
                if (m_Mobile.NextReacquireTime <= DateTime.UtcNow)
                    m_Mobile.NextReacquireTime = DateTime.UtcNow + m_Mobile.ReacquireDelay;
            }

            // no one to fight
            return false;
        }

        /*
		 * Here we check to acquire a target from our surrounding
		 * 
		 *  iRange : The range
		 *  acqType : A type of acquire we want (closest, strongest, etc)
		 *  bPlayerOnly : Don't bother with other creatures or NPCs, want a player
		 *  bFacFriend : Check people in my faction
		 *  bFacFoe : Check people in other factions
		 * 
		 */
        public virtual bool AcquireFocusMobWorker(int iRange, FightMode acqType, FightMode acqFlags, bool bPlayerOnly, bool bFacFriend, bool bFacFoe, List<Mobile> ignore = null)
        {
            if (m_Mobile.Deleted)
                return false;

            BaseCreature me = m_Mobile as BaseCreature;

            if (m_Mobile.BardProvoked)
            {
                if (m_Mobile.BardTarget == null || m_Mobile.BardTarget.Deleted)
                {
                    m_Mobile.FocusMob = null;
                    return false;
                }
                else
                {
                    m_Mobile.FocusMob = m_Mobile.BardTarget;
                    return (m_Mobile.FocusMob != null);
                }
            }
            else if (m_Mobile.Controlled)
            {
                // 8/9/21, Adam: allguardbug fix - i do believe
                //  Scenario: pets are in guard mode (control order Guard.) So, the m_Mobile.ControlTarget is rightly the tamer.
                //  If the tamer is then targeted by a monster, the old logic would incorrectly set the focus mob to ControlTarget (the tamer.)
                // Notes: ControlTarget gets set to an adversary when an "all kill" command is given, and the tamer targets a monster.
                //  Root of the problem: It was bad design for RunUO to overload the ControlTarget as both friend and foe... :/
                // 8/25/2023: Adam Expanded from simply looking at the first ControlMaster aggressor, to looking for the 
                //  first 'available' ControlMaster aggressor, then if not found, first available aggressor of mine.
                if (m_Mobile.ControlTarget == m_Mobile.ControlMaster && m_Mobile.ControlOrder == OrderType.Guard)
                {   // To mitigate this bug, we set the focusmob to be that thing that is attacking the tamer
                    Mobile tempFocus = null;
                    foreach (AggressorInfo aggressorInfo in m_Mobile.ControlMaster.Aggressors)
                    {   // see if we have a viable target
                        MobileInfo attackerinfo = GetMobileInfo(new MobileInfo(aggressorInfo.Attacker));
                        if (!attackerinfo.available)
                            continue;
                        else
                        {
                            tempFocus = aggressorInfo.Attacker;
                            break;
                        }
                    }

                    // okay, nobody is attacking my master, see if someone is attacking me!
                    if (tempFocus == null)
                        foreach (AggressorInfo aggressorInfo in m_Mobile.Aggressors)
                        {   // see if we have a viable target
                            MobileInfo attackerinfo = GetMobileInfo(new MobileInfo(aggressorInfo.Attacker));
                            if (!attackerinfo.available)
                                continue;
                            else
                            {
                                tempFocus = aggressorInfo.Attacker;
                                break;
                            }
                        }

                    m_Mobile.FocusMob = tempFocus;
#if false
                    if (m_Mobile.ControlMaster.Aggressors.Count > 0)
                        m_Mobile.FocusMob = m_Mobile.ControlMaster.Aggressors[0].Attacker;
                    else
                        m_Mobile.FocusMob = null;
#endif
                }
                else
                    m_Mobile.FocusMob = m_Mobile.ControlTarget;

                return (m_Mobile.FocusMob != null);

                #region OBSOLETE
                /* // 8/9/21, Adam: This old implementation assumes the ControlTarget is the correct target. It's not. It might still be the tamer as described above.
                if (m_Mobile.ControlTarget == null || m_Mobile.ControlTarget.Deleted || !m_Mobile.ControlTarget.Alive || m_Mobile.ControlTarget.IsDeadBondedPet || !m_Mobile.InRange(m_Mobile.ControlTarget, m_Mobile.RangePerception * 2))
                {
                    m_Mobile.FocusMob = null;
                    return false;
                }
                else
                {
                    m_Mobile.FocusMob = m_Mobile.ControlTarget;
                    return (m_Mobile.FocusMob != null);
                }
                */
                #endregion OBSOLETE
            }

            // 5/10/23, Adam: I no longer like this notion where the ConstantFocus transcends death
            //  we may have funniness in some AI logic because of this change, but we'll deal with them 
            //  as we encounter them.
            //  Renaming ConstantFocus ==> PreferredFocus to better fit it's new behavior. 
            if (m_Mobile.PreferredFocus != null && m_Mobile.PreferredFocus.Dead)
                m_Mobile.PreferredFocus = null;

            if (m_Mobile.PreferredFocus != null && !Ignored(m_Mobile.PreferredFocus, ignore) && m_Mobile.CanSee(m_Mobile.PreferredFocus))
            {
                m_Mobile.DebugSay(DebugFlags.AI, "Acquired my preferred focus");
                m_Mobile.FocusMob = m_Mobile.PreferredFocus;
                return true;
            }

            if (m_Mobile.NextReacquireTime > DateTime.UtcNow)
            {
                m_Mobile.FocusMob = null;
                return false;
            }

            m_Mobile.DebugSay(DebugFlags.AI, "Acquiring {0}...", acqType);

            Map map = m_Mobile.Map;

            if (map != null)
            {
                Mobile newFocusMob = null;

                // Siege folks are complaining that monsters are aggroing from too far away.
                //  The RunUO code used to have the default range perception at 10, then they changed it to 16
                //  I can only assume that 10'ish was right for the Siege era, and this is the problem.
                //  Rather than change/condition 100 source files, we'll just reduce the aggro range here to 70%
                if (Core.RuleSets.SiegeStyleRules())
                    iRange = (int)((double)iRange * 0.7);

                // add each mobile found to our list
                ArrayList list = new();
                IPooledEnumerable eable = map.GetMobilesInRange(m_Mobile.Location, iRange);
                foreach (Mobile m in eable)
                {
                    if (m != null && !Ignored(m, ignore))
                        list.Add(m);
                }
                eable.Free();

                // before proceeding, we will See() all of these mobiles.
                //  this can be rewritten to sort the tList directly
                See(list.Cast<Mobile>().ToList());

                // now sort our list based on the fight AI flags, weakest,closest,strongest,etc
                if (acqFlags > 0)
                    // for each enum value, check the passed-in acqType for a match
                    for (int ix = 0; ix < m_FightModeValues.Count; ix++)
                    {   // if this fight-mode-flag exists in the acqFlags
                        if (((int)acqFlags & (int)m_FightModeValues[ix]) >= 1)
                        {   // sort the list N times to percolate the 'best fit' values to the head of the list
                            SortDirection direction = (((FightMode)m_FightModeValues[ix] & (FightMode.Weakest | FightMode.Closest)) > 0) ? SortDirection.Ascending : SortDirection.Descending;
                            list.Sort(new AI_Sort(direction, m_Mobile, (FightMode)m_FightModeValues[ix]));
                        }
                    }

                // build a quick lookup table if we are conditional-attack AI to see if they should be attacked
                //	we use this 'memory'to remember hidden players
                Dictionary<Mobile, object> Fought = new Dictionary<Mobile, object>();
                if ((acqType & FightMode.All) == 0)
                {
                    Mobile mx;
                    if (StayAngryAtAggressors)
                        for (int a = 0; a < m_Mobile.Aggressors.Count; ++a)
                        {
                            mx = (m_Mobile.Aggressors[a] as AggressorInfo).Attacker;
                            if (Ignored(mx, ignore))
                                continue;
                            if (mx != null && mx.Deleted == false && mx.Alive && !mx.IsDeadBondedPet && m_Mobile.CanBeHarmful(mx, false) && (m_Mobile.Aggressors[a] as AggressorInfo).Expired == false && Fought.ContainsKey(mx) == false)
                                Fought[mx] = null;
                        }
                    if (StayAngryAtAggressed)
                        for (int a = 0; a < m_Mobile.Aggressed.Count; ++a)
                        {
                            mx = (m_Mobile.Aggressed[a] as AggressorInfo).Defender;
                            if (Ignored(mx, ignore))
                                continue;
                            if (mx != null && mx.Deleted == false && mx.Alive && !mx.IsDeadBondedPet && m_Mobile.CanBeHarmful(mx, false) && (m_Mobile.Aggressed[a] as AggressorInfo).Expired == false && Fought.ContainsKey(mx) == false)
                                Fought[mx] = null;
                        }
                }

                //  we don't want to include Inaccessible mobiles in our sort, too expensive. Instead, we will enqueue them and only
                //  use them as a last resort
                Queue<Mobile> InaccessibleQueue = new Queue<Mobile>();

                // okay, pick a target - the first one *should* be the best match since our list is sorted
                foreach (Mobile m in list)
                {
                    if (m.Deleted || m.Blessed)
                        continue;

                    // Let's not target ourselves...
                    if (m == m_Mobile)
                        continue;

                    // Dead targets are invalid.
                    if (!m.Alive || m.IsDeadBondedPet)
                        continue;

                    // Staff members cannot be targeted.
                    // 7/30/2021, Adam: if a staff member is not blessed, they should be attackable. This makes testing easier, and will require special
                    //  considerations when putting on events (for the better.)
                    //if (m.AccessLevel > AccessLevel.Player)
                    //continue;

                    // Does it have to be a player?
                    if (bPlayerOnly && !m.Player)
                        continue;

                    // Can't acquire a target we can't see.
                    if (!m_Mobile.CanSee(m))
                        continue;

                    if (me != null && m_Mobile.Summoned && m_Mobile.SummonMaster != null)
                    {
                        // EVs and BSs can and will attack their master If their isn't a higher stat(int/str) creature nearby
                        if (!me.GetFlag(FightMode.NoAllegiance))
                            // If this is a summon, it can't target its controller.
                            if (m == m_Mobile.SummonMaster)
                                continue;

                        // 10/15/2023, Adam: EVs and BSs don't attack other EVs and BSs from the same summon master.
                        //  Sort of an AI special, not sure how it works on OSI - but it's intuitive!
                        if (m is BaseCreature them && me.SummonMaster == them.SummonMaster)
                            continue;

                        // It also must abide by harmful spell rules.
                        if (!Server.Spells.SpellHelper.ValidIndirectTarget(m_Mobile.SummonMaster, m))
                            continue;

                        // Animated creatures cannot attack players directly.
                        if (m is PlayerMobile && m_Mobile.IsAnimatedDead)
                            continue;
                    }

                    // If we only want faction friends, make sure it's one.
                    if (bFacFriend && !m_Mobile.IsFriend(m))
                        continue;

                    // Same goes for faction enemies.
                    if (bFacFoe && !m_Mobile.IsEnemy(m, BaseCreature.RelationshipFilter.None))
                        continue;

                    // process conditional-attack AI to see if they should attack
                    if ((acqType & FightMode.All) == 0)
                    {
                        // Only acquire this mobile if it attacked us.
                        //	All conditional-attack mobiles respond to aggression
                        bool bValid = false;

                        // Aggressors and Aggressed
                        if (!bValid)
                            bValid = Fought.ContainsKey(m);

                        // even these conditional-attack mobiles can still have enemies like a faction, or team opposition
                        if (!bValid)
                            bValid = m_Mobile.IsEnemy(m, BaseCreature.RelationshipFilter.Faction);

                        // Okay, if we're not pissed off yet, attack if Evil
                        if ((acqType & FightMode.Evil) > 0 && !bValid)
                        {
                            if (m is BaseCreature bc && bc.Controlled && bc.ControlMaster != null)
                                bValid = (bc.ControlMaster.Karma < 0);
                            else
                                bValid = (m.Karma < 0);
                        }

                        #region Ethics & Faction checks
                        if (!bValid)
                            bValid = (m_Mobile.GetFactionAllegiance(m) == BaseCreature.Allegiance.Enemy || m_Mobile.GetEthicAllegiance(m) == BaseCreature.Allegiance.Enemy);
                        #endregion

                        // Okay, if we're not pissed off yet, attack if Criminal
                        if ((acqType & FightMode.Criminal) > 0 && !bValid)
                        {
                            if (m is BaseCreature && ((BaseCreature)m).Controlled && ((BaseCreature)m).ControlMaster != null)
                                bValid = (((BaseCreature)m).ControlMaster.Criminal);
                            else
                                bValid = (m.Criminal);
                        }

                        // Okay, if we're not pissed off yet, attack if Murderer
                        if ((acqType & FightMode.Murderer) > 0 && !bValid)
                        {
                            if (m is PlayerMobile)
                                //Adam: I don't think we should check Core.RedsInTown here.
                                bValid = (m as PlayerMobile).Red;
                        }

                        // let different AIs do special processing here
                        if (!bValid)
                            bValid = AIFocusMobCallback(m);

                        // Alright, noting here to piss-us-off, keep looking
                        if (!bValid)
                            continue;
                    }

                    // If it's an enemy factioned mobile, make sure we can be harmful to it.
                    if (bFacFoe && !bFacFriend && !m_Mobile.CanBeHarmful(m, false))
                        continue;

                    // faction friends are cool
                    if (IOBSystem.IsFriend(m_Mobile, m) == true && m_Mobile.IsTeamOpposition(m) == false)
                        continue;

                    //Pix: this is the case where a non-IOBAligned mob is trying to target a IOBAligned mob AND the IOBAligned mob is an IOBFollower
                    if (m is BaseCreature && IOBSystem.IsIOBAligned(m) && !IOBSystem.IsIOBAligned(m_Mobile) && (m as BaseCreature).IOBFollower == true)
                        continue;

                    #region Alignment
                    bool aligned = AlignmentSystem.Enabled && AlignmentSystem.IsAlly(m_Mobile, m, true, true);
                    if (aligned && !me.GetFlag(FightMode.NoAllegiance))
                    {
                        m_Mobile.DebugSay(DebugFlags.AI, string.Format("I will ignore {0} because he's one of us", m.Name == null ? m : m.Name));
                        continue;
                    }
                    else if (aligned)
                    {
                        m_Mobile.DebugSay(DebugFlags.AI, string.Format("I will attack {0} because I have no allegiance", m.Name == null ? m : m.Name));
                    }
                    #endregion

                    // do we have LOS?
                    if (m_Mobile.InLOS(m) == false)
                    {   //  IAIOkToInvestigate limits the time between investigations.
                        if (m_Mobile.IAIOkToInvestigate(m))
                            InvestigativeMemoryAI(m);
                        // regardless, we don't have LOS
                        //  we'll check InvestigativeMemory in Wander
                        continue;
                    }

                    // gotcha ya little bastard!
                    // let the Mobile make the final call here
                    newFocusMob = FocusDecisionAI(m);

                    if (newFocusMob != null && !CloseEnough(newFocusMob))
                    {
                        bool clearPath = false;
                        // we sort of punt on water mobs, we don't yet have a 'ClearPathLand' equivalent for water
                        if (m_Mobile.CanSwim && Utility.IsWater(m_Mobile.Map, m_Mobile.Location.X, m_Mobile.Location.Y, m_Mobile.Location.Z))
                            clearPath = true;
                        if (clearPath == false)
                            clearPath = Spawner.ClearPathLand(m_Mobile.Location, newFocusMob.Location, map: map, m_Mobile.CanOpenDoors);

                        if (!clearPath)
                        {   // keep looking. We'll only use this guy as a last resort
                            InaccessibleQueue.Enqueue(newFocusMob);
                            newFocusMob = null;
                            continue;
                        }
                    }

                    // exit as soon as we have a target. 
                    // this is both optimal and needed as we sorted our list in order of Preferred Targets
                    if (newFocusMob != null)
                        break;
                }

                // we have no choice but to use an Inaccessible mobile. Most AIs will ignore this guy anyway (not champs or guards.)
                if (newFocusMob == null && InaccessibleQueue.Count > 0)
                    newFocusMob = InaccessibleQueue.Peek();

                // the new focus mobile
                // if InaccessibleQueue doesn't contain newFocusMob, then the newFocusMob is accessible.
                if (newFocusMob != null && (m_Mobile.IgnoreCombatantAccessibility || !InaccessibleQueue.Contains(newFocusMob)))
                    m_Mobile.FocusMob = newFocusMob;
                else
                {
                    if (newFocusMob != null)
                        m_Mobile.DebugSay(DebugFlags.AI, string.Format("I will ignore {0} because he's inaccessible",
                            newFocusMob.Name != null ? newFocusMob.Name : newFocusMob.GetType().Name));
                    m_Mobile.FocusMob = null;
                }

                // we remember a few things about this mobile like last known location
                //	'memory' is used for smart mobiles that know how to reveal/detect hidden
                // we only remember for 10 seconds
                if (m_Mobile.FocusMob != null)
                    ShortTermMemory.Remember(m_Mobile.FocusMob, new Point3D(m_Mobile.FocusMob.Location), 10);
            }

            return (m_Mobile.FocusMob != null);
        }
        private bool CloseEnough(Mobile m)
        {
            //return m_Mobile.InRange(m, m_Mobile.RangeFight);
            return (m_Mobile.GetDistanceToSqrt(m) < 2.0 && Math.Abs(m_Mobile.Z - m.Z) < 2);
        }
        private bool Ignored(Mobile m, List<Mobile> list)
        {
            if (list != null)
                return list.Contains(m);
            return false;
        }
        public void InvestigativeMemoryAI(Mobile m)
        {   // do we remember this guy?
            Memory.ObjectMemory om = InvestigativeMemory.Recall(m as object);
            if (om == null)
            {   // nope, don't remember him. Let's remember him and whether we can path to him
                m_Mobile.DebugSay(DebugFlags.AI, string.Format("Remembering {0} in InvestigativeMemory.", m));

                // can we get there from here?
                Movement.MovementObject obj_start = new Movement.MovementObject(m_Mobile.Location, m_Mobile.IAIGetPoint(m), null);
                IPoint3D goal = obj_start.Goal;

                // get the target surface
                Spells.SpellHelper.GetSurfaceTop(ref goal);
                obj_start.Goal = new Point3D(goal.X, goal.Y, goal.Z);

                // can we get there?
                MovementPath path = new MovementPath(obj_start);
                if (path != null && path.Success)
                {   // we can path there
                    // Maybe we'll just mosey on over that way and see if things change
                    InvestigativeMemory.Remember(m, new KeyValuePair<MovementPath, int>(path, new int()), 30);
                }
                else
                    InvestigativeMemory.Remember(m, null, 30);
            }
            else
            {
                bool canPath = (om != null && om.Context != null);
                m_Mobile.DebugSay(DebugFlags.AI, string.Format("I remember {0}, {1} path to him.", m, canPath ? "and can" : "but cannot"));
                if (m_Mobile.IAIQuerySuccess(m))
                    m_Mobile.IAIResult(m, canPath: canPath);
                else
                    m_Mobile.IAIResult(null, canPath: false);
            }

            return;
        }

        public virtual Mobile FocusDecisionAI(Mobile m)
        {   // make any final decisions about attacking this mobile
            // This function is called by BaseAI when we've decided who is an attack candidate.
            // we let let the mobile make the final decision. By default, we just attack the 
            //  mobile we were scheduled to attack.
            //  Currently only used by Champions which have a NavBeacon and an objective.
            return this.m_Mobile.FocusDecisionAI(m);
        }

        public virtual bool StayAngryAtAggressors
        { get { return true; } }

        public virtual bool StayAngryAtAggressed
        { get { return true; } }

        public virtual bool AIFocusMobCallback(Mobile m)
        {
            return false;
        }

        public enum SortDirection
        {
            Ascending,
            Descending
        }

        public class AI_Sort : IComparer
        {
            private SortDirection m_direction = SortDirection.Ascending;
            private Mobile m;
            FightMode type;
            public AI_Sort(SortDirection direction, Mobile target, FightMode acqType)
            {
                m_direction = direction;
                m = target;
                type = acqType;
            }

            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// Value Condition Less than zero <paramref name="x"/> is less than <paramref name="y"/>. Zero <paramref name="x"/> equals <paramref name="y"/>. Greater than zero <paramref name="x"/> is greater than <paramref name="y"/>.
            /// </returns>
            /// <exception cref="T:System.ArgumentException">Neither <paramref name="x"/> nor <paramref name="y"/> implements the <see cref="T:System.IComparable"/> interface.-or- <paramref name="x"/> and <paramref name="y"/> are of different types and neither one can handle comparisons with the other. </exception>
            int IComparer.Compare(object x, object y)
            {
                Mobile mobileX = (Mobile)x;
                Mobile mobileY = (Mobile)y;

                if (mobileX == null && mobileY == null)
                {
                    return 0;
                }
                else if (mobileX == null && mobileY != null)
                {
                    return (this.m_direction == SortDirection.Ascending) ? -1 : 1;
                }
                else if (mobileX != null && mobileY == null)
                {
                    return (this.m_direction == SortDirection.Ascending) ? 1 : -1;
                }
                else
                {
                    switch (type)
                    {
                        case FightMode.Weakest:
                            {
                                return (this.m_direction == SortDirection.Ascending) ?
                                    mobileX.Hits.CompareTo(mobileY.Hits) :
                                    mobileY.Hits.CompareTo(mobileX.Hits);
                            }
                        case FightMode.Int:
                            {
                                return (this.m_direction == SortDirection.Ascending) ?
                                    mobileX.Int.CompareTo(mobileY.Int) :
                                    mobileY.Int.CompareTo(mobileX.Int);
                            }
                        case FightMode.Str: // same as FightMode.Strongest
                            {
                                return (this.m_direction == SortDirection.Ascending) ?
                                    mobileX.Str.CompareTo(mobileY.Str) :
                                    mobileY.Str.CompareTo(mobileX.Str);
                            }
                        case FightMode.Dex:
                            {
                                return (this.m_direction == SortDirection.Ascending) ?
                                    mobileX.Dex.CompareTo(mobileY.Dex) :
                                    mobileY.Dex.CompareTo(mobileX.Dex);
                            }
                        case FightMode.Closest:
                            {
                                return (this.m_direction == SortDirection.Ascending) ?
                                    mobileX.GetDistanceToSqrt(m).CompareTo(mobileY.GetDistanceToSqrt(m)) :
                                    mobileY.GetDistanceToSqrt(m).CompareTo(mobileX.GetDistanceToSqrt(m));
                            }
                        default:
                            {   // do not move list items unless you need to since this sort is called multiple times
                                //	to provide a compound sort
                                return 0;
                            }
                    }
                }
            }
        }
    }
}