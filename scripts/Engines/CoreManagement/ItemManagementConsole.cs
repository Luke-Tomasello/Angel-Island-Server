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

/* Scripts\Engines\CoreManagement\ItemManagementConsole.cs
 * ChangeLog
 *  8/12/22, Adam
 *      Remove command access for FreezeDryEnabled.
 *      See also comments: main.cs Initialize()
 *	8/9/22, Adam
 *		Created.
 *		Management console for the Item related global values stored in Engines/AngelIsland/CoreAI.cs
 *		MOVE the following out of CoreManagementConsole to here
 *		DungeonChestBuryRate, DungeonChestBuryMinLevel
 *		MagicGearDropDowngrade, MagicGearDropChance
 *		SlayerWeaponDropRate, SlayerInstrumentDropRate
 *		DebugDecay
 *		FreezeDryEnabled
 *		ADD MagicGearThrottleEnabled
 */

using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class ItemManagementConsole : Item
    {
        [Constructable]
        public ItemManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 0x534;
            Name = "Item Management Console";
        }

        public ItemManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double EnchantedScrollDropChance
        {
            get
            {
                return CoreAI.EnchantedScrollDropChance;
            }
            set
            {
                CoreAI.EnchantedScrollDropChance = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double MagicWandDropChance
        {
            get
            {
                return CoreAI.MagicWandDropChance;
            }
            set
            {
                CoreAI.MagicWandDropChance = value;
            }
        }

        private void EnableCooperatives(bool stash)
        {
            // get a list of all the Cooperatives
            List<Multis.HouseSign> signList = new List<Multis.HouseSign>();
            foreach (Item item in World.Items.Values)
            {
                if (item != null && item.Deleted == false && item is Multis.HouseSign hs)
                    if (stash && item.Map == Map.Felucca || !stash && item.Map == Map.Internal)
                        if (hs.CooperativeType != Multis.BaseHouse.CooperativeType.None)
                            signList.Add(hs);
            }

            // we now have a list of all the Cooperatives
            foreach (Multis.HouseSign hs in signList)
            {
                hs.Structure.StashHouse(stash);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool Cooperatives
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.Cooperatives);
            }
            set
            {
                EnableCooperatives(!value);

                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.Cooperatives);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.Cooperatives);
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool ZoraSystem
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.ZoraSystem);
            }
            set
            {
                EnableZoraSystem(value);

                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.ZoraSystem);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.ZoraSystem);
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool MagicCraftSystem
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.MagicCraftSystem);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.MagicCraftSystem);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.MagicCraftSystem);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool MagicGearThrottleEnabled
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.MagicGearThrottleEnabled);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.MagicGearThrottleEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.MagicGearThrottleEnabled);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int DungeonChestBuryRate
        {
            get
            {
                return CoreAI.DungeonChestBuryRate;
            }
            set
            {
                CoreAI.DungeonChestBuryRate = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int DungeonChestBuryMinLevel
        {
            get
            {
                return CoreAI.DungeonChestBuryMinLevel;
            }
            set
            {
                CoreAI.DungeonChestBuryMinLevel = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int MagicGearDropDowngrade
        {
            get
            {
                return CoreAI.MagicGearDropDowngrade;
            }
            set
            {
                CoreAI.MagicGearDropDowngrade = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int MagicGearDropChance
        {
            get
            {
                return CoreAI.MagicGearDropChance;
            }
            set
            {
                CoreAI.MagicGearDropChance = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double SlayerWeaponDropRate
        {
            get
            {
                return CoreAI.SlayerWeaponDropRate;
            }
            set
            {
                CoreAI.SlayerWeaponDropRate = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public double SiegeGearDropFactor
        {
            get
            {
                return CoreAI.SiegeGearDropFactor;
            }
            set
            {
                CoreAI.SiegeGearDropFactor = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public double SlayerInstrumentDropRate
        {
            get
            {
                return CoreAI.SlayerInstrumentDropRate;
            }
            set
            {
                CoreAI.SlayerInstrumentDropRate = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public double EnchantedEquipmentDropRate
        {
            get
            {
                return CoreAI.EnchantedEquipmentDropRate;
            }
            set
            {
                CoreAI.EnchantedEquipmentDropRate = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool DebugDecay
        {
            get { return CoreAI.DebugItemDecayOutput; }
            set { CoreAI.DebugItemDecayOutput = value; }
        }

        // removing acces to this.
        //  See comments in main.cs (Initialize)
        // [CommandProperty(AccessLevel.Administrator)]
        public bool FreezeDryEnabled
        {
            get
            {
                return World.FreezeDryEnabled;
            }
            set
            {
                World.FreezeDryEnabled = value;
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

        private List<Mobile> ZoraSystemLinks()
        {
            Type tx = ScriptCompiler.FindTypeByName("zora");
            List<Mobile> list = new List<Mobile>();
            if (tx != null)
            {
                foreach (Mobile mob in World.Mobiles.Values)
                {
                    if (mob != null && !mob.Deleted && tx.IsAssignableFrom(mob.GetType()))
                    {
                        list.Add(mob);
                    }
                }
            }
            else
            {
                ;
            }
            return list;
        }
        /*
         * Support functionality for above settings
         */
        private void EnableZoraSystem(bool on)
        {
            List<Mobile> list = ZoraSystemLinks();
            bool zoraSystemExists = list.Count > 0;
            if (on == true)
            {
                if (zoraSystemExists)
                {   // do nothing, we want zora and she's already here
                }
                else if (!zoraSystemExists)
                {   // zora doesn't exist, spawn her
                    // Can't use SpawnerCache here since the zora spawner is never 'running'
                    foreach (Item item in World.Items.Values)
                    {
                        if (item == null) continue;
                        if (item is Spawner sp)
                        {
                            if (sp == null) continue;
                            if (sp.Serial == 0x400025e9)
                                Console.WriteLine();
                            if (sp.ObjectNames == null) continue;
                            foreach (string name in sp.ObjectNames)
                            {
                                if (name.ToLower() == "zora")
                                {
                                    sp.Respawn();
                                }
                            }
                        }
                    }
                }
            }
            else // off
            {
                if (!zoraSystemExists)
                {   // do nothing, we don't want zora and she doesn't exist
                }
                else // zoraSystemExists == true
                {   // we have zora(s) but we don't want her
                    foreach (Mobile zora in list)
                    {
                        if (zora is BaseCreature bc)
                        {
                            if (bc == null) continue;
                            if (bc.Spawner != null)
                                bc.Spawner.Running = false;   // should never be the case with Zora
                            if (bc.Deleted == false)
                                bc.Delete();
                        }
                    }
                }
            }
        }
    }
}