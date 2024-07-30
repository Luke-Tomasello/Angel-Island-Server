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

/* Scripts/Commands/ToAddon.cs
 *  Changelog:
 *  6/7/22, Yoar
 *      Rewrote + moved this command from StaticHouseHelper.cs to here
 *      as this on its own is independent of the static housing system.
 */

using Server.Items;
using System;

namespace Server.Commands
{
    public class ToAddon : BaseCommand
    {
        public static void Initialize()
        {
            TargetCommands.Register(new ToAddon());
        }

        public ToAddon()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Area | CommandSupport.Single;
            Commands = new string[] { "ToAddon" };
            ObjectTypes = ObjectTypes.Items;
            Usage = "ToAddon";
            Description = "Converts the targeted item(s) to AddonComponent";
        }

        private int m_Converted;

        public override void Begin(CommandEventArgs e)
        {
            m_Converted = 0;
        }

        public override void End(CommandEventArgs e)
        {
            AddResponse(String.Format("Done! Converted {0} item(s).", m_Converted));
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            Item item = (Item)obj;

            if (item.ItemID != 0x0 && !(item is AddonComponent))
            {
                AddonComponent ac = new AddonComponent(item.ItemID);

                ac.Hue = item.Hue;
                if (item.Name != item.DefaultName)
                    ac.Name = item.Name;
                ac.Light = item.Light;

                ac.MoveToWorld(item.Location, item.Map);

                item.Delete();

                m_Converted++;
            }

            // TODO: Add option to join all components into an actual addon with a BaseAddon object?
        }
    }
}