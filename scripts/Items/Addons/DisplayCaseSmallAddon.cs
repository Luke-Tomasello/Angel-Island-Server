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

/* Scripts\Items\Addons\DisplayCaseSmallAddon.cs
 * CHANGELOG:
 *  2/5/2024, Adam
 *      Add 'facing' chooser support. 
 */

namespace Server.Items
{
    public class DisplayCaseSmallAddon : BaseAddon
    {
        public override BaseAddonDeed Deed
        {
            get
            {
                return new DisplayCaseSmallAddonDeed();
            }
        }
        [Constructable]
        public DisplayCaseSmallAddon(int type)
        {
            switch (type)
            {
                case 0: // south
                    {
                        AddComponent(new AddonComponent(2725), 0, 1, 5);
                        AddComponent(new AddonComponent(2834), 0, 1, 2);
                        AddComponent(new AddonComponent(2725), 0, 1, 0);
                        AddComponent(new AddonComponent(2721), 0, 0, 5);
                        AddComponent(new AddonComponent(2838), 0, 0, 2);
                        AddComponent(new AddonComponent(2723), 0, -1, 5);
                        AddComponent(new AddonComponent(2832), 0, -1, 2);
                        AddComponent(new AddonComponent(2723), 0, -1, 0);
                        AddComponent(new AddonComponent(2724), 1, -1, 5);
                        AddComponent(new AddonComponent(2835), 1, -1, 2);
                        AddComponent(new AddonComponent(2724), 1, -1, 0);
                        AddComponent(new AddonComponent(2719), 1, 0, 5);
                        AddComponent(new AddonComponent(2836), 1, 0, 2);
                        AddComponent(new AddonComponent(2840), 1, 1, 5);
                        AddComponent(new AddonComponent(2833), 1, 1, 2);
                        AddComponent(new AddonComponent(2840), 1, 1, 0);
                        AddonComponent ac = null;
                        ac = new AddonComponent(2723);
                        AddComponent(ac, 0, -1, 5);
                        ac = new AddonComponent(2721);
                        AddComponent(ac, 0, 0, 5);
                        ac = new AddonComponent(2832);
                        AddComponent(ac, 0, -1, 2);
                        ac = new AddonComponent(2838);
                        AddComponent(ac, 0, 0, 2);
                        ac = new AddonComponent(2723);
                        AddComponent(ac, 0, -1, 0);
                        break;
                    }
                case 1: // east
                    {
                        AddComponent(new AddonComponent(2840), 1, 1, 0);
                        AddComponent(new AddonComponent(2840), 1, 1, 5);
                        AddComponent(new AddonComponent(2833), 1, 1, 2);
                        AddComponent(new AddonComponent(2720), 0, 1, 5);
                        AddComponent(new AddonComponent(2837), 0, 1, 2);
                        AddComponent(new AddonComponent(2725), -1, 1, 0);
                        AddComponent(new AddonComponent(2725), -1, 1, 5);
                        AddComponent(new AddonComponent(2834), -1, 1, 2);
                        AddComponent(new AddonComponent(2723), -1, 0, 0);
                        AddComponent(new AddonComponent(2723), -1, 0, 5);
                        AddComponent(new AddonComponent(2832), -1, 0, 2);
                        AddComponent(new AddonComponent(2722), 0, 0, 5);
                        AddComponent(new AddonComponent(2839), 0, 0, 2);
                        AddComponent(new AddonComponent(2724), 1, 0, 0);
                        AddComponent(new AddonComponent(2724), 1, 0, 5);
                        AddComponent(new AddonComponent(2835), 1, 0, 2);
                        AddonComponent ac = null;
                        ac = new AddonComponent(2723);
                        AddComponent(ac, -1, 0, 0);
                        ac = new AddonComponent(2722);
                        AddComponent(ac, 0, 0, 5);
                        ac = new AddonComponent(2723);
                        AddComponent(ac, -1, 0, 5);
                        ac = new AddonComponent(2839);
                        AddComponent(ac, 0, 0, 2);
                        ac = new AddonComponent(2832);
                        AddComponent(ac, -1, 0, 2);
                        break;
                    }
            }
        }
        [Constructable]
        public DisplayCaseSmallAddon()
            : this(0)
        {

        }

        public DisplayCaseSmallAddon(Serial serial)
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

    public class DisplayCaseSmallAddonDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new DisplayCaseSmallAddon(m_Type);
            }
        }
        public override string DefaultName { get { return "a small display case deed"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "display case (South)",
                "display case (East)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }
        [Constructable]
        public DisplayCaseSmallAddonDeed()
        {
        }

        public DisplayCaseSmallAddonDeed(Serial serial)
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