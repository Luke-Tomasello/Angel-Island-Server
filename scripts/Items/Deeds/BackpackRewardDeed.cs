/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Items/Deeds/BackpackRewardDeed.cs
 * ChangeLog:
 *	3/2/2026, Adam
 *		first time checkin.
 */

using Server.Mobiles;

namespace Server.Items
{

    public class BackpackRewardDeed : Item
    {
        [Constructable]
        public BackpackRewardDeed()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "a backpack reward deed";
        }

        public BackpackRewardDeed(Serial serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            // Must be in pack to use
            if (from?.Backpack == null || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Must be in pack to use!!
                return;
            }

            // Must have a backpack to apply hue to
            var pack = from.Backpack;

            // Player must have a reward hue to apply
            int rewardHue = 0;
            if (from is PlayerMobile pm)
                rewardHue = pm.RewardHue;

            if (rewardHue <= 0)
            {
                from.SendMessage("You do not have a reward hue available.");
                return;
            }

            // Already matches: do not consume deed
            if (pack.Hue == rewardHue)
            {
                from.SendMessage("Your backpack already matches your reward hue. This deed cannot be used.");
                return;
            }

            // Apply + consume
            pack.Hue = rewardHue;
            from.SendMessage("Your backpack has been dyed to match your reward hue.");

            Delete();
        }
    }
}