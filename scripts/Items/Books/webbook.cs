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

/* Scripts/Items/Books/webbook.cs
   Created 12/14/04, Pigpen 
 */

using System;

namespace Server.Gumps
{
    public class WebBook : Item
    {
        private string m_Description;
        private string m_URL;
        private DateTime lastused = DateTime.UtcNow;
        private TimeSpan delay = TimeSpan.FromSeconds(5);

        [CommandProperty(AccessLevel.GameMaster)]
        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                m_Description = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string URL
        {
            get
            {
                return m_URL;
            }
            set
            {
                m_URL = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public WebBook()
            : base(0xFF4)
        {
            Movable = false;
            Hue = 1170;
            Name = "web book";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (lastused + delay > DateTime.UtcNow)
            {
                from.SendMessage("Your request is already being processed. Please wait 5 seconds between uses.");
                return;
            }
            else
            {
                lastused = DateTime.UtcNow;
                from.LaunchBrowser(m_URL);
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Description != null && m_Description.Length > 0)
                LabelTo(from, m_Description);

            base.OnSingleClick(from);
        }

        public WebBook(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write(m_Description);
            writer.Write(m_URL);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Description = reader.ReadString();
                        m_URL = reader.ReadString();

                        break;
                    }
            }
        }
    }
}