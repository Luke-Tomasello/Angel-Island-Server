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

using Server.Township;

namespace Server.Items
{
    [TownshipAddon]
    public class LightwoodPodiumAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new LightwoodPodiumAddonDeed(); } }

        private static readonly int[] m_ItemIDs = new int[]
            {
                0x721, // Podium
                0x722, // Stairs (North)
                0x725, // Stairs (East)
                0x724, // Stairs (South)
                0x723, // Stairs (West)
                0x729, // Stairs (Corner, North-East)
                0x726, // Stairs (Corner, South-East)
                0x728, // Stairs (Corner, South-West)
                0x727, // Stairs (Corner, North-West)
                0x731, // Stairs (L-Shape, North-East)
                0x72E, // Stairs (L-Shape, South-East)
                0x730, // Stairs (L-Shape, South-West)
                0x72F, // Stairs (L-Shape, North-West)
                0x72C, // Stairs (Round, North-East)
                0x72A, // Stairs (Round, South-East)
                0x72D, // Stairs (Round, South-West)
                0x72B, // Stairs (Round, North-West)
            };

        [Constructable]
        public LightwoodPodiumAddon()
            : this(0)
        {
        }

        [Constructable]
        public LightwoodPodiumAddon(int type)
        {
            if (type >= 0 && type < m_ItemIDs.Length)
                AddComponent(new AddonComponent(m_ItemIDs[type]), 0, 0, 0);
        }

        public LightwoodPodiumAddon(Serial serial)
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

    public class LightwoodPodiumAddonDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new LightwoodPodiumAddon(m_Type); } }
        public override string DefaultName { get { return "lightwood podium"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Podium",
                "Stairs (North)",
                "Stairs (East)",
                "Stairs (South)",
                "Stairs (West)",
                "Stairs (Corner, North-East)",
                "Stairs (Corner, South-East)",
                "Stairs (Corner, South-West)",
                "Stairs (Corner, North-West)",
                "Stairs (L-Shape, North-East)",
                "Stairs (L-Shape, South-East)",
                "Stairs (L-Shape, South-West)",
                "Stairs (L-Shape, North-West)",
                "Stairs (Round, North-East)",
                "Stairs (Round, South-East)",
                "Stairs (Round, South-West)",
                "Stairs (Round, North-West)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public LightwoodPodiumAddonDeed()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public LightwoodPodiumAddonDeed(Serial serial)
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

    [TownshipAddon]
    public class DarkwoodPodiumAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new DarkwoodPodiumAddonDeed(); } }

        private static readonly int[] m_ItemIDs = new int[]
            {
                0x738, // Podium
                0x739, // Stairs (North)
                0x73C, // Stairs (East)
                0x73B, // Stairs (South)
                0x73A, // Stairs (West)
                0x740, // Stairs (Corner, North-East)
                0x73D, // Stairs (Corner, South-East)
                0x73F, // Stairs (Corner, South-West)
                0x73E, // Stairs (Corner, North-West)
                0x744, // Stairs (L-Shape, North-East)
                0x741, // Stairs (L-Shape, South-East)
                0x743, // Stairs (L-Shape, South-West)
                0x742, // Stairs (L-Shape, North-West)
                0x746, // Stairs (Round, North-East)
                0x748, // Stairs (Round, South-East)
                0x747, // Stairs (Round, South-West)
                0x745, // Stairs (Round, North-West)
            };

        [Constructable]
        public DarkwoodPodiumAddon()
            : this(0)
        {
        }

        [Constructable]
        public DarkwoodPodiumAddon(int type)
        {
            if (type >= 0 && type < m_ItemIDs.Length)
                AddComponent(new AddonComponent(m_ItemIDs[type]), 0, 0, 0);
        }

        public DarkwoodPodiumAddon(Serial serial)
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

    public class DarkwoodPodiumAddonDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new DarkwoodPodiumAddon(m_Type); } }
        public override string DefaultName { get { return "darkwood podium"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Podium",
                "Stairs (North)",
                "Stairs (East)",
                "Stairs (South)",
                "Stairs (West)",
                "Stairs (Corner, North-East)",
                "Stairs (Corner, South-East)",
                "Stairs (Corner, South-West)",
                "Stairs (Corner, North-West)",
                "Stairs (L-Shape, North-East)",
                "Stairs (L-Shape, South-East)",
                "Stairs (L-Shape, South-West)",
                "Stairs (L-Shape, North-West)",
                "Stairs (Round, North-East)",
                "Stairs (Round, South-East)",
                "Stairs (Round, South-West)",
                "Stairs (Round, North-West)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public DarkwoodPodiumAddonDeed()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public DarkwoodPodiumAddonDeed(Serial serial)
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