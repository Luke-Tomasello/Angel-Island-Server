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

namespace Server.Items
{
    /// <summary>
    /// Available on some crossbows, this special move allows archers to fire while on the move.
    /// This shot is somewhat less accurate than normal, but the ability to fire while running is a clear advantage.
    /// </summary>
    public class MovingShot : WeaponAbility
    {
        public MovingShot()
        {
        }

        public override int BaseMana { get { return 15; } }
        public override double AccuracyScalar { get { return 0.75; } }

        public override void OnMiss(Mobile attacker, Mobile defender)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
                return;

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1060089); // You fail to execute your special move
        }

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
                return;

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1060216); // Your shot was successful
        }
    }
}