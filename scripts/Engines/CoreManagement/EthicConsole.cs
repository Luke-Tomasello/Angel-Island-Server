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

/* scripts\Engines\CoreManagement\EthicConsole.cs
 * CHANGELOG:
 *  1/10/23, Yoar
 *      Initial commit.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

using Server.Ethics;

namespace Server.Engines.Consoles
{
    public class EthicConsole : BaseConsole
    {
        [CommandProperty(AccessLevel.Administrator)]
        public bool Enabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.EthicsEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.EthicsEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.EthicsEnabled);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool EnabledResult => Core.Ethics;   // takes publish/date into consideration

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int MaxLifeForce
        {
            get => EthicConfig.MaxLifeForce;
            set => EthicConfig.MaxLifeForce = value;
        }

        [Constructable]
        public EthicConsole()
            : base("Ethic Settings Console")
        {
        }

        public EthicConsole(Serial serial)
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