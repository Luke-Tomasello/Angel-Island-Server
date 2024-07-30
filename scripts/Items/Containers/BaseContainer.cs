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

/* Scripts\Items\Containers\BaseContainer.cs
 * ChangeLog:
 *  12/11/2023, Adam
 *      Don't generate Enchanted Scrolls if we are in Angel Island Prison
 *  11/29/2023, Adam (StrongResourceBackpack)
 *      StrongBackpack that holds only resources like boards, logs, cotton, cloth, etc.
 *  8/12/22, Adam (BackPack)
 *      Mortalis: Add FeatureBits DynamicFeatures
 *      These are used by mortalis when moving the players backpack items to a new 'prison issued' backpack
 *      this backpack is then dropped into the players bank. Mortalis treats this pack differently (if it's 'prison issued',)
 *      by unpacking it into the players bank.
 *  04/02/22, Yoar
 *      Added MakersMark struct that stored both the crafter's serial and name as string.
 *      This way, the crafter tag persists after the mobile has been deleted.
 *  11/14/21, Yoar
 *      - Added Crafter, Quality, Resource
 *      - BaseContainer now implements ICraftable
 *      - Added versioning (starting at 0x80)
 *	10/27/21, Adam
 * 		Additional support for CheckNonLocalStorage() where you have nested containers
 *  10/17/21, Adam (NonLocalStorage)
 *      Add support for opening/adding/removing items from special containers
 *  9/8/21, Adam (PlayerOwned)
 *      Added the virtual function to server.container so it can call up this abstraction to find out
 *      if a container is player owned. This is important for 'overload exploit' tracking.
 *  8/23/21, Adam:
 *      So the goofballs have decided to pop guards pouches over and over, so I decided to have a little fun with them
 *      See: override ExecuteTrap(Mobile from, bool bAutoReset)
 *  7/31/2021, Adam
 *  Undo this:  2/28/05, Adam
 *		DefaultMaxWeight() added conditional (pm.AccessLevel > AccessLevel.Player) to
 *		allow players staff to carry unlimited weight.
 *		    This change, while mildly helpful, complicates various overload-exploit tests. 
 *		    Also, staff shouldn't be carrying that much stuff.
 *	7/11/10, adam
 *		replace local copy of memory system with the common implementation in Utility.Memory
 *	5/23/10, adam
 *		Add the notion of per-player lift memory.
 *		In Ransom chests, dungeon chests, and treasuremap chests we use this memory to halt/slow lift macros
 *	01/23/07, Pix
 *		In BaseContainer.OnItemLifted: needed to call the base class's OnItemLifted.
 *  01/07.07, Kit
 *      Reverted changes below
 *      Added virtual ManageLockDowns() to IBlockRazorSearch.
 *      override IsLockedDown set to call ManageLockdowns
 *  12/24/06, Adam
 *      In TryDropItem() reset the 'ready state' of IBlockRazorSearch.
 *      We do this to work around the funny way the razor executes OnDoubleClick() on item IDs
 *      that look like containers when dropped into a players backpack  
 *      Case: Drop into a players paperdoll-backpack
 *  12/22/06, Adam
 *      heavy commenting of the IBlockRazorSearch.
 *  12/21/06, Adam
 *      In OnDragDropInto() reset the 'ready state' of IBlockRazorSearch.
 *      We do this to work around the funny way razor executes OnDoubleClick() on item IDs
 *      that look like containers when dropped into a players backpack
 *      Case: Drop into a players open backpack
 *	10/19/06, Adam
 *		Record OnItemLifted() if the item is looted from a non-friend of the house
 *		(exploit audit trail)
 *  8/13/06, Rhiannon
 *		Changed OnDoubleClick to set range of 3 if container is inside a VendorBackpack.
 *  8/05/06, Taran Kain
 *		Modified BaseContainer.TryDropItem to re-use Container.TryDropItem code - was duplicated.
 *	6/7/05, erlein
 *		Added LOS check in BaseContainer to prevent access from behind walls.
 *	5/9/05, Adam
 *		Push the Deco flag down to the core Container level
 *	3/26/05, Adam
 *		Add SmallBasket
 *	2/28/05, Adam
 *		DefaultMaxWeight() added conditional (pm.AccessLevel > AccessLevel.Player) to
 *		allow players staff to carry unlimited weight.
 *	2/26/05, mith
 *		DefaultMaxWeight() Added conditional in case the Container is a player backpack (if Parent is Mobile)
 *			Since player backpacks are not movable, they had no weight limits.
 *	02/24/05, mith
 *		ClosedBarrel created. Weight set to 6 stones to make them stealable with moderate difficulty.
 *  02/17/05, mith
 *		CheckHold()	Override the Core.Container.CheckHold method, copied code from that method
 *			minus the CanStore checks. This enables players to drop items in public containers.
 *		MaxDefaultWeight() Added check if container is movable. That's how it worked in RC0.
 *			This changed in the core with 1.0.0, so we've modified it here instead.
 *		MaxDefaultWeight() put this property back in so that secures no longer have a max weight.
 *  02/15/05, Pixie
 *		CHANGED FOR RUNUO 1.0.0 MERGE.
 *	9/24/04, Adam
 *		Push the deco support all the way down to BaseContainer
 *		Remove explicit support for Deco from WoodenBox
 *	9/21/04, Adam
 *		New version of WoodenBox (Version 1)
 *		WoodenBox now supports the new Deco attribute for decorative containers.
 *	9/1/04, Pixie
 *		Added TinkerTrapableAttribute so we can mark containers as tinkertrapable or not.
 *	6/24/04, Pix
 *		Fixed dropping items onto closed lockeddown containers in houses locking down
 *		the items dropped.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Engines.Craft;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public abstract class BaseContainer : Container, ICraftable
    {
        #region diagnostics
        public static void Initialize()
        {
            Server.CommandSystem.Register("UpdateAllTotals", AccessLevel.GameMaster, new CommandEventHandler(UpdateAllTotals_OnCommand));
        }

        [Usage("UpdateAllTotals")]
        [Description("Recalculates the the total items / weight for this container and all parents.")]
        private static void UpdateAllTotals_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new UpdateAllTotalsTarget();
            e.Mobile.SendMessage("Target the container...");
        }
        private class UpdateAllTotalsTarget : Target
        {
            public UpdateAllTotalsTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is BaseContainer cont)
                {
                    from.SendMessage(string.Format("Before: ({0} items, {1} stones)", cont.TotalItems, cont.TotalWeight));
                    cont.UpdateAllTotals();
                    from.SendMessage(string.Format("After: ({0} items, {1} stones)", cont.TotalItems, cont.TotalWeight));
                }
                else
                    from.SendMessage("That is not a container.");
            }
        }
        #endregion diagnostics

        public override void DropOverflow(Item dropped, string logdata)
        {
            Engines.RecycleBin.RecycleBin.Add(dropped, this, logdata);
        }

        private Memory m_LiftMemory = new Memory();
        protected Memory LiftMemory { get { return m_LiftMemory; } }

        // record both non-house friends and staff looting items
        public override void OnItemLifted(Mobile from, Item item)
        {
            try
            {
                BaseHouse house = BaseHouse.FindHouseAt(item);
                if (house != null)
                {
                    bool bRecord = (house.IsFriend(from) == false || from.AccessLevel > AccessLevel.Player);
                    if (Movable == false && this.RootParent as Mobile == null && bRecord)
                    {
                        string text = String.Format("Looting: Non friend of house lifting item {0} from {1}.", item.Serial, this.Serial);
                        RecordCheater.TrackIt(from, text, true);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Error in Container.OnItemLifted(): " + e.Message);
                Console.WriteLine(e.StackTrace.ToString());
            }

            base.OnItemLifted(from, item);
        }

        public override bool PlayerOwned()
        {
            Item item = this;
            Mobile mobile = null;
            if (this.RootParent != null && this.RootParent is Item)
                item = this.RootParent as Item;
            else if (this.RootParent != null && this.RootParent is Mobile)
                mobile = this.RootParent as Mobile;

            if (BaseHouse.FindHouseAt(item) != null)
                return true;

            if (mobile != null)
                return true;

            if (BaseBoat.FindBoatAt(item) != null)
                return true;

            return false;
        }

        private MakersMark m_Crafter;
        private CraftQuality m_Quality;
        private CraftResource m_Resource;

        [CommandProperty(AccessLevel.GameMaster)]
        public MakersMark Crafter
        {
            get { return m_Crafter; }
            set { m_Crafter = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftQuality Quality
        {
            get { return m_Quality; }
            set { m_Quality = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set { m_Resource = value; Hue = CraftResources.GetHue(value); InvalidateProperties(); }
        }

        /*
         *  7/31/2021, Adam
         *  Undo this:  2/28/05, Adam
         *		DefaultMaxWeight() added conditional(pm.AccessLevel > AccessLevel.Player) to
         *      allow players staff to carry unlimited weight.
         *      Reasoning:
         *		    This change, while mildly helpful, complicates various overload-exploit tests. 
         *		    Also, staff shouldn't be carrying that much stuff.
         */
        public override int DefaultMaxWeight
        {
            get
            {
                if (Parent is PlayerMobile)
                {
                    /*PlayerMobile pm = Parent as PlayerMobile;
                    if (pm != null && pm.AccessLevel > AccessLevel.Player)
                        return 0;
                    else*/
                    return base.DefaultMaxWeight;
                }

                if (IsSecure || !Movable)
                    return 0;

                return base.DefaultMaxWeight;
            }
        }

        public virtual CraftResource DefaultResource { get { return CraftResource.None; } }

        public BaseContainer(Serial serial)
            : base(serial)
        {

        }

        public BaseContainer(int itemID)
            : base(itemID)
        {

        }

        public override bool IsAccessibleTo(Mobile m)
        {
            if (!BaseHouse.CheckAccessible(m, this))
                return false;

            return base.IsAccessibleTo(m);
        }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            // is in a house, check housing rules
            if (!BaseHouse.CheckHold(m, this, item, message, checkItems))
                return false;

            //return base.CheckHold( m, item, message, checkItems );
            int maxItems = this.MaxItems;

            if (checkItems && maxItems != 0 && (this.TotalItems + plusItems + item.TotalItems + (item.IsVirtualItem ? 0 : 1)) > maxItems)
            {
                if (message)
                    SendFullItemsMessage(m, item);

                return false;
            }
            else
            {
                int maxWeight = this.MaxWeight;

                if (maxWeight != 0 && (this.TotalWeight + plusWeight + item.TotalWeight + item.PileWeight) > maxWeight)
                {
                    if (message)
                        SendFullWeightMessage(m, item);

                    return false;
                }
            }
            object parent = this.Parent;

            while (parent != null)
            {
                if (parent is Container)
                    return ((Container)parent).CheckHold(m, item, message, checkItems, plusItems, plusWeight);
                else if (parent is Item)
                    parent = ((Item)parent).Parent;
                else
                    break;
            }

            return true;
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        //This is called when an item is placed into a closed container
        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            // Adam: Disallow dropping on a closed Deco container
            if (base.Deco)
                return false;

            return base.TryDropItem(from, dropped, sendFullMessage);
        }


        //This is called when an item is placed into an open container
        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            // Adam: Disallow dropping on an open Deco container
            if (base.Deco)
                return false;

            return base.OnDragDropInto(from, item, p);
        }
        #region Global Enchanted Scroll Replacement and Item Categorization
        private static List<Type> IgnoreList = new() { typeof(BaseGameBoard) };
        public override Item OnBeforeItemAdded(Item item, object parent)
        {   // Determine Origin for all newly created magic items
            Container cont = parent as Container;
            if (item != null && cont != null)
            {
                // if the item is a 'No Draw' item, delete it
                if (item.ItemID == 1)
                    item.Delete();
                else if (IsMagicLoot(item))
                {   //Get the Root Parent Item
                    Item rpi = (cont.RootParent is Item) ? cont.RootParent as Item : cont;
                    // corpses are constructed on the internal map, as are treasure chests.
                    //  things that fall under the township/house rule would be someone dropping something in a house container.
                    if (IsProperContainer(rpi) && !BadScrollRegion(rpi) && !IsIgnoredParent(rpi) && Validate(item))
                    {
                        // determine origin
                        Genesis genesis = (item.Origin != Genesis.Unknown) ? item.Origin : rpi is Corpse ? Genesis.Monster : rpi is LockableContainer ? ChestType(rpi) : Genesis.Unknown;
                        // establish Origin/Genesis for actual item
                        item.Origin = genesis;

                        if (Core.RuleSets.EnchantedScrollsEnabled())
                        {
                            if (IsValuableLoot(item) && CraftSystem.IsCraftable(item) && Utility.Chance(CoreAI.EnchantedScrollDropChance))
                            {
                                if (Core.Debug)
                                {
                                    LogHelper logger = new LogHelper("Enchanted Items.log", false, true);
                                    logger.Log(string.Format("Replacing {0} within {1}", item, rpi));
                                    logger.Finish();
                                }
                                // update the origin of original item
                                item.Origin = genesis | Genesis.Scroll;
                                item = new EnchantedScroll(item);
                                // establish Origin/Genesis for scroll item
                                item.Origin = genesis & ~Genesis.Scroll;
                            }
                        }
                    }
                }
            }
            return item;
        }
        private bool BadScrollRegion(Item item)
        {
            Point3D location;
            Map map;
            if (item == null)
            {
                return false;
            }
            else if (item is Corpse c && c.Owner is Mobile m)
            {
                location = m.Location;
                map = m.Map;
                bool AngelIsland = Region.Find(location, map) != null && Region.Find(location, map).IsAngelIslandRules;
                bool TownshipOrHouse = Utility.InTownshipOrHouse(m);
                return AngelIsland || TownshipOrHouse;
            }
            else if (item.Map != Map.Internal)
            {
                location = item.Location;
                map = item.Map;
                bool AngelIsland = Region.Find(location, map) != null && Region.Find(location, map).IsAngelIslandRules;
                bool TownshipOrHouse = Utility.InTownshipOrHouse(item);
                return AngelIsland || TownshipOrHouse;
            }

            return false;
        }
        private Item.Genesis ChestType(Item item)
        {
            if (item != null)
            {
                if (item.GetType().IsAssignableTo(typeof(TreasureMapChest)))
                    return Genesis.TChest;
                else if (item.GetType().IsAssignableTo(typeof(DungeonTreasureChest)))
                    return Genesis.DChest;
                else if (item.GetType().IsAssignableTo(typeof(LockableContainer)))
                    //   rat camp, pirate treasure chest, unusual containers, etc.
                    return Genesis.Chest;
            }
            return Genesis.Unknown;
        }
        private bool IsIgnoredParent(Item item)
        {
            if (item != null)
            {
                Type type = item.GetType();
                foreach (Type t in IgnoreList)
                    if (t.IsAssignableFrom(type))
                        return true;
            }
            return false;
        }
        private bool IsProperContainer(Item item)
        {
            if ((item is Corpse c && c.Owner is not PlayerMobile) || item is LockableContainer lc && lc.Locked)
                return true;
            return false;
        }
        private bool IsValuableLoot(Item item)
        {
            if (EnchantedScroll.IsValidType(item))
            {   // okay, so it's a type we support. Now lets see if it's valuable
                //  Value here relates to the items properties. Vanquishing is more valuable than Might, etc.
                //  We want to Drop the newbie type junk, and scroll'ify the good stuff.
                if (item is BaseWeapon bw && item is not BaseWand)
                {
                    if (bw.AccuracyLevel > WeaponAccuracyLevel.Surpassingly || bw.DamageLevel > WeaponDamageLevel.Might)
                        return true;

                    if (bw.MagicCharges > 0 || bw.Slayer != SlayerName.None)
                        return true;
                }
                else if (item is BaseArmor ba)
                {
                    if (ba.ProtectionLevel > ArmorProtectionLevel.Guarding || ba.DurabilityLevel > ArmorDurabilityLevel.Substantial)
                        return true;

                    if (ba.MagicCharges > 0)
                        return true;
                }
                else if (item is BaseJewel bj)
                {
                    if (bj.MagicCharges > 2)
                        return true;
                }
                else if (item is BaseWand bwnd)
                {   // always allow IDWands
                    if (bwnd.MagicCharges > 2 && bwnd.MagicEffect != MagicItemEffect.Identification)
                        return true;
                }
                else if (item is BaseClothing bc)
                {
                    if (bc.MagicCharges > 2)
                        return true;
                }
            }
            return false;
        }
        private bool IsMagicLoot(Item item)
        {
            if (EnchantedScroll.IsValidType(item))
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
            }
            return false;
        }
        public override void AddItem(Item item)
        {
            base.AddItem(item);

            if (item != null && item.Origin == Genesis.Unknown)
                if (this is LockableContainer lc && lc.Locked && Validate(item))
                    item.Origin = ChestType(this);
        }
        private bool Validate(Item item)
        {
            // don't allow recursive creation of scrolls
            if (item is EnchantedScroll)
                return false;

            if (item.GetItemBool(ItemBoolTable.NoScroll))
                return false;

            // prevents false registration if say a GM drops an item into a locked container
            if (DateTime.UtcNow - item.Created > TimeSpan.FromSeconds(1))
                return false;

            return true;
        }
        #endregion Global Enchanted Scroll Replacement and Item Categorization
        private bool CheckNonLocalStorage()
        {
            if (this.NonLocalStorage)
                return true;
            if (RootParent is Container cont)
                return cont.NonLocalStorage;
            return false;
        }

        // Override added to perform LOS check and prevent unauthorized viewing
        // of locked down containers in houses
        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InLOS(this) && !CheckNonLocalStorage())
            {

                from.SendLocalizedMessage(500237); // Target can not be seen.
                return;
            }

            int range = 0;

            if (this.RootParent is VendorBackpack)
                range = 3;
            else
                range = 2;

            if (from.AccessLevel > AccessLevel.Player || from.InRange(this.GetWorldLocation(), range))
                Open(from);
            else
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }

        public virtual void Open(Mobile from)
        {
            DisplayTo(from);
        }
        private const byte mask = 0x80;
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x81); // version

            m_Crafter.Serialize(writer);
            writer.WriteEncodedInt((int)m_Quality);
            writer.WriteEncodedInt((int)m_Resource);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            if ((Utility.PeekByte(reader) & mask) == 0)
                return; // old version

            int version = reader.ReadByte();

            switch (version)
            {
                case 0x81:
                case 0x80:
                    {
                        if (version >= 0x81)
                            m_Crafter.Deserialize(reader);
                        else
                            m_Crafter = reader.ReadMobile();

                        m_Quality = (CraftQuality)reader.ReadEncodedInt();
                        m_Resource = (CraftResource)reader.ReadEncodedInt();
                        break;
                    }
            }

            BaseCraftableItem.PatchResourceHue(this, m_Resource);
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            base.AddNameProperty(list);

            if (m_Crafter != null)
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~

            if (m_Quality == CraftQuality.Exceptional)
                list.Add(1060636); // exceptional
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            if (m_Resource >= CraftResource.OakWood && m_Resource <= CraftResource.Frostwood)
                list.Add(CraftResources.GetLocalizationNumber(m_Resource));
        }

        #region ICraftable Members

        public virtual int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            m_Quality = (CraftQuality)quality;

            if (makersMark)
                m_Crafter = from;

            Type resourceType = typeRes;

            if (resourceType == null)
                resourceType = craftItem.Resources.GetAt(0).ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            CraftContext context = craftSystem.GetContext(from);

            if (context != null && context.DoNotColor)
                Hue = 0;

            return quality;
        }

        #endregion
    }

    public class StrongBackpack : Backpack
    {
        [Constructable]
        public StrongBackpack()
        {
            Layer = Layer.Backpack;
            Weight = 3.0;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int MaxWeight { get { return 1600; } }

        public override bool CheckContentDisplay(Mobile from)
        {
            object root = this.RootParent;

            if (root is BaseCreature && ((BaseCreature)root).Controlled && ((BaseCreature)root).ControlMaster == from)
                return true;

            return base.CheckContentDisplay(from);
        }

        public StrongBackpack(Serial serial)
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
    public class StrongResourceBackpack : StrongBackpack
    {
        [Constructable]
        public StrongResourceBackpack()
        {
            Layer = Layer.Backpack;
            Weight = 3.0;
        }
        private static List<Type> ValidResources = new()
        { typeof(BaseOre), typeof(BaseIngot), typeof(Stahlrim), typeof(BaseHides), typeof(BaseLeather), typeof(BaseLog),
              typeof(BaseBoard), typeof(Cotton), typeof(Flax), typeof(Wool), typeof(SpoolOfThread), typeof(Cloth), typeof(Fish),
              typeof(BigFish),typeof(BaseGranite)
        };

        private static bool IsValidResources(Item dropped)
        {
            foreach (Type type in ValidResources)
                if (dropped.GetType().IsAssignableTo(type))
                    return true;
            return false;
        }

        //This is called when an item is dropped onto a mobile
        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            if (!IsValidResources(dropped))
            {
                if (from is BaseCreature bc && bc.ControlMaster != null)
                    bc.ControlMaster.SendMessage("You may only store resources here.");
                return false;
            }

            return base.TryDropItem(from, dropped, sendFullMessage);
        }

        //This is called when an item is placed into an open container
        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (!IsValidResources(item))
            {
                from.SendMessage("You may only store resources here.");
                return false;
            }

            return base.OnDragDropInto(from, item, p);
        }

        public StrongResourceBackpack(Serial serial)
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
    public class Backpack : BaseContainer, IDyable
    {
        public override int DefaultGumpID { get { return 0x3C; } }
        public override int DefaultDropSound { get { return 0x48; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(44, 65, 142, 94); }
        }

        [Constructable]
        public Backpack()
            : base(0x9B2)
        {
            Layer = Layer.Backpack;
            Weight = 3.0;
        }

        public Backpack(Serial serial)
            : base(serial)
        {
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted) return false;

            Hue = sender.DyedHue;

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version); // version
            switch (version)
            {
                case 1:
                    {
                        writer.Write((UInt32)m_dynamicFeatures);
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        m_dynamicFeatures = (FeatureBits)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }

        public enum FeatureBits : UInt32
        {
            None = 0x00,
            PrisonIssue = 0x01,
        };

        private FeatureBits m_dynamicFeatures = FeatureBits.None;

        [CommandProperty(AccessLevel.GameMaster)]
        public FeatureBits DynamicFeatures
        {
            get { return m_dynamicFeatures; }
            set { m_dynamicFeatures = value; }
        }

        public bool IsDynamicFeatureSet(FeatureBits fb)
        {
            if ((DynamicFeatures & fb) > 0) return true;
            else return false;
        }

        public void SetDynamicFeature(FeatureBits fb)
        {
            DynamicFeatures |= fb;
        }

        public void ClearDynamicFeature(FeatureBits fb)
        {
            DynamicFeatures &= ~fb;
        }
    }
    public class EquipmentPack : Backpack
    {
        [Constructable]
        public EquipmentPack()
            : base()
        {
            Layer = Layer.Invalid;
            Weight = 0.0;
            Hue = 33;
        }

        public EquipmentPack(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version);
            switch (version)
            {
                case 1:
                    {
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        break;
                    }
            }
        }
    }
    public class LinkPack : Backpack
    {
        private int m_Link;
        public int Link { get { return m_Link; } set { m_Link = value; } }
        [Constructable]
        public LinkPack()
            : base()
        {
            Layer = Layer.Invalid;
            Weight = 0.0;
            Hue = 66;
            m_Link = -1;
        }
        //public override Item Dupe(int amount)
        //{
        //    LinkPack new_linkpack = new LinkPack(m_Link);
        //    Utility.CopyProperties(new_linkpack, this);

        //    if (this.Items != null)
        //        foreach (var item in this.Items)
        //        {
        //            Item temp = Utility.DeepDupe(item);
        //            new_linkpack.AddItem(temp);
        //        }

        //    return base.Dupe(new_linkpack, amount);
        //}
        public LinkPack(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version);
            switch (version)
            {
                case 1:
                    {
                        writer.Write(m_Link);
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        m_Link = reader.ReadInt();
                        break;
                    }
            }
        }
    }
    public class Pouch : TrapableContainer
    {
        public override int DefaultGumpID { get { return 0x3C; } }
        public override int DefaultDropSound { get { return 0x48; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(44, 65, 142, 94); }
        }

        [Constructable]
        public Pouch()
            : base(0xE79)
        {
            Weight = 1.0;
        }

        public Pouch(Serial serial)
            : base(serial)
        {
        }
        int m_guardWarning = 0;
        Mobile m_lastBother = null;
        public override bool ExecuteTrap(Mobile from, bool bAutoReset)
        {

            if (this.RootParent is BaseGuard guard)
            {
                if (from != guard)
                    switch (m_guardWarning++)
                    {
                        case 0:
                            guard.Say("EndMusic that.");
                            m_lastBother = from;
                            break;
                        case 1:
                            guard.Say("Cut it out!");
                            m_lastBother = from;
                            break;
                        case 2:
                            guard.Say("Last warning...");
                            m_lastBother = from;
                            break;
                        case 3:
                            if (from != m_lastBother)
                            {
                                m_guardWarning = 0;
                                m_lastBother = from;
                                break;
                            }
                            TrapType saveTrapType = TrapType;
                            int saveTrapLevel = TrapLevel;
                            Mobile saveTrapper = Trapper;
                            bool saveCriminal = from.Criminal;
                            TrapType = Utility.RandomBool() ? TrapType.DartTrap : TrapType.PoisonTrap;
                            TrapLevel = 5;
                            Trapper = from;
                            bool result = base.ExecuteTrap(from, bAutoReset);
                            TrapType = saveTrapType;
                            TrapLevel = saveTrapLevel;
                            Trapper = saveTrapper;
                            from.Criminal = saveCriminal;
                            m_guardWarning = 0;
                            return result;
                        default:
                            break;
                    }
                return base.ExecuteTrap(from, bAutoReset);
            }
            else
                return base.ExecuteTrap(from, bAutoReset);
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

    public abstract class BaseBagBall : BaseContainer, IDyable
    {
        public override int DefaultGumpID { get { return 0x3D; } }
        public override int DefaultDropSound { get { return 0x48; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(29, 34, 108, 94); }
        }

        public BaseBagBall(int itemID)
            : base(itemID)
        {
            Weight = 1.0;
        }

        public BaseBagBall(Serial serial)
            : base(serial)
        {
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            Hue = sender.DyedHue;

            return true;
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

    public class SmallBagBall : BaseBagBall
    {
        [Constructable]
        public SmallBagBall()
            : base(0x2256)
        {
        }

        public SmallBagBall(Serial serial)
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

    public class LargeBagBall : BaseBagBall
    {
        [Constructable]
        public LargeBagBall()
            : base(0x2257)
        {
        }

        public LargeBagBall(Serial serial)
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

    public class Bag : BaseContainer, IDyable
    {
        public override int DefaultGumpID { get { return 0x3D; } }
        public override int DefaultDropSound { get { return 0x48; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(29, 34, 108, 94); }
        }

        [Constructable]
        public Bag()
            : base(0xE76)
        {
            Weight = 2.0;
        }

        public Bag(Serial serial)
            : base(serial)
        {
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted) return false;

            Hue = sender.DyedHue;

            return true;
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

    public class Barrel : BaseWaterContainer
    {
        public override int DefaultGumpID { get { return 0x3E; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(33, 36, 109, 112); }
        }

        public override int EmptyID { get { return 0xE77; } }
        public override int FilledID { get { return 0x154D; } }
        public override int MaxQuantity { get { return 100; } }

        [Constructable]
        public Barrel()
            : this(false)
        {
        }

        [Constructable]
        public Barrel(bool filled)
            : base(filled)
        {
            Weight = 25.0;
        }

        public Barrel(Serial serial)
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

            if (Weight == 0.0)
                Weight = 25.0;
        }
    }

    public class Keg : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x3E; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(33, 36, 109, 112); }
        }

        [Constructable]
        public Keg()
            : base(0xE7F)
        {
            Weight = 15.0;
        }

        public Keg(Serial serial)
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

    public class PicnicBasket : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x3F; } }
        public override int DefaultDropSound { get { return 0x4F; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(19, 47, 163, 76); }
        }

        [Constructable]
        public PicnicBasket()
            : base(0xE7A)
        {
            Weight = 2.0; // Stratics doesn't know weight
        }

        public PicnicBasket(Serial serial)
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

    public class Basket : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x41; } }
        public override int DefaultDropSound { get { return 0x4F; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(35, 38, 110, 78); }
        }

        [Constructable]
        public Basket()
            : base(0x990)
        {
            Weight = 1.0; // Stratics doesn't know weight
        }

        public Basket(Serial serial)
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

    public class SmallBasket : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x41; } }
        public override int DefaultDropSound { get { return 0x4F; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(35, 38, 110, 78); }
        }

        [Constructable]
        public SmallBasket()
            : base(0x9B1)
        {
            Weight = 1.0; // Stratics doesn't know weight
        }

        public SmallBasket(Serial serial)
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

    [Furniture]
    [TinkerTrapable]
    [Flipable(0x9AA, 0xE7D)]
    public class WoodenBox : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x43; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(16, 51, 168, 73); }
        }

        [Constructable]
        public WoodenBox()
            : base(0x9AA)
        {
            Weight = 4.0;
        }

        public WoodenBox(Serial serial)
            : base(serial)
        {

        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);       // version

            writer.Write((bool)true);   // Not Used - available
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:                     // Not Used - available
                    bool dummy = reader.ReadBool();
                    goto case 0;
                case 0:
                    break;
            }
        }
    }

    [Furniture]
    [TinkerTrapable]
    [Flipable(0x9A9, 0xE7E)]
    public class SmallCrate : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(20, 10, 150, 90); }
        }

        [Constructable]
        public SmallCrate()
            : base(0x9A9)
        {
            Weight = 2.0;
        }

        public SmallCrate(Serial serial)
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

            if (Weight == 4.0)
                Weight = 2.0;
        }
    }

    [Furniture]
    [TinkerTrapable]
    [Flipable(0xE3F, 0xE3E)]
    public class MediumCrate : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(20, 10, 150, 90); }
        }

        [Constructable]
        public MediumCrate()
            : base(0xE3F)
        {
            Weight = 2.0;
        }

        public MediumCrate(Serial serial)
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

            if (Weight == 6.0)
                Weight = 2.0;
        }
    }

    [Furniture]
    [TinkerTrapable]
    [FlipableAttribute(0xe3c, 0xe3d)]
    public class LargeCrate : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(20, 10, 150, 90); }
        }

        [Constructable]
        public LargeCrate()
            : base(0xE3C)
        {
            Weight = 1.0;
        }

        public LargeCrate(Serial serial)
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

            if (Weight == 8.0)
                Weight = 1.0;
        }
    }

    [FlipableAttribute(0xe3c, 0xe3d)]
    public class MovingCrate : LockableContainer
    {
        public override string DefaultName => "moving crate";
        private string m_label = null;
        new public string Label { get { return m_label; } set { m_label = value; } }
        private Item m_Property = null;
        public Item Property { get { return m_Property; } set { m_Property = value; } }
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds { get { return new Rectangle2D(20, 10, 150, 90); } }
        [Constructable]
        public MovingCrate(int max_items, int hue, Item owner)
            : base(0xE3C)
        {
            Weight = 1.0;
            Movable = false;
            MaxItems = max_items;
            Hue = hue;
            m_Property = owner;
            IsIntMapStorage = true;
        }
        public MovingCrate()
            : base(0xE3C)
        {
            Weight = 1.0;
            Movable = false;
            MaxItems = MaxItems;
            Hue = Hue;
            m_Property = Property;
            IsIntMapStorage = true;
        }
        //public override Item Dupe(int amount)
        //{   // since there is no reasonable zero parameter constructor, we need a Dupe() override
        //    MovingCrate new_crate = new MovingCrate(MaxItems, Hue, Property);
        //    List<Item> list = new List<Item>(this.Items);
        //    foreach (Item item in list)
        //        new_crate.AddItem(Utility.DeepDupe(item));
        //    return base.Dupe(new_crate, amount);
        //}
        public MovingCrate(Serial serial)
            : base(serial)
        {
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            if (Label != null)
                LabelTo(from, Label);
        }
        //This is called when an item is placed into a closed container
        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            // Adam: Only the system can add items. We add Admin account for testing purposes
            if (!(from == World.GetSystemAcct() || from == World.GetAdminAcct()))
                return false;

            return base.TryDropItem(from, dropped, sendFullMessage);
        }
        //This is called when an item is placed into an open container
        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            // Adam: Only the system can add items. We add Admin account for testing purposes
            if (!(from == World.GetSystemAcct() || from == World.GetAdminAcct()))
                return false;

            return base.OnDragDropInto(from, item, p);
        }
        public override void OnItemRemoved(Item item)
        {   // self-destruct
            base.OnItemRemoved(item);
            // [FdRestore will remove items during reconstruction, we don't want the crate to self-destruct
            if (Items.Count == 0 && GetItemBool(ItemBoolTable.BSBackedUp) == false)
                Delete();
        }
        public override void OnDelete()
        {
            base.OnDelete();
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version);

            switch (version)
            {
                case 0:
                    {
                        writer.Write(m_label);
                        writer.Write(m_Property);
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_label = reader.ReadString();
                        m_Property = reader.ReadItem();
                        break;
                    }
            }

            if (Weight == 8.0)
                Weight = 1.0;
        }
    }

    [DynamicFliping]
    [TinkerTrapable]
    [Flipable(0x9A8, 0xE80)]
    public class MetalBox : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x4B; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(16, 51, 168, 73); }
        }

        [Constructable]
        public MetalBox()
            : base(0x9A8)
        {
            Weight = 3.0; // TODO: Real weight
        }

        public MetalBox(Serial serial)
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

    [DynamicFliping]
    [TinkerTrapable]
    [Flipable(0x9AB, 0xE7C)]
    public class MetalChest : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x4A; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        [Constructable]
        public MetalChest()
            : base(0x9AB)
        {
            Weight = 25.0; // TODO: Real weight
        }

        public MetalChest(Serial serial)
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

    [DynamicFliping]
    [TinkerTrapable]
    [Flipable(0xE41, 0xE40)]
    public class MetalGoldenChest : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x42; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        [Constructable]
        public MetalGoldenChest()
            : base(0xE41)
        {
            Weight = 25.0; // TODO: Real weight
        }

        public MetalGoldenChest(Serial serial)
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
    [Furniture]
    [Flipable(0x280B, 0x280C)]
    public class PlainWoodenChest : LockableContainer
    {
        public override string DefaultName { get { return "plain wooden chest"; } }
        public override int DefaultGumpID { get { return 265; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds
        {
            get { return ItemBounds.Table[ItemID & 0x3FFF]; }
        }
        [Constructable]
        public PlainWoodenChest() : base(0x280B)
        {
        }

        public PlainWoodenChest(Serial serial) : base(serial)
        {
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

            if (version == 0 && Weight == 15)
                Weight = -1;
        }
    }
    [Furniture]
    [Flipable(0x280D, 0x280E)]
    public class OrnateWoodenChest : LockableContainer
    {
        public override string DefaultName { get { return "ornate wooden chest"; } }
        public override int DefaultGumpID { get { return 267; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds
        {
            get { return ItemBounds.Table[ItemID & 0x3FFF]; }
        }
        [Constructable]
        public OrnateWoodenChest() : base(0x280D)
        {
        }

        public OrnateWoodenChest(Serial serial) : base(serial)
        {
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

            if (version == 0 && Weight == 15)
                Weight = -1;
        }
    }
    [Furniture]
    [Flipable(0x280F, 0x2810)]
    public class GildedWoodenChest : LockableContainer
    {
        public override string DefaultName { get { return "gilded wooden chest"; } }
        public override int DefaultGumpID { get { return 266; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return ItemBounds.Table[ItemID & 0x3FFF]; }
        }

        [Constructable]
        public GildedWoodenChest() : base(0x280F)
        {
        }

        public GildedWoodenChest(Serial serial) : base(serial)
        {
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

            if (version == 0 && Weight == 15)
                Weight = -1;
        }
    }
    [Furniture]
    [Flipable(0x2811, 0x2812)]
    public class WoodenFootLocker : LockableContainer
    {
        public override string DefaultName { get { return "wooden footLocker"; } }
        public override int DefaultGumpID { get { return 0x10B; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return ItemBounds.Table[ItemID & 0x3FFF]; }
        }

        [Constructable]
        public WoodenFootLocker() : base(0x2811)
        {
            GumpID = 0x10B;
        }

        public WoodenFootLocker(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version == 0 && Weight == 15)
                Weight = -1;

            if (version < 2)
                GumpID = 0x10B;
        }
    }
    [Furniture]
    [Flipable(0x2813, 0x2814)]
    public class FinishedWoodenChest : LockableContainer
    {
        public override string DefaultName { get { return "finished wooden chest"; } }
        public override int DefaultGumpID { get { return 269; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return ItemBounds.Table[ItemID & 0x3FFF]; }
        }
        [Constructable]
        public FinishedWoodenChest() : base(0x2813)
        {
        }

        public FinishedWoodenChest(Serial serial) : base(serial)
        {
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

            if (version == 0 && Weight == 15)
                Weight = -1;
        }
    }
    [Furniture]
    [Flipable(0x2815, 0x2816)]
    public class TallCabinet : BaseContainer
    {
        public override string DefaultName { get { return "tall cabinet"; } }
        public override int DefaultGumpID { get { return 268; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds
        {
            get { return ItemBounds.Table[ItemID & 0x3FFF]; }
        }
        [Constructable]
        public TallCabinet() : base(0x2815)
        {
            Weight = 1.0;
        }

        public TallCabinet(Serial serial) : base(serial)
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

    [Furniture]
    [Flipable(0x2817, 0x2818)]
    public class ShortCabinet : BaseContainer
    {
        public override string DefaultName { get { return "short cabinet"; } }
        public override int DefaultGumpID { get { return 268; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds
        {
            get { return ItemBounds.Table[ItemID & 0x3FFF]; }
        }
        [Constructable]
        public ShortCabinet() : base(0x2817)
        {
            Weight = 1.0;
        }

        public ShortCabinet(Serial serial) : base(serial)
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

    [Furniture]
    [TinkerTrapable]
    [Flipable(0xe43, 0xe42)]
    public class WoodenChest : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x49; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        [Constructable]
        public WoodenChest()
            : base(0xe43)
        {
            Weight = 15.0; // TODO: Real weight
        }

        public WoodenChest(Serial serial)
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

    //[Furniture]
    //[Flipable(0x280D, 0x280E)]
    //public class OrnateWoodenChest : LockableContainer
    //{
    //    [Constructable]
    //    public OrnateWoodenChest()
    //        : base(0x280D)
    //    {
    //    }

    //    public OrnateWoodenChest(Serial serial)
    //        : base(serial)
    //    {
    //    }

    //    public override void Serialize(GenericWriter writer)
    //    {
    //        base.Serialize(writer);

    //        writer.Write((int)1); // version
    //    }

    //    public override void Deserialize(GenericReader reader)
    //    {
    //        base.Deserialize(reader);

    //        int version = reader.ReadInt();

    //        if (version == 0 && Weight == 15)
    //            Weight = -1;
    //    }
    //}

    public class ClosedBarrel : BaseContainer
    {
        public override int DefaultGumpID { get { return 0x3E; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(33, 36, 109, 112); }
        }

        [Constructable]
        public ClosedBarrel()
            : base(0xFAE)
        {
            Weight = 6.0;
        }

        public ClosedBarrel(Serial serial)
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

            if (Weight == 0.0)
                Weight = 6.0;
        }
    }
}