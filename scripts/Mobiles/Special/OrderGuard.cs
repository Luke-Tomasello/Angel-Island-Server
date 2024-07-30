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

/* Mobiles/Special/OrderGuard.cs
 * CHANGELOG:
 *  4/15/23, Yoar
 *      Order/Chaos guards are now Order/Chaos guild-aligned.
 */

using Server.Engines.Alignment;
using Server.Guilds;
using Server.Items;

namespace Server.Mobiles
{
    public class OrderGuard : BaseShieldGuard
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Order }); } }

        public override int Keyword { get { return 0x21; } } // *order shield*
        public override BaseShield Shield { get { return new OrderShield(); } }
        public override int SignupNumber { get { return 1007141; } } // Sign up with a guild of order if thou art interested.
        public override GuildType Type { get { return GuildType.Order; } }

        [Constructable]
        public OrderGuard()
        {
        }

        public OrderGuard(Serial serial)
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