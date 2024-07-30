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

/* Server/Engines/EventResources/Obsidian/DefObsidian.cs
 * CHANGELOG:
 *  10/18/23, Yoar
 *      Initial version.
 */

using Server.Engines.Craft;
using Server.Items;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Engines.EventResources
{
    public class DefObsidian : EventResourceSystem
    {
        private static EventResourceSystem m_System;

        public static EventResourceSystem System
        {
            get
            {
                if (m_System == null)
                    m_System = new DefObsidian();

                return m_System;
            }
        }

        public DefObsidian()
        {
            Resource = CraftResource.Obsidian;

            ArmorBonus = 0;
            ProtectionLevel = ArmorProtectionLevel.Regular;
            DamageBonus = 0;
            DamageLevel = WeaponDamageLevel.Regular; // WeaponDamageLevel.Force; (too OP)
            DurabilityScalar = 50;

            ArmorImbueChance = 0.75;
            ArmorImbueCap = 70;
            ArmorImbueScaling = true;

            WeaponImbueChance = 0.75;
            WeaponImbueCap = 70;
            WeaponImbueScaling = true;

            StatueImbueChance = 0.75;
            StatueImbueCap = 140;
            StatueImbueScaling = true;

            Enchantments = new EnchantmentEntry[]
                {
                    new EnchantmentEntry(typeof(BaseWeapon), m_WeaponEnchantments),
                    new EnchantmentEntry(typeof(BaseStatue), m_StatueEnchantments),
                };
        }

        public override CraftSystem MutateCraft(Mobile from, BaseTool tool)
        {
            if (HasObsidianForge(from, 2))
            {
                if (tool.CraftSystem == DefBlacksmithy.CraftSystem)
                    return ObsidianCraft.CraftSystem;
                else if (tool.CraftSystem == DefMasonry.CraftSystem)
                    return BlackrockCraft.CraftSystem;
            }

            return null;
        }

        public static bool HasObsidianForge(Mobile from, int range)
        {
            foreach (Item item in from.GetItemsInRange(range))
            {
                if (item is AddonComponent && ((AddonComponent)item).Addon is ObsidianForgeAddon)
                    return true;
            }

            return false;
        }

        private static readonly BaseEnchantment[] m_WeaponEnchantments = new BaseEnchantment[]
            {
                new MagicEnchantment(3, 40, 80, Loot.WeaponEnchantments),
                new SlayerEnchantment(1),
            };

        private static readonly BaseEnchantment[] m_StatueEnchantments = new BaseEnchantment[]
            {
                new MagicEnchantment(4, 40, 80,
                    MagicItemEffect.CreateFood, MagicItemEffect.Heal, MagicItemEffect.NightSight,
                    MagicItemEffect.ReactiveArmor),
                new MagicEnchantment(3, 30, 50,
                    MagicItemEffect.Agility, MagicItemEffect.Cunning, MagicItemEffect.Protection,
                    MagicItemEffect.Strength),
                new MagicEnchantment(2, 25, 40,
                    MagicItemEffect.GreaterHeal, MagicItemEffect.Incognito, MagicItemEffect.MagicReflect,
                    MagicItemEffect.SummonCreature, MagicItemEffect.Invisibility),
                new MagicEnchantment(1, 20, 35,
                    MagicItemEffect.AirElemental, MagicItemEffect.SummonDaemon, MagicItemEffect.EarthElemental,
                    MagicItemEffect.FireElemental, MagicItemEffect.WaterElemental)
            };

        public static void Configure()
        {
            EventSink.WorldLoad += EventSink_WorldLoad;
            EventSink.WorldSave += EventSink_WorldSave;
        }

        private static void EventSink_WorldLoad()
        {
            Load();
        }

        private static void EventSink_WorldSave(WorldSaveEventArgs e)
        {
            Save();
        }

        #region Save/Load

        private const string FilePath = "Saves/ObsidianSystem.xml";

        public static void Save()
        {
            try
            {
                Console.WriteLine("ObsidianSystem Saving...");

                string directoryName = Path.GetDirectoryName(FilePath);

                if (!String.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                XmlTextWriter writer = new XmlTextWriter(FilePath, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("ObsidianSystem");
                writer.WriteAttributeString("version", "7");

                try
                {
                    // version 7

                    writer.WriteElementString("ArmorImbueChance", System.ArmorImbueChance.ToString());
                    writer.WriteElementString("ArmorImbueCap", System.ArmorImbueCap.ToString());
                    writer.WriteElementString("ArmorImbueScaling", System.ArmorImbueScaling.ToString());

                    // version 6

                    // version 5

                    writer.WriteElementString("WeaponImbueCap", System.WeaponImbueCap.ToString());
                    writer.WriteElementString("StatueImbueCap", System.StatueImbueCap.ToString());
                    writer.WriteElementString("WeaponImbueScaling", System.WeaponImbueScaling.ToString());
                    writer.WriteElementString("StatueImbueScaling", System.StatueImbueScaling.ToString());

                    // version 4

                    writer.WriteElementString("WeaponImbueChance", System.WeaponImbueChance.ToString());
                    writer.WriteElementString("StatueImbueChance", System.StatueImbueChance.ToString());

                    // version 3

                    writer.WriteElementString("ProtectionLevel", System.ProtectionLevel.ToString());
                    writer.WriteElementString("DamageLevel", System.DamageLevel.ToString());

                    // version 2

                    writer.WriteElementString("ArmorBonus", System.ArmorBonus.ToString());
                    writer.WriteElementString("DamageBonus", System.DamageBonus.ToString());

                    // version 1

                    // version 0

                    writer.WriteElementString("DurabilityScalar", System.DurabilityScalar.ToString());
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

        private static void Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return;

                Console.WriteLine("ObsidianSystem Loading...");

                XmlTextReader reader = new XmlTextReader(FilePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("ObsidianSystem");

                bool weaponEnchantments = true, statueEnchantments = true;

                switch (version)
                {
                    case 7:
                        {
                            System.ArmorImbueChance = Double.Parse(reader.ReadElementString("ArmorImbueChance"));
                            System.ArmorImbueCap = Int32.Parse(reader.ReadElementString("ArmorImbueCap"));
                            System.ArmorImbueScaling = Boolean.Parse(reader.ReadElementString("ArmorImbueScaling"));

                            goto case 6;
                        }
                    case 6:
                    case 5:
                        {
                            System.WeaponImbueCap = Int32.Parse(reader.ReadElementString("WeaponImbueCap"));
                            System.StatueImbueCap = Int32.Parse(reader.ReadElementString("StatueImbueCap"));
                            System.WeaponImbueScaling = Boolean.Parse(reader.ReadElementString("WeaponImbueScaling"));
                            System.StatueImbueScaling = Boolean.Parse(reader.ReadElementString("StatueImbueScaling"));

                            goto case 4;
                        }
                    case 4:
                        {
                            System.WeaponImbueChance = Double.Parse(reader.ReadElementString("WeaponImbueChance"));
                            System.StatueImbueChance = Double.Parse(reader.ReadElementString("StatueImbueChance"));

                            goto case 3;
                        }
                    case 3:
                        {
                            System.ProtectionLevel = (ArmorProtectionLevel)Enum.Parse(typeof(ArmorProtectionLevel), reader.ReadElementString("ProtectionLevel"));
                            System.DamageLevel = (WeaponDamageLevel)Enum.Parse(typeof(WeaponDamageLevel), reader.ReadElementString("DamageLevel"));

                            goto case 2;
                        }
                    case 2:
                        {
                            System.ArmorBonus = Int32.Parse(reader.ReadElementString("ArmorBonus"));
                            System.DamageBonus = Int32.Parse(reader.ReadElementString("DamageBonus"));

                            goto case 1;
                        }
                    case 1:
                        {
                            if (version < 4)
                            {
                                weaponEnchantments = Boolean.Parse(reader.ReadElementString("WeaponEnchantments"));
                                statueEnchantments = Boolean.Parse(reader.ReadElementString("StatueEnchantments"));
                            }

                            goto case 0;
                        }
                    case 0:
                        {
                            if (version < 6)
                            {
                                reader.ReadElementString("ArmorScalar"); // armor scalar
                                reader.ReadElementString("DamageScalar"); // damage scalar
                            }

                            System.DurabilityScalar = Int32.Parse(reader.ReadElementString("DurabilityScalar"));

                            if (version < 4)
                            {
                                double imbueChance = Int32.Parse(reader.ReadElementString("ImbuePerc")) / 100.0;

                                if (weaponEnchantments)
                                    System.WeaponImbueChance = imbueChance;

                                if (statueEnchantments)
                                    System.StatueImbueChance = imbueChance;
                            }

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
    }

    [TypeAlias("Server.Engines.Obsidian.ObsidianConsole")]
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class ObsidianConsole : Item
    {
        #region Accessors

        [CommandProperty(AccessLevel.GameMaster)]
        public int ArmorBonus
        {
            get { return DefObsidian.System.ArmorBonus; }
            set { DefObsidian.System.ArmorBonus = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ArmorProtectionLevel ProtectionLevel
        {
            get { return DefObsidian.System.ProtectionLevel; }
            set { DefObsidian.System.ProtectionLevel = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DamageBonus
        {
            get { return DefObsidian.System.DamageBonus; }
            set { DefObsidian.System.DamageBonus = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponDamageLevel DamageLevel
        {
            get { return DefObsidian.System.DamageLevel; }
            set { DefObsidian.System.DamageLevel = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DurabilityScalar
        {
            get { return DefObsidian.System.DurabilityScalar; }
            set { DefObsidian.System.DurabilityScalar = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ArmorImbueChance
        {
            get { return DefObsidian.System.ArmorImbueChance; }
            set { DefObsidian.System.ArmorImbueChance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ArmorImbueCap
        {
            get { return DefObsidian.System.ArmorImbueCap; }
            set { DefObsidian.System.ArmorImbueCap = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ArmorImbueScaling
        {
            get { return DefObsidian.System.ArmorImbueScaling; }
            set { DefObsidian.System.ArmorImbueScaling = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double WeaponImbueChance
        {
            get { return DefObsidian.System.WeaponImbueChance; }
            set { DefObsidian.System.WeaponImbueChance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WeaponImbueCap
        {
            get { return DefObsidian.System.WeaponImbueCap; }
            set { DefObsidian.System.WeaponImbueCap = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool WeaponImbueScaling
        {
            get { return DefObsidian.System.WeaponImbueScaling; }
            set { DefObsidian.System.WeaponImbueScaling = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double StatueImbueChance
        {
            get { return DefObsidian.System.StatueImbueChance; }
            set { DefObsidian.System.StatueImbueChance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StatueImbueCap
        {
            get { return DefObsidian.System.StatueImbueCap; }
            set { DefObsidian.System.StatueImbueCap = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool StatueImbueScaling
        {
            get { return DefObsidian.System.StatueImbueScaling; }
            set { DefObsidian.System.StatueImbueScaling = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentImbuedArmors
        {
            get { return DefObsidian.System.CountImbuedByType(typeof(BaseArmor)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentImbuedStatues
        {
            get { return DefObsidian.System.CountImbuedByType(typeof(BaseStatue)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentImbuedWeapons
        {
            get { return DefObsidian.System.CountImbuedByType(typeof(BaseWeapon)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ScaledArmorImbueChance
        {
            get { return DefObsidian.System.GetImbueChance(typeof(BaseArmor)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ScaledStatueImbueChance
        {
            get { return DefObsidian.System.GetImbueChance(typeof(BaseStatue)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ScaledWeaponImbueChance
        {
            get { return DefObsidian.System.GetImbueChance(typeof(BaseWeapon)); }
        }

        #endregion

        [Constructable]
        public ObsidianConsole()
            : base(0x1F14)
        {
            Hue = CraftResources.GetHue(CraftResource.Obsidian);
            Name = "Obsidian Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public ObsidianConsole(Serial serial)
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