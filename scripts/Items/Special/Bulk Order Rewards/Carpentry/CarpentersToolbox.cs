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

/* Scripts/Items/Special/Bulk Order Rewards/Carpentry/CarpentersToolbox.cs
 * CHANGELOG:
 *  11/30/23, Yoar
 *      Initial commit
 */

using Server.Engines.Craft;

namespace Server.Items
{
    [Flipable(0x1EBA, 0x1EBB)]
    public class CarpentersToolbox : BaseTool
    {
        public static double SkillBonus = 15.0;

        public override string DefaultName { get { return "master carpenter's toolbox"; } }
        public override CraftSystem CraftSystem { get { return DefCarpentry.CraftSystem; } }

        [Constructable]
        public CarpentersToolbox()
            : this(200)
        {
        }

        [Constructable]
        public CarpentersToolbox(int uses)
            : base(uses, 0x1EBB)
        {
            Weight = 3.0;
        }

        public CarpentersToolbox(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1 && Weight == 0.0)
                Weight = 3.0;
        }
    }
}