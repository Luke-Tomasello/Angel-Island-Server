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

/* Scripts\Engines\CoreManagement\ItemManagementConsole.cs
 * ChangeLog
 *  8/12/22, Adam
 *      Remove command access for FreezeDryEnabled.
 *	8/9/22, Adam
 *		Created.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

using Server.Items;

namespace Server.Engines.Consoles
{
    public class ItemManagementConsole : BaseConsole
    {
        [Constructable]
        public ItemManagementConsole()
            : base("Item Management Console")
        {
        }

        public ItemManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool ArmorAbsorbClassic
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.ArmorAbsorbClassic);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.ArmorAbsorbClassic);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.ArmorAbsorbClassic);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int ArmorDexLossStrFactor
        {
            get => BaseArmor.StrFactor;
            set => BaseArmor.StrFactor = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool DebugDecay
        {
            get => CoreAI.DebugItemDecayOutput;
            set => CoreAI.DebugItemDecayOutput = value;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
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