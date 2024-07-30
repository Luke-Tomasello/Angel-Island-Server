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

/*	/Scripts/Items/Skill Items/Tools/ScribesPen.cs
 *	ChangeLog :
 *		7/29/05, erlein
 *			Added override for Consume() to prevent scribes pens being
 *			eaten during craft of bookcase.
 *
 */

using Server.Engines.Craft;

namespace Server.Items
{
    [FlipableAttribute(0x0FBF, 0x0FC0)]
    public class ScribesPen : BaseTool
    {
        public override CraftSystem CraftSystem { get { return DefInscription.CraftSystem; } }

        public override int LabelNumber { get { return 1044168; } } // scribe's pen

        public override void Consume(int amount)
        {
        }

        [Constructable]
        public ScribesPen()
            : base(0x0FBF)
        {
            Weight = 1.0;
        }

        [Constructable]
        public ScribesPen(int uses)
            : base(uses, 0x0FBF)
        {
            Weight = 1.0;
        }

        public ScribesPen(Serial serial)
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

            if (Weight == 2.0)
                Weight = 1.0;
        }
    }
}