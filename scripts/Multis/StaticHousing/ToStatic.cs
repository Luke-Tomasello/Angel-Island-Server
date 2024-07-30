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

/* Scripts/Multis/StaticHousing/ToStatic.cs
 * CHANGELOG:
 *	7/30/07, Adam
 *		Add support for conversions from addon to static (support AddonGen house models)
 *	6/26/07, Adam
 *		- Make warning message red
 *      - don't let staff convert original custom houses to static .. force them to make a copy first
 *  6/20/07, Adam
 *      Updated to work with all houses.
 *      comment out code to replace doors as we are requiring players to purchase their own
 *	6/16/07: Pix
 *		Initial Version.
 */

using Server.Items;
using Server.Targeting;

namespace Server.Multis.StaticHousing
{
    class ToStatic
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("ToStatic", AccessLevel.GameMaster, new CommandEventHandler(ToStatic_OnCommand));
        }

        [Usage("ToStatic")]
        [Description("Converts a house or addon to a static model on the spot.")]
        private static void ToStatic_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the addon or house sign of the house which you wish to convert.");
            e.Mobile.SendMessage(0x22, "THE TARGETTED HOUSE/ADDON WILL BE DELETED AND REPLACED BY A STATIC!");
            e.Mobile.Target = new ToStaticTarget();
        }

        private class ToStaticTarget : Target
        {
            public ToStaticTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                int converted = 0;
                int deleted = 0;
                if (targ is HouseSign)
                {
                    HouseSign sign = (HouseSign)targ;
                    if (sign.Structure != null)
                    {
                        if (sign.Structure is BaseHouse)
                        {   // don't let staff convert original custom houses to static .. force them to make a copy first
                            if (sign.Structure is HouseFoundation == true && sign.Structure is StaticHouse == false)
                            {
                                from.SendMessage(0x22, "Please make a COPY of this house before converting it to static.");
                                from.SendMessage(0x22, "[housegen");
                            }
                            else
                            {
                                //ok, we got what we want :)
                                BaseHouse house = sign.Structure as BaseHouse;

                                //get a copy of the location
                                Point3D location = new Point3D(house.Location.X, house.Location.Y, house.Location.Z);

                                // make a copy of the doors - see comments below
                                // ArrayList Doors = new ArrayList(house.Doors);

                                //Now we need to iterate through the components and place their static equivalent while skipping doors and other fixtures
                                for (int i = 0; i < house.Components.List.Length; i++)
                                {
                                    string sz = location.X + house.Components.List[i].GetType().ToString();
                                    if (!IsFixture(house.Components.List[i].m_ItemID))
                                    {
                                        Point3D itemloc = new Point3D(
                                            location.X + house.Components.List[i].m_OffsetX,
                                            location.Y + house.Components.List[i].m_OffsetY,
                                            location.Z + house.Components.List[i].m_OffsetZ
                                            );
                                        Static item = new Static((int)(house.Components.List[i].m_ItemID & 0x3FFF));
                                        item.MoveToWorld(itemloc, from.Map);
                                        converted++;
                                    }
                                    else
                                        deleted++;
                                }

                                // not adding the doors back because we want players to have to purchase them
                                /*foreach (BaseDoor bd in Doors)
								{
									if (bd == null)
										continue;

									BaseDoor replacement = GetFixtureItem(bd.ItemID) as BaseDoor;
									if (replacement != null && replacement.Deleted == false)
									{
										Point3D itemloc = new Point3D(
											location.X + bd.Offset.X,
											location.Y + bd.Offset.Y,
											location.Z + bd.Offset.Z
										);

										replacement.MoveToWorld(itemloc, from.Map);
									}
								}*/

                                //delete the house
                                house.Delete();
                            }
                        }
                    }
                    else
                        from.SendMessage(0x22, "That house sign does not point to a house");
                }
                else if (targ is AddonComponent)
                {
                    //ok, we got what we want :)
                    AddonComponent addon = targ as AddonComponent;

                    // use the players location as the addon's location is whacky
                    Point3D location = new Point3D(from.Location.X, from.Location.Y, from.Location.Z);
                    from.SendMessage("Using your location for the static location.");

                    //Now we need to iterate through the components and place their static equivalent while skipping doors and other fixtures
                    foreach (Item item in addon.Addon.Components)
                    {
                        if (item == null || item is AddonComponent == false)
                            continue;

                        AddonComponent ac = item as AddonComponent;

                        if (!IsFixture((ushort)ac.ItemID))
                        {
                            Point3D itemloc = new Point3D(
                                location.X + ac.Offset.X,
                                location.Y + ac.Offset.Y,
                                location.Z + ac.Offset.Z
                                );
                            Static sitem = new Static((int)(ac.ItemID & 0x3FFF));
                            sitem.MoveToWorld(itemloc, from.Map);
                            converted++;
                        }
                        else
                            deleted++;
                    }

                    //delete the addon
                    addon.Delete();
                }
                else
                    from.SendMessage(0x22, "That is neither a house sign nor an addon");

                from.SendMessage("Conversion complete with {0} tiles converted and {1} deleted.", converted, deleted);
            }

            private bool IsFixture(ushort usItemID)
            {
                int itemID = (int)(usItemID & 0x3FFF);

                // doors
                if ((itemID >= 0x675 && itemID < 0x6F5) ||
                    (itemID >= 0x314 && itemID < 0x364) ||
                    (itemID >= 0x824 && itemID < 0x834) ||
                    (itemID >= 0x839 && itemID < 0x849) ||
                    (itemID >= 0x84C && itemID < 0x85C) ||
                    (itemID >= 0x866 && itemID < 0x876) ||
                    (itemID >= 0xE8 && itemID < 0xF8) ||
                    (itemID >= 0x1FED && itemID < 0x1FFD))
                    return true;

                // teleporter
                if (itemID >= 0x181D && itemID < 0x1829)
                    return true;

                return false;
            }

            private Item GetFixtureItem(int dItemID)
            {
                int itemID = (dItemID & 0x3FFF);

                if (itemID >= 0x181D && itemID < 0x1829)
                {
                    //Ignore teleporters!
                    //HouseTeleporter tp = new HouseTeleporter(itemID);
                    //AddFixture(tp, mte);
                }
                else
                {
                    BaseDoor door = null;

                    if (itemID >= 0x675 && itemID < 0x6F5)
                    {
                        int type = (itemID - 0x675) / 16;
                        DoorFacing facing = (DoorFacing)(((itemID - 0x675) / 2) % 8);

                        switch (type)
                        {
                            case 0: door = new GenericHouseDoor(facing, 0x675, 0xEC, 0xF3); break;
                            case 1: door = new GenericHouseDoor(facing, 0x685, 0xEC, 0xF3); break;
                            case 2: door = new GenericHouseDoor(facing, 0x695, 0xEB, 0xF2); break;
                            case 3: door = new GenericHouseDoor(facing, 0x6A5, 0xEA, 0xF1); break;
                            case 4: door = new GenericHouseDoor(facing, 0x6B5, 0xEA, 0xF1); break;
                            case 5: door = new GenericHouseDoor(facing, 0x6C5, 0xEC, 0xF3); break;
                            case 6: door = new GenericHouseDoor(facing, 0x6D5, 0xEA, 0xF1); break;
                            case 7: door = new GenericHouseDoor(facing, 0x6E5, 0xEA, 0xF1); break;
                        }
                    }
                    else if (itemID >= 0x314 && itemID < 0x364)
                    {
                        int type = (itemID - 0x314) / 16;
                        DoorFacing facing = (DoorFacing)(((itemID - 0x314) / 2) % 8);

                        switch (type)
                        {
                            case 0: door = new GenericHouseDoor(facing, 0x314, 0xED, 0xF4); break;
                            case 1: door = new GenericHouseDoor(facing, 0x324, 0xED, 0xF4); break;
                            case 2: door = new GenericHouseDoor(facing, 0x334, 0xED, 0xF4); break;
                            case 3: door = new GenericHouseDoor(facing, 0x344, 0xED, 0xF4); break;
                            case 4: door = new GenericHouseDoor(facing, 0x354, 0xED, 0xF4); break;
                        }
                    }
                    else if (itemID >= 0x824 && itemID < 0x834)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0x824) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0x824, 0xEC, 0xF3);
                    }
                    else if (itemID >= 0x839 && itemID < 0x849)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0x839) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0x839, 0xEB, 0xF2);
                    }
                    else if (itemID >= 0x84C && itemID < 0x85C)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0x84C) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0x84C, 0xEC, 0xF3);
                    }
                    else if (itemID >= 0x866 && itemID < 0x876)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0x866) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0x866, 0xEB, 0xF2);
                    }
                    else if (itemID >= 0xE8 && itemID < 0xF8)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0xE8) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0xE8, 0xED, 0xF4);
                    }
                    else if (itemID >= 0x1FED && itemID < 0x1FFD)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0x1FED) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0x1FED, 0xEC, 0xF3);
                    }

                    if (door != null)
                    {
                        return door;
                    }
                }

                return null;
            }//end of GetFixtureItem 

        }
    }
}