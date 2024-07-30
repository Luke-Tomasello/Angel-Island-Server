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

/* Scripts/Items/Special/Bulk Order Rewards/Tailoring/KnittingSticks.cs
 * CHANGELOG:
 *  10/3/23, Yoar
 *      Initial commit
 */

using Server.Engines.Craft;

namespace Server.Items
{
    [Flipable(0xDF6, 0xDF7)]
    public class KnittingNeedles : BaseTool
    {
        public static double ExceptionalBonus = 0.20;

        public override string DefaultName { get { return "knitting needles"; } }
        public override CraftSystem CraftSystem { get { return DefTailoring.CraftSystem; } }

        [Constructable]
        public KnittingNeedles()
            : base(0xDF7)
        {
        }

        [Constructable]
        public KnittingNeedles(int uses)
            : base(uses, 0xDF7)
        {
        }

        public KnittingNeedles(Serial serial)
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