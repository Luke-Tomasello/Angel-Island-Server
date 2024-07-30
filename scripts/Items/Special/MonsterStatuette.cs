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

/* Scripts\Items\Special\MonsterStatuette.cs
 * Changelog:
 *	4/9/05, Adam
 *		Changed LootType.Blessed to LootType.Regular
 *	4/07/05, Kitaras
 *		Added new overload for MonsterStatuetteInfo to except new statues useing strings vs clilocs.
 *	8/7/04, Old Salty
 * 		Statuettes no longer play sounds when AccessLevel > Player.
 *                
 *	8/29/04 Lego Eater
 *              changed lich statue to play basesoundid of 412             
 *
 *                 
 */

using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    public enum MonsterStatuetteType
    {
        Crocodile,
        Daemon,
        Dragon,
        EarthElemental,
        Ettin,
        Gargoyle,
        Gorilla,
        Lich,
        Lizardman,
        Ogre,
        Orc,
        Ratman,
        Skeleton,
        Troll,
        Cow,
        Zombie,
        Llama,
        Ophidian,
        Reaper,
        Mongbat,
        Gazer,
        FireElemental,
        Wolf,
        SolenWorker,
        TerathanAvenger,
        GiantRat,
        HordeDemon,
        BillyGoat,
        GrizzlyBear,
        Ghost,
        Ghoul,
        SeaHorse,
        Genie,
        Pixie,
        Unicorn
    }

    public class MonsterStatuetteInfo
    {
        private int m_LabelNumber;
        private int m_ItemID;
        private int[] m_Sounds;
        private string m_Description;

        public int LabelNumber { get { return m_LabelNumber; } }
        public int ItemID { get { return m_ItemID; } }
        public int[] Sounds { get { return m_Sounds; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                m_Description = value;
                //InvalidateProperties();
            }
        }

        public MonsterStatuetteInfo(string labelNumber, int itemID, int baseSoundID)
        {
            m_Description = labelNumber;
            m_ItemID = itemID;
            m_Sounds = new int[] { baseSoundID, baseSoundID + 1, baseSoundID + 2, baseSoundID + 3, baseSoundID + 4 };
        }

        public MonsterStatuetteInfo(int labelNumber, int itemID, int baseSoundID)
        {
            m_LabelNumber = labelNumber;
            m_ItemID = itemID;
            m_Sounds = new int[] { baseSoundID, baseSoundID + 1, baseSoundID + 2, baseSoundID + 3, baseSoundID + 4 };
        }

        private static MonsterStatuetteInfo[] m_Table = new MonsterStatuetteInfo[]
            {
				/* Crocodile */			new MonsterStatuetteInfo( 1041249, 0x20DA, 660 ),
				/* Daemon */			new MonsterStatuetteInfo( 1041250, 0x20D3, 357 ),
				/* Dragon */			new MonsterStatuetteInfo( 1041251, 0x20D6, 362 ),
				/* EarthElemental */		new MonsterStatuetteInfo( 1041252, 0x20D7, 268 ),
				/* Ettin */			new MonsterStatuetteInfo( 1041253, 0x20D8, 367 ),
				/* Gargoyle */			new MonsterStatuetteInfo( 1041254, 0x20D9, 372 ),
				/* Gorilla */			new MonsterStatuetteInfo( 1041255, 0x20F5, 158 ),
				/* Lich */			new MonsterStatuetteInfo( 1041256, 0x20F8, 412 ),
				/* Lizardman */			new MonsterStatuetteInfo( 1041257, 0x20DE, 417 ),
				/* Ogre */			new MonsterStatuetteInfo( 1041258, 0x20DF, 427 ),
				/* Orc */			new MonsterStatuetteInfo( 1041259, 0x20E0, 1114 ),
				/* Ratman */			new MonsterStatuetteInfo( 1041260, 0x20E3, 437 ),
				/* Skeleton */			new MonsterStatuetteInfo( 1041261, 0x20E7, 1165 ),
				/* Troll */			new MonsterStatuetteInfo( 1041262, 0x20E9, 461 ),
				/* Cow */			new MonsterStatuetteInfo( 1041263, 0x2103, 120 ),
				/* Zombie */			new MonsterStatuetteInfo( 1041264, 0x20EC, 471 ),
				/* Llama */			new MonsterStatuetteInfo( 1041265, 0x20F6, 1011 ),
				/* Ophidian */			new MonsterStatuetteInfo( 1049742, 0x2133, 634 ),
				/* Reaper */			new MonsterStatuetteInfo( 1049743, 0x20FA, 442 ),
				/* Mongbat */			new MonsterStatuetteInfo( 1049744, 0x20F9, 422 ),
				/* Gazer */			new MonsterStatuetteInfo( 1049768, 0x20F4, 377 ),
				/* FireElemental */		new MonsterStatuetteInfo( 1049769, 0x20F3, 838 ),
				/* Wolf */			new MonsterStatuetteInfo( 1049770, 0x2122, 229 ),
				/* Solen Worker */		new MonsterStatuetteInfo( "Solen Worker Statuette", 9732, 959 ),
				/* Terathan Avenger */		new MonsterStatuetteInfo( "Terathan Avenger Statuette", 9674, 0x24D ),
				/* Giant Rat*/			new MonsterStatuetteInfo( "Giant Rat Statuette", 8400, 0x188 ),
				/* Horde Demon, */		new MonsterStatuetteInfo( "Horde Demon Statuette", 9745, 357),
				/* Billy Goat,  */		new MonsterStatuetteInfo( "Billy Goat Statuette", 9600, 0x99),
				/* Grizzly Bear,*/		new MonsterStatuetteInfo( "Grizzly Bear Statuette", 8478, 0xA3 ),
				/* Ghost, */			new MonsterStatuetteInfo( "Ghost Statuette", 9671,  0x482),
				/* Ghoul,*/			new MonsterStatuetteInfo( "Ghoul Statuette", 8457, 0x482 ),
				/* Sea Horse */			new MonsterStatuetteInfo( "Sea Horse Statuette", 9658, 357 ),
				/* Genie,  */			new MonsterStatuetteInfo( "Genie Statuette", 9616,  768),
				/* Pixie,  */			new MonsterStatuetteInfo( "Pixie Statuette", 9654, 0x467 ),
				/* Unicorn  */			new MonsterStatuetteInfo( "Unicorn Statuette", 9678, 0x4BC )
            };

        public static MonsterStatuetteInfo GetInfo(MonsterStatuetteType type)
        {
            int v = (int)type;

            if (v < 0 || v >= m_Table.Length)
                v = 0;

            return m_Table[v];
        }
    }

    public class MonsterStatuette : Item, Engines.VeteranRewards.IRewardItem
    {
        private MonsterStatuetteType m_Type;
        private bool m_TurnedOn;
        private bool m_IsRewardItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get { return m_IsRewardItem; }
            set { m_IsRewardItem = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TurnedOn
        {
            get { return m_TurnedOn; }
            set { m_TurnedOn = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MonsterStatuetteType Type
        {
            get { return m_Type; }
            set
            {
                m_Type = value;
                ItemID = MonsterStatuetteInfo.GetInfo(m_Type).ItemID;
                InvalidateProperties();
            }
        }

        public override int LabelNumber
        {
            get { return MonsterStatuetteInfo.GetInfo(m_Type).LabelNumber; }
        }

        [Constructable]
        public MonsterStatuette()
            : this(MonsterStatuetteType.Crocodile)
        {
        }

        [Constructable]
        public MonsterStatuette(MonsterStatuetteType type)
            : base(MonsterStatuetteInfo.GetInfo(type).ItemID)
        {
            Weight = 1.0;
            LootType = LootType.Regular;
            Name = MonsterStatuetteInfo.GetInfo(type).Description;
            m_Type = type;
        }

        public override bool HandlesOnMovement { get { return m_TurnedOn && IsLockedDown; } }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (!(m.AccessLevel > AccessLevel.Player) && m_TurnedOn && IsLockedDown && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
            {
                int[] sounds = MonsterStatuetteInfo.GetInfo(m_Type).Sounds;

                Effects.PlaySound(this.Location, this.Map, sounds[Utility.Random(sounds.Length)]);
            }

            base.OnMovement(m, oldLocation);
        }

        public MonsterStatuette(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_TurnedOn)
                list.Add(502695); // turned on
            else
                list.Add(502696); // turned off
        }

        public bool IsOwner(Mobile mob)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            return (house != null && house.IsOwner(mob));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsOwner(from))
            {
                OnOffGump onOffGump = new OnOffGump(this);
                from.SendGump(onOffGump);
            }
            else
            {
                from.SendLocalizedMessage(502691); // You must be the owner to use this.
            }
        }

        private class OnOffGump : Gump
        {
            private MonsterStatuette m_Statuette;

            public OnOffGump(MonsterStatuette statuette)
                : base(150, 200)
            {
                m_Statuette = statuette;

                AddBackground(0, 0, 300, 150, 0xA28);

                AddHtmlLocalized(45, 20, 300, 35, statuette.TurnedOn ? 1011035 : 1011034, false, false); // [De]Activate this item

                AddButton(40, 53, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(80, 55, 65, 35, 1011036, false, false); // OKAY

                AddButton(150, 53, 0xFA5, 0xFA7, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(190, 55, 100, 35, 1011012, false, false); // CANCEL
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile from = sender.Mobile;

                if (info.ButtonID == 1)
                {
                    bool newValue = !m_Statuette.TurnedOn;
                    m_Statuette.TurnedOn = newValue;

                    if (newValue && !m_Statuette.IsLockedDown)
                        from.SendLocalizedMessage(502693); // Remember, this only works when locked down.
                }
                else
                {
                    from.SendLocalizedMessage(502694); // Cancelled action.
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteEncodedInt((int)m_Type);
            writer.Write((bool)m_TurnedOn);
            writer.Write((bool)m_IsRewardItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Type = (MonsterStatuetteType)reader.ReadEncodedInt();
                        m_TurnedOn = reader.ReadBool();
                        m_IsRewardItem = reader.ReadBool();
                        break;
                    }
            }
        }
    }
}