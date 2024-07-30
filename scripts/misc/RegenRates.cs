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

/* Scripts/Misc/RegenRates.cs
 * CHANGELOG
 *	11/20/06 Taran Kain
 *		Moved Hits, StamRegenRate logic to Mobile.
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 *	8/9/04 - Pixie
 *		Fixed problem with exception on server load.  If someone is wearing some
 *		armor and they are regenerating mana, there's a possibility that the 
 *		armor hasn't loaded yet, so when the person is loaded, the server goes 
 *		to calculate the mediation rate (based on the unloaded armor) and throws
 *		an exception.  I added a check to make sure that the values we check exist
 *		before continuing in GetArmorMeditationValue()
 *	6/27/04 - Pixie
 *		Added modification to stamina rate so that stamina rate is affected by how hungry you are.
 *	6/20/04 - Pixie
 *		Added try/catch in GetArmorOffset and debugging if something is caught to try to
 *		determine the cause of an unreproducable exception.  If we get the exception again,
 *		this should help to find the cause. :-)
 */

using Server.Diagnostics;
using Server.Items;
using System;

namespace Server.Misc
{
    public class RegenRates
    {
        [CallPriority(10)]
        public static void Configure()
        {
            /*Mobile.DefaultHitsRate = TimeSpan.FromSeconds( 11.0 );
			Mobile.DefaultStamRate = TimeSpan.FromSeconds(  7.0 );
			Mobile.DefaultManaRate = TimeSpan.FromSeconds(  7.0 );

			*/
            Mobile.ManaRegenRateHandler = new RegenRateHandler(Mobile_ManaRegenRate);
            /*
						if ( Core.AOS )
						{
							Mobile.StamRegenRateHandler = new RegenRateHandler( Mobile_StamRegenRate );
							Mobile.HitsRegenRateHandler = new RegenRateHandler( Mobile_HitsRegenRate );
						}

						//Pix: our stamina regen rate figure outer
						Mobile.StamRegenRateHandler = new RegenRateHandler( Mobile_AngelIslandStamRegenRate );*/

        }

        private static void CheckBonusSkill(Mobile m, int cur, int max, SkillName skill)
        {
            if (!m.Alive)
                return;

            double n = (double)cur / max;
            double v = Math.Sqrt(m.Skills[skill].Value * 0.005);

            n *= (1.0 - v);
            n += v;

            m.CheckSkill(skill, n, contextObj: new object[2]);
        }

        private static bool CheckTransform(Mobile m, Type type)
        {
            //return TransformationSpell.UnderTransformation( m, type );
            return false;
        }

        private static TimeSpan Mobile_HitsRegenRate(Mobile from)
        {
            int points = AosAttributes.GetValue(from, AosAttribute.RegenHits);

            //if ( CheckTransform( from, typeof( HorrificBeastSpell ) ) )
            //points += 20;

            if (points < 0)
                points = 0;

            return TimeSpan.FromSeconds(1.0 / (0.1 * (1 + points)));
        }

        //		private static TimeSpan Mobile_AngelIslandStamRegenRate( Mobile from )
        //		{
        //			if ( from.Skills == null )
        //				return TimeSpan.Zero; //Mobile.DefaultStamRate;
        //
        //			if( from is Server.Mobiles.PlayerMobile )
        //			{
        //				int hunger = from.Hunger; //should be somewhere from 0 to 20
        //
        //				double maxFoodBonus = 3.5; //Seconds maximum quicker to gain stamina
        //
        //				TimeSpan foodbonus = TimeSpan.FromSeconds(maxFoodBonus * hunger / 20);
        //
        //				if( foodbonus > TimeSpan.FromSeconds(maxFoodBonus) )
        //				{
        //					foodbonus = TimeSpan.FromSeconds(maxFoodBonus);
        //				}
        //
        //				return Mobile.DefaultStamRate - foodbonus;
        //			}
        //			else
        //			{
        //				return Mobile.DefaultStamRate;
        //			}
        //		}
        //
        //		private static TimeSpan Mobile_StamRegenRate( Mobile from )
        //		{
        //			if ( from.Skills == null )
        //				return Mobile.DefaultStamRate;
        //
        //			CheckBonusSkill( from, from.Stam, from.StamMax, SkillName.Focus );
        //
        //			int points = AosAttributes.GetValue( from, AosAttribute.RegenStam ) +
        //				(int)(from.Skills[SkillName.Focus].Value * 0.1);
        //
        //			//if ( CheckTransform( from, typeof( VampiricEmbraceSpell ) ) )
        //				//points += 15;
        //
        //			if ( points < -1 )
        //				points = -1;
        //
        //			return TimeSpan.FromSeconds( 1.0 / (0.1 * (2 + points)) );
        //		}

        private static TimeSpan Mobile_ManaRegenRate(Mobile from)
        {
            if (from.Skills == null)
                return TimeSpan.FromSeconds(7.0);

            if (!from.Meditating)
                CheckBonusSkill(from, from.Mana, from.ManaMax, SkillName.Meditation);

            double rate;
            double armorPenalty = GetArmorOffset(from);

            if (Core.RuleSets.AOSRules())
            {
                double medPoints = from.Int + (from.Skills[SkillName.Meditation].Value * 3);

                medPoints *= (from.Skills[SkillName.Meditation].Value < 100.0) ? 0.025 : 0.0275;

                //CheckBonusSkill(from, from.Mana, from.ManaMax, SkillName.Focus);

                //double focusPoints = (int)(from.Skills[SkillName.Focus].Value * 0.05);
                double focusPoints = 0.0;

                if (armorPenalty > 0)
                    medPoints = 0; // In AOS, wearing any meditation-blocking armor completely removes meditation bonus

                double totalPoints = AosAttributes.GetValue(from, AosAttribute.RegenMana) +
                    focusPoints + medPoints + (from.Meditating ? (medPoints > 13.0 ? 13.0 : medPoints) : 0.0);

                //if ( CheckTransform( from, typeof( VampiricEmbraceSpell ) ) )
                //totalPoints += 3;

                //else if ( CheckTransform( from, typeof( LichFormSpell ) ) )
                //totalPoints += 13;

                if (totalPoints < -1)
                    totalPoints = -1;

                rate = 1.0 / (0.1 * (2 + (int)totalPoints));
            }
            else
            {
                double medPoints = (from.Int + from.Skills[SkillName.Meditation].Value) * 0.5;

                if (medPoints <= 0)
                    rate = 7.0;
                else if (medPoints <= 100)
                    rate = 7.0 - (239 * medPoints / 2400) + (19 * medPoints * medPoints / 48000);
                else if (medPoints < 120)
                    rate = 1.0;
                else
                    rate = 0.75;

                rate += armorPenalty;

                if (from.Meditating)
                    rate *= 0.5;

                if (rate < 0.5)
                    rate = 0.5;
                else if (rate > 7.0)
                    rate = 7.0;
            }

            return TimeSpan.FromSeconds(rate);
        }

        private static double GetArmorOffset(Mobile from)
        {
            double rating = 0.0;

            try
            {
                rating += GetArmorMeditationValue(from.ShieldArmor as BaseArmor);
                rating += GetArmorMeditationValue(from.NeckArmor as BaseArmor);
                rating += GetArmorMeditationValue(from.HandArmor as BaseArmor);
                rating += GetArmorMeditationValue(from.HeadArmor as BaseArmor);
                rating += GetArmorMeditationValue(from.ArmsArmor as BaseArmor);
                rating += GetArmorMeditationValue(from.LegsArmor as BaseArmor);
                rating += GetArmorMeditationValue(from.ChestArmor as BaseArmor);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("NONFATAL Exception caught in Server.Misc.RegenRates.GetArmorOffset.");
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine(e.StackTrace.ToString());
                System.Console.WriteLine("SEND THIS OUTPUT TO PIXIE PLEASE!!");
                System.Console.WriteLine("Shield: " + from.ShieldArmor);
                System.Console.WriteLine("Neck: " + from.NeckArmor);
                System.Console.WriteLine("Hand: " + from.HandArmor);
                System.Console.WriteLine("Head: " + from.HeadArmor);
                System.Console.WriteLine("Arms: " + from.ArmsArmor);
                System.Console.WriteLine("Legs: " + from.LegsArmor);
                System.Console.WriteLine("Chest: " + from.ChestArmor);
            }

            return rating / 4;
        }

        private static double GetArmorMeditationValue(BaseArmor ar)
        {
            if (ar == null /*|| 
				ar.ArmorAttributes == null ||
				ar.ArmorAttributes.MageArmor != 0 || 
				ar.Attributes == null ||
				ar.Attributes.SpellChanneling != 0*/
                                                     )
            {
                return 0.0;
            }

            switch (ar.MeditationAllowance)
            {
                default:
                case ArmorMeditationAllowance.None:
                    {
                        return ar.ArmorRating;
                    }
                case ArmorMeditationAllowance.Half:
                    {
                        return ar.ArmorRating / 2.0;
                    }
                case ArmorMeditationAllowance.All:
                    {
                        return 0.0;
                    }
            }
        }
    }
}