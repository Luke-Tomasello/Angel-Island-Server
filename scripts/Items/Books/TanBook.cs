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
using System;

namespace Server.Items
{
    public class TanBook : BaseBook
    {
        [Constructable]
        public TanBook()
            : base(0xFF0)
        {
        }

        [Constructable]
        public TanBook(int pageCount, bool writable)
            : base(0xFF0, pageCount, writable)
        {
        }

        [Constructable]
        public TanBook(string title, string author, int pageCount, bool writable)
            : base(0xFF0, title, author, pageCount, writable)
        {
        }
        public override Item Dupe(int amount)
        {
            TanBook new_book = new TanBook();
            Utility.CopyProperties(new_book, this);
            Inscribe.CopyBook(this, new_book);
            return base.Dupe(new_book, amount);
        }
        public TanBook(Serial serial)
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