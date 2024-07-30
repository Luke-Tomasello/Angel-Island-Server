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

/* Scripts/Engines/RTT/SmallPagedRTTGump.cs
 * CHANGELOG:
 *  8/26/2007, Pix
 *      InitialVersion
 */


using Server.Gumps;

namespace Server.RTT
{
    class SmallPagedRTTGump : BaseRTTGump
    {
        #region Button and Text descriptions

        private static int[] ItemIDS = new int[]
        {
            0xFAF, //anvil
			0xF9D, //sewing kit
			0xF9F, //scissors
			//0x11CD, //flowerpot //Something's wrong with this - too big?
			0x1443, //axe
			0x1AE2, //skull
			0x1BF4, //ingots
			//0x2329, //snowman //Too big for the gump :(
			0x1721, //bananas
			0x1712, //boots
			0x153E, //apron
			0x14F7, //anchor
			0x14F8, //rope
			0x13B9, //sword
			0x0EEF, //gold coins
			0x0F39, //shovel
			0x0F40, //arrows
			0x0E43, //wooden chest
			0x09E9, //cake
			0x09CF, //fish
			0x0993, //fruit basket
			0x0E77, //barrel
			0x0F9C, //bolt of cloth
			0x0FB6, //horseshoes
			0x100F, //key
			0x116E, //gravestone
			//0x113C, //leather tunic //Something's wrong with this - wrong ID?
			0x14F3, //ship model
			0x171A, //feathered hat
			0x1811, //hourglass
			0x1B76, //shield
			0x1BDE, //logs
			0x1E2F, //dartboard
			0x0EB2, //lap harp
			0x0E59, //stump
			0x0E2F, //crystal ball
			0x0EFA, //spellbook
			0x0F5D, //mace
			0x104B, //clock
			0x13A5, //pillow
			0x14F6, //spyglass
			0x1E13, //potted cactus
			0x1E68, //deer trophy

		};
        private static string[] ItemIDS_Strings = new string[]
        {
            "anvil",
            "sewing kit",
            "scissors",
			//"flowerpot", //Something's wrong with this - too big?
			"axe",
            "skull",
            "ingots",
			//"snowman", //Too big for the gump
			"bananas",
            "boots",
            "apron",
            "anchor",
            "rope",
            "sword",
            "gold coins",
            "shovel",
            "arrows",
            "wooden chest",
            "cake",
            "fish",
            "fruit basket",
            "barrel",
            "bolt of cloth",
            "horseshoes",
            "key",
            "gravestone",
			//"leather tunic", //Something's wrong with this - wrong ID?
			"ship model",
            "feathered hat",
            "hourglass",
            "shield",
            "logs",
            "dartboard",
            "lap harp",
            "stump",
            "crystal ball",
            "spellbook",
            "mace",
            "clock",
            "pillow",
            "spyglass",
            "potted cactus",
            "deer trophy",
        };


        #endregion

        private string m_CorrectDescription = "";

        private int[] itemids;
        private int[] buttonids;

        private const int LabelColor32 = 0xFFFFFF;

        public SmallPagedRTTGump(Mobile from, string strNotify, string strSkill)
            : base(from, strNotify, strSkill, 10, 10)
        {
            //Nothing to do here - SetupGump is called from the base class.
        }

        protected override void SetupGump()
        {
            //make sure they can't move it from this position
            this.Dragable = false;

            SetupArrays();

            int random = Utility.Random(0, 5);
            //random = 0;
            int border = 5 + random;
            random = Utility.Random(0, 10);
            int width = 540 + random;
            random = Utility.Random(0, 10);
            int questionpaneheight = 80 + random;
            random = Utility.Random(0, 10);
            int answerpaneheight = 100 + random;

            //build outer layer
            AddPage(0);
            AddBackground(0, 0, width, questionpaneheight + answerpaneheight + 3 * border, 5054);

            //AddBlackAlpha(border, border, width - 2*border, questionpaneheight);
            AddBackground(border, border, width - 2 * border, questionpaneheight, 5054);

            //AddBlackAlpha(border, 2 * border + questionpaneheight, width - 2 * border, answerpaneheight);
            AddBackground(border, 2 * border + questionpaneheight, width - 2 * border, answerpaneheight, 5054);

            AddHtml(border + 2, border + 2, width - 4 * border, 70, Color(this.Notification, LabelColor32), false, false);

            string queryString = "Pick the " + m_CorrectDescription + " from the choices below.";
            switch (Utility.RandomMinMax(1, 6))
            {
                case 1:
                    //use the default above
                    break;
                case 2:
                    queryString = "Pick the following from the choices below: " + m_CorrectDescription + ".";
                    break;
                case 3:
                    queryString = "Find the image of the " + m_CorrectDescription + " to continue.";
                    break;
                case 4:
                    queryString = "Select the picture of the " + m_CorrectDescription + " below to proceed.";
                    break;
                case 5:
                    queryString = "Select the " + m_CorrectDescription + " below.";
                    break;
                case 6:
                    queryString = "Find the " + m_CorrectDescription + " to continue.";
                    break;
            }
            AddHtml(border + 2, border + 2 + 60, width - 4 * border, 35, Color(queryString, LabelColor32), false, false);

            //Here we do the buttons on multiple 'pages':

            int x = border + 2;
            int y = 2 * border + questionpaneheight + 2;

            int numPages = (itemids.Length + 4) / 5;
            for (int i = 1; i <= numPages; i++)
            {
                AddPage(i);
                //AddBackground(border, 2 * border + questionpaneheight, width - 2 * border, answerpaneheight, 5054);
                int nextPage = i + 1;
                int prevPage = i - 1;
                if (nextPage > numPages) nextPage = 1;
                if (prevPage < 1) prevPage = numPages;

                AddButton(x, y, 9706, 9707, 0, GumpButtonType.Page, prevPage);
                AddHtml(x + 25, y, 50, 25, "Prev", false, false);

                for (int j = 0; j < 5; j++)
                {
                    int index = (i - 1) * 5 + j;
                    if (index < itemids.Length)
                    {
                        int thisItem = itemids[index];
                        int thisButtonID = buttonids[index] + this.CorrectResponseOffset;
                        AddItem(x + 30 + j * 90, y + 20, thisItem);
                        AddButton(x + 40 + j * 90, y + 70, 4005, 4007, thisButtonID, GumpButtonType.Reply, 0);
                    }
                }

                //AddHtml(x + 200, y + 2, 200, 35, i.ToString(), false, false);
                AddButton(x + 500, y, 9702, 9703, 0, GumpButtonType.Page, nextPage);
                AddHtml(x + 450, y, 50, 25, "Next", false, false);
            }
        }

        private void SetupArrays()
        {
            int len = ItemIDS.Length;
            itemids = new int[len];
            buttonids = new int[len];

            //fill arrays
            ItemIDS.CopyTo(itemids, 0);
            for (int b = 0; b < len; b++) buttonids[b] = b;

            //randomize arrays
            for (int a = 0; a < len; a++)
            {
                int swap = Utility.Random(a, len - a);
                int temp = itemids[a];
                itemids[a] = itemids[swap];
                itemids[swap] = temp;

                temp = buttonids[a];
                buttonids[a] = buttonids[swap];
                buttonids[swap] = temp;
            }

            //get correct responses
            this.CorrectResponse = Utility.Random(0, len);
            this.CorrectResponseOffset = Utility.Random(100, 100);

            m_CorrectDescription = ItemIDS_Strings[this.CorrectResponse];
        }


        #region Gump Utility Functions
        public void AddBlackAlpha(int x, int y, int width, int height)
        {
            AddImageTiled(x, y, width, height, 2624);
            AddAlphaRegion(x, y, width, height);
        }

        #endregion
    }
}