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

using Server.SkillHandlers;

namespace Server.Items
{
    public class BrownBook : BaseBook
    {
        [Constructable]
        public BrownBook()
            : base(0xFEF)
        {
        }

        [Constructable]
        public BrownBook(int pageCount, bool writable)
            : base(0xFEF, pageCount, writable)
        {
        }

        [Constructable]
        public BrownBook(string title, string author, int pageCount, bool writable)
            : base(0xFEF, title, author, pageCount, writable)
        {
        }
        public override Item Dupe(int amount)
        {
            BrownBook new_book = new BrownBook();
            Utility.CopyProperties(new_book, this);
            Inscribe.CopyBook(this, new_book);
            return base.Dupe(new_book, amount);
        }
        public BrownBook(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }
    }
}