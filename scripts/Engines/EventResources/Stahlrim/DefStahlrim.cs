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

/* Server/Engines/Stahlrim/DefStahlrim.cs
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
    public class DefStahlrim : EventResourceSystem
    {
        private static EventResourceSystem m_System;

        public static EventResourceSystem System
        {
            get
            {
                if (m_System == null)
                    m_System = new DefStahlrim();

                return m_System;
            }
        }

        public DefStahlrim()
        {
            Resource = CraftResource.Stahlrim;

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
            if (HasFrozenForge(from, 2))
            {
                if (tool.CraftSystem == DefBlacksmithy.CraftSystem)
                    return StahlrimCraft.CraftSystem;
                else if (tool.CraftSystem == DefMasonry.CraftSystem)
                    return FrostrockCraft.CraftSystem;
            }

            return null;
        }

        public static bool HasFrozenForge(Mobile from, int range)
        {
            foreach (Item item in from.GetItemsInRange(range))
            {
                if (item is AddonComponent && ((AddonComponent)item).Addon is FrozenForgeAddon)
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

        private const string FilePath = "Saves/StahlrimSystem.xml";

        public static void Save()
        {
            try
            {
                Console.WriteLine("StahlrimSystem Saving...");

                string directoryName = Path.GetDirectoryName(FilePath);

                if (!String.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                XmlTextWriter writer = new XmlTextWriter(FilePath, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("StahlrimSystem");
                writer.WriteAttributeString("version", "0");

                try
                {
                    // version 0

                    writer.WriteElementString("ArmorBonus", System.ArmorBonus.ToString());
                    writer.WriteElementString("ProtectionLevel", System.ProtectionLevel.ToString());
                    writer.WriteElementString("DamageBonus", System.DamageBonus.ToString());
                    writer.WriteElementString("DamageLevel", System.DamageLevel.ToString());
                    writer.WriteElementString("DurabilityScalar", System.DurabilityScalar.ToString());

                    writer.WriteElementString("ArmorImbueChance", System.ArmorImbueChance.ToString());
                    writer.WriteElementString("ArmorImbueCap", System.ArmorImbueCap.ToString());
                    writer.WriteElementString("ArmorImbueScaling", System.ArmorImbueScaling.ToString());

                    writer.WriteElementString("WeaponImbueChance", System.WeaponImbueChance.ToString());
                    writer.WriteElementString("WeaponImbueCap", System.WeaponImbueCap.ToString());
                    writer.WriteElementString("WeaponImbueScaling", System.WeaponImbueScaling.ToString());

                    writer.WriteElementString("StatueImbueChance", System.StatueImbueChance.ToString());
                    writer.WriteElementString("StatueImbueCap", System.StatueImbueCap.ToString());
                    writer.WriteElementString("StatueImbueScaling", System.StatueImbueScaling.ToString());
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

                Console.WriteLine("StahlrimSystem Loading...");

                XmlTextReader reader = new XmlTextReader(FilePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("StahlrimSystem");

                bool weaponEnchantments = true, statueEnchantments = true;

                switch (version)
                {
                    case 0:
                        {
                            System.ArmorBonus = Int32.Parse(reader.ReadElementString("ArmorBonus"));
                            System.ProtectionLevel = (ArmorProtectionLevel)Enum.Parse(typeof(ArmorProtectionLevel), reader.ReadElementString("ProtectionLevel"));
                            System.DamageBonus = Int32.Parse(reader.ReadElementString("DamageBonus"));
                            System.DamageLevel = (WeaponDamageLevel)Enum.Parse(typeof(WeaponDamageLevel), reader.ReadElementString("DamageLevel"));
                            System.DurabilityScalar = Int32.Parse(reader.ReadElementString("DurabilityScalar"));

                            System.ArmorImbueChance = Double.Parse(reader.ReadElementString("ArmorImbueChance"));
                            System.ArmorImbueCap = Int32.Parse(reader.ReadElementString("ArmorImbueCap"));
                            System.ArmorImbueScaling = Boolean.Parse(reader.ReadElementString("ArmorImbueScaling"));

                            System.WeaponImbueChance = Double.Parse(reader.ReadElementString("WeaponImbueChance"));
                            System.WeaponImbueCap = Int32.Parse(reader.ReadElementString("WeaponImbueCap"));
                            System.WeaponImbueScaling = Boolean.Parse(reader.ReadElementString("WeaponImbueScaling"));

                            System.StatueImbueChance = Double.Parse(reader.ReadElementString("StatueImbueChance"));
                            System.StatueImbueCap = Int32.Parse(reader.ReadElementString("StatueImbueCap"));
                            System.StatueImbueScaling = Boolean.Parse(reader.ReadElementString("StatueImbueScaling"));

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

    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class StahlrimConsole : Item
    {
        #region Accessors

        [CommandProperty(AccessLevel.GameMaster)]
        public int ArmorBonus
        {
            get { return DefStahlrim.System.ArmorBonus; }
            set { DefStahlrim.System.ArmorBonus = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ArmorProtectionLevel ProtectionLevel
        {
            get { return DefStahlrim.System.ProtectionLevel; }
            set { DefStahlrim.System.ProtectionLevel = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DamageBonus
        {
            get { return DefStahlrim.System.DamageBonus; }
            set { DefStahlrim.System.DamageBonus = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public WeaponDamageLevel DamageLevel
        {
            get { return DefStahlrim.System.DamageLevel; }
            set { DefStahlrim.System.DamageLevel = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DurabilityScalar
        {
            get { return DefStahlrim.System.DurabilityScalar; }
            set { DefStahlrim.System.DurabilityScalar = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ArmorImbueChance
        {
            get { return DefStahlrim.System.ArmorImbueChance; }
            set { DefStahlrim.System.ArmorImbueChance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ArmorImbueCap
        {
            get { return DefStahlrim.System.ArmorImbueCap; }
            set { DefStahlrim.System.ArmorImbueCap = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ArmorImbueScaling
        {
            get { return DefStahlrim.System.ArmorImbueScaling; }
            set { DefStahlrim.System.ArmorImbueScaling = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double WeaponImbueChance
        {
            get { return DefStahlrim.System.WeaponImbueChance; }
            set { DefStahlrim.System.WeaponImbueChance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WeaponImbueCap
        {
            get { return DefStahlrim.System.WeaponImbueCap; }
            set { DefStahlrim.System.WeaponImbueCap = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool WeaponImbueScaling
        {
            get { return DefStahlrim.System.WeaponImbueScaling; }
            set { DefStahlrim.System.WeaponImbueScaling = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double StatueImbueChance
        {
            get { return DefStahlrim.System.StatueImbueChance; }
            set { DefStahlrim.System.StatueImbueChance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StatueImbueCap
        {
            get { return DefStahlrim.System.StatueImbueCap; }
            set { DefStahlrim.System.StatueImbueCap = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool StatueImbueScaling
        {
            get { return DefStahlrim.System.StatueImbueScaling; }
            set { DefStahlrim.System.StatueImbueScaling = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentImbuedArmors
        {
            get { return DefStahlrim.System.CountImbuedByType(typeof(BaseArmor)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentImbuedStatues
        {
            get { return DefStahlrim.System.CountImbuedByType(typeof(BaseStatue)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentImbuedWeapons
        {
            get { return DefStahlrim.System.CountImbuedByType(typeof(BaseWeapon)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ScaledArmorImbueChance
        {
            get { return DefStahlrim.System.GetImbueChance(typeof(BaseArmor)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ScaledStatueImbueChance
        {
            get { return DefStahlrim.System.GetImbueChance(typeof(BaseStatue)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ScaledWeaponImbueChance
        {
            get { return DefStahlrim.System.GetImbueChance(typeof(BaseWeapon)); }
        }

        #endregion

        [Constructable]
        public StahlrimConsole()
            : base(0x1F14)
        {
            Hue = CraftResources.GetHue(CraftResource.Stahlrim);
            Name = "Stahlrim Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public StahlrimConsole(Serial serial)
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