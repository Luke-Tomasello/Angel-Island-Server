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
    public class NecromancerSpellbook : Spellbook
    {
        public override SpellbookType SpellbookType { get { return SpellbookType.Necromancer; } }
        public override int BookOffset { get { return 100; } }
        public override int BookCount { get { return 16; } }

        public override Item Dupe(int amount)
        {
            Spellbook book = new NecromancerSpellbook();

            book.Content = this.Content;

            return base.Dupe(book, amount);
        }

        [Constructable]
        public NecromancerSpellbook()
            : this((ulong)0)
        {
        }

        [Constructable]
        public NecromancerSpellbook(ulong content)
            : base(content, 0x2253)
        {
            Layer = Layer.OneHanded;
        }

        public NecromancerSpellbook(Serial serial)
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

            if (version < 1 && Layer == Layer.Invalid)
                Layer = Layer.OneHanded;
        }
    }
}