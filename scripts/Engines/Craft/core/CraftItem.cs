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

/* Scripts/Engines/Craft/Core/CraftItem.cs
 * ChangeLog:
 *  4/16/23, Yoar
 *      Added large/small forges as heat sources.
 *  4/2/23, Yoar
 *      Bolas are now markable (Angel Island only)
 *  3/17/22, Adam (OnBeforeResDelete)
 *      Before actually deleting the resources for the CraftItem, give our craft imp a chance to make use of needed resources before they are lost forever.
 *  1/8/22, Yoar
 *      Removed old, unused, code.
 *      Simplified input structure of CraftRes, CraftGroup constructors using TextEntry.
 *      Added CraftRes.Predicate: Check additional property requirements for the resource.
 *      Rewrote resource grouping/consumption using ItemConsumer.
 *  12/30/21, Yoar
 *      Added UpdateDisplay() method which deals with calculating the ItemID, Hue(, ItemPicture) of the CraftItem.
 *      Added CraftItem.Picture. This is used to draw addons in the CraftGumpItem display.
 *  11/28/21, Yoar
 *      Raised the stack-craft cap from 800 to 60K.
 *  11/22/21, Yoar
 *      Simplified input structure of CraftItem, CraftItem.AddRes using TextEntry.
 *      Added CraftItem.ItemID, CraftItem.ItemHue: Sets the item display in the craft gump.
 *      Added CraftSystem.GiveItem: Instead of adding the item to the player's pack, do something else.
 *  11/17/21, Adam
 *      Replace: new Type[]{ typeof( AshLog ), typeof( YewBoard ) },
 *      With: new Type[]{ typeof( AshLog ), typeof( AshBoard ) },
 *  11/17/21, Yoar
 *      Added GetEquivalentResources.
 *  11/14/21, Yoar
 *      Added ML wood types.
 *  10/15/21, Adam (CompleteCraft)
 *      Change object notice = 0; to object notice = null;
 *      The old way made 'notice' an int and therefore couldn't carry the appropriate messageback to the crafting gump. I.e.,
 *      "You create an exceptional quality item." etc.
 *  10/10/21, Yoar
 *      Crafted UnCutCloth now retains the hue of the cloth that was used to craft it.
 *  9/22/21, Yoar
 *      Now capping the maximum amount of items you can craft when UseAllRes is enabled.
 *  9/19/21, Yoar
 *      Rewrote special dye tub crafting.
 *      Dye crafts no longer give skill gains!
 *  9/19/21, Yoar
 *      Added CraftArgs: Pass additional arguments with which the crafted item is constructed
 *      CustomCraft.CompleteCraft: Changed "out int message" to "out object message" to support strings 
 *	3/16/16, Adam
 *		Do not show the old-school makers mark gump if core is UOAI
 *	04/06/09, plasma
 *		Removed ability to gain from mixing & lightening/darkening dyes completely
 *  01/04/07, Plasma
 *      Fixed special dye tub skill gain exploit      
 *  7/20/06, Rhiannon
 *		Fixed order of precedence bug when setting hitpoints of exceptional and low quality clothing.
 *  7/18/06, Rhiannon
 *		Added default hue of 1001 for undyed cloth gloves.
 *	11/10/05, erlein
 *		Removed assignment of SaveHue property (made obsolete by PlayerCrafted).
 *	10/17/05, erlein
 *		Fixed darkening/lightening and targetting bugs.
 *	10/16/05, erlein
 *		Altered use of special dye tub's "Prepped" property to ascertain whether more dye can be added.
 *		Changed consumption of resources so darken/lighten require 2 per 1-5, 4 per 6-10, 5 per 11-15 etc.
 *	10/15/05, erlein
 *		Added amount limit for special dye tub (on Uses property).
 *		Altered resource consumption to occur after craft in order to control it better.
 *		Created SpecialDyeTubTarget, instanced whenever special dye is crafted and
 *		mutltiple dye tubs exist.
 *	10/15/05, erlein
 *		Re-worked special dye handling to accommodate new dye tub based craft model.
 *	10/15/05, erlein
 *		Added conditions to handle the craft of special dyes.
 *	9/7/05, Adam
 *		In order to keep players from farming new characters for newbie clothes
 *		we are moving this valuable resource into the hands of crafters.
 *		Exceptionally crafted clothes are now newbied. They do however wear out.
 *	02/10/05, erlein
 *		Added initial hits assignment for BaseClothing types.
 *  09/12/05 TK
 *		Added Bolas to Markable Types array, so that they carry crafter's mark.
 *  08/18/05 TK
 *		Made bolas carry over quality.
 *	08/18/05, erlein
 *		Added jewellery to markable types array.
 *		Added extra commands to pass crafter and quality down to BaseJewel.
 *	08/01/05, erlein
 *		Added runebooks and instruments to markable types array.
 *		Added extra commands to pass crafter and quality down to runebook.
 *	07/30/05, erlein
 *		Removed the filling of bookcases (so books are just consumed) and added checks for other
 *		fullbookcase types (randomized when craft lists are established ;)
 *	07/27/05, erlein
 *		Added special messages, filling for bookcases and condition for scribes pen during carpentry craft.
 *	02/25/05, Adam
 *		Make the item and mark it as PlayerCrafted
 *		See:	Item item = Activator.CreateInstance( ItemType ) as Item;
 *				item.PlayerCrafted = true;
 *	1/8/05, mith
 *		CompleteCraft(): Modified Failure consumption to not use all of a resource on failure (i.e. cooking fish and failing doesn't lose all raw fish)
 *		CompleteCraft(): put call to PlayEndingEffect() so that alchemy doesn't consume bottles on failures
 *	1/4/05 smerX
 *		Fixxed issue with crafting exceptional items.
 *	12/30/04, smerX
 *		Players now lose 1/2 to 1/3 of the required resources when failing a craft skill
 * 	10/30/04, Darva
 *		Changed results of player created lockedcontainers to have their
 *		difficulty in line with treasure chests.
 *	9/1/04, Pixie
 *		Now when a tinker traps a container, we make sure that the container
 *		has the TinkerTrapable attribute (vs only checking that it is a TrapableContainer)
 *	8/12/04, mith
 *		ConsumeType(): Added option for ConsumeType.One, used when crafts that set UseAllRes fail (rather than consuming half of the stack of whatever is being crafter)
 *		CompleteCraft(): Added functionality so that items that should only consume one reasource on failure do.
 *		CheckSkills(): Fixed a bug where an exceptional item would be crafted, but the skill check to actually create the item could fail.
 *		CompleteCraft(): Removed a second CheckSkills call that was failing if players had PromptForMark set, so that the item could fail to be created after they were prompted.
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/22/2004, pixie
 *		Changed so tinkers couldn't trap an already trapped container.
 *	5/18/2004
 *		Added handling for the crafting of tinker traps
 */

using Server.Engines.EventResources;
using Server.Factions;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Regions;
using Server.Township;
using System;

namespace Server.Engines.Craft
{
    public enum ConsumeType
    {
        All, Half, Fail, One, None
    }

    public interface ICraftable
    {
        int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue);
    }

    public class CraftItem
    {
        private CraftResCol m_arCraftRes;
        private CraftSkillCol m_arCraftSkill;
        private Type m_Type;

        private object[] m_CraftArgs;

        private TextDefinition m_GroupName;
        private TextDefinition m_Name;

        private int m_ItemID;
        private int m_ItemHue;

        private ItemPicture m_Picture;

        private int m_Mana;
        private int m_Hits;
        private int m_Stam;

        private bool m_UseAllRes;

        private bool m_NeedHeat;
        private bool m_NeedOven;
        private bool m_NeedMill;
        private bool m_NeedWater;   // erl: added for special dyes

        private bool m_UseSubRes2;

        private bool m_ForceNonExceptional;

        public bool ForceNonExceptional
        {
            get { return m_ForceNonExceptional; }
            set { m_ForceNonExceptional = value; }
        }

        public CraftItem(Type type, TextDefinition groupName, TextDefinition name)
        {
            m_arCraftRes = new CraftResCol();
            m_arCraftSkill = new CraftSkillCol();

            m_Type = type;

            m_GroupName = groupName;
            m_Name = name;

            m_ItemID = -1;
        }

        public void AddRes(Type type, TextDefinition name, int amount)
        {
            AddRes(type, name, amount, null, null);
        }

        public void AddRes(Type type, TextDefinition name, int amount, TextDefinition message)
        {
            AddRes(type, name, amount, message, null);
        }

        public void AddRes(Type type, TextDefinition name, int amount, TextDefinition message, Predicate<Item> predicate)
        {
            m_arCraftRes.Add(new CraftRes(type, name, amount, message, predicate));
        }

        public void AddSkill(SkillName skillToMake, double minSkill, double maxSkill)
        {
            CraftSkill craftSkill = new CraftSkill(skillToMake, minSkill, maxSkill);
            m_arCraftSkill.Add(craftSkill);
        }

        public object[] CraftArgs
        {
            get { return m_CraftArgs; }
            set { m_CraftArgs = value; }
        }

        public int Mana
        {
            get { return m_Mana; }
            set { m_Mana = value; }
        }

        public int Hits
        {
            get { return m_Hits; }
            set { m_Hits = value; }
        }

        public int Stam
        {
            get { return m_Stam; }
            set { m_Stam = value; }
        }

        public bool UseSubRes2
        {
            get { return m_UseSubRes2; }
            set { m_UseSubRes2 = value; }
        }

        public bool UseAllRes
        {
            get { return m_UseAllRes; }
            set { m_UseAllRes = value; }
        }

        public bool NeedHeat
        {
            get { return m_NeedHeat; }
            set { m_NeedHeat = value; }
        }

        public bool NeedOven
        {
            get { return m_NeedOven; }
            set { m_NeedOven = value; }
        }

        public bool NeedMill
        {
            get { return m_NeedMill; }
            set { m_NeedMill = value; }
        }

        public bool NeedWater
        {
            get { return m_NeedWater; }
            set { m_NeedWater = value; }
        }

        public Type ItemType
        {
            get { return m_Type; }
        }

        public int ItemID
        {
            get { EnsureDisplay(); return m_ItemID; }
            set { m_ItemID = value; }
        }

        public int ItemHue
        {
            get { EnsureDisplay(); return m_ItemHue; }
            set { m_ItemHue = value; }
        }

        public ItemPicture Picture
        {
            get { EnsureDisplay(); return m_Picture; }
            set { m_Picture = value; }
        }

        private void EnsureDisplay()
        {
            if (m_ItemID != -1)
                return;

            m_ItemID = 0; // indicates that we have a display

            if (m_Picture != null)
                return; // we have a picture, use that

            object[] attrs = m_Type.GetCustomAttributes(typeof(CraftItemIDAttribute), false);

            if (attrs.Length > 0)
            {
                m_ItemID = ((CraftItemIDAttribute)attrs[0]).ItemID;
                return;
            }

            Item item = null;

            try
            {
                item = Activator.CreateInstance(m_Type, m_CraftArgs) as Item;
            }
            catch
            {
            }

            if (item != null)
            {
                if (item is BaseAddonDeed)
                {
                    BaseAddon addon = ((BaseAddonDeed)item).Addon;

                    if (addon != null)
                    {
                        m_Picture = ItemPicture.FromItem(addon);

                        addon.Delete();
                    }
                }
                else if (item is BaseAddon)
                {
                    m_Picture = ItemPicture.FromItem(item);
                }
                else
                {
                    m_ItemID = item.ItemID;
                    m_ItemHue = item.Hue;
                }

                item.Delete();
            }
        }

        public TextDefinition GroupName
        {
            get { return m_GroupName; }
        }

        public TextDefinition Name
        {
            get { return m_Name; }
        }

        public CraftResCol Resources
        {
            get { return m_arCraftRes; }
        }

        public CraftSkillCol Skills
        {
            get { return m_arCraftSkill; }
        }

        public bool ConsumeAttributes(Mobile from, ref TextDefinition message, bool consume)
        {
            bool consumMana = false;
            bool consumHits = false;
            bool consumStam = false;

            if (Hits > 0 && from.Hits < Hits)
            {
                message = "You lack the required hit points to make that.";
                return false;
            }
            else
            {
                consumHits = consume;
            }

            if (Mana > 0 && from.Mana < Mana)
            {
                message = "You lack the required mana to make that.";
                return false;
            }
            else
            {
                consumMana = consume;
            }

            if (Stam > 0 && from.Stam < Stam)
            {
                message = "You lack the required stamina to make that.";
                return false;
            }
            else
            {
                consumStam = consume;
            }

            if (consumMana)
                from.Mana -= Mana;

            if (consumHits)
                from.Hits -= Hits;

            if (consumStam)
                from.Stam -= Stam;

            return true;
        }

        #region TABLES
        private static int[] m_HeatSources = new int[]
            {
                0x461, 0x48E, // Sandstone oven/fireplace
				0x92B, 0x96C, // Stone oven/fireplace
				0xDE3, 0xDE9, // Campfire
				0xFAC, 0xFAC, // Firepit
				0x184A, 0x184C, // Heating stand (left)
				0x184E, 0x1850, // Heating stand (right)
				0x398C, 0x399F,  // Fire field
                0x197A, 0x19A9, // Large Forge 
				0x0FB1, 0x0FB1, // Small Forge
                0x2352, 0x235D, // Home Hearth (east)
                0x2360, 0x236B, // Home Hearth (south)
			};

        public static int[] HeatSources { get { return m_HeatSources; } }

        private static int[] m_Ovens = new int[]
            {
                0x461, 0x46F, // Sandstone oven
				0x92B, 0x93F  // Stone oven
			};

        private static int[] m_Mills = new int[]
            {
                0x1920, 0x1928, // flour mill east
				0x192C, 0x1934 // flour mill south
			};

        private static int[] m_WaterTroughs = new int[]
            {
                0xB41, 0xB42, // Watertrough east
				0xB43, 0xB44  // Watertrough south
			};

        private static Type[] m_MarkableTable = new Type[]
                {
                    typeof( BaseArmor ),
                    typeof( BaseWeapon ),
                    typeof( BaseClothing ),
                    typeof( BaseInstrument ),
                    typeof( DragonBardingDeed ),
                    typeof( BaseTool ),
                    typeof( BaseHarvestTool ),
					/*typeof( FukiyaDarts ), typeof( Shuriken ),*/
					typeof( Spellbook ), typeof( Runebook ),
                    typeof( Bola ), // Angel Island
                };

        private static Type[][] m_TypesTable = new Type[][]
            {
                new Type[]{ typeof( Log ), typeof( Board ) },
                new Type[]{ typeof( HeartwoodLog ), typeof( HeartwoodBoard ) },
                new Type[]{ typeof( BloodwoodLog ), typeof( BloodwoodBoard ) },
                new Type[]{ typeof( FrostwoodLog ), typeof( FrostwoodBoard ) },
                new Type[]{ typeof( OakLog ), typeof( OakBoard ) },
                new Type[]{ typeof( AshLog ), typeof( AshBoard ) },
                new Type[]{ typeof( YewLog ), typeof( YewBoard ) },
                new Type[]{ typeof( Leather ), typeof( Hides ) },
                new Type[]{ typeof( SpinedLeather ), typeof( SpinedHides ) },
                new Type[]{ typeof( HornedLeather ), typeof( HornedHides ) },
                new Type[]{ typeof( BarbedLeather ), typeof( BarbedHides ) },
                new Type[]{ typeof( BlankMap ), typeof( BlankScroll ) },
                new Type[]{ typeof( Cloth ), typeof( UncutCloth ) },
                new Type[]{ typeof( CheeseWheel ), typeof( CheeseWedge ) }
            };

        private static Type[] m_ColoredItemTable = new Type[]
            {
                typeof( BaseWeapon ), typeof( BaseArmor ), typeof( BaseClothing ),
                typeof( BaseJewel ), typeof( DragonBardingDeed ), typeof( UncutCloth ),
                typeof( BaseCraftableItem ), typeof( BaseContainer ), typeof( BaseInstrument ),
            };

        private static Type[] m_ColoredResourceTable = new Type[]
            {
                typeof( BaseIngot ), typeof( BaseOre ),
                typeof( BaseLeather ), typeof( BaseHides ),
                typeof( UncutCloth ), typeof( Cloth ),
                typeof( BaseGranite ), typeof( BaseScales ),
                typeof( BaseBoard ), typeof( BaseLog ),
                typeof( Obsidian ), typeof( Stahlrim ),
            };
        #endregion

        public bool IsMarkable(Type type)
        {
            if (m_ForceNonExceptional)  //Don't even display the stuff for marking if it can't ever be exceptional.
                return false;

            for (int i = 0; i < m_MarkableTable.Length; ++i)
            {
                if (type == m_MarkableTable[i] || type.IsSubclassOf(m_MarkableTable[i]))
                    return true;
            }

            return false;
        }

        public bool RetainsColorFrom(CraftSystem system, Type type)
        {
            if (system.RetainsColorFrom(this, type))
                return true;

            bool inItemTable = false, inResourceTable = false;

            for (int i = 0; !inItemTable && i < m_ColoredItemTable.Length; ++i)
                inItemTable = (m_Type == m_ColoredItemTable[i] || m_Type.IsSubclassOf(m_ColoredItemTable[i]));

            for (int i = 0; inItemTable && !inResourceTable && i < m_ColoredResourceTable.Length; ++i)
                inResourceTable = (type == m_ColoredResourceTable[i] || type.IsSubclassOf(m_ColoredResourceTable[i]));

            return (inItemTable && inResourceTable);
        }

        public static bool Find(Mobile from, int[] itemIDs, int range = 2)
        {
            Map map = from.Map;

            if (map == null)
                return false;

            IPooledEnumerable eable = map.GetItemsInRange(from.Location, range);

            foreach (Item item in eable)
            {
                if ((item.Z + 16) > from.Z && (from.Z + 16) > item.Z && Find(item.ItemID, itemIDs))
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();

            for (int x = -range; x <= range; ++x)
            {
                for (int y = -range; y <= range; ++y)
                {
                    int vx = from.X + x;
                    int vy = from.Y + y;

                    StaticTile[] tiles = map.Tiles.GetStaticTiles(vx, vy, true);

                    for (int i = 0; i < tiles.Length; ++i)
                    {
                        int z = tiles[i].Z;
                        int id = tiles[i].ID & 0x3FFF;

                        if ((z + 16) > from.Z && (from.Z + 16) > z && Find(id, itemIDs))
                            return true;
                    }
                }
            }

            return false;
        }

        public static bool Find(int itemID, int[] itemIDs)
        {
            bool contains = false;

            for (int i = 0; !contains && i < itemIDs.Length; i += 2)
                contains = (itemID >= itemIDs[i] && itemID <= itemIDs[i + 1]);

            return contains;
        }

#if RunUO
        public bool IsQuantityType(Type[][] types)
        {
            for (int i = 0; i < types.Length; ++i)
            {
                Type[] check = types[i];

                for (int j = 0; j < check.Length; ++j)
                {
                    if (typeof(IHasQuantity).IsAssignableFrom(check[j]))
                        return true;
                }
            }

            return false;
        }

        public int ConsumeQuantity(Container cont, Type[][] types, int[] amounts)
        {
            if (types.Length != amounts.Length)
                throw new ArgumentException();

            Item[][] items = new Item[types.Length][];
            int[] totals = new int[types.Length];

            for (int i = 0; i < types.Length; ++i)
            {
                items[i] = cont.FindItemsByType(types[i], true);

                for (int j = 0; j < items[i].Length; ++j)
                {
                    IHasQuantity hq = items[i][j] as IHasQuantity;

                    if (hq == null)
                    {
                        totals[i] += items[i][j].Amount;
                    }
                    else
                    {
                        if (hq is BaseBeverage && ((BaseBeverage)hq).Content != BeverageType.Water)
                            continue;

                        totals[i] += hq.Quantity;
                    }
                }

                if (totals[i] < amounts[i])
                    return i;
            }

            for (int i = 0; i < types.Length; ++i)
            {
                int need = amounts[i];

                for (int j = 0; j < items[i].Length; ++j)
                {
                    Item item = items[i][j];
                    IHasQuantity hq = item as IHasQuantity;

                    if (hq == null)
                    {
                        int theirAmount = item.Amount;

                        if (theirAmount < need)
                        {
                            need -= theirAmount;
                            item.Delete();
                        }
                        else
                        {
                            item.Consume(need);
                            break;
                        }
                    }
                    else
                    {
                        if (hq is BaseBeverage && ((BaseBeverage)hq).Content != BeverageType.Water)
                            continue;

                        int theirAmount = hq.Quantity;

                        if (theirAmount < need)
                        {
                            hq.Quantity -= theirAmount;
                            need -= theirAmount;
                        }
                        else
                        {
                            hq.Quantity -= need;
                            break;
                        }
                    }
                }
            }

            return -1;
        }

        public int GetQuantity(Container cont, Type[] types)
        {
            Item[] items = cont.FindItemsByType(types, true);

            int amount = 0;

            for (int i = 0; i < items.Length; ++i)
            {
                IHasQuantity hq = items[i] as IHasQuantity;

                if (hq == null)
                {
                    amount += items[i].Amount;
                }
                else
                {

                    if (hq is BaseBeverage && ((BaseBeverage)hq).Content != BeverageType.Water)
                        continue;

                    amount += hq.Quantity;
                }
            }

            return amount;
        }
#endif

        public static Type[] GetEquivalentResources(Type baseType)
        {
            for (int i = 0; i < m_TypesTable.Length; ++i)
            {
                if (m_TypesTable[i][0] == baseType)
                    return m_TypesTable[i];
            }

            return new Type[] { baseType };
        }

        public bool ConsumeRes(Mobile from, Type typeRes, CraftSystem craftSystem, ref int resHue, ref int maxAmount, ConsumeType consumeType, ref TextDefinition message)
        {
            return ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, consumeType, ref message, false);
        }

        public bool ConsumeRes(Mobile from, Type typeRes, CraftSystem craftSystem, ref int resHue, ref int maxAmount, ConsumeType consumeType, ref TextDefinition message, bool isFailure)
        {
            Container ourPack = from.Backpack;

            if (ourPack == null)
                return false;

            if (m_NeedHeat && !Find(from, m_HeatSources))
            {
                message = 1044487; // You must be near a fire source to cook.
                return false;
            }

            if (m_NeedOven && !Find(from, m_Ovens))
            {
                message = 1044493; // You must be near an oven to bake that.
                return false;
            }

            if (m_NeedMill && !Find(from, m_Mills))
            {
                message = 1044491; // You must be near a flour mill to do that.
                return false;
            }

            if (m_NeedWater && !Find(from, m_WaterTroughs))
            {
                from.SendMessage("You must be near a water trough to mix dyes.");
                return false;
            }

            if (craftSystem == Township.DefTownshipCraft.CraftSystem && Township.DefTownshipCraft.RequiresMasonry(this) && (!(from is PlayerMobile) || !((PlayerMobile)from).Masonry))
            {
                message = 1044633; // You havent learned stonecraft.
                return false;
            }

            Type[][] types = new Type[m_arCraftRes.Count][];
            int[] amounts = new int[m_arCraftRes.Count];

#if RunUO
            maxAmount = int.MaxValue;
#else
            maxAmount = 60000; // Yoar: let's cap this!
#endif

            CraftSubResCol resCol = (m_UseSubRes2 ? craftSystem.CraftSubRes2 : craftSystem.CraftSubRes);

            for (int i = 0; i < types.Length; ++i)
            {
                CraftRes craftRes = m_arCraftRes.GetAt(i);
                Type baseType = craftRes.ItemType;

                // Resource Mutation
                if ((baseType == resCol.ResType) && (typeRes != null))
                {
                    baseType = typeRes;

                    CraftSubRes subResource = resCol.SearchFor(baseType);

                    if (subResource != null && from.Skills[craftSystem.MainSkill].Base < subResource.RequiredSkill)
                    {
                        message = subResource.Message;
                        return false;
                    }
                }
                // ******************

                for (int j = 0; types[i] == null && j < m_TypesTable.Length; ++j)
                {
                    if (m_TypesTable[j][0] == baseType)
                        types[i] = m_TypesTable[j];
                }

                if (types[i] == null)
                    types[i] = new Type[] { baseType };

                amounts[i] = craftRes.Amount;

                // For stackable items that can be crafted more than one at a time
                if (UseAllRes)
                {
#if RunUO
                    int tempAmount = ourPack.GetAmount(types[i]);
#else
                    int tempAmount = ItemConsumer.GetBestGroupAmount(ourPack, types[i], true, m_arCraftRes.GetAt(i).Predicate, ItemConsumer.DefaultGrouper);
#endif

                    tempAmount /= amounts[i];
                    if (tempAmount < maxAmount)
                    {
                        maxAmount = tempAmount;

                        if (maxAmount == 0)
                        {
                            CraftRes res = m_arCraftRes.GetAt(i);

                            if (!TextDefinition.IsNullOrEmpty(res.Message))
                                message = res.Message;
                            else
                                message = 502925; // You don't have the resources required to make that item.

                            return false;
                        }
                    }
                }
                // ****************************

                if (isFailure && !craftSystem.ConsumeResOnFailure(from, types[i][0], this))
                    amounts[i] = 0;
            }

            // We adjust the amount of each resource to consume the max posible
            if (UseAllRes)
            {
                for (int i = 0; i < amounts.Length; ++i)
                    amounts[i] *= maxAmount;
            }
            else
                maxAmount = -1;

#if RunUO
            Item consumeExtra = null;

            if (m_NameNumber == 1041267)
            {
                // Runebooks are a special case, they need a blank recall rune

                Item[] runes = ourPack.FindItemsByType(typeof(RecallRune));

                for (int i = 0; i < runes.Length; ++i)
                {
                    RecallRune rune = runes[i] as RecallRune;

                    if (rune != null && !rune.Marked)
                    {
                        consumeExtra = rune;
                        break;
                    }
                }

                if (consumeExtra == null)
                {
                    message = 1044253; // You don't have the components needed to make that.
                    return false;
                }
            }
#endif

#if RunUO
			int index = 0;
#else
            int index = -1;
#endif

            switch (consumeType)
            {
                // Consume ALL
                case ConsumeType.All:
                    {
                        break;
                    }
                // Consume Half ( for use all resource craft type )
                case ConsumeType.Half:
                    {
                        for (int i = 0; i < amounts.Length; i++)
                        {
                            amounts[i] /= 2;

                            if (amounts[i] < 1)
                                amounts[i] = 1;
                        }

                        break;
                    }
                // Consume 1/2 to 1/3 of required - skill check failed
                case ConsumeType.Fail:
                    {
                        for (int i = 0; i < amounts.Length; i++)
                        {
                            amounts[i] /= Utility.Random(2, 3);

                            if (amounts[i] < 1)
                                amounts[i] = 1;
                        }

                        break;
                    }
                case ConsumeType.One:
                    {
                        for (int i = 0; i < amounts.Length; i++)
                            amounts[i] = 1;

                        break;
                    }
            }

            if (consumeType != ConsumeType.None)
            {
                m_ResHue = 0; m_ResAmount = 0; m_System = craftSystem;

                #region Township Stockpile

                if (craftSystem is DefTownshipCraft)
                {
                    TownshipRegion tsr = TownshipRegion.GetTownshipAt(from);

                    if (tsr != null && tsr.TStone != null && tsr.TStone.AllowBuilding(from))
                    {
                        for (int i = 0; i < types.Length && i < amounts.Length; i++)
                        {
                            TownshipStockpile.StockFlag stock = TownshipStockpile.Identify(types[i]);

                            int withdraw = tsr.TStone.Stockpile.WithdrawUpTo(from, stock, amounts[i]);

                            if (withdraw > 0)
                            {
                                amounts[i] -= withdraw;
                                from.SendMessage("You withdrew {0} {1} from the township's stockpile.", withdraw, Township.TownshipStockpile.GetLabel(stock));
                            }
                        }
                    }
                }

                #endregion

#if RunUO
                if (IsQuantityType(types))
                    index = ConsumeQuantity(ourPack, types, amounts);
                else
                    index = ourPack.ConsumeTotalGrouped(types, amounts, true, new OnItemConsumed(OnResourceConsumed), new CheckItemGroup(CheckHueGrouping));
#else
                Item[][] groups = new Item[types.Length][];

                for (int i = 0; i < types.Length && index == -1; i++)
                {
                    Item[] group;

                    if (ItemConsumer.Group(ourPack, types[i], amounts[i], out group, true, m_arCraftRes.GetAt(i).Predicate, ItemConsumer.DefaultGrouper))
                        groups[i] = group;
                    else
                        index = i;
                }

                if (index == -1)
                {
                    for (int i = 0; i < types.Length; i++)
                        ItemConsumer.Consume(groups[i], amounts[i], OnResourceConsumed);
                }
#endif

                resHue = m_ResHue;
            }
            else // ConstumeType.None ( it's basicaly used to know if the crafter has enough resource before starting the process )
            {
                #region Township Stockpile

                if (craftSystem is DefTownshipCraft)
                {
                    TownshipRegion tsr = TownshipRegion.GetTownshipAt(from);

                    if (tsr != null && tsr.TStone != null && tsr.TStone.AllowBuilding(from))
                    {
                        for (int i = 0; i < types.Length && i < amounts.Length; i++)
                        {
                            TownshipStockpile.StockFlag stock = TownshipStockpile.Identify(types[i]);

                            amounts[i] -= Math.Min(amounts[i], tsr.TStone.Stockpile[(uint)stock]);
                        }
                    }
                }

                #endregion

#if RunUO
                index = -1;

                if (IsQuantityType(types))
                {
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (GetQuantity(ourPack, types[i]) < amounts[i])
                        {
                            index = i;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (ourPack.GetBestGroupAmount(types[i], true, new CheckItemGroup(CheckHueGrouping)) < amounts[i])
                        {
                            index = i;
                            break;
                        }
                    }
                }
#else
                for (int i = 0; i < types.Length && index == -1; i++)
                {
                    Item[] group;

                    if (!ItemConsumer.Group(ourPack, types[i], amounts[i], out group, true, m_arCraftRes.GetAt(i).Predicate, ItemConsumer.DefaultGrouper))
                        index = i;
                }
#endif
            }

            if (index == -1)
            {
#if RunUO
                if (consumeType != ConsumeType.None)
                    if (consumeExtra != null)
                        consumeExtra.Delete();
#endif

                return true;
            }
            else
            {
                CraftRes res = m_arCraftRes.GetAt(index);

                if (!TextDefinition.IsNullOrEmpty(res.Message))
                    message = res.Message;
                else
                    message = 502925; // You don't have the resources required to make that item.

                return false;
            }
        }

        private int m_ResHue;
        private int m_ResAmount;
        private CraftSystem m_System;

        private void OnResourceConsumed(Item item, int amount)
        {
            if (!RetainsColorFrom(m_System, item.GetType()))
                return;

            if (amount >= m_ResAmount)
            {
                m_ResHue = item.Hue;
                m_ResAmount = amount;
            }
        }

#if RunUO
        private int CheckHueGrouping(Item a, Item b)
        {
            return b.Hue.CompareTo(a.Hue);
        }
#endif

        public double GetExceptionalChance(CraftSystem system, double chance, Mobile from, Item tool = null)
        {
            if (m_ForceNonExceptional)
                return 0.0;

            double excChance;

            switch (system.ECA)
            {
                default:
                case CraftECA.ChanceMinusSixty: excChance = chance - 0.6; break;
                case CraftECA.FiftyPercentChanceMinusTenPercent: excChance = (chance * 0.5) - 0.1; break;
                case CraftECA.ChanceMinusSixtyToFourtyFive:
                    {
                        double skillValue = GetSkillValue(from, system, tool);

                        double offset = 0.60 - ((skillValue - 95.0) * 0.03);

                        if (offset < 0.45)
                            offset = 0.45;
                        else if (offset > 0.60)
                            offset = 0.60;

                        excChance = chance - offset;
                        break;
                    }
            }

            if (excChance > 0.0 && system == DefTailoring.CraftSystem && tool is KnittingNeedles)
                excChance += KnittingNeedles.ExceptionalBonus;

            return excChance;
        }

        public bool CheckSkills(Mobile from, Type typeRes, CraftSystem craftSystem, Item tool, ref int quality, ref bool allRequiredSkills)
        {
            return CheckSkills(from, typeRes, craftSystem, tool, ref quality, ref allRequiredSkills, craftSystem.DoesSkillGain(this));
        }

        public bool CheckSkills(Mobile from, Type typeRes, CraftSystem craftSystem, Item tool, ref int quality, ref bool allRequiredSkills, bool gainSkills)
        {
            bool success = true;
            double rand = Utility.RandomDouble(); // *

            double chance = GetSuccessChance(from, typeRes, craftSystem, gainSkills, ref allRequiredSkills, tool);

            if (GetExceptionalChance(craftSystem, chance, from, tool) >= rand) // ** exceptional
                quality = 2;
            else if (chance >= rand) // ** average
                quality = 1;
            else // failure
                success = false;

            return success;
        }

        public double GetSuccessChance(Mobile from, Type typeRes, CraftSystem craftSystem, bool gainSkills, ref bool allRequiredSkills, Item tool = null)
        {
            double minMainSkill = 0.0;
            double maxMainSkill = 0.0;
            double valMainSkill = 0.0;

            allRequiredSkills = true;

            for (int i = 0; i < m_arCraftSkill.Count; i++)
            {
                CraftSkill craftSkill = m_arCraftSkill.GetAt(i);

                double minSkill = craftSkill.MinSkill;
                double maxSkill = craftSkill.MaxSkill;
                double valSkill;

                if (craftSkill.SkillToMake == craftSystem.MainSkill)
                    valSkill = GetSkillValue(from, craftSystem, tool);
                else
                    valSkill = from.Skills[craftSkill.SkillToMake].Value;

                if (valSkill < minSkill)
                    allRequiredSkills = false;

                if (craftSkill.SkillToMake == craftSystem.MainSkill)
                {
                    minMainSkill = minSkill;
                    maxMainSkill = maxSkill;
                    valMainSkill = valSkill;
                }

                if (gainSkills) // This is a passive check. Success chance is entirely dependant on the main skill
                    from.CheckSkill(craftSkill.SkillToMake, minSkill, maxSkill, contextObj: new object[2]);
            }

            double chance;

            if (allRequiredSkills)
                chance = craftSystem.GetChanceAtMin(this) + ((valMainSkill - minMainSkill) / (maxMainSkill - minMainSkill) * (1.0 - craftSystem.GetChanceAtMin(this)));
            else
                chance = 0.0;

            if (allRequiredSkills && valMainSkill == maxMainSkill)
                chance = 1.0;

            return chance;
        }

        public double GetSkillValue(Mobile from, CraftSystem craftSystem, Item tool)
        {
            double value = from.Skills[craftSystem.MainSkill].Value;

            if (craftSystem.MainSkill == SkillName.Carpentry && tool is CarpentersToolbox)
                value += CarpentersToolbox.SkillBonus;

            if (craftSystem.MainSkill == SkillName.Tailoring && tool is TailorsSewingKit)
                value += TailorsSewingKit.SkillBonus;

            return value;
        }

        public void Craft(Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool)
        {
            if (from.BeginAction(typeof(CraftSystem)))
            {
                bool allRequiredSkills = true;
                double chance = GetSuccessChance(from, typeRes, craftSystem, false, ref allRequiredSkills, tool);

                if (allRequiredSkills && chance >= 0.0)
                {
                    TextDefinition badCraft = craftSystem.CanCraft(from, tool, m_Type);

                    if (TextDefinition.IsNullOrEmpty(badCraft))
                    {
                        int resHue = 0;
                        int maxAmount = 0;
                        TextDefinition message = null;

                        // just check and see if they have thge resources
                        if (ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, ConsumeType.None, ref message))
                        {
                            message = null;

                            // just check and see if they have the attributes
                            if (ConsumeAttributes(from, ref message, false))
                            {
                                CraftContext context = craftSystem.GetContext(from);

                                if (context != null)
                                    context.OnMade(this);

                                int iCountMax = craftSystem.GetCraftEffectMax(this);

                                new InternalTimer(from, craftSystem, this, typeRes, tool, iCountMax).Start();
                            }
                            else
                            {
                                from.EndAction(typeof(CraftSystem));
                                if (!craftSystem.OldSchool)
                                    from.SendGump(new CraftGump(from, craftSystem, tool, message));
                                else
                                    TextDefinition.SendMessageTo(from, message);
                            }
                        }
                        else
                        {
                            from.EndAction(typeof(CraftSystem));
                            if (!craftSystem.OldSchool)
                                from.SendGump(new CraftGump(from, craftSystem, tool, message));
                            else
                                TextDefinition.SendMessageTo(from, message);
                        }
                    }
                    else
                    {
                        from.EndAction(typeof(CraftSystem));
                        if (!craftSystem.OldSchool)
                            from.SendGump(new CraftGump(from, craftSystem, tool, badCraft));
                        else
                            TextDefinition.SendMessageTo(from, badCraft);
                    }
                }
                else
                {
                    from.EndAction(typeof(CraftSystem));
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, 1044153)); // You don't have the required skills to attempt this item.
                    else
                        from.SendLocalizedMessage(1044153);
                }
            }
            else
            {
                from.SendLocalizedMessage(500119); // You must wait to perform another action
            }
        }

        public void CompleteCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CustomCraft customCraft)
        {
            TextDefinition badCraft = craftSystem.CanCraft(from, tool, m_Type);

            if (!TextDefinition.IsNullOrEmpty(badCraft))
            {
                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, badCraft));
                    else
                        TextDefinition.SendMessageTo(from, badCraft);
                }
                else
                    TextDefinition.SendMessageTo(from, badCraft);

                return;
            }

            int checkResHue = 0, checkMaxAmount = 0;
            TextDefinition checkMessage = null;

            // Do we have enough resource to craft it
            if (!ConsumeRes(from, typeRes, craftSystem, ref checkResHue, ref checkMaxAmount, ConsumeType.None, ref checkMessage, true))
            {
                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, checkMessage));
                    else
                        TextDefinition.SendMessageTo(from, checkMessage);
                }
                else
                    TextDefinition.SendMessageTo(from, checkMessage);

                return;
            }
            // Do we have enough attributes to craft it
            else if (!ConsumeAttributes(from, ref checkMessage, false))
            {
                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, checkMessage));
                    else
                        TextDefinition.SendMessageTo(from, checkMessage);
                }
                else
                    TextDefinition.SendMessageTo(from, checkMessage);

                return;
            }

            bool toolBroken = false;
            int ignored = 1;
            int endquality = 1;
            bool allRequiredSkills = true;

            if (CheckSkills(from, typeRes, craftSystem, tool, ref ignored, ref allRequiredSkills))
            {
                // Resource
                int resHue = 0;
                int maxAmount = 0;

                TextDefinition message = null;

                ConsumeType ct = ConsumeType.All;

                // Not enough resource to craft it				ha
                if (!ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, ct, ref message))
                {
                    if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                    {
                        if (!craftSystem.OldSchool)
                            from.SendGump(new CraftGump(from, craftSystem, tool, message));
                        else
                            TextDefinition.SendMessageTo(from, message);
                    }
                    else
                        TextDefinition.SendMessageTo(from, message);

                    return;
                }
                else if (!ConsumeAttributes(from, ref message, true))
                {
                    if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                    {
                        if (!craftSystem.OldSchool)
                            from.SendGump(new CraftGump(from, craftSystem, tool, message));
                        else
                            TextDefinition.SendMessageTo(from, message);
                    }
                    else
                        TextDefinition.SendMessageTo(from, message);

                    return;
                }

                tool.UsesRemaining--;

                if (tool.UsesRemaining < 1)
                    toolBroken = true;

                if (toolBroken)
                    tool.Delete();

                TextDefinition notice = null;

                EnchantedScroll enchantedScroll = EnchantedScroll.Find(from, ItemType);
                Item enchantedItem = null;

                Item item;
                if (customCraft != null)
                {
                    item = customCraft.CompleteCraft(out notice);
                }
                else if (typeof(MapItem).IsAssignableFrom(ItemType) && from.Map != Map.Trammel && from.Map != Map.Felucca)
                {
                    item = new IndecipherableMap();
                    from.SendLocalizedMessage(1070800); // The map you create becomes mysteriously indecipherable.
                }
                else if (m_System.SupportsEnchantedScrolls && enchantedScroll != null && enchantedScroll.HandleCraft(from, craftSystem.MainSkill, out enchantedItem, out notice))
                {
                    item = Utility.Dupe(enchantedItem);
                }
                else
                {
                    item = Activator.CreateInstance(ItemType, m_CraftArgs) as Item;
                }

                if (item != null)
                {
                    // Adam: mark it as PlayerCrafted
                    item.PlayerCrafted = true;

                    if (item is ICraftable)
                        endquality = ((ICraftable)item).OnCraft(quality, makersMark, from, craftSystem, typeRes, tool, this, resHue);
                    else if (item.Hue == 0)
                        item.Hue = resHue;

                    if (maxAmount > 0)
                    {
                        if (!item.Stackable && item is IUsesRemaining)
                            ((IUsesRemaining)item).UsesRemaining *= maxAmount;
                        else
                            item.Amount = maxAmount;
                    }

                    #region FullBookcase HACK
                    if (item is FullBookcase || item is FullBookcase2 || item is FullBookcase3)
                    {
                        // Does it now become a ruined bookcase? 5% chance. (must not already be for sale)
                        if (Utility.RandomDouble() > 0.95 && !Loot.AlreadyForSale(item))
                        {
                            from.SendMessage("You craft the bookcase, but it is ruined.");

                            item.Delete();
                            item = new RuinedBookcase();
                            item.Movable = true;
                        }
                        else
                            from.SendMessage("You finish the bookcase and fill it with books.");
                    }
                    #endregion FullBookcase HACK

                    if (enchantedItem != null)
                    {   // Adam: some enchanted items already have a Resource type and Hue, restore that here.
                        //  Also, this is where also reset the Quality and Origin
                        EnchantedScroll.CopyProps(enchantedItem, item);
                        enchantedItem.Delete();
                    }

                    EventResourceSystem ecs = EventResourceSystem.Find(EventCraftAttribute.Find(craftSystem.GetType()));

                    if (ecs != null)
                        ecs.OnCraft(from, item);

                    if (!craftSystem.GiveItem(from, item))
                        from.AddToBackpack(item);
                }

                if (notice == null)
                {
                    notice = craftSystem.PlayEndingEffect(from, false, true, toolBroken, endquality, makersMark, this);

                    // not sure what era we want to attach this to, but I know it's not appropriate for AI
                    // offer the mark option at this point
                    if (!Core.Localized && !Core.RuleSets.AngelIslandRules())
                    {   // old-school makers mark gump
                        if (quality == 2 && from.Skills[craftSystem.MainSkill].Base >= 100.0 && this.IsMarkable(item.GetType()))
                            from.SendGump(new OldSchoolMakersMarkGump(from, item));
                    }
                }

                // faction stuff
                #region Factions
                bool queryFactionImbue = false;
                int availableSilver = 0;
                FactionItemDefinition def = null;
                Faction faction = null;

                if (item is IFactionItem)
                {
                    def = FactionItemDefinition.Identify(item);

                    if (def != null)
                    {
                        faction = Faction.Find(from);

                        if (faction != null)
                        {
                            Town town = Town.FromRegion(from.Region);

                            if (town != null && town.Owner == faction)
                            {
                                Container pack = from.Backpack;

                                if (pack != null)
                                {
                                    availableSilver = pack.GetAmount(typeof(Silver));

                                    if (availableSilver >= def.SilverCost)
                                        queryFactionImbue = Faction.IsNearType(from, def.VendorType, 12);
                                }
                            }
                        }
                    }
                }

                // TODO: Scroll imbuing
                #endregion

                if (queryFactionImbue && !item.Deleted && enchantedItem == null) // disallow faction-imbue for enchanted items
                    from.SendGump(new FactionImbueGump(quality, item, from, craftSystem, tool, notice, availableSilver, faction, def));
                else if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, notice));
                    else
                        TextDefinition.SendMessageTo(from, notice);
                }
                else
                    TextDefinition.SendMessageTo(from, notice);
            }
            else if (!allRequiredSkills)
            {
                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                {
                    if (!craftSystem.OldSchool)
                        from.SendGump(new CraftGump(from, craftSystem, tool, 1044153));
                    else
                        from.SendLocalizedMessage(1044153);
                }
                else
                    from.SendLocalizedMessage(1044153); // You don't have the required skills to attempt this item.
            }
            else
            {
                ConsumeType consumeType = (UseAllRes ? ConsumeType.Fail : ConsumeType.Half);
                int resHue = 0;
                int maxAmount = 0;

                TextDefinition message = null;

                // Do we have enough resource to craft it? If so, consume them
                if (!ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, consumeType, ref message))
                {
                    if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                    {
                        if (!craftSystem.OldSchool)
                            from.SendGump(new CraftGump(from, craftSystem, tool, message));
                        else
                            TextDefinition.SendMessageTo(from, message);
                    }
                    else
                        TextDefinition.SendMessageTo(from, message);

                    return;
                }
                // adam: added 5/27/11 to allow consumption of attributes like mana on a failure.
                // this is the case for some early shards like publish 5 Siege
                // Currently we're not restoring the bug "don�t have the proper materials" case
                // http://www.uoguide.com/Publish_15
                // BUG No:	"Players will no longer lose mana when attempting to inscribe a scroll they don�t have the proper materials for."
                // BUG Yes:	"Characters will no longer lose mana when they fail to make a scroll."
                else if (!ConsumeAttributes(from, ref message, craftSystem.ConsumeAttributeOnFailure(from)))
                {
                    if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                    {
                        if (!craftSystem.OldSchool)
                            from.SendGump(new CraftGump(from, craftSystem, tool, message));
                        else
                            TextDefinition.SendMessageTo(from, message);
                    }
                    else
                        TextDefinition.SendMessageTo(from, message);

                    return;
                }

                tool.UsesRemaining--;

                if (tool.UsesRemaining < 1)
                    toolBroken = true;

                if (toolBroken)
                    tool.Delete();

                // SkillCheck failed.
                message = craftSystem.PlayEndingEffect(from, true, true, toolBroken, endquality, false, this);

                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0 && !craftSystem.OldSchool)
                    from.SendGump(new CraftGump(from, craftSystem, tool, message));
                else
                    TextDefinition.SendMessageTo(from, message);
            }
        }

        private class InternalTimer : Timer
        {
            private Mobile m_From;
            private int m_iCount;
            private int m_iCountMax;
            private CraftItem m_CraftItem;
            private CraftSystem m_CraftSystem;
            private Type m_TypeRes;
            private BaseTool m_Tool;

            public InternalTimer(Mobile from, CraftSystem craftSystem, CraftItem craftItem, Type typeRes, BaseTool tool, int iCountMax)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(craftSystem.Delay), iCountMax)
            {
                m_From = from;
                m_CraftItem = craftItem;
                m_iCount = 0;
                m_iCountMax = iCountMax;
                m_CraftSystem = craftSystem;
                m_TypeRes = typeRes;
                m_Tool = tool;
            }

            protected override void OnTick()
            {
                m_iCount++;

                m_From.DisruptiveAction();

                if (m_iCount < m_iCountMax)
                {
                    if (m_CraftSystem.DoesCraftEffect(m_CraftItem))
                        m_CraftSystem.PlayCraftEffect(m_From, m_CraftItem);
                }
                else
                {
                    m_From.EndAction(typeof(CraftSystem));

                    TextDefinition badCraft = m_CraftSystem.CanCraft(m_From, m_Tool, m_CraftItem.m_Type);

                    if (!TextDefinition.IsNullOrEmpty(badCraft))
                    {
                        if (m_Tool != null && !m_Tool.Deleted && m_Tool.UsesRemaining > 0)
                        {
                            if (!m_CraftSystem.OldSchool)
                                m_From.SendGump(new CraftGump(m_From, m_CraftSystem, m_Tool, badCraft));
                            else
                                TextDefinition.SendMessageTo(m_From, badCraft);
                        }
                        else
                            TextDefinition.SendMessageTo(m_From, badCraft);

                        return;
                    }

                    int quality = 1;
                    bool allRequiredSkills = true;

                    m_CraftItem.CheckSkills(m_From, m_TypeRes, m_CraftSystem, m_Tool, ref quality, ref allRequiredSkills, false);

                    CraftContext context = m_CraftSystem.GetContext(m_From);

                    if (context == null)
                        return;

                    if (typeof(CustomCraft).IsAssignableFrom(m_CraftItem.ItemType))
                    {
                        int length = 6;

                        if (m_CraftItem.CraftArgs != null)
                            length += m_CraftItem.CraftArgs.Length;

                        object[] args = new object[length];

                        args[0] = m_From;
                        args[1] = m_CraftItem;
                        args[2] = m_CraftSystem;
                        args[3] = m_TypeRes;
                        args[4] = m_Tool;
                        args[5] = quality;

                        if (m_CraftItem.CraftArgs != null)
                            Array.Copy(m_CraftItem.CraftArgs, 0, args, 6, m_CraftItem.CraftArgs.Length);

                        CustomCraft cc = null;

                        try { cc = Activator.CreateInstance(m_CraftItem.ItemType, args) as CustomCraft; }
                        catch { }

                        if (cc != null)
                            cc.EndCraftAction();

                        return;
                    }

                    bool makersMark = false;

                    if (quality == 2 && m_From.Skills[m_CraftSystem.MainSkill].Base >= 100.0)
                    {
                        for (int i = 0; !makersMark && i < m_MarkableTable.Length; ++i)
                        {
                            Type t = m_MarkableTable[i];
                            makersMark = (m_MarkableTable[i].IsAssignableFrom(m_CraftItem.ItemType));
                        }
                    }

                    if (makersMark && context.MarkOption == CraftMarkOption.PromptForMark)
                    {
                        m_From.SendGump(new QueryMakersMarkGump(quality, m_From, m_CraftItem, m_CraftSystem, m_TypeRes, m_Tool));
                    }
                    else
                    {
                        if (context.MarkOption == CraftMarkOption.DoNotMark)
                            makersMark = false;

                        m_CraftItem.CompleteCraft(quality, makersMark, m_From, m_CraftSystem, m_TypeRes, m_Tool, null);
                    }
                }
            }
        }
    }
}