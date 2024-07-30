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

/* Scripts\Spells\Fifth\SummonCreature.cs
 * ChangeLog:
 *	2/3/11, Adam
 *		Make ability to summon a Horde Minion based upon Core.AngelIsland
 *	7/16/10, adam
 *		Add HordeMinionFamiliar as a summonable creature
 *			o 3 control slots
 *			o requires SpiritSpeak to summon
 * 	6/5/04, Pix
 * 		Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using System;

namespace Server.Spells.Fifth
{
    public class SummonCreatureSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Summon Creature", "Kal Xen",
                SpellCircle.Fifth,
                266,
                9040,
                Reagent.Bloodmoss,
                Reagent.MandrakeRoot,
                Reagent.SpidersSilk
            );

        public SummonCreatureSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        // TODO: Get real list
        private static Type[] m_Types = new Type[]
            {
                typeof( PolarBear ),
                typeof( GrizzlyBear ),
                typeof( BlackBear ),
                typeof( BrownBear ),
                typeof( Horse ),
                typeof( Walrus ),
                typeof( GreatHart ),
                typeof( Hind ),
                typeof( Dog ),
                typeof( Boar ),
                typeof( Chicken ),
                typeof( Rabbit )
            };

        public override bool CheckCast()
        {
            if (!base.CheckCast())
                return false;


            if ((Caster.FollowerCount + 2) > Caster.FollowersMax)
            {
                Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                return false;
            }

            return true;
        }

        public override void OnCast()
        {
            if (CheckSequence())
            {
                try
                {
                    BaseCreature creature = null;
                    if ((Core.RuleSets.AngelIslandRules()) && Caster.Skills.SpiritSpeak.Value > 50 && (Caster.Skills.SpiritSpeak.Value / 100) >= Utility.RandomDouble())
                    {
                        creature = new HordeMinionFamiliar();
                        creature.ControlSlots = 3;
                    }
                    else
                    {
                        creature = (BaseCreature)Activator.CreateInstance(m_Types[Utility.Random(m_Types.Length)]);
                        creature.ControlSlots = 2;
                    }

                    TimeSpan duration;

                    if (Core.RuleSets.AOSRules())
                        duration = TimeSpan.FromSeconds((2 * Caster.Skills.Magery.Fixed) / 5);
                    else
                        duration = TimeSpan.FromSeconds(4.0 * Caster.Skills[SkillName.Magery].Value);

                    SpellHelper.Summon(creature, Caster, 0x215, duration, false, false);
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            }

            FinishSequence();
        }

        public override TimeSpan GetCastDelay()
        {
            if (Core.RuleSets.AOSRules())
                return TimeSpan.FromTicks(base.GetCastDelay().Ticks * 5);

            return base.GetCastDelay() + TimeSpan.FromSeconds(6.0);
        }
    }
}