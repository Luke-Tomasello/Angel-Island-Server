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

// Engines/AngelIsland/TheCellBook.cs, last modified 4/12/04 by Adam.
// Bassed on Engines/AngelIsland/TemplateBook.cs, last modified 4/12/04 by Pixie.
// 4/28/04 Adam
//   Initial Revision.
// 4/28/04 Created by Adam;
// 5/5/04 suggested that the cave spawn must be defeated;

namespace Server.Items
{
    public class TheCellBook : BaseBook
    {
        private const string TITLE = "Angel Island";
        private const string AUTHOR = "Adam Ant";
        private const int PAGES = 23;   //This doesn't *HAVE* to be updated, it'll fill up the 
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
        public TheCellBook()
            : base(Utility.RandomList(BOOKTYPES), TITLE, AUTHOR, PAGES, WRITABLE)
        {
            // NOTE: There are 8 lines per page and
            // approx 22 to 24 characters per line.
            //  0----+----1----+----2----+
            int cnt = 0;
            string[] lines;

            lines = new string[]
            {
                "--day 1",
                "So. They've put me away ",
                "for my crimes. Heh, I ",
                "feel no remorse and I ",
                "shall return to my duties ",
                "sooner than they may ",
                "think.",
                "",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "--day 2",
                "It seems time does not ",
                "pass so slowly here when ",
                "you are carrying the ",
                "death of many upon your ",
                "back. I have witnessed ",
                "several inmates walk to ",
                "freedom through the ",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "magical gates in the ",
                "parole officer's office. ",
                "",
                "",
                "",
                "",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;

            lines = new string[]
            {
                "--day 3",
                "One of my cell mates and ",
                "I found a curious storage ",
                "room full of boxes and ",
                "crates. We were hoping ",
                "to find some food but ",
                "only found a disabled ",
                "teleporter. ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "--day 10 ",
                "I'm starving! ",
                "They don�t feed us ",
                "enough here, and what ",
                "they do feed us is likely ",
                "waste from the guard ",
                "kitchen. ",
                "I'll kill that Roderick ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "if he looks at me again!",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "--day 15 ",
                "I do not know what day ",
                "it is today. Went to ask ",
                "the parole officer about ",
                "how much 'time' I have ",
                "left, and she told me ",
                "�time is money�. I ",
                "wonder what she meant ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "by that?",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "--day 17 ",
                "There was a prison break ",
                "today! The ghosts of two ",
                "inmates returned through ",
                "the teleporter in the ",
                "supply room while the ",
                "other two never came ",
                "back. I can only assume ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "that they made it!",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "--day 23 ",
                "I have now come to ",
                "believe that the parole ",
                "officer will take bribes. I ",
                "do not know how much is ",
                "required, but I've found ",
                "that gold can be found in ",
                "the cemetery behind the ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "prison.",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "--day 27 ",
                "I'm getting out of here. ",
                "I simply can't take the ",
                "hunger any longer. ",
                "Mortimer and I will follow ",
                "the next group that tires ",
                "to escape to freedom. ",
                "",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "--day 33",
                "Today, me, Mortimer, and ",
                "Calvin watched as a small ",
                "group of inmates lead by ",
                "Yeager killed the cell ",
                "guards, took their ",
                "weapons and made a ",
                "beeline to the guard ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "outpost on the west side ",
                "of the island. ",
                "Yeager and his buddies ",
                "then fought the post ",
                "guards in a bloody battle. ",
                "Roderick died as did ",
                "several of Yeager's ",
                "buddies. ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "Immediately after the last ",
                "guard fell, one of ",
                "Yeager's men yelled ",
                "�I've got them�, then ",
                "they all ran for the ",
                "storage room. ",
                "",
                "When we arrived at the ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "storage room, Yeager and ",
                "his crew were gone, but ",
                "the teleporter was now ",
                "active! We tried to ",
                "enter, but it would not ",
                "allow it! ",
                "We'll try again tomorrow. ",
                "",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "--day 42",
                "We've attempted to ",
                "escape several times now, ",
                "but to no avail. We have ",
                "however amassed a good ",
                "supply of weapons, ",
                "reagents, and other ",
                "essentials. ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "--day 45 ",
                "Ha! Yeager is back on ",
                "The Rock! We had a good ",
                "talk today and he told ",
                "me that the storage ",
                "room only takes you as ",
                "far as the old private ",
                "cave. He says it's the ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "stairs in the rear of the ",
                "cave that lead up to the ",
                "lighthouse and freedom; ",
                "but beware the cave ",
                "guardian, he shall let none ",
                "pass without a fight! ",
                "",
                "",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "--day 52 ",
                "Me and Yeager are ",
                "getting out! We've ",
                "decided to let one of the ",
                "larger crews here at the ",
                "prison do the work of ",
                "killing all the guards, then ",
                "we'll roll them in the ",
            };
            Pages[cnt++].Lines = lines;
            lines = new string[]
            {
                "storage room for their ",
                "lighthouse passes. ",
                "I'm not spending another ",
                "night here. ",
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

        public TheCellBook(Serial serial)
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