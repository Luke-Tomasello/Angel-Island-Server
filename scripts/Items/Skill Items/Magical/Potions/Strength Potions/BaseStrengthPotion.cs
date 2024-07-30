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

/* Scripts/Items/Skill Items/Magical/Potions/Strength Potions/BaseStrengthPotion.cs
 * ChangeLog
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 */

using System;

namespace Server.Items
{
    public abstract class BaseStrengthPotion : BasePotion
    {
        public abstract int StrOffset { get; }
        public abstract TimeSpan Duration { get; }

        public BaseStrengthPotion(PotionEffect effect)
            : base(0xF09, effect)
        {
        }

        public BaseStrengthPotion(Serial serial)
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

        public bool DoStrength(Mobile from)
        {
            // TODO: Verify scaled; is it offset, duration, or both?
            if (Spells.SpellHelper.AddStatOffset(from, StatType.Str, Scale(from, StrOffset), Duration))
            {
                from.FixedEffect(0x375A, 10, 15);
                from.PlaySound(0x1E7);
                return true;
            }

            from.SendLocalizedMessage(502173); // You are already under a similar effect.
            return false;
        }

        public override void Drink(Mobile from)
        {
            if (DoStrength(from))
            {
                BasePotion.PlayDrinkEffect(from);

                if (!Engines.ConPVP.DuelContext.IsFreeConsume(from))
                    this.Consume();
            }
        }
    }
}