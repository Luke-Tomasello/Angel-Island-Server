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

/* Scripts/Mobiles/Vendors/GenericBuy.cs
 * ChangeLog
 *  8/14/2023, Adam(FlushInventory())
 *      Add a FlushInventory() function to clear out all inventory in a uniform manner, including unsetting IsIntMapStorage = false;
 *      We do this because IsIntMapStorage items and mobiles are logged as an exception, or at least an alert.
 *  11/11/22, Adam (public GenericBuyInfo(Type type))
 *      When creating a GBI for a pooled resource, make sure to set:
 *       m_MinAmount = m_MaxAmount = m_Amount = m_RestockAmount = 0;
 *       Otherwise, the vendor will stock these items even when the resource pool contains no such item.
 *  11/8/22, Adam: (Vendor System)
 *  Complete overhaul of the vendor system:
 * - DisplayCache:
 *   Display cache objects are now strongly typed and there is a separate list for each.
 *   I still dislike the fact it is a �Container�, but we can live with that.
 *   Display cache objects are serialized and are deleted on each server restart as the vendors rebuild the list.
 *   Display cache objects are marked as �int map storage� so Cron doesn�t delete them.
 * - ResourcePool:
 *   Properly exclude all ResourcePool interactions when resource pool is not being used. (Buy/Sell now works correctly without ResourcePool.)
 *   Normalize/automate all ResourcePool resources for purchase/sale within a vendor. If a vendor Sells X, he will Buy X. 
 *   At the top of each SB<merchant> there is a list of ResourcePool types. This list is uniformly looped over for creating all ResourcePool buy/sell entries.
 * - Standard Pricing Database
 *   No longer do we hardcode in what we believe the buy/sell price of X is. We now use a Standard Pricing Database for assigning these prices.
 *   I.e., BaseVendor.PlayerPays(typeof(Drums)), or BaseVendor.VendorPays(typeof(Drums))
 *   This database was derived from RunUO 2.6 first and added items not covered from Angel Island 5.
 *   The database was then filtered, checked, sorted and committed. 
 * - Make use of Rule Sets as opposed to hardcoding shard configurations everywhere.
 *   Exampes:
 *   if (Core.UOAI_SVR) => if (Core.RuleSets.AngelIslandRules())
 *   if (Server.Engines.ResourcePool.ResourcePool.Enabled) => if (Core.RuleSets.ResourcePoolRules())
 *   etc. In this way we centrally adjust who sell/buys what when. And using the SPD above, for what price.
 *  11/4/22, Adam (DisplayCache) 
 *      I couldn't stand it, so i had to rewrite:
 *      - old. Serialize: Would explicitly write the mobile list and implicitly write the item list (contained in the container Items.)
 *      - old. Deserialize: would read, then delete all the stuff just serialized (stupid RunUO)
 *      - old. Removed the notion of 'cached' that nobody used. (only complicated the code.)
 *      - new. Both Items & Mobiles have their own strongly typed lists.
 *      - new. Did away with the 'cache' (certain remnants still remain. but will be removed.)
 *      - new. No longer serializing and deserializing these objects.
 *      - new. Serialize now uses the opportunity to defrag the lists (and complains if defrag had to do any work as these items should not be deleted.)
 *  11/4/22, Adam (DisplayCache:Deserialize) 
 *      Note: I really hate this DisplayCache model. A Container? With a list of Mobiles?
 *          They are all Serialized, but wiped on Deserialize? Good lord.
 *      Anyway, since the world is currently 'loading', these items are not immediately
 *          deleted. They are instead queued for deletion at a future time.
 *      We will clear the list now since all the items will be deleted and to avoid confusion with
 *          having a list of deleted items. Ugh.
 *  7/30/22, Yoar
 *      Enabled the Siege x3 price hike for Mortalis
 * 11/16/21, Yoar
 *      BBS overhaul. Moved most code related to the BBS to ResourcePool.cs.
 *	2/13/11, Adam
 *		UOSP: houses are 10x the price
 *		http://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
 *	6/11/07, Pix
 *		Made the args parameter to GenericBuyInfo constructors actually get hooked up!
 *	4/23/05, Pix
 *		Fixed internalmobiles cleanup stopping animal vendors from working.
 *		Now it re-creates the mobs if they were deleted by [internalmobiles
 *	4/19/05, Pix
 *		Merged in RunUO 1.0 release code.
 *		Fixes 'vendor buy' showing just 'Deed'
 *	2/7/05, Adam
 *		Leave previous try/catch, but relax the comment and debug message
 *  07/02/05 TK
 *		Added in null sanity check and error message in main GenericBuy constructor
 *	02/07/05, Adam
 *		More patches to stop server crashes...
 *  05/02/05 TK
 *		Made "I can give you a better price with comm deed" message only show if the
 *		price actually will be better with a commodity deed
 *  02/02/05 TK
 *		Put in check for ValidFailsafe, to prevent autogeneration of things like valorite
 *  01/31/05 TK
 *		Reworked RP logic to allow normal spawning of resources if there are 0 in system
 *	01/23/05, Taran Kain
 *		Added logic to support Resource Pool.
 *  10/18/04, Froste
 *      Reworked OnRestock in order to remove OnRestockReagents
 *      Added a parameter MinValue that can be set in GenericBuyInfo
 *      If MinValue or MAxValue are not specified, they will default to 20 and 100 respectively
 *  10/13/04, Froste
 *      Added a parameter MaxValue that can be set in GenericBuyInfo
 *      Reworked OnRestockReagents() routine to allow for differing MaxValues
 *	4/29/04
 *		Modified OnRestock() routine to never retsock more than 20 of any item.
 *		Created OnRestockReagents() routine, called by NPCs that need to restock regs (since they will restock up to 100 of each item)
 *
 *  
 *      
 */

using Server.Engines.ResourcePool;
using Server.Items;
using Server.Multis.Deeds;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class GenericBuyInfo : IBuyItemInfo
    {
        private class DisplayCache : Container, IPersistence
        {
            private static DisplayCache m_Instance;
            public Item GetInstance() { return m_Instance; }
            public static DisplayCache Cache { get { return m_Instance; } }
            public static Point3D Dogtag = Utility.Dogtag.Get(typeof(DisplayCache).FullName);
            private static List<Mobile> m_Mobiles = new();
            private static List<Item> m_Items = new();
            public static List<Mobile> Mobiles { get { return m_Mobiles; } }
            public static new List<Item> Items { get { return m_Items; } }
            public DisplayCache()
                : base(0)
            {
                if (m_Instance == null || m_Instance.Deleted)
                    m_Instance = this;
                else
                    base.Delete();

                // set this so Cron doesn't try to clean it up
                this.IsIntMapStorage = true;
            }
#if false
            public void Store(Type key, object obj)
            {
                if (obj is Item item)
                {   // not quite Noah's Arc - we only want one of each object
                    if (m_Items.FindAll(i => i.GetType() == item.GetType()).Count == 0)
                        m_Items.Add(item);
                    else
                        throw new ArgumentException("object	is already stored.");
                }
                else if (obj is Mobile mobile)
                {   // not quite Noah's Arc - we only want one of each object
                    if (m_Mobiles.FindAll(m => m.GetType() == mobile.GetType()).Count == 0)
                        m_Mobiles.Add(mobile);
                    else
                        throw new ArgumentException("object	is already stored.");
                }
            }
#else
            public void Store(Type key, object obj)
            {
                if (obj is Item item)
                    m_Items.Add(item);
                else if (obj is Mobile mobile)
                    m_Mobiles.Add(mobile);
            }
#endif
            public DisplayCache(Serial serial)
                : base(serial)
            {
            }
            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                FlushInventory();

                if (m_Instance == this)
                    m_Instance = null;
            }
            private int FlushInventory()
            {
                List<Mobile> temp_mobiles = new(m_Mobiles);
                List<Item> temp_Items = new(m_Items);
                int count = m_Items.Count + m_Mobiles.Count;
                foreach (Mobile m in temp_mobiles) if (m is Mobile mob) { mob.IsIntMapStorage = false; mob.Delete(); }
                foreach (Item i in temp_Items) if (i is Item item) { item.IsIntMapStorage = false; item.Delete(); }
                m_Mobiles.Clear();
                m_Items.Clear();
                return count;
            }
            public void Defrag()
            {
                int defrag = 0;
                defrag += m_Mobiles.RemoveAll(m => m == null || m.Deleted);
                defrag += m_Items.RemoveAll(i => i == null || i.Deleted);
                if (defrag > 0)
                    Utility.ConsoleWriteLine("Info: {0} Display cache objects being deleted.", ConsoleColor.Yellow, defrag);
            }
            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)2); // version

                // Use this opportunity to defrag
                Defrag();

                // interesting aspect of the display cache.
                //  All items are serialized, but deleted on Deserialize.
                //  This is done because Vendors will recreate their lists, and to cleanup orphaned items created when a vendor is deleted or respawned.
                //  All display objects are marked 'int map storage' so that Cron will not delete them intrasession.
                writer.WriteMobileList<Mobile>(m_Mobiles);
                writer.WriteItemList<Item>(m_Items);

                if (Core.Debug)
                {   // these numbers should remain relative consistent and should only increase after initial world load if a new vendor is added that has items not already in the lists.
                    Utility.ConsoleWriteLine("DisplayCache now contains {0} mobiles.", ConsoleColor.Yellow, m_Mobiles.Count);
                    Utility.ConsoleWriteLine("DisplayCache now contains {0} items.", ConsoleColor.Yellow, m_Items.Count);
                }
            }
            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
                switch (version)
                {
                    case 2:
                        {
                            // cleanup the last run's mobiles and start again.
                            m_Mobiles = reader.ReadMobileList<Mobile>();
                            m_Items = reader.ReadItemList<Item>();
                            FlushInventory();
                            goto case 1;
                        }
                    case 1:
                        {
                            if (m_Instance == null)
                            {
                                m_Instance = this;
                                // set this so Cron doesn't try to clean it up
                                m_Instance.IsIntMapStorage = true;
                            }
                            else
                                Delete();
                            break;
                        }
                    case 0:
                        {
                            // we don't write this stuff, so no need in reading it back
                            m_Mobiles = reader.ReadMobileList<Mobile>();

                            // 11/4/22, Adam: Since the world is currently 'loading', these items are not immediately
                            //  deleted. They are instead queued for deletion at a future time.
                            // We will clear the list now since all the items will be deleted.
                            FlushInventory();

                            if (m_Instance == null)
                                m_Instance = this;
                            else
                                Delete();

                            break;
                        }
                }
            }
        }

        private Type m_Type;
        private string m_Name;
        private int m_Price;
        private int m_MaxAmount, m_MinAmount, m_Amount, m_RestockAmount;
        private int m_ItemID;
        private int m_Hue;
        private object[] m_Args;
        private object m_DisplayObject;

        public virtual int ControlSlots { get { return 0; } }
        private bool IsDeleted(object obj)
        {
            if (obj is Item)
                return (obj as Item).Deleted;
            else if (obj is Mobile)
                return (obj as Mobile).Deleted;

            return false;
        }
        public object GetDisplayObject()
        {
            DisplayCache.Cache.Defrag();

            if (m_DisplayObject != null && !IsDeleted(m_DisplayObject))
            {   // someone is actively looking at this/these items, refresh their state
                BaseVendor.ScheduleRefresh(m_DisplayObject);
                return m_DisplayObject;
            }
#if false
            if (m_DisplayObject == null || IsDeleted(m_DisplayObject))
                m_DisplayObject = LookUpCachedObject();
#endif
            //Pix: we need to test for BOTH null and if it's deleted here!
            if (m_DisplayObject == null || IsDeleted(m_DisplayObject))
            {   // first time a vendor is asked to show his list (buy) we create the object and 'Store' it below
                m_DisplayObject = GetObject();
                // store the newly created object
                DisplayCache.Cache.Store(m_Type, m_DisplayObject);
                // move this to our special place to help identify it
                DogtagIt(m_DisplayObject);
                // Send to IntMapStorage so Cron doesn't delete it
                BaseVendor.ScheduleRefresh(m_DisplayObject);
            }

            return m_DisplayObject;
        }
        private object LookUpCachedObject()
        {
            if (typeof(Mobile).IsAssignableFrom(m_Type))
                return DisplayCache.Mobiles.Find(m => m.GetType() == m_Type);
            if (typeof(Item).IsAssignableFrom(m_Type))
                return DisplayCache.Items.Find(i => i.GetType() == m_Type);

            return null;
        }
        public static class Diagnostics
        {   // diagnostic use only
            // See: Nuke
            public static List<Mobile> Mobiles { get { return DisplayCache.Mobiles; } }
            public static List<Item> Items { get { return DisplayCache.Items; } }
        }
        private void DogtagIt(object thing)
        {
            if (thing is Mobile m)
                m.MoveToWorld(DisplayCache.Dogtag, Map.Internal);
            if (thing is Item i)
                i.MoveToWorld(DisplayCache.Dogtag, Map.Internal);
        }
        public Type Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }
        public int DefaultPrice { get { return m_PriceScalar; } }
        private int m_PriceScalar;
        public int PriceScalar
        {
            get { return m_PriceScalar; }
            set { m_PriceScalar = value; }
        }
        public int Price
        {
            get
            {
                int base_price = m_Price;

                // adjust for inflation (siege)
                if (Core.RuleSets.SiegeStyleRules() && PublishInfo.Publish < 11)
                {
                    double delta = (double)(m_RestockAmount - m_Amount);
                    double percent = ((delta / m_RestockAmount) * 100.00) / 100;
                    if (percent >= .3)                                              // stock is down 30%
                        base_price += (int)((double)base_price * (percent / 4.0));  // about a 12% markup
                }

                if (m_PriceScalar != 0)
                {
                    if (base_price > 5000000)
                    {
                        long price = base_price;

                        price *= m_PriceScalar;
                        price += 50;
                        price /= 100;

                        if (price > int.MaxValue)
                            price = int.MaxValue;

                        return (int)price;
                    }

                    return (((base_price * m_PriceScalar) + 50) / 100);
                }

                return base_price;
            }
            set { m_Price = value; }
        }
        public int ItemID
        {
            get { return m_ItemID; }
            set { m_ItemID = value; }
        }
        public int Hue
        {
            get { return m_Hue; }
            set { m_Hue = value; }
        }
        public int Amount
        {
            get
            {
                // Adam: Sanity - Why is this NULL?
                if (m_Type == null)
                {
                    System.Console.WriteLine("Error with GetBuyInfo.Amount");
                    System.Console.WriteLine("In Amount.get(): (m_Type == null)");
                    return 0;
                }

                return m_Amount;
            }
            set { if (value < 0) value = 0; m_Amount = value; }
        }
        public int MinAmount
        {
            get { return m_MinAmount; }
            set { m_MinAmount = value; }
        }
        public int MaxAmount
        {
            get { return m_MaxAmount; }
            set { m_MaxAmount = value; }
        }
        public object[] Args
        {
            get { return m_Args; }
            set { m_Args = value; }
        }
        public GenericBuyInfo(Type type, int price, int amount, int min_amount, int max_amount, int itemID, int hue)
            : this(null, type, price, amount, min_amount, max_amount, itemID, hue, null)
        {
            MaxAmount = max_amount;
            MinAmount = min_amount;
        }
        public GenericBuyInfo(Type type, int price, int amount, int itemID, int hue)
            : this(null, type, price, amount, 0, 0, itemID, hue, null)
        {
        }
        public GenericBuyInfo(string name, Type type, int price, int amount, int itemID, int hue)
            : this(name, type, price, amount, 0, 0, itemID, hue, null)
        {
        }
        public GenericBuyInfo(Type type, int price, int amount, int itemID, int hue, object[] args)
            : this(null, type, price, amount, 0, 0, itemID, hue, args)
        {
        }
        public static List<int> Registry = new();
        public GenericBuyInfo(string name, Type type, int price, int amount, int min_amount, int max_amount, int itemID, int hue, object[] args)
        {
            //amount = 20;

            m_Type = type;
            if (m_Type == null)
            {
                Console.WriteLine();
                Console.WriteLine("***WARNING***: GenericBuy constructor passed null for m_Type! BAD!");
                Console.WriteLine("Item ID of offending object: {0:X}", itemID);
                Console.WriteLine("Search all files for that ID (and possibly decimal version!) and look for any that pass null to this constructor.");
                Console.WriteLine("Setting type to recall rune...");
                Console.WriteLine();
                m_Type = typeof(Server.Items.RecallRune);
            }
            else
            {
                if (!Registry.Contains(itemID))
                    Registry.Add(itemID);
            }

            m_Price = price;

            // all standard vendor fare have already been marked up for Siege in the Standard Pricing Dictionary,
            //  But house deeds go through a different system and aren't managed by SPD. We therefore 
            //  handle it here.
            if (Core.RuleSets.SiegeStyleRules())
                if (typeof(HouseDeed).IsAssignableFrom(m_Type))
                    m_Price = Core.RuleSets.SiegePriceRules(m_Type, m_Price);

            m_Amount = amount;
            m_ItemID = itemID;
            m_Hue = hue;

            m_Args = args;

            if (max_amount == 0)
                m_MaxAmount = 100;
            else
                m_MaxAmount = max_amount;

            if (min_amount == 0)
                m_MinAmount = 20;
            else
                m_MinAmount = min_amount;

            m_RestockAmount = amount;

            if (name == null)
                m_Name = (1020000 + (itemID & 0x3FFF)).ToString();
            else
                m_Name = name;
        }
        // ResourcePool handle
        public GenericBuyInfo(Type type)
        {
            if (!Core.RuleSets.ResourcePoolRules())
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("ResourcePool Error: {0} is not a pooled resource.", (type != null) ? type.FullName : "(null)");
                Utility.PopColor();
            }

            if (ResourcePool.IsPooledResource(type))
            {
                m_Type = type; // will load all props dynamically from ResourcePool

                /*  The old code here was incorrect as it would cause the vendor to stock these items
                 *  even when there was no such item in the resource pool.
                 *  (And the item otherwise should not be for sale.)
                 *  m_MinAmount = 20;
                 *  m_MaxAmount = 100;
                 *  m_Amount = m_RestockAmount = 20;
                 */

                m_MinAmount = m_MaxAmount = m_Amount = m_RestockAmount = 0;
            }
            else
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("ResourcePool Error: {0} is not a pooled resource.", (type != null) ? type.FullName : "(null)");
                Utility.PopColor();
            }
        }
        //get a new instance of an object (we just bought it)
        public virtual object GetObject()
        {
            object o = null;
            if (m_Args == null || m_Args.Length == 0)
                o = Activator.CreateInstance(m_Type);
            else
                o = Activator.CreateInstance(m_Type, m_Args);

            //if (o is Item item)
            //{
            //    o = DeLocalize(item, item.Name);
            //}

            return o;
        }
        //private static Item DeLocalize(Item item, string name)
        //{   // remove LabenNumber from item which prevents robust naming the object (client-side)
        //    Item delocalized_item = new Item(item.ItemID);
        //    Utility.CopyProperties(delocalized_item, item);
        //    delocalized_item.Name = name;
        //    item.Delete();
        //    return delocalized_item;
        //}
        //Attempt to restock with item, (return true if restock sucessful)
        public bool Restock(Item item, int amount)
        {
            return false;
            /*if ( item.GetType() == m_Type )
			{
				if ( item is BaseWeapon )
				{
					BaseWeapon weapon = (BaseWeapon)item;

					if ( weapon.Quality == WeaponQuality.Low || weapon.Quality == WeaponQuality.Exceptional || (int)weapon.DurabilityLevel > 0 || (int)weapon.DamageLevel > 0 || (int)weapon.AccuracyLevel > 0 )
						return false;
				}

				if ( item is BaseArmor )
				{
					BaseArmor armor = (BaseArmor)item;

					if ( armor.Quality == ArmorQuality.Low || armor.Quality == ArmorQuality.Exceptional || (int)armor.Durability > 0 || (int)armor.ProtectionLevel > 0 )
						return false;
				}

				m_Amount += amount;

				return true;
			}
			else
			{
				return false;
			}*/
        }
        public void OnRestock()
        {
            if (m_Amount <= 0)  // Do we half the stock or double it?
            {
                m_RestockAmount *= 2;
                m_Amount = m_RestockAmount;
            }
            else
                m_RestockAmount /= 2;

            m_Amount = m_RestockAmount;

            if (m_Amount < m_MinAmount) // never below minimum nor above maximum
                m_Amount = m_MinAmount;
            else if (m_Amount > m_MaxAmount)
                m_Amount = m_MaxAmount;

            m_RestockAmount = m_Amount; //update restock_amount
        }
    }
}