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

/* Scripts/Items/Skill Items/Specialized/WoodEngravingBook.cs
 * ChangeLog:
 *	05/01/06, weaver
 *		Normalized requirements to 90 primary skill / 80 secondary skill.
 *		Changed instances of 'erlein' to 'weaver' in code comments.
 *	03/09/04, weaver
 *		Initial creation
 */

using Server.Mobiles;

namespace Server.Items
{
    public class WoodEngravingBook : Item
    {
        [Constructable]
        public WoodEngravingBook()
            : base(0xFF4)
        {
            Name = "Wood Engraving";
            Weight = 1.0;
        }

        public WoodEngravingBook(Serial serial)
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
            PlayerMobile pm;

            if (from is PlayerMobile)
                pm = (PlayerMobile)from;
            else
                return;

            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            else if (pm.Skills[SkillName.Carpentry].Base < 90.0 || pm.Skills[SkillName.Inscribe].Base < 80.0)
                pm.SendMessage("Only a Master Carpenter and Expert Scribe can learn from this book.");
            else if (pm.WoodEngraving)
                pm.SendMessage("You have already learned this.");
            else
            {
                pm.WoodEngraving = true;
                pm.SendMessage("You have learned the art of engraving wood. Use a square graver to customize your hand-crafted goods.");
                Delete();
            }
        }
    }
}