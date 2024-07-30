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

/* Scripts\Spells\Eighth\FireElemental.cs
 * 	ChangeLog:
 *	7/16/10, adam
 *		o decrease duration by 50%
 * 	6/5/04, Pix
 * 		Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using System;

namespace Server.Spells.Eighth
{
    public class FireElementalSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Fire Elemental", "Kal Vas Xen Flam",
                SpellCircle.Eighth,
                269,
                9050,
                false,
                Reagent.Bloodmoss,
                Reagent.MandrakeRoot,
                Reagent.SpidersSilk,
                Reagent.SulfurousAsh
            );

        public FireElementalSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override bool CheckCast()
        {
            if (!base.CheckCast())
                return false;

            if ((Caster.FollowerCount + 4) > Caster.FollowersMax)
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
                TimeSpan duration = TimeSpan.FromSeconds((2 * Caster.Skills.Magery.Fixed) / 10);
                SpellHelper.Summon(new FireElemental(Core.RuleSets.AngelIslandRules() ? true : false), Caster, 0x217, duration, false, false);
            }

            FinishSequence();
        }
    }
}