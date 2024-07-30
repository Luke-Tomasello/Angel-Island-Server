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

/* Scripts/Gumps/Guilds/GuildChangeTypeGump.cs
 * ChangeLog:
 *  4/19/23, Yoar
 *      Added ConfirmGuildTypeGump
 *  4/14/23, Yoar
 *      Reworked gump. Added support for new Alignment system.
 *  8/28/22, Yoar
 *      You can no longer change your guild alignment while in a faction.
 *	7/23/06, Pix
 *		Now disallows kin alignment change when GuildKinChangeDisabled featurebit is set.
 *	4/28/06, Pix
 *		Changes for Kin alignment by guild.
 *  12/14/05, Kit
 *		Added check to prevent special type guilds changeing to 
 *		a differnt special type if allied with opposeing type guilds.
 */

using Server.Engines.Alignment;
using Server.Factions;
using Server.Guilds;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Gumps
{
    public class GuildChangeTypeGump : Gump
    {
        private Mobile m_Mobile;
        private Guild m_Guild;
        private GuildOption[] m_Options;

        public GuildChangeTypeGump(Mobile from, Guild guild)
            : base(20, 30)
        {
            m_Mobile = from;
            m_Guild = guild;
            m_Options = GetOptions();

            Dragable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 400, 5054);
            AddBackground(10, 10, 530, 380, 3000);

            AddHtmlLocalized(20, 15, 510, 30, 1013062, false, false); // <center>Change Guild Type Menu</center>

            AddHtmlLocalized(50, 50, 450, 30, 1013066, false, false); // Please select the type of guild you would like to change to

            int x = 20;
            int y = 100;

            for (int i = 0; i < m_Options.Length; i++)
            {
                if (i != 0 && i % 10 == 0)
                {
                    x += 280;
                    y = 100;
                }

                AddButton(x, y, 4005, 4007, i + 1, GumpButtonType.Reply, 0);
                TextDefinition.AddHtmlText(this, x + 65, y, 300, 30, m_Options[i].Label, false, false);

                y += 24;
            }

            AddButton(300, 360, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(335, 360, 150, 30, 1011012, false, false); // CANCEL
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
                return;

            int index = info.ButtonID - 1;

            if (index >= 0 && index < m_Options.Length)
            {
                m_Mobile.CloseGump(typeof(ConfirmGuildTypeGump));
                m_Mobile.SendGump(new ConfirmGuildTypeGump(m_Guild, m_Options[index]));

                return;
            }

            GuildGump.EnsureClosed(m_Mobile);
            m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
        }

        private static void ChangeType(Mobile m, Guild guild, GuildOption option)
        {
            if (!option.Validate(guild))
                return;

            if (PlayerState.Find(m) != null)
            {
                m.SendLocalizedMessage(1010405); // You cannot change guild types while in a Faction!
                return;
            }

            if (m.AccessLevel < AccessLevel.GameMaster && DateTime.UtcNow < guild.NextTypeChange)
            {
                m.SendLocalizedMessage(1011142); // You have already changed your guild type recently.
                return;
            }

            ArrayList allies = guild.Allies;

            for (int i = 0; i < allies.Count; i++)
            {
                Guild ally = (Guild)allies[i];

                if (option.IsConflict(ally))
                {
                    m.SendMessage("In order to change to this guild type, you must first break all alliances with opposing guild types.");
                    return;
                }
            }

            option.OnSelect(m, guild);
        }

        private GuildOption[] GetOptions()
        {
            List<GuildOption> list = new List<GuildOption>();

            if (Guild.OrderChaosEnabled)
            {
                list.Add(new OrderChaosOption(GuildType.Regular, 1013063)); // Standard guild
                list.Add(new OrderChaosOption(GuildType.Order, 1013064)); // Order guild
                list.Add(new OrderChaosOption(GuildType.Chaos, 1013065)); // Chaos guild
            }

            if (Core.RuleSets.KinSystemEnabled())
            {
                list.Add(new IOBOption(IOBAlignment.None));
                list.Add(new IOBOption(IOBAlignment.Brigand));
                list.Add(new IOBOption(IOBAlignment.Council));
                list.Add(new IOBOption(IOBAlignment.Good));
                list.Add(new IOBOption(IOBAlignment.Orcish));
                list.Add(new IOBOption(IOBAlignment.Pirate));
                list.Add(new IOBOption(IOBAlignment.Savage));
                list.Add(new IOBOption(IOBAlignment.Undead));
            }

            if (AlignmentSystem.Enabled)
            {
                list.Add(new AlignmentOption(AlignmentType.None, "Unaligned"));

                foreach (Alignment alignment in Alignment.Table)
                {
                    if (alignment.Definition.Available)
                        list.Add(new AlignmentOption(alignment.Type, alignment.Definition.Name));
                }
            }

            return list.ToArray();
        }

        private abstract class GuildOption
        {
            public readonly TextDefinition Label;

            public GuildOption(TextDefinition label)
            {
                Label = label;
            }

            public abstract bool Validate(Guild g);
            public abstract bool IsConflict(Guild g);
            public abstract void OnSelect(Mobile m, Guild g);
        }

        private sealed class OrderChaosOption : GuildOption
        {
            private GuildType m_Type;

            public OrderChaosOption(GuildType type, TextDefinition label)
                : base(label)
            {
                m_Type = type;
            }

            public override bool Validate(Guild g)
            {
                return Guild.OrderChaosEnabled;
            }

            public override bool IsConflict(Guild g)
            {
                return (m_Type != GuildType.Regular && g.Type != GuildType.Regular && g.Type != m_Type);
            }

            public override void OnSelect(Mobile m, Guild g)
            {
                g.Type = m_Type;
            }
        }

        private sealed class IOBOption : GuildOption
        {
            private IOBAlignment m_Type;

            public IOBOption(IOBAlignment type)
                : base(Engines.IOBSystem.IOBSystem.GetIOBName(type))
            {
                m_Type = type;
            }

            public override bool Validate(Guild g)
            {
                return Core.RuleSets.KinSystemEnabled();
            }

            public override bool IsConflict(Guild g)
            {
                return (m_Type != IOBAlignment.None && g.IOBAlignment != IOBAlignment.None && g.IOBAlignment != m_Type);
            }

            public override void OnSelect(Mobile m, Guild g)
            {
                if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.GuildKinChangeDisabled))
                {
                    m.SendMessage("Guild Kin Alignment change is currently disabled.");
                    return;
                }

                g.IOBAlignment = m_Type;

                if (g.IOBAlignment != IOBAlignment.None)
                    g.GuildMessage("Your guild is now allied with the " + Label);
                else
                    g.GuildMessage("Your guild has broken its kin alignment, it is now unaligned.");
            }
        }

        private sealed class AlignmentOption : GuildOption
        {
            private AlignmentType m_Type;

            public AlignmentOption(AlignmentType type, TextDefinition label)
                : base(label)
            {
                m_Type = type;
            }

            public override bool Validate(Guild g)
            {
                Alignment alignment = Alignment.Get(m_Type);

                return (AlignmentSystem.Enabled && (m_Type == AlignmentType.None || (alignment != null && alignment.Definition.Available)));
            }

            public override bool IsConflict(Guild g)
            {
                return false; // we can ally with opposite aligned guilds
            }

            public override void OnSelect(Mobile m, Guild g)
            {
                g.Alignment = m_Type;
            }
        }

        private class ConfirmGuildTypeGump : Gump
        {
            private Guild m_Guild;
            private GuildOption m_Option;

            public ConfirmGuildTypeGump(Guild guild, GuildOption option)
                : base(50, 50)
            {
                m_Guild = guild;
                m_Option = option;

                AddPage(0);

                AddBackground(10, 10, 190, 140, 0x242C);

                AddHtml(25, 30, 160, 80, String.Format("<center>Are you sure you wish to set your guild type to: {0}?</center>", option.Label), false, false);

                AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
                AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                Mobile from = state.Mobile;

                if (info.ButtonID == 0x1)
                    ChangeType(from, m_Guild, m_Option);

                GuildGump.EnsureClosed(from);
                from.SendGump(new GuildmasterGump(from, m_Guild));
            }
        }
    }
}