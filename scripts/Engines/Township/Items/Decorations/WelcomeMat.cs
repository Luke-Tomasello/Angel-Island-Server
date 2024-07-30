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

/* Engines/Township/Items/Decorations/WelcomeMat.cs
 * CHANGELOG:
 * 3/17/22, Adam
 *  Update OnBuild to use new parm list
 * 1/13/21, Yoar
 *      Removed instance list. Township item registry is now dealt with by TownshipItemRegistry.
 * 11/24/21, Yoar
 *      Initial version.
*/

using Server.Regions;

namespace Server.Township
{
    public class WelcomeMat : TownshipFloor
    {
        [Constructable]
        public WelcomeMat()
            : base(0x28A8) // goza mat
        {
            Name = "Welcome Mat";
        }

        public override void OnBuild(Mobile from)
        {
            TownshipRegion tsi = TownshipRegion.GetTownshipAt(from.Location, from.Map);

            if (tsi != null && tsi.TStone != null)
            {
                Item existing = tsi.TStone.WelcomeMat;

                if (existing != null)
                    existing.Delete(); // delete existing welcome mat

                tsi.TStone.WelcomeMat = this;
            }

            base.OnBuild(from);
        }

        public WelcomeMat(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}