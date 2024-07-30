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

/* Scripts\Items\Skill Items\Musical Instruments\LapHarp.cs
 * Changelog
 *  2/19/22, Adam
 *      Obsoleting the specific Award instruments (AwardHarp, AwardLapHarp, and AwardLute) in favor of AwardInstrument
 */

namespace Server.Items
{
    public class LapHarp : BaseInstrument
    {
        [Constructable]
        public LapHarp()
            : base(0xEB2, 0x45, 0x46)
        {
            Weight = 10.0;
        }

        public LapHarp(Serial serial)
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
                Weight = 10.0;
        }
    }

    public class AwardLapHarp : RazorInstrument
    {
        //[Constructable]
        public AwardLapHarp()
            : base(0xEB2, 0x45, 0x46)
        {
            Weight = 10.0;
        }

        public AwardLapHarp(Serial serial)
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
                Weight = 10.0;
        }
    }
}