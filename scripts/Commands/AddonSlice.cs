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

/* Scripts/Commands/AddonSlice.cs
 * Changelog
 *	12/5/21 Yoar
 *		Initial version.
 */

using Server.Items;
using Server.Targeting;

namespace Server.Commands
{
    public static class AddonSlice
    {
        public static void Initialize()
        {
            CommandSystem.Register("AddonSlice", AccessLevel.GameMaster, new CommandEventHandler(AddonSlice_OnCommand));
        }

        [Usage("AddonSlice")]
        [Description("Converts the targeted addon into individual statics. The BaseAddon object is deleted.")]
        private static void AddonSlice_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the addon.");
            e.Mobile.Target = new InternalTarget();
        }

        private class InternalTarget : Target
        {
            public InternalTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                BaseAddon addon;

                if (targeted is AddonComponent)
                    addon = ((AddonComponent)targeted).Addon;
                else
                    addon = targeted as BaseAddon;

                if (addon == null)
                {
                    from.SendMessage("That is not an addon.");
                    return;
                }

                int count = 0;

                foreach (AddonComponent c in addon.Components)
                {
                    Static stat = new Static(c.ItemID);

                    stat.Hue = c.Hue;
                    if (c.Name != c.DefaultName)
                        stat.Name = c.Name;
                    stat.Light = c.Light;

                    stat.MoveToWorld(c.Location, c.Map);

                    count++;
                }

                addon.Delete();

                from.SendMessage("Done! Converted {0} addon components.", count);
            }
        }
    }
}