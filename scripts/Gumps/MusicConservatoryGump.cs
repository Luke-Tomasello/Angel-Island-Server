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

/* Scripts/Gumps/MusicConservatoryGump.cs
 * CHANGELOG:
 *  11/17/26, Adam
 *      Initial Version. The Angel Island Music Conservatory's welcome for a new character, shown a few seconds after
 *      it is created (CharacterCreation): the composer's crate first (a music box, manuscript paper and composition
 *      books), then what Test Center's welcome (TestCenterGump) says: set your skills and stats ('help'), and what is
 *      waiting in the bank box.
 *      An approved song is also published in the music catalog at game-master.net, free to listen to and download.
 */

namespace Server.Gumps
{
    public class MusicConservatoryGump : Gump
    {
        public MusicConservatoryGump(Mobile m)
            : base((640 - 440) / 2, (480 - 450) / 2)
        {
            // incarcerated: no welcome
            if (Utility.IsIncarcerated(m))
                return;

            int width = 440;
            int height = 450;
            int headerColor = 0x7FFF;
            int contentColor = 0xFFFFFF;

            Closable = true;

            AddPage(0);

            AddBackground(0, 0, width, height, 5054);

            AddImageTiled(10, 10, width - 20, 20, 2624);
            AddAlphaRegion(10, 10, width - 20, 20);
            AddHtml(10, 10, width - 20, 20,
                string.Format("<BASEFONT COLOR=#{0:X6}>Welcome to the {1}</BASEFONT>", headerColor, Core.MusicConservatoryName),
                false,
                false);

            AddImageTiled(10, 40, width - 20, height - 80, 2624);
            AddAlphaRegion(10, 40, width - 20, height - 80);

            AddHtml(10, 40, width - 20, height - 80,
                string.Format("<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>",
                    contentColor,
                    "Compose here. Share everywhere.<br><br>" +
                    "For your compositions, the magenta crate in your bank box holds:<br>" +
                    "- A music box. Place it in your house and give it your sheet music: once a song is approved, it's " +
                    "shared with every Angel Island shard.<br>" +
                    "- 50 sheets of manuscript paper. Write a song in a composition book, then use a sheet of paper on the " +
                    "book to transcribe it into sheet music.<br>" +
                    "- 5 music composition books.<br><br>" +
                    "Once approved, your song is also published in the Angel Island music catalog at game-master.net, " +
                    "where anyone can listen to it and download it for free.<br><br>" +
                    "You are able to customize your character's stats and skills any way you wish. To see the commands to do this just say 'help'.<br>" +
                    "You will also find the following waiting for you in your bank box:<br>" +
                    "- Bank checks worth nearly 1.5 million gold<br>" +
                    "- A spellbook and a bag of reagents<br>" +
                    "- Various tools like, tinker tools, saw, mortar and pestle, etc.<br>" +
                    "- Various raw materials like ingots, logs, feathers, hides, etc.<br>" +
                    "- Nine unmarked recall runes<br>" +
                    "- A keg of each potion<br>" +
                    "- Two of each level of treasure map<br>" +
                    "- 60,000 gold pieces. Spend it as you see fit and enjoy yourself!"),
                false,
                true);

            AddImageTiled(10, height - 30, width - 20, 20, 2624);
            AddAlphaRegion(10, height - 30, width - 20, 20);

            AddButton(10, height - 30, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, height - 30, 170, 20, 1011036, 32767, false, false); // OKAY
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
        }
    }
}
