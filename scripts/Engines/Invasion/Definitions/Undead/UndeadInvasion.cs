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

/* Scripts\Engines\Invasion\Definitions\UndeadInvasion.cs
 * Changelog:
 *  10/7/23, Yoar
 *      Initial version.
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Engines.Invasion
{
    public class UndeadInvasion : InvasionSystem
    {
        public override InvasionType Type { get { return InvasionType.Undead; } }

        public UndeadInvasion()
            : base()
        {
            PetDamageScalar = 66;
            FactionScalar = 105;
            ArtifactChance = 0.15; // i.e. 15% chance for a balron, but much less for lower-tier mobs
            ArtifactMessage = "For your valor in combating the Undead horde, a special artifact has been bestowed on you.";
            LootChance = 2.0; // i.e. 200% chance for a balron, but much less for lower-tier mobs
            FameScaling = true;
            PartySharing = true;

            // kills needed to advance each tier
            Tiers = new int[]
                {
                    100,
                    100,
                    100,
                    60, // no new rewards, only stronger mobs
                };

            CreatureDefs = new CreatureDefinition[]
                {
                    new CreatureDefinition(0, 1, typeof(Ghoul)),
                    new CreatureDefinition(0, 1, typeof(Shade)),
                    new CreatureDefinition(0, 1, typeof(Skeleton)),
                    new CreatureDefinition(0, 1, typeof(Spectre)),
                    new CreatureDefinition(0, 1, typeof(Wraith)),
                    new CreatureDefinition(0, 1, typeof(Zombie)),

                    new CreatureDefinition(1, 1, typeof(BoneKnight)),
                    new CreatureDefinition(1, 1, typeof(BoneMagi)),
                    new CreatureDefinition(1, 1, typeof(SkeletalKnight)),
                    new CreatureDefinition(1, 1, typeof(SkeletalMage)),

                    new CreatureDefinition(2, 1, typeof(BoneKnightLord)),
                    new CreatureDefinition(2, 1, typeof(BoneMagiLord)),
                    new CreatureDefinition(2, 1, typeof(Lich)),
                    new CreatureDefinition(2, 1, typeof(Mummy)),

                    new CreatureDefinition(3, 2, typeof(LichLord)),
                    new CreatureDefinition(3, 1, typeof(RottingCorpse)),
                    new DefWraithWarrior(3, 1),

                    new CreatureDefinition(4, 1, typeof(Server.Engines.Invasion.WraithRiderWarrior)),
                    new CreatureDefinition(4, 1, typeof(Server.Engines.Invasion.WraithRiderMage)),
                    new CreatureDefinition(4, 2, typeof(AncientLich)),
                };

            ArtifactDefs = new ArtifactDefinition[]
                {
                    new ArtifactDefinition(0, 2, typeof(BurialGown)),
                    new ArtifactDefinition(0, 2, typeof(CreepyBust)),
                    new ArtifactDefinition(0, 2, typeof(Spiderweb)),
                    new ArtifactDefinition(0, 2, typeof(SkullMug)),

                    new DefGravestone(1, 3),
                    new ArtifactDefinition(1, 3, typeof(SkullCandle)),
                    new ArtifactDefinition(1, 3, typeof(FlamingHeadDeed)),
                    new DefStatuette(1, 3),

                    new ArtifactDefinition(2, 3, typeof(BoneContainer)),
                    new ArtifactDefinition(2, 3, typeof(CoffinAddonDeed)),
                    new ArtifactDefinition(2, 3, typeof(GuillotineDeed)),
                    new ArtifactDefinition(2, 3, typeof(IronMaidenDeed)),

                    new ArtifactDefinition(3, 3, typeof(BloodyPentagramDeed)),
                    new ArtifactDefinition(3, 3, typeof(CasketAddonDeed)),
                    new ArtifactDefinition(3, 3, typeof(SarcophagusAddonDeed)),
                    new ArtifactDefinition(3, 3, typeof(NecromancerSpellbook)),
                    new ArtifactDefinition(3, 1, typeof(BoneCouchDeed)),
                    new ArtifactDefinition(3, 1, typeof(BoneTableDeed)),
                    new ArtifactDefinition(3, 1, typeof(BoneThroneDeed)),

                    new ArtifactDefinition(4, 2, typeof(BoneCouchDeed)),
                    new ArtifactDefinition(4, 2, typeof(BoneTableDeed)),
                    new ArtifactDefinition(4, 2, typeof(BoneThroneDeed)),
                };

            LootDefs = new LootDefinition[]
                {
                    new DefMouldyFood(0, 1),
                    new LootDefinition(0, 1, typeof(NightSightPotion)),
                    new LootDefinition(0, 2, typeof(HolyWater)),
                    new LootDefinition(0, 1, typeof(HolyHandGrenade)),
                    new LootDefinition(0, 1, typeof(BatWing)),
                    new LootDefinition(0, 1, typeof(GraveDust)),
                    new LootDefinition(0, 1, typeof(DaemonBlood)),
                    new LootDefinition(0, 1, typeof(NoxCrystal)),
                    new LootDefinition(0, 1, typeof(PigIron)),
                };
        }

        private class DefWraithWarrior : CreatureDefinition
        {
            public DefWraithWarrior(int tier, int weight)
                : base(tier, weight, typeof(Server.Engines.Invasion.WraithRiderWarrior))
            {
            }

            public override BaseCreature Construct()
            {
                BaseCreature bc = new Server.Engines.Invasion.WraithRiderWarrior();

                bc.Title = "the wraith warrior";

                BaseMount mount = bc.Mount as BaseMount;

                if (mount != null)
                {
                    mount.Rider = null;
                    mount.Delete();
                }

                return bc;
            }
        }

        private class DefStatuette : ArtifactDefinition
        {
            public DefStatuette(int reqTier, int weight)
                : base(reqTier, weight, typeof(MonsterStatuette))
            {
            }

            public override Item Construct()
            {
                if (m_Types.Length == 0)
                    return null;

                return new MonsterStatuette(m_Types[Utility.Random(m_Types.Length)]);
            }

            private static readonly MonsterStatuetteType[] m_Types = new MonsterStatuetteType[]
                {
                    MonsterStatuetteType.Lich,
                    MonsterStatuetteType.Skeleton,
                    MonsterStatuetteType.Zombie,
                    MonsterStatuetteType.Ghost,
                    MonsterStatuetteType.Ghoul,
                };
        }

        private class DefGravestone : ArtifactDefinition
        {
            public DefGravestone(int reqTier, int weight)
                : base(reqTier, weight, typeof(Gravestone))
            {
            }

            public override Item Construct()
            {
                Item item = base.Construct();

                if (item != null)
                    item.Weight = 5.0;

                return item;
            }
        }

        private class DefMouldyFood : LootDefinition
        {
            public DefMouldyFood(int reqTier, int weight)
                : base(reqTier, weight, typeof(Food))
            {
            }

            public override Item Construct()
            {
                if (m_Table.Length == 0)
                    return null; // sanity

                FoodInfo info = m_Table[Utility.Random(m_Table.Length)];

                Food food = InvasionSystem.Construct<Food>(info.Type);

                if (food != null)
                {
                    food.Name = info.Name;
                    food.Hue = 1415;
                    food.Poison = Poison.Lesser;
                }

                return food;
            }

            private static readonly FoodInfo[] m_Table = new FoodInfo[]
                {
                    new FoodInfo(typeof(Grapes), "a mouldy grape bunch"),
                    new FoodInfo(typeof(Ham), "a mouldy ham"),
                    new FoodInfo(typeof(CheeseWedge), "a mouldy wedge of cheese"),
                    new FoodInfo(typeof(Muffins), "mouldy muffins"),
                    new FoodInfo(typeof(FishSteak), "a mouldy fish steak"),
                    new FoodInfo(typeof(Ribs), "mouldy cut of ribs"),
                    new FoodInfo(typeof(CookedBird), "a mouldy cooked bird"),
                    new FoodInfo(typeof(Sausage), "mouldy sausage"),
                    new FoodInfo(typeof(Apple), "a mouldy apple"),
                    new FoodInfo(typeof(Peach), "a mouldy peach"),
                };

            private class FoodInfo
            {
                private Type m_Type;
                private string m_Name;

                public Type Type { get { return m_Type; } }
                public string Name { get { return m_Name; } }

                public FoodInfo(Type type, string name)
                {
                    m_Type = type;
                    m_Name = name;
                }
            }
        }
    }

    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class UndeadInvasionConsole : Item
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        #region Save/Load

        private const string FilePath = "Saves/UndeadInvasion.xml";

        public static void OnSave(WorldSaveEventArgs e)
        {
            InvasionSystem system = InvasionSystem.Get(InvasionType.Undead);

            if (system == null)
                return;

            try
            {
                Console.WriteLine("UndeadInvasion Saving...");

                string directoryName = Path.GetDirectoryName(FilePath);

                if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                XmlTextWriter writer = new XmlTextWriter(FilePath, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("UndeadInvasion");
                writer.WriteAttributeString("version", "1");

                try
                {
                    // version 1

                    writer.WriteElementString("FactionScalar", system.FactionScalar.ToString());

                    // version 0

                    writer.WriteElementString("PetDamageScalar", system.PetDamageScalar.ToString());
                    writer.WriteElementString("SummonDamageScalar", system.SummonDamageScalar.ToString());
                    writer.WriteElementString("BardDamageScalar", system.BardDamageScalar.ToString());
                    writer.WriteElementString("ArtifactChance", system.ArtifactChance.ToString());
                    writer.WriteElementString("ArtifactMessage", system.ArtifactMessage.ToString());
                    writer.WriteElementString("LootChance", system.LootChance.ToString());
                    writer.WriteElementString("FameScaling", system.FameScaling.ToString());
                    writer.WriteElementString("PartySharing", system.PartySharing.ToString());
                }
                finally
                {
                    writer.WriteEndDocument();
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        public static void OnLoad()
        {
            InvasionSystem system = InvasionSystem.Get(InvasionType.Undead);

            if (system == null)
                return;

            try
            {
                if (!File.Exists(FilePath))
                    return;

                Console.WriteLine("UndeadInvasion Loading...");

                XmlTextReader reader = new XmlTextReader(FilePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("UndeadInvasion");

                switch (version)
                {
                    case 1:
                        {
                            system.FactionScalar = Int32.Parse(reader.ReadElementString("FactionScalar"));

                            goto case 0;
                        }
                    case 0:
                        {
                            system.PetDamageScalar = Int32.Parse(reader.ReadElementString("PetDamageScalar"));
                            system.SummonDamageScalar = Int32.Parse(reader.ReadElementString("SummonDamageScalar"));
                            system.BardDamageScalar = Int32.Parse(reader.ReadElementString("BardDamageScalar"));
                            system.ArtifactChance = Double.Parse(reader.ReadElementString("ArtifactChance"));
                            system.ArtifactMessage = reader.ReadElementString("ArtifactMessage");
                            system.LootChance = Double.Parse(reader.ReadElementString("LootChance"));
                            system.FameScaling = Boolean.Parse(reader.ReadElementString("FameScaling"));
                            system.PartySharing = Boolean.Parse(reader.ReadElementString("PartySharing"));

                            break;
                        }
                }

                reader.ReadEndElement();

                reader.Close();
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public int PetDamageScalar
        {
            get { return (System == null ? -1 : System.PetDamageScalar); }
            set { if (System != null) System.PetDamageScalar = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SummonDamageScalar
        {
            get { return (System == null ? -1 : System.SummonDamageScalar); }
            set { if (System != null) System.SummonDamageScalar = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BardDamageScalar
        {
            get { return (System == null ? -1 : System.BardDamageScalar); }
            set { if (System != null) System.BardDamageScalar = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FactionScalar
        {
            get { return (System == null ? -1 : System.FactionScalar); }
            set { if (System != null) System.FactionScalar = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ArtifactChance
        {
            get { return (System == null ? -1.0 : System.ArtifactChance); }
            set { if (System != null) System.ArtifactChance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ArtifactMessage
        {
            get { return (System == null ? null : System.ArtifactMessage); }
            set { if (System != null) System.ArtifactMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double LootChance
        {
            get { return (System == null ? -1.0 : System.LootChance); }
            set { if (System != null) System.LootChance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool FameScaling
        {
            get { return (System == null ? false : System.FameScaling); }
            set { if (System != null) System.FameScaling = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PartySharing
        {
            get { return (System == null ? false : System.PartySharing); }
            set { if (System != null) System.PartySharing = value; }
        }

        private InvasionSystem System { get { return InvasionSystem.Get(InvasionType.Undead); } }

        [Constructable]
        public UndeadInvasionConsole()
            : base(0x1F14)
        {
            Hue = 0x835;
            Name = "Undead Invasion Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public UndeadInvasionConsole(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadEncodedInt();
        }
    }
}