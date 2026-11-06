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

/* Scripts\Engines\ChampionSpawn\Champs\Special\PumpkinKing\Items\HarvestOfFacesBook.cs
 * CHANGELOG
 *  10/28/2026, Adam
 *      Initial version: "The Harvest of Faces", the lore of The Pumpkin King (Halloween 2026). Read-only; dropped into
 *      his corpse.
 */

namespace Server.Items
{
    public class HarvestOfFacesBook : BaseBook
    {
        public static readonly BookContent Content = new BookContent
            (
                "The Harvest of Faces", "a Britain scribe",
                new BookPageInfo
                (
                    "There once lived a",
                    "farmer named Jaskar",
                    "Mallow, whose gourds",
                    "were the pride of",
                    "every Hallow's Eve",
                    "market in Britannia."
                ),
                new BookPageInfo
                (
                    "One autumn night,",
                    "raiders came. They",
                    "burned his fields,",
                    "took his harvest,",
                    "and left him among",
                    "the rotting vines."
                ),
                new BookPageInfo
                (
                    "No one came to bury",
                    "him. No one",
                    "remembered his name."
                ),
                new BookPageInfo
                (
                    "What happened next,",
                    "no one can say for",
                    "certain. Some",
                    "believe his fields",
                    "grew over an ancient",
                    "burial ground, and",
                    "the vines drank of",
                    "the dead."
                ),
                new BookPageInfo
                (
                    "Others say Jaskar,",
                    "with his last breath,",
                    "made a bargain with",
                    "something beneath",
                    "the earth."
                ),
                new BookPageInfo
                (
                    "All agree on this:",
                    "something climbed",
                    "out of that soil.",
                    "A body of bone, a",
                    "crown of burning",
                    "gourds.",
                    "",
                    "The Pumpkin King."
                ),
                new BookPageInfo
                (
                    "The King cannot",
                    "create life. He can",
                    "only harvest it.",
                    "Every soul in",
                    "Britannia, living or",
                    "fallen, casts an",
                    "echo."
                ),
                new BookPageInfo
                (
                    "The King gathers",
                    "these as Jaskar once",
                    "gathered gourds, and",
                    "grows them in his",
                    "cursed patch."
                ),
                new BookPageInfo
                (
                    "What ripens there",
                    "wears our faces, our",
                    "armor, our blades.",
                    "Hollow things that",
                    "know our names."
                ),
                new BookPageInfo
                (
                    "Strike one down and",
                    "its original may",
                    "feel a chill, for",
                    "the bond between a",
                    "soul and its echo",
                    "is not easily",
                    "broken."
                ),
                new BookPageInfo
                (
                    "Cut the King down",
                    "and his harvest",
                    "withers. But the",
                    "vines remember, and",
                    "the vines return."
                ),
                new BookPageInfo
                (
                    "It is said the",
                    "King's servants",
                    "steal the honey",
                    "cakes left out for",
                    "the dead on",
                    "Hallow's Eve.",
                    "Whoever defeats him",
                    "may yet find his"
                ),
                new BookPageInfo
                (
                    "hoard.",
                    "",
                    "",
                    "- collected by a",
                    "  Britain scribe"
                )
            );

        public override BookContent DefaultContent { get { return Content; } }

        [Constructable]
        public HarvestOfFacesBook() : base(Utility.Random(0xFF1, 2), false)
        {
        }

        public HarvestOfFacesBook(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}
