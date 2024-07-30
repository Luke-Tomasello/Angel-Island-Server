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

/* Scripts\Items\Addons\Cannon.cs
 * CHANGELOG
 *  4/10/2024, Adam
 *      Initial version.
 */

namespace Server.Items
{
    public class CannonAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new CannonDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public CannonAddon()
            : this(1)
        {
        }

        [Constructable]
        public CannonAddon(int type)
        {
            switch (type)
            {
                case 0: // east
                    {
                        /*
                         <ItemID>3732</ItemID>
                          <X>-1</X>
                          <Y>0</Y>
                          <Z>0</Z>
                        </Component>
                        <Component Flags="0x00">
                          <ItemID>3733</ItemID>
                          <X>0</X>
                          <Y>0</Y>
                          <Z>0</Z>
                        </Component>
                        <Component Flags="0x00">
                          <ItemID>3734</ItemID>
                          <X>1</X>
                          <Y>0</Y>
                          <Z>0</Z>
                        */
                        AddComponent(new AddonComponent(3732), -1, 0, 0);
                        AddComponent(new AddonComponent(3733), 0, 0, 0);
                        AddComponent(new AddonComponent(3734), 1, 0, 0);
                        break;
                    }
                case 1: // south
                    {
                        /*
                         <ItemID>3731</ItemID>
                          <X>0</X>
                          <Y>-1</Y>
                          <Z>0</Z>
                        </Component>
                        <Component Flags="0x00">
                          <ItemID>3730</ItemID>
                          <X>0</X>
                          <Y>0</Y>
                          <Z>0</Z>
                        </Component>
                        <Component Flags="0x00">
                          <ItemID>3729</ItemID>
                          <X>0</X>
                          <Y>1</Y>
                          <Z>0</Z> 
                         */
                        AddComponent(new AddonComponent(3731), 0, -1, 0);
                        AddComponent(new AddonComponent(3730), 0, 0, 0);
                        AddComponent(new AddonComponent(3729), 0, 1, 0);
                        break;
                    }
            }
        }

        public CannonAddon(Serial serial)
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

    public class CannonDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "a cannon deed"; } }
        public override BaseAddon Addon { get { return new CannonAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Cannon (East)",
                "Cannon (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public CannonDeed()
            : base()
        {
        }

        public CannonDeed(Serial serial)
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