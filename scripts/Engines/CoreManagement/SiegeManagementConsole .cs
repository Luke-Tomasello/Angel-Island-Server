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

/* OVERVIEW
 * This module controls those settings which are common to both Siege and Mortalis.
 *  Unlike ItemManagementConsole which controls items globally and specifically for Angel Island
 */

/* Scripts\Engines\CoreManagement\SiegeManagementConsole .cs
 * ChangeLog
 *  9/6/22, Yoar
 *      Add a switch for EnableIslandSiegeZone
 * First time check in
 *  8/26/22, Adam (SiegeBless)
 *      Add toggle for siege bless.
 */

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class SiegeManagementConsole : Item
    {
        [Constructable]
        public SiegeManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 0x534;
            Name = "Siege Management Console";
        }
        public SiegeManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool EnableSiegeBless
        {
            //SiegeBless
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SiegeBless);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.SiegeBless);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.SiegeBless);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool EnableIslandSiegeZone
        {
            get { return WorldZone.ActiveZone != null; }
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
            {
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            }
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