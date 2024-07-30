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

/* Engines/Guilds/GuildDeed.cs
 * CHANGELOG:
 * 06/09/06, Pix
 *		Added check on IOBAlignment for placing new guild.
 * 12/14/05 Kit,
 *		Added search for initial name as it didnt check dureing initial creation.
 */

using Server.Guilds;
using Server.Multis;
using Server.Prompts;

namespace Server.Items
{
    public class GuildDeed : Item
    {
        public override int LabelNumber { get { return 1041055; } } // a guild deed

        [Constructable]
        public GuildDeed()
            : base(0x14F0)
        {
            Weight = 1.0;
        }

        public GuildDeed(Serial serial)
            : base(serial)
        {
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

            if (Weight == 0.0)
                Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (from.Guild != null)
            {
                from.SendLocalizedMessage(501137); // You must resign from your current guild before founding another!
            }
            else if (from is Mobiles.PlayerMobile && ((Mobiles.PlayerMobile)from).IOBAlignment != IOBAlignment.None)
            {
                from.SendMessage("You cannot start a guild while you are still kin aligned after leaving your old guild.");
                from.SendMessage("You must wait seven days from when you left your last guild.");
            }
            else
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house == null)
                {
                    from.SendLocalizedMessage(501138); // You can only place a guildstone in a house.
                }
                else if (house.FindGuildstone() != null)
                {
                    from.SendLocalizedMessage(501142);//Only one guildstone may reside in a given house.
                }
                else if (!house.IsOwner(from))
                {
                    from.SendLocalizedMessage(501141); // You can only place a guildstone in a house you own!
                }
                else
                {
                    from.SendLocalizedMessage(1013060); // Enter new guild name (40 characters max):
                    from.Prompt = new InternalPrompt(this);
                }
            }
        }

        private class InternalPrompt : Prompt
        {
            private GuildDeed m_Deed;

            public InternalPrompt(GuildDeed deed)
            {
                m_Deed = deed;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Deed.Deleted)
                    return;

                if (!m_Deed.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                }
                else if (from.Guild != null)
                {
                    from.SendLocalizedMessage(501137); // You must resign from your current guild before founding another!
                }
                else
                {
                    BaseHouse house = BaseHouse.FindHouseAt(from);

                    BaseGuild NameTest = Guild.FindByName(text);
                    //make sure we dont initially create a guild with same name as a exsisting one.
                    if (NameTest != null)
                    {
                        from.SendMessage("A guild with that name already exsists.");
                    }

                    else if (house == null)
                    {
                        from.SendLocalizedMessage(501138); // You can only place a guildstone in a house.
                    }
                    else if (house.FindGuildstone() != null)
                    {
                        from.SendLocalizedMessage(501142);//Only one guildstone may reside in a given house.
                    }
                    else if (!house.IsOwner(from))
                    {
                        from.SendLocalizedMessage(501141); // You can only place a guildstone in a house you own!
                    }
                    else
                    {
                        m_Deed.Delete();

                        if (text.Length > 40)
                            text = text.Substring(0, 40);

                        Guild guild = new Guild(from, text, "none");

                        from.Guild = guild;
                        from.GuildTitle = "Guildmaster";

                        Guildstone stone = new Guildstone(guild);

                        stone.MoveToWorld(from.Location, from.Map);

                        guild.Guildstone = stone;
                    }
                }
            }

            public override void OnCancel(Mobile from)
            {
                from.SendLocalizedMessage(501145); // Placement of guildstone cancelled.
            }
        }
    }
}