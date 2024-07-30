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

namespace Server.Items
{
    public class BookOfChivalry : Spellbook
    {
        public override SpellbookType SpellbookType { get { return SpellbookType.Paladin; } }
        public override int BookOffset { get { return 200; } }
        public override int BookCount { get { return 10; } }

        public override Item Dupe(int amount)
        {
            Spellbook book = new BookOfChivalry();

            book.Content = this.Content;

            return base.Dupe(book, amount);
        }

        [Constructable]
        public BookOfChivalry()
            : this((ulong)0x3FF)
        {
        }

        [Constructable]
        public BookOfChivalry(ulong content)
            : base(content, 0x2252)
        {
            Layer = Layer.Invalid;
        }

        public BookOfChivalry(Serial serial)
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
            Layer = Layer.Invalid;
        }
    }
}