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

/* Scripts\Items\Addons\DisplayCaseTinyAddon.cs
 * CHANGELOG:
 *  2/5/2024, Adam
 *      Add 'facing' chooser support. 
 */

namespace Server.Items
{
    public class DisplayCaseTinyAddon : BaseAddon
    {
        public override BaseAddonDeed Deed
        {
            get
            {
                return new DisplayCaseTinyAddonDeed();
            }
        }

        [Constructable]
        public DisplayCaseTinyAddon(int type)
        {
            switch (type)
            {
                case 0: // south
                    {
                        AddComponent(new AddonComponent(2825), 0, 0, 2);
                        AddComponent(new AddonComponent(2827), 0, 0, 0);
                        AddComponent(new AddonComponent(2827), 0, 0, 4);
                        break;
                    }
                case 1: // east
                    {
                        AddComponent(new AddonComponent(2826), 0, 0, 2);
                        AddComponent(new AddonComponent(2828), 0, 0, 0);
                        AddComponent(new AddonComponent(2828), 0, 0, 4);
                        break;
                    }
            }
        }
        [Constructable]
        public DisplayCaseTinyAddon()
            : this(0)
        {

        }

        public DisplayCaseTinyAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class DisplayCaseTinyAddonDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new DisplayCaseTinyAddon(m_Type);
            }
        }
        public override string DefaultName { get { return "a tiny display case deed"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "display case (South)",
                "display case (East)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }
        [Constructable]
        public DisplayCaseTinyAddonDeed()
        {

        }

        public DisplayCaseTinyAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}