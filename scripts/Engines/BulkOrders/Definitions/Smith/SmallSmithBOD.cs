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

/* Scripts/Engines/BulkOrders/SmallSmithBOD.cs
 * CHANGELOG:
 *  11/22/23, Yoar
 *      Moved Randomization to SmallBOD
 *  10/31/21, Yoar
 *      Added BulkOrderType getter
 */

using System;

namespace Server.Engines.BulkOrders
{
    [TypeAlias("Scripts.Engines.BulkOrders.SmallSmithBOD")]
    public class SmallSmithBOD : SmallBOD
    {
        public override BulkOrderSystem System { get { return DefSmith.System; } }

        [Constructable]
        public SmallSmithBOD()
            : this(0, false, BulkMaterialType.None, 0, null, 0, 0)
        {
            Randomize();
        }

        public SmallSmithBOD(int amountMax, bool reqExceptional, BulkMaterialType material, int amountCur, Type type, int number, int graphic)
            : base(amountMax, reqExceptional, material, amountCur, type, number, graphic)
        {
        }

        public SmallSmithBOD(Serial serial)
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
        }
    }
}