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

/*
 * Engines/Items/Books/BookofUpgradeContracts.cs
 * CHANGELOG:
 * 9/26/21, Adam
 *  Obsolete. See Scripts\Items\Books\Defined\StorageUpgradeInfo.cs
 *  We can't remove this from the build since players have already purchased this book.
 * 5/8/07, Adam
 *  Initial Revision.
 */
/*
 * -------------- OBSOLETE -------------- *
 */
namespace Server.Items
{
    public class BookofUpgradeContracts : BaseBook
    {
        private const string TITLE = "Upgrades Explained";
        private const int PAGES = 6;
        private const bool WRITABLE = false;
        private const int PURPLE_BOOK = 0xFF2;

        [Constructable]
        public BookofUpgradeContracts()
            : base(PURPLE_BOOK, TITLE, NameList.RandomName("female"), PAGES, WRITABLE)
        {
            // NOTE: There are 8 lines per page and
            // approx 22 to 24 characters per line.
            //  0----+----1----+----2----+
            int cnt = 0;
            string[] lines;
            Name = TITLE;

            lines = new string[]
            {
                "Modest Upgrade is",
                "",
                "500 lockdowns",
                "3 secures",
                "3 lockboxes",
                "",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "Moderate Upgrade is",
                "",
                "900 lockdowns",
                "6 secures",
                "4 lockboxes",
                "",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "Premium Upgrade is",
                "",
                "1300 lockdowns",
                "9 secures",
                "5 lockboxes",
                "",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;


            lines = new string[]
            {
                "Lavish Upgrade is",
                "",
                "1950 lockdowns",
                "14 secures",
                "7 lockboxes",
                "",
                "",
                ""
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {  //0123456789012345678901234
				"Your investment is safe!",
                "",
                "When you redeed your home",
                "a check for the full cost",
                "of all upgrades will be",
                "deposited in your bank.",
                "",
                ""
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "To perform an upgrade",
                "",
                "Stand under house sign",
                "Double click contract",
                "Target your house sign",
                "Your storage will be",
                "Upgraded.",
                "Enjoy your upgrade!"
            };
            Pages[cnt++].Lines = lines;
        }

        public BookofUpgradeContracts(Serial serial)
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