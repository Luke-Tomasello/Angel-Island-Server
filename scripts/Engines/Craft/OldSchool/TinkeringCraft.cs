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

/* Scripts\Engines\Craft\OldSchool\TinkeringCraft.cs
 * ChangeLog:
 *	06/21/11, Adam
 *		Begin work on old school craft system
 */

using Server.Engines.Craft;
using Server.Items;
using Server.Menus.ItemLists;
using System;
using System.Collections.Generic;

namespace Server.Engines.OldSchoolCraft
{
    public class TinkeringCraft : SkillCraft
    {
        #region Constants
        public enum Context
        {
            TopMenu,            //	i.e., Repair, Smelt Tools, Armor, Weapons
            PickResource,       // select the ingots
            PickArmorClass,     // Ringmail, Chainmail, Axes, Polearms, etc.
            PickArmorPiece,     // gloves, tunic, chest, etc.
            PickShieldClass,
            PickToolPiece,
            PickWeaponClass,
            PickWeaponPiece,
        }

        public enum TopMenuSelectionID
        {
            // top level 'what do you want to do' menu operations
            Tools = 0xF9F,          // scissors
            Parts = 0x1053,         // gears (0x1054)
            Utensils = 0x13F6,      // butcher knife
            Miscellaneous = 0x1011, // keyring
            Jewelry = 0x1089,       // gold beaded necklace
            MakeLast = 0xbc7,       // not used
        }

        public enum ArmorMenuSelectionID
        {
            // select the class of armor you want to make
            Ringmail = 0x13ec,
            Chainmail = 0x13bf,
            Platemail = 0x1415,
            Helmets = 0x1412,
        }

        public enum WeaponMenuSelectionID
        {
            // select the class of weapon you want to make
            Bladed = 0x13b9,
            Axes = 0xf49,
            Polearms = 0xf4d,
            Bashing = 0x1407,
        }

        public enum ToolMenuSelectionID
        {
            // select the class of shield you want to make
            Tools = 0xF9F,
        }
        #endregion

        public TinkeringCraft(OldSchoolCraft craft)
            : base(craft)
        {
        }

        public void DoCraft()
        {
            // entry point and main error catchall
            try
            {
                // init the system data
                InitSystemData();

                ShowTopMenu();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        #region Item Sorter
        private static int CompareItemsByIngotCostAndName(itemTable x, itemTable y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the 
                    // lengths of the two strings.
                    //
                    int retval = x.Amount.CompareTo(y.Amount);

                    if (retval != 0)
                        return retval;

                    // secondary sort on name
                    return (x.Name as string).CompareTo(y.Name as string);
                }
            }
        }
        private static int CompareItemsByGroup(itemTable x, itemTable y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal. 
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater. 
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the 
                    // lengths of the two strings.
                    //
                    int retval = x.GroupName.CompareTo(y.GroupName);
                    return retval;
                }
            }
        }
        #endregion

        #region Menus and Menu Handlers

        #region TopMenu
        public void ShowTopMenu()
        {
            // build a menu of the different Blacksmithy Options
            //	i.e., Repair, Smelt Tools, Armor, Weapons
            List<Selection> temp = new List<Selection>();
            temp.Add(new Selection(new ItemListEntry("Tools", (int)TopMenuSelectionID.Tools, 0), (int)Context.TopMenu));
            temp.Add(new Selection(new ItemListEntry("Parts", (int)TopMenuSelectionID.Parts, 0), (int)Context.TopMenu));
            temp.Add(new Selection(new ItemListEntry("Utensils", (int)TopMenuSelectionID.Utensils, 0), (int)Context.TopMenu));
            temp.Add(new Selection(new ItemListEntry("Miscellaneous", (int)TopMenuSelectionID.Miscellaneous, 0), (int)Context.TopMenu));
            temp.Add(new Selection(new ItemListEntry("Jewelry", (int)TopMenuSelectionID.Jewelry, 0), (int)Context.TopMenu));

            /*#region MAKE LAST (turned off)
			// turn off make last since we cannot find any evidence that thie existed in-era
			if (false && Craft.From is Mobiles.PlayerMobile && (Craft.From as Mobiles.PlayerMobile).LastCraftObject is itemTable)
			{
				itemTable found = (Craft.From as Mobiles.PlayerMobile).LastCraftObject as itemTable;
				bool allRequiredSkills = false;
				if (found.CraftItem.GetSuccessChance(Craft.From, typeof(IronIngot), Craft.CraftSystem, false, ref allRequiredSkills) > 0.0 && allRequiredSkills)
				{	// user can make this
					temp.Add(new Selection(new ItemListEntry("Make Last Item", (int)TopMenuSelectionID.MakeLast, 0), (int)Context.TopMenu));
				}
			}
			#endregion*/

            Selection[] bsOptions = temp.ToArray();

            Craft.From.SendMenu(new ItemMenu(this, bsOptions, "What do you want to make?"));
        }

        public void HandleTopMenu(int operation)
        {
            int prompt = 0;
            switch ((TopMenuSelectionID)operation)
            {
                case TopMenuSelectionID.Tools:
                    //prompt = 1044276;	// Target an item to repair.
                    DoToolsClassMenu(null);
                    //break;
                    return;
                case TopMenuSelectionID.Parts:
                    prompt = 1044273;   // Target an item to recycle.
                    break;
                case TopMenuSelectionID.Utensils:
                case TopMenuSelectionID.Miscellaneous:
                case TopMenuSelectionID.Jewelry:
                    prompt = 502928;    // What materials would you like to work with?
                    break;
                case TopMenuSelectionID.MakeLast:
                    itemTable found = (Craft.From as Mobiles.PlayerMobile).LastCraftObject as itemTable;
                    if (found is itemTable)
                    {
                        itemTable[] table = GetGroupTable(found.GroupName);
                        if (table is itemTable[])
                        {
                            MakeItem(found.ItemID, table);
                            return;
                        }
                    }
                    return;
            }
            Craft.From.SendLocalizedMessage(prompt);
            Craft.From.Target = new ItemTarget(this, (int)Context.PickResource, (int)operation);
        }
        #endregion

        #region Armor
        private void DoArmorClassMenu(object targeted)
        {
            ShowClassMenu(Armor, 1011076, 1011079, Context.PickArmorClass);
        }
        public void HandleArmorClassMenu(int operation)
        {
            int group = 0;
            switch ((ArmorMenuSelectionID)operation)
            {
                case ArmorMenuSelectionID.Ringmail:
                    group = 1011076;
                    break;
                case ArmorMenuSelectionID.Chainmail:
                    group = 1011077;
                    break;
                case ArmorMenuSelectionID.Platemail:
                    group = 1011078;
                    break;
                case ArmorMenuSelectionID.Helmets:
                    group = 1011079;
                    break;
            }

            ShowItemMenu(group, Armor, Context.PickArmorPiece);
        }
        public void HandlePickArmorPiece(int operation)
        {
            MakeItem(operation, Armor);
        }
        #endregion

        #region Tools
        private void DoToolsClassMenu(object targeted)
        {
            HandleToolsClassMenu((int)ToolMenuSelectionID.Tools);
        }
        public void HandleToolsClassMenu(int operation)
        {
            int group = 0;
            switch ((ToolMenuSelectionID)operation)
            {
                case ToolMenuSelectionID.Tools:
                    group = 1044046;
                    break;
            }

            ShowItemMenu(group, Tools, Context.PickToolPiece);
        }
        public void HandlePickToolsPiece(int operation)
        {
            MakeItem(operation, Tools);
        }

        #endregion

        #region Weapons
        private void DoWeaponsClassMenu(object targeted)
        {
            ShowClassMenu(Weapons, 1011081, 1011084, Context.PickWeaponClass);
        }
        public void HandleWeaponsClassMenu(int operation)
        {
            int group = 0;
            switch ((WeaponMenuSelectionID)operation)
            {
                case WeaponMenuSelectionID.Bladed:
                    group = 1011081;
                    break;
                case WeaponMenuSelectionID.Axes:
                    group = 1011082;
                    break;
                case WeaponMenuSelectionID.Polearms:
                    group = 1011083;
                    break;
                case WeaponMenuSelectionID.Bashing:
                    group = 1011084;
                    break;
            }

            ShowItemMenu(group, Weapons, Context.PickWeaponPiece);
        }
        public void HandlePickWeaponsPiece(int operation)
        {
            MakeItem(operation, Weapons);
        }
        #endregion

        #endregion

        #region Helpers
        public Type GetResource(int itemID, itemTable[] table)
        {
            itemTable temp = itemTable.Find(table, itemID);

            // recall the selected resource
            CraftContext context = Craft.CraftSystem.GetContext(Craft.From);
            Type type = null;
            if (context != null)
            {
                CraftSubResCol res = (temp.CraftItem.UseSubRes2 ? Craft.CraftSystem.CraftSubRes2 : Craft.CraftSystem.CraftSubRes);
                int resIndex = (temp.CraftItem.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex);

                if (resIndex > -1)
                    type = res.GetAt(resIndex).ItemType;
            }

            return type;
        }
        /// <summary>
        /// Gets the list of indexes into the item table of appropriate items
        /// </summary>
        /// <param name="list"></param>
        /// <param name="table"></param>
        /// <param name="context"></param>
        public void ShowItemMenu(int group, itemTable[] table, Context context)
        {
            // sort item tables on ingot cost - this is the order of display
            //Array.Sort<itemTable>(table, CompareItemsByIngotCostAndName);	// sort by ingot cost then name

            // filter the incoming list on abilility
            List<Selection> selections = new List<Selection>();
            for (int ix = 0; ix < table.Length; ix++)
            {
                itemTable found = table[ix];

                // is it in the right gropup?
                if (found.GroupName == group)
                {
                    // just check and see if they have the resources
                    int resHue = 0; int maxAmount = 0; TextDefinition message = null;
                    if (!found.CraftItem.ConsumeRes(Craft.From, GetResource(found.ItemID, table), Craft.CraftSystem, ref resHue, ref maxAmount, ConsumeType.None, ref message))
                        continue;

                    // can the user make this?
                    bool allRequiredSkills = false;
                    if (found.CraftItem.GetSuccessChance(Craft.From, typeof(IronIngot), Craft.CraftSystem, false, ref allRequiredSkills) > 0.0 && allRequiredSkills)
                        selections.Add(new Selection(new ItemListEntry(string.Format("{0}: {1} {2}", found.Name, found.Amount, "Ingots"), found.ItemID), (int)context));
                }
            }

            Selection[] Options = selections.ToArray();

            // build a menu of the different armor Options
            //	i.e., gloves, leggings, sleeves, etc.
            if (Options.Length > 0)
                Craft.From.SendMenu(new ItemMenu(this, Options, "What do you want to make?"));
            else
                Craft.From.SendLocalizedMessage(500586);    // You do not have enough skill or resources to build anything!
        }

        private void ShowClassMenu(itemTable[] table, int first, int last, Context context)
        {
            // sort item tables on group - this is the order of display
            //Array.Sort<itemTable>(table, CompareItemsByGroup);

            List<Selection> selections = new List<Selection>();
            List<int> groups = new List<int>();
            for (int ix = 0; ix < table.Length; ix++)
            {   // make sure it's armor
                if (!(table[ix].GroupName >= first && table[ix].GroupName <= last))
                    continue;

                bool allRequiredSkills = false;
                if (table[ix].CraftItem.GetSuccessChance(Craft.From, typeof(IronIngot), Craft.CraftSystem, false, ref allRequiredSkills) > 0.0 && allRequiredSkills)
                {
                    // only add unique groups at thie time
                    if (groups.Contains(table[ix].GroupName))
                        continue;

                    // another group tag
                    groups.Add(table[ix].GroupName);

                    // collect all the groups we can make into a list
                    selections.Add(new Selection(new ItemListEntry(GetGroupName(table[ix].GroupName), GetGroupImage(table[ix].GroupName)), (int)context));
                }
            }

            // okay build an array for our list menu
            Selection[] bsOptions = new Selection[selections.Count];
            bsOptions = selections.ToArray();

            // build a menu of the different Blacksmithy Options
            //	i.e., Repair, Smelt Tools, Armor, Weapons
            Craft.From.SendMenu(new ItemMenu(this, bsOptions, "What do you want to make?"));
        }
        public void MakeItem(int itemID, itemTable[] table)
        {   // find the item we want to create
            itemTable temp = itemTable.Find(table, itemID);

            // save the item to make as our Last Item
            if (Craft.From is Mobiles.PlayerMobile)
                (Craft.From as Mobiles.PlayerMobile).LastCraftObject = temp;

            // recall the selected resource
            Type type = GetResource(itemID, table);

            // make it baby!
            temp.CraftItem.Craft(Craft.From, Craft.CraftSystem, type, Craft.Tool);
        }
        private void InitSystemData()
        {
            // Set the overidable material
            Craft.CraftSystem.SetSubRes(typeof(IronIngot), 1044022);

            // Add every material you want the player to be able to chose from
            // This will overide the overidable material
            Craft.CraftSystem.AddSubRes(typeof(IronIngot), 1044022, 00.0, 1044036, 1044267);
            Craft.CraftSystem.AddSubRes(typeof(DullCopperIngot), 1044023, 65.0, 1044036, 1044268);
            Craft.CraftSystem.AddSubRes(typeof(ShadowIronIngot), 1044024, 70.0, 1044036, 1044268);
            Craft.CraftSystem.AddSubRes(typeof(CopperIngot), 1044025, 75.0, 1044036, 1044268);
            Craft.CraftSystem.AddSubRes(typeof(BronzeIngot), 1044026, 80.0, 1044036, 1044268);
            Craft.CraftSystem.AddSubRes(typeof(GoldIngot), 1044027, 85.0, 1044036, 1044268);
            Craft.CraftSystem.AddSubRes(typeof(AgapiteIngot), 1044028, 90.0, 1044036, 1044268);
            Craft.CraftSystem.AddSubRes(typeof(VeriteIngot), 1044029, 95.0, 1044036, 1044268);
            Craft.CraftSystem.AddSubRes(typeof(ValoriteIngot), 1044030, 99.0, 1044036, 1044268);
        }
        private itemTable[] GetGroupTable(int index)
        {
            switch (index)
            {
                case 1011080: return Tools;     // Tools
                case 1011076: return Armor;         // "Ringmail";
                case 1011077: return Armor;         // "Chainmail";
                case 1011078: return Armor;         // "Platemail";
                case 1011079: return Armor;         // "Helmets";
                case 1011081: return Weapons;       // "Bladed";
                case 1011082: return Weapons;       // "Axes";
                case 1011083: return Weapons;       // "Polearms";
                case 1011084: return Weapons;       // "Bashing";
            }

            return null;
        }
        private string GetGroupName(int index)
        {
            switch (index)
            {
                case 1011080: return "Tools";
                case 1011076: return "Ringmail";
                case 1011077: return "Chainmail";
                case 1011078: return "Platemail";
                case 1011079: return "Helmets";
                case 1011081: return "Bladed";
                case 1011082: return "Axes";
                case 1011083: return "Polearms";
                case 1011084: return "Bashing";
            }

            return "error";
        }
        private int GetGroupImage(int index)
        {
            switch (index)
            {
                case 1011080: return 0x1b72;        //	"Tools";
                case 1011076: return 0x13ec;        //	"Ringmail";
                case 1011077: return 0x13bf;        //	"Chainmail";
                case 1011078: return 0x1415;        //	"Platemail";
                case 1011079: return 0x1412;        //	"Helmets";
                case 1011081: return 0x13b9;        //	"Bladed";
                case 1011082: return 0xf49;         //	"Axes";
                case 1011083: return 0xf4d;         //	"Polearms";
                case 1011084: return 0x1407;        //	"Bashing";
            }

            return 0;                               //	"error";
        }
        #endregion

        #region Menu and Item Selection
        public override void OnMenuSelection(int context, int operation)
        {
            switch ((Context)context)
            {
                case TinkeringCraft.Context.TopMenu:
                    HandleTopMenu(operation);
                    break;

                case TinkeringCraft.Context.PickArmorClass:
                    HandleArmorClassMenu(operation);
                    break;

                case TinkeringCraft.Context.PickArmorPiece:
                    HandlePickArmorPiece(operation);
                    break;

                case TinkeringCraft.Context.PickShieldClass:
                    HandleToolsClassMenu(operation);
                    break;

                case TinkeringCraft.Context.PickToolPiece:
                    HandlePickToolsPiece(operation);
                    break;

                case TinkeringCraft.Context.PickWeaponClass:
                    HandleWeaponsClassMenu(operation);
                    break;

                case TinkeringCraft.Context.PickWeaponPiece:
                    HandlePickWeaponsPiece(operation);
                    break;
            }
        }

        public override void OnItemSelection(int context, int operation, object targeted)
        {
            // Handle errors first
            int prompt = 0;
            switch ((Context)context)
            {
                case Context.PickResource:
                    switch ((TopMenuSelectionID)operation)
                    {
                        case TopMenuSelectionID.Tools:
                            if (!(targeted is BaseArmor || targeted is BaseWeapon))
                                prompt = 1044277;   // That item cannot be repaired.
                            else if (!(targeted as Item).IsChildOf(Craft.From.Backpack))
                                prompt = 1044275;   // The item must be in your backpack to repair it.
                            break;
                        case TopMenuSelectionID.Parts:
                            if (!(targeted is BaseArmor || targeted is BaseWeapon))
                                prompt = 1044272;   // You can't melt that down into ingots.
                            /*
							 * Publish - September 22, 1999
							 * The following was released as the September 22nd Publish on September 22, 1999.
							 * Smelting
							 * Items you wish to smelt must be in your back-pack.
							 */
                            else if (!(targeted as Item).IsChildOf(Craft.From.Backpack))
                                prompt = 1044274;   // The item must be in your backpack to recycle it.
                            break;
                        case TopMenuSelectionID.Utensils:
                        case TopMenuSelectionID.Miscellaneous:
                        case TopMenuSelectionID.Jewelry:
                            if (targeted is Item)
                            {   // make sure it's the right resource type and make sure we have the skill to use it
                                CraftSubRes csr = Craft.CraftSystem.CraftSubRes.SearchFor((targeted as Item).GetType());
                                if (csr == null)
                                    prompt = 500586;    // You do not have enough skill or resources to build anything!
                                else if (Craft.From.Skills[SkillName.Blacksmith].Value < csr.RequiredSkill)
                                    prompt = 500586;    // You do not have enough skill or resources to build anything!
                                                        // is the backpack required?
                                else if (!(targeted as Item).IsChildOf(Craft.From.Backpack))
                                    prompt = 1044275;   // The item must be in your backpack to repair it.

                                // remember which resource we selected
                                CraftContext ctx = Craft.CraftSystem.GetContext(Craft.From);
                                if (ctx != null)
                                    ctx.LastResourceIndex = Craft.CraftSystem.CraftSubRes.IndexOf((targeted as Item).GetType());
                            }
                            break;
                    }
                    break;
            }

            // there was a selection error, exit now
            if (prompt != 0)
            {
                Craft.From.SendLocalizedMessage(prompt);
                return;
            }
            else
            {
                switch ((Context)context)
                {
                    case Context.PickResource:
                        switch ((TopMenuSelectionID)operation)
                        {
                            case TopMenuSelectionID.Tools: DoRepair(targeted); return;
                            case TopMenuSelectionID.Parts: DoSmelt(targeted); return;
                            case TopMenuSelectionID.Utensils: DoArmorClassMenu(targeted); return;
                            case TopMenuSelectionID.Miscellaneous: DoToolsClassMenu(targeted); return;
                            case TopMenuSelectionID.Jewelry: DoWeaponsClassMenu(targeted); return;
                        }
                        break;
                }
            }
        }
        #endregion

        #region item table
        /// <summary>
        /// Do not assume these tables will remain in this order. 
        /// These tables are sorted and resorted to supply items in the right order for various purposes
        /// </summary>
        private static itemTable[] Tools = new itemTable[]
        {	// tools marked as updated were updated based upon the table here: http://web.archive.org/web/20000408223135/http://uo.stratics.com/
			// Tools
				itemTable.AddCraft(typeof(Scissors), 1044046, 1023998, 5.0, 55.0, typeof(IronIngot), 1044036, 4, 1044037),	// updated
				itemTable.AddCraft(typeof(MortarPestle), 1044046, 1023739, 20.0, 70.0, typeof(IronIngot), 1044036, 3, 1044037),
                itemTable.AddCraft(typeof(Scorp), 1044046, 1024327, 30.0, 80.0, typeof(IronIngot), 1044036, 2, 1044037),
            itemTable.AddCraft(typeof(TinkerToolsOS), 1044046, 1011218, 10.0, 60.0, typeof(IronIngot), 1044036, 2, 1044037),
                itemTable.AddCraft(typeof(Hatchet), 1044046, 1023907, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037),
                itemTable.AddCraft(typeof(DrawKnife), 1044046, 1024324, 30.0, 80.0, typeof(IronIngot), 1044036, 2, 1044037),
                itemTable.AddCraft(typeof(SewingKit), 1044046, 1023997, 10.0, 70.0, typeof(IronIngot), 1044036, 2, 1044037),
                itemTable.AddCraft(typeof(Saw), 1044046, 1024148, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037),
                itemTable.AddCraft(typeof(DovetailSaw), 1044046, 1024136, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037),
                itemTable.AddCraft(typeof(Froe), 1044046, 1024325, 30.0, 80.0, typeof(IronIngot), 1044036, 2, 1044037),
                itemTable.AddCraft(typeof(Shovel), 1044046, 1023898, 40.0, 90.0, typeof(IronIngot), 1044036, 4, 1044037),
                itemTable.AddCraft(typeof(Hammer), 1044046, 1024138, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037),	// updated
				itemTable.AddCraft(typeof(Tongs), 1044046, 1024028, 35.0, 85.0, typeof(IronIngot), 1044036, 4, 1044037),	// updated
				itemTable.AddCraft(typeof(SmithHammer), 1044046, 1025091, 40.0, 90.0, typeof(IronIngot), 1044036, 4, 1044037),

                itemTable.AddCraft(typeof(SledgeHammer), 1044046, 1011225, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037),

                itemTable.AddCraft(typeof(Inshave), 1044046, 1024326, 30.0, 80.0, typeof(IronIngot), 1044036, 2, 1044037),
                itemTable.AddCraft(typeof(Pickaxe), 1044046, 1023718, 40.0, 90.0, typeof(IronIngot), 1044036, 4, 1044037),
                itemTable.AddCraft(typeof(Lockpick), 1044046, 1025371, 45.0, 95.0, typeof(IronIngot), 1044036, 1, 1044037),
            itemTable.AddCraft(typeof(TinkerTools), 1044046, 1011218, 10.0, 60.0, typeof(IronIngot), 1044036, 2, 1044037),
			//itemTable.AddCraft(typeof(Skillet), 1044046, 1044567, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037),
			//itemTable.AddCraft(typeof(FlourSifter), 1044046, 1024158, 50.0, 100.0, typeof(IronIngot), 1044036, 3, 1044037),
			//itemTable.AddCraft(typeof(FletcherTools), 1044046, 1044166, 35.0, 85.0, typeof(IronIngot), 1044036, 3, 1044037),
			//itemTable.AddCraft(typeof(MapmakersPen), 1044046, 1044167, 25.0, 75.0, typeof(IronIngot), 1044036, 1, 1044037),
			//itemTable.AddCraft(typeof(ScribesPen), 1044046, 1044168, 25.0, 75.0, typeof(IronIngot), 1044036, 1, 1044037),
		};
        private static itemTable[] Armor = new itemTable[]
        {
			// Ringmail
			itemTable.AddCraft(typeof(RingmailGloves), 1011076, 1025099, 12.0, 62.0, typeof(IronIngot), 1044036, 10, 1044037),
            itemTable.AddCraft(typeof(RingmailLegs), 1011076, 1025104, 19.4, 69.4, typeof(IronIngot), 1044036, 16, 1044037),
            itemTable.AddCraft(typeof(RingmailArms), 1011076, 1025103, 16.9, 66.9, typeof(IronIngot), 1044036, 14, 1044037),
            itemTable.AddCraft(typeof(RingmailChest), 1011076, 1025100, 21.9, 71.9, typeof(IronIngot), 1044036, 18, 1044037),
			// Chainmail
			itemTable.AddCraft(typeof(ChainCoif), 1011077, 1025051, 14.5, 64.5, typeof(IronIngot), 1044036, 10, 1044037),
            itemTable.AddCraft(typeof(ChainLegs), 1011077, 1025054, 36.7, 86.7, typeof(IronIngot), 1044036, 18, 1044037),
            itemTable.AddCraft(typeof(ChainChest), 1011077, 1025055, 39.1, 89.1, typeof(IronIngot), 1044036, 20, 1044037),
			// Platemail
			itemTable.AddCraft(typeof(PlateArms), 1011078, 1025136, 66.3, 116.3, typeof(IronIngot), 1044036, 18, 1044037),
            itemTable.AddCraft(typeof(PlateGloves), 1011078, 1025140, 58.9, 108.9, typeof(IronIngot), 1044036, 12, 1044037),
            itemTable.AddCraft(typeof(PlateGorget), 1011078, 1025139, 56.4, 106.4, typeof(IronIngot), 1044036, 10, 1044037),
            itemTable.AddCraft(typeof(PlateLegs), 1011078, 1025137, 68.8, 118.8, typeof(IronIngot), 1044036, 20, 1044037),
            itemTable.AddCraft(typeof(PlateChest), 1011078, 1046431, 71.2, 121.2, typeof(IronIngot), 1044036, 25, 1044037),
            itemTable.AddCraft(typeof(FemalePlateChest), 1011078, 1046430, 44.1, 94.1, typeof(IronIngot), 1044036, 20, 1044037),
			// Helmets
			itemTable.AddCraft(typeof(Bascinet), 1011079, 1025132, 8.3, 58.3, typeof(IronIngot), 1044036, 15, 1044037),
            itemTable.AddCraft(typeof(CloseHelm), 1011079, 1025128, 37.9, 87.9, typeof(IronIngot), 1044036, 15, 1044037),
            itemTable.AddCraft(typeof(Helmet), 1011079, 1025130, 37.9, 87.9, typeof(IronIngot), 1044036, 15, 1044037),
            itemTable.AddCraft(typeof(NorseHelm), 1011079, 1025134, 37.9, 87.9, typeof(IronIngot), 1044036, 15, 1044037),
            itemTable.AddCraft(typeof(PlateHelm), 1011079, 1025138, 62.6, 112.6, typeof(IronIngot), 1044036, 15, 1044037)
        };
        private static itemTable[] Weapons = new itemTable[]
        {
			// Bladed
			itemTable.AddCraft(typeof(Broadsword), 1011081, 1023934, 35.4, 85.4, typeof(IronIngot), 1044036, 10, 1044037),
            itemTable.AddCraft(typeof(Cutlass), 1011081, 1025185, 24.3, 74.3, typeof(IronIngot), 1044036, 8, 1044037),
            itemTable.AddCraft(typeof(Dagger), 1011081, 1023921, -0.4, 49.6, typeof(IronIngot), 1044036, 3, 1044037),
            itemTable.AddCraft(typeof(Katana), 1011081, 1025119, 44.1, 94.1, typeof(IronIngot), 1044036, 8, 1044037),
            itemTable.AddCraft(typeof(Kryss), 1011081, 1025121, 36.7, 86.7, typeof(IronIngot), 1044036, 8, 1044037),
            itemTable.AddCraft(typeof(Longsword), 1011081, 1023937, 28.0, 78.0, typeof(IronIngot), 1044036, 12, 1044037),
            itemTable.AddCraft(typeof(Scimitar), 1011081, 1025046, 31.7, 81.7, typeof(IronIngot), 1044036, 10, 1044037),
            itemTable.AddCraft(typeof(VikingSword), 1011081, 1025049, 24.3, 74.3, typeof(IronIngot), 1044036, 14, 1044037),
			// Axes
			itemTable.AddCraft(typeof(Axe), 1011082, 1023913, 34.2, 84.2, typeof(IronIngot), 1044036, 14, 1044037),
            itemTable.AddCraft(typeof(BattleAxe), 1011082, 1023911, 30.5, 80.5, typeof(IronIngot), 1044036, 14, 1044037),
            itemTable.AddCraft(typeof(DoubleAxe), 1011082, 1023915, 29.3, 79.3, typeof(IronIngot), 1044036, 12, 1044037),
            itemTable.AddCraft(typeof(ExecutionersAxe), 1011082, 1023909, 34.2, 84.2, typeof(IronIngot), 1044036, 14, 1044037),
            itemTable.AddCraft(typeof(LargeBattleAxe), 1011082, 1025115, 28.0, 78.0, typeof(IronIngot), 1044036, 12, 1044037),
            itemTable.AddCraft(typeof(TwoHandedAxe), 1011082, 1025187, 33.0, 83.0, typeof(IronIngot), 1044036, 16, 1044037),
            itemTable.AddCraft(typeof(WarAxe), 1011082, 1025040, 39.1, 89.1, typeof(IronIngot), 1044036, 16, 1044037),
			// Pole Arms
			itemTable.AddCraft(typeof(Bardiche), 1011083, 1023917, 31.7, 81.7, typeof(IronIngot), 1044036, 18, 1044037),
            itemTable.AddCraft(typeof(Halberd), 1011083, 1025183, 39.1, 89.1, typeof(IronIngot), 1044036, 20, 1044037),
            itemTable.AddCraft(typeof(ShortSpear), 1011083, 1025123, 45.3, 95.3, typeof(IronIngot), 1044036, 6, 1044037),
            itemTable.AddCraft(typeof(Spear), 1011083, 1023938, 49.0, 99.0, typeof(IronIngot), 1044036, 12, 1044037),
            itemTable.AddCraft(typeof(WarFork), 1011083, 1025125, 42.9, 92.9, typeof(IronIngot), 1044036, 12, 1044037),
			// Bashing
			itemTable.AddCraft(typeof(HammerPick), 1011084, 1025181, 34.2, 84.2, typeof(IronIngot), 1044036, 16, 1044037),
            itemTable.AddCraft(typeof(Mace), 1011084, 1023932, 14.5, 64.5, typeof(IronIngot), 1044036, 6, 1044037),
            itemTable.AddCraft(typeof(Maul), 1011084, 1025179, 19.4, 69.4, typeof(IronIngot), 1044036, 10, 1044037),
            itemTable.AddCraft(typeof(WarMace), 1011084, 1025127, 28.0, 78.0, typeof(IronIngot), 1044036, 14, 1044037),
            itemTable.AddCraft(typeof(WarHammer), 1011084, 1025177, 34.2, 84.2, typeof(IronIngot), 1044036, 16, 1044037)
        };
        #endregion

        #region Repair, Smelt terminal functions
        #region Repair
        private void DoRepair(object targeted)
        {
            int number = 0;
            bool usingDeed = false;
            //bool toDelete = false;
            Mobile from = m_from;

            if (targeted is BaseWeapon)
            {
                BaseWeapon weapon = (BaseWeapon)targeted;
                SkillName skill = m_craftSystem.MainSkill;
                int toWeaken = 0;

                if (Core.RuleSets.AOSRules())
                {
                    toWeaken = 1;
                }
                else if (skill != SkillName.Tailoring)
                {
                    double skillLevel = (usingDeed) ? m_Deed.SkillLevel : from.Skills[skill].Base;

                    if (skillLevel >= 90.0)
                        toWeaken = 1;
                    else if (skillLevel >= 70.0)
                        toWeaken = 2;
                    else
                        toWeaken = 3;
                }

                if (m_craftSystem.CraftItems.SearchForSubclass(weapon.GetType()) == null && !IsSpecialWeapon(weapon))
                {
                    number = (usingDeed) ? 1061136 : 1044277; // That item cannot be repaired. // You cannot repair that item with this type of repair contract.
                }
                else if (!weapon.IsChildOf(from.Backpack))
                {
                    number = 1044275; // The item must be in your backpack to repair it.
                }
                else if (weapon.MaxHitPoints <= 0 || weapon.HitPoints == weapon.MaxHitPoints)
                {
                    number = 500423; // That is already in full repair.
                }
                else if (weapon.MaxHitPoints <= toWeaken)
                {
                    number = 1044278; // That item has been repaired many times, and will break if repairs are attempted again.
                }
                else
                {
                    if (CheckWeaken(from, skill, weapon.HitPoints, weapon.MaxHitPoints))
                    {
                        weapon.MaxHitPoints -= toWeaken;
                        weapon.HitPoints = Math.Max(0, weapon.HitPoints - toWeaken);
                    }

                    if (CheckRepairDifficulty(from, skill, weapon.HitPoints, weapon.MaxHitPoints))
                    {
                        number = 1044279; // You repair the item.
                        m_craftSystem.PlayCraftEffect(from, obj: null);
                        weapon.HitPoints = weapon.MaxHitPoints;
                    }
                    else
                    {
                        number = (usingDeed) ? 1061137 : 1044280; // You fail to repair the item. [And the contract is destroyed]
                        m_craftSystem.PlayCraftEffect(from, obj: null);
                    }

                    //toDelete = true;
                }
            }
            else if (targeted is BaseArmor)
            {
                BaseArmor armor = (BaseArmor)targeted;
                SkillName skill = m_craftSystem.MainSkill;
                int toWeaken = 0;

                if (Core.RuleSets.AOSRules())
                {
                    toWeaken = 1;
                }
                else if (skill != SkillName.Tailoring)
                {
                    double skillLevel = (usingDeed) ? m_Deed.SkillLevel : from.Skills[skill].Base;

                    if (skillLevel >= 90.0)
                        toWeaken = 1;
                    else if (skillLevel >= 70.0)
                        toWeaken = 2;
                    else
                        toWeaken = 3;
                }

                if (m_craftSystem.CraftItems.SearchForSubclass(armor.GetType()) == null)
                {
                    number = (usingDeed) ? 1061136 : 1044277; // That item cannot be repaired. // You cannot repair that item with this type of repair contract.
                }
                else if (!armor.IsChildOf(from.Backpack))
                {
                    number = 1044275; // The item must be in your backpack to repair it.
                }
                else if (armor.MaxHitPoints <= 0 || armor.HitPoints == armor.MaxHitPoints)
                {
                    number = 500423; // That is already in full repair.
                }
                else if (armor.MaxHitPoints <= toWeaken)
                {
                    number = 1044278; // That item has been repaired many times, and will break if repairs are attempted again.
                }
                else
                {
                    if (CheckWeaken(from, skill, armor.HitPoints, armor.MaxHitPoints))
                    {
                        armor.MaxHitPoints -= toWeaken;
                        armor.HitPoints = Math.Max(0, armor.HitPoints - toWeaken);
                    }

                    if (CheckRepairDifficulty(from, skill, armor.HitPoints, armor.MaxHitPoints))
                    {
                        number = 1044279; // You repair the item.
                        m_craftSystem.PlayCraftEffect(from, obj: null);
                        armor.HitPoints = armor.MaxHitPoints;
                    }
                    else
                    {
                        number = (usingDeed) ? 1061137 : 1044280; // You fail to repair the item. [And the contract is destroyed]
                        m_craftSystem.PlayCraftEffect(from, obj: null);
                    }

                    //toDelete = true;
                }
            }

            // tell the user what happened
            if (number != 0)
                Craft.From.SendLocalizedMessage(number);
        }
        #endregion

        #region Smelt
        private void DoSmelt(object targeted)
        {
            Mobile from = Craft.From;
            BaseTool tool = Craft.Tool;
            CraftSystem craftSystem = Craft.CraftSystem;

            TextDefinition badCraft = craftSystem.CanCraft(from, tool, null);

            if (!TextDefinition.IsNullOrEmpty(badCraft))
            {
                TextDefinition.SendMessageTo(from, badCraft);
                return;
            }
            else
            {
                SmeltResult result = SmeltResult.Invalid;
                bool isStoreBought = false;
                int message;

                if (targeted is BaseArmor)
                {
                    result = Resmelt(from, (BaseArmor)targeted, ((BaseArmor)targeted).Resource);
                    isStoreBought = !((Item)targeted).PlayerCrafted;
                }
                else if (targeted is BaseWeapon)
                {
                    result = Resmelt(from, (BaseWeapon)targeted, ((BaseWeapon)targeted).Resource);
                    isStoreBought = !((Item)targeted).PlayerCrafted;
                }
                else if (targeted is DragonBardingDeed)
                {
                    result = Resmelt(from, (DragonBardingDeed)targeted, ((DragonBardingDeed)targeted).Resource);
                    isStoreBought = false;
                }

                switch (result)
                {
                    default:
                    case SmeltResult.Invalid: message = 1044272; break; // You can't melt that down into ingots.
                    case SmeltResult.NoSkill: message = 1044269; break; // You have no idea how to work this metal.
                    case SmeltResult.Success: message = isStoreBought ? 500418 : 1044270; break; // You melt the item down into ingots.
                }

                from.SendLocalizedMessage(message);
            }
        }
        public enum SmeltResult
        {
            Success,
            Invalid,
            NoSkill
        }
        private SmeltResult Resmelt(Mobile from, Item item, CraftResource resource)
        {
            try
            {
                if (CraftResources.GetType(resource) != CraftResourceType.Metal)
                    return SmeltResult.Invalid;

                CraftResourceInfo info = CraftResources.GetInfo(resource);

                if (info == null || info.ResourceTypes.Length == 0)
                    return SmeltResult.Invalid;

                CraftItem craftItem = Craft.CraftSystem.CraftItems.SearchFor(item.GetType());

                if (craftItem == null || craftItem.Resources.Count == 0)
                    return SmeltResult.Invalid;

                CraftRes craftResource = craftItem.Resources.GetAt(0);

                if (craftResource.Amount < 2)
                    return SmeltResult.Invalid; // Not enough metal to resmelt

                double difficulty = 0.0;

                switch (resource)
                {
                    case CraftResource.DullCopper: difficulty = 65.0; break;
                    case CraftResource.ShadowIron: difficulty = 70.0; break;
                    case CraftResource.Copper: difficulty = 75.0; break;
                    case CraftResource.Bronze: difficulty = 80.0; break;
                    case CraftResource.Gold: difficulty = 85.0; break;
                    case CraftResource.Agapite: difficulty = 90.0; break;
                    case CraftResource.Verite: difficulty = 95.0; break;
                    case CraftResource.Valorite: difficulty = 99.0; break;
                }

                if (difficulty > from.Skills[SkillName.Mining].Value)
                    return SmeltResult.NoSkill;

                Type resourceType = info.ResourceTypes[0];
                Item ingot = (Item)Activator.CreateInstance(resourceType);

                if (item is DragonBardingDeed || (item is BaseArmor && !item.StoreBought) || (item is BaseWeapon && !item.StoreBought) || (item is BaseClothing && !item.StoreBought))
                    ingot.Amount = craftResource.Amount / 2;
                else
                    ingot.Amount = 1;

                /* Publish - September 22, 1999
				 * Items you wish to smelt must be in your back-pack.
				 * Smelting will be tied to the mining skill. The higher your skill in mining, the more ingots you will get back.
				 * The more wear and tear on an item, the less ingots it will return.
				 * Items purchased from an NPC will yield only one ingot when smelted.
				 * http://www.uoguide.com/Publish_-_September_22,_1999
				 */
                if (Core.RuleSets.StandardShardRules())
                {
                    double amount = (double)ingot.Amount;

                    // Smelting will be tied to the mining skill. The higher your skill in mining, the more ingots you will get back.
                    amount = amount / 2 + ((amount / 2) * (from.Skills[SkillName.Mining].Value / 100));

                    // The more wear and tear on an item, the less ingots it will return.
                    double MaxHitPoints = 0;
                    double HitPoints = 0;
                    if (item is BaseWeapon)
                    {
                        MaxHitPoints = (double)(item as BaseWeapon).MaxHitPoints;
                        HitPoints = (double)(item as BaseWeapon).HitPoints;
                    }
                    else if (item is BaseArmor)
                    {
                        MaxHitPoints = (double)(item as BaseArmor).MaxHitPoints;
                        HitPoints = (double)(item as BaseArmor).HitPoints;
                    }

                    // reduce the ingots by the amount of wear on the item. An item that is 50% worn out will reduce the ingots by 50%
                    double difference = ((MaxHitPoints - HitPoints) / MaxHitPoints) * 100.0;
                    if (difference > 0.0)
                        amount *= (100.0 - difference) / 100.0;

                    // okay, adjust ingot output
                    ingot.Amount = (int)Math.Round(amount);

                    if (ingot.Amount < 1)
                        ingot.Amount = 1;
                }

                item.Delete();
                from.AddToBackpack(ingot);

                from.PlaySound(0x2A);
                from.PlaySound(0x240);
                return SmeltResult.Success;
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            return SmeltResult.Success;
        }
        #endregion
        #endregion
    }
}