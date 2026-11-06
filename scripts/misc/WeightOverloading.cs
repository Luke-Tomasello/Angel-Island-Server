/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/***************************************************************************
 *
 * Mount Stamina System
 * --------------------
 *
 * Purpose:
 *   Reintroduces classic-style mount endurance by allowing mounts to tire
 *   from sustained running. Unlike standard RunUO, rider stamina is not
 *   tied to mount stamina; instead, each mount maintains and consumes its
 *   own stamina pool.
 *
 * Design Goals:
 *   - Make mount choice meaningful without making travel frustrating.
 *   - Preserve OSI-style stamina regeneration, feeding, and recovery.
 *   - Reward mount training naturally through improved Dexterity/Stamina.
 *   - Allow runtime tuning without code changes.
 *
 * Overview:
 *   While mounted and running, mounts periodically lose stamina. The rate
 *   of loss is determined by the mount's endurance multiplier and the
 *   global MountEnduranceFactor. As stamina declines, the rider receives
 *   warning messages indicating the mount is becoming fatigued.
 *
 * Endurance Model:
 *   Endurance is derived from the mount's physical stamina characteristics.
 *   Training a mount improves Dexterity, which increases StamMax and
 *   therefore overall endurance.
 *
 * Runtime Tuning:
 *   CoreAI.MountEnduranceFactor provides a global multiplier that can be
 *   adjusted live without code changes. This allows staff to tune overall
 *   travel times while preserving the relative differences between mount
 *   types.
 *
 * Recovery:
 *   - Standard stamina regeneration remains intact.
 *   - Feeding restores stamina.
 *   - "All Follow Me" provides a small stamina refresh based upon loyalty.
 *   - Standing and walking recover stamina normally.
 *
 ***************************************************************************/

/* Scripts/Misc/WeightOverloading.cs
 * ChangeLog
 *  3/11/23, Yoar
 *      Disabled mount stamina.
 *  3/11/23, Yoar
 *      Added AI/MO check to stamina damage customization
 *      Refactored movement event handler
 *  11/25/21, Yoar
 *      Implemented stamina drain scaling.
 * 7/16/21 - Liberation
 *      change: stamina drain from taking damage reduced for players
 *      reason: on-damage stam drain too severe
 *	6/28/04, Pix
 *		Tweaked to only drain low-on-stamina people when they're running.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using System;

namespace Server.Misc
{
    public enum DFAlgorithm
    {
        Standard,
        PainSpike
    }

    public class WeightOverloading
    {
        public static void Initialize()
        {
            EventSink.Movement += new MovementEventHandler(EventSink_Movement);
        }

        private static DFAlgorithm m_DFA;

        public static DFAlgorithm DFA
        {
            get { return m_DFA; }
            set { m_DFA = value; }
        }

        public static bool MountStamina
        {
            get
            {
                // 7/18/2026, Adam: tested against Season 2, and seemed good. Turning on everywhere
                return true;

                // 6/20/2026, Adam: just test here, if it work out, turn on everywhere
                //return (Core.RuleSets.SeasonsRules(2, 3) || Core.RuleSets.UpheavalRules());
                //                // 3/11/32 Yoar, Disabled mount stamina
                //#if false
                //                // no mount stamina on Angel Island/Mortalis
                //                return (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisRules());
                //#else
                //                return false;
                //#endif
            }
        }

        public static void FatigueOnDamage(Mobile m, int damage)
        {
            double fatigue = 0.0;

            switch (m_DFA)
            {
                case DFAlgorithm.Standard:
                    {
                        /* 8/6/21, Adam
                         *  Add the clause "&& m.Str <= 111" (100 Str + Str buff)
                         *  This puts superbeings in the same class as monsters and therefore reduces
                         *  Stam loss accordingly.
                         * 7/16/21 - Liberation
                         * change: stamina drain from taking damage reduced for players
                         * reason: on-damage stam drain too severe
                         */
                        if ((Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisStyleRules()) && m.Player && m.Str <= 120)
                        {
                            fatigue = (damage * ((double)m.HitsMax / 2 / m.Hits) * m.Stam / 100) - 5.0;
                            break;
                        }
                        /* end change */

                        fatigue = (damage * (100.0 / m.Hits) * ((double)m.Stam / 100)) - 5.0;
                        break;
                    }
                case DFAlgorithm.PainSpike:
                    {
                        fatigue = (damage * ((100.0 / m.Hits) + ((50.0 + m.Stam) / 100) - 1.0)) - 5.0;
                        break;
                    }
            }

            if (fatigue > 0)
                m.Stam -= (int)fatigue;
        }

        public const int OverloadAllowance = 4; // We can be four stones overweight without getting fatigued

        public static int GetMaxWeight(Mobile m)
        {
            return 40 + (int)(3.5 * m.Str);
        }

        public static void EventSink_Movement(MovementEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!from.Player || !from.Alive || from.AccessLevel >= AccessLevel.GameMaster)
                return;

            int mobWeight = Mobile.BodyWeight + from.TotalWeight;
            int maxWeight = GetMaxWeight(from) + OverloadAllowance;
            int overWeight = mobWeight - maxWeight;

            if (overWeight > 0)
            {
                from.Stam -= GetStamLoss(from, overWeight, (e.Direction & Direction.Running) != 0);

                if (from.Stam == 0)
                {
                    from.SendLocalizedMessage(500109); // You are too fatigued to move, because you are carrying too much weight!
                    e.Blocked = true;
                    return;
                }
            }

            BaseMount? mount = from.Mount as BaseMount;
            bool mounted = (mount != null);

            if (!MountStamina)
                mounted = false;

            if (!mounted && ((!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisStyleRules()) || (e.Direction & Direction.Running) != 0))
            {
                if (((from.Stam * 100) / Math.Max(from.StamMax, 1)) < 10)
                    --from.Stam;
            }

            if (mounted && mount.Stam == 0)
            {
                from.SendLocalizedMessage(500108); // Your mount is too fatigued to move.
                e.Blocked = true;
                return;
            }

            if (from.Stam == 0)
            {
                from.SendLocalizedMessage(500110); // You are too fatigued to move.
                e.Blocked = true;
                return;
            }

            if (from is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)from;
                ++pm.StepsTaken;

                if (!mounted)
                {
                    if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.StaminaDrainByWeight))
                    {
                        double baseDrain = 1.0 / (from.Mounted ? 48 : 16);

                        /* Drain stamina based on weight:
                         * 
                         * weight >= 150: factor = 1.00
                         * weight ==  75: factor = 0.75
                         * weight ==   0: factor = 0.50
                         */
                        double drainFactor;
                        if (mobWeight >= 150)
                            drainFactor = 1.0;
                        else
                            drainFactor = (1.0 + (Math.Max(0, mobWeight) - 150) / 300.0);

                        DrainStamina(pm, baseDrain * drainFactor);
                    }
                    else
                    {
                        int amt = (from.Mounted ? 48 : 16);
                        if ((pm.StepsTaken % amt) == 0)
                            --from.Stam;
                    }
                }
                else
                {
                    double multiplier = mount.MountEnduranceMultiplier * CoreAI.MountEnduranceFactor;
                    int drainEvery = Math.Max(1, (int)Math.Round(6.0 * multiplier));

                    if (((pm.StepsTaken % drainEvery) == 0) && ((e.Direction & Direction.Running) != 0))
                    {
                        --mount.Stam;

                        if (!MountStaminaWarning.Recall(from))
                        {
                            int pct = (mount.Stam * 100) / Math.Max(mount.StamMax, 1);

                            if (pct <= 10)
                            {
                                MountStaminaWarning.Remember(from, 3.0);
                                from.SendLocalizedMessage(500133); // Your mount is very fatigued.
                                Effects.PlaySound(from, from.Map, mount.GetAngerSound());
                            }
                            else if (pct <= 25)
                            {
                                MountStaminaWarning.Remember(from, 6.0);
                                from.SendMessage("Your mount is growing weary.");
                            }
                            else if (pct <= 50)
                            {
                                MountStaminaWarning.Remember(from, 10.0);
                                from.SendMessage("Your mount begins to tire.");
                            }
                        }
                    }
                }
            }
        }

        private static Memory MountStaminaWarning = new Memory();

        public static int GetStamLoss(Mobile from, int overWeight, bool running)
        {
            int loss = 5 + (overWeight / 25);

            if (from.Mounted)
                loss /= 3;

            if (running)
                loss *= 2;

            return loss;
        }

        public static bool IsOverloaded(Mobile m)
        {
            if (!m.Player || !m.Alive || m.AccessLevel >= AccessLevel.GameMaster)
                return false;

            return ((Mobile.BodyWeight + m.TotalWeight) > (GetMaxWeight(m) + OverloadAllowance));
        }

        public static void DrainStamina(PlayerMobile pm, double drain)
        {
            // enqueue stamina drain
            pm.StamDrain += drain;

            //pm.SendMessage("Stam drain: {0:F2}", pm.StamDrain);

            if (pm.StamDrain >= 1.0)
            {
                // process stamina drain
                int iDrain = (int)pm.StamDrain;
                pm.StamDrain -= iDrain;
                pm.Stam -= iDrain;
            }
        }
        public static double GetMountEnduranceMultiplier(BaseMount mount)
        {
            if (mount == null)
                return 1.0;

            const double MinMultiplier = 1.40;
            const double MaxMultiplier = 1.60;

            const double MinScore = 60.0;   // average low-end horse/llama neighborhood
            const double MaxScore = 150.0;  // trained / high-end mount neighborhood

            double score = GetMountEnduranceScore(mount);

            double normalized = (score - MinScore) / (MaxScore - MinScore);

            normalized = Math.Max(0.0, normalized);
            normalized = Math.Min(1.0, normalized);

            return MinMultiplier + ((MaxMultiplier - MinMultiplier) * normalized);
        }

        public static double GetMountEnduranceScore(BaseMount mount)
        {
            if (mount == null)
                return 0.0;

            double stam = Math.Max(1, mount.StamMax);
            double str = Math.Max(1, mount.Str);

            // Travel endurance model:
            // - StamMax is the primary signal.
            // - Strength contributes only as a small compressed bonus.
            // - Do not use Power here; Power is a combat/spawn-budget metric and
            //   creates excessive gaps between ordinary and elite mounts.
            double strengthBonus = Math.Sqrt(str) * 1.5;

            return stam + strengthBonus;
        }
    }
}