/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/*
 * Scripts\Items\Books\Defined\StorageUpgradeInfo.cs
 * CHANGELOG:
 * 10/18/2026, Adam
 *  The shard is named by Core.Server (author line and text) instead of "Angel Island" or "Siege", so Upheaval,
 *  Mortalis and Seasons players see their own shard. New page after "Custom Housing": the starting storage (270
 *  lockdowns, 2 secures, 2 lockboxes) and that each secure container uses 125 lockdowns. On Seasons, where custom
 *  houses start at the storage of the closest classic house, both pages say so instead.
 * 10/15/26, Adam
 *  Building permits now stack on top of storage upgrades: replaced the outdated "double your lockbox allowance" text,
 *  and corrected the message players see when a contract cannot upgrade their house.
 * 11/5/21, Yoar
 *  Disabled the information regarding storage tax credits.
 * 9/26/21, Adam
 *  Initial Revision.
 */

namespace Server.Items
{
    public class StorageUpgradeInfo : BaseBook
    {
        private const string TITLE = "Upgrades Explained";
        private const int PAGES = 20;
        private const bool WRITABLE = false;
        private const int PURPLE_BOOK = 0xFF2;

        public static readonly BookContent Content = new BookContent
            (
                "Storage Upgrade Info", Core.Server,
                new BookPageInfo
                (
                    "On " + Core.Server + " there are ",
                    "two different ways to ",
                    "increase your house's ",
                    "storage capacity. Storage ",
                    "upgrade deeds which are ",
                    "a luxury item, and ",
                    "Building Permits for ",
                    "extra Lockboxes. "
                ),
                new BookPageInfo
                (
                    "Storage Upgrades:",
                    "",
                    "These deeds will increase ",
                    "your houses base maximum ",
                    "storage capacity. This will ",
                    "affect your maximum ",
                    "number of secure",
                    "containers, lockboxes, and  "
                ),
                new BookPageInfo
                (
                   "total lock downs. Once ",
                    "purchased, take the ",
                    "deed to your home and ",
                    "double click it. Then ",
                    "target your house sign. ",
                    "You must be under the ",
                    "house sign in order for ",
                    "it to work.        "
                ),
                new BookPageInfo
                (
                    "Modest Upgrade:",
                    "",
                    "500 Lockdowns",
                    "3 Secure Containers",
                    "3 Lockboxes",
                    ""
                ),
                new BookPageInfo
                (
                    "Moderate Upgrade:",
                    "",
                    "900 Lockdowns",
                    "6 Secure Containers",
                    "4 Lockboxes"
                ),
                new BookPageInfo
                (
                    "Premium Upgrade:",
                    "",
                    "1300 Lockdowns",
                    "9 Secure Containers",
                    "5 Lockboxes"
                ),
                new BookPageInfo
                (
                    "Lavish Upgrade:",
                    "",
                    "1950 Lockdowns",
                    "14 Secure Containers",
                    "7 Lockboxes"
                ),
                new BookPageInfo
                (
                    "Deluxe Upgrade:",
                    "",
                    "4076 Lockdowns",
                    "28 Secure Containers",
                    "7 Lockboxes",
                    "",
                    "This contract may only be",
                    "added to large towers,",
                    "keeps and castles."
                ),
                new BookPageInfo
                (
                    "Your investment is safe! ",
                    "",
                    "When you redeed your ",
                    "house, a check for the ",
                    "full cost of your ",
                    "upgrades will be deposited ",
                    "in your bank. "
                ),
                new BookPageInfo
                (
                    "In cases where your ",
                    "house already has equal ",
                    "to or greater than the ",
                    "amount that the upgrade ",
                    "is for, the contract ",
                    "cannot be used. Your ",
                    "storage is never ",
                    "downgraded."
                ),
                new BookPageInfo
                (
                    "Building Permit: Lockbox:",
                    "",
                    "Building permits for ",
                    "lockboxes allow for one ",
                    "extra container to be ",
                    "locked down inside your ",
                    "home for use as a ",
                    "lockbox. Items inside these "
                ),
                new BookPageInfo
                (
                     "lockboxes will not decay. ",
                    "Each permit adds one ",
                    "lockbox, with no limit on ",
                    "how many you may use. ",
                    "Permits are added on top ",
                    "of your storage upgrade, ",
                    "so upgrading never takes ",
                    "them away. "
                ),
                new BookPageInfo
                (
                    "Lockboxes do not qualify as ",
                    "an upgrade, therefore the ",
                    "costs associated with these ",
                    "contracts are not refundable."
                ),
                // Seasons starts custom houses at the storage of the nearest classic house footprint
                //  (StaticHouse.GetClassicStorage); every other shard starts them at small house storage
                Core.RuleSets.SeasonsRules()
                ? new BookPageInfo
                (
                    "Custom Housing:",
                    "",
                    "On " + Core.Server + ", custom ",
                    "housing starts with the ",
                    "storage of the classic ",
                    "house closest to its ",
                    "size, so larger homes ",
                    "begin with more storage. "
                )
                : new BookPageInfo
                (
                    "Custom Housing:",
                    "",
                    "On " + Core.Server + ", custom ",
                    "housing of any size will ",
                    "always start with the ",
                    "lowest possible storage ",
                    "rating in the game; the ",
                    "same as a small house. "
                ),
                Core.RuleSets.SeasonsRules()
                ? new BookPageInfo
                (
                    "For example, a 14x14 ",
                    "house starts like a two ",
                    "story house: 750 ",
                    "lockdowns, 5 secure ",
                    "containers, 3 lockboxes. ",
                    "Each secure container ",
                    "uses 125 of your ",
                    "lockdowns. "
                )
                : new BookPageInfo
                (
                    "That is 270 lockdowns, ",
                    "2 secure containers and ",
                    "2 lockboxes, whatever ",
                    "the size of the house. ",
                    "Each secure container ",
                    "uses 125 of your ",
                    "lockdowns. Storage ",
                    "upgrades expand these. "
                ),
                new BookPageInfo
                (
                     "Therefore, the methods ",
                    "for increasing a home's",
                    "storage capacity noted ",
                    "within this book are ",
                    "relevant to all custom ",
                    "house owners and ",
                    "enthusiasts. It is ",
                    "important to understand "
                ),
                new BookPageInfo
                (
                    "that Storage Upgrade ",
                    "deeds can only increase ",
                    "your storage to their ",
                    "pre-determined maximum ",
                    "values and DO NOT stack ",
                    "with your current storage ",
                    "rating, except for extra ",
                    "lockboxes; building "
                ),
                new BookPageInfo
                (
                    "permits for extra ",
                    "lockboxes are *not* lost ",
                    "when using storage ",
                    "upgrades. This means that ",
                    "regardless of your house's ",
                    "initial storage capacity, ",
                    "every home can have the ",
                    "storage of a castle. "
                ),
                new BookPageInfo
                (
                    "That summarizes ",
                    "everything there is to ",
                    "know about increasing ",
                    "your houses storage ",
                    "capacity on " + Core.Server + ". "
                )
            );

        public override BookContent DefaultContent { get { return Content; } }

        [Constructable]
        public StorageUpgradeInfo()
            : base(PURPLE_BOOK, TITLE, NameList.RandomName("female"), PAGES, WRITABLE)
        {
            Name = TITLE;
        }

        public StorageUpgradeInfo(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}