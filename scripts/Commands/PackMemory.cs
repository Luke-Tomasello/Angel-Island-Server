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

/* Scripts/Commands/PackMemory.cs
 * CHANGELOG:
 *  12/21/06, Kit
 *      changed to report location of bonded non controlled ghost pets
 *	12/19/06, Pix
 *		PackMemory command to fix all BaseHouseDoorAddon.
 *  11/28/06 Taran Kain
 *      Neutralized WMD.
 *  11/27/06 Taran Kain
 *      Changed PackMemory back to InitializeGenes
 *  11/27/06, Adam
 *      stable all pets
 *	11/20/06 Taran Kain
 *		Change PackMemory to generate valid gene data for all mobiles
 *	10/20/06, Adam
 *		Remove bogus comments and watchlisting 
 *	10/19/06, Adam
 *		(bring this back from file revision 1.9)
 *		- Retarget all castle runes, moonstones, and runebooks to the houses ban location if the holder is not a friend of the house
 *		- set all houses to insecure
 *  10/15/06, Adam
 *		Make email Dumper
 *  10/09/06, Kit
 *		Disabled b1 revert PackMemory
 *  10/07/06, Kit
 *		B1 control slot revert(Decrease all bonded pets control slots  by one
 *	9/27/06, Adam
 *		Sandal finder
 *  8/17/06, Kit
 *		Changed back to relink forges and trees!!
 *  8/16/06, weaver
 *		Re-link tent backpacks to their respective tents!
 *  8/7/06, Kit
 *		Make PackMemory command relink forges to house addon list, make christmas trees be added to house addon list.
 *  7/30/06, Kit
 *		Respawn all forge addons to new animated versions!
 *	7/22/06, weaver
 *		Adapted to search through all characters' bankboxes and give them a tent each.
 *	7/6/06, Adam
 *		- Retarget all castle runes, moonstones, and runebooks to the houses ban location if the holder is not a friend of the house
 *		- set all houses to insecure
 *	05/30/06, Adam
 *		Unnewbie all reagents around the world.
 *		Note: You must use [RehydrateWorld immediatly before using this command.
 *  05/15/06, Kit
 *		New warhead wipe the world of dead bonded pets that have been released but didnt delete because of B1 bug.
 *  05/14/06, Kit
 *		Disabled this WMD.
 *  05/02/06, Kit
 *		Changed to check all pets in world add them to masters stables and then increase any bonded pets control slots by +1.
 *	03/28/06 Taran Kain
 *		Changed to reset BoneContainer hues. Checks one item per game iteration. RH's and re-FD's containers.
 *  6/16/05, Kit
 *		Changed to Wipe phantom items from vendors vs clothing hue.
 *	4/22/05: Kit
 *		Initial Version
 */

namespace Server.Commands
{

    public class PackMemory
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("PackMemory", AccessLevel.Administrator, new CommandEventHandler(PackMemory_OnCommand));
        }

        [Usage("PackMemory")]
        [Description("Global Garbage Collect")]
        public static void PackMemory_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length == 0 || ((e.Arguments[0] != "true" && e.Arguments[0] != "false")))
            {
                e.Mobile.SendMessage("Usage: PackMemory <true|false>");
                e.Mobile.SendMessage("Where: true means to WaitForPendingFinalizers.");
                return;
            }

            Utility.TimeCheck tc = new Utility.TimeCheck();
            e.Mobile.SendMessage("Packing memory...");
            tc.Start();
            System.GC.Collect();
            if (e.Arguments[0] == "true")
                System.GC.WaitForPendingFinalizers();
            tc.End();
            e.Mobile.SendMessage("{0} bytes in allocated memory", System.GC.GetTotalMemory(false));
            e.Mobile.SendMessage("PackMemory took {0}", tc.TimeTaken);
        }
    }
}