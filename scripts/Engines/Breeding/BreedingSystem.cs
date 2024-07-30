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

/* Scripts\Engines\Breeding\BreedingSystem.cs
 * Changelog:
 * 2/14/22, Yoar
 *      When auto-tamed on hatch, the new owner is now properly added to the creature's owner list.
 * 12/17/21, Yoar
 *      Added [GrowProgress command to check the growth progress of the targeted creature.
 * 10/24/21, Yoar
 *      Removed LogStats, ensured that raw properties aren't changed during the breeding system conversions.
 * 10/23/21, Yoar
 *      Added LogStats: Logs the stats of pets prior to the breeding system conversions.
 * 10/22/21, Yoar
 *      Added [grow command.
 * 10/22/21, Yoar
 *      Fixed control master search logic when an egg hatched.
 * 10/20/21, Yoar: Breeding System overhaul
 *      Initial version. This class contains helper methods for the breeding system.
 */

using Server.Commands;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Engines.Breeding
{
    public enum Maturity : byte
    {
        Ageless = 0,

        Egg = 1,
        Infant,
        Child,
        Youth,
        Adult,
        Ancient,
    }

    public enum BreedingRole : byte
    {
        None,
        Male,
        Female,
    }

    public static class BreedingSystem
    {

        //public static bool Enabled { get { return true; } }

        public static bool Enabled { get { return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.BreedingEnabled); } }


        public static bool ChicksFindMaster = true; // if enabled, hatched chicks attempt to find a control master

        public static void Initialize()
        {
            TargetCommands.Register(new GrowCommand());
#if false
            TargetCommands.Register(new GrowProgressCommand());
#endif
        }

        public class GrowCommand : BaseCommand
        {
            public GrowCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "Grow" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "Grow";
                Description = "Grows-up the targeted creature by increasing its maturity.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                BaseCreature bc = obj as BaseCreature;

                if (bc == null)
                {
                    LogFailure("That is not a creature.");
                    return;
                }
                else if (bc.Maturity < Maturity.Egg || bc.Maturity > Maturity.Adult)
                {
                    LogFailure("Its maturity cannot be increased.");
                    return;
                }

                SetMaturity(bc, (Maturity)((int)bc.Maturity + 1));
            }
        }

#if false
        public class GrowProgressCommand : BaseCommand
        {
            public GrowProgressCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "GrowProgress" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "GrowProgress";
                Description = "Checks the growth progress of the targeted creature.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                BaseCreature bc = obj as BaseCreature;

                if (bc == null)
                {
                    LogFailure("That is not a creature.");
                    return;
                }
                else if (bc.Maturity < Maturity.Egg || bc.Maturity > Maturity.Adult)
                {
                    LogFailure("That doesn't grow.");
                    return;
                }

                e.Mobile.SendMessage("Growth progress: {0:F6} weeks.", GetGrowthProgress(bc));
            }
        }
#endif

        public static void OnHatch(BaseCreature bc)
        {
            if (bc.Backpack != null)
            {
                List<Item> items = bc.Backpack.Items;

                for (int i = items.Count - 1; i >= 0; i--)
                    ((Item)items[i]).Delete();
            }

            bc.BreedingParticipant = true;

            bc.Birthdate = DateTime.UtcNow;

            if (bc.Ageless)
                bc.Maturity = Maturity.Ageless;
            else
                SetMaturity(bc, Maturity.Infant);

            bc.OnHatch();

            if (ChicksFindMaster && bc.Tamable && !bc.Controlled)
            {
                Mobile closest = null;
                double distMin = double.MaxValue;

                foreach (Mobile m in bc.GetMobilesInRange(6))
                {
                    if (IsValidMaster(bc, m))
                    {
                        double dist = bc.GetDistanceToSqrt(m);

                        if (dist < distMin)
                        {
                            closest = m;
                            distMin = dist;
                        }
                    }
                }

                if (closest != null)
                {
                    bc.Owners.Add(closest);
                    bc.SetControlMaster(closest);
                    bc.IsBonded = false;
                }
            }
        }

        private static bool IsValidMaster(BaseCreature bc, Mobile m)
        {
            if (!m.Alive || !m.Player || m.NetState == null)
                return false;

            if (m.Female ? !bc.AllowFemaleTamer : !bc.AllowMaleTamer)
                return false;

            if (m.FollowerCount + bc.ControlSlots > m.FollowersMax)
                return false;

            if (m.Skills[SkillName.AnimalTaming].Value < bc.MinTameSkill)
                return false;

            if (!bc.CanSee(m) || !bc.InLOS(m))
                return false;

            return true;
        }

        public static void OnThink(BaseCreature bc)
        {
            CheckGrow(bc);

            MatingRitual.OnThink(bc);
        }

        private static void CheckGrow(BaseCreature bc)
        {
            if (!bc.BreedingParticipant || bc.Maturity == Maturity.Ageless || bc.Maturity == Maturity.Egg || bc.Maturity == Maturity.Ancient || DateTime.UtcNow < bc.NextGrowth)
                return;

            bc.NextGrowth = DateTime.UtcNow + TimeSpan.FromMinutes(30.0);

            double weeks = (DateTime.UtcNow - bc.Birthdate).TotalDays / 7.0;

            if (Core.UOTC_CFG)
                weeks *= 24.0; // speed up 24x

            // progress is a double between 0.0 and 1.0
            double progress = GetTrainingProgress(bc);

            bool grow = false;

            switch (bc.Maturity)
            {
                case Maturity.Infant: grow = (weeks * progress >= 1.0); break;
                case Maturity.Child: grow = (weeks * progress >= 3.0); break;
                case Maturity.Youth: grow = (weeks * progress >= 5.0); break;
                case Maturity.Adult: grow = (weeks / progress >= 50.0); break;
            }

            if (grow)
                SetMaturity(bc, (Maturity)((int)bc.Maturity + 1));
        }

        private static double GetTrainingProgress(BaseCreature bc)
        {
            double stats = 0.0;

            stats += (double)bc.RawStr / bc.StrMax;
            stats += (double)bc.RawInt / bc.IntMax;
            stats += (double)bc.RawDex / bc.DexMax;

            stats /= Genetics.GetValue(bc, "Versatility", 1.0);

            return stats;
        }

        public static void SetMaturity(BaseCreature bc, Maturity maturity)
        {
            Maturity oldMaturity = bc.Maturity;

            bc.Maturity = maturity;
            bc.OnGrowth(oldMaturity);
            bc.SuppressNormalLoot = true;
        }

        public static bool DoActionOverride(BaseCreature bc, bool obey)
        {
            if (MatingRitual.DoActionOverride(bc, obey))
                return true;

            return false;
        }

        public static void MatedWith(BaseCreature mom, BaseCreature dad, bool stillBorn)
        {
            BaseCreature child = null;

            try
            {
                child = (BaseCreature)Activator.CreateInstance(mom.GetType());
            }
            catch (Exception e)
            {
                Console.WriteLine("BreedingSystem Error: {0}", e);
            }

            if (child != null)
                Genetics.Recombination(child, mom, dad);

            Item egg = null;

            if (mom.EggType != null)
            {
                try
                {
                    egg = (BaseHatchableEgg)Activator.CreateInstance(mom.EggType, new object[] { child });
                }
                catch (Exception e)
                {
                    Console.WriteLine("BreedingSystem Error: {0}", e);
                }
            }
            else
            {
                // TODO: Pregnancy!
            }

            if (stillBorn)
            {
                child.Delete(); // rip
                child = null;
            }

            if (egg != null)
                egg.MoveToWorld(mom.Location, mom.Map);
            else if (child != null)
                child.MoveToWorld(mom.Location, mom.Map);

            // TODO: Logging?
        }

        public static bool OnDragDrop(BaseCreature bc, Mobile from, Item dropped)
        {
            if (bc.IsDeadPet)
                return false;

            if (bc.ControlMaster == from)
            {
                if (bc.EatsBP && dropped is BlackPearl)
                {
                    AnimateEat(bc);
                    bc.SayTo(from, "*{0} straightens up and seems more vibrant*", bc.Name);
                    bc.Items.Add(new TimedProperty(Use.BPBonus, null, TimeSpan.FromMinutes(5.0)));

                    dropped.Consume();
                    return dropped.Deleted;
                }

                if (bc.EatsSA && dropped is SulfurousAsh)
                {
                    AnimateEat(bc);
                    bc.SayTo(from, "*{0} gets a deep, fiery, gleam in {1} eyes*", bc.Name, bc.Female ? "her" : "his");
                    bc.Items.Add(new TimedProperty(Use.SABonus, null, TimeSpan.FromMinutes(5.0)));

                    dropped.Consume();
                    return dropped.Deleted;
                }
            }

            return false;
        }

        private static void AnimateEat(BaseCreature bc)
        {
            if (bc.Body.IsAnimal)
                bc.Animate(3, 5, 1, true, false, 0);
            else if (bc.Body.IsMonster)
                bc.Animate(17, 5, 1, true, false, 0);
        }

        public static void ScaleStats(BaseCreature bc, double scalar)
        {
            bc.RawStr = (int)(bc.RawStr * scalar);
            bc.RawInt = (int)(bc.RawInt * scalar);
            bc.RawDex = (int)(bc.RawDex * scalar);
        }

        public static void ModifyGainChance(BaseCreature bc, ref double gc)
        {
            if (!bc.BreedingParticipant)
                return;

            switch (bc.Maturity)
            {
                case Maturity.Child: gc *= 1.5; break;
                case Maturity.Youth: gc *= 5.0; break;
                case Maturity.Ancient: gc = 0.0; break;
            }
        }
    }
}