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

/* Items/Weapons/Abilties/Dismount.cs
 * CHANGELOG:
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using System;

namespace Server.Items
{
    /// <summary>
    /// Perfect for the foot-soldier, the Dismount special attack can unseat a mounted opponent.
    /// The fighter using this ability must be on his own two feet and not in the saddle of a steed
    /// (with one exception: players may use a lance to dismount other players while mounted).
    /// If it works, the target will be knocked off his own mount and will take some extra damage from the fall!
    /// </summary>
    public class Dismount : WeaponAbility
    {
        public Dismount()
        {
        }

        public override int BaseMana { get { return 20; } }

        public override bool Validate(Mobile from)
        {
            if (!base.Validate(from))
                return false;

            if (from.Mounted && !(from.Weapon is Lance))
            {
                from.SendLocalizedMessage(1061283); // You cannot perform that attack while mounted!
                return false;
            }

            return true;
        }

        public static readonly TimeSpan BlockMountDuration = TimeSpan.FromSeconds(10.0); // TODO: Taken from bola script, needs to be verified

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker))
                return;

            if (attacker.Mounted && !(defender.Weapon is Lance)) // TODO: Should there be a message here?
                return;

            ClearCurrentAbility(attacker);

            IMount mount = defender.Mount;

            if (mount == null)
            {
                attacker.SendLocalizedMessage(1060848); // This attack only works on mounted targets
                return;
            }

            if (!CheckMana(attacker, true))
                return;

            attacker.SendLocalizedMessage(1060082); // The force of your attack has dislodged them from their mount!

            if (attacker.Mounted)
                defender.SendLocalizedMessage(1062315); // You fall off your mount!
            else
                defender.SendLocalizedMessage(1060083); // You fall off of your mount and take damage!

            defender.PlaySound(0x140);
            defender.FixedParticles(0x3728, 10, 15, 9955, EffectLayer.Waist);

            mount.Rider = null;

            defender.BeginAction(typeof(BaseMount));
            Timer.DelayCall(BlockMountDuration, new TimerStateCallback(ReleaseMountLock), defender);

            if (!attacker.Mounted)
                AOS.Damage(defender, attacker, Utility.RandomMinMax(15, 25), 100, 0, 0, 0, 0, this);
        }

        private void ReleaseMountLock(object state)
        {
            ((Mobile)state).EndAction(typeof(BaseMount));
        }
    }
}