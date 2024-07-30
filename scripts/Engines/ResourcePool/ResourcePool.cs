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

/* Scripts/Engines/ResourcePool/ResourcePool.cs
 * ChangeLog
 *  3/16/23, Adam (PatchStaticPriceWithSPD())
 *      We use a Standard Pricing Database to ensure prices across the shard are consistent.
 *      ResourcePool hard-codes prices, and therefore bypasses our usual SPD pricing and subsequent markup (like the Siege markup.)
 *      We therefore patch prices to SPD + any markup during the load of ResourceData(reader).
 *      We do it here and not at the higher Vendor level since the ResourcePool uses these prices internally for certain booking operations,
 *      like for instance the commission book.
 *  8/19/22, Adam (EventSink_OnPreWorldLoad)
 *      Move Init() functionality from Configure() ==> EventSink_OnPreWorldLoad()
 *  12/26/21, Adam (LoadConsignments)
 *      Increment the loop counter so that we exit without an exception. :P
 *	11/16/21, Yoar
 *	    BBS overhaul:
 *	    - Rewrote RDDirect into EquivalentResource. EquivalentResource info is stored in ResourceData.
 *	    - Now dynamically creating/caching GBI for the single/bunch BBS sales. Bunch types are no longer needed.
 *	    - Moved/rewrote all code related to the BBS into the ResourcePool class.
 *	09/13/21 Yoar
 *	    Serialization overhaul
 *	
 *	    1. On Configure, we only load the config file. We use the following logic to select which config file to load:
 *	       Does the Saves folder exist?
 *	           Load Saves/ResourcePool/config.xml
 *	       Otherwise
 *	           Load Data/ResourcePool/config.xml
 *	
 *	    2. On World Save, we write the following three files to Saves/ResourcePool/:
 *	       a. Saves/ResourcePool/config.xml
 *	       b. Saves/ResourcePool/Consignments.dat
 *	       c. Saves/ResourcePool/Debts.dat
 *	       (*) We write the files separately, each in one method.
 *	
 *	    3. On World Load, if the Saves folder exists, we load the following two files from Saves/ResourcePool/:
 *	       a. Saves/ResourcePool/Consignments.dat
 *	       b. Saves/ResourcePool/Debts.dat
 *	       (*) We load the files separately, each in one method.
 *	
 *	    4. Upgraded data structure (config version >= 2):
 *	       a. Added version to Debts.dat.
 *	       b. Added array count to Consignments.dat.
 *	       c. Removed calibration string "ResourceConsignment" from Consignments.dat.
 *	
 *	07/19/09, plasma
 *			Added exception handler to the loading of Debts.dat
 *  7/13/07, Adam
 *      rewrite Debt hashtable saving and loading to eliminage
 *      BinaryFileReader.End() as the means for detecting the EOF.
 *      http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=127647&SiteID=1
 *		Also add a 'PlayerMobile' filter to the mobile associated with debt.
 *			When I was debugging I found 127 mobiles where only 5 were PlayerMobile. 
 *			I'm assuming the original PlayerMobile was deleted and the serial reassigned to some other creature.
 *	06/2/06, Adam
 *		Give this console a special hue
 *  04/27/05 TK
 *		Made ResourceDatas sortable, using Name as the comparator
 *	04/23/05 Pix
 *		Added GetBunchType() for use in 'vendor buy'
 *  02/24/05 TK
 *		Fixed ResourcePool.Save so that it doesn't save Deleted mobiles
 *		Fixed auto-detection algorithm for configuration data
 *  02/19/05, Adam
 *		** HACK **
 *		Added null checks to stop startup crash
 *		Commented out 'auto change detection' as it was always firing
 *  02/11/05 TK
 *		Made sure all files are closed so that backups occur correctly.
 *  02/10/05 TK
 *		Added a player==null check to prevent crashes when paying off consignments from
 *		deleted players. Paid gold goes to BountySystem.BountyKeeper.LBFund
 *  06/02/05 TK
 *		Overhauled save implementation, hopefully for last time
 *  04/02/05 TK
 *		Changed write access levels to Administrator instead of GM
 *  03/02/05 TK
 *		Added DatFilePosition element to XML configuration to allow a bit more tolerance
 *		Moved ResourceLogger code to own file
 *		Cleaned up ResourcePool [props window
 *  02/02/05 TK
 *		Revamped ResourcePool save system to use XML configuration.
 *		Removed Resource setup in ResourcePool.Configure(), made it call Load
 *		Documentation to come, don't f*ck with the config file till then ;)
 *		Added ValidFailsafe flag to prevent auto-generation of valorite ingots etc
 *  01/31/05
 *		Various changes that I forgot (small ones, no worries ;)
 *		Cleaned up OnLoad failure notifications.
 *  01/28/05 Taran Kain
 *		Changed logging system, added RDRedirect class, probably some other stuff
 *	01/24/05 Taran Kain
 *		Removed PaymentCheck and placed in own file.
 *  01/23/05 Taran Kain
 *		Version 1.0
 */

using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server.Engines.ResourcePool
{
    public class ResourceConsignment : IComparable
    {
        private double m_Price;
        private Mobile m_Seller;
        private Type m_Type;
        private int m_Amount;

        public double Price { get { return m_Price; } }
        public Mobile Seller { get { return m_Seller; } }
        public Type Type { get { return m_Type; } }

        public int Amount
        {
            get { return m_Amount; }
            set { m_Amount = value; }
        }

        public ResourceConsignment(Type type, int amount, double price, Mobile seller)
        {
            m_Price = price;
            m_Seller = seller;
            m_Type = type;
            m_Amount = amount;
        }

        public int CompareTo(object obj)
        {
            if (obj is ResourceConsignment)
                return m_Amount.CompareTo(((ResourceConsignment)obj).Amount);

            throw new ArgumentException("object	is not ResourceConsignment");
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)1); // version

            writer.Write((double)m_Price);
            writer.Write((Mobile)m_Seller);
            writer.Write((string)m_Type.FullName);
            writer.Write((int)m_Amount);
        }

        public ResourceConsignment(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        m_Price = reader.ReadDouble();
                        m_Seller = reader.ReadMobile();
                        m_Type = Type.GetType(reader.ReadString());
                        m_Amount = reader.ReadInt();

                        if (version < 1)
                            reader.ReadString(); // "ResourceConsignment"

                        break;
                    }
                default: throw new Exception("ResourcePool Error: Invalid version of \"ResourceConsignment\".");
            }
        }
    }

    [PropertyObject]
    public class ResourceData : IComparable
    {
        private Type m_Type;

        private string m_Name;
        private string m_BunchName;
        private int m_ItemID;
        private int m_Hue;

        private double m_WholesalePrice;
        private double m_ResalePrice;
        private double m_SmallestFirstFactor;
        private bool m_ValidFailsafe;

        private List<EquivalentResource> m_EquivalentResources;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Type Type { get { return m_Type; } }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public string Name { get { return m_Name; } }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public string BunchName { get { return m_BunchName; } }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int ItemID { get { return m_ItemID; } }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int Hue { get { return m_Hue; } }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double WholesalePrice
        {
            get { return m_WholesalePrice; }
            set { m_WholesalePrice = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double ResalePrice
        {
            get { return m_ResalePrice; }
            set { m_ResalePrice = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double SmallestFirstFactor
        {
            get { return m_SmallestFirstFactor; }
            set { m_SmallestFirstFactor = Math.Max(0.0, Math.Min(1.0, value)); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool ValidFailsafe
        {
            get { return m_ValidFailsafe; }
            set { m_ValidFailsafe = value; }
        }

        public List<EquivalentResource> EquivalentResources { get { return m_EquivalentResources; } }

        private EquivalentResource GetEquivalentResourceAt(int index)
        {
            if (index >= 0 && index < m_EquivalentResources.Count)
                return m_EquivalentResources[index];

            return null;
        }

        #region Equivalent Resource Accessors

        [CommandProperty(AccessLevel.Counselor)]
        public EquivalentResource EquivRes1 { get { return GetEquivalentResourceAt(0); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public EquivalentResource EquivRes2 { get { return GetEquivalentResourceAt(1); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public EquivalentResource EquivRes3 { get { return GetEquivalentResourceAt(2); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public EquivalentResource EquivRes4 { get { return GetEquivalentResourceAt(3); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public EquivalentResource EquivRes5 { get { return GetEquivalentResourceAt(4); } set { } }

        [CommandProperty(AccessLevel.Counselor)]
        public EquivalentResource EquivRes6 { get { return GetEquivalentResourceAt(5); } set { } }

        #endregion

        [CommandProperty(AccessLevel.Counselor)]
        public int TotalAmount { get { return ResourcePool.GetTotalAmount(m_Type); } }

        [CommandProperty(AccessLevel.Counselor)]
        public double TotalInvested { get { return ResourcePool.GetTotalInvested(m_Type); } }

        [CommandProperty(AccessLevel.Counselor)]
        public double TotalValue { get { return ResourcePool.GetTotalValue(m_Type); } }

        public ResourceData(Type type, string name, string bunchname, int itemid, int hue, double wholesalePrice, double resalePrice, bool validfailsafe)
        {
            m_Type = type;

            m_Name = name;
            m_BunchName = bunchname;
            m_ItemID = itemid;
            m_Hue = hue;

            m_WholesalePrice = wholesalePrice;
            m_ResalePrice = resalePrice;
            m_SmallestFirstFactor = 0.5;
            m_ValidFailsafe = validfailsafe;

            m_EquivalentResources = new List<EquivalentResource>();
        }

        public EquivalentResource GetEquivalentResource(Type type)
        {
            foreach (EquivalentResource er in m_EquivalentResources)
            {
                if (er.Type == type)
                    return er;
            }

            return null;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("ResourceData");
            writer.WriteAttributeString("version", "1");

            try
            {
                writer.WriteElementString("Name", m_Name);
                writer.WriteElementString("Type", m_Type.FullName);
                writer.WriteElementString("BunchName", m_BunchName);
                writer.WriteElementString("ResalePrice", m_ResalePrice.ToString("R"));
                writer.WriteElementString("WholesalePrice", m_WholesalePrice.ToString("R"));
                writer.WriteElementString("ItemID", m_ItemID.ToString());
                writer.WriteElementString("Hue", m_Hue.ToString());
                writer.WriteElementString("SmallestFirstFactor", m_SmallestFirstFactor.ToString("R"));
                writer.WriteElementString("ValidFailsafe", m_ValidFailsafe.ToString());

                try
                {
                    writer.WriteStartElement("EquivalentResources");
                    writer.WriteAttributeString("Count", m_EquivalentResources.Count.ToString());

                    for (int i = 0; i < m_EquivalentResources.Count; i++)
                        m_EquivalentResources[i].Save(writer);
                }
                finally { writer.WriteEndElement(); }
            }
            finally { writer.WriteEndElement(); }
        }

        public ResourceData(XmlTextReader reader)
        {
            m_EquivalentResources = new List<EquivalentResource>();

            int version = Int32.Parse(reader.GetAttribute("version"));
            reader.ReadStartElement("ResourceData");

            switch (version)
            {
                case 1:
                case 0:
                    {
                        m_Name = reader.ReadElementString("Name");
                        string typeName = reader.ReadElementString("Type");
                        m_Type = Type.GetType(typeName);
                        m_BunchName = reader.ReadElementString("BunchName");
                        m_ResalePrice = Double.Parse(reader.ReadElementString("ResalePrice"));
                        m_WholesalePrice = Double.Parse(reader.ReadElementString("WholesalePrice"));
                        m_ItemID = Int32.Parse(reader.ReadElementString("ItemID"));
                        m_Hue = Int32.Parse(reader.ReadElementString("Hue"));
                        m_SmallestFirstFactor = Double.Parse(reader.ReadElementString("SmallestFirstFactor"));
                        m_ValidFailsafe = bool.Parse(reader.ReadElementString("ValidFailsafe"));

                        if (m_Type == null)
                            throw new Exception(String.Format("ResourcePool Error: Resource type \"{0}\" does not exist.", typeName));

                        if (m_Type.GetInterface("ICommodity", false) == null)
                            throw new Exception(String.Format("ResourcePool Error: Resource type \"{0}\" does not implement \"ICommodity\".", typeName));

                        if (version >= 1)
                        {
                            int count = Int32.Parse(reader.GetAttribute("Count"));

                            if (count == 0)
                            {
                                reader.Read(); // read the empty element
                            }
                            else
                            {
                                reader.ReadStartElement("EquivalentResources");

                                for (int i = 0; i < count; i++)
                                {
                                    EquivalentResource er = new EquivalentResource(reader);

                                    m_EquivalentResources.Add(er);

                                    ResourcePool.Equivalents[er.Type] = m_Type;
                                }

                                reader.ReadEndElement();
                            }
                        }

                        break;
                    }
                default: throw new Exception("ResourcePool Error: Invalid version of \"ResourceData\".");
            }

            reader.ReadEndElement();

            // We use a Standard Pricing Database to ensure prices across the shard are consistent.
            //  ResourcePool hard-codes prices, and therefore bypasses our usual SPD pricing and subsequent markup (like the Siege markup.)
            //  We therefore patch prices to SPD + any markup.
            //  We do it here and not at the higher Vendor level since the ResourcePool uses these prices internally for certain booking operations,
            //      like for instance the commission book.
            PatchStaticPriceWithSPD();
        }
        private void PatchStaticPriceWithSPD()
        {   // query our Standard Pricing Database to see what the standard price is for this item
            m_ResalePrice = BaseVendor.PlayerPays(m_Type);
            m_WholesalePrice = BaseVendor.PlayerPays(m_Type);
        }
        public int CompareTo(object obj)
        {
            if (obj is ResourceData)
                return m_Name.CompareTo(((ResourceData)obj).m_Name);

            throw new ArgumentException("object	is not ResourceData");
        }

        public override string ToString()
        {
            return "...";
        }
    }

    [PropertyObject]
    public class EquivalentResource
    {
        private Type m_Type;

        private double m_AmountFactor;
        private double m_PriceFactor;
        private string m_Message;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Type Type { get { return m_Type; } }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double AmountFactor
        {
            get { return m_AmountFactor; }
            set { m_AmountFactor = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double PriceFactor
        {
            get { return m_PriceFactor; }
            set { m_PriceFactor = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public string Message
        {
            get { return m_Message; }
            set { m_Message = value; }
        }

        public EquivalentResource(Type type, double amountFactor, double priceFactor, string message)
        {
            m_Type = type;

            m_AmountFactor = amountFactor;
            m_PriceFactor = priceFactor;
            m_Message = message;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("EquivalentResource");
            writer.WriteAttributeString("version", "0");

            try
            {
                writer.WriteElementString("Type", this.Type.FullName);

                writer.WriteElementString("AmountFactor", m_AmountFactor.ToString("R"));
                writer.WriteElementString("PriceFactor", m_PriceFactor.ToString("R"));
                writer.WriteElementString("Message", m_Message);
            }
            finally { writer.WriteEndElement(); }
        }

        public EquivalentResource(XmlTextReader reader)
        {
            int version = Int32.Parse(reader.GetAttribute("version"));
            reader.ReadStartElement("EquivalentResource");

            switch (version)
            {
                case 0:
                    {
                        string typeName = reader.ReadElementString("Type");
                        m_Type = Type.GetType(typeName);

                        m_AmountFactor = Double.Parse(reader.ReadElementString("AmountFactor"));
                        m_PriceFactor = Double.Parse(reader.ReadElementString("PriceFactor"));
                        m_Message = reader.ReadElementString("Message");

                        if (m_Type == null)
                            throw new Exception(String.Format("ResourcePool Error: Equivalent resource type \"{0}\" does not exist.", typeName));

                        break;
                    }
                default: throw new Exception("ResourcePool Error: Invalid version of \"EquivalentResource\".");
            }

            reader.ReadEndElement();
        }

        public override string ToString()
        {
            return "...";
        }
    }

    public class ResourcePool : Item
    {
        public static bool Enabled { get { return Core.RuleSets.ResourcePoolRules(); } }

        private static double m_PaymentFactor; // the player gets this cut of the sale
        private static double m_FailsafePriceHike; // price multiplier for when the vendor sells spawned resources

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static double PaymentFactor
        {
            get { return m_PaymentFactor; }
            set { m_PaymentFactor = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static double FailsafePriceHike
        {
            get { return m_FailsafePriceHike; }
            set { m_FailsafePriceHike = value; }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public static RPProps RPProps
        {
            get { return RPProps.Instance; }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static int LogLevel
        {
            get { return ResourceLogger.LogLevel; }
            set { ResourceLogger.LogLevel = value; }
        }

        private static Dictionary<Type, ResourceData> m_Resources;
        private static Dictionary<Type, Type> m_Equivalents;
        private static Dictionary<Type, List<ResourceConsignment>> m_Consignments;
        private static Dictionary<Mobile, double> m_Debts;

        public static Dictionary<Type, ResourceData> Resources { get { return m_Resources; } }
        public static Dictionary<Type, Type> Equivalents { get { return m_Equivalents; } }
        public static Dictionary<Type, List<ResourceConsignment>> Consignments { get { return m_Consignments; } }
        public static Dictionary<Mobile, double> Debts { get { return m_Debts; } }

        public static ResourceData GetResource(Type type)
        {
            return GetResource(type, false);
        }

        public static ResourceData GetResource(Type type, bool equivalent)
        {
            ResourceData rd;

            if (m_Resources.TryGetValue(type, out rd))
                return rd;

            if (equivalent)
            {
                Type equivalentType = GetEquivalentType(type);

                if (equivalentType != null && m_Resources.TryGetValue(equivalentType, out rd))
                    return rd;
            }

            return null;
        }

        public static Type GetEquivalentType(Type type)
        {
            Type equivalentType;

            if (m_Equivalents.TryGetValue(type, out equivalentType))
                return equivalentType;

            return null;
        }

        public static bool IsPooledResource(Type type)
        {
            return IsPooledResource(type, false);
        }

        public static bool IsPooledResource(Type type, bool equivalent)
        {
            if (type == null)
                return false;
            return GetResource(type, equivalent) != null;
        }

        public static List<ResourceConsignment> GetConsignmentList(Type type)
        {
            List<ResourceConsignment> rcList;

            if (!m_Consignments.TryGetValue(type, out rcList))
                m_Consignments[type] = rcList = new List<ResourceConsignment>();

            return rcList;
        }

        [Obsolete("Bunch types are no longer needed")]
        public static string GetBunchName(Type type)
        {
            ResourceData rd = GetResource(type);

            if (rd != null)
                return rd.BunchName;

            return String.Empty;
        }

        public static int GetTotalAmount(Type type)
        {
            List<ResourceConsignment> rcList = GetConsignmentList(type);

            int total = 0;

            foreach (ResourceConsignment rc in rcList)
                total += rc.Amount;

            return total;
        }

        public static double GetTotalInvested(Type type)
        {
            List<ResourceConsignment> rcList = GetConsignmentList(type);

            double total = 0.0;

            foreach (ResourceConsignment rc in rcList)
                total += rc.Amount * rc.Price;

            return total;
        }

        public static double GetTotalValue(Type type)
        {
            ResourceData rd = GetResource(type);

            if (rd == null)
                return 0.0;

            return rd.ResalePrice * GetTotalAmount(type);
        }

        #region Vendor Sell

        public static bool AddSellInfo(BaseVendor vendor, Item item, ref Dictionary<Item, SellItemState> table)
        {
            if (!Enabled)
                return false;

            CommodityDeed cd = item as CommodityDeed;

            if (cd != null && (cd.Commodity == null || cd.Commodity.Deleted))
                return false; // invalid commodity deed

            Type type;

            if (cd != null)
                type = cd.Commodity.GetType();
            else
                type = item.GetType();

            ResourceData rd = GetResource(type, true);

            if (rd == null)
                return false; // this is not a pooled resource

            double salePrice = rd.WholesalePrice;

            if (cd != null)
                salePrice *= cd.Commodity.Amount;

            if (type != rd.Type) // this is an equivalent resource
            {
                EquivalentResource er = rd.GetEquivalentResource(type);

                if (er == null)
                    return false; // sanity

                salePrice *= er.PriceFactor;
            }

            int isalePrice = (int)salePrice; // truncated sale price

            if (isalePrice <= 0)
                return false;

            string name;

            if (cd != null)
                name = String.Format("Commodity Deed ({0})", rd.Name);
            else
                name = rd.Name;

            table[item] = new SellItemState(item, isalePrice, name);

            return true;
        }

        public static bool OnVendorSell(BaseVendor vendor, Mobile seller, Item item, int amount)
        {
            if (!Enabled)
                return false;

            CommodityDeed cd = item as CommodityDeed;

            if (cd != null && (cd.Commodity == null || cd.Commodity.Deleted || amount != 1))
                return false; // invalid commodity deed

            Type type;

            if (cd != null)
                type = cd.Commodity.GetType();
            else
                type = item.GetType();

            ResourceData rd = GetResource(type, true);

            if (rd == null)
                return false; // this is not a pooled resource

            double salePrice = rd.WholesalePrice;

            double toPool;

            if (cd != null)
                toPool = cd.Commodity.Amount;
            else
                toPool = amount;

            if (type != rd.Type) // this is an equivalent resource
            {
                EquivalentResource er = rd.GetEquivalentResource(type);

                if (er == null)
                    return false; // sanity

                salePrice *= er.PriceFactor;
                toPool *= er.AmountFactor;

                if (!String.IsNullOrEmpty(er.Message))
                    seller.SendMessage(er.Message);
            }

            // TODO: Needed?
#if false
            bool betterInBulk = false;

            if (cd == null && (int)salePrice != salePrice)
            {
                salePrice = Math.Floor(salePrice); // truncate the sale price

                betterInBulk = true;
            }
#endif

            if (salePrice <= 0.0)
                return false;

            int iToPool = (int)toPool;

            if (toPool <= 0)
                return false;

            if (cd != null)
            {
#if true
                // our CDs are kinda wonky. You'd think you want to delete the Commodity here, but referencing
                // Commodity at all will create a new Commodity. I.e., cd.Commodity.Consume(amount)
                //  what you need to do is simply consume the deed itself.
                //  Our Cron IntMapItemCleanup() will handle the orphaned Commodity here.
                cd.Consume(amount);
#else
                cd.Commodity.Consume(amount);
                if (cd.Commodity.Amount <= 0)
                    item.Delete();
#endif
            }
            else
            {
                item.Consume(amount);
            }

            // eliminate the fractional portion if it's zero
            string friendlyPrice = string.Format("{0:F2}", salePrice);
            friendlyPrice = friendlyPrice.Replace(".00", "");
            vendor.SayTo(seller, true, "Thank you. I will sell these for you at {0} gp each, minus my commission.", friendlyPrice);

            // TODO: Needed?
#if false
            if (betterInBulk)
                vendor.SayTo(seller, true, "I can give you a slightly better price if you sell in bulk with commodity deeds.");
#endif

            AddConsignment(rd.Type, seller, salePrice, iToPool);

            ResourceTransaction rt = new ResourceTransaction(TransactionType.Sale);

            rt.ResName = rd.Name;
            rt.Amount = amount;
            rt.Price = salePrice;
            rt.NewAmount = GetTotalAmount(rd.Type);
            rt.VendorID = vendor.Serial;

            ResourceLogger.Add(rt, seller);

            return true;
        }

        private static void AddConsignment(Type type, Mobile seller, double salePrice, int amount)
        {
            List<ResourceConsignment> rcList = GetConsignmentList(type);

            bool add = true;

            foreach (ResourceConsignment rc in rcList)
            {
                if (rc.Seller == seller && rc.Price == salePrice)
                {
                    rc.Amount += amount;
                    add = false;
                    break;
                }
            }

            if (add)
                rcList.Add(new ResourceConsignment(type, amount, salePrice, seller));

            rcList.Sort(); // sort in ascending order by amount
        }

        #endregion

        #region Vendor Buy

        private class GBIEntry
        {
            private GenericBuyInfo m_Single;
            private GenericBuyInfo m_Bunch;

            public GenericBuyInfo Single { get { return m_Single; } }
            public GenericBuyInfo Bunch { get { return m_Bunch; } }

            public GBIEntry(Type type)
            {
                m_Single = new GenericBuyInfo("", type, 1, 1, 20, 100, 0, 0, new object[0]);
                m_Bunch = new GenericBuyInfo("", type, 1, 1, 20, 100, 0, 0, new object[0]);
            }
        }

        private static readonly Dictionary<Type, GBIEntry> m_GBICache = new Dictionary<Type, GBIEntry>();

        private static GenericBuyInfo GetBuyInfo(Type type, bool bunch)
        {
            GBIEntry entry;

            if (!m_GBICache.TryGetValue(type, out entry))
                m_GBICache[type] = entry = new GBIEntry(type);

            if (bunch)
                return entry.Bunch;

            return entry.Single;
        }

        public static GenericBuyInfo LookupBuyInfo(object obj)
        {
            bool bunch;

            return LookupBuyInfo(obj, out bunch);
        }

        private static GenericBuyInfo LookupBuyInfo(object obj, out bool bunch)
        {
            bunch = false;

            GBIEntry entry;

            if (!m_GBICache.TryGetValue(obj.GetType(), out entry))
                return null;

            if (entry.Single.GetDisplayObject() == obj)
                return entry.Single;

            if (entry.Bunch.GetDisplayObject() == obj)
            {
                bunch = true;
                return entry.Bunch;
            }

            return null;
        }

        private enum StockType : byte { NPC, Single, Bunch }

        private static void UpdateBuyInfo(GenericBuyInfo gbi, ResourceData rd, StockType stockType)
        {
            string name = rd.Name;
            double price = rd.ResalePrice;
            int amount = gbi.Amount; // by default, use vendor amount

            switch (stockType)
            {
                case StockType.NPC:
                    {
                        price *= FailsafePriceHike; // apply the price hike to NPC resources

                        if (!rd.ValidFailsafe)
                            amount = 0;

                        break;
                    }
                case StockType.Single:
                    {
                        amount = GetTotalAmount(rd.Type) % 100;
                        break;
                    }
                case StockType.Bunch:
                    {
                        name = rd.BunchName;
                        price *= 100.0;
                        amount = GetTotalAmount(rd.Type) / 100;

                        object obj = gbi.GetDisplayObject();

                        if (obj is Item)
                            ((Item)obj).Name = name;
                        else if (obj is Mobile)
                            ((Mobile)obj).Name = name;

                        break;
                    }
                default: amount = 0; break; // sanity
            }

            int iprice = (int)price;

            if (iprice <= 0)
                iprice = 1;

            // don't update the type, it shouldn't change!
            gbi.Name = name;
            gbi.Price = iprice;
            gbi.Amount = amount;
            gbi.ItemID = rd.ItemID;
            gbi.Hue = rd.Hue;
        }
        private static Item DeLocalize(Item item, string name)
        {   // remove LabenNumber from item which prevents robust naming the object (client-side)
            Item delocalized_item = new Item(item.ItemID);
            Utility.CopyProperties(delocalized_item, item);
            delocalized_item.Name = name;
            //item.Delete();
            return delocalized_item;
        }
        public static bool AddBuyInfo(BaseVendor vendor, IBuyItemInfo bii, Container cont, ref int count, ref List<BuyItemState> buyInfo, ref ArrayList opls)
        {
            if (!Enabled)
                return false;

            ResourceData rd = GetResource(bii.Type);

            if (rd == null)
                return false; // this is not a pooled resource

            if (GetTotalAmount(rd.Type) <= 0) // we have no resources in the pool
            {
                if (!rd.ValidFailsafe)
                    return true; // don't sell anything

                // NOTE: Only GBI supported; if you use another implementation of IBuyItemInfo, this will crash
                GenericBuyInfo gbiNPC = (GenericBuyInfo)bii;
                UpdateBuyInfo(gbiNPC, rd, StockType.NPC);

                return false; // sell NPC resources
            }

            GenericBuyInfo gbiBunch = GetBuyInfo(rd.Type, true);
            UpdateBuyInfo(gbiBunch, rd, StockType.Bunch);

            if (gbiBunch.Amount > 0)
            {
                IEntity disp = gbiBunch.GetDisplayObject() as IEntity;

                //if (disp is Item)
                //disp = DeLocalize(disp as Item, gbiBunch.Name);

                buyInfo.Add(new BuyItemState(
                    gbiBunch.Name,
                    cont.Serial,
                    disp == null ? (Serial)0x7FC0FFEE : disp.Serial,
                    gbiBunch.Price,
                    gbiBunch.Amount,
                    gbiBunch.ItemID,
                    gbiBunch.Hue));
                count++;

                if (disp is Item)
                    opls.Add(((Item)disp).PropertyList);
                else if (disp is Mobile)
                    opls.Add(((Mobile)disp).PropertyList);
            }

            if (buyInfo.Count >= 250)
                return true; // just in case, check BuyInfo limit

            GenericBuyInfo gbiSingle = GetBuyInfo(rd.Type, false);
            UpdateBuyInfo(gbiSingle, rd, StockType.Single);

            if (gbiSingle.Amount > 0)
            {
                IEntity disp = gbiSingle.GetDisplayObject() as IEntity;

                buyInfo.Add(new BuyItemState(
                    gbiSingle.Name,
                    cont.Serial,
                    disp == null ? (Serial)0x7FC0FFEE : disp.Serial,
                    gbiSingle.Price,
                    gbiSingle.Amount,
                    gbiSingle.ItemID,
                    gbiSingle.Hue));
                count++;

                if (disp is Item)
                    opls.Add(((Item)disp).PropertyList);
                else if (disp is Mobile)
                    opls.Add(((Mobile)disp).PropertyList);
            }

            return true;
        }

        public static bool OnVendorBuy(BaseVendor vendor, Mobile buyer, ref IBuyItemInfo bii, ref int amount)
        {
            if (!Enabled)
                return false;

            ResourceData rd = GetResource(bii.Type);

            if (rd == null)
                return false; // this is not a pooled resource

            // NOTE: Only GBI supported; if you use another implementation of IBuyItemInfo, this will crash
            GenericBuyInfo gbi = (GenericBuyInfo)bii;

            bool bunch;

            if (LookupBuyInfo(gbi.GetDisplayObject(), out bunch) != gbi)
            {
                UpdateBuyInfo(gbi, rd, StockType.NPC); // it's neither the single gbi nor the bunch gbi, it must be the vendor's own gbi
                return false;
            }

            UpdateBuyInfo(gbi, rd, bunch ? StockType.Bunch : StockType.Single);

            if (amount > gbi.Amount)
                amount = gbi.Amount;

            if (amount <= 0)
                return true; // there is nothing for us to buy, do nothing

            if (bunch)
                amount *= 100;

            ResourceConsignment[] toBuy = Extract(rd.Type, amount, rd.SmallestFirstFactor);

            // it could be that the total amount in 'toBuy' doesn't match 'amount' - let's recalculate it
            amount = 0;
            for (int i = 0; i < toBuy.Length; i++)
                amount += toBuy[i].Amount;

            int newAmount = GetTotalAmount(rd.Type);

            ResourceTransaction rt = new ResourceTransaction(TransactionType.Purchase);

            rt.ResName = rd.Name;
            rt.Amount = amount;
            rt.Price = rd.ResalePrice;
            rt.NewAmount = newAmount;
            rt.VendorID = vendor.Serial;

            ResourceLogger.Add(rt, buyer);

            foreach (ResourceConsignment rc in toBuy)
            {
                double addToDebt = rc.Amount * rc.Price * m_PaymentFactor;

                if (rc.Seller == null || rc.Seller.Deleted)
                {
                    BountySystem.BountyKeeper.LBFund += (int)addToDebt;
                    continue;
                }

                // do this here instead of when writing check so player may know how much is waiting
                ResourceTransaction rtpay = new ResourceTransaction(TransactionType.Payment);

                rtpay.ResName = rd.Name;
                rtpay.Amount = rc.Amount;
                rtpay.Price = rc.Price * m_PaymentFactor;
                rtpay.NewAmount = newAmount;
                rtpay.VendorID = vendor.Serial;

                ResourceLogger.Add(rtpay, rc.Seller);

                if (m_Debts.ContainsKey(rc.Seller))
                    m_Debts[rc.Seller] += addToDebt;
                else
                    m_Debts[rc.Seller] = addToDebt;
            }

            PayDebts();

            return true;
        }

        private static ResourceConsignment[] Extract(Type type, int amount, double smallestFirstFactor)
        {
            List<ResourceConsignment> rcList = GetConsignmentList(type);

            int smallest = (int)Math.Ceiling(amount * smallestFirstFactor);
            int communal = amount - smallest;

            List<ResourceConsignment> extract = new List<ResourceConsignment>();

            // take 'smallest' from the first entries in RCList, where RCList is sorted in ascending order by amount
            for (int i = 0; i < rcList.Count && smallest > 0; i++)
            {
                ResourceConsignment rc = rcList[i];

                if (rc.Amount <= 0)
                    continue;

                int toTake = Math.Min(smallest, rc.Amount);

                smallest -= toTake;
                rc.Amount -= toTake;

                extract.Add(new ResourceConsignment(rc.Type, toTake, rc.Price, rc.Seller));
            }

            // take 'communal' from all entries in RCList, but from every entry, only take a portion
            for (int i = 0; i < rcList.Count && communal > 0; i++)
            {
                ResourceConsignment rc = rcList[i];

                if (rc.Amount <= 0)
                    continue;

                int toTake = Math.Max(1, Math.Min(communal / (rcList.Count - i), rc.Amount));

                communal -= toTake;
                rc.Amount -= toTake;

                extract.Add(new ResourceConsignment(rc.Type, toTake, rc.Price, rc.Seller));
            }

            rcList.Sort(); // sort in ascending order by amount

            while (rcList.Count > 0 && rcList[0].Amount <= 0)
                rcList.RemoveAt(0);

            return extract.ToArray();
        }

        public static int CheckCap = 1000000; // cap of bank checks

        private static void PayDebts()
        {
            List<Mobile> keys = new List<Mobile>(m_Debts.Keys);

            foreach (Mobile m in keys)
            {
                double debt = m_Debts[m];

                if (debt >= 1.0)
                {
                    int toGive = (int)debt;

                    Container bank = m.BankBox;

                    Item[] checks = bank.FindItemsByType(typeof(PaymentCheck), false);

                    for (int i = 0; i < checks.Length && toGive > 0; i++)
                    {
                        PaymentCheck check = (PaymentCheck)checks[i];

                        int worth = Math.Min(toGive, CheckCap - check.Worth);

                        if (worth > 0)
                        {
                            m_Debts[m] -= worth;

                            check.Worth += worth;

                            toGive -= worth;
                        }
                    }

                    while (toGive > 0 && bank.Items.Count < bank.MaxItems)
                    {
                        int worth = Math.Min(toGive, CheckCap);

                        m_Debts[m] -= worth;

                        bank.DropItem(new PaymentCheck(worth));

                        toGive -= worth;
                    }
                }

                if (debt <= 0.0)
                    m_Debts.Remove(m);
            }
        }

        public static bool IsConsignmentPurchase(List<BuyItemResponse> list)
        {
            if (!Enabled)
                return false;

            foreach (BuyItemResponse buy in list)
            {
                object obj = World.FindEntity(buy.Serial);

                if (obj == null)
                    continue;

                if (LookupBuyInfo(obj) != null)
                    return true;
            }

            return false;
        }

        #endregion

        public static string DescribeInvestment(Type type, Mobile m)
        {
            List<ResourceConsignment> rcList = GetConsignmentList(type);

            int totalAmount = 0;
            double totalPrice = 0;

            foreach (ResourceConsignment rc in rcList)
            {
                if (rc.Seller != m || rc.Amount <= 0)
                    continue;

                totalAmount += rc.Amount;
                totalPrice += rc.Price * rc.Amount;
            }

            if (totalAmount == 0)
                return "None";

            double avgPrice = totalPrice / totalAmount;

            return String.Format("{0} at avg {1:F2} gp each", totalAmount, avgPrice);
        }
        [CallPriority(1)]   // must come after the BaseVendor StandardPricing database is loaded
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(EventSink_OnWorldLoad);
            EventSink.PreWorldLoad += new PreWorldLoadEventHandler(EventSink_OnPreWorldLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(EventSink_OnWorldSave);
        }
        private static void EventSink_OnPreWorldLoad()
        {
            m_PaymentFactor = 1;
            m_FailsafePriceHike = 1;

            m_Resources = new Dictionary<Type, ResourceData>();
            m_Equivalents = new Dictionary<Type, Type>();
            m_Consignments = new Dictionary<Type, List<ResourceConsignment>>();
            m_Debts = new Dictionary<Mobile, double>();

            Init();
        }
        private static void EventSink_OnWorldLoad()
        {
            Load();
        }

        public static void EventSink_OnWorldSave(WorldSaveEventArgs args)
        {
            Save();
        }

        #region Data Management

        private static int m_ConfigVersion = -1;

        private static void Init()
        {
            Console.Write("Resource Pool Configuring...");
            int read = LoadConfig(Path.Combine(Core.DataDirectory, "ResourcePool/config.xml"));
            Console.WriteLine("Read {0} elements", read);
        }

        private static void Save()
        {
            Console.WriteLine("Resource Pool Saving...");

            if (!Directory.Exists("Saves/ResourcePool"))
                Directory.CreateDirectory("Saves/ResourcePool");

            SaveConfig("Saves/ResourcePool/config.xml");
            SaveConsignments("Saves/ResourcePool/Consignments.dat");
            SaveDebts("Saves/ResourcePool/Debts.dat");
        }

        private static void Load()
        {
            if (!Directory.Exists("Saves/ResourcePool"))
                return;

            Console.Write("Resource Pool Loading...");
            int read = 0;
            read += LoadConfig("Saves/ResourcePool/config.xml");
            read += LoadConsignments("Saves/ResourcePool/Consignments.dat");
            read += LoadDebts("Saves/ResourcePool/Debts.dat");

            Console.WriteLine("Read {0} elements", read);
        }

        private static void SaveConfig(string fileName)
        {
            XmlTextWriter writer = new XmlTextWriter(fileName, System.Text.Encoding.Default);
            writer.Formatting = Formatting.Indented;

            writer.WriteStartDocument(true);
            writer.WriteStartElement("ResourcePool");
            writer.WriteAttributeString("version", "3");

            try
            {
                writer.WriteElementString("PaymentFactor", m_PaymentFactor.ToString("R"));
                writer.WriteElementString("FailsafePriceHike", m_FailsafePriceHike.ToString("R"));

                foreach (KeyValuePair<Type, ResourceData> kvp in m_Resources)
                    kvp.Value.Save(writer);
            }
            finally { writer.WriteEndDocument(); }

            writer.Close();
        }

        private static int LoadConfig(string fileName)
        {
            int read = 0;
            if (File.Exists(fileName) == false || new FileInfo(fileName).Length == 0)
            {
                Utility.ConsoleWriteLine(String.Format("ResourcePool Initializing: \"{0}\".", Path.GetFileName(fileName)), ConsoleColor.Yellow);
                SaveConfig(fileName);
                if (File.Exists(fileName) == false)
                {
                    Core.LoggerShortcuts.BootError(String.Format("Error while reading ResourcePool information from \"{0}\".", fileName));
                    return 0;
                }
            }

            XmlTextReader reader = new XmlTextReader(fileName);
            reader.WhitespaceHandling = WhitespaceHandling.None;
            reader.MoveToContent();

            m_ConfigVersion = Int32.Parse(reader.GetAttribute("version"));
            reader.ReadStartElement("ResourcePool");
            read++;

            switch (m_ConfigVersion)
            {
                case 3:
                case 2:
                case 1:
                case 0:
                    {
                        m_PaymentFactor = Double.Parse(reader.ReadElementString("PaymentFactor"));
                        m_FailsafePriceHike = Double.Parse(reader.ReadElementString("FailsafePriceHike"));

                        while (reader.LocalName == "ResourceData")
                        {
                            ResourceData rd = new ResourceData(reader);

                            m_Resources[rd.Type] = rd;
                        }

                        if (m_ConfigVersion < 3)
                        {
                            while (reader.LocalName == "RDRedirect")
                                LoadRDRedirect(reader);
                        }

                        break;
                    }
                default: throw new Exception("ResourcePool Error: Invalid version of config file (\"ResourcePool/config.xml\").");
            }

            reader.ReadEndElement();

            reader.Close();
            return read;
        }

        private static void LoadRDRedirect(XmlTextReader reader) // legacy
        {
            int version = Int32.Parse(reader.GetAttribute("version"));
            reader.ReadStartElement("RDRedirect");

            switch (version)
            {
                case 0:
                    {
                        double amountFactor = Double.Parse(reader.ReadElementString("AmountFactor"));
                        double priceFactor = Double.Parse(reader.ReadElementString("PriceFactor"));
                        string redirectTypeName = reader.ReadElementString("Redirect");
                        Type redirectType = Type.GetType(redirectTypeName);
                        string message = reader.ReadElementString("Message");

                        string typeName = reader.ReadElementString("Type");
                        Type type = Type.GetType(typeName);

                        if (redirectType == null)
                            throw new Exception(String.Format("ResourcePool Error: Resource type \"{0}\" does not exist.", redirectTypeName));

                        if (type == null)
                            throw new Exception(String.Format("ResourcePool Error: Equivalent resource type \"{0}\" does not exist.", typeName));

                        ResourceData rd = GetResource(redirectType);

                        if (rd == null)
                            throw new Exception(String.Format("ResourcePool Error: Type \"{0}\" is not a pooled resource.", redirectTypeName));

                        rd.EquivalentResources.Add(new EquivalentResource(type, amountFactor, priceFactor, message));

                        m_Equivalents[type] = redirectType;

                        break;
                    }
                default:
                    {
                        throw new Exception("Error loading RDRedirect: Invalid save version");
                    }
            }

            reader.ReadEndElement();
        }

        private static void SaveConsignments(string fileName)
        {
            BinaryFileWriter writer = new BinaryFileWriter(new FileStream(fileName, FileMode.Create, FileAccess.Write), true);

            List<ResourceConsignment> toWrite = new List<ResourceConsignment>();

            foreach (KeyValuePair<Type, List<ResourceConsignment>> kvp in m_Consignments)
            {
                foreach (ResourceConsignment rc in kvp.Value)
                {
                    if (!SaveMobile(rc.Seller) || rc.Amount <= 0)
                        continue;

                    toWrite.Add(rc);
                }
            }

            writer.Write((int)1); // version
            writer.Write((int)toWrite.Count);

            foreach (ResourceConsignment rc in toWrite)
                rc.Serialize(writer);

            writer.Close();
        }

        private static int LoadConsignments(string fileName)
        {
            int read = 0;
            if (File.Exists(fileName) == false || new FileInfo(fileName).Length == 0)
            {
                Utility.ConsoleWriteLine(String.Format("ResourcePool Initializing: \"{0}\".", Path.GetFileName(fileName)), ConsoleColor.Yellow);
                SaveConsignments(fileName);
                if (File.Exists(fileName) == false)
                {
                    Core.LoggerShortcuts.BootError(String.Format("Error while reading ResourcePool information from \"{0}\".", fileName));
                    return 0;
                }
            }

            BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Read)));

            int version = reader.ReadInt();
            int count = (version >= 1 ? reader.ReadInt() : -1);

            int i = 0;

            while ((count == -1) ? (!reader.End()) : (i++ < count))
            {
                ResourceConsignment rc = new ResourceConsignment(reader);
                read++;
                if (rc.Seller != null)
                {
                    List<ResourceConsignment> rcList;

                    if (!m_Consignments.TryGetValue(rc.Type, out rcList))
                        m_Consignments[rc.Type] = rcList = new List<ResourceConsignment>();

                    rcList.Add(rc);
                }
            }

            reader.Close();
            return read;
        }

        private static void SaveDebts(string fileName)
        {
            BinaryFileWriter writer = new BinaryFileWriter(new FileStream(fileName, FileMode.Create, FileAccess.Write), true);

            List<Mobile> toWrite = new List<Mobile>();

            foreach (KeyValuePair<Mobile, double> kvp in m_Debts)
            {
                if (!SaveMobile(kvp.Key) || kvp.Value <= 0.0)
                    continue;

                toWrite.Add(kvp.Key);
            }

            writer.Write((int)1); // version
            writer.Write((int)toWrite.Count);

            foreach (Mobile m in toWrite)
            {
                writer.Write((Mobile)m);
                writer.Write((double)m_Debts[m]);
            }

            writer.Close();
        }

        private static int LoadDebts(string fileName)
        {
            int read = 0;
            //if (!File.Exists(fileName))
            //return;
            if (File.Exists(fileName) == false || new FileInfo(fileName).Length == 0)
            {
                Utility.ConsoleWriteLine(String.Format("ResourcePool Initializing: \"{0}\".", Path.GetFileName(fileName)), ConsoleColor.Yellow);
                SaveDebts(fileName);
                if (File.Exists(fileName) == false)
                {
                    Core.LoggerShortcuts.BootError(String.Format("Error while reading ResourcePool information from \"{0}\".", fileName));
                    return 0;
                }
            }

            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            BinaryFileReader reader = new BinaryFileReader(new BinaryReader(fileStream));

            int version = (m_ConfigVersion >= 2 ? reader.ReadInt() : 0);
            int count = (m_ConfigVersion >= 1 ? reader.ReadInt() : -1);

            int i = 0;

            while ((count == -1) ? (!reader.End()) : (i < count))
            {
                Mobile m = reader.ReadMobile();
                double debt = reader.ReadDouble();
                read++;
                if (m != null)
                    m_Debts[m] = debt;

                i++;
            }

            reader.Close();
            return read;
        }

        private static bool SaveMobile(Mobile m)
        {
            return /*!m.Deleted &&*/ m is PlayerMobile; // mobile serials can get reassigned to non PlayerMobiles (can they??)
        }

        #endregion

        [Constructable]
        public ResourcePool()
            : base(0x1F14)
        {
            Name = "Resource Pool";
            Weight = 1.0;
            Hue = 0x53;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Counselor)
                from.SendGump(new PropertiesGump(from, this));
        }

        public ResourcePool(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
}