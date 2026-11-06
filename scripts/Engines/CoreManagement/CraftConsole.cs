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

/* scripts\Engines\CoreManagement\CraftConsole.cs
 * CHANGELOG:
 *  1/10/23, Yoar
 *      Initial commit.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

using Server.Items;

namespace Server.Engines.Consoles
{
    public class CraftConsole : BaseConsole
    {
        [CommandProperty(AccessLevel.Administrator)]
        public double TownshipWallCostFactor
        {
            get => CoreAI.TownshipWallCostFactor;
            set => CoreAI.TownshipWallCostFactor = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool DragonScaleEnabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.DragonScaleArmor);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.DragonScaleArmor);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.DragonScaleArmor);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool DSEnabledResult => BaseScales.Enabled;

        [Constructable]
        public CraftConsole()
            : base("Craft Settings Console")   // Fixed typo from original "Ethic Settings Console"
        {
        }

        public CraftConsole(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
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