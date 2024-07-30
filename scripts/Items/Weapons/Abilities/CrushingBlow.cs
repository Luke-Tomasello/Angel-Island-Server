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
    /// Also known as the Haymaker, this attack dramatically increases the damage done by a weapon reaching its mark.
    /// </summary>
    public class CrushingBlow : WeaponAbility
    {
        public CrushingBlow()
        {
        }

        public override int BaseMana { get { return 25; } }
        public override double DamageScalar { get { return 1.5; } }


        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
                return;

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1060090); // You have delivered a crushing blow!
            defender.SendLocalizedMessage(1060091); // You take extra damage from the crushing attack!

            defender.PlaySound(0x1E1);
            defender.FixedParticles(0, 1, 0, 9946, EffectLayer.Head);

            Effects.SendMovingParticles(new Entity(Serial.Zero, new Point3D(defender.X, defender.Y, defender.Z + 50), defender.Map), new Entity(Serial.Zero, new Point3D(defender.X, defender.Y, defender.Z + 20), defender.Map), 0xFB4, 1, 0, false, false, 0, 3, 9501, 1, 0, EffectLayer.Head, 0x100);
        }
    }
}