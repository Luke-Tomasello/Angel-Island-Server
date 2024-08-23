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

/* Scripts/Mobiles/Vendors/BaseVendor.cs 
 * Changelog
 *  11/10/2023, Adam (UpdateBuyInfo())
 *      remove housing from participation in faction pricing.
 *      By order of Lord British blah blah, real estate is owned by the kingdom and therefore...
 *  4/26/23, Adam (OnSee)
 *      An item now informs nearby mobiles when it is moved to world.
 *      Most mobiles don't care about this, but vendors are now smart enough to recognize a ticking bomb (explosion potion.)
 *      The vendor will then assess if an innocent will take damage from the blast and call guards accordingly.
 *  11/18/22, Adam (TCUpgrade())
 *      Remove the Test Center upgrade to 'exceptional' for weapons and armor.
 *      Maybe appropriate for Server Wars, but not Test Center
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
 *  10/24/22, Adam
 *      Remove m_Invulnerable
 *      Return true for IsInvulnerable
 *      IsInvulnerable pushed down to Mobile
 * 8/6/22, Adam
 *      Rename CanBuyerPurchase() ==> CanBuyerPurchaseFromBank()
 *      Add CanBuyerPurchaseFromBackpack()
 *      Add CanBuyerPurchaseFromUnemploymentCheck()
 *      Our previous change would disallow a purchase, even if they had enough gold in their backpack.
 *      We now check both containers (and unemployment checks) for sufficient funds.
 * 8/4/22, Adam (CanBuyerPurchase())
 * integer overflow (exploit) prevention:
 *  It's easier to simply add the overflow checking in this tiny routine than to sprinkle overflow checking in all the different places purchase prices are calculated.
 *  Here, we simply abort out as soon as the buyer can no longer afford what they have selected.
 * 1/4/22, Adam (Banker.Withdraw())
 *  For purchases > 2000 gold, replace:
 *      cont.ConsumeTotal(typeof(Gold), totalCost)
 *  with
 *      Banker.Withdraw(buyer, totalCost)
 *  this removes the classic UO (clunky) requirement that you must have gold (and not checks) in your bank.
 *  This new system pulls from both gold and checks. This system also supports 'shared bankbox' support.
 * 12/29/21, Adam (HasInventoryItem)
 *  Add a new public method to query if this vendor has a specific item.
 * 11/16/21, Yoar
 *      BBS overhaul. Moved most code related to the BBS to ResourcePool.cs.
 * 10/14/21, Yoar
 *      Bulk Order System overhaul:
 *      - Re-enabled support for bulk orders.
 *      - The bulk order system remains disabled unless BulkOrderSystem.Enabled returns true.
 *	3/12/11, adam
 *		Don't add loot to vendor backpack if server is siege
 *	05/25/09, plasma
 *		- Implemented tax properly without floating point math
 *		- Excluded BBS from the tax.
 *	02/02/09, plasma
 *		Implement OnVendvorBuy hook for faction city regions
 *		Implement taxing of goods for faction city regions
 *  10/6/07, Adam
 *      all weapons and armor are exceptional during server wars
 *  8/16/07, Adam
 *    Visualize the adding of loot InitLoot()
 *    If you override this method, be sure to add a backpack if needed.
 *  4/6/07, Adam
 *      additional Hardening to OnBuyItems() logic.
 *      Exception:
 *        System.NullReferenceException: Object reference not set to an instance of an object.
 *           at Server.Mobiles.BaseVendor.OnBuyItems(Mobile buyer, ArrayList list)
 *           at Server.Network.PacketHandlers.VendorBuyReply(NetState state, PacketReader pvSrc)
 *           at Server.Network.MessagePump.HandleReceive(NetState ns)
 *           at Server.Network.MessagePump.Slice()
 *           at Server.Core.Main(String[] args)
 *  07/18/06, Kit
 *		Added CanBeHarmful override to return false if invulnerable is set to true.
 *		Fixs problem with explosion pots hitting vendors even if unable to deal damage.
 *  07/02/06, InitOutfit/Body overrides
 *	06/28/06, Adam
 *		Logic cleanup
 *	06/28/06, Adam
 *		- Make IsInvulnerable accessible by Admin
 *		- have m_Invulnerable default to true
 *  06/27/06, Kit
 *		Changed Invunerability to bool vs function override.
 *  05/19/06, Kit
 *		Added check for control chance of handleing a pet when purchaseing. 
 *		If buyer does not have 50% control or greater of pet tell them to bugger off.
 *	12/15/05, Pix
 *		Vendors now restock from 50-70 minutes (instead of 60).
 *		Added properties to see last vendor restock time and timespan until next restock.
 *	8/30/05, Pix
 *		Now only Admins can buy things w/o gold.
 *	4/20/05, Pix
 *		Fixed ResourcePool working with new client and new RunUO1.0 BaseVendor code.
 *	4/19/05, Pix
 *		Merged in RunUO 1.0 release code.
 *		Fixes 'vendor buy' showing just 'Deed'
 *  2/24/05 TK
 *		Any resource purchase of over 100 now gets commodity deed
 *		Commodity deeds will now list at the individual price of the resource, not
 *			price * amount (VendorSellList packet limitation - ushort price)
 *	2/7/05, Adam
 *		Leave previous try/catch, but relax the comment
 *	2/7/05, Adam
 *		Emergency patch to stop from crashing server - Catch the exception
 *		This is a stop-gap solution only.. we still need to find *why* this is throwing
 *		an exception (line 542)
 *  01/31/05 - TK
 *		Removed isBunch from IBuyItem interface
 *		Added a check to prevent players creating stacks > 60000
 *		Modified RP interface to work with failsafe resource generation
 *  01/28/05 - Taran Kain
 *		Changed slightly the ResourcePool interface
 *  01/23/05 - Taran Kain
 *		Added logic in VendorSell, VendorBuy, OnBuyItems, OnSellItems to support
 *		Resource Pool.
 *	11/3/04 - Pixie
 *		Put conditional code around the new vendor update packets so only clients
 *		version 4.0.5a and later get those packets.
 *	10/29/04 - Pixie
 *		Added try/catch around D6VendorPacket for safety.
 *	10/28/04 - Pixie
 *		Added new Packet for updating vendor descriptions that were broken
 *		with the 4.0.5b patch (I think that's the right number).
 *	10/18/04, Froste
 *      Commented out OnRestockReagents() as it is no longer in use
 *	10/12/04, Froste
 *      added line 581 to temporarily deal with empty buy lists
 */

using Server.ContextMenus;
using Server.Diagnostics;
using Server.Engines.BulkOrders;
using Server.Engines.ResourcePool;
using Server.Factions;
using Server.Items;
using Server.Mobiles;
using Server.Multis.Deeds;
using Server.Multis.StaticHousing;
using Server.Network;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Server.Utility;

namespace Server.Mobiles
{
    public enum VendorShoeType
    {
        None,
        Shoes,
        Boots,
        Sandals,
        ThighBoots
    }

    public abstract class BaseVendor : BaseCreature, IVendor
    {
        #region OnSee(Item item)
        public override void OnSee(Mobile from, Item item)
        {
            base.OnSee(from, item);
            if (from != null)
                if (item is BaseExplosionPotion bep && bep.Armed)
                {   // adam: criminal action on armed if any mobiles are to be affected but me or bad guys
                    if (bep.CriminalBombing(from, item.Location, item.Map))
                        from.CriminalAction(true);

                    // Targeting will reveal, lighting on the ground is a different case
                    from.RevealingAction();

                    // nearby NPCs can call guards if 1) they are within range, and 2) they are lucky enough to have seen you
                    if (AIObject is not null)
                        AIObject.LookAround();

                    // just for fun
                    if (.25 >= Utility.RandomDouble())
                        Timer.DelayCall(TimeSpan.FromSeconds(.25), new TimerStateCallback(PanicSpeak), new object[] { null });
                }
        }
        private void PanicSpeak(object state)
        {
            switch (Utility.Random(4))
            {
                case 0: Yell("It's a BOMB!"); break;
                case 1: Yell("Run for your lives! A BOMB!"); break;
                case 2: Yell("There's a BOMB! Someone call the guards!"); break;
                case 3: Emote("*covers eyes*"); break;
            }
        }
        #endregion OnSee(Item item)
        private const int MaxSell = 500;

        protected abstract ArrayList SBInfos { get; }
        public ArrayList Inventory { get { return SBInfos; } }

        private ArrayList m_ArmorBuyInfo = new ArrayList();
        private ArrayList m_ArmorSellInfo = new ArrayList();

        private DateTime m_LastRestock;

        public override bool CanTeach { get { return Core.RuleSets.SiegeStyleRules() ? false : true; } }

        public override bool CanDeactivate { get { return true; } }

        public virtual bool IsActiveVendor { get { return true; } }
        public virtual bool IsActiveBuyer { get { return IsActiveVendor; } } // response to vendor SELL
        public virtual bool IsActiveSeller { get { return IsActiveVendor; } } // response to vendor BUY

        public virtual NpcGuild NpcGuild { get { return NpcGuild.None; } }

        public override bool ShowFameTitle { get { return false; } }

        public virtual BulkOrderSystem BulkOrderSystem { get { return null; } }

        #region Faction
        public virtual int GetPriceScalar()
        {
            Town town = Town.FromRegion(this.Region);

            if (town != null)
                return (100 + town.Tax);

            return 100;
        }

        public void UpdateBuyInfo()
        {
            int priceScalar = GetPriceScalar();

            IBuyItemInfo[] buyinfo = (IBuyItemInfo[])m_ArmorBuyInfo.ToArray(typeof(IBuyItemInfo));

            if (buyinfo != null)
            {
                foreach (IBuyItemInfo info in buyinfo)
                    // 11/10/2023, Adam: remove housing from participation in faction pricing.
                    // By order of Lord British blah blah, real estate is owned by the kingdom and therefore...
                    if (info.Type.IsAssignableTo(typeof(HouseDeed)) || info.Type.IsAssignableTo(typeof(StaticDeed)))
                        info.PriceScalar = 100;
                    else
                        info.PriceScalar = priceScalar;

            }
        }
        #endregion

        public BaseVendor(string title)
            : base(AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2)
        {

            // Shopkeepers can fight (poorly) on Siege, make sure they have SOME skill
            //	we don't know what the correct skill here is...
            if (Core.RuleSets.SiegeStyleRules() && this.Skills != null && this.Skills.Wrestling != null)
                if (this.Skills.Wrestling.Value == 0)
                    SetSkill(SkillName.Wrestling, 36.0, 68.0);

            LoadSBInfo();


            this.Title = title;

            InitBody();
            InitOutfit();

            Container pack;
            //these packs MUST exist, or the client will crash when the packets are sent
            pack = new Backpack();
            pack.Layer = Layer.ShopBuy;
            pack.Movable = false;
            pack.Visible = false;
            AddItem(pack);

            pack = new Backpack();
            pack.Layer = Layer.ShopResale;
            pack.Movable = false;
            pack.Visible = false;
            AddItem(pack);

            m_LastRestock = DateTime.UtcNow;

            // defaulted here, but can be overridden by the spawner
            IsInvulnerable = Core.RuleSets.BaseVendorInvulnerability();
        }

        public BaseVendor(Serial serial)
            : base(serial)
        {
        }
        public bool HasInventoryItem(string item)
        {
            ArrayList infos = this.Inventory;
            if (infos != null)
            {
                foreach (SBInfo info in infos)
                {
                    if (info == null) continue;

                    foreach (GenericBuyInfo binfo in info.BuyInfo)
                    {   /// name will be one of name, type.Name, or a label
                        string name = Utility.GetObjectDisplayName(binfo.GetDisplayObject(), binfo.Type);
                        // do they carry this item?
                        if (name.ToLower() == item.ToLower())
                            return true;
                    }
                }
            }

            return false;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime LastRestock
        {
            get
            {
                return m_LastRestock;
            }
            set
            {
                m_LastRestock = value;
                m_NextRestockVariant = TimeSpan.FromMinutes((Utility.RandomDouble() * 20) - 10.0);
            }
        }

        private TimeSpan m_NextRestockVariant = TimeSpan.MinValue;

        [CommandProperty(AccessLevel.Counselor)]
        public virtual TimeSpan NextRestockVariant
        {
            get
            {
                if (m_NextRestockVariant == TimeSpan.MinValue)
                {
                    m_NextRestockVariant = TimeSpan.FromMinutes((Utility.RandomDouble() * 20) - 10.0);
                }
                return m_NextRestockVariant;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public virtual TimeSpan RestockDelay
        {
            get
            {
                return (TimeSpan.FromHours(1.0) + NextRestockVariant);
            }
        }

        public Container BuyPack
        {
            get
            {
                Container pack = FindItemOnLayer(Layer.ShopBuy) as Container;

                if (pack == null)
                {
                    pack = new Backpack();
                    pack.Layer = Layer.ShopBuy;
                    pack.Visible = false;
                    AddItem(pack);
                }

                return pack;
            }
        }

        public abstract void InitSBInfo();

        protected void LoadSBInfo()
        {
            m_LastRestock = DateTime.UtcNow;
            m_NextRestockVariant = TimeSpan.FromMinutes((Utility.RandomDouble() * 20) - 10.0);

            InitSBInfo();

            m_ArmorBuyInfo.Clear();
            m_ArmorSellInfo.Clear();

            for (int i = 0; i < SBInfos.Count; i++)
            {
                SBInfo sbInfo = (SBInfo)SBInfos[i];
                m_ArmorBuyInfo.AddRange(sbInfo.BuyInfo);
                m_ArmorSellInfo.Add(sbInfo.SellInfo);
            }
        }
        public virtual bool GetGender()
        {
            return Utility.RandomBool();
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            SpeechHue = Utility.RandomSpeechHue();
            Hue = Utility.RandomSkinHue();

            if (Female = GetGender())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }

        public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness, bool ignoreOurDeadness)
        {
            if (IsInvulnerable)
                return false;

            return base.CanBeHarmful(target, message, ignoreOurBlessedness, ignoreOurDeadness);
        }
        public virtual int GetRandomHue()
        {
            switch (Utility.Random(5))
            {
                default:
                case 0: return Utility.RandomBlueHue();
                case 1: return Utility.RandomGreenHue();
                case 2: return Utility.RandomRedHue();
                case 3: return Utility.RandomYellowHue();
                case 4: return Utility.RandomNeutralHue();
            }
        }

        public virtual int GetShoeHue()
        {
            if (0.1 > Utility.RandomDouble())
                return 0;

            return Utility.RandomNeutralHue();
        }

        public virtual VendorShoeType ShoeType
        {
            get { return VendorShoeType.Shoes; }
        }

        public virtual int RandomBrightHue()
        {
            return Utility.RandomBrightHue();
        }

        public virtual void CheckMorph()
        {
            if (CheckGargoyle())
                return;

            CheckNecromancer();
        }

        public virtual bool CheckGargoyle()
        {
            Map map = this.Map;

            if (map != Map.Ilshenar)
                return false;

            if (Region.Name != "Gargoyle City")
                return false;

            if (Body != 0x2F6 || (Hue & 0x8000) == 0)
                TurnToGargoyle();

            return true;
        }

        public virtual bool CheckNecromancer()
        {
            Map map = this.Map;

            if (map != Map.Malas)
                return false;

            if (Region.Name != "Umbra")
                return false;

            if (Hue != 0x83E8)
                TurnToNecromancer();

            return true;
        }

        protected override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            CheckMorph();
        }

        protected override void OnMapChange(Map oldMap)
        {
            base.OnMapChange(oldMap);

            CheckMorph();
        }

        public virtual int GetRandomNecromancerHue()
        {
            switch (Utility.Random(20))
            {
                case 0: return 0;
                case 1: return 0x4E9;
                default: return Utility.RandomList(0x485, 0x497);
            }
        }

        public virtual void TurnToNecromancer()
        {
            ArrayList items = new ArrayList(this.Items);

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = (Item)items[i];

                if (item is Hair || item is Beard)
                    item.Hue = 0;
                else if (item is BaseClothing || item is BaseWeapon || item is BaseArmor || item is BaseTool)
                    item.Hue = GetRandomNecromancerHue();
            }

            Hue = 0x83E8;
        }

        public virtual void TurnToGargoyle()
        {
            ArrayList items = new ArrayList(this.Items);

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = (Item)items[i];

                if (item is BaseClothing || item is Hair || item is Beard)
                    item.Delete();
            }

            Body = 0x2F6;
            Hue = RandomBrightHue() | 0x8000;
            Name = NameList.RandomName("gargoyle vendor");

            CapitalizeTitle();
        }

        public virtual void CapitalizeTitle()
        {
            string title = this.Title;

            if (title == null)
                return;

            string[] split = title.Split(' ');

            for (int i = 0; i < split.Length; ++i)
            {
                if (Insensitive.Equals(split[i], "the"))
                    continue;

                if (split[i].Length > 1)
                    split[i] = Char.ToUpper(split[i][0]) + split[i].Substring(1);
                else if (split[i].Length > 0)
                    split[i] = Char.ToUpper(split[i][0]).ToString();
            }

            this.Title = string.Join(" ", split);
        }

        public virtual int GetHairHue()
        {
            return Utility.RandomHairHue();
        }

        /*
		private static int[] m_SandalHues = new int[]
			{
				0x489, 0x47F, 0x482,
				0x47E, 0x48F, 0x494,
				0x484, 0x497
			};

		private static Item CreateSandals(int type)
		{
			return new Sandals(m_SandalHues[Utility.Random(m_SandalHues.Length)]);
		}*/

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            switch (Utility.Random(3))
            {
                case 0: AddItem(new FancyShirt(GetRandomHue())); break;
                case 1: AddItem(new Doublet(GetRandomHue())); break;
                case 2: AddItem(new Shirt(GetRandomHue())); break;
            }

            /* Publish 4
			 * Shopkeeper Changes
			 * NPC shopkeepers will no longer have colored sandals. Evil NPC Mages will carry these items.
			 * adam: I'm unsure of the hue, but sice this was likely the hue 'moved' to evil mage lords in publish 4, we will assume these are the hues used here
			 * http://forums.uosecondage.com/viewtopic.php?f=8&t=22266
			 * runuo.com/community/threads/evil-mage-hues.91540/
			 */
            int sandal_hue = (PublishInfo.Publish >= 4) ? GetShoeHue() : Utility.RandomBool() ? Utility.RandomRedHue() : Utility.RandomBlueHue();

            switch (ShoeType)
            {
                case VendorShoeType.Shoes: AddItem(new Shoes(GetShoeHue())); break;
                case VendorShoeType.Boots: AddItem(new Boots(GetShoeHue())); break;
                case VendorShoeType.Sandals: AddItem(new Sandals(sandal_hue)); break;
                case VendorShoeType.ThighBoots: AddItem(new ThighBoots(GetShoeHue())); break;
            }

            int hairHue = GetHairHue();

            if (Female)
            {
                switch (Utility.Random(6))
                {
                    case 0: AddItem(new ShortPants(GetRandomHue())); break;
                    case 1:
                    case 2: AddItem(new Kilt(GetRandomHue())); break;
                    case 3:
                    case 4:
                    case 5: AddItem(new Skirt(GetRandomHue())); break;
                }

                switch (Utility.Random(9))
                {
                    case 0: AddItem(new Afro(hairHue)); break;
                    case 1: AddItem(new KrisnaHair(hairHue)); break;
                    case 2: AddItem(new PageboyHair(hairHue)); break;
                    case 3: AddItem(new PonyTail(hairHue)); break;
                    case 4: AddItem(new ReceedingHair(hairHue)); break;
                    case 5: AddItem(new TwoPigTails(hairHue)); break;
                    case 6: AddItem(new ShortHair(hairHue)); break;
                    case 7: AddItem(new LongHair(hairHue)); break;
                    case 8: AddItem(new BunsHair(hairHue)); break;
                }
            }
            else
            {
                switch (Utility.Random(2))
                {
                    case 0: AddItem(new LongPants(GetRandomHue())); break;
                    case 1: AddItem(new ShortPants(GetRandomHue())); break;
                }

                switch (Utility.Random(8))
                {
                    case 0: AddItem(new Afro(hairHue)); break;
                    case 1: AddItem(new KrisnaHair(hairHue)); break;
                    case 2: AddItem(new PageboyHair(hairHue)); break;
                    case 3: AddItem(new PonyTail(hairHue)); break;
                    case 4: AddItem(new ReceedingHair(hairHue)); break;
                    case 5: AddItem(new TwoPigTails(hairHue)); break;
                    case 6: AddItem(new ShortHair(hairHue)); break;
                    case 7: AddItem(new LongHair(hairHue)); break;
                }

                switch (Utility.Random(5))
                {
                    case 0: AddItem(new LongBeard(hairHue)); break;
                    case 1: AddItem(new MediumLongBeard(hairHue)); break;
                    case 2: AddItem(new Vandyke(hairHue)); break;
                    case 3: AddItem(new Mustache(hairHue)); break;
                    case 4: AddItem(new Goatee(hairHue)); break;
                }
            }
        }

        public override bool CheckPoisonImmunity(Mobile from, Poison poison)
        {
            // Publish 5
            // Shopkeepers may no longer be poisoned
            // http://www.uoguide.com/Publish_5
            if (PublishInfo.Publish >= 5)
                return true;
            else
                return false;
        }

        public virtual void InitLoot()
        {
            PackGold(100, 200);
        }

        public virtual void Restock()
        {
            try
            {
                m_LastRestock = DateTime.UtcNow;
                m_NextRestockVariant = TimeSpan.FromMinutes((Utility.RandomDouble() * 20) - 10.0);

                IBuyItemInfo[] buyInfo = this.GetBuyInfo();

                foreach (IBuyItemInfo bii in buyInfo)
                    bii.OnRestock();
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Error with Restock.GetBuyInfo()");
                System.Console.WriteLine(exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }

        private static TimeSpan InventoryDecayTime = TimeSpan.FromHours(1.0);

        public virtual void VendorBuy(Mobile from)
        {
            try
            {
                // adam: add new sanity checks
                if (from == null || from.NetState == null)
                    return;

                if (!IsActiveSeller)
                    return;

                if (!from.CheckAlive())
                    return;

                if (!CheckVendorAccess(from))
                {
                    Say(501522); // I shall not treat with scum like thee!
                    return;
                }

                if (DateTime.UtcNow - m_LastRestock > RestockDelay)
                    Restock();

                UpdateBuyInfo();

                int count = 0;
                List<BuyItemState> list;

                // Adam: Catch the exception
                IBuyItemInfo[] buyInfo = null;
                try
                {
                    buyInfo = this.GetBuyInfo();
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("Error with GetBuyInfo() :: Please send output to Taran:");
                    System.Console.WriteLine(exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                    // if buyInfo is null, we can't continue
                    throw new ApplicationException("Error with GetBuyInfo()");
                }

                IShopSellInfo[] sellInfo = this.GetSellInfo();

                list = new List<BuyItemState>(buyInfo.Length);
                Container cont = this.BuyPack;

                ArrayList opls = new ArrayList();
                try
                {
                    for (int idx = 0; idx < buyInfo.Length; idx++)
                    {
                        IBuyItemInfo buyItem = (IBuyItemInfo)buyInfo[idx];

                        if (Core.RuleSets.ResourcePoolRules())
                        {
                            // NOTE: Call ResourcePool.AddBuyInfo before checking buyItem.Amount
                            if (list.Count >= 250 || ResourcePool.AddBuyInfo(this, buyItem, cont, ref count, ref list, ref opls) || buyItem.Amount <= 0)
                                continue;
                        }
                        else
                            if (list.Count >= 250 || buyItem.Amount <= 0)
                            continue;

                        // NOTE: Only GBI supported; if you use another implementation of IBuyItemInfo, this will crash
                        GenericBuyInfo gbi = (GenericBuyInfo)buyItem;
                        IEntity disp = gbi.GetDisplayObject() as IEntity;

                        int price = gbi.Price;

                        #region city tax
#if old
                    //plasma: Implement city tax here												
                    KinCityRegion kcr = KinCityRegion.GetKinCityAt(this);
                    if (kcr != null && kcr.CityTaxRate > 0.0)
                    {
                        double tax = buyItem.Price * kcr.CityTaxRate;
                        price += (int)Math.Floor(tax);
                    }
#endif
                        #endregion city tax

                        list.Add(new BuyItemState(buyItem.Name,
                            cont.Serial,
                            disp == null ? (Serial)0x7FC0FFEE : disp.Serial,
                            price,
                            buyItem.Amount,
                            buyItem.ItemID,
                            buyItem.Hue));
                        count++;

                        if (disp is Item)
                            opls.Add((disp as Item).PropertyList);
                        else if (disp is Mobile)
                            opls.Add((disp as Mobile).PropertyList);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                List<Item> playerItems = cont.Items;

                for (int i = playerItems.Count - 1; i >= 0; --i)
                {
                    if (i >= playerItems.Count)
                        continue;

                    Item item = (Item)playerItems[i];

                    if ((item.LastMoved + InventoryDecayTime) <= DateTime.UtcNow)
                        item.Delete();
                }

                for (int i = 0; i < playerItems.Count; ++i)
                {
                    Item item = (Item)playerItems[i];

                    int price = 0;
                    string name = null;

                    foreach (IShopSellInfo ssi in sellInfo)
                    {
                        if (ssi.IsSellable(item))
                        {
                            price = ssi.GetBuyPriceFor(item);
                            name = ssi.GetNameFor(item);
                            break;
                        }
                    }

                    if (name != null && list.Count < 250)
                    {

                        list.Add(new BuyItemState(name, cont.Serial, item.Serial, price, item.Amount, item.ItemID, item.Hue));
                        count++;

                        opls.Add(item.PropertyList);
                    }
                }

                //one (not all) of the packets uses a byte to describe number of items in the list.  Osi = dumb.
                //if ( list.Count > 255 )
                //	Console.WriteLine( "Vendor Warning: Vendor {0} has more than 255 buy items, may cause client errors!", this );

                if (list.Count > 0)
                {
                    list.Sort(new BuyItemStateComparer());

                    SendPacksTo(from);

                    if (from.NetState == null)
                        return;

                    if (from.NetState.ContainerGridLines /*from.NetState.IsPost6017*/)
                        from.Send(new VendorBuyContent6017(list));
                    else
                        from.Send(new VendorBuyContent(list));

                    from.Send(new VendorBuyList(this, list));
                    from.Send(new DisplayBuyList(this));
                    from.Send(new MobileStatusExtended(from));//make sure their gold amount is sent

                    for (int i = 0; i < opls.Count; ++i)
                        from.Send(opls[i] as Packet);

                    SayTo(from, 500186); // Greetings.  Have a look around.
                }
                else
                    SayTo(from, "I'm all out of stock. Please come back later."); // Added to deal with an empty buy list
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }
        }

        public virtual void SendPacksTo(Mobile from)
        {
            Item pack = FindItemOnLayer(Layer.ShopBuy);

            if (pack == null)
            {
                pack = new Backpack();
                pack.Layer = Layer.ShopBuy;
                pack.Movable = false;
                pack.Visible = false;
                AddItem(pack);
            }

            from.Send(new EquipUpdate(pack));

            pack = FindItemOnLayer(Layer.ShopSell);

            if (pack != null)
                from.Send(new EquipUpdate(pack));

            pack = FindItemOnLayer(Layer.ShopResale);

            if (pack == null)
            {
                pack = new Backpack();
                pack.Layer = Layer.ShopResale;
                pack.Movable = false;
                pack.Visible = false;
                AddItem(pack);
            }

            from.Send(new EquipUpdate(pack));
        }

        public virtual void VendorSell(Mobile from)
        {
            if (!IsActiveBuyer)
                return;

            if (!from.CheckAlive())
                return;

            if (!CheckVendorAccess(from))
            {
                Say(501522); // I shall not treat with scum like thee!
                return;
            }

            Container pack = from.Backpack;

            if (pack != null)
            {
                IShopSellInfo[] info = GetSellInfo();

                Dictionary<Item, SellItemState> table = new Dictionary<Item, SellItemState>();

                foreach (IShopSellInfo ssi in info)
                {
                    Item[] items = pack.FindItemsByType(ssi.Types);

                    foreach (Item item in items)
                    {
                        if (item is Container && ((Container)item).Items.Count != 0)
                            continue;

                        if (item.IsStandardLoot() && item.Movable && ssi.IsSellable(item))
                        {
                            if (Core.RuleSets.ResourcePoolRules())
                                if (ResourcePool.AddSellInfo(this, item, ref table))
                                    continue;

                            table[item] = new SellItemState(item, ssi.GetSellPriceFor(item), ssi.GetNameFor(item));
                        }
                    }
                }

                if (table.Count > 0)
                {
                    SendPacksTo(from);

                    from.Send(new VendorSellList(this, table.Values));
                }
                else
                {
                    Say(true, "You have nothing I would be interested in.");
                }
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is BaseBOD && BulkOrderSystem != null && BulkOrderSystem.IsEnabled() && BulkOrderSystem.SupportsBulkOrders(from))
                return BulkOrderSystem.HandleBulkOrderDropped(from, this, (BaseBOD)dropped);

            return base.OnDragDrop(from, dropped);
        }

        private GenericBuyInfo LookupDisplayObject(object obj)
        {
            try
            {
                if (Core.RuleSets.ResourcePoolRules())
                {
                    GenericBuyInfo rpgbi = ResourcePool.LookupBuyInfo(obj);

                    if (rpgbi != null)
                        return rpgbi;
                }

                IBuyItemInfo[] buyInfo = this.GetBuyInfo();

                for (int i = 0; i < buyInfo.Length; ++i)
                {
                    GenericBuyInfo gbi = buyInfo[i] as GenericBuyInfo;
                    if (gbi.GetDisplayObject() == obj)
                        return gbi;
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Error with LookupDisplayObject.GetBuyInfo()");
                System.Console.WriteLine(exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }

            return null;
        }
        bool ProcessSinglePurchase(BuyItemResponse buy, IBuyItemInfo bii, ArrayList validBuy, ref int controlSlots, ref bool fullPurchase, ref int totalCost)
        {
            int amount = buy.Amount;

            if (amount > bii.Amount)
                amount = bii.Amount;

            if (amount <= 0)
                return false;

            int slots = bii.ControlSlots * amount;

            if (controlSlots >= slots)
            {
                controlSlots -= slots;
            }
            else
            {
                fullPurchase = false;
                return false;
            }

            totalCost += bii.Price * amount;

            validBuy.Add(buy);
            return true;
        }

        private void ProcessValidPurchase(int amount, IBuyItemInfo bii, Mobile buyer, Container cont)
        {
            if (amount > bii.Amount)
                amount = bii.Amount;

            if (amount < 1)
                return;

            bool asCommodityDeed = false;

            if (Core.RuleSets.ResourcePoolRules() && ResourcePool.OnVendorBuy(this, buyer, ref bii, ref amount))
            {
                if (amount <= 0)
                    return;

                if (amount >= 100)
                    asCommodityDeed = true;
            }
            else
            {
                bii.Amount -= amount;
            }

            //get a new instance of an object (we just bought it)
            object o = bii.GetObject();

            if (o is Item)
            {
                Item item = (Item)o;

                if (item.Stackable)
                {
                    item.Amount = amount;

                    if (asCommodityDeed)
                        item = new CommodityDeed(item);

                    if (cont == null || !cont.TryDropItem(buyer, item, false))
                    {
                        StoreBought(o);
                        TCUpgrade(o);
                        item.MoveToWorld(buyer.Location, buyer.Map);
                        OnPurchase(buyer, item);
                    }
                }
                else
                {
                    item.Amount = 1;

                    if (cont == null || !cont.TryDropItem(buyer, item, false))
                    {
                        StoreBought(o);
                        TCUpgrade(o);
                        item.MoveToWorld(buyer.Location, buyer.Map);
                        OnPurchase(buyer, item);
                    }

                    for (int i = 1; i < amount; i++)
                    {
                        item = bii.GetObject() as Item;

                        if (item != null)
                        {
                            item.Amount = 1;

                            if (cont == null || !cont.TryDropItem(buyer, item, false))
                            {
                                StoreBought(o);
                                TCUpgrade(o);
                                item.MoveToWorld(buyer.Location, buyer.Map);
                                OnPurchase(buyer, item);
                            }
                        }
                    }
                }
            }
            else if (o is Mobile)
            {
                Mobile m = (Mobile)o;

                m.Direction = (Direction)Utility.Random(8);
                m.MoveToWorld(buyer.Location, buyer.Map);
                m.PlaySound(m.GetIdleSound());

                if (m is BaseCreature)
                    ((BaseCreature)m).SetControlMaster(buyer);

                for (int i = 1; i < amount; ++i)
                {
                    m = bii.GetObject() as Mobile;

                    if (m != null)
                    {
                        m.Direction = (Direction)Utility.Random(8);
                        m.MoveToWorld(buyer.Location, buyer.Map);

                        if (m is BaseCreature)
                            ((BaseCreature)m).SetControlMaster(buyer);
                    }
                }
            }
        }

        public virtual void OnPurchase(Mobile from, Item item)
        {
        }

        private static void TCUpgrade(object o)
        {
            // turning this off for now. Test Center should be an accurate representation of the shard
            //  it represents. If we want this functionality for Server Wars, then we need a special condition for that.
#if false
            if (o is Item item)
            {
                // all weapons and armor are exceptional during server wars
                if (Server.Misc.TestCenter.Enabled == true && !Core.UOBETA_CFG)
                {
                    if (item is BaseArmor)
                    {
                        (item as BaseArmor).Quality = ArmorQuality.Exceptional;
                        (item as BaseArmor).DurabilityLevel = ArmorDurabilityLevel.Fortified;
                        (item as BaseArmor).Identified = true;
                    }
                    if (item is BaseWeapon)
                    {
                        (item as BaseWeapon).Quality = WeaponQuality.Exceptional;
                        (item as BaseWeapon).DurabilityLevel = WeaponDurabilityLevel.Fortified;
                        (item as BaseWeapon).Identified = true;
                    }
                }
            }
#endif
        }
        private static void StoreBought(object o)
        {
            // all items bought in a store (from an NPC) only yield 1 ingot when smelted
            if (o is Item i)
                i.StoreBought = true;
        }
        public static void ScheduleRefresh(object o)
        {   // make sure Cron doesn't delete these
            //  It should be noted however, Deserialization does delete these objects,
            //  we just don't want Cron cleaning them up intrasession.
            if (o is Mobile m)
                m.IsIntMapStorage = true;
            else if (o is Item i)
                i.IsIntMapStorage = true;
        }
        public virtual bool OnBuyItems(Mobile buyer, List<BuyItemResponse> list)
        {
            // adam: additional Hardening.
            try
            {
                // adam: add new sanity checks
                if (buyer == null || buyer.NetState == null || list == null)
                    return false;

                if (!IsActiveSeller)
                    return false;

                if (!buyer.CheckAlive())
                    return false;

                // added back for factions vendor checks
                if (!CheckVendorAccess(buyer))
                {
                    Say(501522); // I shall not treat with scum like thee!
                    return false;
                }

                UpdateBuyInfo();

                IBuyItemInfo[] buyInfo = this.GetBuyInfo();
                IShopSellInfo[] info = GetSellInfo();
                int totalCost = 0;
                ArrayList validBuy = new ArrayList(list.Count);
                Container cont;
                bool bought = false;
                bool fromBank = false;
                bool fullPurchase = true;
                int controlSlots = buyer.FollowersMax - buyer.FollowerCount;
                //int totalTax = 0;
                //KinCityRegion kcr = KinCityRegion.GetKinCityAt(this);

                // early exit if the buyer cannot afford what they have selected. Avoids overflow checks sprinkled throught the code. 
                if (CanAfford(buyer, list) == false)
                {
                    SayTo(buyer, 500192);//Begging thy pardon, but thou casnt afford that.
                    return false;
                }

                // okay, loop over all list items and total purchases.
                foreach (BuyItemResponse buy in list)
                {
                    Serial ser = buy.Serial;
                    int amount = buy.Amount;

                    if (ser.IsItem)
                    {
                        Item item = World.FindItem(ser);

                        if (item == null)
                            continue;

                        GenericBuyInfo gbi = LookupDisplayObject(item);

                        if (gbi != null)
                        {
                            if (ProcessSinglePurchase(buy, gbi, validBuy, ref controlSlots, ref fullPurchase, ref totalCost))
                            {
#if old
                                //plasma: after processing the single purchase, apply tax to each item if this isn't a bbs sale
                                int price = gbi.Price;
                                //plasma: Implement city tax here, but only if the pool is empty (this prevents tax being applied to BBS sales)
                                if (ResourcePool.GetTotalAmount(gbi.Type) == 0)
                                {
                                    if (kcr != null && kcr.CityTaxRate > 0.0)
									{
										double tax = gbi.Price * kcr.CityTaxRate;
										tax = Math.Floor(tax);
										tax = tax * buy.Amount;
										totalCost += (int)tax;
										totalTax += (int)tax;
									}
                                }
#endif
                            }

                        }
                        else if (item.RootParent == this)
                        {
                            if (amount > item.Amount)
                                amount = item.Amount;

                            if (amount <= 0)
                                continue;

                            foreach (IShopSellInfo ssi in info)
                            {
                                if (ssi.IsSellable(item))
                                {
                                    if (ssi.IsResellable(item))
                                    {
                                        totalCost += ssi.GetBuyPriceFor(item) * amount;
                                        validBuy.Add(buy);
                                        break;
                                    }
                                }
                            }

                        }
                    }
                    else if (ser.IsMobile)
                    {
                        Mobile mob = World.FindMobile(ser);

                        if (mob == null)
                            continue;


                        if (mob is BaseCreature)
                        {

                            double chance = ((BaseCreature)mob).GetControlChance(buyer);
                            if (chance <= 0.50) //require 50% control or better
                            {
                                SayTo(buyer, true, "You don't look like you would make a fitting owner for this fine animal.");
                                return false;
                            }

                        }

                        GenericBuyInfo gbi = LookupDisplayObject(mob);

                        if (gbi != null)
                        {
                            if (ProcessSinglePurchase(buy, gbi, validBuy, ref controlSlots, ref fullPurchase, ref totalCost))
                            {
                                //plasma: after processing the single purchase, apply tax to each item
                                int price = gbi.Price;

#if old
                                //plasma: Implement city tax here, but only if the pool is empty (this prevents tax being applied to BBS sales)
                                if (ResourcePool.GetTotalAmount(gbi.Type) == 0)
								{
									if (kcr != null && kcr.CityTaxRate > 0.0)
									{
										double tax = gbi.Price * kcr.CityTaxRate;
										tax = Math.Floor(tax);
										tax = tax * gbi.Amount;
										totalCost += (int)tax;
										totalTax += (int)tax;
									}
								}
#endif
                            }
                        }
                    }
                }//foreach

                if (fullPurchase && validBuy.Count == 0)
                {
                    SayTo(buyer, 500190); // Thou hast bought nothing!
                }
                else if (validBuy.Count == 0)
                {
                    SayTo(buyer, 500187); // Your order cannot be fulfilled, please try again.
                }

                if (validBuy.Count == 0)
                {
                    return false;
                }

                // remove the special privileges for staff .. makes testing difficult
                bought = (false && buyer.AccessLevel >= AccessLevel.Administrator);//Pix: I decided to bump this up to Admin... staff shouldn't be buying things anyways

                cont = buyer.Backpack;
                if (!bought && cont != null)
                {   // try to get it from their backpack first.
                    // Players cannot make a ConsignmentPurchase with an UnemploymentCheck though.
                    if ((!ResourcePool.IsConsignmentPurchase(list) && ConsumeTotal(buyer, cont, typeof(UnemploymentCheck), totalCost)) || cont.ConsumeTotal(typeof(Gold), totalCost))
                    {
                        bought = true;
                    }
                    else if (totalCost < 2000)
                    {
                        if (HasUnemploymentCheck(buyer, cont, typeof(UnemploymentCheck), totalCost))
                            SayTo(buyer, "The seller of these commodities has instructed me to only accept a cash payment.");
                        else
                            SayTo(buyer, 500192);//Begging thy pardon, but thou casnt afford that.
                    }
                }
                // we don't need a case here for UnemploymentChecks since they are always less then 2000 gold
                if (!bought && totalCost >= 2000)
                {   // get it from their bank box
                    cont = buyer.BankBox;
                    // 1/4/22, Adam: Use the new banking system for large purchases.
                    //  this includes 'shared bankbox' support
                    //if (cont != null && cont.ConsumeTotal(typeof(Gold), totalCost))
                    if (cont != null && Banker.CombinedWithdrawFromAllEnrolled(buyer, totalCost))
                    {
                        bought = true;
                        fromBank = true;
                    }
                    else
                    {
                        SayTo(buyer, 500191); //Begging thy pardon, but thy bank account lacks these funds.
                    }
                }

                if (!bought)
                {
                    return false;
                }
                else
                {

                    buyer.PlaySound(0x32);
                }

                cont = buyer.Backpack;
                if (cont == null)
                {
                    cont = buyer.BankBox;
                }

                foreach (BuyItemResponse buy in validBuy)
                {
                    Serial ser = buy.Serial;
                    int amount = buy.Amount;

                    if (amount < 1)
                        continue;

                    if (ser.IsItem)
                    {
                        Item item = World.FindItem(ser);

                        if (item == null)
                            continue;

                        // record this item purchase for our gold sink tracking
                        Engines.DataRecorder.DataRecorder.GoldSink(buyer, item, amount);

                        GenericBuyInfo gbi = LookupDisplayObject(item);

                        if (gbi != null)
                        {
                            ProcessValidPurchase(amount, gbi, buyer, cont);
                        }
                        else
                        {
                            if (amount > item.Amount)
                                amount = item.Amount;

                            foreach (IShopSellInfo ssi in info)
                            {
                                if (ssi.IsSellable(item))
                                {
                                    if (ssi.IsResellable(item))
                                    {
                                        Item buyItem;
                                        if (amount >= item.Amount)
                                        {
                                            buyItem = item;
                                        }
                                        else
                                        {
                                            buyItem = item.Dupe(amount);
                                            item.Amount -= amount;
                                        }

                                        if (cont == null || !cont.TryDropItem(buyer, buyItem, false))
                                            buyItem.MoveToWorld(buyer.Location, buyer.Map);

                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (ser.IsMobile)
                    {
                        Mobile mob = World.FindMobile(ser);

                        if (mob == null)
                            continue;

                        GenericBuyInfo gbi = LookupDisplayObject(mob);

                        if (gbi != null)
                            ProcessValidPurchase(amount, gbi, buyer, cont);
                    }

                    /*if ( ser >= 0 && ser <= buyInfo.Length )
					{
							IBuyItemInfo bii = buyInfo[ser];

					}
					else
					{
							Item item = World.FindItem( buy.Serial );

							if ( item == null )
									continue;

							if ( amount > item.Amount )
									amount = item.Amount;

							foreach( IShopSellInfo ssi in info )
							{
									if ( ssi.IsSellable( item ) )
									{
											if ( ssi.IsResellable( item ) )
											{
													Item buyItem;
													if ( amount >= item.Amount )
													{
															buyItem = item;
													}
													else
													{
															buyItem = item.Dupe( amount );
															item.Amount -= amount;
													}

													if ( cont == null || !cont.TryDropItem( buyer, buyItem, false ) )
															buyItem.MoveToWorld( buyer.Location, buyer.Map );

													break;
											}
									}
							}
					}*/
                }//foreach

                if (fullPurchase)
                {   // turn off special privileges for staff .. makes testing difficult.
                    if (false && buyer.AccessLevel >= AccessLevel.Administrator)//Pix: I decided to bump this up to Admin... staff shouldn't be buying things anyways
                        SayTo(buyer, true, "I would not presume to charge thee anything.  Here are the goods you requested.");
                    else if (fromBank)
                        SayTo(buyer, true, "The total of thy purchase is {0} gold, which has been withdrawn from your bank account.  My thanks for the patronage.", totalCost.ToString("N0"));
                    else
                        SayTo(buyer, true, "The total of thy purchase is {0} gold.  My thanks for the patronage.", totalCost.ToString("N0"));
                }
                else
                {   // turn off special privileges for staff .. makes testing difficult.
                    if (false && buyer.AccessLevel >= AccessLevel.Administrator)//Pix: I decided to bump this up to Admin... staff shouldn't be buying things anyways
                        SayTo(buyer, true, "I would not presume to charge thee anything.  Unfortunately, I could not sell you all the goods you requested.");
                    else if (fromBank)
                        SayTo(buyer, true, "The total of thy purchase is {0} gold, which has been withdrawn from your bank account.  My thanks for the patronage.  Unfortunately, I could not sell you all the goods you requested.", totalCost.ToString("N0"));
                    else
                        SayTo(buyer, true, "The total of thy purchase is {0} gold.  My thanks for the patronage.  Unfortunately, I could not sell you all the goods you requested.", totalCost.ToString("N0"));
                }
                //plasma:  Process the sale here if this is a faction region
                //if (kcr != null) kcr.OnVendorBuy(buyer, totalTax);
                return true;
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

            return false;
        }
        private bool CanBuyerPurchaseFromBank(Mobile buyer, List<BuyItemResponse> list)
        {
            int buyerGold = Banker.GetAccessibleBalance(buyer);
            return CanBuy(list, buyerGold);
        }
        private bool CanBuyerPurchaseFromBackpack(Mobile buyer, List<BuyItemResponse> list)
        {
            if (buyer.Backpack == null)
                return false;
            int buyerGold = buyer.Backpack.GetAmount(typeof(Gold));
            return CanBuy(list, buyerGold);
        }
        private bool CanBuyerPurchaseFromUnemploymentCheck(Mobile buyer, List<BuyItemResponse> list)
        {
            if (buyer.Backpack == null)
                return false;
            int buyerGold = UnemploymentCheckValue(buyer, buyer.Backpack);
            return CanBuy(list, buyerGold);
        }
        //adam: integer overflow (exploit) prevention:
        // It's easier to simply add the overflow checking in this tiny routine than to sprinkle overflow checking in all the different places purchase prices are calculated.
        //  Here, we simply abort out as soon as the buyer can no longer afford what they have selected.
        private bool CanBuy(List<BuyItemResponse> list, int buyerGold)
        {
            int _totalCost = 0;
            foreach (BuyItemResponse buy in list)
            {
                object o = World.FindEntity(buy.Serial);
                if (o == null) return false;
                GenericBuyInfo gbi = LookupDisplayObject(o);
                int amount = buy.Amount;
                int price = int.MaxValue;
                if (gbi != null)
                {
                    price = gbi.Price;
                }
                else if (o is Item item)
                {
                    // get the price of 1 unit. We will do the total amount calc below (checked)
                    price = LookupBuyPackDisplayObject(item, 1);
                }
                else
                    return false;

                bool overflow = false;
                try
                {
                    _totalCost = checked(_totalCost + price * amount);
                }
                catch (OverflowException)
                {
                    overflow = true;
                }
                if (_totalCost > buyerGold || overflow)
                {
                    //insufficient funds
                    return false;
                }
            }
            //sufficient funds
            return true;
        }
        private int LookupBuyPackDisplayObject(Item item, int amount)
        {
            int price = int.MaxValue;
            IShopSellInfo[] info = GetSellInfo();
            if (item.RootParent == this)
            {
                if (amount > item.Amount)
                    amount = item.Amount;

                if (amount > 0)
                    foreach (IShopSellInfo ssi in info)
                    {
                        if (ssi.IsSellable(item))
                        {
                            if (ssi.IsResellable(item))
                            {
                                price = ssi.GetBuyPriceFor(item) * amount;
                                break;
                            }
                        }
                    }
            }
            return price;
        }
        private bool CanAfford(Mobile buyer, List<BuyItemResponse> list)
        {
            return (CanBuyerPurchaseFromBank(buyer, list) == true || CanBuyerPurchaseFromBackpack(buyer, list) == true || CanBuyerPurchaseFromUnemploymentCheck(buyer, list) == true);
        }
        // they have enough money in their UnemploymentCheck
        public bool HasUnemploymentCheck(Mobile buyer, Container cont, Type type, int amount)
        {
            Item[] items = cont.FindItemsByType(type, true);
            if (items.Length == 0)
                return false;

            // okay, they have an UnemploymentCheck
            UnemploymentCheck uc = null;
            for (int ix = 0; ix < items.Length; ix++)
            {   // make sure one of them is theirs
                uc = items[ix] as UnemploymentCheck;
                if (uc.OwnerSerial != buyer.Serial)
                    continue;   // not their UnemploymentCheck!
                else break;
            }

            // make sure we found one that belongs to them
            if (uc.OwnerSerial != buyer.Serial)
                return false;   // not their UnemploymentCheck!

            // okay, they have an UnemploymentCheck. They may only have one, so further looking
            //	is not justified.
            if ((uc == null) || uc != null && amount > uc.Worth)
                return false;   // not enough left in their check

            return true;
        }
        public int UnemploymentCheckValue(Mobile buyer, Container cont)
        {
            Item[] items = cont.FindItemsByType(typeof(UnemploymentCheck), true);
            if (items.Length == 0)
                return 0;

            // okay, they have an UnemploymentCheck
            UnemploymentCheck uc = null;
            for (int ix = 0; ix < items.Length; ix++)
            {   // make sure one of them is theirs
                uc = items[ix] as UnemploymentCheck;
                if (uc.OwnerSerial != buyer.Serial)
                    continue;   // not their UnemploymentCheck!
                else break;
            }

            // make sure we found one that belongs to them
            if (uc.OwnerSerial != buyer.Serial)
                return 0;   // not their UnemploymentCheck!

            // okay, they have an UnemploymentCheck. They may only have one, so further looking
            //	is not justified.
            if ((uc == null) || uc != null && uc.Worth == 0)
                return 0;   // not enough left in their check

            return uc.Worth;
        }
        public bool ConsumeTotal(Mobile buyer, Container cont, Type type, int amount)
        {
            Item[] items = cont.FindItemsByType(type, true);
            if (items.Length == 0)
                return false;

            // okay, they have an UnemploymentCheck
            UnemploymentCheck uc = null;
            for (int ix = 0; ix < items.Length; ix++)
            {   // make sure one of them is theirs
                uc = items[ix] as UnemploymentCheck;
                if (uc.OwnerSerial != buyer.Serial)
                    continue;   // not their UnemploymentCheck!
                else break;
            }

            // make sure we found one that belongs to them
            if (uc.OwnerSerial != buyer.Serial)
                return false;   // not their UnemploymentCheck!

            // okay, they have an UnemploymentCheck. They may only have one, so further looking
            //	is not justified.
            if ((uc == null) || uc != null && amount > uc.Worth)
                return false;   // not enough left in their check

            // okay, they have a check that belongs to them and it holds enough value for the purchase.
            int remainder = uc.Worth - amount;
            if (remainder == 0)
                uc.Delete();    // delete the check if it is spent

            // new balance of the UnemploymentCheck
            uc.Worth = remainder;

            return true;
        }
        public virtual bool CheckVendorAccess(Mobile from)
        {
            GuardedRegion reg = this.Region as GuardedRegion;

            if (reg != null && !reg.CheckVendorAccess(this, from))
                return false;

            if (this.Region != from.Region)
            {
                reg = from.Region as GuardedRegion;

                if (reg != null && !reg.CheckVendorAccess(this, from))
                    return false;
            }

            return true;
        }

        public virtual bool OnSellItems(Mobile seller, List<SellItemResponse> list)
        {
            // adam: additional Hardening.
            try
            {
                // adam: add new sanity checks
                if (seller == null || seller.NetState == null || list == null)
                    return false;

                if (!IsActiveBuyer)
                    return false;

                if (!seller.CheckAlive())
                    return false;

                // adam: added back to support factions
                if (!CheckVendorAccess(seller))
                {
                    Say(501522); // I shall not treat with scum like thee!
                    return false;
                }

                seller.PlaySound(0x32);

                IShopSellInfo[] info = GetSellInfo();
                IBuyItemInfo[] buyInfo = this.GetBuyInfo();
                int GiveGold = 0;
                int Sold = 0;
                Container cont;
                ArrayList delete = new ArrayList();
                ArrayList drop = new ArrayList();

                foreach (SellItemResponse resp in list)
                {
                    if (resp.Item.RootParent != seller || resp.Amount <= 0)
                        continue;

                    foreach (IShopSellInfo ssi in info)
                    {
                        if (ssi.IsSellable(resp.Item))
                        {
                            Sold++;
                            break;
                        }
                    }
                }

                if (Sold > MaxSell)
                {
                    SayTo(seller, true, "You may only sell {0} items at a time!", MaxSell);
                    return false;
                }
                else if (Sold == 0)
                {
                    return true;
                }

                foreach (SellItemResponse resp in list)
                {
                    if (resp.Item.RootParent != seller || resp.Amount <= 0)
                        continue;

                    foreach (IShopSellInfo ssi in info)
                    {
                        if (ssi.IsSellable(resp.Item))
                        {
                            int amount = resp.Amount;

                            if (amount > resp.Item.Amount)
                                amount = resp.Item.Amount;

                            if (Core.RuleSets.ResourcePoolRules())
                                if (ResourcePool.OnVendorSell(this, seller, resp.Item, amount))
                                    break;

                            if (ssi.IsResellable(resp.Item))
                            {
                                bool found = false;

                                foreach (IBuyItemInfo bii in buyInfo)
                                {
                                    if (bii.Restock(resp.Item, amount))
                                    {
                                        resp.Item.Consume(amount);
                                        found = true;

                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    cont = this.BuyPack;

                                    if (amount < resp.Item.Amount)
                                    {
                                        resp.Item.Amount -= amount;
                                        Item item = resp.Item.Dupe(amount);
                                        item.SetLastMoved();
                                        cont.DropItem(item);
                                    }
                                    else
                                    {
                                        resp.Item.SetLastMoved();
                                        cont.DropItem(resp.Item);
                                    }
                                }
                            }
                            else
                            {
                                if (amount < resp.Item.Amount)
                                    resp.Item.Amount -= amount;
                                else
                                    resp.Item.Delete();
                            }

                            GiveGold += ssi.GetSellPriceFor(resp.Item) * amount;
                            break;
                        }
                    }
                }

                if (GiveGold > 0)
                {
                    while (GiveGold > 60000)
                    {
                        seller.AddToBackpack(new Gold(60000));
                        GiveGold -= 60000;
                    }

                    seller.AddToBackpack(new Gold(GiveGold));

                    seller.PlaySound(0x0037);//Gold dropping sound

                    if (BulkOrderSystem != null && BulkOrderSystem.IsEnabled() && BulkOrderSystem.SupportsBulkOrders(seller) && DateTime.UtcNow >= BulkOrderSystem.GetNextBOD(seller))
                        BulkOrderSystem.OfferBulkOrder(seller, this, false);
                }
                //no cliloc for this?
                //SayTo( seller, true, "Thank you! I bought {0} item{1}. Here is your {2}gp.", Sold, (Sold > 1 ? "s" : ""), GiveGold );

                return true;
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // removed in version 3. Pushed down to Mobile
            //writer.Write((bool)m_Invulnerable);

            ArrayList sbInfos = this.SBInfos;

            for (int i = 0; sbInfos != null && i < sbInfos.Count; ++i)
            {
                SBInfo sbInfo = (SBInfo)sbInfos[i];
                ArrayList buyInfo = sbInfo.BuyInfo;

                for (int j = 0; buyInfo != null && j < buyInfo.Count; ++j)
                {
                    GenericBuyInfo gbi = (GenericBuyInfo)buyInfo[j];

                    int maxAmount = gbi.MaxAmount;
                    int doubled = 0;

                    switch (maxAmount)
                    {
                        case 40: doubled = 1; break;
                        case 80: doubled = 2; break;
                        case 160: doubled = 3; break;
                        case 320: doubled = 4; break;
                        case 640: doubled = 5; break;
                        case 999: doubled = 6; break;
                    }

                    if (doubled > 0)
                    {
                        writer.WriteEncodedInt(1 + ((j * sbInfos.Count) + i));
                        writer.WriteEncodedInt(doubled);
                    }
                }
            }

            writer.WriteEncodedInt(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            LoadSBInfo();

            ArrayList sbInfos = this.SBInfos;

            switch (version)
            {
                case 3:
                    {
                        // skip to version 1 as we no longer read m_Invulnerable
                        goto case 1;
                    }
                case 2:
                    {
                        /*m_Invulnerable = */
                        reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        int index;

                        while ((index = reader.ReadEncodedInt()) > 0)
                        {
                            int doubled = reader.ReadEncodedInt();

                            if (sbInfos != null)
                            {
                                index -= 1;
                                int sbInfoIndex = index % sbInfos.Count;
                                int buyInfoIndex = index / sbInfos.Count;

                                if (sbInfoIndex >= 0 && sbInfoIndex < sbInfos.Count)
                                {
                                    SBInfo sbInfo = (SBInfo)sbInfos[sbInfoIndex];
                                    ArrayList buyInfo = sbInfo.BuyInfo;

                                    if (buyInfo != null && buyInfoIndex >= 0 && buyInfoIndex < buyInfo.Count)
                                    {
                                        GenericBuyInfo gbi = (GenericBuyInfo)buyInfo[buyInfoIndex];

                                        int amount = 20;

                                        switch (doubled)
                                        {
                                            case 1: amount = 40; break;
                                            case 2: amount = 80; break;
                                            case 3: amount = 160; break;
                                            case 4: amount = 320; break;
                                            case 5: amount = 640; break;
                                            case 6: amount = 999; break;
                                        }

                                        gbi.Amount = gbi.MaxAmount = amount;
                                    }
                                }
                            }
                        }

                        break;
                    }
            }

            NameHue = CalcInvulNameHue();

            CheckMorph();
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            if (from.Alive && IsActiveVendor)
            {
                if (IsActiveSeller)
                    list.Add(new VendorBuyEntry(from, this));

                if (IsActiveBuyer)
                    list.Add(new VendorSellEntry(from, this));

                if (BulkOrderSystem != null && BulkOrderSystem.IsEnabled() && BulkOrderSystem.SupportsBulkOrders(from))
                    BulkOrderSystem.AddContextMenuEntries(from, this, list);
            }

            base.AddCustomContextEntries(from, list);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (BulkOrderSystem != null && BulkOrderSystem.IsEnabled() && BulkOrderSystem.SupportsBulkOrders(e.Mobile))
                BulkOrderSystem.HandleSpeech(e.Mobile, this, e);

            base.OnSpeech(e);
        }

        public virtual IShopSellInfo[] GetSellInfo()
        {
            return (IShopSellInfo[])m_ArmorSellInfo.ToArray(typeof(IShopSellInfo));
        }

        public virtual IBuyItemInfo[] GetBuyInfo()
        {
            return (IBuyItemInfo[])m_ArmorBuyInfo.ToArray(typeof(IBuyItemInfo));
        }

        public override bool CanBeDamaged()
        {
            return !IsInvulnerable;
        }

        public override void OnDeath(Container c)
        {
            /*
			 * Publish 4
			 * Shopkeeper Changes
			 * If a shopkeeper is killed, a new shopkeeper will appear as soon as another player (other than the one that killed it) approaches.
			 * http://www.uoguide.com/Publish_4
			 */
            if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisRules() && PublishInfo.Publish >= 4)
            {
                if (Spawner != null && !Spawner.Deleted && Spawner.Running)
                {
                    foreach (AggressorInfo ai in this.Aggressors)
                    {
                        if (ai.Attacker.Player && ai.CanReportMurder && !ai.InitialAggressionInNoCountZone)
                        {   // no need to remember longer than the next spawn time
                            Spawner.BadGuys.Remember(ai.Attacker, Spawner.NextSpawn.TotalSeconds);
                        }
                    }

                }
            }

            /* Publish 4
			 * Shopkeeper Changes
			 * NPC shopkeepers will give a murder count when they die unless they are criminal or evil. The issue with murder counts from NPCs not decaying (as reported on Siege Perilous) will also be addressed.
			 * http://www.uoguide.com/Publish_4
			 */
            if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisRules() && PublishInfo.Publish >= 4)
            {
                foreach (AggressorInfo ai in this.Aggressors)
                {
                    if (ai.Attacker.Player && ai.CanReportMurder && !ai.InitialAggressionInNoCountZone)
                    {
                        Mobile killer = ai.Attacker;
                        if (killer != null && !killer.Deleted)
                        {
                            killer.LongTermMurders++;
                            killer.ShortTermMurders++;

                            if (killer is PlayerMobile)
                                ((PlayerMobile)killer).ResetKillTime();

                            killer.SendLocalizedMessage(1049067);//You have been reported for murder!

                            if (killer.LongTermMurders == 5)
                                killer.SendLocalizedMessage(502134);//You are now known as a murderer!
                            else if (SkillHandlers.Stealing.SuspendOnMurder && killer.LongTermMurders == 1 && killer is PlayerMobile && ((PlayerMobile)killer).NpcGuild == NpcGuild.ThievesGuild)
                                killer.SendLocalizedMessage(501562); // You have been suspended by the Thieves Guild.

                            killer.OnReportedForMurder(this);
                        }
                    }
                }
            }

            base.OnDeath(c);
        }

        public override void OnActionCombat(MobileInfo info)
        {
            if (this.AI == AIType.AI_Vendor)
            {
                // save our pissed-off state
                bool warmode = this.Warmode;
                Mobile combatant = this.Combatant;
                Mobile focusMob = this.FocusMob;

                if (this.Combatant != null && this.Combatant.NetState is not null)
                {   // close open inventory lists
                    this.Combatant.NetState.Send(new EndVendorBuy(this));
                }

                this.AI = AIType.AI_Melee;
                if (AIObject != null)
                {   // creating a new AI will reset a bunch of state, so we need to restore it
                    AIObject.Action = ActionType.Combat;
                    this.Warmode = warmode;
                    this.Combatant = combatant;
                    this.FocusMob = focusMob;
                    this.CurrentSpeed = this.ActiveSpeed;
                    this.FightMode = FightMode.All | FightMode.Closest;
                }
                DebugSay(DebugFlags.AI, "Changing from AI_Vendor to AI_Melee");
            }

            base.OnActionCombat(info: info);
        }


        public override void OnActionFlee()
        {
            if (this.Combatant != null && this.Combatant.NetState != null)
            {   // close open inventory lists
                this.Combatant.NetState.Send(new EndVendorBuy(this));
            }

            base.OnActionFlee();
        }

        /*public override void OnActionFlee()
		{
			base.OnActionFlee();
		}*/

        public override void OnActionWander()
        {
            if (this.AI == AIType.AI_Melee)
            {
                this.AI = AIType.AI_Vendor;         // just a vendor again
                this.FightMode = FightMode.None;    // revert to passive
                this.Aggressed.Clear();             // because we do not AcquireFocusMob in AI_Vendor AI, we fail to reset the Combatant 
                this.Aggressors.Clear();            //	in mobile.AggressiveAction(). Rather that handle this special case, we can force the issue
                                                    //	by simply clearing the vendor's agressors list
                DebugSay(DebugFlags.AI, "Changing from AI_Melee to AI_Vendor");
            }

            base.OnActionWander();
        }
        #region Standard Pricing Database
        [CallPriority(0)]   // must come before ResourcePool
        public static void Configure()
        {   // Note: PreWorldLoad here since we need to be up and running before 
            //  WorldLoad where BaseVendor configures his pricing tables
            EventSink.PreWorldLoad += new PreWorldLoadEventHandler(Load);
        }
        public const int PlayerPaysFailsafe = 1000000;
        public static int PlayerPays(int price)
        {   // this is for items that can't be looked up by type,
            //  For instance a a Pitcher of Milk costs different that a Pitcher of Ale
            if (Core.RuleSets.SiegeStyleRules())
                price *= 3;
            return price;
        }
        public static int PlayerPays(Type type, double siege_factor = 1.0)
        {
            if (StandardPricingDictionary.ContainsKey(type))
                foreach (var kvp in StandardPricingDictionary[type])
                    if (kvp.Key == StandardPricingType.Buy)
                        if (Core.RuleSets.SiegeStyleRules())
                            return Core.RuleSets.SiegePriceRules(type, (int)(kvp.Value * siege_factor));
                        else
                            return (int)(kvp.Value * siege_factor);

            Utility.Monitor.WriteLine("Could not find buy price for type {0}", ConsoleColor.Red, type == null ? "(null)" : type.FullName);
            // failsafe pricing
            return PlayerPaysFailsafe;
        }
        public const int VendorPaysFailsafe = 0;
        public static int VendorPays(Type type)
        {
            if (StandardPricingDictionary.ContainsKey(type))
                foreach (var kvp in StandardPricingDictionary[type])
                    if (kvp.Key == StandardPricingType.Sell)
                        return kvp.Value;

            Utility.Monitor.WriteLine("Could not find sell price for type {0}", ConsoleColor.Red, type == null ? "(null)" : type.FullName);
            // failsafe pricing
            return VendorPaysFailsafe;
        }
        public static Dictionary<Type, List<KeyValuePair<StandardPricingType, int>>> StandardPricingDictionary = new();
        public static void Load()
        {
            Console.Write("Standard Pricing Database Loading...");
            try
            {
                string pathName = Path.Combine(Core.DataDirectory, "vendor pricing.txt");
                const Int32 BufferSize = 256;
                using (var fileStream = File.OpenRead(pathName))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    string line;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        // comment?
                        if (line.StartsWith('#'))
                            continue;

                        string[] toks = line.Split(new char[] { ':', ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        Type type = ScriptCompiler.FindTypeByFullName(toks[0]);
                        if (type == null)
                        {   // not to worry, RunUO (from which this database was derived,) has many more items than Angel Island.
                            //Utility.ConsoleOut("Could not find type {0}", ConsoleColor.Red, toks[0]);
                            continue;
                        }
                        StandardPricingType spt = toks[1].ToLower() == "buy" ? StandardPricingType.Buy : StandardPricingType.Sell;
                        int price = Convert.ToInt32(toks[2]);
                        if (price <= 0) continue;
                        if (StandardPricingDictionary.ContainsKey(type))
                        {
                            bool found = false;
                            foreach (var v in StandardPricingDictionary[type])
                                if (v.Key == spt) { found = true; break; }

                            if (found)
                                // we already have this price 
                                continue;
                            else
                            {
                                // add this price
                                StandardPricingDictionary[type].Add(new KeyValuePair<StandardPricingType, int>(spt, price));
                            }
                        }
                        else
                        {
                            // add this price
                            StandardPricingDictionary.Add(type,
                                new List<KeyValuePair<StandardPricingType, int>>()
                                { new KeyValuePair<StandardPricingType, int>(spt, price) });
                        }
                    }
                }

                Console.WriteLine(" {0} rules loaded.", StandardPricingDictionary.Count);

            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Standard Pricing Database, using default values.", ConsoleColor.Red);
            }
        }
        #endregion Standard Pricing Database
    }
    public enum StandardPricingType
    {
        Buy,
        Sell
    }
}

namespace Server.ContextMenus
{
    public class VendorBuyEntry : ContextMenuEntry
    {
        private BaseVendor m_Vendor;

        public VendorBuyEntry(Mobile from, BaseVendor vendor)
            : base(6103, 8)
        {
            m_Vendor = vendor;
            Enabled = vendor.CheckVendorAccess(from);
        }

        public override void OnClick()
        {
            m_Vendor.VendorBuy(this.Owner.From);
        }
    }

    public class VendorSellEntry : ContextMenuEntry
    {
        private BaseVendor m_Vendor;

        public VendorSellEntry(Mobile from, BaseVendor vendor)
            : base(6104, 8)
        {
            m_Vendor = vendor;
            Enabled = vendor.CheckVendorAccess(from);
        }

        public override void OnClick()
        {
            m_Vendor.VendorSell(this.Owner.From);
        }
    }
}

namespace Server
{
    public interface IShopSellInfo
    {
        //get display name for an item
        string GetNameFor(Item item);

        //get price for an item which the player is selling
        int GetSellPriceFor(Item item);

        //get price for an item which the player is buying
        int GetBuyPriceFor(Item item);

        //can we sell this item to this vendor?
        bool IsSellable(Item item);

        //What do we sell?
        Type[] Types { get; }

        //does the vendor resell this item?
        bool IsResellable(Item item);
    }

    public interface IBuyItemInfo
    {
        //get a new instance of an object (we just bought it)
        object GetObject();

        int ControlSlots { get; }

        int PriceScalar { get; set; }

        //display price of the item
        int Price { get; }

        //display name of the item
        string Name { get; }

        //display hue
        int Hue { get; }

        //display id
        int ItemID { get; }

        //amount in stock
        int Amount { get; set; }

        //max amount in stock
        int MaxAmount { get; }

        Type Type { get; }

        //Attempt to restock with item, (return true if restock sucessful)
        bool Restock(Item item, int amount);

        //called when its time for the whole shop to restock
        void OnRestock();
        // void OnRestockReagents();
    }
}