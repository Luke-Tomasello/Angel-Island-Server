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

/* Scripts/Engines/Plants/PlantItem.cs
 * CHANGELOG:
 *  3/27/22, Adam (Liquor)
 *      Allow the application of liquor to a plant (or decorative plant) to bleach out all genetic colors (hue)
 *  3/24/22, Adam
 *      Add virtual OnAfterDecorative() - not used
 *  3/17/22, Adam
 *      Virtualize several functions so we can derive our own plant items
 *      Fix a bug in NextGrowthPossible (9/23/04 - Pix) Can't believe this has been busted for so long.
 *      Basically, when a plant if made Decorative, it's PlantSystem is set to null (correct). But
 *          NextGrowthPossible wasn't checking for a null PlantSystem and crashing.
 *	9/23/04 - Pix
 *		Added property NextGrowthPossible so GMs can see how much time (minimum) before the
 *		next growth happens.
 */

using Server.Gumps;
using Server.Items;
using Server.Multis;
using System;
using System.Collections;

namespace Server.Engines.Plants
{
    public enum PlantStatus
    {
        BowlOfDirt = 0,
        Seed = 1,
        Sapling = 2,
        Plant = 4,
        FullGrownPlant = 7,
        DecorativePlant = 10,
        DeadTwigs = 11,

        Stage1 = 1,
        Stage2 = 2,
        Stage3 = 3,
        Stage4 = 4,
        Stage5 = 5,
        Stage6 = 6,
        Stage7 = 7,
        Stage8 = 8,
        Stage9 = 9
    }

    public class PlantItem : Item, ISecurable
    {
        private PlantSystem m_PlantSystem;

        private PlantStatus m_PlantStatus;
        private PlantType m_PlantType;
        private PlantHue m_PlantHue;
        private bool m_ShowType;

        private SecureLevel m_Level;

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get { return m_Level; }
            set { m_Level = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextGrowthPossible
        {   // 3/17/22, Adam: Added the null check here. Seems to always have been broken but only shows itself
            //  When [props is done on a DecorativePlant (DecorativePlants have their PlantSystem set to null.)
            get { return PlantSystem == null ? DateTime.MaxValue : this.PlantSystem.NextGrowth; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantSystem PlantSystem { get { return m_PlantSystem; } set { } }

        public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        public override string DefaultName
        {
            get
            {
                if (m_PlantStatus >= PlantStatus.FullGrownPlant && m_PlantStatus < PlantStatus.DecorativePlant)
                {
                    PlantTypeInfo typeInfo = PlantTypeInfo.GetInfo(m_PlantType);

                    if (typeInfo.Name.Number == 0 && typeInfo.Name.String != null)
                        return typeInfo.Name.String;
                }

                return base.DefaultName;
            }
        }

        public override int LabelNumber
        {
            get
            {
                if (m_PlantStatus >= PlantStatus.DeadTwigs)
                    return base.LabelNumber;
                else if (m_PlantStatus >= PlantStatus.DecorativePlant)
                    return 1061924; // a decorative plant
                else if (m_PlantStatus >= PlantStatus.FullGrownPlant)
                    return PlantTypeInfo.GetInfo(m_PlantType).Name.Number;
                else
                    return 1029913; // plant bowl
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantStatus PlantStatus
        {
            get { return m_PlantStatus; }
            set
            {
                if (m_PlantStatus == value || value < PlantStatus.BowlOfDirt || value > PlantStatus.DeadTwigs)
                    return;

                double ratio;
                if (m_PlantSystem != null)
                    ratio = (double)m_PlantSystem.Hits / m_PlantSystem.MaxHits;
                else
                    ratio = 1.0;

                m_PlantStatus = value;

                if (m_PlantStatus >= PlantStatus.DecorativePlant)
                {
                    m_PlantSystem = null;
                }
                else
                {
                    if (m_PlantSystem == null)
                        m_PlantSystem = new PlantSystem(this, false);

                    int hits = (int)(m_PlantSystem.MaxHits * ratio);

                    if (hits == 0 && m_PlantStatus > PlantStatus.BowlOfDirt)
                        m_PlantSystem.Hits = hits + 1;
                    else
                        m_PlantSystem.Hits = hits;
                }

                Update();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantType PlantType
        {
            get { return m_PlantType; }
            set
            {
                m_PlantType = value;
                Update();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantHue PlantHue
        {
            get { return m_PlantHue; }
            set
            {
                m_PlantHue = value;
                Update();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowType
        {
            get { return m_ShowType; }
            set
            {
                m_ShowType = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool ValidGrowthLocation
        {
            get
            {
                Mobile owner = RootParent as Mobile;

                if (owner != null && IsChildOf(owner.Backpack))
                    return true;

                if (owner != null && IsChildOf(owner.BankBox))
                    return true;

                if (IsLockedDown && Parent == null)
                    return true;

                Regions.TownshipRegion tsr = Regions.TownshipRegion.GetTownshipAt(this);

                if (tsr != null && tsr.TStone != null && tsr.TStone.IsLockedDown(this) && Parent == null)
                    return true;

                return false;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsGrowable
        {
            get { return m_PlantStatus >= PlantStatus.BowlOfDirt && m_PlantStatus <= PlantStatus.Stage9; }
        }

        [Constructable]
        public PlantItem()
            : this(false)
        {
        }

        [Constructable]
        public PlantItem(bool fertileDirt)
            : base(0x1602)
        {
            Weight = 1.0;

            m_PlantStatus = PlantStatus.BowlOfDirt;
            m_PlantSystem = new PlantSystem(this, fertileDirt);
            m_Level = SecureLevel.CoOwners;
        }

        public PlantItem(Serial serial)
            : base(serial)
        {
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        public int GetLocalizedPlantStatus()
        {
            if (m_PlantStatus >= PlantStatus.Plant)
                return 1060812; // plant
            else if (m_PlantStatus >= PlantStatus.Sapling)
                return 1023305; // sapling
            else
                return 1060810; // seed
        }

        public virtual void Update()
        {
            if (m_PlantStatus >= PlantStatus.DeadTwigs)
            {
                ItemID = 0x1B9D;
                Hue = PlantHueInfo.GetInfo(m_PlantHue).Hue;
            }
            else if (m_PlantStatus >= PlantStatus.FullGrownPlant)
            {
                ItemID = PlantTypeInfo.GetInfo(m_PlantType).ItemID;
                Hue = PlantHueInfo.GetInfo(m_PlantHue).Hue;
            }
            else if (m_PlantStatus >= PlantStatus.Plant)
            {
                ItemID = 0x1600;
                Hue = 0;
            }
            else
            {
                ItemID = 0x1602;
                Hue = 0;
            }

            InvalidateProperties();
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (m_PlantStatus >= PlantStatus.DecorativePlant)
            {
                base.AddNameProperty(list);
            }
            else if (m_PlantStatus >= PlantStatus.FullGrownPlant)
            {
                PlantHueInfo hueInfo = PlantHueInfo.GetInfo(m_PlantHue);
                PlantTypeInfo typeInfo = PlantTypeInfo.GetInfo(m_PlantType);
                string args = string.Format("#{0}\t#{1}\t{2}", m_PlantSystem.GetLocalizedHealth(), hueInfo.Name, typeInfo.Argument);

                if (typeInfo.ContainsPlant)
                {
                    // a ~1_HEALTH~ [bright] ~2_COLOR~ ~3_NAME~
                    list.Add(hueInfo.IsBright() ? 1061891 : 1061889, args);
                }
                else
                {
                    // a ~1_HEALTH~ [bright] ~2_COLOR~ ~3_NAME~ plant
                    list.Add(hueInfo.IsBright() ? 1061892 : 1061890, args);
                }
            }
            else if (m_PlantStatus >= PlantStatus.Seed)
            {
                PlantHueInfo hueInfo = PlantHueInfo.GetInfo(m_PlantHue);
                string args = string.Format("#{0}\t#{1}\t#{2}", m_PlantSystem.GetLocalizedDirtStatus(), m_PlantSystem.GetLocalizedHealth(), hueInfo.Name);

                if (m_ShowType)
                {
                    PlantTypeInfo typeInfo = PlantTypeInfo.GetInfo(m_PlantType);
                    args += "\t" + typeInfo.Argument;

                    if (typeInfo.ContainsPlant && m_PlantStatus == PlantStatus.Plant)
                    {
                        // a bowl of ~1_val~ dirt with a ~2_val~ [bright] ~3_val~ ~4_val~
                        list.Add(hueInfo.IsBright() ? 1060832 : 1060831, args);
                    }
                    else
                    {
                        // a bowl of ~1_val~ dirt with a ~2_val~ [bright] ~3_val~ ~4_val~ ~5_val~
                        list.Add(hueInfo.IsBright() ? 1061887 : 1061888, args + "\t#" + GetLocalizedPlantStatus().ToString());
                    }
                }
                else
                {
                    // a bowl of ~1_val~ dirt with a ~2_val~ [bright] ~3_val~ ~4_val~
                    list.Add(hueInfo.IsBright() ? 1060832 : 1060831, args + "\t#" + GetLocalizedPlantStatus().ToString());
                }
            }
            else
            {
                list.Add(1060830, "#" + m_PlantSystem.GetLocalizedDirtStatus()); // a bowl of ~1_val~ dirt
            }
        }

        public virtual bool IsUsableBy(Mobile from)
        {
            if (IsChildOf(from.Backpack))
                return true;

            if (IsChildOf(from.BankBox))
                return true;

            if (IsLockedDown && IsAccessibleTo(from))
                return true;

            Item root = RootParent as Item;

            if (root != null && root.IsSecure && root.IsAccessibleTo(from))
                return true;

            Regions.TownshipRegion tsr = Regions.TownshipRegion.GetTownshipAt(this);

            if (tsr != null && tsr.TStone != null && tsr.TStone.IsLockedDown(this) && IsAccessibleTo(from))
                return true;

            return false;
        }

        public virtual void OnAfterDecorative()
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_PlantStatus >= PlantStatus.DecorativePlant)
                return;

            if (!IsUsableBy(from))
            {
                OnNotUsable(from);
                return;
            }

            from.SendGump(new MainPlantGump(this));
        }

        public virtual void OnNotUsable(Mobile from)
        {
            LabelTo(from, 1061856); // You must have the item in your backpack or locked down in order to use it.
        }

        public virtual void PlantSeed(Mobile from, Seed seed)
        {
            if (m_PlantStatus >= PlantStatus.FullGrownPlant)
            {
                LabelTo(from, 1061919); // You must use a seed on a bowl of dirt!
            }
            else if (!IsUsableBy(from))
            {
                LabelTo(from, 1061921); // The bowl of dirt must be in your pack, or you must lock it down.
            }
            else if (m_PlantStatus != PlantStatus.BowlOfDirt)
            {
                if (m_PlantStatus >= PlantStatus.Plant)
                    LabelTo(from, "This bowl of dirt already has a plant in it!");
                else if (m_PlantStatus >= PlantStatus.Sapling)
                    LabelTo(from, "This bowl of dirt already has a sapling in it!");
                else
                    LabelTo(from, "This bowl of dirt already has a seed in it!");
            }
            else if (m_PlantSystem.Water < 2)
            {
                LabelTo(from, 1061920); // The dirt in this bowl needs to be softened first.
            }
            else
            {
                m_PlantType = seed.PlantType;
                m_PlantHue = seed.PlantHue;
                m_ShowType = seed.ShowType;

                seed.Delete();

                PlantStatus = PlantStatus.Seed;

                m_PlantSystem.Reset(false);

                LabelTo(from, 1061922); // You plant the seed in the bowl of dirt.
            }
        }

        public virtual void Die()
        {
            if (m_PlantStatus >= PlantStatus.FullGrownPlant)
            {
                PlantStatus = PlantStatus.DeadTwigs;
            }
            else
            {
                PlantStatus = PlantStatus.BowlOfDirt;
                m_PlantSystem.Reset(true);
            }
        }

        public void Pour(Mobile from, Item item)
        {
            if (m_PlantStatus >= PlantStatus.DeadTwigs)
                return;

            if (m_PlantStatus == PlantStatus.DecorativePlant)
            {
                #region Bleaching

                BaseBeverage beverage = item as BaseBeverage;

                if (beverage != null && !beverage.IsEmpty && beverage.Pourable && beverage.Content == BeverageType.Liquor && beverage.ValidateUse(from, true))
                {
                    if (!IsAccessibleTo(from))
                        from.SendMessage("You cannot bleach a plant you do not own.");
                    else if (PlantHue == PlantHue.Plain)
                        LabelTo(from, "There is no color to bleach from this plant.");
                    else
                        from.SendGump(new RemoveGeneticHueGump(this));

                    return;
                }

                #endregion

                LabelTo(from, 1053049); // This is a decorative plant, it does not need watering!
                return;
            }

            if (!IsUsableBy(from))
            {
                LabelTo(from, 1061856); // You must have the item in your backpack or locked down in order to use it.
                return;
            }

            if (item is BaseBeverage)
            {
                BaseBeverage beverage = (BaseBeverage)item;

                if (!beverage.IsEmpty && beverage.Pourable && beverage.Content == BeverageType.Liquor)
                {
                    if (PlantHue == PlantHue.Plain)
                        LabelTo(from, "There is no color to bleach from this plant.");
                    else if (beverage.ValidateUse(from, true))
                        from.SendGump(new RemoveGeneticHueGump(this));

                    return;
                }
                else if (beverage.IsEmpty || !beverage.Pourable || beverage.Content != BeverageType.Water)
                {
                    LabelTo(from, 1053069); // You can't use that on a plant!
                    return;
                }

                if (!beverage.ValidateUse(from, true))
                    return;

                beverage.Quantity--;
                m_PlantSystem.Water++;

                from.PlaySound(0x4E);
                LabelTo(from, 1061858); // You soften the dirt with water.
            }
            else if (item is BasePotion)
            {
                BasePotion potion = (BasePotion)item;

                int message;
                if (ApplyPotion(potion.PotionEffect, false, out message))
                {
                    potion.Delete();
                    from.PlaySound(0x240);
                    from.AddToBackpack(new Bottle());
                }
                LabelTo(from, message);
            }
            else if (item is PotionKeg)
            {
                PotionKeg keg = (PotionKeg)item;

                if (keg.Held <= 0)
                {
                    LabelTo(from, 1053069); // You can't use that on a plant!
                    return;
                }

                int message;
                if (ApplyPotion(keg.Type, false, out message))
                {
                    keg.Held--;
                    from.PlaySound(0x240);
                }
                LabelTo(from, message);
            }
            else
            {
                LabelTo(from, 1053069); // You can't use that on a plant!
            }
        }

        public bool ApplyPotion(PotionEffect effect, bool testOnly, out int message)
        {
            if (m_PlantStatus >= PlantStatus.DecorativePlant)
            {
                message = 1053049; // This is a decorative plant, it does not need watering!
                return false;
            }

            if (m_PlantStatus == PlantStatus.BowlOfDirt)
            {
                message = 1053066; // You should only pour potions on a plant or seed!
                return false;
            }

            bool full = false;

            if (effect == PotionEffect.PoisonGreater || effect == PotionEffect.PoisonDeadly)
            {
                if (m_PlantSystem.IsFullPoisonPotion)
                    full = true;
                else if (!testOnly)
                    m_PlantSystem.PoisonPotion++;
            }
            else if (effect == PotionEffect.CureGreater)
            {
                if (m_PlantSystem.IsFullCurePotion)
                    full = true;
                else if (!testOnly)
                    m_PlantSystem.CurePotion++;
            }
            else if (effect == PotionEffect.HealGreater)
            {
                if (m_PlantSystem.IsFullHealPotion)
                    full = true;
                else if (!testOnly)
                    m_PlantSystem.HealPotion++;
            }
            else if (effect == PotionEffect.StrengthGreater)
            {
                if (m_PlantSystem.IsFullStrengthPotion)
                    full = true;
                else if (!testOnly)
                    m_PlantSystem.StrengthPotion++;
            }
            else if (effect == PotionEffect.PoisonLesser || effect == PotionEffect.Poison || effect == PotionEffect.CureLesser || effect == PotionEffect.Cure ||
                effect == PotionEffect.HealLesser || effect == PotionEffect.Heal || effect == PotionEffect.Strength)
            {
                message = 1053068; // This potion is not powerful enough to use on a plant!
                return false;
            }
            else
            {
                message = 1053069; // You can't use that on a plant!
                return false;
            }

            if (full)
            {
                message = 1053065; // The plant is already soaked with this type of potion!
                return false;
            }
            else
            {
                message = 1053067; // You pour the potion over the plant.
                return true;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((int)m_Level);

            writer.Write((int)m_PlantStatus);
            writer.Write((int)m_PlantType);
            writer.Write((int)m_PlantHue);
            writer.Write((bool)m_ShowType);

            if (m_PlantStatus < PlantStatus.DecorativePlant)
                m_PlantSystem.Save(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                case 1:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 1)
                            m_Level = SecureLevel.CoOwners;

                        m_PlantStatus = (PlantStatus)reader.ReadInt();

                        if (version < 2)
                            m_PlantType = (PlantType)Seed.OldTreeConv(reader.ReadInt());
                        else
                            m_PlantType = (PlantType)reader.ReadInt();

                        m_PlantHue = (PlantHue)reader.ReadInt();
                        m_ShowType = reader.ReadBool();

                        if (m_PlantStatus < PlantStatus.DecorativePlant)
                            m_PlantSystem = new PlantSystem(this, reader);

                        break;
                    }
            }
        }
    }
}