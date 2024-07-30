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

/* Scripts\Mobiles\Monsters\Polymorphs\BasePolymorphic.cs
 * ChangeLog
 *  12/11/23, Adam
 *      Created.
 */

using Server.Diagnostics;
using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class BasePolymorphic : BaseCreature
    {
        private int m_level;
        private int m_max_levels;
        public int Level { get { return m_level; } }
        public int MaxLevels { get { return m_max_levels; } }
        public override void Configure(int current_level, int max_levels)
        {   // This is called by the champ engine to tell us what level we are in.
            //  this information allows us to morph into increasingly tougher mobs.
            m_level = current_level;
            m_max_levels = max_levels;
            if (m_config_timer != null)
            {
                m_config_timer.Stop();
                m_config_timer = null;
            }
            Tick(new object[] { this });
        }
        private Timer m_config_timer = null;
        [Constructable]
        public BasePolymorphic()
            : base(AIType.AI_Melee, FightMode.None, 10, 1, 0.25, 0.5)
        {
            // we need to delay the configuration of this mobile until the call to Configure() comes in
            m_config_timer = Timer.DelayCall(TimeSpan.FromSeconds(3.5), new TimerStateCallback(Tick), new object[] { this });
            Hidden = true;
        }
        private void Tick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is BaseCreature && (aState[0] as BaseCreature).Deleted == false)
            {
                InitBody();
                InitStats();
                InitOutfit();
                Hidden = false;
            }
        }

        #region basic props
        public override bool AlwaysMurderer { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanRummageCorpses { get { return Core.RuleSets.AllShards ? true : false; } }
        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return Core.RuleSets.AllShards ? TimeSpan.FromSeconds(Utility.RandomMinMax(10, 13)) : base.BandageDelay; } }
        #endregion basic props

        public BasePolymorphic(Serial serial)
            : base(serial)
        {
        }
        public virtual void InitStats()
        {
            bool mini = MaxLevels == 4;
            if (mini)
                switch (Level)
                {
                    default:
                    case 0:
                        {
                            InitMelee(1);
                            break;
                        }
                    case 1:
                        if (Utility.RandomChance(80))
                            InitMelee(2);
                        else
                            InitArcher(2);
                        break;
                    case 2:
                        {
                            if (Utility.RandomChance(80))
                                InitMage(0);
                            else
                                InitArcher(2);
                            break;
                        }

                }
            else
                switch (Level)
                {
                    // Configure was not set, so go with the basics
                    default:
                    // sub level 1
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        {
                            InitMelee(1);
                            break;
                        }
                    // sublevel 2
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                        {
                            if (Utility.RandomChance(80))
                                InitMelee(2);
                            else
                                InitArcher(2);
                            break;
                        }
                    // subLevel 3
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                        {
                            if (Utility.RandomChance(80))
                                InitArcher(2);
                            else
                                InitMage(0);
                            break;
                        }
                    // sublevel 4
                    case 14:
                    case 15:
                    case 16:
                        {
                            if (Utility.RandomChance(80))
                                InitMage(0);
                            else
                                InitArcher(2);
                            break;
                        }
                }
        }
        private Type m_my_type = null;
        public virtual Type MyType { get { return m_my_type; } set { m_my_type = value; } }
        public virtual void InitArcher(int subLevel)
        {
            Utility.CopyConstruction(typeof(BrigandArcher), this);
            Utility.CopyStats(typeof(BrigandArcher), this);
            MyType = typeof(BrigandArcher);
        }
        public virtual void InitMage(int unused)
        {
            Utility.CopyConstruction(typeof(GolemController), this);
            Utility.CopyStats(typeof(GolemController), this);
            MyType = typeof(GolemController);

            /* The following configure options are not supported for 'standard' Golem Controllers on, for example, Siege. 
             * But since this is a custom mob we will allow it.
             */
            BardImmune = true;
            UsesHumanWeapons = false;
            UsesBandages = true;
            UsesPotions = true;
            CanRun = true;
            CanReveal = true;   // magic and smart
            CrossHeals = true;  // classic Angel Island Golem Controllers

            PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
            PackStrongPotions(6, 12);
            PackItem(new Pouch(), lootType: LootType.UnStealable);
        }
        public virtual void InitMelee(int subLevel)
        {
            Utility.CopyConstruction(typeof(Brigand), this);
            Utility.CopyStats(typeof(Brigand), this);
            MyType = typeof(Brigand);
        }
        public override void InitBody()
        {
        }
        public override void InitOutfit()
        {

        }
        public override void GenerateLoot()
        {
            if (!Spawning)
            {
                if (MyType != null)
                {
                    List<Item> list = Utility.CopyLoot(MyType);
                    List<Item> keep_list = new();
                    foreach (Item item in list)
                        if (item is BaseClothing bc && !IsMagicLoot(item))
                            continue;               // don't want any clothes unless they are magic (we'll already drop our clothes)
                        else if (WeAlreadyHave(item) && !IsMagicLoot(item))
                            continue;               // we already have one of these (weapons, armor)
                        else
                            keep_list.Add(item);    // we'll take it

                    foreach (Item item in keep_list)
                        PackItem(item);
                }
                else
                    ;//error (possibly killed before fully created)
            }
        }
        private bool WeAlreadyHave(Item item)
        {
            if (item is BaseWeapon bw)
            {
                foreach (Item equip in Items)
                    if (equip is BaseWeapon)
                        return true;        // we are already dropping a weapon
                return false;               // yeah, we can use one of these
            }
            if (item is BaseArmor ba)
            {
                foreach (Item equip in Items)
                    if (equip is BaseArmor)
                        return true;        // we are already dropping armor
                return false;               // yeah, we can use one of these
            }
            // if we've not excluded it, we'll take it
            return false;
        }
        private bool IsMagicLoot(Item item)
        {
            if (item is BaseWeapon bw && item is not BaseWand)
            {
                if (bw.AccuracyLevel > WeaponAccuracyLevel.Regular || bw.DamageLevel > WeaponDamageLevel.Regular || bw.DurabilityLevel > WeaponDurabilityLevel.Regular)
                    return true;

                if (bw.MagicCharges > 0 || bw.Slayer != SlayerName.None)
                    return true;
            }
            else if (item is BaseArmor ba)
            {
                if (ba.ProtectionLevel > ArmorProtectionLevel.Regular || ba.DurabilityLevel > ArmorDurabilityLevel.Regular)
                    return true;

                if (ba.MagicCharges > 0)
                    return true;
            }
            else if (item is BaseJewel bj)
            {
                if (bj.MagicCharges > 0)
                    return true;
            }
            else if (item is BaseWand bwnd)
            {
                if (bwnd.MagicCharges > 0)
                    return true;
            }
            else if (item is BaseClothing bc)
            {
                if (bc.MagicCharges > 0)
                    return true;
            }
            return false;
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version);
            switch (version)
            {
                case 1:
                    writer.Write(m_level);
                    writer.Write(m_max_levels);
                    writer.Write(m_my_type.Name);
                    goto case 0;
                case 0:
                    break;
            }

        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    m_level = reader.ReadInt();
                    m_max_levels = reader.ReadInt();
                    string temp = reader.ReadString();
                    m_my_type = ScriptCompiler.FindTypeByName(temp);
                    goto case 0;
                case 0:
                    break;
            }
        }
        public void HueAllLayers(int hue)
        {
            try
            {
                //make sure cloths/weapons are newbied so they don't drop
                Item[] items = new Item[19];
                items[0] = this.FindItemOnLayer(Layer.Shoes);
                items[1] = this.FindItemOnLayer(Layer.Pants);
                items[2] = this.FindItemOnLayer(Layer.Shirt);
                items[3] = this.FindItemOnLayer(Layer.Helm);
                items[4] = this.FindItemOnLayer(Layer.Gloves);
                items[5] = this.FindItemOnLayer(Layer.Neck);
                items[6] = this.FindItemOnLayer(Layer.Waist);
                items[7] = this.FindItemOnLayer(Layer.InnerTorso);
                items[8] = this.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = this.FindItemOnLayer(Layer.Arms);
                items[10] = this.FindItemOnLayer(Layer.Cloak);
                items[11] = this.FindItemOnLayer(Layer.OuterTorso);
                items[12] = this.FindItemOnLayer(Layer.OuterLegs);
                items[13] = this.FindItemOnLayer(Layer.InnerLegs);
                items[14] = this.FindItemOnLayer(Layer.Bracelet);
                items[15] = this.FindItemOnLayer(Layer.Ring);
                items[16] = this.FindItemOnLayer(Layer.Earrings);
                items[17] = this.FindItemOnLayer(Layer.OneHanded);
                items[18] = this.FindItemOnLayer(Layer.TwoHanded);
                for (int i = 0; i < 19; i++)
                {
                    if (items[i] != null)
                    {
                        items[i].Hue = hue;
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

        }
        public void RemoveHueOnLiftAllLayers()
        {
            try
            {
                //make sure cloths/weapons are not movable so they stay equipped
                Item[] items = new Item[19];
                items[0] = this.FindItemOnLayer(Layer.Shoes);
                items[1] = this.FindItemOnLayer(Layer.Pants);
                items[2] = this.FindItemOnLayer(Layer.Shirt);
                items[3] = this.FindItemOnLayer(Layer.Helm);
                items[4] = this.FindItemOnLayer(Layer.Gloves);
                items[5] = this.FindItemOnLayer(Layer.Neck);
                items[6] = this.FindItemOnLayer(Layer.Waist);
                items[7] = this.FindItemOnLayer(Layer.InnerTorso);
                items[8] = this.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = this.FindItemOnLayer(Layer.Arms);
                items[10] = this.FindItemOnLayer(Layer.Cloak);
                items[11] = this.FindItemOnLayer(Layer.OuterTorso);
                items[12] = this.FindItemOnLayer(Layer.OuterLegs);
                items[13] = this.FindItemOnLayer(Layer.InnerLegs);
                items[14] = this.FindItemOnLayer(Layer.Bracelet);
                items[15] = this.FindItemOnLayer(Layer.Ring);
                items[16] = this.FindItemOnLayer(Layer.Earrings);
                items[17] = this.FindItemOnLayer(Layer.OneHanded);
                items[18] = this.FindItemOnLayer(Layer.TwoHanded);
                for (int i = 0; i < 19; i++)
                {
                    if (items[i] != null)
                    {
                        items[i].SetItemBool(Item.ItemBoolTable.RemoveHueOnLift, true);
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

        }

        public static void DeNewbieLayers(Mobile src)
        {
            try
            {
                Item[] items = new Item[21];
                items[0] = src.FindItemOnLayer(Layer.Shoes);
                items[1] = src.FindItemOnLayer(Layer.Pants);
                items[2] = src.FindItemOnLayer(Layer.Shirt);
                items[3] = src.FindItemOnLayer(Layer.Helm);
                items[4] = src.FindItemOnLayer(Layer.Gloves);
                items[5] = src.FindItemOnLayer(Layer.Neck);
                items[6] = src.FindItemOnLayer(Layer.Waist);
                items[7] = src.FindItemOnLayer(Layer.InnerTorso);
                items[8] = src.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = src.FindItemOnLayer(Layer.Arms);
                items[10] = src.FindItemOnLayer(Layer.Cloak);
                items[11] = src.FindItemOnLayer(Layer.OuterTorso);
                items[12] = src.FindItemOnLayer(Layer.OuterLegs);
                items[13] = src.FindItemOnLayer(Layer.InnerLegs);
                items[14] = src.FindItemOnLayer(Layer.Bracelet);
                items[15] = src.FindItemOnLayer(Layer.Ring);
                items[16] = src.FindItemOnLayer(Layer.Earrings);
                items[17] = src.FindItemOnLayer(Layer.OneHanded);
                items[18] = src.FindItemOnLayer(Layer.TwoHanded);
                items[19] = src.FindItemOnLayer(Layer.Hair);
                items[20] = src.FindItemOnLayer(Layer.FacialHair);
                for (int i = 0; i < items.Length; i++)
                {   // No spellbooks (no runebools though - too personal)
                    if (items[i] != null && items[i] is Runebook == false && items[i] is Spellbook == false)
                    {
                        items[i].LootType = LootType.Regular;
                    }
                }
            }
            catch (Exception exc)
            {
                Diagnostics.LogHelper.LogException(exc);
                System.Console.WriteLine("Send to Zen please: ");
                System.Console.WriteLine("Exception caught in Spawner.CopyLayers: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }

        }
    }
}