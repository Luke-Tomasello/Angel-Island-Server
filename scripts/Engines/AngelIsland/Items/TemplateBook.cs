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

// Engined/AngelIsland/TemplateBook.cs, last modified 4/12/04 by Pixie.
// 4/12/04 pixie
//   Initial Revision.
// 4/12/04 Created by Pixie;

namespace Server.Items
{
    public class TemplateBook : BaseBook
    {
        private const string TITLE = "The Template Book";
        private const string AUTHOR = "Pixie";
        private const int PAGES = 40;   //This doesn't *HAVE* to be updated, it'll fill up the 
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
        public TemplateBook()
            : base(Utility.RandomList(BOOKTYPES), TITLE, AUTHOR, PAGES, WRITABLE)
        {
            // NOTE: There are 8 lines per page and
            // approx 22 to 24 characters per line.
            //  0----+----1----+----2----+
            int cnt = 0;
            string[] lines;

            lines = new string[]
            {
                "Here's a sample book.",
                "",
                "It's really not too",
                "entertaining.",
                "",
                "But it does get the",
                "procedure across.",
                "",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "This is a second page",
                "You can keep adding ",
                "page after page of",
                "text in the constructor.",
                "",
                "And as long as you",
                "follow the template",
                "all will be well.",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "",
                "",
                "",
                "3rd PAGE!!!!!!",
                "",
                "",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;


            lines = new string[]
            {
                "",
                "",
                "     Long live",
                "      Pixie",
                "       of",
                "      PAG",
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

        public TemplateBook(Serial serial)
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