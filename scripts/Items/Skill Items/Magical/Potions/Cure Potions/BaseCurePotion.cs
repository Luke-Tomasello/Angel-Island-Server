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

/* Scripts/Items/Skill Items/Magical/Potions/Cure Potions/BaseCurePotion.cs
 * ChangeLog
 *  8/23/2023, Adam (Refactor)
 *      Refactor algorithm, and update cure rates to match UOR
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 */

/*
 * Lesser Cure Potions have the following chances to cure poison: (1 Garlic to craft)
 * 5% chance to cure Lethal Poison (applied by monsters only)
 * 10% chance to cure Deadly Poison (highest level applicable by players)
 * 15% chance to cure Greater Poison
 * 35% chance to cure Standard Poison
 * 100% chance to cure Lesser Poison
 * 
 * Cure Potions have the following chances to cure poison: (3 Garlic to craft)
 * 15% chance to cure Lethal Poison (applied by monsters only)
 * 25% chance to cure Deadly Poison (highest level applicable by players)
 * 45% chance to cure Greater Poison
 * 95% chance to cure Standard Poison
 * 100% chance to cure Lesser Poison
 * 
 * Greater Cure Potions have the following chances to cure poison: (6 Garlic to craft)
 * 25% chance to cure Lethal Poison (applied by monsters only)
 * 45% chance to cure Deadly Poison (highest level applicable by players)
 * 75% chance to cure Greater Poison
 * 100% chance to cure Standard and Lesser Poison
 * ---
 * http://www.uorenaissance.com/info/Cure_Potion
 */
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public abstract class BaseCurePotion : BasePotion
    {
        public static Dictionary<Type, Dictionary<Poison, double>> CureTable = new() {
                {typeof(LesserCurePotion), new Dictionary<Poison, double>()
                {
                    {Poison.Lesser,     1.0 },
                    {Poison.Regular,    0.35 },
                    {Poison.Greater,    0.15 },
                    {Poison.Deadly,     0.10 },
                    {Poison.Lethal,     0.05 },
                }},
                {typeof(CurePotion), new Dictionary<Poison, double>()
                {
                    {Poison.Lesser,     1.0 },
                    {Poison.Regular,    0.95 },
                    {Poison.Greater,    0.45 },
                    {Poison.Deadly,     0.25 },
                    {Poison.Lethal,     0.15 },
                }},
                {typeof(GreaterCurePotion), new Dictionary<Poison, double>()
                {
                    {Poison.Lesser,     1.0 },
                    {Poison.Regular,    1.0 },
                    {Poison.Greater,    0.75 },
                    {Poison.Deadly,     0.45 },
                    {Poison.Lethal,     0.25 },
                }},
            };
        public BaseCurePotion(PotionEffect effect)
            : base(0xF07, effect)
        {
        }

        public BaseCurePotion(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public void DoCure(Mobile from)
        {
            bool cure = Utility.Chance(Scale(from, CureTable[this.GetType()][from.Poison]));
            //Utility.ConsoleOut("Cure Chance {0}", ConsoleColor.Red, CureTable[this.GetType()][from.Poison]);
            //Utility.ConsoleOut("Was Cured? {0}", ConsoleColor.Red, cure);

            if (cure && from.CurePoison(from))
            {
                from.SendLocalizedMessage(500231); // You feel cured of poison!

                from.FixedEffect(0x373A, 10, 15);
                from.PlaySound(0x1E0);
            }
            else if (!cure)
            {
                from.SendLocalizedMessage(500232); // That potion was not strong enough to cure your ailment!
            }
        }

        public override void Drink(Mobile from)
        {
            //if ( Spells.Necromancy.TransformationSpell.UnderTransformation( from, typeof( Spells.Necromancy.VampiricEmbraceSpell ) ) )
            //{
            //	from.SendLocalizedMessage( 1061652 ); // The garlic in the potion would surely kill you.
            //}
            //else 
            if (from.Poisoned)
            {
                DoCure(from);

                BasePotion.PlayDrinkEffect(from);

                from.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                from.PlaySound(0x1E0);

                if (!Engines.ConPVP.DuelContext.IsFreeConsume(from))
                    this.Consume();
            }
            else
            {
                from.SendLocalizedMessage(1042000); // You are not poisoned.
            }
        }
    }
}