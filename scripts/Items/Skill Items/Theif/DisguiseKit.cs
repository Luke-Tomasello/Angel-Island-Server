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

/* Scripts/Items/Skill Items/Thief/DisguiseKit.cs
 * ChangeLog
 *  7/23/2023, Adam
 *      - DisguiseKit now set default hue
 *      - DisguiseKit now save NPC name, beard/hair and hue
 *      - DisguiseKit now allow the restoration of previously defined disguises
 *      - The DisguiseKit gives the name of the disguise when single clicked.
 *  8/28/22, Yoar
 *      You can no longer use a disguise kit while holding a sigil.
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;
using Server.Spells.Fifth;
using Server.Spells.Seventh;
using System;
using System.Collections;

namespace Server.Items
{
    public class DisguiseKit : Item
    {
        public const int NoChange = -2;
        public const int Restore = -1;
        private string m_lastName = null;
        public string LastName { get { return m_lastName; } set { m_lastName = value; } }
        private int m_lastHairID = DisguiseKit.NoChange;
        public int LastHairID { get { return m_lastHairID; } set { m_lastHairID = value; } }
        private int m_lastHairHue = 0;
        public int LastHairHue { get { return m_lastHairHue; } set { m_lastHairHue = value; } }
        private int m_lastBeardID = DisguiseKit.NoChange;
        public int LastBeardID { get { return m_lastBeardID; } set { m_lastBeardID = value; } }
        private int m_lastBeardHue = 0;
        public int LastBeardHue { get { return m_lastBeardHue; } set { m_lastBeardHue = value; } }
        private int m_HairHueIndex = 1102;  // beginning of the hair hues

        public override int LabelNumber { get { return 1041078; } } // a disguise kit

        [Constructable]
        public DisguiseKit()
            : base(0xE05)
        {
            Weight = 1.0;
        }

        public DisguiseKit(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(m_lastName);
            writer.Write(m_lastHairID);
            writer.Write(m_lastHairHue);
            writer.Write(m_lastBeardID);
            writer.Write(m_lastBeardHue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        m_lastName = reader.ReadString();
                        m_lastHairID = reader.ReadInt();
                        m_lastHairHue = reader.ReadInt();
                        m_lastBeardID = reader.ReadInt();
                        m_lastBeardHue = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }

            if (version < 2)
            {
                if (m_lastHairHue == DisguiseKit.Restore)
                    m_lastHairHue = DisguiseKit.NoChange;

                if (m_lastBeardHue == DisguiseKit.Restore)
                    m_lastBeardHue = DisguiseKit.NoChange;
            }

        }

        public bool ValidateUse(Mobile from)
        {
            PlayerMobile pm = from as PlayerMobile;

            if (!IsChildOf(from.Backpack))
            {
                // That must be in your pack for you to use it.
                from.SendLocalizedMessage(1042001);
            }
            else if (pm == null || pm.NpcGuild != NpcGuild.ThievesGuild)
            {
                // Only Members of the thieves guild are trained to use this item.
                from.SendLocalizedMessage(501702);
            }
            else if (Stealing.SuspendOnMurder && pm.LongTermMurders > 0)
            {
                // You are currently suspended from the thieves guild.  They would frown upon your actions.
                from.SendLocalizedMessage(501703);
            }
            else if (!from.CanBeginAction(typeof(IncognitoSpell)))
            {
                // You cannot disguise yourself while incognitoed.
                from.SendLocalizedMessage(501704);
            }
            else if (Factions.Sigil.ExistsOn(from))
            {
                from.SendLocalizedMessage(1010465); // You cannot disguise yourself while holding a sigil
            }
            else if (Engines.Alignment.TheFlag.ExistsOn(from))
            {
                from.SendMessage("You cannot disguise yourself while holding a flag");
            }
            //else if ( TransformationSpell.UnderTransformation( from ) )
            //{
            // You cannot disguise yourself while in that form.
            //from.SendLocalizedMessage( 1061634 );
            //}
            else if (from.BodyMod == 183 || from.BodyMod == 184)
            {
                // You cannot disguise yourself while wearing body paint
                from.SendLocalizedMessage(1040002);
            }
            else if (!from.CanBeginAction(typeof(PolymorphSpell)) || from.IsBodyMod)
            {
                // You cannot disguise yourself while polymorphed.
                from.SendLocalizedMessage(501705);
            }
            else
            {
                return true;
            }

            return false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (ValidateUse(from))
                from.SendGump(new DisguiseGump(from, this, true, false));
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (!string.IsNullOrEmpty(LastName))
                LabelTo(from, "(" + LastName + ")");
        }

        // allows players to easily cycle through the hair hues in Disguise kit
        public int NextHairHue()
        {   // hair hues are 1102, 48
            if (m_HairHueIndex < 1102 || m_HairHueIndex >= 1102 + 48)
                m_HairHueIndex = 1102;

            return m_HairHueIndex++;
        }
    }

    public class DisguiseGump : Gump
    {
        private Mobile m_From;
        private DisguiseKit m_Kit;
        private bool m_Used;

        public DisguiseGump(Mobile from, DisguiseKit kit, bool startAtHair, bool used)
            : base(50, 50)
        {
            m_From = from;
            m_Kit = kit;
            m_Used = used;

            from.CloseGump(typeof(DisguiseGump));

            AddPage(0);

            AddBackground(100, 10, 400, 385, 2600);

            // <center>THIEF DISGUISE KIT</center>
            AddHtmlLocalized(100, 25, 400, 35, 1011045, false, false);

            AddButton(140, 353, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(172, 355, 90, 35, 1011036, false, false); // OKAY

            AddButton(257, 353, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(289, 355, 90, 35, 1011046, false, false); // APPLY

            AddButton(364, 353, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtmlLocalized(396, 355, 90, 35, 1011070, false, false); // Make Last

            if (from.Female || from.Body.IsFemale)
            {
                DrawEntries(0, 1, -1, m_HairEntries, -1);
            }
            else if (startAtHair)
            {
                DrawEntries(0, 1, 2, m_HairEntries, 1011056);
                DrawEntries(1, 2, 1, m_BeardEntries, 1011059);
            }
            else
            {
                DrawEntries(1, 1, 2, m_BeardEntries, 1011059);
                DrawEntries(0, 2, 1, m_HairEntries, 1011056);
            }
        }

        private void DrawEntries(int index, int page, int nextPage, DisguiseEntry[] entries, int nextNumber)
        {
            AddPage(page);

            if (nextPage != -1)
            {
                AddButton(155, 320, 250 + (index * 2), 251 + (index * 2), 0, GumpButtonType.Page, nextPage);
                AddHtmlLocalized(180, 320, 150, 35, nextNumber, false, false);
            }

            for (int i = 0; i < entries.Length; ++i)
            {
                DisguiseEntry entry = entries[i];

                if (entry == null)
                    continue;

                int x = (i % 2) * 205;
                int y = (i / 2) * 55;

                if (entry.m_GumpID != 0)
                {
                    AddBackground(220 + x, 60 + y, 50, 50, 2620);
                    AddImage(153 + x + entry.m_OffsetX, 15 + y + entry.m_OffsetY, entry.m_GumpID);
                }

                AddHtmlLocalized(140 + x, 72 + y, 80, 35, entry.m_Number, false, false);
                AddRadio(118 + x, 73 + y, 208, 209, false, (i * 2) + index);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 0)
            {
                if (m_Used)
                    m_From.SendLocalizedMessage(501706); // Disguises wear off after 2 hours.
                else
                    m_From.SendLocalizedMessage(501707); // You're looking good.

                return;
            }
            else if (info.ButtonID == 2)
            {
                if (m_Kit.LastName == null)
                {   // just assign defaults
                    m_Kit.LastName = m_From.NameMod = NameList.RandomName(m_From.Female ? "female" : "male");
                    if (m_From is PlayerMobile pm)
                    {
                        if (pm.Body.IsFemale)
                            pm.SetHairMods(m_Kit.LastHairID = Utility.RandomList(Utility.HairIDs), 0);
                        else
                            pm.SetHairMods(m_Kit.LastHairID = Utility.RandomList(Utility.HairIDs), m_Kit.LastBeardID = Utility.RandomList(Utility.BeardIDs));

                        Item look = pm.FindItemOnLayer(Layer.Hair);

                        if (look != null)
                            m_Kit.LastHairHue = look.Hue = m_Kit.NextHairHue();

                        look = pm.FindItemOnLayer(Layer.FacialHair);

                        if (look != null)
                            m_Kit.LastBeardHue = look.Hue = m_Kit.NextHairHue();
                    }
                }
                else
                {   // rebuild using last settings

                    m_From.NameMod = m_Kit.LastName;
                    if (m_From is PlayerMobile pm)
                    {
                        if (pm.Body.IsFemale)
                            pm.SetHairMods(m_Kit.LastHairID, 0);
                        else
                            pm.SetHairMods(m_Kit.LastHairID, m_Kit.LastBeardID);

                        Item look = pm.FindItemOnLayer(Layer.Hair);

                        if (look != null && m_Kit.LastHairID != DisguiseKit.NoChange)
                            look.Hue = m_Kit.LastHairHue;

                        look = pm.FindItemOnLayer(Layer.FacialHair);

                        if (look != null && m_Kit.LastBeardID != DisguiseKit.NoChange)
                            look.Hue = m_Kit.LastBeardHue;
                    }
                }

                StopTimer(m_From);
#if DEBUG
                m_Timers[m_From] = Timer.DelayCall(TimeSpan.FromMinutes(10.0), new TimerStateCallback(OnDisguiseExpire), m_From);
#else
                m_Timers[m_From] = Timer.DelayCall(TimeSpan.FromHours(2.0), new TimerStateCallback(OnDisguiseExpire), m_From);
#endif
                m_From.Delta(MobileDelta.Name | MobileDelta.Body);
            }
            else
            {
                int[] switches = info.Switches;

                if (switches.Length == 0)
                    return;

                int switched = switches[0];
                int type = switched % 2;
                int index = switched / 2;

                bool hair = (type == 0);

                DisguiseEntry[] entries = (hair ? m_HairEntries : m_BeardEntries);

                if (index >= 0 && index < entries.Length)
                {
                    DisguiseEntry entry = entries[index];

                    if (entry == null)
                        return;

                    if (!m_Kit.ValidateUse(m_From))
                        return;

                    if (!hair && (m_From.Female || m_From.Body.IsFemale))
                        return;

                    m_Kit.LastName = m_From.NameMod = NameList.RandomName(m_From.Female ? "female" : "male");

                    if (m_From is PlayerMobile)
                    {
                        PlayerMobile pm = (PlayerMobile)m_From;

                        if (hair)
                            pm.SetHairMods(m_Kit.LastHairID = entry.m_ItemID, DisguiseKit.NoChange);
                        else
                            pm.SetHairMods(DisguiseKit.NoChange, m_Kit.LastBeardID = entry.m_ItemID);

                        #region Hue
                        Item look;

                        if (hair)
                        {
                            look = pm.FindItemOnLayer(Layer.Hair);

                            if (look != null)
                                m_Kit.LastHairHue = look.Hue = m_Kit.NextHairHue();
                        }
                        else
                        {
                            look = pm.FindItemOnLayer(Layer.FacialHair);

                            if (look != null)
                                m_Kit.LastBeardHue = look.Hue = m_Kit.NextHairHue();
                        }
                        #endregion Hue
                    }

                    m_From.SendGump(new DisguiseGump(m_From, m_Kit, hair, true));
                }

                StopTimer(m_From);
#if DEBUG
                m_Timers[m_From] = Timer.DelayCall(TimeSpan.FromMinutes(10.0), new TimerStateCallback(OnDisguiseExpire), m_From);
#else
                m_Timers[m_From] = Timer.DelayCall(TimeSpan.FromHours(2.0), new TimerStateCallback(OnDisguiseExpire), m_From);
#endif
                m_From.Delta(MobileDelta.Name | MobileDelta.Body);
            }
        }

        public static void OnDisguiseExpire(object state)
        {
            Mobile m = (Mobile)state;

            StopTimer(m);

            m.NameMod = null;

            if (m is PlayerMobile)
                ((PlayerMobile)m).SetHairMods(DisguiseKit.Restore, DisguiseKit.Restore);

            m.Delta(MobileDelta.Name | MobileDelta.Body);
        }

        public static bool IsDisguised(Mobile m)
        {
            return m_Timers.Contains(m);
        }

        public static bool StopTimer(Mobile m)
        {
            Timer t = (Timer)m_Timers[m];

            if (t != null)
            {
                t.Stop();
                m_Timers.Remove(m);
            }

            return (t != null);
        }

        private static Hashtable m_Timers = new Hashtable();

        private static DisguiseEntry[] m_HairEntries = new DisguiseEntry[]
            {
                new DisguiseEntry( 8251, 50700, 0,  5, 1011052 ), // Short
				new DisguiseEntry( 8261, 60710, 0,  3, 1011047 ), // Pageboy
				new DisguiseEntry( 8252, 60708, 0,- 5, 1011053 ), // Long
				new DisguiseEntry( 8264, 60901, 0,  5, 1011048 ), // Receding
				new DisguiseEntry( 8253, 60702, 0,- 5, 1011054 ), // Ponytail
				new DisguiseEntry( 8265, 60707, 0,- 5, 1011049 ), // 2-tails
				new DisguiseEntry( 8260, 50703, 0,  5, 1011055 ), // Mohawk
				new DisguiseEntry( 8266, 60713, 0, 10, 1011050 ), // Topknot
				null,
                new DisguiseEntry( 0, 0, 0, 0, 1011051 ) // None
			};

        private static DisguiseEntry[] m_BeardEntries = new DisguiseEntry[]
            {
                new DisguiseEntry( 8269, 50906, 0,  0, 1011401 ), // Vandyke
				new DisguiseEntry( 8257, 50808, 0,- 2, 1011062 ), // Mustache
				new DisguiseEntry( 8255, 50802, 0,  0, 1011060 ), // Short beard
				new DisguiseEntry( 8268, 50905, 0,-10, 1011061 ), // Long beard
				new DisguiseEntry( 8267, 50904, 0,  0, 1011060 ), // Short beard
				new DisguiseEntry( 8254, 50801, 0,-10, 1011061 ), // Long beard
				null,
                new DisguiseEntry( 0, 0, 0, 0, 1011051 ) // None
			};

        private class DisguiseEntry
        {
            public int m_Number;
            public int m_ItemID;
            public int m_GumpID;
            public int m_OffsetX;
            public int m_OffsetY;

            public DisguiseEntry(int itemID, int gumpID, int ox, int oy, int name)
            {
                m_ItemID = itemID;
                m_GumpID = gumpID;
                m_OffsetX = ox;
                m_OffsetY = oy;
                m_Number = name;
            }
        }
    }
}