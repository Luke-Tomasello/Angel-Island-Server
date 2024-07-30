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

// Engines/AngelIsland/TheGuardianBook.cs, last modified 4/12/04 by Adam.
// Bassed on Engines/AngelIsland/TemplateBook.cs, last modified 4/12/04 by Pixie.
// 4/12/04 Adam
//   Initial Revision.
// 4/12/04 Created by Adam;

namespace Server.Items
{
    public class TheGuardianBook : BaseBook
    {
        private const string TITLE = "The Guardian";
        private const string AUTHOR = "Adam Ant";
        private const int PAGES = 6;    //This doesn't *HAVE* to be updated, it'll fill up the 
                                        //book with blank pages though.  It'd be cleaner if it
                                        //had the exact right number of pages.

        private const bool WRITABLE = false;

        //This randomly chooses one of the four types of books.
        //If you wish to only have one particular book, or a couple
        //of different types, remove the ones you don't want
        private static int[] BOOKTYPES = new int[]
            {
              0xFEF, //brown
			  0xFF0, //tan
			  0xFF1, //red
			  0xFF2  //purple
			};

        [Constructable]
        public TheGuardianBook()
            : base(Utility.RandomList(BOOKTYPES), TITLE, AUTHOR, PAGES, WRITABLE)
        {
            // NOTE: There are 8 lines per page and
            // approx 22 to 24 characters per line.
            //  0----+----1----+----2----+
            int cnt = 0;
            string[] lines;

            lines = new string[]
            {
                "He who hunteth the lich",
                "in these caves committeth",
                "a crime of the gravest",
                "order, deserving of death.",
                "The liches and I dwell",
                "together in these caves,",
                "which we devoutly believe",
                "to be given us as our",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "sacred home.",
                "I gladly give my blessing",
                "to all who wish to hunt",
                "down and deliver unto",
                "death all Daemons and",
                "Dragons. These are",
                "creatures of vile and",
                "loathsome aspect,",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "malodorous and displeasing",
                "to the senses.",
                "If you would join in the",
                "hunt to rid my sacred",
                "home of these vile",
                "monsters, you must don a",
                "scarf of the colour of",
                "blood in token of your",
            };
            Pages[cnt++].Lines = lines;


            lines = new string[]
            {
                "respect.",
                "If you fail in this regard,",
                "and look not upon this",
                "magical garment, you will",
                "die swiftly and surely to",
                "my hand.",
                "I have given unto you fit",
                "means of escape, which I",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "enjoin you to take unto",
                "you and employ.",
                "I have marked and",
                "designed for your especial",
                "use a rune, and this will",
                "bring you at the end of",
                "your travels to the great",
                "city of Britain.",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "May your going be safe",
                "and sure. Blessings upon",
                "you.",
                "",
                "",
                "",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;

            /* PAGE SYNTAX:
						lines = new string[]
						{
							"",
							"",
							"",
							"",
							"",
							"",
							"",
							"",
						};
						Pages[cnt++].Lines = lines;
			*/
        }

        public TheGuardianBook(Serial serial)
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