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

/* Scripts/Items/SkillItems/CarpenterItems/TaxidermyKit.cs
 * CHANGELOG
 *  1/7/23, Yoar
 *      Trophies now retain their hue on redeed/replace.
 *  9/29/23, Yoar
 *      Fish Trophies now also display the weight/catcher stats if the weight is below 20 stones.
 *  2/26/22, Adam
 *      obsolete some parms
 *      make calls to the new music system
 *  1/20/22, Adam
 *      Condition music on IsMusical()
 *      Remove instrument added to player, and replace with a RazorInstrument.
 *  1/3/21, Adam (Serialization)
 *      Forgot to move the the correct case statement in serialization.
 *  1/2/21, Adam (Place[Hue])
 *      Allow the setting of the how the award owner 'placed' (1st, 2nd, or 3rd)
 *      Setting the Place selects a canned Resource hue for the trophy/deed
 *  12/23/21, Adam (music)
 *      added the ability for trophies to play music.
 *      added SongFile. If SongFile is not set, the Trophy acts as per usual.
 *	11/29/21, Yoar
 *	    Added m_Hunter, m_TrophyWeight in order to display fisher/weight for big fish trophies.
 *	11/29/21, Yoar
 *	    Merged int m_DeedNumber, string m_Deed fields into TextEntry m_DeedName.
 *	    Merged int m_AddonNumber, string m_Addon fields into TextEntry m_AddonName.
 *	11/14/21, Yoar
 *	    Added BaseBoard base class for wooden boards.
 *	04/07/05, Kitaras	
 *		Added overload methods to accept strings for deed and trophy name vs cliloc's, added Zvalue adjusment
 */

using Server.Commands;
using Server.Engines;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;
using System;
using System.IO;

namespace Server.Items
{
    [FlipableAttribute(0x1EBA, 0x1EBB)]
    public class TaxidermyKit : Item
    {
        public override int LabelNumber { get { return 1041279; } } // a taxidermy kit

        [Constructable]
        public TaxidermyKit()
            : base(0x1EBA)
        {
            Weight = 1.0;
        }

        public TaxidermyKit(Serial serial)
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
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (from.Skills[SkillName.Carpentry].Base < 90.0)
            {
                from.SendLocalizedMessage(1042594); // You do not understand how to use this.
            }
            else
            {
                from.SendLocalizedMessage(1042595); // Target the corpse to make a trophy out of.
                from.Target = new CorpseTarget(this);
            }
        }

        private static object[,] m_Table = new object[,]
            {
                { typeof( BrownBear ),      0x1E60,     1041093, 1041107 },
                { typeof( GreatHart ),      0x1E61,     1041095, 1041109 },
                { typeof( BigFish ),        0x1E62,     1041096, 1041110 },
                { typeof( Gorilla ),        0x1E63,     1041091, 1041105 },
                { typeof( Orc ),            0x1E64,     1041090, 1041104 },
                { typeof( PolarBear ),      0x1E65,     1041094, 1041108 },
                { typeof( Troll ),          0x1E66,     1041092, 1041106 }
            };

        private class CorpseTarget : Target
        {
            private TaxidermyKit m_Kit;

            public CorpseTarget(TaxidermyKit kit)
                : base(3, false, TargetFlags.None)
            {
                m_Kit = kit;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Kit.Deleted)
                    return;

                if (!(targeted is Corpse) && !(targeted is BigFish))
                {
                    from.SendLocalizedMessage(1042600); // That is not a corpse!
                }
                else if (targeted is Corpse && ((Corpse)targeted).VisitedByTaxidermist)
                {
                    from.SendLocalizedMessage(1042596); // That corpse seems to have been visited by a taxidermist already.
                }
                else if (!m_Kit.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                }
                else if (from.Skills[SkillName.Carpentry].Base < 90.0)
                {
                    from.SendLocalizedMessage(1042603); // You would not understand how to use the kit.
                }
                else
                {
                    object obj = targeted;

                    if (obj is Corpse)
                        obj = ((Corpse)obj).Owner;

                    for (int i = 0; obj != null && i < m_Table.GetLength(0); ++i)
                    {
                        if ((Type)m_Table[i, 0] == obj.GetType())
                        {
                            Container pack = from.Backpack;

                            if (pack != null && pack.ConsumeTotal(typeof(BaseBoard), 10))
                            {
                                from.SendLocalizedMessage(1042278); // You review the corpse and find it worthy of a trophy.
                                from.SendLocalizedMessage(1042602); // You use your kit up making the trophy.

                                TrophyDeed deed = new TrophyDeed((int)m_Table[i, 1] + 7, (int)m_Table[i, 1], (int)m_Table[i, 2], (int)m_Table[i, 3], 0);

                                from.AddToBackpack(deed);

                                if (targeted is Corpse)
                                {
                                    ((Corpse)targeted).VisitedByTaxidermist = true;
                                }
                                else if (targeted is BigFish)
                                {
                                    BigFish bigFish = (BigFish)targeted;

                                    deed.Hunter = bigFish.Fisher;
                                    deed.TrophyWeight = (int)bigFish.Weight;

                                    bigFish.Consume();
                                }

                                m_Kit.Delete();
                                return;
                            }
                            else
                            {
                                from.SendLocalizedMessage(1042598); // You do not have enough boards.
                                return;
                            }
                        }
                    }

                    from.SendLocalizedMessage(1042599); // That does not look like something you want hanging on a wall.
                }
            }
        }
    }

    [NoSort]
    public class TrophyAddon : Item
    {
        private int m_WestID;
        private int m_NorthID;
        private TextEntry m_DeedName;
        private TextEntry m_AddonName;
        private int m_OffsetZ;          // new variable to allow adjustments of z-values
        private string m_Hunter;
        private int m_TrophyWeight;
        private string m_RazorFile;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Rank
        {
            get { return PrizeAddon.GetRankFromHue(Hue); }
            set { Hue = PrizeAddon.GetHueFromRank(value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WestID { get { return m_WestID; } set { m_WestID = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NorthID { get { return m_NorthID; } set { m_NorthID = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry DeedName { get { return m_DeedName; } set { m_DeedName = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry AddonName { get { return m_AddonName; } set { this.Name = m_AddonName = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OffsetZ { get { return m_OffsetZ; } set { m_OffsetZ = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Hunter { get { return m_Hunter; } set { m_Hunter = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TrophyWeight { get { return m_TrophyWeight; } set { m_TrophyWeight = value; InvalidateProperties(); } }

        public override int LabelNumber { get { return m_AddonName.Number; } }

        [Constructable]
        public TrophyAddon(int itemID, int westID, int northID, TextEntry deedName, TextEntry addonName, int offsetZ, string hunter, int trophyWeight)
            : base(itemID)
        {
            Movable = false;
            m_WestID = westID;
            m_NorthID = northID;
            m_DeedName = deedName;
            this.Name = m_AddonName = addonName;
            m_OffsetZ = offsetZ;
            m_Hunter = hunter;
            m_TrophyWeight = trophyWeight;

            NextMusicTime.Start(0);                 // we can play now
            m_MusicPlayer.AllDone = EndMusic;
        }

        public TrophyAddon(Serial serial)
            : base(serial)
        {
        }

        #region Music Management
        [CommandProperty(AccessLevel.GameMaster)]
        public string RazorFile
        {
            get { return m_RazorFile; }
            set
            {
                if (value == null)
                    m_RazorFile = null;
                else if (!value.EndsWith(".rzr") || !File.Exists(Path.Combine(Core.DataDirectory, "song files", value)))
                    m_RazorFile = "Please enter: <name>.rzr";
                else
                {
                    CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(value);
                    SendSystemMessage(string.Format("Adding music {0}", data?.Song));

                    m_RazorFile = value;
                    // delete the rolled up sheet music if it exists
                    RolledUpSheetMusic rsm = GetRSM();
                    if (rsm != null)
                    {
                        SendSystemMessage(string.Format("Deleting music {0}", rsm));
                        RemoveItem(rsm);
                        rsm.Delete();
                    }
                }
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public virtual Item SheetMusic
        {
            get
            {
                foreach (Item item in PeekItems)
                    if (item is RolledUpSheetMusic rsm)
                        return rsm;
                return null;
            }
            set
            {
                Item musicObject = null;

                if (value != null && value is not RolledUpSheetMusic)
                {
                    this.SendSystemMessage("That is not rolled up sheet music");
                    return;
                }

                foreach (Item item in PeekItems)
                    if (item is RolledUpSheetMusic rsm)
                    {
                        musicObject = rsm;
                        break;
                    }

                if (musicObject != value)
                {
                    // delete an razor file reference if it exists
                    if (m_RazorFile != null)
                    {
                        if (GetRZR() != null)
                            SendSystemMessage(string.Format("Deleting music {0}", m_RazorFile));
                        m_RazorFile = null;
                    }
                    if (musicObject != null)
                    {
                        SendSystemMessage(string.Format("Deleting music {0}", musicObject));
                        RemoveItem(musicObject);
                        musicObject.Delete();
                    }

                    musicObject = value;

                    if (musicObject != null)
                    {
                        Item item = Utility.Dupe(musicObject);
                        SendSystemMessage(string.Format("Adding music {0}", item));
                        AddItem(item);
                    }
                }
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public string NextPlay
        {
            get
            {
                object music = GetMusic();
                if (music != null && !IsPlayLocked())
                {
                    if (NextMusicTime.Triggered)
                        return "Now";
                    else
                        return string.Format("in {0} seconds", NextMusicTime.Remaining / 1000);
                }

                return "Unknown at this time";
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public string Song
        {
            get
            {
                CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(GetMusic());
                return data?.Song;
            }
        }
        private int m_time_between = 12000;                // two minutes until the next playback
        [CommandProperty(AccessLevel.Seer)]
        public int DelayBetween
        {
            get { return m_time_between; }
            set { m_time_between = value; }
        }
        private int m_range_enter = 2;                      // I get this close to trigger the device
        [CommandProperty(AccessLevel.Seer)]
        public int RangeEnter
        {
            get { return m_range_enter; }
            set { m_range_enter = value; }
        }
        private int m_range_exit = 5;                       // I get this far away and the music stops
        [CommandProperty(AccessLevel.Seer)]
        public int RangeExit
        {
            get { return m_range_exit; }
            set { m_range_exit = value; }
        }
        private RolledUpSheetMusic GetRSM()
        {
            foreach (Item item in PeekItems)
                if (item is RolledUpSheetMusic rsm)
                    return rsm;
            return null;
        }
        private string GetRZR()
        {
            if (m_RazorFile != null && m_RazorFile.EndsWith(".rzr") && File.Exists(Path.Combine(Core.DataDirectory, "song files", m_RazorFile)))
                return m_RazorFile;
            return null;
        }
        private object GetMusic()
        {
            object music = GetRSM();
            if (music == null)
                music = GetRZR();
            return music;
        }
        private CalypsoMusicPlayer m_MusicPlayer = new CalypsoMusicPlayer();
        private Utility.LocalTimer NextMusicTime = new Utility.LocalTimer();
        public override bool HandlesOnMovement { get { return GetMusic() != null; } }
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {   // CanPlay blocks if: the player is already playing, OR another player nearby (15 tiles) is playing
            bool all_good = PlayMobile(m) && NextMusicTime.Triggered && !IsPlayLocked() && m.InRange(this, m_range_enter) && m.CanSee(this) && m.InLOS(this) && m_MusicPlayer.CanPlay(this);
            // check if this user is playing music elsewhere
            bool blocked = SystemMusicPlayer.IsUserBlocked(m_MusicPlayer.MusicContextMobile);
            if (all_good && !blocked)
            {
                object song = GetMusic();
                if (song != null)
                {
                    // some music devices use a fake mobile for music context. This will be the mobile we check for distance etc.
                    m_MusicPlayer.PlayerMobile = m;
                    m_MusicPlayer.RangeExit = m_range_exit;         // how far the mobile can be from this
                    //m_MusicPlayer.Play(m, this, song);
                    m_MusicPlayer.Play(Utility.MakeTempMobile(), this, song);
                    NextMusicTime.Stop();                           // just to block us. See EndMusic
                }
            }
            else if (Core.Debug && all_good && blocked)
                Utility.SendSystemMessage(m, 5, "Debug message: music not started because this player has initiated music elsewhere.");

            base.OnMovement(m, oldLocation);
        }
        private bool PlayMobile(Mobile m)
        {
            if (m is PlayerMobile pm)
            {
                if (pm.Hidden)
                {
                    if (pm.AccessLevel > AccessLevel.Player)
                    {
                        //if (!staff_notification.Recall(pm))
                        //{
                        //    SendSystemMessage("Hidden staff do not trigger music playback");
                        //    staff_notification.Remember(pm, 60);
                        //}
                    }
                    return false;
                }

                return true;
            }
            return false;
        }
        public void EndMusic(Mobile from)
        {   // called back from RazorInstrument
            NextMusicTime.Start(m_time_between);
            return;
        }
        private bool IsPlayLocked()
        {
            return !NextMusicTime.Running;
        }
        #endregion

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)7); // version

            // version 7
            writer.WriteEncodedInt(m_time_between);
            writer.WriteEncodedInt(m_range_enter);
            writer.WriteEncodedInt(m_range_exit);

            // older versions
            writer.Write((string)m_RazorFile);

            writer.Write((string)m_Hunter);
            writer.Write((int)m_TrophyWeight);

            m_DeedName.Serialize(writer);
            m_AddonName.Serialize(writer);

            writer.Write((int)m_OffsetZ);

            writer.Write((int)m_WestID);
            writer.Write((int)m_NorthID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 7: // eliminate instrument, add m_time_between
                    {
                        m_time_between = reader.ReadEncodedInt();
                        m_range_enter = reader.ReadEncodedInt();
                        m_range_exit = reader.ReadEncodedInt();
                        goto case 6;
                    }
                case 6:
                case 5:
                    {
                        if (version < 6)
                            reader.ReadInt(); // place

                        goto case 4;
                    }
                case 4:
                    {
                        m_RazorFile = reader.ReadString();

                        if (version <= 6)
                        {   // no longer needed
                            Item instrument = reader.ReadItem();

                            if (instrument != null)
                                instrument.Delete();
                        }

                        goto case 3;
                    }
                case 3:
                    {
                        m_Hunter = reader.ReadString();
                        m_TrophyWeight = reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        m_DeedName = new TextEntry(reader);
                        m_AddonName = new TextEntry(reader);
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 2)
                        {
                            m_DeedName = new TextEntry(m_DeedName.Number, reader.ReadString());
                            m_AddonName = new TextEntry(m_AddonName.Number, reader.ReadString());
                        }
                        m_OffsetZ = reader.ReadInt();
                        goto case 0;
                    }

                case 0:
                    {
                        m_WestID = reader.ReadInt();
                        m_NorthID = reader.ReadInt();
                        if (version < 2)
                        {
                            m_DeedName = new TextEntry(reader.ReadInt(), m_DeedName.String);
                            m_AddonName = new TextEntry(reader.ReadInt(), m_AddonName.String);
                        }
                        break;
                    }
            }

            NextMusicTime.Start(0);                 // we can play now
            m_MusicPlayer.AllDone = EndMusic;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (ItemID == 0x1E62 || ItemID == 0x1E69 || m_TrophyWeight >= 20)
            {
                if (m_Hunter != null)
                    list.Add(1070857, m_Hunter); // Caught by ~1_fisherman~

                list.Add(1070858, m_TrophyWeight.ToString()); // ~1_weight~ stones
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (ItemID == 0x1E62 || ItemID == 0x1E69 || m_TrophyWeight >= 20)
            {
                if (m_Hunter != null)
                    LabelTo(from, 1070857, m_Hunter); // Caught by ~1_fisherman~

                LabelTo(from, 1070858, m_TrophyWeight.ToString()); // ~1_weight~ stones
            }
        }
        public TrophyDeed GetDeed()
        {
            TrophyDeed deed = new TrophyDeed(m_WestID, m_NorthID, m_DeedName, m_AddonName, m_OffsetZ, m_Hunter, m_TrophyWeight);

            deed.Hue = Hue;
            deed.RazorFile = RazorFile;
            if (GetRSM() != null)
                deed.AddItem(GetRSM());

            deed.DelayBetween = m_time_between;
            deed.RangeEnter = m_range_enter;
            deed.RangeExit = m_range_exit;

            return deed;
        }
        public override void OnDoubleClick(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && house.IsCoOwner(from))
            {
                if (from.InRange(GetWorldLocation(), 1))
                {
                    TrophyDeed deed = new TrophyDeed(m_WestID, m_NorthID, m_DeedName, m_AddonName, m_OffsetZ, m_Hunter, m_TrophyWeight);

                    deed.Hue = Hue;
                    deed.RazorFile = RazorFile;
                    if (GetRSM() != null)
                        deed.AddItem(GetRSM());

                    deed.DelayBetween = m_time_between;
                    deed.RangeEnter = m_range_enter;
                    deed.RangeExit = m_range_exit;

                    from.AddToBackpack(deed);

                    Delete();
                }
                else
                {
                    from.SendLocalizedMessage(500295); // You are too far away to do that.
                }
            }
        }
    }

    [NoSort]
    [Flipable(0x14F0, 0x14EF)]
    public class TrophyDeed : Item
    {
        private int m_WestID;
        private int m_NorthID;
        private TextEntry m_DeedName;
        private TextEntry m_AddonName;
        private int m_OffsetZ;
        private string m_Hunter;
        private int m_TrophyWeight;

        #region Music Management
        private string m_RazorFile;

        [CommandProperty(AccessLevel.GameMaster)]
        public string RazorFile
        {
            get { return m_RazorFile; }
            set
            {
                if (value == null)
                    m_RazorFile = null;
                else if (!value.EndsWith(".rzr") || !File.Exists(Path.Combine(Core.DataDirectory, "song files", value)))
                    m_RazorFile = "Please enter: <name>.rzr";
                else
                {
                    CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(value);
                    SendSystemMessage(string.Format("Adding music {0}", data?.Song));

                    m_RazorFile = value;
                    // delete the rolled up sheet music if it exists
                    RolledUpSheetMusic rsm = GetRSM();
                    if (rsm != null)
                    {
                        SendSystemMessage(string.Format("Deleting music {0}", rsm));
                        RemoveItem(rsm);
                        rsm.Delete();
                    }
                }
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public Item SheetMusic
        {
            get
            {
                foreach (Item item in PeekItems)
                    if (item is RolledUpSheetMusic rsm)
                        return rsm;
                return null;
            }
            set
            {
                Item musicObject = null;

                if (value != null && value is not RolledUpSheetMusic)
                {
                    this.SendSystemMessage("That is not rolled up sheet music");
                    return;
                }

                foreach (Item item in PeekItems)
                    if (item is RolledUpSheetMusic rsm)
                    {
                        musicObject = rsm;
                        break;
                    }

                if (musicObject != value)
                {
                    // delete an razor file reference if it exists
                    if (m_RazorFile != null)
                    {
                        if (GetRZR() != null)
                            SendSystemMessage(string.Format("Deleting music {0}", m_RazorFile));
                        m_RazorFile = null;
                    }
                    if (musicObject != null)
                    {
                        SendSystemMessage(string.Format("Deleting music {0}", musicObject));
                        RemoveItem(musicObject);
                        musicObject.Delete();
                    }

                    musicObject = value;

                    if (musicObject != null)
                    {
                        Item item = Utility.Dupe(musicObject);
                        item.SetItemBool(ItemBoolTable.StaffOwned, false);
                        SendSystemMessage(string.Format("Adding music {0}", item));
                        AddItem(item);
                    }
                }
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public string Song
        {
            get
            {
                CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(GetMusic());
                return data?.Song;
            }
        }
        private int m_time_between = 12000;                 // two minutes until the next playback
        [CommandProperty(AccessLevel.Seer)]
        public int DelayBetween
        {
            get { return m_time_between; }
            set { m_time_between = value; }
        }
        private int m_range_enter = 2;                      // I get this close to trigger the device
        [CommandProperty(AccessLevel.Seer)]
        public int RangeEnter
        {
            get { return m_range_enter; }
            set { m_range_enter = value; }
        }
        private int m_range_exit = 5;                       // I get this far away and the music stops
        [CommandProperty(AccessLevel.Seer)]
        public int RangeExit
        {
            get { return m_range_exit; }
            set { m_range_exit = value; }
        }
        private RolledUpSheetMusic GetRSM()
        {
            foreach (Item item in PeekItems)
                if (item is RolledUpSheetMusic rsm)
                    return rsm;
            return null;
        }
        private string GetRZR()
        {
            if (m_RazorFile != null && m_RazorFile.EndsWith(".rzr") && File.Exists(Path.Combine(Core.DataDirectory, "song files", m_RazorFile)))
                return m_RazorFile;
            return null;
        }
        private object GetMusic()
        {
            object music = GetRSM();
            if (music == null)
                music = GetRZR();
            return music;
        }
        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public int Rank
        {
            get { return PrizeAddon.GetRankFromHue(Hue); }
            set { Hue = PrizeAddon.GetHueFromRank(value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WestID { get { return m_WestID; } set { m_WestID = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NorthID { get { return m_NorthID; } set { m_NorthID = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry DeedName { get { return m_DeedName; } set { this.Name = m_DeedName = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry AddonName { get { return m_AddonName; } set { m_AddonName = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OffsetZ { get { return m_OffsetZ; } set { m_OffsetZ = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Hunter { get { return m_Hunter; } set { m_Hunter = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TrophyWeight { get { return m_TrophyWeight; } set { m_TrophyWeight = value; } }

        public override int LabelNumber { get { return m_DeedName.Number; } }

        [Constructable]
        public TrophyDeed()
            : this(0x1, 0x1, TextEntry.Empty, TextEntry.Empty, 0, null, 0)
        {
        }

        [Constructable]
        public TrophyDeed(int westID, int northID, TextEntry deedName, TextEntry addonName, int offsetZ)
            : this(westID, northID, deedName, addonName, offsetZ, null, 0)
        {
        }

        [Constructable]
        public TrophyDeed(int westID, int northID, TextEntry deedName, TextEntry addonName, int offsetZ, string hunter, int trophyWeight)
            : base(0x14F0)
        {
            m_WestID = westID;
            m_NorthID = northID;
            this.Name = m_DeedName = deedName;
            m_AddonName = addonName;
            m_OffsetZ = offsetZ;
            m_Hunter = hunter;
            m_TrophyWeight = trophyWeight;
        }

        public TrophyDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)7); // version

            // version 7
            writer.WriteEncodedInt(m_range_enter);
            writer.WriteEncodedInt(m_range_exit);
            writer.Write(m_time_between);

            // version 6
            writer.Write((string)m_RazorFile);

            // older versions
            writer.Write((string)m_Hunter);
            writer.Write((int)m_TrophyWeight);

            m_DeedName.Serialize(writer);
            m_AddonName.Serialize(writer);

            writer.Write((int)m_OffsetZ);

            writer.Write((int)m_WestID);
            writer.Write((int)m_NorthID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 7:
                    {
                        m_range_enter = reader.ReadEncodedInt();
                        m_range_exit = reader.ReadEncodedInt();
                        m_time_between = reader.ReadInt();
                        goto case 6;
                    }
                case 6:
                case 5:
                    {
                        if (version < 6)
                            reader.ReadInt(); // place

                        goto case 4;
                    }
                case 4:
                    {
                        m_RazorFile = reader.ReadString();
                        goto case 3;
                    }
                case 3:
                    {
                        m_Hunter = reader.ReadString();
                        m_TrophyWeight = reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        m_DeedName = new TextEntry(reader);
                        m_AddonName = new TextEntry(reader);
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 2)
                        {
                            m_DeedName = new TextEntry(m_DeedName.Number, reader.ReadString());
                            m_AddonName = new TextEntry(m_AddonName.Number, reader.ReadString());
                        }
                        m_OffsetZ = reader.ReadInt();
                        goto case 0;
                    }

                case 0:
                    {
                        m_WestID = reader.ReadInt();
                        m_NorthID = reader.ReadInt();
                        if (version < 2)
                        {
                            m_DeedName = new TextEntry(reader.ReadInt(), m_DeedName.String);
                            m_AddonName = new TextEntry(reader.ReadInt(), m_AddonName.String);
                        }
                        break;
                    }
            }
        }

        public static bool IsWall(int x, int y, int z, Map map)
        {
            if (map == null)
                return false;

            StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile t = tiles[i];
                ItemData id = TileData.ItemTable[t.ID & 0x3FFF];

                if ((id.Flags & TileFlag.Wall) != 0 && (z + 16) > t.Z && (t.Z + t.Height) > z)
                    return true;
            }

            return false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house != null && house.IsCoOwner(from))
                {
                    bool northWall = IsWall(from.X, from.Y - 1, from.Z, from.Map);
                    bool westWall = IsWall(from.X - 1, from.Y, from.Z, from.Map);

                    if (northWall && westWall)
                    {
                        switch (from.Direction & Direction.Mask)
                        {
                            case Direction.North:
                            case Direction.South: northWall = true; westWall = false; break;

                            case Direction.East:
                            case Direction.West: northWall = false; westWall = true; break;

                            default: from.SendMessage("Turn to face the wall on which to hang this trophy."); return;
                        }
                    }

                    int itemID = 0;

                    if (northWall)
                        itemID = m_NorthID;
                    else if (westWall)
                        itemID = m_WestID;
                    else
                        from.SendLocalizedMessage(1042626); // The trophy must be placed next to a wall.

                    if (itemID > 0)
                    {
                        TrophyAddon trophy = new TrophyAddon(itemID, m_WestID, m_NorthID, m_DeedName, m_AddonName, m_OffsetZ, m_Hunter, m_TrophyWeight);

                        trophy.Hue = Hue;
                        trophy.RazorFile = RazorFile;
                        if (GetRSM() != null)
                            trophy.AddItem(GetRSM());

                        trophy.DelayBetween = m_time_between;
                        trophy.RangeEnter = m_range_enter;
                        trophy.RangeExit = m_range_exit;

                        trophy.MoveToWorld(new Point3D(from.X, from.Y, from.Z + m_OffsetZ), from.Map);

                        house.Addons.Add(trophy);

                        Delete();
                    }
                }
                else
                {
                    from.SendLocalizedMessage(502092); // You must be in your house to do this.
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }
    }
}