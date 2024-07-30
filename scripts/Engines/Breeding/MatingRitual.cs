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

/* Scripts\Engines\Breeding\MatingRitual.cs
 * Changelog:
 *  5/29/2024, Adam: cast bc.Hits to double in the following check:
 *      if (DateTime.UtcNow < m_NextSearch || (double)bc.Hits / bc.HitsMax < HealthThreshold)
 *      Apparently, to C# bc.Hits / bc.HitsMax == integer 0 if bc.Hits < bc.HitsMax.
 *          0 is THEN cast to double for the comparison to HealthThreshold (0.8)
 *      By casting bc.Hits to double, the whole equation is floating point and the comparison to HealthThreshold does what it's supposed to.
 * 10/20/21, Yoar: Breeding System overhaul
 *      Initial version. This class deals with the mating ritual. This is not an AI,
 *      technically. It's simply an attachment to BaseCreature. Its methods are called
 *      in BaseCreature.DoActionOverride.
 *      
 *      Changes from Taren's mating ritual:
 *      - Removed the 2 min. male/female idle delay. It turned out to be too much
 *        trouble to implement for what it's worth. Instead, males now also have to
 *        wait the search delay *before* the first search.
 *      - Boosted the bonuses of black pearl and sulfurous ash from x1.25 to x2.00.
 *      - Simplified the mating success chance calculation for nicer numbers.
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Engines.Breeding
{
    public enum MatingStage : byte
    {
        Searching,
        Approaching,
        Competition,
        MovingIn,
        TheNasty
    }

    [PropertyObject]
    public class MatingRitual
    {
        public static int SearchDelayMin = 20; // min time between two consecutive searches (in seconds)
        public static int SearchDelayMax = 45; // max time between two consecutive searches (in seconds)
        public static TimeSpan MatingTimeout = TimeSpan.FromMinutes(5.0); // stop mating if it takes this long
        public static TimeSpan TheNastyDuration = TimeSpan.FromSeconds(10.0); // we must be doing the nasty for this long
        public static double HealthThreshold = 0.80; // health requirement for mating
        public static double AttemptChance = 0.10; // base chance to begin mating
        public static double FightChance = 0.50; // base chance to fight competition
        public static double AcceptChance = 0.60; // base chance to accept being mating
        public static double SuccessChance = 0.25; // base chance to succeed in mating
        public static double StillBornChance = 0.05; // chance for a still born
        public static double BPBonusFactor = 2.00; // Black Pearl bonus factor (Yoar: changed from 1.25 to 2.00)
        public static double SABonusFactor = 2.00; // Sulfurous Ash bonus factor (Yoar: changed from 1.25 to 2.00)

        private static readonly List<MatingRitual> m_Registry = new List<MatingRitual>();

        public static void Initialize()
        {
            EventSink.WorldSave += new WorldSaveEventHandler(EventSink_OnWorldSave);
        }

        private static void EventSink_OnWorldSave(WorldSaveEventArgs e)
        {
            for (int i = m_Registry.Count - 1; i >= 0; i--)
            {
                MatingRitual instance = m_Registry[i];

                if (!CanBreed(instance.Male) || instance.Male.GetBreedingRole() != BreedingRole.Male)
                {
                    instance.Reset();

                    instance.Male.MatingRitual = null; // TODO: Recycling?

                    m_Registry.RemoveAt(i);
                }
            }
        }

        public static void OnThink(BaseCreature bc)
        {
            if (bc.MatingRitual == null && CanBreed(bc) && bc.GetBreedingRole() == BreedingRole.Male)
                bc.MatingRitual = new MatingRitual(bc);
        }

        public static bool DoActionOverride(BaseCreature bc, bool obey)
        {
            return bc.MatingRitual != null && bc.MatingRitual.DoAction(bc, obey);
        }

        public static bool IsFightingCompetition(BaseCreature bc)
        {
            return bc.MatingRitual != null && bc.MatingRitual.Male == bc && bc.MatingRitual.Stage == MatingStage.Competition;
        }

        private BaseCreature m_Male;
        private BaseCreature m_Female;
        private MatingStage m_Stage;
        private DateTime m_NextSearch;
        private DateTime m_BeganMating;
        private DateTime m_BeganTheNasty;
        private int m_FightRange;
        private List<Mobile> m_Ignore;

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Male { get { return m_Male; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Female { get { return m_Female; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public MatingStage Stage
        {
            get { return m_Stage; }
            set { m_Stage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextSearch
        {
            get { return m_NextSearch; }
            set { m_NextSearch = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSearchIn
        {
            get
            {
                TimeSpan ts = m_NextSearch - DateTime.UtcNow;

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
            set { m_NextSearch = DateTime.UtcNow + value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime BeganMating
        {
            get { return m_BeganMating; }
            set { m_BeganMating = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime BeganTheNasty
        {
            get { return m_BeganTheNasty; }
            set { m_BeganTheNasty = value; }
        }

        private MatingRitual(BaseCreature bc)
        {
            m_Male = bc;

            m_Registry.Add(this);
        }

        private void Reset()
        {
            if (m_Female != null)
                m_Female.MatingRitual = null;

            m_Stage = MatingStage.Searching;
            m_NextSearch = DateTime.MinValue;
        }

        private bool DoAction(BaseCreature bc, bool obey)
        {
            bool continueMating = false;
            bool overrideAction = false;
            bool success = false;

            if (Validate())
            {
                continueMating = true;

                if (m_Stage != MatingStage.Searching)
                {
                    Utility.BreedingLog(bc, null, $"DoAction: {m_Stage.ToString()}");
                    LogCommonFailures(bc, obey);
                }

                switch (m_Stage)
                {
                    case MatingStage.Searching: DoStageSearching(bc, obey, ref continueMating, ref overrideAction); break;
                    case MatingStage.Approaching: DoStageApproaching(bc, obey, ref continueMating, ref overrideAction); break;
                    case MatingStage.Competition: DoStageCompetition(bc, obey, ref continueMating, ref overrideAction); break;
                    case MatingStage.MovingIn: DoStageMovingIn(bc, obey, ref continueMating, ref overrideAction); break;
                    case MatingStage.TheNasty: DoStageTheNasty(bc, obey, ref continueMating, ref overrideAction, ref success); break;

                    default: continueMating = false; break; // sanity
                }
            }
            else
            {

            }

            if (!continueMating)
            {
                if (m_Stage != MatingStage.Searching)
                {
                    if (success == true)
                        Utility.BreedingLog(bc, null, $"DoAction: {m_Stage.ToString()} succeeded.");
                    else
                        Utility.BreedingLog(bc, null, $"DoAction: {m_Stage.ToString()} failed.");
                }
                Reset();
            }

            return overrideAction;
        }
        private void LogCommonFailures(BaseCreature bc, bool obey)
        {
            bool will_fail = false;
            if (obey)
            {
                if (bc.ControlOrder != OrderType.None)
                {
                    Utility.BreedingLog(bc, null, $"DoAction: has a ControlOrder {bc.ControlOrder.ToString()}. Needs to be {OrderType.None.ToString()}");
                    will_fail = true;
                }
                if (bc.Combatant != null)
                {
                    Utility.BreedingLog(bc, null, $"DoAction: has a Combatant {bc.Combatant}.");
                    will_fail = true;
                }
            }
            else
            {
                if (bc.AIObject != null)
                {
                    ActionType action = bc.AIObject.Action;
                    if (!IsAction(bc, ActionType.Wander))
                    {
                        Utility.BreedingLog(bc, null, $"DoAction: Action type is {action.ToString()}. Needs to be {ActionType.Wander.ToString()}");
                        will_fail = true;
                    }
                }
            }

            if (will_fail)
                Utility.BreedingLog(bc, null, $"DoAction obey: {obey}.");
        }
        private void LogCommonFailures(BaseCreature bc, BaseCreature other)
        {
            if (!CanBreed(other))
            {
                if (!other.BreedingParticipant)
                    Utility.BreedingLog(bc, other, $"CommonFailures: they are not enrolled in the breeding system.");

                if (DateTime.UtcNow < other.NextMating)
                    Utility.BreedingLog(bc, other, $"CommonFailures: they are not ready to breed yet. Can breed in {other.NextMating - DateTime.UtcNow}");

                if (!((bc.Maturity == Maturity.Adult || bc.Maturity == Maturity.Ageless) && bc.Controlled))
                    Utility.BreedingLog(bc, null, $"CommonFailures: not a controlled creature of age. Must be Controlled and Adult or Ageless");
            }
            else if (other.GetBreedingRole() != BreedingRole.Female)
                Utility.BreedingLog(other, null, $"CommonFailures: {other}.GetBreedingRole() is {other.GetBreedingRole().ToString()}, must be {BreedingRole.Female.ToString()}");
            else if (!bc.CanBreedWith(other))
            {
                // handled in CanBreedWith (sister check)
            }
        }
        private bool BadHealth(Mobile m)
        {
            // 5/29/2024, Adam: cast bc.Hits to double. Apparently, to C# bc.Hits / bc.HitsMax == integer 0 if bc.Hits < bc.HitsMax.
            //  0 is THEN cast to double for the comparison to HealthThreshold (0.8)
            //  By casting bc.Hits to double, the whole equation is floating point and the comparison to HealthThreshold does what it's supposed to.
            return (double)m.Hits / m.HitsMax < HealthThreshold;
        }
        private void DoStageSearching(BaseCreature bc, bool obey, ref bool continueMating, ref bool overrideAction)
        {
            if (obey ? (bc.ControlOrder != OrderType.None || bc.Combatant != null) : !IsAction(bc, ActionType.Wander))
            {
                continueMating = false;
                return;
            }

            if (bc != m_Male)
                return;

            if (m_NextSearch == DateTime.MinValue)
                m_NextSearch = DateTime.UtcNow + RandomTimeSpan(SearchDelayMin, SearchDelayMax);

            if (DateTime.UtcNow < m_NextSearch || BadHealth(bc))
            {
                if (BadHealth(bc))
                    Utility.BreedingLog(bc, null, $"DoStageSearching: Health is too low. Must be at least {bc.HitsMax * HealthThreshold} hits.");
                return;
            }

            m_NextSearch = DateTime.UtcNow + RandomTimeSpan(SearchDelayMin, SearchDelayMax);

            bc.DebugSay(DebugFlags.AI, "Love shack... In the looove shack...");

            BaseCreature closest = null;
            double distMin = double.MaxValue;

            foreach (Mobile m in bc.GetMobilesInRange(bc.RangePerception))
            {
                if (m is BaseCreature other && other != bc && other.Alive)
                {
                    if (BadHealth(other))
                    {
                        Utility.BreedingLog(null, other, $"DoStageSearching: Health is too low. Must be at least {other.HitsMax * HealthThreshold} hits.");
                        continue;
                    }

                    double dist = bc.GetDistanceToSqrt(other);

                    if (dist > bc.RangePerception) // TODO: LOS check?
                    {
                        Utility.BreedingLog(bc, other, $"DoStageSearching result: too far");
                        continue;
                    }

                    if (!CanBreed(other) || other.GetBreedingRole() != BreedingRole.Female || !bc.CanBreedWith(other))
                    {
                        LogCommonFailures(bc, other);
                        continue;
                    }

                    if (dist < distMin)
                    {
                        closest = other;
                        distMin = dist;
                    }
                }
            }

            if (closest == null)
            {
                bc.DebugSay(DebugFlags.AI, "Man, it's a sausage fest around here...");
                return;
            }

            bc.DebugSay(DebugFlags.AI, "A woman!");
            Utility.BreedingLog(bc, closest, $"DoStageSearching result: found a female");

            double attemptChance = AttemptChance;
            Utility.BreedingLog(bc, closest, $"DoStageSearching AttemptChance: {attemptChance}");

            if (Property.FindUse(bc, Use.SABonus))
                attemptChance *= SABonusFactor;

            Utility.BreedingLog(bc, closest, $"DoStageSearching AttemptChance (after sulfurous ash bonus, if any): {attemptChance}");
            bc.DebugSay(DebugFlags.AI, $"Chance (after sulfurous ash bonus, if any): {attemptChance}");

            if (Core.UOTC_CFG || Utility.RandomDouble() < attemptChance)
            {
                bc.DebugSay(DebugFlags.AI, "Found a mate I like! Trying to mate...");
                Utility.BreedingLog(bc, closest, $"DoStageSearching AttemptChance Success");
                bc.PlaySound(bc.GetAngerSound());
                m_Female = closest;
                m_Stage = MatingStage.Approaching;
                m_BeganMating = DateTime.UtcNow;
                overrideAction = true;
                return;
            }
            else
                Utility.BreedingLog(bc, closest, $"DoStageSearching AttemptChance Fail");

            bc.DebugSay(DebugFlags.AI, "But I'm too scared to approach...");
        }

        private void DoStageApproaching(BaseCreature bc, bool obey, ref bool continueMating, ref bool overrideAction)
        {
            if (obey ? (bc.ControlOrder != OrderType.None || bc.Combatant != null) : !IsAction(bc, ActionType.Wander))
            {
                continueMating = false;
                return;
            }

            if (bc != m_Male)
                return;

            if (DateTime.UtcNow >= m_BeganMating + MatingTimeout)
            {
                bc.DebugSay(DebugFlags.AI, "F it. This broad's too much work.");
                continueMating = false;
                return;
            }

            if (!WalkMobileRange(bc, m_Female, 1, false, 0, 1))
            {
                bc.DebugSay(DebugFlags.AI, "Approaching...");
                overrideAction = true;
                return;
            }

            m_FightRange = Math.Max(-1, -1 + (int)Math.Round((bc.Temper + Utility.RandomMinMax(-10, 10)) / 35.0));

            if (m_FightRange >= 0)
                m_Stage = MatingStage.Competition;
            else
                m_Stage = MatingStage.MovingIn;

            overrideAction = true;
        }

        private void DoStageCompetition(BaseCreature bc, bool obey, ref bool continueMating, ref bool overrideAction)
        {
            if (obey ? (bc.ControlOrder != OrderType.None) : (!IsAction(bc, ActionType.Wander) && !IsAction(bc, ActionType.Combat)))
            {
                continueMating = false;
                return;
            }

            if (bc != m_Male)
                return;

            if (DateTime.UtcNow >= m_BeganMating + MatingTimeout)
            {
                bc.DebugSay(DebugFlags.AI, "F it. This broad's too much work.");
                continueMating = false;
                return;
            }

            Mobile c = bc.Combatant;

            if (c != null && c.Alive && c.Map == bc.Map && c.GetDistanceToSqrt(bc) <= m_FightRange)
            {
                bc.DebugSay(DebugFlags.AI, "Fighting competition");
                return;
            }

            bc.DebugSay(DebugFlags.AI, "Looking for competition...");

            if (m_Ignore == null)
                m_Ignore = new List<Mobile>();

            foreach (Mobile m in m_Female.GetMobilesInRange(m_FightRange)) // TODO: Check competition from shortest distance to longest distance?
            {
                if (m == m_Male || m == m_Female || !m.Alive || m.GetDistanceToSqrt(m_Female) > m_FightRange || m_Ignore.Contains(m)) // TODO: LOS check?
                    continue;

                BaseCreature other = m as BaseCreature;

                if (other == null || other.GetBreedingRole() != BreedingRole.Male || !bc.CompetesWith(other))
                    continue;

                double fightChance = FightChance;

                fightChance += bc.Temper / 20.0;
                fightChance -= bc.Wisdom / 20.0;
                fightChance -= (other.DamageMin + other.DamageMax) / 20.0;

                if (Core.UOTC_CFG || Utility.RandomDouble() < fightChance)
                {
                    bc.DebugSay(DebugFlags.AI, "Get away from my woman!");
                    bc.Combatant = other;
                    return;
                }

                m_Ignore.Add(m);
            }

            bc.DebugSay(DebugFlags.AI, "All clear, going back to the bedroom");
            m_Stage = MatingStage.MovingIn;
            overrideAction = true;
        }

        private void DoStageMovingIn(BaseCreature bc, bool obey, ref bool continueMating, ref bool overrideAction)
        {
            if (obey ? (bc.ControlOrder != OrderType.None || bc.Combatant != null) : !IsAction(bc, ActionType.Wander))
            {
                continueMating = false;
                return;
            }

            if (bc != m_Male)
                return;

            if (DateTime.UtcNow >= m_BeganMating + MatingTimeout)
            {
                bc.DebugSay(DebugFlags.AI, "F it. This broad's too much work.");
                continueMating = false;
                return;
            }

            if (!WalkMobileRange(bc, m_Female, 1, false, 0, 0))
            {
                bc.DebugSay(DebugFlags.AI, "Gettin in close...");
                overrideAction = true;
                return;
            }

            double acceptChance = AcceptChance;

            // male's wisdom can up chances up to .10, our patience can up chances up to .05, our temper can lower chances up to .15
            acceptChance += bc.Wisdom * 1000.0;
            acceptChance += m_Female.Patience / 2000.0;
            acceptChance -= m_Female.Temper * 3.0 / 2000.0;

            if (Core.UOTC_CFG || Utility.RandomDouble() < acceptChance)
            {
                m_Stage = MatingStage.TheNasty;
                m_BeganTheNasty = DateTime.UtcNow;
                m_Female.MatingRitual = this;
                overrideAction = true;
                return;
            }

            bc.DebugSay(DebugFlags.AI, "Shit! Rejected!");
            m_Female.PlaySound(m_Female.GetAngerSound());
            continueMating = false;
        }

        private void DoStageTheNasty(BaseCreature bc, bool obey, ref bool continueMating, ref bool overrideAction, ref bool success)
        {
            if (obey ? (bc.ControlOrder != OrderType.None || bc.Combatant != null) : !IsAction(bc, ActionType.Wander))
            {
                continueMating = false;
                return;
            }

            if (bc != m_Male)
            {
                if (Utility.RandomDouble() < 0.30)
                    bc.Moan();

                overrideAction = true;
                return;
            }

            if (DateTime.UtcNow < m_BeganTheNasty + TheNastyDuration)
            {
                bc.DebugSay(DebugFlags.AI, "Get down tonight...");

                if (Utility.RandomDouble() < 0.30)
                    bc.Moan();

                overrideAction = true;
                return;
            }

            double successChance = SuccessChance;

            if (Property.FindUse(bc, Use.BPBonus))
                successChance *= BPBonusFactor;

            successChance += bc.Patience / 500.0; // Yoar

            if (Core.UOTC_CFG || Utility.RandomDouble() < successChance)
            {
                bc.DebugSay(DebugFlags.AI, "Smokin a cig...");
                bc.Moan();
                bc.NextMating = DateTime.UtcNow + bc.GetBreedingDelay();
                m_Female.NextMating = DateTime.UtcNow + m_Female.GetBreedingDelay();
                BreedingSystem.MatedWith(m_Female, bc, Utility.RandomDouble() < StillBornChance); // note: order of input arguments is intentional
                success = true;
            }
            else
            {
                bc.DebugSay(DebugFlags.AI, "Crap, 'sploded early.");
                bc.PlaySound(bc.GetDeathSound());
            }

            continueMating = false;
            overrideAction = true;
        }

        private bool Validate()
        {
            if (!CanBreed(m_Male))
            {
                Utility.BreedingLog(m_Male, null, $"Validate: the male is unable to breed (at this time)");
                return false; // the male is unable to breed (at this time)
            }

            if (m_Male.GetBreedingRole() != BreedingRole.Male)
            {
                Utility.BreedingLog(m_Male, null, $"Validate: mismatching breeding role");
                return false; // mismatching breeding role
            }

            if (m_Stage == MatingStage.Searching)
                // no log
                return true; // the male is still searching...

            if (m_Female == null)
                // no log
                return false; // there is no female

            if (BadHealth(m_Female))
            {
                Utility.BreedingLog(m_Male, null, $"Validate: the female is not healthy enough to breed");
                return false; // the female is not healthy enough to breed
            }

            if (m_Male.Map != m_Female.Map || !m_Male.InRange(m_Female, 18))
                // no log
                return false; // the female is too far away from the male

            // TODO: LOS check?

            if (!CanBreed(m_Female))
            {
                Utility.BreedingLog(m_Male, null, $"Validate: the female is unable to breed (at this time)");
                return false; // the female is unable to breed (at this time)
            }

            if (m_Female.GetBreedingRole() != BreedingRole.Female)
            {
                Utility.BreedingLog(m_Male, null, $"Validate: mismatching breeding role");
                return false; // mismatching breeding role
            }

            if (!m_Male.CanBreedWith(m_Female))
            {
                Utility.BreedingLog(m_Male, null, $"Validate: the male can't breed with the female");
                return false; // the male can't breed with the female
            }

            return true;
        }

        private static bool CanBreed(BaseCreature bc)
        {
            if (bc.Deleted || !bc.Alive || bc.Map == null || bc.Map == Map.Internal)
                return false; // sanity

            if (!bc.BreedingParticipant)
                return false; // we are not enrolled in the breeding system

            if (DateTime.UtcNow < bc.NextMating)
            {
                Utility.BreedingLog(bc, null, $"CanBreed: Can breed in {bc.NextMating - DateTime.UtcNow}");
                return false; // we are not ready to breed yet
            }

            if (!bc.CanBreed())
                return false; // we can't breed

            return true;
        }

        private static bool IsAction(BaseCreature bc, ActionType action)
        {
            return bc.AIObject != null && bc.AIObject.Action == action;
        }

        private static bool WalkMobileRange(BaseCreature bc, Mobile m, int iSteps, bool bRun, int iWantDistMin, int iWantDistMax)
        {
            if (bc.AIObject != null)
                return bc.AIObject.WalkMobileRange(m, iSteps, bRun, iWantDistMin, iWantDistMax);

            return false;
        }

        private static TimeSpan RandomTimeSpan(int secondsMin, int secondsMax)
        {
            return TimeSpan.FromSeconds(Utility.RandomMinMax(secondsMin, secondsMax));
        }

        public override string ToString()
        {
            return "...";
        }
    }
}