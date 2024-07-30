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

/* Scripts\Engines\CoreManagement\CropManagementConsole.cs
 * CHANGELOG:
 *  10/1/2023, Adam
 *      Initial version.
 */

using Server.Gumps;
using Server.Items;

namespace Server.Engines
{
    [NoSort]
    public class CropManagementConsole : Item
    {
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static double FarmableCotton
        {
            get { return CoreAI.FarmableCotton; }
            set
            {
                CoreAI.FarmableCotton = value;
                foreach (Item item in World.Items.Values)
                    if (item is GridSpawner gs)
                        if (gs.Spawns("FarmableCotton"))
                        {
                            gs.Sparsity = value;
                            gs.RemoveObjects();
                            gs.ScheduleRespawn = true;
                        }
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public static int CottonPlots
        {
            get
            {
                int count = 0;
                foreach (Item item in World.Items.Values)
                    if (item is GridSpawner gs)
                        if (gs.Spawns("FarmableCotton"))
                            count++;
                return count;
            }
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static double FarmableFlax
        {
            get { return CoreAI.FarmableFlax; }
            set
            {
                CoreAI.FarmableFlax = value;
                foreach (Item item in World.Items.Values)
                    if (item is GridSpawner gs)
                        if (gs.Spawns("FarmableFlax"))
                        {
                            gs.Sparsity = value;
                            gs.RemoveObjects();
                            gs.ScheduleRespawn = true;
                        }
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public static int FlaxPlots
        {
            get
            {
                int count = 0;
                foreach (Item item in World.Items.Values)
                    if (item is GridSpawner gs)
                        if (gs.Spawns("FarmableFlax"))
                            count++;
                return count;
            }
        }
        [Constructable]
        public CropManagementConsole()
            : base(0x1F14)
        {
            Name = "Crop Management Console";
            Weight = 1.0;
            Hue = 0x44F;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Counselor)
                from.SendGump(new PropertiesGump(from, this));
        }

        public CropManagementConsole(Serial serial)
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