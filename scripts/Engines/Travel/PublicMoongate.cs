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

/* scripts\Engines\Travel\PublicMoongate.cs
 * CHANGELOG:
 *  6/8/23, Yoar
 *      Invisible public moongates cannot be used
 *  9/4/22, Yoar (WorldZone)
 *      Enabled rotating moongates for world zones.
 *  9/4/22, Yoar (WorldZone)
 *      Added WorldZone check in order to contain players within the world zone.
 *      Made DeleteAll, MoonGen public and added GenerateAll method-to be called by World Zone system.
 *  9/4/22, Yoar
 *      Added 'OldMoongates' getter.
 *      Added 'CheckGate' method that tries to gate the mobile using the specified PMEntry and PMList.
 *      We call 'CheckGate' twice; once in 'UseGate' and once in 'MoongateGump.OnResponse'.
 *      Added TextEntry support for the moongate gump.
 *	2/24/11, Adam
 *		Update to RunUO script for old-style moongates.
 *		o Added our BaseOverland check
 *		o removed Young checks
 *		o Convert murderer checks to GMN style
 *		-- RunUO v2.0 SVN 313 script modified
 *		-- to provide old style osi behaviour
 *		-- based on modification on 6/18/06 by David
 *		-- mod version 1.1 3/21/09 by David added destination verbage (cliloc text)
 *	11/23/04, Darva
 *		Changed GetDetinationIndex so that a player won't wind up back at the location they just left.
 *	8/6/04, mith
 *		Modified the way we figure new location, based on steps between tram phase and fel phase.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	3/15/04, mith
 *		Removed moongate gump and replaced with code to get moonphase to determine destination.
 *		Used reference at Stratics for moongate messages based on destination.
 *		Reference for location based on moonphase: http://martin.brenner.de/ultima/uo/moongates.html
 *		Reference for description based on location: http://uo.stratics.com/content/basics/moongate/moongate.shtml
 *		Script location: Scripts/Items/Misc/PublicMoongate.cs
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class PublicMoongate : Item
    {
        public bool OldMoongates { get { return true; } } // TODO: Core check

        //public override bool ForceShowProperties { get { return ObjectPropertyList.Enabled; } }

        [Constructable]
        public PublicMoongate()
            : base(0xF6C)
        {
            Movable = false;
            Light = LightType.Circle300;
        }

        public PublicMoongate(Serial serial)
            : base(serial)
        {
        }

        public override void OnAosSingleClick(Mobile from)
        {
            OnSingleClick(from);
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (Utility.InRange(from.Location, this.Location, 3))
            {
                if (m_forceEntry)
                {
                    if (m_entry != null)
                        from.SendLocalizedMessage(m_entry.Description);
                }
                else if (OldMoongates && (this.Map == Map.Felucca || this.Map == Map.Trammel))
                {
                    PMEntry entry;
                    PMList list;

                    GetGateEntry(this, out entry, out list);

                    if (entry != null)
                        from.SendLocalizedMessage(entry.Description);
                }
            }
            else
                from.SendLocalizedMessage(500446); // That is too far away.
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!Visible || !from.Player)
                return;

            if (from.InRange(GetWorldLocation(), 1))
                UseGate(from);
            else
                from.SendLocalizedMessage(500446); // That is too far away.
        }

        public override bool OnMoveOver(Mobile from)
        {
            if (!Visible || !from.Player)
                return true;

            return UseGate(from);
        }

        public override bool HandlesOnMovement { get { return true; } }

        public override void OnMovement(Mobile from, Point3D oldLocation)
        {
            if (from is PlayerMobile)
            {
                if (!Utility.InRange(from.Location, this.Location, 1) && Utility.InRange(oldLocation, this.Location, 1))
                    from.CloseGump(typeof(MoongateGump));
            }
        }

        #region moongate override
        private bool m_forceEntry;
        private PMEntry m_entry;
        private PMList m_list;
        public bool ForceEntry { get { return m_forceEntry; } set { m_forceEntry = value; } }
        public PMEntry Entry { get { return m_entry; } set { m_entry = value; } }
        public PMList List { get { return m_list; } set { m_list = value; } }
        #endregion moongate override

        public bool CheckGate(Mobile from, PMList list, PMEntry entry)
        {
            if (!from.InRange(this.GetWorldLocation(), 1) || from.Map != this.Map)
            {
                from.SendLocalizedMessage(1019002); // You are too far away to use the gate.
                return false;
            }
            // 8/10/22 Adam: I think we should check Core.RedsInTown at this location. 
            else if (from.Player && (from.Red && !Core.RedsInTown) && list.Map != Map.Felucca)
            {
                from.SendLocalizedMessage(1019004); // You are not allowed to travel there.
                return false;
            }
            else if (Factions.Sigil.ExistsOn(from) && list.Map != Factions.Faction.Facet)
            {
                from.SendLocalizedMessage(1019004); // You are not allowed to travel there.
                return false;
            }
            else if (from.Criminal)
            {
                from.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return false;
            }
            else if (SpellHelper.CheckCombat(from))
            {
                from.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return false;
            }
            else if (from.Spell != null)
            {
                from.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
                return false;
            }
            else if (from.CheckState(Mobile.ExpirationFlagID.EvilCrim))
            {
                from.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return false;
            }
            else if (from is BaseCreature bc && bc.OnMagicTravel() == BaseCreature.TeleportResult.AnyRejected)
            {   // message issued in OnMagicTravel()
                return false; // we're not traveling like this
            }
            else if (from.Map == list.Map && from.InRange(entry.Location, 1))
            {
                from.SendLocalizedMessage(1019003); // You are already there.
                return false;
            }
            else
            {
                //{
                //    /* Debugging */
                //    Console.WriteLine("\nPlayer: {0}", from.Name);
                //    Console.WriteLine("  From: {0}, {1}, {2} on {3}", from.X, from.Y, from.Z, from.Map);
                //    Console.WriteLine("    To: {0}, {1}, {2} on {3}", m_entry.Location.X, m_entry.Location.Y, m_entry.Location.Z, m_list.Map);
                //}

                BaseCreature.TeleportPets(from, entry.Location, list.Map);

                from.Combatant = null;
                from.Warmode = false;
                from.Hidden = true;

                from.MoveToWorld(entry.Location, list.Map);
                from.Send(new PlaySound(0x20E, from.Location));

                //{
                //    /* Debugging */
                //    Console.WriteLine("Result: {0}, {1}, {2} on {3}", from.X, from.Y, from.Z, from.Map);
                //}
                return true;
            }
        }

        public bool UseGate(Mobile from)
        {
            if (m_forceEntry)
            {
                if (m_list != null && m_entry != null)
                    return !CheckGate(from, m_list, m_entry);

                return true;
            }
            else if (OldMoongates && (this.Map == Map.Felucca || this.Map == Map.Trammel))
            {
                PMEntry entry;
                PMList list;

                GetGateEntry(this, out entry, out list);

                if (list != null && entry != null)
                    return !CheckGate(from, list, entry);

                return true;
            }
            else
            {
                from.CloseGump(typeof(MoongateGump));
                from.SendGump(new MoongateGump(from, this));

                if (!from.Hidden || from.AccessLevel == AccessLevel.Player)
                    Effects.PlaySound(from.Location, from.Map, 0x20E);

                return true;
            }
        }

        public static void GetGateEntry(PublicMoongate gate, out PMEntry entry, out PMList list) // For Old Style Moongates
        {
            #region World Zone
            if (WorldZone.IsInside(gate.GetWorldLocation(), gate.Map) && WorldZone.ActiveZone.PublicMoongateList != null)
                list = WorldZone.ActiveZone.PublicMoongateList;
            #endregion
            else if (gate.Map == Map.Felucca)
                list = PMList.Felucca;
            else
                list = PMList.Trammel;

            if (list.Entries.Length == 0)
            {
                entry = null;
                return;
            }

            int gateNum = 0;

            for (int i = 0; i < list.Entries.Length; i++)
            {
                entry = list.Entries[i];

                if (gate.Location == entry.Location)
                {
                    gateNum = i;
                    break;
                }
            }

            int hours, minutes;

            Clock.GetTime(gate.Map, gate.X, gate.Y, out hours, out minutes);

            int cycle = (60 * hours + minutes) % 120;
            int steps = 0;
            if (cycle > 7) ++steps;
            if (cycle > 27) ++steps;
            if (cycle > 37) ++steps;
            if (cycle > 57) ++steps;
            if (cycle > 67) ++steps;
            if (cycle > 87) ++steps;
            if (cycle > 97) ++steps;
            if (cycle > 117) steps = 0;

            int destNum = (gateNum + steps) % list.Entries.Length;

            entry = list.Entries[destNum];

            //{
            //    /* Debugging */            
            //    int generalNum;
            //    string exactTime;
            //    Clock.GetTime(gate.Map, gate.X, gate.Y, out generalNum, out exactTime);
            //    Console.WriteLine("\ngateNum: {0}", gateNum);
            //    Console.WriteLine("steps: {0}", steps);
            //    Console.WriteLine("destNum: {0}", destNum);
            //    Console.WriteLine("destXYZ: {0}, {1}, {2}", entry.Location.X, entry.Location.Y, entry.Location.Z);
            //    Console.WriteLine("Time: " + exactTime);
            //}
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
        }

        public static void Initialize()
        {
            CommandSystem.Register("MoonGen", AccessLevel.Owner, new CommandEventHandler(MoonGen_OnCommand));
        }

        [Usage("MoonGen")]
        [Description("Generates public moongates. Removes all old moongates.")]
        public static void MoonGen_OnCommand(CommandEventArgs e)
        {
            int count = DeleteAll();

            World.Broadcast(0x35, true, "{0} moongates removed.", count);

            count = GenerateAll();

            World.Broadcast(0x35, true, "{0} moongates generated.", count);
        }

        public static int DeleteAll()
        {
            List<Item> list = new List<Item>();

            foreach (Item item in World.Items.Values)
            {
                if (item is PublicMoongate)
                    list.Add(item);
            }

            foreach (Item item in list)
                item.Delete();

            return list.Count;
        }

        public static int GenerateAll()
        {
            int count = 0;

            count += Generate(PMList.Trammel);
            count += Generate(PMList.Felucca);
            count += Generate(PMList.Ilshenar);
            count += Generate(PMList.Malas);
            count += Generate(PMList.Tokuno);

            return count;
        }

        public static Dictionary<Item, Tuple<Point3D, Map, bool>> GenerateAll(Map[] maps, bool listOnly = false)
        {
            Dictionary<Item, Tuple<Point3D, Map, bool>> changeLog = new();
            int count = 0;
            count += Generate(PMList.Trammel);
            count += Generate(PMList.Felucca);
            count += Generate(PMList.Ilshenar);
            count += Generate(PMList.Malas);
            count += Generate(PMList.Tokuno);

            return changeLog;
        }

        public static int Generate(PMList list, bool listOnly = false, Dictionary<Item, Tuple<Point3D, Map, bool>> changeLog = null)
        {
            foreach (PMEntry entry in list.Entries)
            {
                Item item = new PublicMoongate();

                if (listOnly)
                    changeLog.Add(item, new Tuple<Point3D, Map, bool>(entry.Location, list.Map, false));
                else
                {
                    List<Item> pm = Utility.FindItemAt(entry.Location, list.Map, typeof(PublicMoongate));
                    foreach (Item moongate in pm)
                        if (moongate is not null)
                            moongate.Delete();

                    item.MoveToWorld(entry.Location, list.Map);
                }

                if (entry.Number == 1060642) // Umbra
                    item.Hue = 0x497;
            }

            return list.Entries.Length;
        }
    }

    public class PMEntry
    {
        private Point3D m_Location;
        private TextEntry m_Number;
        private TextEntry m_DescNumber; // Added to support Old Style Moongates

        public Point3D Location
        {
            get
            {
                return m_Location;
            }
        }

        public TextEntry Number
        {
            get
            {
                return m_Number;
            }
        }

        public TextEntry Description
        {
            get
            {
                return m_DescNumber;
            }
        }

        public PMEntry(Point3D loc, TextEntry number)
            : this(loc, number, 1005397) //The moongate is cloudy, and nothing can be made out. 
        {
        }

        public PMEntry(Point3D loc, TextEntry number, TextEntry description)
        {
            m_Location = loc;
            m_Number = number;
            m_DescNumber = description;
        }
    }

    public class PMList
    {
        private TextEntry m_Number, m_SelNumber;
        private Map m_Map;
        private PMEntry[] m_Entries;

        public TextEntry Number
        {
            get
            {
                return m_Number;
            }
        }

        public TextEntry SelNumber
        {
            get
            {
                return m_SelNumber;
            }
        }

        public Map Map
        {
            get
            {
                return m_Map;
            }
        }

        public PMEntry[] Entries
        {
            get
            {
                return m_Entries;
            }
        }

        public PMList(TextEntry number, TextEntry selNumber, Map map, PMEntry[] entries)
        {
            m_Number = number;
            m_SelNumber = selNumber;
            m_Map = map;
            m_Entries = entries;
        }

        // **** Order changed to support old style Moongates **** //
        public static readonly PMList Trammel =
            new PMList(1012000, 1012012, Map.Trammel, new PMEntry[]
                {
                    new PMEntry( new Point3D( 1336, 1997, 5 ), 1012004, 1005390 ), // Britain
					new PMEntry( new Point3D( 4467, 1283, 5 ), 1012003, 1005389 ), // Moonglow
					new PMEntry( new Point3D( 3563, 2139, 34), 1012010, 1005396 ), // Magincia
					new PMEntry( new Point3D(  643, 2067, 5 ), 1012009, 1005395 ), // Skara Brae
					new PMEntry( new Point3D( 1828, 2948,-20), 1012008, 1005394 ), // Trinsic
					new PMEntry( new Point3D( 2701,  692, 5 ), 1012007, 1005393 ), // Minoc
					new PMEntry( new Point3D(  771,  752, 5 ), 1012006, 1005392 ), // Yew
					new PMEntry( new Point3D( 1499, 3771, 5 ), 1012005, 1005391 ), // Jhelom
                    // comment out New Haven entry for OSI correct Old Style Moongates
					//new PMEntry( new Point3D( 3450, 2677, 25), 1078098 )  // New Haven
				});

        // **** Order changed to support old style Moongates **** //
        public static readonly PMList Felucca =
            new PMList(1012001, 1012013, Map.Felucca, new PMEntry[]
                {
                    new PMEntry( new Point3D( 1336, 1997, 5 ), 1012004, 1005390 ), // Britain
					new PMEntry( new Point3D( 4467, 1283, 5 ), 1012003, 1005389 ), // Moonglow
					new PMEntry( new Point3D( 3563, 2139, 34), 1012010, 1005396 ), // Magincia
					new PMEntry( new Point3D(  643, 2067, 5 ), 1012009, 1005395 ), // Skara Brae
					new PMEntry( new Point3D( 1828, 2948,-20), 1012008, 1005394 ), // Trinsic
					new PMEntry( new Point3D( 2701,  692, 5 ), 1012007, 1005393 ), // Minoc
					new PMEntry( new Point3D(  771,  752, 5 ), 1012006, 1005392 ), // Yew
					new PMEntry( new Point3D( 1499, 3771, 5 ), 1012005, 1005391 ), // Jhelom
                    // comment out Buccaneer's Den entry for OSI correct Old Style Moongates
					//new PMEntry( new Point3D( 2711, 2234, 0 ), 1019001 )  // Buccaneer's Den
				});

        public static readonly PMList Ilshenar =
            new PMList(1012002, 1012014, Map.Ilshenar, new PMEntry[]
                {
                    new PMEntry( new Point3D( 1215,  467, -13 ), 1012015 ), // Compassion
					new PMEntry( new Point3D(  722, 1366, -60 ), 1012016 ), // Honesty
					new PMEntry( new Point3D(  744,  724, -28 ), 1012017 ), // Honor
					new PMEntry( new Point3D(  281, 1016,   0 ), 1012018 ), // Humility
					new PMEntry( new Point3D(  987, 1011, -32 ), 1012019 ), // Justice
					new PMEntry( new Point3D( 1174, 1286, -30 ), 1012020 ), // Sacrifice
					new PMEntry( new Point3D( 1532, 1340, - 3 ), 1012021 ), // Spirituality
					new PMEntry( new Point3D(  528,  216, -45 ), 1012022 ), // Valor
					new PMEntry( new Point3D( 1721,  218,  96 ), 1019000 )  // Chaos
				});

        public static readonly PMList Malas =
            new PMList(1060643, 1062039, Map.Malas, new PMEntry[]
                {
                    new PMEntry( new Point3D( 1015,  527, -65 ), 1060641 ), // Luna
					new PMEntry( new Point3D( 1997, 1386, -85 ), 1060642 )  // Umbra
				});

        public static readonly PMList Tokuno =
            new PMList(1063258, 1063415, Map.Tokuno, new PMEntry[]
                {
                    new PMEntry( new Point3D( 1169,  998, 41 ), 1063412 ), // Isamu-Jima
					new PMEntry( new Point3D(  802, 1204, 25 ), 1063413 ), // Makoto-Jima
					new PMEntry( new Point3D(  270,  628, 15 ), 1063414 )  // Homare-Jima
				});

        public static readonly PMList[] UORLists = new PMList[] { Trammel, Felucca };
        public static readonly PMList[] UORListsYoung = new PMList[] { Trammel };
        public static readonly PMList[] LBRLists = new PMList[] { Trammel, Felucca, Ilshenar };
        public static readonly PMList[] LBRListsYoung = new PMList[] { Trammel, Ilshenar };
        public static readonly PMList[] AOSLists = new PMList[] { Trammel, Felucca, Ilshenar, Malas };
        public static readonly PMList[] AOSListsYoung = new PMList[] { Trammel, Ilshenar, Malas };
        public static readonly PMList[] SELists = new PMList[] { Trammel, Felucca, Ilshenar, Malas, Tokuno };
        public static readonly PMList[] SEListsYoung = new PMList[] { Trammel, Ilshenar, Malas, Tokuno };
        public static readonly PMList[] RedLists = new PMList[] { Felucca };
        public static readonly PMList[] SigilLists = new PMList[] { Felucca };
    }

    public class MoongateGump : Gump
    {
        private Mobile m_Mobile;
        private PublicMoongate m_Moongate;
        private PMList[] m_Lists;

        public MoongateGump(Mobile mobile, PublicMoongate moongate)
            : base(100, 100)
        {
            m_Mobile = mobile;
            m_Moongate = moongate;

            PMList[] checkLists;

            if (mobile.Player)
            {
                #region World Zone
                if (WorldZone.IsInside(m_Moongate.GetWorldLocation(), m_Moongate.Map) && WorldZone.ActiveZone.PublicMoongateList != null)
                {
                    checkLists = WorldZone.ActiveZone.GetPMLists();
                }
                #endregion
                else if (Factions.Sigil.ExistsOn(mobile))
                {
                    checkLists = PMList.SigilLists;
                }
                // 8/10/22 Adam: should we consider Core.RedsInTown an this location?
                else if (mobile.Red)
                {
                    checkLists = PMList.RedLists;
                }
                else
                {
                    ClientFlags flags = mobile.NetState == null ? 0 : mobile.NetState.Flags;
                    bool young = mobile is PlayerMobile ? ((PlayerMobile)mobile).Young : false;

                    if (Core.SE && (flags & ClientFlags.Tokuno) != 0)
                        checkLists = young ? PMList.SEListsYoung : PMList.SELists;
                    else if (Core.AOS && (flags & ClientFlags.Malas) != 0)
                        checkLists = young ? PMList.AOSListsYoung : PMList.AOSLists;
                    else if ((flags & ClientFlags.Ilshenar) != 0)
                        checkLists = young ? PMList.LBRListsYoung : PMList.LBRLists;
                    else
                        checkLists = young ? PMList.UORListsYoung : PMList.UORLists;
                }
            }
            else
            {
                checkLists = PMList.SELists;
            }

            m_Lists = new PMList[checkLists.Length];

            for (int i = 0; i < m_Lists.Length; ++i)
                m_Lists[i] = checkLists[i];

            for (int i = 0; i < m_Lists.Length; ++i)
            {
                if (m_Lists[i].Map == mobile.Map)
                {
                    PMList temp = m_Lists[i];

                    m_Lists[i] = m_Lists[0];
                    m_Lists[0] = temp;

                    break;
                }
            }

            AddPage(0);

            AddBackground(0, 0, 380, 280, 5054);

            AddButton(10, 210, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(45, 210, 140, 25, 1011036, false, false); // OKAY

            AddButton(10, 235, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(45, 235, 140, 25, 1011012, false, false); // CANCEL

            AddHtmlLocalized(5, 5, 200, 20, 1012011, false, false); // Pick your destination:

            for (int i = 0; i < checkLists.Length; ++i)
            {
                AddButton(10, 35 + (i * 25), 2117, 2118, 0, GumpButtonType.Page, Array.IndexOf(m_Lists, checkLists[i]) + 1);
                TextEntry.AddHtmlText(this, 30, 35 + (i * 25), 150, 20, checkLists[i].Number, false, false);
            }

            for (int i = 0; i < m_Lists.Length; ++i)
                RenderPage(i, Array.IndexOf(checkLists, m_Lists[i]));
        }

        private void RenderPage(int index, int offset)
        {
            PMList list = m_Lists[index];

            AddPage(index + 1);

            AddButton(10, 35 + (offset * 25), 2117, 2118, 0, GumpButtonType.Page, index + 1);
            TextEntry.AddHtmlText(this, 30, 35 + (offset * 25), 150, 20, list.SelNumber, false, false);

            PMEntry[] entries = list.Entries;

            for (int i = 0; i < entries.Length; ++i)
            {
                AddRadio(200, 35 + (i * 25), 210, 211, false, (index * 100) + i);
                TextEntry.AddHtmlText(this, 225, 35 + (i * 25), 150, 20, entries[i].Number, false, false);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 0) // Cancel
                return;
            else if (m_Mobile.Deleted || m_Moongate.Deleted || m_Mobile.Map == null)
                return;

            int[] switches = info.Switches;

            if (switches.Length == 0)
                return;

            int switchID = switches[0];
            int listIndex = switchID / 100;
            int listEntry = switchID % 100;

            if (listIndex < 0 || listIndex >= m_Lists.Length)
                return;

            PMList list = m_Lists[listIndex];

            if (listEntry < 0 || listEntry >= list.Entries.Length)
                return;

            PMEntry entry = list.Entries[listEntry];

            m_Moongate.CheckGate(m_Mobile, list, entry);
        }
    }
}