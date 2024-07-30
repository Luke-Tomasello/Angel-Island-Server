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

/* Scripts/Engines/BulkOrders/LargeSmithBOD.cs
 * CHANGELOG:
 *  11/22/23, Yoar
 *      Moved Randomization to LargeBOD
 *  10/31/21, Yoar
 *      Added BulkOrderType getter
 */

namespace Server.Engines.BulkOrders
{
    [TypeAlias("Scripts.Engines.BulkOrders.LargeSmithBOD")]
    public class LargeSmithBOD : LargeBOD
    {
        public override BulkOrderSystem System { get { return DefSmith.System; } }

        [Constructable]
        public LargeSmithBOD()
            : this(0, false, BulkMaterialType.None, new LargeBulkEntry[0])
        {
            Randomize();
        }

        public LargeSmithBOD(int amountMax, bool requireExeptional, BulkMaterialType material, LargeBulkEntry[] entries)
            : base(amountMax, requireExeptional, material, entries)
        {
        }

        public LargeSmithBOD(Serial serial)
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