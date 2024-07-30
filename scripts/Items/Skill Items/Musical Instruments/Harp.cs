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

/* Scripts\Items\Skill Items\Musical Instruments\Harp.cs
 * ChangeLog
 *  2/19/22, Adam
 *      Obsoleting the specific Award instruments (AwardHarp, AwardLapHarp, and AwardLute) in favor of AwardInstrument
 *  12/8/21, Adam (AwardHarp)
 *      Initial checkin of AwardHarp.
 *      OVERVIEW:
 *      This instrument carries the award title
 *      names of the winner or 2nd, or 3rd place.
 *      How they placed, Song Name, Song notes, and year recorded.
 *      All props can be set through the properties, with one exception:
 *      Props (and set) have limits on the length of a string. I've created a new [cat (concatenate) command that concatenates strings for the named property. E.g, 
 *      [cat song eh 0.2 eh 0.2 eh 0.5 eh 0.2 eh 0.2 eh 0.5 eh 0.2 gh 0.2 c 0.2 dh 0.2 eh 1.0|
 *      [cat song fh 0.2 fh 0.2 fh 0.5 fh fh fh 0.2 eh 0.2 eh 0.5 eh eh eh 0.2 dh 0.1 dh 0.2 eh 0.2 dh 0.5 gh|
 *      etc.
 *      You'll notice the trailing pipe character '|'. This is not processed by [cat, but is used by the AwardHarp to understand verses.
 *  12/8/21, Adam (AwardHarp)
 *      Upgraded to accept song files from the Data/'Song File' directory
 *      Much more convenient than [cat
 *  12/9/21, Adam (AwardHarp)
 *      1. Upgraded parser to support Razor syntax for
 *      PLAY
 *      SAY
 *      WAIT
 *      2. The parser also pulls the Author, how they placed, and the song name from the header of the songfile.rzr if they were supplied.
 *      3. Double the harp again to cancel playback
 */

namespace Server.Items
{
    public class Harp : BaseInstrument
    {
        [Constructable]
        public Harp()
            : base(0xEB1, 0x43, 0x44)
        {
            Weight = 35.0;
        }

        public Harp(Serial serial)
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

            if (Weight == 3.0)
                Weight = 35.0;
        }
    }

    public class AwardHarp : RazorInstrument
    {
        //[Constructable]
        public AwardHarp()
            : base(0xEB1, 0x43, 0x44)
        {
            Weight = 35.0;
        }

        public AwardHarp(Serial serial)
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

            if (Weight == 3.0)
                Weight = 35.0;
        }
    }
}