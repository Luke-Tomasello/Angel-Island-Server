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

/* Scripts\Engines\CoreManagement\SiegeManagementConsole.cs
 * ChangeLog
 *  9/6/22, Yoar
 *      Add a switch for EnableIslandSiegeZone
 *  8/26/22, Adam (SiegeBless)
 *      Add toggle for siege bless.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

namespace Server.Engines.Consoles
{
    public class SiegeManagementConsole : BaseConsole
    {
        [Constructable]
        public SiegeManagementConsole()
            : base("Siege Management Console")
        {
        }

        public SiegeManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool EnableSiegeBless
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SiegeBless);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.SiegeBless);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.SiegeBless);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool EnableIslandSiegeZone
        {
            get => WorldZone.ActiveZone != null;
            set
            {
                if (value)
                    WorldZone.ActiveZone = WorldZone.LoadZone("island-siege");
                else
                    WorldZone.ActiveZone = null;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}