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

/* Scripts/Multis/Camps/AngryMinerCamp.cs
 * ChangeLog
 *  11/2/2023, Adam (AngryMinerCampRare)
 *      Add 'takeable' ore cart and decorative small ore
 *      Ore cart auto-converts to a deed when dropped.
 *      Rules:
 *      1. all mobiles must be killed
 *      2. No high-level tames
 *      3. Only spawns for level 2 camps and above
 *  8/12/2023, Adam (AngryMinerCampRare)
 *      Ore and tool are always stealable.
 *  8/6/2023, Adam
 *      - Add AngryMinerCampRare
 *      - Make rares stealable 
 *      - Make rares unstealable if farmed by a tamer
 *  10/27/21, Adam
 *      Add the bonus for the Ancient Smithy Hammer (rare)
 * 
 *  Created 7/27/21 by adam
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using static Server.Utility;

namespace Server.Multis
{
    public class AngryMinerCamp : BaseCamp
    {
        string m_resourceType = null;
        int m_level;
        [Constructable]
        public AngryMinerCamp(string resourceType, int level)
            : base(0x1F6)   // BankerCamp type, seemed most appropriate
        {
            m_resourceType = resourceType;
            m_level = level;
        }

        public override void AddComponents()
        {
            AddMobile(new AngryMiner(m_resourceType), 5, -4, 3, 7);
            AddMobile(new AngryMiner(m_resourceType), 5, 4, -2, 0);
            AddMobile(new AngryMiner(m_resourceType), 5, -2, -3, 0);
            AddMobile(new AngryMiner(m_resourceType), 5, 2, -3, 0);
        }

        public AngryMinerCamp(Serial serial)
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

    public class AngryMinerCampSmall : BaseCamp
    {
        string m_resourceType = null;
        public string ResourceType { get { return m_resourceType; } set { m_resourceType = value; } }
        int m_level;
        public int Level { get { return m_level; } set { m_level = value; } }
        [Constructable]
        public AngryMinerCampSmall(string resourceType, int level)
            : base(0x1F14)   // recall rune
        {
            //m_resourceType = ScriptCompiler.FindTypeByName(resourceType);
            m_resourceType = resourceType;
            m_level = level;
        }

        public override void AddComponents()
        {
            Point3D pt = new Point3D(this.Location);
            List<object> toMove = new List<object>();
            Point3D center = new Point3D(this.X - 0, this.Y - 0, this.Z - 0);

            TentWalls walls = new TentWalls(TentStyle.Siege);
            TentRoof roof = new TentRoof(877);
            TentFloor floor = new TentFloor();
            AddItem(walls, 0, 0, 0);
            AddItem(roof, 0, 0, 0);
            AddItem(floor, 0, 0, 0);

            // Deco
            Item anvil = Activator.CreateInstance(ScriptCompiler.FindTypeByName("Anvil")) as Item;
            AddItem(anvil, -1, 0, 0);
            Item box = Activator.CreateInstance(ScriptCompiler.FindTypeByName("MagicBox")) as Item;
            box.ItemID = 0xE80;
            box.Movable = false;
            // level 1 chest lock strength (magic unlock ok)
            (box as MagicBox).Locked = true;
            (box as MagicBox).LockLevel = 42;
            (box as MagicBox).MaxLockLevel = 92;
            AddItem(box, 0, -1, 0);
            Item ingotStack = CreateIngotStack(m_resourceType);
            ingotStack.Movable = false;
            AddItem(ingotStack, -1, -1, 0);
            // special rare
            Item tool;
            if (Utility.RandomBool())
            {
                tool = Activator.CreateInstance(ScriptCompiler.FindTypeByName("AncientSmithyHammer")) as Item;
                CraftResourceInfo ri = CraftResources.GetInfo(CraftResources.GetFromType(ScriptCompiler.FindTypeByName(m_resourceType)));

                if (ri != null)
                {
                    tool.Hue = ri.Hue;
                    BaseOre temp = Activator.CreateInstance(ScriptCompiler.FindTypeByName(m_resourceType)) as BaseOre;
                    if (temp != null)
                    {
                        (tool as AncientSmithyHammer).Resource = temp.Resource;
                        temp.Delete();
                    }
                }

                (tool as AncientSmithyHammer).Bonus = 20;
            }
            else
            {
                tool = Activator.CreateInstance(ScriptCompiler.FindTypeByName("GargoylesPickaxe")) as Item;
                CraftResourceInfo ri = CraftResources.GetInfo(CraftResources.GetFromType(ScriptCompiler.FindTypeByName(m_resourceType)));
                if (ri != null)
                {
                    tool.Hue = ri.Hue;
                    BaseOre temp = Activator.CreateInstance(ScriptCompiler.FindTypeByName(m_resourceType)) as BaseOre;
                    if (temp != null)
                    {
                        (tool as GargoylesPickaxe).Resource = temp.Resource;
                        temp.Delete();
                    }
                }
            }

            // make sure it's stealable
            tool.Weight = Math.Min(9, tool.Weight);

            // link the tool to the BaseCamp, and put the tool in the magic box
            tool.BaseCamp = this;
            box.AddItem(tool);

            // don't addItem if it's a rare drop, or the system will delete it later.
            if (Utility.Chance(0.07))
                tool.Movable = true;
            else
                tool.Movable = false;

            // create some angry miners
            Mobile m = null;
            for (int jx = 0; jx < m_level; jx++)
                switch (jx)
                {
                    case 0: AddMobile(m = new AngryMiner(m_resourceType), 5, -4, 3, 7); break;
                    case 1: AddMobile(m = new AngryMiner(m_resourceType), 5, 4, -2, 0); break;
                    case 2: AddMobile(m = new AngryMiner(m_resourceType), 5, -2, -3, 0); break;
                    case 3: AddMobile(m = new AngryMiner(m_resourceType), 5, 2, -3, 0); break;
                }

            if (m != null)
            {   // give the box a personalized name
                string lastWord = m.Name.Split(' ').Last();
                if (lastWord.EndsWith("s"))
                    box.Name = lastWord + "' " + "stuff";
                else
                    box.Name = lastWord + "'s " + "stuff";
            }
        }

        Item CreateIngotStack(string resourceType)
        {
            switch (resourceType)
            {
                case "CopperOre":
                    return Activator.CreateInstance(ScriptCompiler.FindTypeByName("CopperIngots")) as Item;

                case "DullCopperOre":
                    return Activator.CreateInstance(ScriptCompiler.FindTypeByName("DullCopperIngots")) as Item;

                case "IronOre":
                    return Activator.CreateInstance(ScriptCompiler.FindTypeByName("IronIngots")) as Item;

                case "ShadowIronOre":
                    return Activator.CreateInstance(ScriptCompiler.FindTypeByName("ShadowIronIngots")) as Item;

                case "GoldOre":
                    return Activator.CreateInstance(ScriptCompiler.FindTypeByName("GoldIngots")) as Item;

                case "SilverOre":   // not implemented
                    return Activator.CreateInstance(ScriptCompiler.FindTypeByName("SilverIngots")) as Item;

                case "BronzeOre":
                    return Activator.CreateInstance(ScriptCompiler.FindTypeByName("BronzeIngots")) as Item;

                case "AgapiteOre":
                    return Activator.CreateInstance(ScriptCompiler.FindTypeByName("AgapiteIngots")) as Item;

                case "VeriteOre":
                    return Activator.CreateInstance(ScriptCompiler.FindTypeByName("VeriteIngots")) as Item;

                case "ValoriteOre":
                    return Activator.CreateInstance(ScriptCompiler.FindTypeByName("ValoriteIngots")) as Item;
            }

            return null;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
        }

        public AngryMinerCampSmall(Serial serial)
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

    public class AngryMinerCampRare : AngryMinerCampSmall
    {
        [Constructable]
        public AngryMinerCampRare()
            : base(null, 0)   // recall rune
        {
            base.ResourceType = RandomIngotStack();
            base.Level = LevelForIngotStack(base.ResourceType);
            Timer.DelayCall(TimeSpan.FromSeconds(3), new TimerCallback(CheckCorruptedTick));
        }
        public override void AddComponents()
        {
            base.AddComponents();
            foreach (Item item in ItemComponents)
            {
                if (item != null)
                {
                    if (IsIngotStack(item.GetType()))
                    {
                        item.Movable = false;
                        item.LootType = LootType.Rare;
                        item.SetItemBool(Item.ItemBoolTable.MustSteal, true);
                    }
                    else if (item.GetType() == typeof(MagicBox))
                    {
                        foreach (Item reward in (item as BaseContainer).Items)
                            if ((reward is AncientSmithyHammer || reward is GargoylesPickaxe))
                            {   // set movable to false here since AngryMinerCampSmall sometimes makes this movable
                                reward.Movable = false;
                                reward.LootType = LootType.Rare;
                                reward.SetItemBool(Item.ItemBoolTable.MustSteal, true);
                            }
                    }
                }
            }

            if (Level > 1)
            {   // add some small 'decorative ore'
                Point3D[] oreLocs = new[] { new Point3D(-3, 6, 0), new Point3D(-2, 6, 0), new Point3D(-4, 5, 0) };
                Point3D px = oreLocs[Utility.Random(oreLocs.Length)];
                int[] ore = new[] { 0x19B7, 0x19BA };
                AddItem(new Static(ore[Utility.Random(ore.Length)]), px.X, px.Y, px.Z);
                // add the cart
                AddItem(new OreCartEastAddon(), -2, 5, 0);
            }
        }
        public bool IsIngotStack(Type type)
        {
            if (
                type == typeof(CopperIngots) || type == typeof(DullCopperIngots) || type == typeof(IronIngots) ||
                type == typeof(ShadowIronIngots) || type == typeof(GoldIngots) || type == typeof(SilverIngots) ||
                type == typeof(BronzeIngots) || type == typeof(AgapiteIngots) || type == typeof(VeriteIngots) ||
                type == typeof(ValoriteIngots)
                )
                return true;

            return false;
        }

        private void CheckCorruptedTick()
        {
            if (Deleted)
                return;

            if (Map != null && Map != Map.Internal && CampCorrupted())
            {
                // if they have a pet, make the stuff not stealable
                if (HighPowerTameNearby())
                {
                    foreach (Item item in ItemComponents)
                    {
                        if (item != null)
                        {
                            if (IsIngotStack(item.GetType()))
                            {
                                item.SetItemBool(Item.ItemBoolTable.MustSteal, false);
                            }
                            else if (item.GetType() == typeof(MagicBox))
                            {
                                foreach (Item reward in (item as BaseContainer).Items)
                                    if ((reward is AncientSmithyHammer || reward is GargoylesPickaxe) && Utility.RandomBool())
                                    {
                                        reward.SetItemBool(Item.ItemBoolTable.MustSteal, false);
                                    }
                            }
                        }
                    }
                }

                // if all camp mobiles are dead and there are no tames nearby, allow the cart to be picked up
                bool allDead = true;
                if (MobileComponents != null && !HighPowerTameNearby())
                    foreach (Mobile m in MobileComponents)
                        if (m != null && m.Deleted == false)
                            allDead = false;
                if (allDead)
                {
                    int[] ore = new[] { 0x19B7, 0x19BA };
                    foreach (Item item in ItemComponents)
                        if (item != null)
                            if (item is OreCartEastAddon)
                            {   // player can now pick up this addon. Will auto-convert to deed when dropped
                                foreach (var c in (item as OreCartEastAddon).Components)
                                    if (c is AddonComponent ac && ac.Movable == false)
                                        ac.Movable = true;
                            }
                            else if (ore.Contains(item.ItemID))
                            {
                                item.Movable = true;
                            }
                }
            }

            Timer.DelayCall(TimeSpan.FromSeconds(3), new TimerCallback(CheckCorruptedTick));
        }
        private bool HighPowerTameNearby()
        {
            IPooledEnumerable eable = Map.GetMobilesInRange(this.Location, Map.MaxLOSDistance);
            foreach (Mobile m in eable)
            {
                // if a creature 
                if (m is BaseCreature)
                {   // if controlled (summoned or tame)
                    if (((m as BaseCreature).Controlled && (m as BaseCreature).ControlMaster != null) || ((m as BaseCreature).Summoned && (m as BaseCreature).SummonMaster != null))
                    {
                        if (true)
                        {
                            eable.Free();
                            return true;
                        }
                    }
                }

            }
            eable.Free();

            return false;
        }
        private string RandomIngotStack()
        {
            string[] table = new[] {
            "CopperOre","DullCopperOre","IronOre","ShadowIronOre","GoldOre",
            "SilverOre","BronzeOre","AgapiteOre","VeriteOre","ValoriteOre",
            };

            return table[Utility.RandomMinMaxExp(table.Length, Exponent.d0_20)];
        }
        private int LevelForIngotStack(string res)
        {
            switch (res)
            {
                case "ValoriteOre":
                    return 4;
                case "VeriteOre":
                    return 3;
                case "AgapiteOre":
                    return 2;
                default:
                    return 1;
            }
        }
        public AngryMinerCampRare(Serial serial)
        : base(serial)
        {
            Timer.DelayCall(TimeSpan.FromSeconds(3), new TimerCallback(CheckCorruptedTick));
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