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

/* Scripts\Skills\Meditation.cs
 * ChangeLog:
 * 12/26/07, Pix
 *      Now you can hold a bounty ledger and meditate.
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *	2/16/05, Adam
 *		Convert casts to using the 'as' operator and remove check for .ShieldArmor in CheckMeddableArmor()
 *  9/3/04, Pix
 *		Now when trying to actively meditate in non-meddable armor, meditation fails 
 *		with the message "Regenerative forces cannot penetrate your armor."
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using System;

namespace Server.SkillHandlers
{
    class Meditation
    {
        public static void Initialize()
        {
            SkillInfo.Table[46].Callback = new SkillUseCallback(OnUse);
        }

        public static bool CheckOkayHolding(Item item)
        {
            if (item == null)
                return true;

            if (item is Spellbook || item is Runebook || item is BountyLedger)
                return true;

            //if ( Core.AOS && item is BaseWeapon && ((BaseWeapon)item).Attributes.SpellChanneling != 0 )
            //return true;

            //if ( Core.AOS && item is BaseArmor && ((BaseArmor)item).Attributes.SpellChanneling != 0 )
            //return true;

            return false;
        }

        public static bool IsMeddableArmor(BaseArmor ba)
        {
            try
            {
                if (ba == null)
                {
                    return true;
                }

                if (ba.MeditationAllowance == ArmorMeditationAllowance.None)
                {
                    return false;
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            return true;
        }

        public static bool CheckMeddableArmor(Mobile m)
        {
            if (!IsMeddableArmor(m.NeckArmor as BaseArmor) ||
                !IsMeddableArmor(m.HandArmor as BaseArmor) ||
                !IsMeddableArmor(m.HeadArmor as BaseArmor) ||
                !IsMeddableArmor(m.ArmsArmor as BaseArmor) ||
                !IsMeddableArmor(m.LegsArmor as BaseArmor) ||
                !IsMeddableArmor(m.ChestArmor as BaseArmor))
            {
                return false;
            }

            return true;
        }


        public static TimeSpan OnUse(Mobile m)
        {
            m.RevealingAction();

            if (m.Target != null)
            {
                m.SendLocalizedMessage(501845); // You are busy doing something else and cannot focus.

                return TimeSpan.FromSeconds(5.0);
            }
            else if (m.Hits < (m.HitsMax / 10)) // Less than 10% health
            {
                m.SendLocalizedMessage(501849); // The mind is strong but the body is weak.

                return TimeSpan.FromSeconds(5.0);
            }
            else if (m.Mana >= m.ManaMax)
            {
                m.SendLocalizedMessage(501846); // You are at peace.

                return TimeSpan.FromSeconds(5.0);
            }
            else
            {
                Item oneHanded = m.FindItemOnLayer(Layer.OneHanded);
                Item twoHanded = m.FindItemOnLayer(Layer.TwoHanded);

                if (Core.RuleSets.AOSRules())
                {
                    if (!CheckOkayHolding(oneHanded))
                        m.AddToBackpack(oneHanded);

                    if (!CheckOkayHolding(twoHanded))
                        m.AddToBackpack(twoHanded);
                }
                else if (!CheckOkayHolding(oneHanded) || !CheckOkayHolding(twoHanded))
                {
                    m.SendLocalizedMessage(502626); // Your hands must be free to cast spells or meditate.

                    return TimeSpan.FromSeconds(2.5);
                }
                else if (!CheckMeddableArmor(m))
                {
                    m.SendMessage("Regenerative forces cannot penetrate your armor.");

                    return TimeSpan.FromSeconds(2.5);
                }

                if (m.CheckSkill(SkillName.Meditation, 0, 100, contextObj: new object[2]))
                {
                    m.SendLocalizedMessage(501851); // You enter a meditative trance.
                    m.Meditating = true;

                    if (m.Player || m.Body.IsHuman)
                        m.PlaySound(0xF9);
                }
                else
                {
                    m.SendLocalizedMessage(501850); // You cannot focus your concentration.
                }

                return TimeSpan.FromSeconds(10.0);
            }
        }
    }
}