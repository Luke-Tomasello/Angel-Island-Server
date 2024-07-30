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

/* Scripts\Engines\CoreManagement\CustomMobManagementConsole.cs
 * ChangeLog
 *  1/5/2024, Adam 
 *		Created.
 */

namespace Server.Items
{
    [NoSort]
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class CustomMobManagementConsole : Item
    {
        [Constructable]
        public CustomMobManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 1151;
            Name = "Custom Mob Management Console";
        }

        public CustomMobManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Administrator)]
        public string HireMinstrelSection
        {
            get
            {
                return "Hire Minstrel Section";
            }
        }

        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public double BasePay
        {
            get
            {
                return CoreAI.MinstrelBasePay;
            }
            set
            {
                CoreAI.MinstrelBasePay = value;
            }
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
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