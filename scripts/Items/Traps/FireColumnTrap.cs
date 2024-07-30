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

/* Items/Traps/FireColumnTrap.cs
 * CHANGELOG:
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using System;

namespace Server.Items
{
    public class FireColumnTrap : BaseTrap
    {
        [Constructable]
        public FireColumnTrap()
            : base(0x1B71)
        {
        }

        public override bool PassivelyTriggered { get { return true; } }
        public override TimeSpan PassiveTriggerDelay { get { return TimeSpan.FromSeconds(2.0); } }
        public override int PassiveTriggerRange { get { return 3; } }
        public override TimeSpan ResetDelay { get { return TimeSpan.FromSeconds(0.5); } }

        public override void OnTrigger(Mobile from)
        {
            Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
            Effects.PlaySound(Location, Map, 0x225);

            if (from.Alive && CheckRange(from.Location, 0))
                Spells.SpellHelper.Damage(TimeSpan.FromSeconds(0.5), from, from, Utility.RandomMinMax(10, 40), 0, 100, 0, 0, 0);

            base.OnTrigger(from);
        }

        public FireColumnTrap(Serial serial)
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
    }
}