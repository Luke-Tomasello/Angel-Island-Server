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

/* Scripts/Mobiles/Vendors/NPC/RealEstateBroker.cs
 *  Changelog:
 *  9/17/21, Yoar
 *      Turned on logging of failures in ComputePriceFor() on TC
 *  9/17/21, Yoar
 *      Static housing revamp
 *  9/10/21, Adam (if(!Core.UOTC_CFG))
 *      Turn off logging of failures in ComputePriceFor() on test center untill we fix all the deeds there.
 *      Note: when we copied all the data files from prod to TC, we copied all the production houses and deeds along with it.
 *	9/1/07, Adam
 *		Make ComputePriceFor(HouseDeed deed) static so we can access it publicly
 *	8/11/07, Adam
 *		- Replace 10000000 with PriceError constant
 *		- add assert for invalid price
 *	08/06/2007, plasma
 *		- Initial changelog creation
 *		- Allow StaticDeeds to be sold back!
 */

using Server.Diagnostics;
using Server.Items;
using Server.Multis.Deeds;
using Server.Multis.StaticHousing;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class RealEstateBroker : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public RealEstateBroker()
            : base("the real estate broker")
        {
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.Alive && from.InRange(this, 3))
                return true;

            return base.HandlesOnSpeech(from);
        }

        private DateTime m_NextCheckPack;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (Core.RuleSets.CanBuyHouseRules())
                if (DateTime.UtcNow > m_NextCheckPack && InRange(m, 4) && !InRange(oldLocation, 4) && m.Player)
                {
                    Container pack = m.Backpack;

                    if (pack != null)
                    {
                        m_NextCheckPack = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);

                        Item deed = pack.FindItemByType(typeof(HouseDeed), false);

                        if (deed != null)
                        {
                            // If you have a deed, I can appraise it or buy it from you...
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500605, m.NetState);

                            // Simply hand me a deed to sell it.
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500606, m.NetState);
                        }
                    }
                }

            base.OnMovement(m, oldLocation);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.Mobile.Alive && e.HasKeyword(0x38)) // *appraise*
            {
                PublicOverheadMessage(MessageType.Regular, 0x3B2, 500608); // Which deed would you like appraised?
                e.Mobile.BeginTarget(12, false, TargetFlags.None, new TargetCallback(Appraise_OnTarget));
                e.Handled = true;
            }

            base.OnSpeech(e);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {   // belt and suspenders!
            if (dropped is HouseDeed deed && !deed.Deleted && deed.Map == Map.Internal && deed.Parent == null)
            {
                int price = ComputePriceFor(deed);                      // included Siege markup
                price = AOS.Scale(price, 80);                           // refunds 80% of the purchase price

                if (price > 0 && Core.RuleSets.CanBuyHouseRules())      // check the Publish
                {
                    if (Banker.Deposit(from, price))
                    {
                        // For the deed I have placed gold in your bankbox : 
                        PublicOverheadMessage(MessageType.Regular, 0x3B2, 1008000, AffixType.Append, price.ToString("N0")/*(price.ToString()*/, "");

                        deed.Delete();
                        return true;
                    }
                    else
                    {
                        PublicOverheadMessage(MessageType.Regular, 0x3B2, 500390); // Your bank box is full.
                        return false;
                    }
                }
                else
                {
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 500607); // I'm not interested in that.
                    return false;
                }
            }

            return base.OnDragDrop(from, dropped);
        }

        public void Appraise_OnTarget(Mobile from, object obj)
        {
            if (obj is HouseDeed)
            {
                HouseDeed deed = (HouseDeed)obj;
                int price = ComputePriceFor(deed);
                price = AOS.Scale(price, 80); // refunds 80% of the purchase price

                if (!Core.RuleSets.CanBuyHouseRules() && price > 0)
                {
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, true,
                        String.Format("That deed is worth about {0} gold pieces to someone willing to buy it.", price));
                }
                else if (price > 0)
                {
                    // I will pay you gold for this deed : 
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 1008001, AffixType.Append, price.ToString(), "");

                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 500610); // Simply hand me the deed if you wish to sell it.
                }
                else
                {
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, 500607); // I'm not interested in that.
                }
            }
            else
            {
                PublicOverheadMessage(MessageType.Regular, 0x3B2, 500609); // I can't appraise things I know nothing about...
            }
        }

        public static int ComputePriceFor(HouseDeed deed)
        {
            int price = 0;

            if (deed is HouseDeed)
                price = deed.Price;

            //check for the failsafe price and if so set to 0 - dont want someone getting 8 million back!
            if (price == StaticHouseHelper.PriceError)
                price = 0;

            // Pub 13.6
            // houses are 10x the price for Siege from Pub 13.6 on
            // otherwise, apply the regular x3 price hike for Siege
            // http://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
            if (Core.RuleSets.SiegeStyleRules())
                price = ComputeHousingMarkupForSiege(price);

            // track the error
            if (price == 0)
            {
                LogHelper logger = new LogHelper("HousingExceptions.log", false);
                logger.Log(LogType.Text, string.Format("House price 0 on {0}",
                    Core.UOTC_CFG ? "Test Center" : Core.Server));
                logger.Finish();
            }

            return price;
        }
        public static int ComputeHousingMarkupForSiege(int price)
        {   // Pub 13.6
            // houses are 10x the price for Siege from Pub 13.6 on
            // otherwise, apply the regular x3 price hike for Siege
            // 1/19/2024, Adam. SiegeII: in order to spur the economy on such a small shard, we are adopting the 3x pricing for houses even though it's not era accurate
            // http://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
            if (Core.RuleSets.SiegeStyleRules())
            {
                if (PublishInfo.Publish >= 13.6 && !Core.SiegeII_CFG)
                    price *= 10;
                else
                    price *= 3;
            }

            return price;
        }
        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBRealEstateBroker());
        }

        public RealEstateBroker(Serial serial)
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
}