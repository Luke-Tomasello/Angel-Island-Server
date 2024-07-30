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

/* Scripts/Items/Special/Bulk Order Rewards/Tailoring/TailorsSewingKit.cs
 * CHANGELOG:
 *  11/30/23, Yoar
 *      Initial commit
 */

using Server.Engines.Craft;

namespace Server.Items
{
    public class TailorsSewingKit : BaseTool
    {
        public static double SkillBonus = 15.0;

        public override string DefaultName { get { return "master tailor's sewing kit"; } }
        public override CraftSystem CraftSystem { get { return DefTailoring.CraftSystem; } }

        [Constructable]
        public TailorsSewingKit()
            : this(200)
        {

        }

        [Constructable]
        public TailorsSewingKit(int uses)
            : base(uses, 0xF9D)
        {
        }

        public TailorsSewingKit(Serial serial)
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