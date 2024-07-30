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

using System;

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
        // production (12)
        // new ColorInfo( 2213, 6, "Gold" ),            // Gold             2213 - 2218 (6)
        // development (17)
        // new ColorInfo( 2119, 6, "Blue" ),            // Blue             2119 - 2124 (6)
        public override bool OnEquip(Mobile from)
        {
            if (base.OnEquip(from) == true)
            {
                try
                {
                    string sub_server = GetSubserver();
                    from.Title = "[" + Core.Server + sub_server + "]";
                    from.Fame = 0;
                    from.Karma = 0;
                    Colorize(sub_server);
                }
                catch { }

                return true;
            }
            else return false;
        }
        private string GetSubserver()
        {
            return Core.UOEV_CFG ? " EV" : Core.UOTC_CFG ? " TC" : "";
        }
        private new void Colorize(string sub_server)
        {
            bool localHost = false;
            if (Environment.MachineName.Equals("LUKES-MAIN", StringComparison.OrdinalIgnoreCase))
                localHost = true;

            if (localHost)
            {
                Hue = Utility.RandomSpecialHue(Utility.ColorSelect.Blue, Utility.GetStableHashCode(Core.Server + sub_server));
            }
            else
            {
                if (Core.Server.Equals("Siege Perilous", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(sub_server))
                    Hue = 0x204F;   // main server on production, we have the cool black robe
                else
                {
                    Hue = Utility.RandomSpecialHue(Utility.ColorSelect.Gold, Utility.GetStableHashCode(Core.Server + sub_server));
                }
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
            Colorize(GetSubserver());
        }
    }
}