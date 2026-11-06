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

/* Scripts/Items/Suits/AdminRobe.cs
 * ChangeLog
 *  7/21/06, Rhiannon
 *		Added Owner access level to Godly robe
 *	6/27/06, Adam
 *		- (re)set the player mobile titles automatically if not Godly
 *		- clear fame and karma (titles) if not Godly
 *		- reset color on robe load
 *		- Add Godly version of robes for Adam And Jade
 *	4/17/06, Adam
 *		Explicitly set name
 *	11/07/04, Jade
 *		Changed hue to 0x1.
 */

namespace Server.Items
{
    public class AdminRobe : BaseSuit
    {
        private const int m_hue = 0x0;  // Admin color

        [Constructable]
        public AdminRobe()
            : base(AccessLevel.Administrator, m_hue, 0x204F)
        {
            Name = "Administrator Robe";
        }

        public AdminRobe(Serial serial)
            : base(serial)
        {
        }

        public override bool OnEquip(Mobile from)
        {
            if (base.OnEquip(from) == true)
            {
                from.Title = "a shard administrator";
                from.Fame = 0;
                from.Karma = 0;
                return true;
            }
            else return false;
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

            if (Hue != m_hue)
                Hue = m_hue;
        }
    }

    public class GodlyRobe : BaseSuit
    {
        [Constructable]
        public GodlyRobe()
            : base(AccessLevel.Owner, hue: 0x1, 0x204F)
        {
            Name = "Godly Robe";
        }

        public GodlyRobe(Serial serial)
            : base(serial)
        {
        }

        /// <summary>
        /// Called after the robe has been added to the Mobile (placed on OuterTorso layer).
        /// This is a better hook than OnEquip because the item is guaranteed to be on the layer.
        /// </summary>
        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile mob)
            {
                RefreshGodlyEffects(mob);
            }
        }

        /// <summary>
        /// Refreshes the owner's title, clears fame/karma, and updates robe color 
        /// based on the current server configuration.
        /// 
        /// Safe to call from OnAdded(), OnLogin(), Deserialize(), or InitShard().
        /// </summary>
        public static void RefreshGodlyEffects(Mobile from)
        {
            if (from == null)
                return;

            // Set title based on current server (e.g. [Seasons], [Seasons TC], [Seasons EV])
            string subServer = GetSubserver();
            from.Title = "[" + Core.Server + subServer + "]";

            // Clear fame and karma so only the server tag is shown
            from.Fame = 0;
            from.Karma = 0;

            // Update robe color if the GodlyRobe is currently equipped
            if (from.FindItemOnLayer(Layer.OuterTorso) is GodlyRobe robe)
            {
                robe.ApplyGodlyAppearance(subServer);
            }
        }

        private static string GetSubserver()
        {
            return Core.GMNEV_CFG ? " EV" : Core.GMNTC_CFG ? " TC" : "";
        }

        /// <summary>
        /// Applies the correct hue to this GodlyRobe based on server type:
        /// - Black on main production shard (currently Seasons)
        /// - Gold on all other production shards
        /// - Blue on localhost / development
        /// </summary>
        private void ApplyGodlyAppearance(string subServer)
        {
            bool isLocalHost = Core.Developing;

            if (isLocalHost)
            {
                // Development / localhost = Blue tones
                Hue = Utility.RandomSpecialHue(Utility.ColorSelect.Blue, Utility.GetStableHashCode(Core.Server + subServer));
            }
            else
            {
                if (Core.ServerID == ServerID.Upheaval && string.IsNullOrEmpty(subServer))
                    Hue = 0x204F;           // Main Upheaval production = Cool Black
                else
                    // All other production shards (EV, etc.) = Gold tones
                    Hue = Utility.RandomSpecialHue(Utility.ColorSelect.Gold, Utility.GetStableHashCode(Core.Server + subServer));
            }
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

            // Refresh appearance in case server configuration has changed
            if (RootParent is Mobile m)
                RefreshGodlyEffects(m);
        }
    }
}