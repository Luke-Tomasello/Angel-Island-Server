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

/* ChangeLog:
 *  8/28/22, Yoar
 *      Can no longer cast incognito while holding a sigil.
 * 12/14/05, Kit
 *		Fixed bug allowing incognito with savage paint on.
	6/5/04, Pix
		Merged in 1.0RC0 code.
*/

using Server.Items;
using Server.Mobiles;
using Server.Spells.Seventh;
using System;
using System.Collections;

namespace Server.Spells.Fifth
{
    public class IncognitoSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Incognito", "Kal In Ex",
                SpellCircle.Fifth,
                206,
                9002,
                Reagent.Bloodmoss,
                Reagent.Garlic,
                Reagent.Nightshade
            );

        public IncognitoSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override bool CheckCast()
        {
            if (Factions.Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1010445); // You cannot incognito if you have a sigil
                return false;
            }
            else if (Engines.Alignment.TheFlag.ExistsOn(Caster))
            {
                Caster.SendMessage("You cannot incognito if you have a flag");
                return false;
            }
            else if (!Caster.CanBeginAction(typeof(IncognitoSpell)))
            {
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                return false;
            }
            else if (Caster is PlayerMobile && ((PlayerMobile)Caster).SavagePaintExpiration != TimeSpan.Zero)
            {
                Caster.SendLocalizedMessage(1042402); // You cannot use incognito while wearing body paint
                return false;
            }

            return true;
        }

        public override void OnCast()
        {
            if (Factions.Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1010445); // You cannot incognito if you have a sigil
            }
            else if (Engines.Alignment.TheFlag.ExistsOn(Caster))
            {
                Caster.SendMessage("You cannot incognito if you have a flag");
            }
            else if (!Caster.CanBeginAction(typeof(IncognitoSpell)))
            {
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
            }
            else if (Caster.BodyMod == 183 || Caster.BodyMod == 184)
            {
                Caster.SendLocalizedMessage(1042402); // You cannot use incognito while wearing body paint
            }
            else if (!Caster.CanBeginAction(typeof(PolymorphSpell)) || Caster.IsBodyMod)
            {
                DoFizzle();
            }
            else if (CheckSequence())
            {
                if (Caster.BeginAction(typeof(IncognitoSpell)))
                {
                    DisguiseGump.StopTimer(Caster);

                    Caster.BodyMod = Utility.RandomList(400, 401);
                    Caster.HueMod = Utility.RandomSkinHue();
                    Caster.NameMod = Caster.Body.IsFemale ? NameList.RandomName("female") : NameList.RandomName("male");

                    PlayerMobile pm = Caster as PlayerMobile;

                    if (pm != null)
                    {
                        if (pm.Body.IsFemale)
                            pm.SetHairMods(Utility.RandomList(Utility.HairIDs), 0);
                        else
                            pm.SetHairMods(Utility.RandomList(Utility.HairIDs), Utility.RandomList(Utility.BeardIDs));

                        Item hair = pm.FindItemOnLayer(Layer.Hair);

                        if (hair != null)
                            hair.Hue = Utility.RandomHairHue();

                        hair = pm.FindItemOnLayer(Layer.FacialHair);

                        if (hair != null)
                            hair.Hue = Utility.RandomHairHue();
                    }

                    Caster.FixedParticles(0x373A, 10, 15, 5036, EffectLayer.Head);
                    Caster.PlaySound(0x3BD);

                    BaseArmor.ValidateMobile(Caster);

                    StopTimer(Caster);

                    Timer t = new InternalTimer(Caster);

                    m_Timers[Caster] = t;

                    t.Start();
                }
                else
                {
                    Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                }
            }

            FinishSequence();
        }

        private static Hashtable m_Timers = new Hashtable();

        public static bool StopTimer(Mobile m)
        {
            Timer t = (Timer)m_Timers[m];

            if (t != null)
            {
                t.Stop();
                m_Timers.Remove(m);
            }

            return (t != null);
        }

        private class InternalTimer : Timer
        {
            private Mobile m_Owner;

            public InternalTimer(Mobile owner)
                : base(TimeSpan.FromSeconds(0))
            {
                m_Owner = owner;

                int val = ((6 * owner.Skills.Magery.Fixed) / 50) + 1;

                if (val > 144)
                    val = 144;

                Delay = TimeSpan.FromSeconds(val);
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                if (!m_Owner.CanBeginAction(typeof(IncognitoSpell)))
                {
                    if (m_Owner is PlayerMobile)
                        ((PlayerMobile)m_Owner).SetHairMods(-1, -1);

                    m_Owner.BodyMod = 0;
                    m_Owner.HueMod = -1;
                    m_Owner.NameMod = null;
                    m_Owner.EndAction(typeof(IncognitoSpell));

                    BaseArmor.ValidateMobile(m_Owner);
                }
            }
        }
    }
}