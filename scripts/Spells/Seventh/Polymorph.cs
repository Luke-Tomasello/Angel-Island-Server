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

/* Scripts/Spells/Seventh/Polymorph.cs
 * ChangeLog: 
 *  8/28/22, Yoar
 *      Can no longer cast polymorph while holding a sigil.
 * 12/14/05, Kit
 *		Fixed bug allowing to polymorph with savage paint on.
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
*/

using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Fifth;
using System;
using System.Collections;

namespace Server.Spells.Seventh
{
    public class PolymorphSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Polymorph", "Vas Ylem Rel",
                SpellCircle.Seventh,
                221,
                9002,
                Reagent.Bloodmoss,
                Reagent.SpidersSilk,
                Reagent.MandrakeRoot
            );

        private int m_NewBody;

        public PolymorphSpell(Mobile caster, Item scroll, int body)
            : base(caster, scroll, m_Info)
        {
            m_NewBody = body;
        }

        public PolymorphSpell(Mobile caster, Item scroll)
            : this(caster, scroll, 0)
        {
        }

        public override bool CheckCast()
        {
            /*if ( Caster.Mounted )
            {
                Caster.SendLocalizedMessage( 1042561 ); //Please dismount first.
                return false;
            }
            else */
            if (Factions.Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1010521); // You cannot polymorph while you have a Town Sigil
                return false;
            }
            else if (Engines.Alignment.TheFlag.ExistsOn(Caster))
            {
                Caster.SendMessage("You cannot polymorph while you have a flag");
                return false;
            }
            /*else if ( Necromancy.TransformationSpell.UnderTransformation( Caster ) )
            {
                Caster.SendLocalizedMessage( 1061633 ); // You cannot polymorph while in that form.
                return false;
            }*/
            else if (DisguiseGump.IsDisguised(Caster))
            {
                Caster.SendLocalizedMessage(502167); // You cannot polymorph while disguised.
                return false;
            }
            else if (Caster is PlayerMobile && ((PlayerMobile)Caster).SavagePaintExpiration != TimeSpan.Zero)
            {
                Caster.SendLocalizedMessage(1042512); // You cannot polymorph while wearing body paint
                return false;
            }
            else if (!Caster.CanBeginAction(typeof(PolymorphSpell)))
            {
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                return false;
            }
            else if (m_NewBody == 0)
            {
                Caster.SendGump(new PolymorphGump(Caster, Scroll));
                return false;
            }

            return true;
        }

        public override void OnCast()
        {
            /*if ( Caster.Mounted )
            {
                Caster.SendLocalizedMessage( 1042561 ); //Please dismount first.
            } 
            else */
            if (Factions.Sigil.ExistsOn(Caster))
            {
                Caster.SendLocalizedMessage(1010521); // You cannot polymorph while you have a Town Sigil
            }
            else if (Engines.Alignment.TheFlag.ExistsOn(Caster))
            {
                Caster.SendMessage("You cannot polymorph while you have a flag");
            }
            else if (!Caster.CanBeginAction(typeof(PolymorphSpell)))
            {
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
            }
            /*else if ( Necromancy.TransformationSpell.UnderTransformation( Caster ) )
            {
                Caster.SendLocalizedMessage( 1061633 ); // You cannot polymorph while in that form.
            }*/
            else if (DisguiseGump.IsDisguised(Caster))
            {
                Caster.SendLocalizedMessage(502167); // You cannot polymorph while disguised.
            }
            else if (Caster.BodyMod == 183 || Caster.BodyMod == 184)
            {
                Caster.SendLocalizedMessage(1042512); // You cannot polymorph while wearing body paint
            }
            else if (!Caster.CanBeginAction(typeof(IncognitoSpell)) || Caster.IsBodyMod)
            {
                DoFizzle();
            }
            else if (CheckSequence())
            {
                if (Caster.BeginAction(typeof(PolymorphSpell)))
                {
                    if (m_NewBody != 0)
                    {
                        if (!((Body)m_NewBody).IsHuman)
                        {
                            Mobiles.IMount mt = Caster.Mount;

                            if (mt != null)
                                mt.Rider = null;
                        }

                        Caster.BodyMod = m_NewBody;

                        if (m_NewBody == 400 || m_NewBody == 401)
                            Caster.HueMod = Utility.RandomSkinHue();
                        else
                            Caster.HueMod = 0;

                        BaseArmor.ValidateMobile(Caster);

                        StopTimer(Caster);

                        Timer t = new InternalTimer(Caster);

                        m_Timers[Caster] = t;

                        t.Start();
                    }
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

                int val = (int)owner.Skills[SkillName.Magery].Value;

                //if ( val > 120 )
                //	val = 120;
                if (val > 100)
                    val = 100;

                Delay = TimeSpan.FromSeconds(val);
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                if (!m_Owner.CanBeginAction(typeof(PolymorphSpell)))
                {
                    m_Owner.BodyMod = 0;
                    m_Owner.HueMod = -1;
                    m_Owner.EndAction(typeof(PolymorphSpell));

                    BaseArmor.ValidateMobile(m_Owner);
                }
            }
        }
    }
}