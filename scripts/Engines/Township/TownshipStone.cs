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

/* Scripts/Engines/Township/TownshipStone.cs
 * CHANGELOG:
 *  6/19/2024, Adam
 *      Fix a bug that was causing a failure to decay 'visits' in a timely manner.
 *      Schedule daily Township charges to happen at 12:00 AM UTC (GMT)
 *      Add a Subsidies gump to display your pending credits.
 *      Add ForceBumpNow property for debugging the advancement of time
 *  3/14/2024, Adam (Pack Up Township)
 *      For BaseAddon, we need to stuff the addon hue into the deed. This will be reflected when the addon is instantiated.
 *  10/14/2023, Adam (MakeLivestock)
 *      clear any control orders since 1) they were tame and may be in 'speedy mode', 2) they are no longer controlled.
 *      bc.ControlOrder = OrderType.Stop;
 *  9/13/2023, Adam (item.IsLockedDown)
 *      Set/clear item.IsLockedDown for township lockdowns/items.
 *      This allows items to be queried on a global level, essentially, if they are a house or township lockdown
 *  9/3/2023, Adam (CanView) 
 *      Comment code that gives access to the gump to anyone in the house
 *  7/22/2023, Adam
 *      Allow locking down of any item within the township by township members.
 *  6/16/23, Yoar
 *      Fixed item cleanup. Now looping over ItemRegistry, which contains all of our items/addons
 *  6/8/23, Yoar
 *      Now implements IOracleDestination
 *  12/10/22, Adam (ReduceRegion()) : TODO
 *      We need to cleanup deco here which was orphaned when the township shrank.
 *      Look at the cleanup code in this file in: override void Delete()
 *  10/18/22, Adam (Delete)
 *      Have the township cleanup township items and addons when the stone is deleted.
 *  4/16/22, Adam (OnSpeech)
 *      Make sure speaker isn't in a house when issuing township speech commands.
 *  3/30/22, Adam
 *      added 'heat of battle' checks to RemoveThySelf
 *  3/28/22, Adam
 *      Made a couple of the methods for township lock downs public for access elsewhere
 *  3/28/22, Adam (remove thyself)
 *      Added remove thyself processing for townships.
 *      Players can easily build stuck spots with deco. This works much remove thyself within a house.
 * 3/23/22, Yoar
 *      Players now have ownership of any AddonComponent that is part of an owned addon.
 * 3/20/22, Adam (Township Lock downs)
 *      1. Add subsystem for managing township lock downs.
 *          Players can lockdown and release only decorative plants at this time.
 *      2. register each township stone to receive text spoken anywhere in the region.
 *          This is used for at least the township lockDown system
 * 2/27/22, Yoar
 *      Refactored visitors counter.
 *      Added VisitorCounter object: Keeps track of visitor counts during the week.
 * 2/18/22, Yoar
 *      More township cleanups.
 *      Added IsMember, IsAlly, IsMemberOrAlly, IsLeader, Contains helper methods.
 * 2/18/22, Yoar
 *      Rewrote township gump view rules. Use CanView to verify whether we can view the township gump.
 *      Added player command: '[township' (otherwise '[ts') to open the township gump.
 * 2/15/22, Yoar
 *      More township cleanups.
 *      Now consistently using either the term 'charge' or 'fee' to describe flat payments vs daily fees respectively.
 * 1/13/22, Yoar
 *		Added township item registry.
 * 1/12/22, Yoar
 *		Township cleanups.
 * 11/24/21, Yoar
 *      Added WelcomeMat property. This property refers to the item that marks the township's "welcome location".
 * 7/13/10, Pix
 *      Change so all allies can access stone - instead of allies who are friends of the house that the stone is in.
 * 5/8/10, adam
 *		Add TownshipMember(Mobile m) to determine if some random mobile is a township member
 * 2010-05-02, SMD
 *      ensure lightlevel is set to -1 - this makes the township follow normal lightcycle rules
 * 8/2/08, Pix
 *		Added UpdateRegionName() function for when a guild name changes.
 * 8/2/08, Pix
 *		Added CanExtendResult enum and return this from CanExtend() function (instead of a bool).
 * 7/20/08, Pix
 *		De-coupled township stones from houses.
 *	07/13/08, Pix
 *		Added TownshipCenter variable/property to enable possible movement of the stone.
 *	01/18/08, Pix
 *		Allies that are kin-interferers will no longer be considered enemies.
 *	12/11/07, Pix
 *		Allies that are friends of the house can now access the township stone.
 *	5/14/07, Pix
 *		On delete of township stone, make sure to remove the townshipstone from the global list
 *	5/14/07, Pix
 *		Fixed growth (after none->low, it always took 1 week longer than wanted)
 *		Added accessor to WeeksAtThisLevel
 *	Pix: 5/3/07, Pix
 *		Fixed potential re-setting of travel rules on restart.
 *	Pix: 4/30/07,
 *		Changed enter message under 'normal' counting rules.
 *		Added property for ALLastCalculated.
 *	Pix: 4/22/07,
 *		Tweaked enter message to make it consistent with the exit message.
 *	Pix: 3/18/07
 *		Fixed NoGateOut charge.
 *	Pix: 4/19/07
 *		Added dials for all fees/charges and modifiers.
 */

using Server.Diagnostics;
using Server.Engines.Plants;
using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.StaticHousing;
using Server.Regions;
using Server.Targeting;
using Server.Township;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Items
{
    public class TownshipStone : CustomRegionControl, IOracleDestination
    {
        public const int MAX_GOLD_HELD = 10000000;
        public const int INITIAL_RADIUS = 50;
        public const int EXTENDED_RADIUS = 75;

        private static readonly List<TownshipStone> m_Instances = new List<TownshipStone>();

        public static List<TownshipStone> AllTownshipStones { get { return m_Instances; } }

        public static void Initialize()
        {
            CommandSystem.Register("Township", AccessLevel.Player, new CommandEventHandler(Township_OnCommand));
            CommandSystem.Register("TS", AccessLevel.Player, new CommandEventHandler(Township_OnCommand));

            CommandSystem.Register("Stockpile", AccessLevel.Player, new CommandEventHandler(Stockpile_OnCommand));
        }

        #region Commands

        [Usage("Township")]
        [Aliases("TS")]
        [Description("Opens the township stone menu.")]
        public static void Township_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(from);

            if (tsr == null || tsr.TStone == null)
            {
                from.SendMessage("You are not in a township!");
                return;
            }

            if (!tsr.TStone.CheckView(from) || !tsr.TStone.CheckAccess(from, Township.TownshipAccess.Ally))
                return;

            from.CloseGump(typeof(TownshipGump));
            from.SendGump(new TownshipGump(tsr.TStone, from));
        }

        [Usage("Stockpile")]
        [Description("Opens the township stockpile menu.")]
        public static void Stockpile_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(from);

            if (tsr == null || tsr.TStone == null)
            {
                from.SendMessage("You are not in a township!");
                return;
            }

            if (!tsr.TStone.CheckView(from))
                return;

            if (!tsr.TStone.AllowBuilding(from))
            {
                from.SendMessage("You have no building rights in this township.");
                return;
            }

            from.CloseGump(typeof(TownshipGump));
            from.SendGump(new TownshipGump(tsr.TStone, from, TownshipGump.Page.Stockpile));
        }

        #endregion

        public override string DefaultName { get { return "a township stone"; } }

        private Guild m_Guild;
        private DateTime m_BuiltOn;
        private int m_GoldHeld;
        private Point3D m_TownshipCenter;
        private Township.TownshipStockpile m_Stockpile;

        [CommandProperty(AccessLevel.GameMaster)]
        public Guild Guild
        {
            get { return m_Guild; }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildName
        {
            get
            {
                if (m_Guild != null)
                    return m_Guild.Name;

                return string.Empty;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildAbbreviation
        {
            get
            {
                if (m_Guild != null)
                    return m_Guild.Abbreviation;

                return string.Empty;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime BuiltOn
        {
            get { return m_BuiltOn; }
            set { m_BuiltOn = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GoldHeld
        {
            get { return m_GoldHeld; }
            set { m_GoldHeld = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double RLDaysLeftInFund
        {
            get { return (double)this.GoldHeld / (double)this.TotalFeePerDay; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D TownshipCenter
        {
            get { return m_TownshipCenter; }
            set
            {
                if (m_TownshipCenter != value)
                {
                    m_TownshipCenter = value;

                    RemakeRegion();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Township.TownshipStockpile Stockpile
        {
            get { return m_Stockpile; }
            set { m_Stockpile = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double HouseOwnershipGuilded
        {
            get { return TownshipDeed.CalculateHouseOwnership(m_TownshipCenter, this.Map, m_Extended ? EXTENDED_RADIUS : INITIAL_RADIUS, m_Guild, false); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double HouseOwnershipAllied
        {
            get { return TownshipDeed.CalculateHouseOwnership(m_TownshipCenter, this.Map, m_Extended ? EXTENDED_RADIUS : INITIAL_RADIUS, m_Guild, true); }
        }

        public TownshipStone()
            : this(null)
        {
        }

        public TownshipStone(Guild g)
            : base()
        {
            ItemID = 0xED4;
            Hue = Township.TownshipSettings.Hue;
            Visible = true;

            InitRegionProps();

            m_BuiltOn = Now();
            m_Enemies = new List<Mobile>();
            m_ItemRegistry = new Township.TownshipItemRegistry(this);
            m_Stockpile = new Township.TownshipStockpile();
            m_BuildingPermits = new List<Mobile>();
            m_Livestock = new Dictionary<BaseCreature, Mobile>();
            m_Messages = new List<MessageEntry>();
            m_TownshipNPCs = new List<Mobile>();

            if (g != null)
            {
                m_Guild = g;
                m_Guild.TownshipStone = this;

                UpdateRegionName();

                foreach (BaseHouse house in this.TownshipHouses)
                {
                    if (house.Owner != null && IsMember(house.Owner))
                        house.LastTraded = Now().AddSeconds(10.0);
                }

                CustomRegion.GuildAlignment = g.Alignment;
            }

            m_Instances.Add(this);
        }

        public TownshipStone(Serial serial)
            : base(serial)
        {
            InitRegionProps();

            m_Enemies = new List<Mobile>();
            m_ItemRegistry = new Township.TownshipItemRegistry(this);
            m_Stockpile = new Township.TownshipStockpile();
            m_BuildingPermits = new List<Mobile>();
            m_Livestock = new Dictionary<BaseCreature, Mobile>();
            m_Messages = new List<MessageEntry>();
            m_TownshipNPCs = new List<Mobile>();

            m_Instances.Add(this);
        }

        private void InitRegionProps()
        {
            CustomRegion.IsGuarded = false;
            CustomRegion.CanRessurect = true;
            CustomRegion.CanUsePotions = true;
            CustomRegion.EnableStuckMenu = true;
            CustomRegion.EnableHousing = true;
            CustomRegion.LightLevel = -1;
            CustomRegion.AllowTravelSpellsInRegion = true;
            CustomRegion.NoGateInto = false;
            CustomRegion.NoRecallInto = false;
        }

        public override void UpdateHue()
        {
            // do not change the hue
        }

        public void HandleSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!e.Handled)
            {
                Utility.ConsoleWriteLine(e.Speech, ConsoleColor.Cyan);

                if (e.HasKeyword(0x23)) // I wish to lock this down
                {
                    BeginLockdown(from);

                    e.Handled = true;
                }
                else if (e.HasKeyword(0x24)) // I wish to release this
                {
                    BeginRelease(from);

                    e.Handled = true;
                }
                else if (e.HasKeyword(0x33)) // remove thyself
                {
                    LeaveTownship(from);

                    e.Handled = true;
                }
                else if (e.Speech.ToLower() == "leave township")
                {
                    LeaveTownship(from);

                    e.Handled = true;
                }
                else if (e.Speech.ToLower() == "i wish to make this decorative" && (Core.RuleSets.DecorativeFurniture()))
                {
                    if (!HasAccess(from, Township.TownshipAccess.Ally))
                    {
                        from.SendMessage("You must be in your township to do this.");
                    }
                    else
                    {
                        from.SendMessage("Make what decorative?");
                        from.Target = new HouseDecoTarget(true, this);
                    }

                    e.Handled = true;
                }
                else if (e.Speech.ToLower() == "i wish to make this functional" && (Core.RuleSets.DecorativeFurniture()))
                {
                    if (!HasAccess(from, Township.TownshipAccess.Ally))
                    {
                        from.SendMessage("You must be in your township to do this.");
                    }
                    else
                    {
                        from.SendMessage("Make what functional?");
                        from.Target = new HouseDecoTarget(false, this);
                    }

                    e.Handled = true;
                }
                else if (e.Speech.ToLower() == "i wish to make this unrestricted access" && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules()))
                {
                    if (!HasAccess(from, Township.TownshipAccess.Ally))
                    {
                        from.SendMessage("You must be in your township to do this.");
                    }
                    else
                    {
                        from.SendMessage("Make what unrestricted access?");
                        from.Target = new TownshipAccessTarget(this, true);
                    }

                    e.Handled = true;
                }
                else if (e.Speech.ToLower() == "i wish to make this restricted access" && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules()))
                {
                    if (!HasAccess(from, Township.TownshipAccess.Ally))
                    {
                        from.SendMessage("You must be in your township to do this.");
                    }
                    else
                    {
                        from.SendMessage("Make what restricted access?");
                        from.Target = new TownshipAccessTarget(this, false);
                    }

                    e.Handled = true;
                }
            }
        }

        #region Leave Township

        private static readonly Memory m_LeaveTownshipMemory = new Memory();

        public void LeaveTownship(Mobile from)
        {
            if (this.CustomRegion == null || this.CustomRegion.Coords.Count == 0 || this.CustomRegion.Map == null)
                return;

            if (IsAlly(from) && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("Allies of the township may not use this command.");
            }
            else if (from.Criminal && from.AccessLevel == AccessLevel.Player)
            {
                from.SendLocalizedMessage(1005270); // Thou'rt a criminal and cannot escape so easily...
            }
            else if (Spells.SpellHelper.CheckCombat(from) && from.AccessLevel == AccessLevel.Player)
            {
                from.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
            }
            else if (m_LeaveTownshipMemory.Recall(from) && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("You may only use this command once every 30 minutes.");
            }
            else
            {
                m_LeaveTownshipMemory.Remember(from, 60.0 * 30.0); // 60 seconds * 30 = 30 minutes

                List<Point3D> list = new List<Point3D>();

                Utility.CalcRegionBounds(this.CustomRegion, ref list);

                Point3D targetLoc = Point3D.Zero;

                for (int i = 0; targetLoc == Point3D.Zero && list.Count != 0 && i < 50; i++)
                {
                    int rnd = Utility.Random(list.Count);

                    Point3D p = list[Utility.Random(list.Count)];

                    if (this.CustomRegion.Map.CanSpawnLandMobile(p))
                        targetLoc = p;
                    else
                        list.RemoveAt(rnd);
                }

                if (targetLoc != Point3D.Zero)
                {
                    BaseCreature.TeleportPets(from, targetLoc, this.CustomRegion.Map);
                    from.MoveToWorld(targetLoc, this.CustomRegion.Map);

                    from.SendMessage("You have been teleported.");
                }
            }
        }

        #endregion

        public void UpdateRegionName()
        {
            CustomRegion.Name = string.Format("The township of {0}", GuildName);
        }

        public override CustomRegion CreateRegion(CustomRegionControl rc)
        {
            return new TownshipRegion(rc);
        }

        public Rectangle3D GetBounds()
        {
            if (CustomRegion != null && CustomRegion.Coords.Count != 0)
                return CustomRegion.Coords[0];

            return new Rectangle3D();
        }

        public bool IsMember(Mobile m)
        {
            return (m_Guild != null && m_Guild.IsMember(m));
        }

        public bool IsAlly(Mobile m)
        {
            return (m_Guild != null && m.Guild is Guild && m_Guild.IsAlly((Guild)m.Guild));
        }

        public bool IsMemberOrAlly(Mobile m)
        {
            return (IsMember(m) || IsAlly(m));
        }

        public bool IsLeader(Mobile m)
        {
            return (m_Guild != null && m_Guild.Leader != null && m_Guild.Leader == m);
        }

        public bool IsCoLeader(Mobile m)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            return (house != null && house.IsCoOwner(m));
        }

        public Township.TownshipAccess GetAccess(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
                return Township.TownshipAccess.Leader;

            if (IsLeader(m))
                return Township.TownshipAccess.Leader;

            if (IsCoLeader(m))
                return Township.TownshipAccess.CoLeader;

            if (IsMember(m))
                return Township.TownshipAccess.Member;

            if (IsAlly(m))
                return Township.TownshipAccess.Ally;

            if (IsEnemy(m))
                return Township.TownshipAccess.Enemy;

            return Township.TownshipAccess.Neutral;
        }

        public bool HasAccess(Mobile m, Township.TownshipAccess access)
        {
            return (GetAccess(m) >= access);
        }

        public bool CheckAccess(Mobile m, Township.TownshipAccess access)
        {
            Township.TownshipAccess mobAccess = GetAccess(m);

            if (mobAccess >= access)
                return true;

            switch (access)
            {
                case Township.TownshipAccess.Ally:
                    {
                        m.SendMessage("You must be an ally of the township to do this.");
                        break;
                    }
                case Township.TownshipAccess.Member:
                    {
                        m.SendMessage("You must be a member of the township to do this.");
                        break;
                    }
                case Township.TownshipAccess.CoLeader:
                    {
                        m.SendMessage("You must be the co-leader of the township to do this.");
                        break;
                    }
                case Township.TownshipAccess.Leader:
                    {
                        m.SendMessage("You must be the leader of the township to do this.");
                        break;
                    }
                default:
                    {
                        if (mobAccess == Township.TownshipAccess.Enemy)
                            m.SendMessage("Enemies of the township cannot do this.");
                        else
                            m.SendMessage("You do not have the necessary township access to do this.");

                        break;
                    }
            }

            return false;
        }

        public bool Contains(Item item)
        {
            return Contains(item.GetWorldLocation(), item.Map);
        }

        public bool Contains(Mobile m)
        {
            return Contains(m.Location, m.Map);
        }

        public bool Contains(Point3D loc, Map map)
        {
            return (CustomRegion != null && CustomRegion.Map == map && CustomRegion.Contains(loc));
        }

        #region Enemies

        private List<Mobile> m_Enemies;

        public List<Mobile> Enemies
        {
            get { return m_Enemies; }
        }

        public bool IsEnemy(Mobile m)
        {
            Guild g = m.Guild as Guild;

            if (g != null && !m_Guild.IsAlly(g) && m_Guild.IsEnemy(g))
                return true;

            if (m_Enemies.Contains(m))
                return true;

            return false;
        }

        public void AddEnemy(Mobile m)
        {
            if (!m_Enemies.Contains(m))
                m_Enemies.Add(m);
        }

        public int SyncEnemies()
        {
            if (m_Guild == null)
                return 0;

            foreach (BaseHouse house in this.TownshipHouses)
            {
                if (house.Owner != null && IsMemberOrAlly(house.Owner))
                {
                    for (int i = 0; i < house.Bans.Count; i++)
                    {
                        Mobile m = house.Bans[i] as Mobile;

                        if (m != null)
                            AddEnemy(m);
                    }
                }
            }

            return m_Enemies.Count;
        }

        #endregion

        #region Transactions

        private List<Transaction> m_Withdrawals = new List<Transaction>();
        private List<Transaction> m_Deposits = new List<Transaction>();

        public List<Transaction> Withdrawals { get { return m_Withdrawals; } }
        public List<Transaction> Deposits { get { return m_Deposits; } }

        public void RecordWithdrawal(int value, string description)
        {
            while (m_Withdrawals.Count >= 10)
                m_Withdrawals.RemoveAt(m_Withdrawals.Count - 1);

            m_Withdrawals.Insert(0, new Transaction(Now(), value, description));
        }

        public void RecordDeposit(int value, string description)
        {
            while (m_Deposits.Count >= 10)
                m_Deposits.RemoveAt(m_Deposits.Count - 1);

            m_Deposits.Insert(0, new Transaction(Now(), value, description));
        }

        public string LastWithdrawalsHTML
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < m_Withdrawals.Count; i++)
                {
                    sb.Append("<br>");
                    sb.Append(m_Withdrawals[i]);
                }
                return sb.ToString();
            }
        }

        public string LastDepositsHTML
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < m_Deposits.Count; i++)
                {
                    sb.Append("<br>");
                    sb.Append(m_Deposits[i]);
                }
                return sb.ToString();
            }
        }

        public class Transaction
        {
            private DateTime m_Date;
            private int m_Value;
            private string m_Description;

            public DateTime Date { get { return m_Date; } }
            public int Value { get { return m_Value; } }
            public string Description { get { return m_Description; } }

            public Transaction(DateTime date, int value, string description)
            {
                m_Date = date;
                m_Value = value;
                m_Description = description;
            }

            public void Serialize(GenericWriter writer)
            {
                writer.WriteEncodedInt(1); // version

                writer.Write((DateTime)m_Date);
                writer.Write((int)m_Value);
                writer.Write((string)m_Description);
            }

            public Transaction(GenericReader reader, bool legacy)
            {
                int version = (legacy ? 0 : reader.ReadEncodedInt());

                switch (version)
                {
                    case 1:
                        {
                            m_Date = reader.ReadDateTime();
                            m_Value = reader.ReadInt();
                            m_Description = reader.ReadString();

                            break;
                        }
                    case 0:
                        {
                            m_Description = reader.ReadString();

                            break;
                        }
                }
            }

            public override string ToString()
            {
                // legacy format
                if (m_Date == DateTime.MinValue && m_Value == 0)
                    return m_Description;

                StringBuilder sb = new StringBuilder();

                if (m_Date != DateTime.MinValue)
                    sb.AppendFormat("[{0} {1}] ", m_Date.ToShortDateString(), m_Date.ToShortTimeString());

                if (!string.IsNullOrEmpty(m_Description))
                    sb.AppendFormat("{0}: ", m_Description);

                sb.Append(m_Value.ToString("N0"));

                return sb.ToString();
            }
        }

        #endregion

        #region Extend

        private bool m_Extended = false;
        private DateTime m_LastExtendedChange = DateTime.MinValue;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Extended
        {
            get { return m_Extended; }
            set
            {
                if (m_Extended != value)
                {
                    if (value)
                        ExtendRegion();
                    else
                        ReduceRegion();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastExtendedChange
        {
            get { return m_LastExtendedChange; }
            set { m_LastExtendedChange = value; }
        }

        public void ExtendRegion()
        {
            m_Extended = true;

            RemakeRegion();
        }

        public void ReduceRegion()
        {
            // 12/10/22, Adam: We need to cleanup deco here which was orphaned when the township shrank.
            //  Look at the cleanup code in this file in: public override void Delete()

            m_Extended = false;

            RemakeRegion();
        }

        public void RemakeRegion()
        {
            bool wasRegistered = CustomRegion.Registered;

            CustomRegion.Registered = false;

            CustomRegion.Coords.Clear();

            int radius = GetRadius();

            Point3D p1 = new Point3D(m_TownshipCenter.X - radius, m_TownshipCenter.Y - radius, Region.DefaultMinZ);
            Point3D p2 = new Point3D(m_TownshipCenter.X + radius + 1, m_TownshipCenter.Y + radius + 1, Region.DefaultMaxZ);

            CustomRegion.Coords.Add(new Rectangle3D(p1, p2));

            if (wasRegistered)
                CustomRegion.Registered = true;
        }

        public int GetRadius()
        {
            return (m_Extended ? EXTENDED_RADIUS : INITIAL_RADIUS);
        }
        #region BoolTable
        [Flags]
        public enum TownshipStoneBoolTable
        {
            None = 0x00,
            IsPackedUp = 0x01,
            HasLBTaxSubsidy = 0x02,
            HasLBFameSubsidy = 0x04,
        }
        private TownshipStoneBoolTable m_BoolTable;

        public void SetTownshipStoneBool(TownshipStoneBoolTable flag, bool value)
        {
            if (value)
                m_BoolTable |= flag;
            else
                m_BoolTable &= ~flag;
        }

        public bool GetTownshipStoneBool(TownshipStoneBoolTable flag)
        {
            return ((m_BoolTable & flag) != 0);
        }
        #endregion BoolTable
        [Flags]
        public enum ExtendFlag : byte
        {
            None = 0x0,
            ConflictingRegion = 0x1,
            HousingPercentage = 0x2,
        }

        public ExtendFlag GetExtendFlags()
        {
            ExtendFlag flags = ExtendFlag.None;

            if (TownshipDeed.HasConflictingRegion(this.TownshipCenter, this.Map, EXTENDED_RADIUS, this.CustomRegion))
                flags |= ExtendFlag.ConflictingRegion;

            if (TownshipDeed.CalculateHouseOwnership(this.TownshipCenter, this.Map, EXTENDED_RADIUS, m_Guild, true) < Township.TownshipSettings.GuildHousePercentage)
                flags |= ExtendFlag.HousingPercentage;

            return flags;
        }

        #endregion

        #region Daily Fees

        private string m_DailyFeesHTML = string.Empty;

        public string DailyFeesHTML
        {
            get { return m_DailyFeesHTML; }
        }

        //returns number of townships removed
        public static int DoAllTownshipFees()
        {
            int numberoftownships = m_Instances.Count;

            for (int i = m_Instances.Count - 1; i >= 0; i--)
            {
                m_Instances[i].DecayVisits();           // if the week is changing, decay the visits
                m_Instances[i].ApplyLBFameSubsity();    // apply any fame visits
                m_Instances[i].DoDailyTownshipFees();   // calc and apply any daily charges
            }

            return numberoftownships - m_Instances.Count;
        }
        private int CalcLBTaxSubsidy(int subtotal)
        {
            if (LBTaxSubsidy > 0)
            {
                int thirtyPercent = (int)(subtotal * .3);
                int cost = thirtyPercent * 2;
                int have = Math.Min(LBTaxSubsidy, cost);
                int discount = have / 2;
                return discount;
            }
            return 0;
        }
        private void ConsumeLBTaxSubsidy(int total)
        {
            if (LBTaxSubsidy > 0)
            {
                int cost = CalcLBTaxSubsidy(total);
                LBTaxSubsidy -= 2 * cost;
            }
            return;
        }
        public override string FormatName()
        {
            string name;

            if (m_Guild == null)
                name = "(unfounded)";
            else if ((name = m_Guild.Name) == null || (name = name.Trim()).Length <= 0)
                name = "(unnamed)";

            return string.Format($"0x{this.Serial.Value:X} \"{name}\"");
        }
        public bool DoDailyTownshipFees()
        {
            bool bReturn = true;
            int discount = 0;
            int feeForToday = GetTotalFeePerDay(recordFees: true, ref discount);

            // consume tax subsidies applied
            ConsumeLBTaxSubsidy(feeForToday + discount);

            if (discount > 0)
            {
                LogHelper logger = new LogHelper("Township Tax Subsidy.log", overwrite: false, sline: true);
                logger.Log($"{FormatName()} was awarded a discount of {discount} gold for '{Now().DayOfWeek.ToString()}'");
                logger.Finish();
            }

            if (feeForToday <= this.GoldHeld)
            {
                RecordWithdrawal(feeForToday, "Daily fees");
                this.GoldHeld -= feeForToday;
            }
            else
            {
                string message = string.Format("Township {0} is being deleted: daily fees: {1} funds: {2}",
                    this.GuildName, TotalFeePerDay, GoldHeld);
                Server.Diagnostics.LogHelper log = new Server.Diagnostics.LogHelper("township.log", false);
                log.Log(message);
                log.Finish();
                this.Delete();
            }

            // Fame and tax subsidies have been applied. Now record their current state
            RecordFameAndTaxBankInfo();

            //finally, after we've done the daily fees, make sure that we update our activity level if needed
            if (IsActivityLevelCalcNeeded())
                CalculateActivityLevel();

            return bReturn;
        }

        #region Lord British Subsidy 
        private int m_LBTaxSubsidy;
        private int m_CurrentWeekOfYear; // tells us when the week has changed (and to wipe our visit counts)
        private int m_LBFameSubsidy;
        private int m_LastLBFameSubsidy;
        private string m_FameAndTaxBankHTML = "No Data";
        public string FameAndTaxBankHTML
        {
            get { return m_FameAndTaxBankHTML; }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int LBTaxSubsidy
        {
            get { return m_LBTaxSubsidy; }
            set
            {
                m_LBTaxSubsidy = value;
                SetTownshipStoneBool(TownshipStoneBoolTable.HasLBTaxSubsidy, m_LBTaxSubsidy > 0);
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int LBFameSubsidy
        {
            get { return m_LBFameSubsidy; }
            set
            {
                m_LBFameSubsidy = value;
                SetTownshipStoneBool(TownshipStoneBoolTable.HasLBFameSubsidy, m_LBFameSubsidy > 0);
            }
        }
        #region Fame Constants
        /* Kill Point Cost of a visit (heuristic model)
             * average kill point yield from a champ spawn is about 85668
             * We want to award a township BOOMING visits should the township take down a champ
             * 41 visits are required each day (288/7)
             * So... we will convert each 2098 (85668/41/7) chunk of their LBFameSubsidy into one visit, not to exceed 41 visits for the day.
             * -- weekly values --
             * NoneToLow = 36;
             * LowToMedium = 72;
             * MediumToHigh = 144;
             * HighToBooming = 288;
            */

        public const int LBFameVisitCost = 288;             // cost in fame for one visit
        public const int LBFameDailyCost = 288 * 41;        // 288 damage points consumed for each visit awarded * 41 visits (BOOMING)
        public const int LBFameDailyAwardGoal = 41;         // we want to award this many visits per day (natural visits may reduce this number)
        public const int LBFameWeeklyAwardGoal = 41 * 7;    // we want to award this many visits per week (natural visits may reduce this number)

        private bool CheckFameApply()
        {
            DayOfWeek day = Now().DayOfWeek;
            int weekOfYear = System.Globalization.ISOWeek.GetWeekOfYear(Now());
            if (m_LastVisit == DateTime.MinValue || m_CurrentWeekOfYear != weekOfYear)
                // Ok: a fresh stone, or the Week Will Rollover
                return true;

            // okay if we've not reached our goal for the day
            return m_VisitorCounts[day] < LBFameDailyAwardGoal;
        }
        #endregion Fame Constants
        public void ApplyLBFameSubsity()
        {
            DayOfWeek day = Now().DayOfWeek;

            int temp = 0;
            while (LBFameSubsidy >= LBFameVisitCost && CheckFameApply())
            {
                LBFameSubsidy -= LBFameVisitCost;
                RegisterFakeVisit();    // Region.OnEnter() is where the week is rolled over
                temp++;
            }
            m_LastLBFameSubsidy = temp;

            if (m_LastLBFameSubsidy > 0)
            {
                LogHelper logger = new LogHelper("Township Fame Awards.log", overwrite: false, sline: true);
                logger.Log($"{FormatName()} was awarded {m_LastLBFameSubsidy} bonus visits for {Now().DayOfWeek.ToString()}");
                logger.Finish();
            }
        }
        private void RegisterFakeVisit()
        {
            if (CustomRegion != null)
            {
                PlayerMobile dummy = new PlayerMobile();
                dummy.Player = true;
                CustomRegion.OnEnter(dummy);    // this where visits are counted
                Timer.DelayCall(TimeSpan.FromSeconds(5), new TimerStateCallback(DeleteDummyTick), new object[] { dummy });
            }
        }
        private void DeleteDummyTick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is PlayerMobile pm && pm.Deleted == false)
            {
                pm.Delete();
            }
        }

        public static TownshipStone GetPlayerTownship(Mobile player)
        {
            if (player != null && player is PlayerMobile pm && pm.Guild is Guild guild && guild.TownshipStone is TownshipStone ts)
                return ts;
            return null;
        }
        #endregion Lord British Subsidy 

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalFeePerDay
        {
            get { int discount = 0; return GetTotalFeePerDay(false, ref discount); }
        }
        public void RecordFameAndTaxBankInfo()
        {
            StringBuilder subsidiesListing = new StringBuilder();

            /* info on subsidies
             */
            //subsidiesListing.Append("<br><br>-- Subsidies --");

            subsidiesListing.Append("<br>LB Tax Subsidy Banked: ");
            subsidiesListing.Append(LBTaxSubsidy / 2 + " gold");

            subsidiesListing.Append("<br>LB Fame Bonus points banked: ");
            subsidiesListing.Append(LBFameSubsidy);
            subsidiesListing.Append("<br>Approximate Fame Bonus Days Remaining: ");
            subsidiesListing.Append(LBFameSubsidy / (LBFameDailyAwardGoal * LBFameVisitCost));
            subsidiesListing.Append("<br>Approximate Fame Bonus Weeks Remaining: ");
            subsidiesListing.Append(LBFameSubsidy / (LBFameWeeklyAwardGoal * LBFameVisitCost));

            m_FameAndTaxBankHTML = subsidiesListing.ToString();
        }
        public int GetTotalFeePerDay(bool recordFees, ref int discount)
        {
            //make sure special NPC requirements are met.
            CheckNPCRequirements();

            StringBuilder feeListing = new StringBuilder();

            double basefee_activitymodifier = 1.0;

            switch (this.m_LastActualActivityLevel)
            {
                case Township.ActivityLevel.NONE:
                    basefee_activitymodifier = Township.TownshipSettings.BaseModifierNone;
                    break;
                case Township.ActivityLevel.LOW:
                    basefee_activitymodifier = Township.TownshipSettings.BaseModifierLow;
                    break;
                case Township.ActivityLevel.MEDIUM:
                    basefee_activitymodifier = Township.TownshipSettings.BaseModifierMed;
                    break;
                case Township.ActivityLevel.HIGH:
                    basefee_activitymodifier = Township.TownshipSettings.BaseModifierHigh;
                    break;
                case Township.ActivityLevel.BOOMING:
                    basefee_activitymodifier = Township.TownshipSettings.BaseModifierBoom;
                    break;
            }

            int total = 0;

            //// heading
            //if (recordFees)
            //{
            //    feeListing.Append("<br><b>Daily Fees</b>");
            //}

            //base fee
            total += (int)(Township.TownshipSettings.BaseFee * basefee_activitymodifier);
            if (recordFees)
            {
                feeListing.Append("<br>Base Fee: ");
                feeListing.Append((int)(Township.TownshipSettings.BaseFee * basefee_activitymodifier));
            }

            //NPC fees
            total += CalculateNPCFee();
            if (recordFees)
            {
                feeListing.Append("<br>NPCs: ");
                feeListing.Append(CalculateNPCFee());
            }

            //travel fees
            if (this.NoGateOut)
            {
                total += Township.TownshipSettings.NoGateOutFee;
                if (recordFees)
                {
                    feeListing.Append("<br>No Gate Out: ");
                    feeListing.Append(Township.TownshipSettings.NoGateOutFee);
                }
            }
            if (this.NoGateInto)
            {
                total += Township.TownshipSettings.NoGateInFee;
                if (recordFees)
                {
                    feeListing.Append("<br>No Gate In: ");
                    feeListing.Append(Township.TownshipSettings.NoGateInFee);
                }
            }
            if (this.NoRecallOut)
            {
                total += Township.TownshipSettings.NoRecallOutFee;
                if (recordFees)
                {
                    feeListing.Append("<br>No Recall Out: ");
                    feeListing.Append(Township.TownshipSettings.NoRecallOutFee);
                }
            }
            if (this.NoRecallInto)
            {
                total += Township.TownshipSettings.NoRecallInFee;
                if (recordFees)
                {
                    feeListing.Append("<br>No Recall In: ");
                    feeListing.Append(Township.TownshipSettings.NoRecallInFee);
                }
            }

            //lawlevel fees
            if (LawLevel != Township.LawLevel.NONE)
            {
                total += CalculateLawLevelFee();
                if (recordFees)
                {
                    feeListing.Append("<br>Lawlevel: ");
                    feeListing.Append(CalculateLawLevelFee());
                }
            }

            //extended region fee
            if (this.Extended)
            {
                total += (int)(Township.TownshipSettings.ExtendedFee * basefee_activitymodifier);
                if (recordFees)
                {
                    feeListing.Append("<br>Extended Area: ");
                    feeListing.Append((int)(Township.TownshipSettings.ExtendedFee * basefee_activitymodifier));
                }
            }

            if (this.m_LastLBFameSubsidy != 0)
            {
                if (recordFees)
                {
                    feeListing.Append("<br>Fame Bonus: ");
                    feeListing.Append((int)this.m_LastLBFameSubsidy);
                    feeListing.Append(" (visits)");
                }
            }

            // subtotal before tax subsidies
            if (recordFees)
            {
                feeListing.Append("<br><br>Subtotal: ");
                feeListing.Append(total);
            }

            // tax subsidy. Must be last in Total calculations
            discount = this.CalcLBTaxSubsidy(subtotal: total);
            total -= discount;
            if (recordFees)
            {
                feeListing.Append("<br>Tax Subsidy: ");
                feeListing.Append((int)discount);
            }

            ///* info on subsidies
            // */
            //if (recordFees)
            //{
            //    feeListing.Append("<br><br>-- Subsidies --");

            //    feeListing.Append("<br>LB Tax Subsidy Banked: ");
            //    feeListing.Append(LBTaxSubsidy + " gold");

            //    feeListing.Append("<br>LB Fame Bonus points banked: ");
            //    feeListing.Append(LBFameSubsidy);
            //    feeListing.Append("<br>Approximate Fame Bonus Days Remaining: ");
            //    feeListing.Append(LBFameSubsidy / (LBFameDailyAwardGoal * LBFameVisitCost));
            //    feeListing.Append("<br>Approximate Fame Bonus Weeks Remaining: ");
            //    feeListing.Append(LBFameSubsidy / (LBFameWeeklyAwardGoal * LBFameVisitCost));
            //}


            /* Grand Total
             */
            if (recordFees)
            {
                feeListing.Append("<br><br>Total: ");
                feeListing.Append(total);

                m_DailyFeesHTML = feeListing.ToString();
            }

            return total;
        }

        public int CalculateLawLevelFee()
        {
            return CalculateLawLevelFee(m_LawLevel);
        }

        public int CalculateLawLevelFee(Township.LawLevel ll)
        {
            double cost = 0.0;

            switch (ll)
            {
                case Township.LawLevel.NONE:
                    break;
                case Township.LawLevel.LAWLESS:
                    cost += Township.TownshipSettings.LawlessFee;
                    break;
                case Township.LawLevel.AUTHORITY:
                    cost += Township.TownshipSettings.LawAuthFee;
                    break;
            }

            switch (m_LastActualActivityLevel)
            {
                case Township.ActivityLevel.NONE:
                    cost *= Township.TownshipSettings.LLModifierNone;
                    break;
                case Township.ActivityLevel.LOW:
                    cost *= Township.TownshipSettings.LLModifierLow;
                    break;
                case Township.ActivityLevel.MEDIUM:
                    cost *= Township.TownshipSettings.LLModifierMed;
                    break;
                case Township.ActivityLevel.HIGH:
                    cost *= Township.TownshipSettings.LLModifierHigh;
                    break;
                case Township.ActivityLevel.BOOMING:
                    cost *= Township.TownshipSettings.LLModifierBoom;
                    break;
            }

            return (int)cost;
        }

        public int CalculateNPCFee()
        {
            int totalFee = 0;

            foreach (Mobile m in TownshipNPCs)
                totalFee += ModifyNPCFee(TownshipNPCHelper.GetNPCFee(m.GetType()));

            return totalFee;
        }

        public int ModifyNPCFee(int fee)
        {
            double dfee = fee;

            switch (m_LastActualActivityLevel)
            {
                case Township.ActivityLevel.NONE:
                    dfee *= Township.TownshipSettings.NPCModifierNone;
                    break;
                case Township.ActivityLevel.LOW:
                    dfee *= Township.TownshipSettings.NPCModifierLow;
                    break;
                case Township.ActivityLevel.MEDIUM:
                    dfee *= Township.TownshipSettings.NPCModifierMed;
                    break;
                case Township.ActivityLevel.HIGH:
                    dfee *= Township.TownshipSettings.NPCModifierHigh;
                    break;
                case Township.ActivityLevel.BOOMING:
                    dfee *= Township.TownshipSettings.NPCModifierBoom;
                    break;
            }

            return (int)Math.Round(dfee);
        }

        #endregion

        #region Activity

        private DateTime m_ALLastCalculated;
        private Township.ActivityLevel m_ActivityLevel; //this is the 'size' that the town has grown to
        private Township.ActivityLevel m_LastActualActivityLevel; //this is the actual activity of the town last week (what we base NPC fees on)
        private int m_LastActualActivityWeekTotal = 0;
        private int m_WeeksAtThisLevel = 0;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime ALLastCalculated
        {
            get { return m_ALLastCalculated; }
            set { m_ALLastCalculated = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Township.ActivityLevel ActivityLevel
        {
            get
            {
                //Calculate Activity Level every week.
                if (IsActivityLevelCalcNeeded())
                {
                    CalculateActivityLevel();
                    return m_ActivityLevel;
                }
                else
                {
                    return this.m_ActivityLevel;
                }
            }
            set { m_ActivityLevel = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Township.ActivityLevel LastActivityLevel
        {
            get { return m_LastActualActivityLevel; }
            set { m_LastActualActivityLevel = value; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int LastActualWeekNumber
        {
            get { return m_LastActualActivityWeekTotal; }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int WeeksAtThisLevel
        {
            get { return m_WeeksAtThisLevel; }
            set { m_WeeksAtThisLevel = value; }
        }

        private bool IsActivityLevelCalcNeeded()
        {
            bool bReturn;

            if (Server.Misc.TestCenter.Enabled)
            {
                bReturn = (m_ALLastCalculated + TimeSpan.FromDays(0.5) < Now());
            }
            else
            {
                //Pix: because heartbeat jobs aren't exact, we want to add a little 'flex room' for them,
                // change from 7.0 days (10080 minutes) to 10070 minutes, giving a 10-minute time buffer
                bReturn = (m_ALLastCalculated + TimeSpan.FromMinutes(10070) < Now());
            }

            return bReturn;
        }
        #region Debugging Township Decay
        /*  normally, when offset_time is 0, normal time is returned. However,
         *  when offset_time is not 0, time is advanced by N weeks (offset_time)
         */
#if DEBUG
        [CommandProperty(AccessLevel.Administrator)]
        public bool ForceBumpNow
        {
            get { return false; }
            set
            {
                if (value)
                    offset_time++;      // advance into the next week
                else
                    offset_time = 0;    // restore normal time

                SendSystemMessage(string.Format($"Date is now {(DateTime.UtcNow + TimeSpan.FromDays(8 * offset_time)).ToString()}"));
            }
        }
#endif
        private static int offset_time = 0;
        private static DateTime Now()
        {
            return DateTime.UtcNow + TimeSpan.FromDays(8 * offset_time);
        }
        #endregion Debugging Township Decay
        [CommandProperty(AccessLevel.Administrator)]
        public bool ForceCalculateAL
        {
            get { return false; }
            set
            {
                if (value)
                    CalculateActivityLevel();
            }
        }

        private void CalculateActivityLevel()
        {
            int visitorsThisWeek = m_VisitorCounts.VisitorsThisWeek;

            /* Adam: Important design note, heh
             * private Township.ActivityLevel m_ActivityLevel; //this is the 'size' that the town has grown to
             * private Township.ActivityLevel m_LastActualActivityLevel; //this is the actual activity of the town last week (what we base NPC fees on)
             * So... once you reach BOOMING for instance, it never goes down and you will always be able to purchase all the NPCs. 
             * However m_LastActualActivityLevel dictates the cost of those NPCs
             */
            switch (m_ActivityLevel)
            {
                case Township.ActivityLevel.NONE:
                    if (visitorsThisWeek > Township.TownshipSettings.NoneToLow)
                    {
                        //Should bump up to low after one week, so bump it.
                        m_ActivityLevel = Township.ActivityLevel.LOW;
                    }
                    break;
                case Township.ActivityLevel.LOW:
                    if (visitorsThisWeek > Township.TownshipSettings.LowToMedium)
                    {
                        if (m_WeeksAtThisLevel >= 1)
                        {
                            m_ActivityLevel = Township.ActivityLevel.MEDIUM;
                            m_WeeksAtThisLevel = 0;
                        }
                        else
                        {
                            m_WeeksAtThisLevel++;
                        }
                    }
                    else
                    {
                        // didn't meet the activity requirements this week, set counter to 0
                        m_WeeksAtThisLevel = 0;
                    }
                    break;
                case Township.ActivityLevel.MEDIUM:
                    if (visitorsThisWeek > Township.TownshipSettings.MediumToHigh)
                    {
                        if (m_WeeksAtThisLevel >= 2)
                        {
                            m_ActivityLevel = Township.ActivityLevel.HIGH;
                            m_WeeksAtThisLevel = 0;
                        }
                        else
                        {
                            m_WeeksAtThisLevel++;
                        }
                    }
                    else
                    {
                        // didn't meet the activity requirements this week, set counter to 0
                        m_WeeksAtThisLevel = 0;
                    }
                    break;
                case Township.ActivityLevel.HIGH:
                    if (visitorsThisWeek > Township.TownshipSettings.HighToBooming)
                    {
                        if (m_WeeksAtThisLevel >= 3)
                        {
                            m_ActivityLevel = Township.ActivityLevel.BOOMING;
                            m_WeeksAtThisLevel = 0;
                        }
                        else
                        {
                            m_WeeksAtThisLevel++;
                        }
                    }
                    else
                    {
                        // didn't meet the activity requirements this week, set counter to 0
                        m_WeeksAtThisLevel = 0;
                    }
                    break;
                case Township.ActivityLevel.BOOMING:
                    //already biggest, do nothing
                    break;
            }

            if (visitorsThisWeek > Township.TownshipSettings.HighToBooming)
                m_LastActualActivityLevel = Township.ActivityLevel.BOOMING;
            else if (visitorsThisWeek > Township.TownshipSettings.MediumToHigh)
                m_LastActualActivityLevel = Township.ActivityLevel.HIGH;
            else if (visitorsThisWeek > Township.TownshipSettings.LowToMedium)
                m_LastActualActivityLevel = Township.ActivityLevel.MEDIUM;
            else if (visitorsThisWeek > Township.TownshipSettings.NoneToLow)
                m_LastActualActivityLevel = Township.ActivityLevel.LOW;
            else
                m_LastActualActivityLevel = Township.ActivityLevel.NONE;

            m_LastActualActivityWeekTotal = visitorsThisWeek;
            m_ALLastCalculated = Now();
        }

        #endregion

        #region Township NPCs

        private bool m_OutsideNPCInteractionAllowed;
        private List<Mobile> m_TownshipNPCs;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool OutsideNPCInteractionAllowed
        {
            get { return m_OutsideNPCInteractionAllowed; }
            set { m_OutsideNPCInteractionAllowed = value; }
        }

        public List<Mobile> TownshipNPCs
        {
            get { DefragTownshipNPCs(); return m_TownshipNPCs; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TownshipNPCCount
        {
            get { return TownshipNPCs.Count; }
        }

        public bool HasNPC(Type type)
        {
            foreach (Mobile m in TownshipNPCs)
            {
                if (m != null && type.IsAssignableFrom(m.GetType()))
                    return true;
            }

            return false;
        }

        public int CountNPCs(Type type)
        {
            int count = 0;

            foreach (Mobile m in TownshipNPCs)
            {
                if (m != null && type.IsAssignableFrom(m.GetType()))
                    count++;
            }

            return count;
        }

        public void RemoveNPCs(Type type)
        {
            foreach (Mobile m in TownshipNPCs)
            {
                if (m != null && type.IsAssignableFrom(m.GetType()))
                    m.Delete();
            }
        }

        public void CheckNPCRequirements(Type toCheck = null)
        {
            if ((toCheck == null || toCheck == typeof(TSEmissary)) && !HasNPC(typeof(TSEmissary)))
            {
                if (m_LawLevel == Township.LawLevel.AUTHORITY)
                    m_LawLevel = Township.LawLevel.NONE;
            }

            if ((toCheck == null || toCheck == typeof(TSEvocator)) && !HasNPC(typeof(TSEvocator)))
            {
                this.NoRecallInto = false;
                this.NoRecallOut = false;
                this.NoGateInto = false;
                this.NoGateOut = false;
            }

            if ((toCheck == null || toCheck == typeof(TSTownCrier)) && !HasNPC(typeof(TSTownCrier)))
            {
                if (m_Extended)
                    ReduceRegion();
            }

            if ((toCheck == null || toCheck == typeof(TSNecromancer)) && !HasNPC(typeof(TSNecromancer)))
            {
                if (CustomRegion != null && CustomRegion.Season == SeasonType.Desolation)
                    CustomRegion.Season = SeasonType.Default;
            }
        }

        public void DefragTownshipNPCs()
        {
            for (int i = m_TownshipNPCs.Count - 1; i >= 0; i--)
            {
                if (m_TownshipNPCs[i].Deleted)
                    m_TownshipNPCs.RemoveAt(i);
            }
        }

        #endregion

        #region Travel Rules

        private DateTime m_LastTravelChange = DateTime.MinValue;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool NoRecallInto
        {
            get { return CustomRegion.NoRecallInto; }
            set { CustomRegion.NoRecallInto = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool NoGateInto
        {
            get { return CustomRegion.NoGateInto; }
            set { CustomRegion.NoGateInto = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool NoRecallOut
        {
            get { return CustomRegion.IsRestrictedSpell(31); }
            set { CustomRegion.SetRestrictedSpell(31, value); }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool NoGateOut
        {
            get { return CustomRegion.IsRestrictedSpell(51); }
            set { CustomRegion.SetRestrictedSpell(51, value); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime LastTravelChange
        {
            get { return m_LastTravelChange; }
            set { m_LastTravelChange = value; }
        }

        #endregion

        #region Township Items

        private Township.TownshipItemRegistry m_ItemRegistry;

        [CommandProperty(AccessLevel.GameMaster)]
        public Township.TownshipItemRegistry ItemRegistry
        {
            get { return m_ItemRegistry; }
            set { }
        }

        private Item m_WelcomeMat; // cached ref. to the welcome mat

        [CommandProperty(AccessLevel.GameMaster)]
        public Item WelcomeMat
        {
            get
            {
                if (m_WelcomeMat != null && !ValidateWelcomeMat(m_WelcomeMat))
                    m_WelcomeMat = null;

                if (m_WelcomeMat == null)
                {
                    foreach (Item item in m_ItemRegistry.Table.Keys)
                    {
                        if (item is Township.WelcomeMat && ValidateWelcomeMat(item))
                        {
                            m_WelcomeMat = item;
                            break;
                        }
                    }
                }

                return m_WelcomeMat;
            }
            set
            {
                m_WelcomeMat = value;
            }
        }

        private bool ValidateWelcomeMat(Item item)
        {
            return (!item.Deleted && Contains(item));
        }

        public void SetItemOwner(Item item, Mobile owner)
        {
            SetItemOwner(item, owner, Now());
        }

        public void SetItemOwner(Item item, Mobile owner, DateTime placed)
        {
            Township.TownshipItemContext c = m_ItemRegistry.Lookup(item, true);

            if (c != null)
            {
                c.Owner = owner;
                c.Placed = placed;
            }
        }

        public Mobile GetItemOwner(Item item)
        {
            Township.TownshipItemContext c = m_ItemRegistry.Lookup(item);

            if (c == null && item is AddonComponent)
            {
                AddonComponent ac = (AddonComponent)item;

                if (ac.Addon != null)
                    c = m_ItemRegistry.Lookup(ac.Addon);
            }

            if (c != null)
                return c.Owner;

            return null;
        }

        public bool IsItemOwner(Mobile m, Item item)
        {
            return (GetItemOwner(item) == m || HasAccess(m, Township.TownshipAccess.Leader));
        }

        #endregion

        #region Visitors
        private DateTime m_LastVisit = DateTime.MinValue;
        private DayOfWeek m_CurrentDay = (DayOfWeek)(-1);
        private ArrayList m_TodaysVisitors = new ArrayList();
        private VisitorCounter m_VisitorCounts = new VisitorCounter();

        [CommandProperty(AccessLevel.Counselor)]
        public VisitorCounter VisitorCounts
        {
            get { return m_VisitorCounts; }
            set { }
        }

        [NoSort]
        [PropertyObject]
        public class VisitorCounter
        {
            private int[] m_Array;

            public int this[DayOfWeek day]
            {
                get { return GetVisitors(day); }
                set { SetVisitors(day, value); }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
            public int VisitorsToday
            {
                get { return GetVisitors(Now().DayOfWeek); }
                set { SetVisitors(Now().DayOfWeek, value); }
            }

            [CommandProperty(AccessLevel.Counselor)]
            public int VisitorsThisWeek
            {
                get
                {
                    int count = 0;

                    for (int i = 0; i < m_Array.Length; i++)
                        count += m_Array[i];

                    return count;
                }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
            public int VisitorsSunday
            {
                get { return GetVisitors(DayOfWeek.Sunday); }
                set { SetVisitors(DayOfWeek.Sunday, value); }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
            public int VisitorsMonday
            {
                get { return GetVisitors(DayOfWeek.Monday); }
                set { SetVisitors(DayOfWeek.Monday, value); }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
            public int VisitorsTuesday
            {
                get { return GetVisitors(DayOfWeek.Tuesday); }
                set { SetVisitors(DayOfWeek.Tuesday, value); }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
            public int VisitorsWednesday
            {
                get { return GetVisitors(DayOfWeek.Wednesday); }
                set { SetVisitors(DayOfWeek.Wednesday, value); }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
            public int VisitorsThursday
            {
                get { return GetVisitors(DayOfWeek.Thursday); }
                set { SetVisitors(DayOfWeek.Thursday, value); }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
            public int VisitorsFriday
            {
                get { return GetVisitors(DayOfWeek.Friday); }
                set { SetVisitors(DayOfWeek.Friday, value); }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
            public int VisitorsSaturday
            {
                get { return GetVisitors(DayOfWeek.Saturday); }
                set { SetVisitors(DayOfWeek.Saturday, value); }
            }

            public VisitorCounter()
            {
                m_Array = new int[7];
            }

            public void Serialize(GenericWriter writer)
            {
                writer.Write((int)this.VisitorsSunday);
                writer.Write((int)this.VisitorsMonday);
                writer.Write((int)this.VisitorsTuesday);
                writer.Write((int)this.VisitorsWednesday);
                writer.Write((int)this.VisitorsThursday);
                writer.Write((int)this.VisitorsFriday);
                writer.Write((int)this.VisitorsSaturday);
            }

            public void Deserialize(GenericReader reader)
            {
                this.VisitorsSunday = reader.ReadInt();
                this.VisitorsMonday = reader.ReadInt();
                this.VisitorsTuesday = reader.ReadInt();
                this.VisitorsWednesday = reader.ReadInt();
                this.VisitorsThursday = reader.ReadInt();
                this.VisitorsFriday = reader.ReadInt();
                this.VisitorsSaturday = reader.ReadInt();
            }

            private int GetVisitors(DayOfWeek day)
            {
                int index = (int)day;

                if (index >= 0 && index < m_Array.Length)
                    return m_Array[index];

                return 0;
            }

            private void SetVisitors(DayOfWeek day, int value)
            {
                int index = (int)day;

                if (index >= 0 && index < m_Array.Length)
                    m_Array[index] = value;
            }

            public override string ToString()
            {
                return "...";
            }
        }

        #endregion

        #region Law

        private Township.LawLevel m_LawLevel;
        private DateTime m_LastLawChange = DateTime.MinValue;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Township.LawLevel LawLevel
        {
            get { return m_LawLevel; }
            set { m_LawLevel = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime LastLawChange
        {
            get { return m_LastLawChange; }
            set { m_LastLawChange = value; }
        }

        public bool MurderZone
        {
            get { return true; }
        }

        public bool IsMobileCountable(Mobile attacker)
        {
            bool bReturn;

            switch (LawLevel)
            {
                case Township.LawLevel.LAWLESS:
                    bReturn = false;
                    break;
                case Township.LawLevel.AUTHORITY:
                    if (IsMemberOrAlly(attacker))
                        bReturn = false;
                    else
                        bReturn = true;
                    break;
                default:
                case Township.LawLevel.NONE:
                    bReturn = true;
                    break;
            }

            return bReturn;
        }

        public static string FormatLawLevel(Township.LawLevel lawLevel)
        {
            switch (lawLevel)
            {
                case Township.LawLevel.AUTHORITY:
                    return "Grant of Authority";
                case Township.LawLevel.LAWLESS:
                    return "Lawless";
                default:
                    return "Standard";
            }
        }

        #endregion

        #region On[Enter/Exit]

        private bool m_ShowTownshipMessages = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowTownshipMessages
        {
            get { return m_ShowTownshipMessages; }
            set { m_ShowTownshipMessages = value; }
        }
        public bool DecayVisits()
        {
            // reset daily stats
            DayOfWeek day = Now().DayOfWeek;
            int weekOfYear = System.Globalization.ISOWeek.GetWeekOfYear(Now());
            if (m_LastVisit == DateTime.MinValue || m_CurrentWeekOfYear != weekOfYear)
            {
                m_CurrentDay = day;
                m_TodaysVisitors.Clear();
                m_VisitorCounts[day] = 0;
                m_CurrentWeekOfYear = weekOfYear;
                return true;
            }
            return false;
        }
        public override void OnEnter(Mobile m)
        {
            bool bShowEnterMessage = m_ShowTownshipMessages;

            PlayerMobile pm = m as PlayerMobile;

            if (pm != null && pm.LastRegionIn is HouseRegion)
                bShowEnterMessage = false;

            if (m.Player && m.AccessLevel == AccessLevel.Player) //only count players, not mobs or staff
            {
                DayOfWeek day = Now().DayOfWeek;

                // reset daily stats if need be
                DecayVisits();

                // set last visit time
                m_LastVisit = Now();

                // register visitor
                if (!m_TodaysVisitors.Contains(m))
                {
                    m_TodaysVisitors.Add(m);
                    m_VisitorCounts[day]++;
                }
            }

            if (bShowEnterMessage)
            {
                string sizeDesc = GetTownshipSizeDesc(ActivityLevel).ToLower();

                StringBuilder enterMessage = new StringBuilder();
                enterMessage.Append("You have entered the ");
                enterMessage.Append(sizeDesc);
                enterMessage.Append(" of ");
                enterMessage.Append(GuildName);
                enterMessage.Append(".  ");

                switch (LawLevel)
                {
                    case Township.LawLevel.AUTHORITY:
                        enterMessage.Append("The ");
                        enterMessage.Append(GuildName);
                        enterMessage.Append(" has received a Grant of Authority by Lord British to enforce ");
                        enterMessage.Append("the law within this ");
                        enterMessage.Append(sizeDesc);
                        enterMessage.Append(".");
                        enterMessage.Append("  The following guilds are authorized to protect this town: ");
                        try
                        {
                            enterMessage.Append(this.GuildAbbreviation);
                            foreach (object o in m_Guild.Allies)
                            {
                                if (o is Guild)
                                    enterMessage.Append(", " + ((Guild)o).Abbreviation);
                            }
                        }
                        catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                        break;
                    case Township.LawLevel.LAWLESS:
                        enterMessage.Append("Beware!  There are no laws being enforced within this ");
                        enterMessage.Append(sizeDesc);
                        enterMessage.Append("!");
                        break;
                    case Township.LawLevel.NONE:
                    default:
                        //enterMessage.Append("Lord British enforces the laws within this ");
                        enterMessage.Append("Lord British will make note of any murders that are reported within this ");
                        enterMessage.Append(sizeDesc);
                        enterMessage.Append(".");
                        break;
                }

                m.SendMessage(enterMessage.ToString());
            }
        }

        public override void OnExit(Mobile m)
        {
            if (m_ShowTownshipMessages)
            {
                if (m is PlayerMobile)
                {
                    PlayerMobile pm = (PlayerMobile)m;

                    if (pm != null && pm.Region is HouseRegion) //Nested regions are shitty, don't display exit message when entering a house
                        return;

                    m.SendMessage("You have left the {0} of {1}.", GetTownshipSizeDesc(ActivityLevel).ToLower(), GuildName);
                }
            }
        }

        #endregion

        public List<BaseHouse> TownshipHouses
        {
            get
            {
                List<BaseHouse> list = new List<BaseHouse>();

                Rectangle3D rect = GetBounds();

                for (int x = rect.Start.X; x < rect.End.X; x++)
                {
                    for (int y = rect.Start.Y; y < rect.End.Y; y++)
                    {
                        HouseRegion r = Server.Region.Find(new Point3D(x, y, this.Z), this.Map) as HouseRegion;

                        if (r != null && r.House != null && !list.Contains(r.House))
                            list.Add(r.House);
                    }
                }

                return list;
            }
        }

        public static string GetTownshipSizeDesc(Township.ActivityLevel al)
        {
            switch (al)
            {
                case Township.ActivityLevel.NONE:
                    return "Locality";
                case Township.ActivityLevel.LOW:
                    return "Hamlet";
                case Township.ActivityLevel.MEDIUM:
                    return "Village";
                case Township.ActivityLevel.HIGH:
                    return "Township";
                case Township.ActivityLevel.BOOMING:
                    return "City";
            }

            return "ERROR";
        }

        public static string GetTownshipActivityDesc(Township.ActivityLevel al)
        {
            switch (al)
            {
                case Township.ActivityLevel.NONE:
                    return "Stagnant";
                case Township.ActivityLevel.LOW:
                    return "Declining";
                case Township.ActivityLevel.MEDIUM:
                    return "Stable";
                case Township.ActivityLevel.HIGH:
                    return "Growing";
                case Township.ActivityLevel.BOOMING:
                    return "Booming";
            }

            return "ERROR";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!CheckView(from))
                return;

            // 1/8/24, Yoar: Kind of a weird case, but open the stockpile gump if we have a building permit
            if (!HasAccess(from, Township.TownshipAccess.Ally) && AllowBuilding(from))
            {
                from.CloseGump(typeof(TownshipGump));
                from.SendGump(new TownshipGump(this, from, TownshipGump.Page.Stockpile));
                return;
            }

            if (CheckAccess(from, Township.TownshipAccess.Ally))
            {
                from.CloseGump(typeof(TownshipGump));
                from.SendGump(new TownshipGump(this, from));
            }
        }

        public bool CheckView(Mobile from, bool message = true, bool range_check = true)
        {
            if (CustomRegion != null && (CustomRegion.Map != from.Map || !CustomRegion.Contains(from.Location)))
            {
                if (message)
                    from.SendMessage("You must be within the township to view the township menu.");

                return false;
            }

            if (range_check && !from.InRange(GetWorldLocation(), Township.TownshipSettings.TStoneUseRange))
            {
                if (message)
                    from.SendMessage("You are too far away from the township stone to view the township menu.");

                return false;
            }

            return true;
        }

        public override void Delete()
        {
            try
            {
                if (m_Guild != null)
                    m_Guild.TownshipStone = null;

                for (int i = m_TownshipNPCs.Count - 1; i >= 0; i--)
                    m_TownshipNPCs[i].Delete();

                ReleaseAllLivestock();

                m_Instances.Remove(this);

                // free stuff!
                UnlockLockDowns();

                // free the scaffolding
                for (int i = m_Scaffolds.Count - 1; i >= 0; i--)
                    m_Scaffolds[i].Delete();

                // safety: while looking for things to delete,
                // look for any change in regions, or new overlapping regions.
                List<Region> initialRegions = GetRegionList(this.Location, this.Map);

                // cleanup the township items
                List<Item> list = new List<Item>(ItemRegistry.Table.Keys);
                foreach (Item tsi in list)
                {
                    if (DeleteSafetyCheck(tsi, initialRegions))
                        tsi.Delete();
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            base.Delete();
        }

        public bool DeleteSafetyCheck(Item item, List<Region> initialRegions)
        {
            if (item != null && !item.Deleted && Contains(item))
            {   // if we find an item not covered by our initialRegions, it will be ignored
                List<Region> regions = GetRegionList(item.Location, item.Map);
                // has the list or regions changed?
                if (RegionListsSame(initialRegions, regions))
                    // is it in a house?
                    if (BaseHouse.FindHouseAt(item) == null)
                        return true;
            }
            // not a safe delete
            return false;
        }

        public List<Region> GetRegionList(Point3D px, Map map)
        {
            ArrayList list = Region.FindAll(px, map);
            List<Region> tmp = list.Cast<Region>().ToList();
            // remove house regions as they are handled elsewhere
            tmp.RemoveAll(i => i.IsHouseRules);
            return tmp;
        }

        private static bool RegionListsSame(List<Region> list1, List<Region> list2)
        {   // TODO
            // var a = ints1.All(ints2.Contains) && ints1.Count == ints2.Count;
            var firstNotSecond = list1.Except(list2).ToList();
            var secondNotFirst = list2.Except(list1).ToList();
            return !firstNotSecond.Any() && !secondNotFirst.Any();
        }

        #region Lockdowns

        public record LockDownContext
        {
            private Mobile m_Mobile;

            public Mobile Mobile { get { return m_Mobile; } }

            public LockDownContext(Mobile m)
            {
                m_Mobile = m;
            }

            public void Serialize(GenericWriter writer)
            {
                writer.WriteEncodedInt(0);

                writer.Write((Mobile)m_Mobile);
            }

            public LockDownContext(GenericReader reader)
            {
                int version = reader.ReadEncodedInt();

                m_Mobile = reader.ReadMobile();
            }
        }

        protected Dictionary<Item, LockDownContext> m_LockdownRegistry = new Dictionary<Item, LockDownContext>();

        public Dictionary<Item, LockDownContext> LockdownRegistry { get { return m_LockdownRegistry; } }

        protected void UnlockLockDowns()
        {
            List<Item> released = new List<Item>();

            foreach (Item item in m_LockdownRegistry.Keys)
            {
                if (!item.Deleted)
                {
                    item.Movable = true;
                    item.IsLockedDown = false;
                    item.IsTSItemFreelyAccessible = false;
                    item.SetLastMoved();
                    released.Add(item);
                }
            }

            m_LockdownRegistry.Clear();

            foreach (Item item in released)
                item.OnRelease();
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LockDowns
        {
            get { return m_LockdownRegistry.Count; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockDowns
        {
            get
            {
                int count = 0;

                try
                {
                    foreach (BaseHouse house in TownshipDeed.GetHousesInRadius(m_TownshipCenter, Map, m_Extended ? TownshipStone.EXTENDED_RADIUS : TownshipStone.INITIAL_RADIUS))
                    {
                        if (house is SiegeTent || house.Owner == null)
                            continue; // ignore completely

                        Guild houseGuild = house.Owner.Guild as Guild;

                        if (houseGuild != null && (m_Guild == houseGuild || m_Guild.IsAlly(houseGuild)))
                            count += house.MaxLockDowns;
                    }
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }

                return count;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LockBoxes
        {
            get
            {
                int count = 0;

                foreach (Item item in m_LockdownRegistry.Keys)
                {
                    if (!item.Deleted && item is Container && !((Container)item).Deco)
                        count++;
                }

                return count;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockBoxes
        {
            get
            {
                int count = 0;

                try
                {
                    foreach (BaseHouse house in TownshipDeed.GetHousesInRadius(m_TownshipCenter, Map, m_Extended ? TownshipStone.EXTENDED_RADIUS : TownshipStone.INITIAL_RADIUS))
                    {
                        if (house is SiegeTent || house.Owner == null)
                            continue; // ignore completely

                        Guild houseGuild = house.Owner.Guild as Guild;

                        if (houseGuild != null && (m_Guild == houseGuild || m_Guild.IsAlly(houseGuild)))
                            count += house.MaxLockboxes;
                    }
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }

                return count;
            }
        }

        public new bool IsLockedDown(Item item)
        {
            return (m_LockdownRegistry.ContainsKey(item));
        }

        public Mobile GetLockdownOwner(Item item)
        {
            LockDownContext context;

            if (m_LockdownRegistry.TryGetValue(item, out context))
                return context.Mobile;

            return null;
        }

        public bool IsLockdownOwner(Mobile m, Item item)
        {
            return (GetLockdownOwner(item) == m || HasAccess(m, Township.TownshipAccess.Leader));
        }

        public void BeginLockdown(Mobile from)
        {
            if (CheckAccess(from, Township.TownshipAccess.Member))
            {
                from.SendLocalizedMessage(502097); // Lock what down?
                from.Target = new LockdownTarget(this);
            }
        }

        private class LockdownTarget : Target
        {
            private TownshipStone m_Stone;

            public LockdownTarget(TownshipStone stone)
                : base(12, false, TargetFlags.None)
            {
                m_Stone = stone;
            }

            protected override void OnTargetNotAccessible(Mobile from, object targeted)
            {
                OnTarget(from, targeted);
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.CheckAlive() || !m_Stone.CheckAccess(from, Township.TownshipAccess.Member))
                    return;

                if (targeted is Item)
                {
                    Item item = (Item)targeted;

                    if (item.Parent != null || BaseHouse.FindHouseAt(item) != null || BaseBoat.FindBoatAt(item) != null || TownshipRegion.GetTownshipAt(item) != m_Stone.CustomRegion)
                    {
                        from.SendLocalizedMessage(1005377); // You cannot lock that down
                    }
                    else if (!m_Stone.IsLockedDown(item) && !item.Movable)
                    {
                        from.SendMessage("That item does not belong to the township.");
                    }
                    else if (Utility.FindOneItemAt(item.Location, item.Map, typeof(Teleporter), 2, false) != null)
                    {
                        from.SendMessage("Something is preventing you from locking this down here.");
                    }
                    else if (m_Stone.m_LockdownRegistry.Count >= m_Stone.MaxLockDowns)
                    {
                        from.SendMessage("That would exceed the maximum lock down limit for this township.");
                    }
                    else if (item is Container && !((Container)item).Deco && m_Stone.LockBoxes >= m_Stone.MaxLockBoxes)
                    {
                        from.SendMessage("That would exceed the maximum lock box limit for this township");
                    }
                    else if (m_Stone.IsLockedDown(item))
                    {
                        from.SendLocalizedMessage(1005526); // That is already locked down
                    }
                    else if (item is BaseWaterContainer bwc && bwc.Quantity != 0)
                    {
                        item.SendMessageTo(from, false, "That must be empty before you can lock it down.");
                    }
                    else
                    {
                        m_Stone.LockDown(item, from);

                        item.SendMessageTo(from, false, 0x3B2, "(locked down)");
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1005377); // You cannot lock that down
                }
            }
        }

        public void LockDown(Item item, Mobile m)
        {
            item.Movable = false;
            item.IsLockedDown = true;

            m_LockdownRegistry[item] = new LockDownContext(m);

            item.OnLockdown();
        }

        public void BeginRelease(Mobile from)
        {
            if (CheckAccess(from, Township.TownshipAccess.Member))
            {
                from.SendLocalizedMessage(502100); // Choose the item you wish to release
                from.Target = new ReleaseTarget(this);
            }
        }

        private class ReleaseTarget : Target
        {
            private TownshipStone m_Stone;

            public ReleaseTarget(TownshipStone stone)
                : base(12, false, TargetFlags.None)
            {
                m_Stone = stone;
            }

            protected override void OnTargetNotAccessible(Mobile from, object targeted)
            {
                OnTarget(from, targeted);
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.CheckAlive() || !m_Stone.CheckAccess(from, Township.TownshipAccess.Member))
                    return;

                if (targeted is Item)
                {
                    Item item = (Item)targeted;

                    if (item.Parent != null)
                    {
                        from.SendMessage("You cannot release that");
                    }
                    else if (!m_Stone.IsLockedDown(item))
                    {
                        from.SendLocalizedMessage(501722); // That isn't locked down...
                    }
                    else if (!m_Stone.IsLockdownOwner(from, item))
                    {
                        from.SendLocalizedMessage(1010418); // You did not lock this down, and you are not able to release this.
                    }
                    else if (item is BaseWaterContainer bwc && bwc.Quantity != 0)
                    {
                        item.SendMessageTo(from, false, "That must be empty before you can release it.");
                    }
                    else
                    {
                        m_Stone.Release(item);

                        item.SendLocalizedMessageTo(from, 501657); // (no longer locked down)
                    }
                }
                else
                {
                    from.SendMessage("You cannot release that");
                }
            }
        }

        public void Release(Item item)
        {
            item.Movable = true;
            item.IsLockedDown = false;
            item.IsTSItemFreelyAccessible = false;
            item.SetLastMoved();

            m_LockdownRegistry.Remove(item);

            item.OnRelease();
        }

        public static bool IsTownshipLockdown(Item item)
        {
            TownshipRegion tsr = TownshipRegion.GetTownshipAt(item);

            return (tsr != null && tsr.TStone != null && tsr.TStone.IsLockedDown(item));
        }

        #endregion

        #region OracleDestination

        Point3D IOracleDestination.DestLoc
        {
            get
            {
                Item welcomeMat = WelcomeMat;

                return (welcomeMat != null ? welcomeMat.Location : Point3D.Zero);
            }
        }

        Map IOracleDestination.DestMap
        {
            get
            {
                Item welcomeMat = WelcomeMat;

                return (welcomeMat != null ? welcomeMat.Map : null);
            }
        }

        OracleFlag IOracleDestination.Flag
        {
            get { return OracleFlag.Township; }
        }

        bool IOracleDestination.Validate(MoonGateWizard oracle, Mobile from)
        {
            Item welcomeMat = WelcomeMat;

            return (m_Guild != null && welcomeMat != null && !welcomeMat.Deleted && welcomeMat.Parent == null && welcomeMat.Map != null && welcomeMat.Map != Map.Internal);
        }

        bool IOracleDestination.WasNamed(SpeechEventArgs e)
        {
            return ((!string.IsNullOrEmpty(GuildName) && Insensitive.Equals(e.Speech, GuildName)) || (!string.IsNullOrEmpty(GuildAbbreviation) && Insensitive.Equals(e.Speech, GuildAbbreviation)));
        }

        string IOracleDestination.Format()
        {
            return GuildName;
        }

        #endregion

        public bool CheckAccessibility(Item item, Mobile from)
        {
            if (HasAccess(from, Township.TownshipAccess.Ally))
                return true;

            if (item.IsTSItemFreelyAccessible)
                return true;

            // the following list is mostly from BaseHouse.CheckAccessibility
            if (item is Forge)
                return true;
            else if (item is Runebook)
                return true;
            else if (item is SeedBox)
                return true;
            else if (item is Library)
                return true;
            else if (item is Container)
                return true;
            else if (item is BaseGameBoard)
                return true;
            else if (item is Dices)
                return true;
            else if (item is RecallRune)
                return true;
            else if (item is TreasureMap)
                return true;
            else if (item is Clock)
                return true;
            else if (item is BaseBook)
                return true;
            else if (item is BaseInstrument)
                return true;
            else if (item is Dyes || item is DyeTub)
                return true;
            else if (item is EternalEmbers)
                return true;
            else if (item is BaseDoor)
                return true;
            else if (item is Township.TownshipStatic)
                return true;
            else if (item is Township.TownshipLight)
                return true;
            else if (item is Township.TownshipAddonComponent)
                return true;

            return false;
        }

        private List<Mobile> m_BuildingPermits = new List<Mobile>();

        public List<Mobile> BuildingPermits { get { return m_BuildingPermits; } }

        public bool AllowBuilding(Mobile m)
        {
            return (HasAccess(m, Township.TownshipAccess.Member) || (!IsEnemy(m) && m_BuildingPermits.Contains(m)));
        }

        #region Livestock

        private Dictionary<BaseCreature, Mobile> m_Livestock = new Dictionary<BaseCreature, Mobile>();

        public Dictionary<BaseCreature, Mobile> Livestock
        {
            get
            {
                DefragLivestock();

                return m_Livestock;
            }
        }

        public int MaxLivestock
        {
            get { return 6 * CountNPCs(typeof(TSRancher)); }
        }

        private void DefragLivestock()
        {
            List<BaseCreature> toRemove = null;

            foreach (BaseCreature bc in m_Livestock.Keys)
            {
                if (bc.Deleted)
                {
                    if (toRemove == null)
                        toRemove = new List<BaseCreature>();

                    toRemove.Add(bc);
                }
            }

            if (toRemove != null)
            {
                foreach (BaseCreature bc in toRemove)
                    m_Livestock.Remove(bc);
            }
        }

        public void MakeLivestock(BaseCreature bc, Mobile owner)
        {
            if (m_Livestock.ContainsKey(bc))
                return;

            // clear control master
            bc.SetControlMaster(null);

            // clear herding target
            bc.Herder = null;
            bc.TargetLocation = Point2D.Zero;

            bc.SetCreatureBool(CreatureBoolTable.IsTownshipLivestock, true);
            bc.Home = bc.Location;
            bc.RangeHome = 3;
            bc.Guild = m_Guild;
            bc.DisplayGuildTitle = true;

            // township NPCs are invulnerable
            // let's make livestock invulnerable as well
            bc.IsInvulnerable = true;

            m_Livestock[bc] = owner;
        }

        public bool ReleaseLivestock(BaseCreature bc)
        {
            if (!m_Livestock.ContainsKey(bc))
                return false;

            bc.SetCreatureBool(CreatureBoolTable.IsTownshipLivestock, false);
            bc.Home = Point3D.Zero;
            bc.RangeHome = 10;
            bc.Guild = null;
            bc.DisplayGuildTitle = false;

            bc.IsInvulnerable = false;

            m_Livestock.Remove(bc);

            return true;
        }

        public void ReleaseAllLivestock()
        {
            List<BaseCreature> toRelease = new List<BaseCreature>(m_Livestock.Keys);

            foreach (BaseCreature bc in toRelease)
                ReleaseLivestock(bc);

            m_Livestock.Clear(); // sanity
        }

        public bool IsLivestockOwner(BaseCreature bc, Mobile m)
        {
            Mobile owner;

            return (m_Livestock.TryGetValue(bc, out owner) && m == owner);
        }

        #endregion

        #region Messages

        public static int MessageCap = 16;
        public static TimeSpan MessageExpire = TimeSpan.FromHours(12.0);

        private List<MessageEntry> m_Messages;

        public List<MessageEntry> Messages { get { return m_Messages; } }

        public void SendMessage(string text, bool allies = true)
        {
            GuildMessage(text);

            if (allies)
                AllyMessage(string.Format("[{0} of {1}]: {2}", GetTownshipSizeDesc(ActivityLevel), GuildName, text));

            AddMessage(text);
        }

        public void AddMessage(string text)
        {
            DefragMessages();

            while (m_Messages.Count >= MessageCap)
                m_Messages.RemoveAt(m_Messages.Count - 1);

            m_Messages.Insert(0, new MessageEntry(text));
        }

        private void DefragMessages()
        {
            for (int i = m_Messages.Count - 1; i >= 0; i--)
            {
                if (m_Messages[i].Expired)
                    m_Messages.RemoveAt(i);
            }
        }

        public void GuildMessage(string text)
        {
            if (m_Guild == null)
                return;

            m_Guild.GuildMessage(text);
        }

        public void AllyMessage(string text)
        {
            if (m_Guild == null)
                return;

            foreach (Guild ally in m_Guild.Allies)
                ally.GuildMessage(text);
        }

        public class MessageEntry
        {
            private string m_Text;
            private DateTime m_Date;

            public string Text { get { return m_Text; } }
            public DateTime Date { get { return m_Date; } }

            public bool Expired
            {
                get { return (Now() >= m_Date + MessageExpire); }
            }

            public MessageEntry(string text)
            {
                m_Text = text;
                m_Date = Now();
            }
        }

        #endregion

        private List<Township.Scaffold> m_Scaffolds = new List<Township.Scaffold>(); // not serialized

        public List<Township.Scaffold> Scaffolds
        {
            get { return m_Scaffolds; }
        }
        #region PackUpTownship
        public bool PackUpTownship(Mobile from, LogHelper logger)
        {
            if (this is TownshipStone stone && !IsPackedUp())
            {
                logger.Log(string.Format("--packing up township for {0}.--", stone.Guild));
                logger.Log(LogType.Mobile, from);

                // disallow packing up township if the guild stone is being moved
                if (stone.Guild == null || stone.Guild.Guildstone == null || stone.Guild.Guildstone.Map == Map.Internal)
                {
                    from.SendMessage("You cannot pack up your township while your guild stone is being moved.");
                    from.SendMessage("Place your guild stone and try again.");
                    return false;
                }

                List<IEntity> ignore_list = new List<IEntity>();

                #region Register unregistered Christmas trees
                {
                    TownshipRegion tsr = TownshipRegion.GetTownshipAt(stone);
                    if (tsr != null)
                    {
                        foreach (var rect in tsr.Coords)
                        {
                            IPooledEnumerable eable = Map.Felucca.GetItemsInBounds(new Rectangle2D(rect.Start, rect.End));
                            foreach (Item item in eable)
                            {
                                if (ignore_list.Contains(item) || item.Deleted)
                                    continue;

                                if (item is ChristmasTreeAddon)
                                {
                                    if (!IsInHouse(item))
                                    {
                                        if (!stone.ItemRegistry.Table.Keys.Contains(item))
                                        {
                                            TownshipItemHelper.SetOwnership(item, from);
                                            if (!stone.ItemRegistry.Table.Keys.Contains(item))
                                                ; // error
                                        }
                                    }
                                    else
                                        ; // just want to know
                                }
                            }
                            eable.Free();
                        }
                    }
                    else
                        ; // error
                }
                #endregion Register unregistered Christmas trees

                List<TownshipLivestockRestorationDeed> livestockDeeds = new();
                #region Livestock
                {
                    if (stone.Livestock != null)
                    {
                        foreach (BaseCreature bc in stone.Livestock.Keys)
                        {
                            if (ignore_list.Contains(bc) || bc.Deleted)
                                continue;

                            TownshipLivestockRestorationDeed deed = new TownshipLivestockRestorationDeed(stone, bc);
                            livestockDeeds.Add(deed);
                        }

                        foreach (var unit in livestockDeeds)
                            logger.Log(string.Format("Packing {0} into deed {1}", unit.Mobile, unit));
                    }
                }
                #endregion Livestock

                List<TownshipItemRestorationDeed> lockdownDeeds = new();
                #region  pack up township lockdowns
                {
                    List<Item> toMove = new List<Item>();
                    foreach (Item item in stone.LockdownRegistry.Keys)
                    {
                        if (ignore_list.Contains(item) || item.Deleted)
                            continue;

                        item.SetLastMoved();
                        toMove.Add(item);
                        ignore_list.Add(item);
                    }

                    if (toMove.Count > 0)
                    {
                        foreach (Item item in toMove)
                            lockdownDeeds.Add(new TownshipItemRestorationDeed(stone, item));

                        foreach (var unit in lockdownDeeds)
                            logger.Log(string.Format("Packing {0} into deed {1}", unit.Item, unit));
                    }

                }
                #endregion pack up township lockdowns

                List<TownshipNPCDeed> NPCDeeds = new();
                #region pack up the NPCs
                {   // pack up the NPCs
                    Dictionary<Mobile, Item> logging = new();
                    if (stone.TownshipNPCs != null)
                    {
                        List<Mobile> list = stone.TownshipNPCs;
                        foreach (Mobile m in list)
                        {
                            if (ignore_list.Contains(m) || m.Deleted)
                                continue;

                            TownshipNPCDeed deed = ConfirmRedeedNPCGump.GetDeed(from, m.GetType());
                            deed.RestorationMobile = m.Serial;
                            logging.Add(m, deed);
                            m.MoveToIntStorage();
                            NPCDeeds.Add(deed);
                        }

                        foreach (var unit in logging)
                            logger.Log(string.Format("Packing {0} into deed {1}", unit.Key, unit.Value));
                    }
                }
                #endregion pack up the NPCs

                List<TownshipItemRestorationDeed> plantDeeds = new();
                SmallCrate seedBox = new SmallCrate();
                seedBox.Name = "(seeds)";
                #region Plants
                {
                    List<Item> toMove = new();
                    List<ITownshipItem> list = new();
                    foreach (var ele in Utility.TownshipItems(stone, new List<Type>() { typeof(TownshipPlantItem) }))
                    {
                        if (ignore_list.Contains(ele as Item) || ele.Deleted)
                            continue;
                        list.Add(ele);
                    }
                    if (list.Count > 0)
                    {
                        seedBox.MaxItems = list.Count;  // ensure storage
                        foreach (var tpi in list)
                            if (tpi is StaticPlantItem spi && !spi.Deleted)
                            {
                                if (spi.PlantStatus == PlantStatus.DecorativePlant)
                                {   // grab the decorative plant
                                    toMove.Add(spi);
                                    ignore_list.Add(spi);
                                }
                                else if (spi.PlantStatus != PlantStatus.DeadTwigs)
                                {   // generate a seed
                                    Seed seed = new Seed(spi.PlantType, spi.PlantHue, showType: true);
                                    seed.Name = string.Format("{0} {1} seed", seed.PlantHue, seed.PlantType);
                                    logger.Log(string.Format("Generating seed {0} from {1}", seed, spi));
                                    if (!seedBox.TryDropItem(World.GetSystemAcct(), seed, sendFullMessage: false))
                                        throw new ApplicationException(string.Format("Max items:{0} insufficient to store seeds in seed box:{1}.", seedBox.MaxItems, seedBox));
                                    logger.Log(string.Format("Dropping seed {0} into {1}", seed, seedBox));
                                    if (TownshipItemHelper.AllTownshipItems.Contains(tpi))
                                        TownshipItemHelper.Unregister(tpi);
                                    spi.Delete();
                                    ignore_list.Add(spi);
                                }
                            }

                        if (toMove.Count > 0 || seedBox.Items.Count > 0)
                        {
                            foreach (Item item in toMove)
                                if (item.Deleted)
                                    throw new ApplicationException("Trying to deed an item that has already been deleted.");
                                else
                                    plantDeeds.Add(new TownshipItemRestorationDeed(stone, item));

                            foreach (var unit in plantDeeds)
                                logger.Log(string.Format("Packing {0} into deed {1}", unit.Item, unit));

                            foreach (var deed in seedBox.Items)
                                logger.Log(string.Format("Packing deed {0} into deed box {1}", deed, seedBox));
                        }
                    }
                }
                #endregion Plants

                List<TownshipItemRestorationDeed> addonDeeds = new();
                SmallCrate addonDeedBox = new();
                addonDeedBox.Name = "(deeds)";
                #region cleanup the township building items
                {   // cleanup the township building items

                    List<Item> toMove = new();

                    List<Item> list = new List<Item>(stone.ItemRegistry.Table.Keys);
                    foreach (Item tsi in list)
                    {
                        if (ignore_list.Contains(tsi) || tsi.Deleted)
                            continue;

                        if (tsi is PlantAddon pa && ignore_list.Contains(pa.PlantItem))
                            continue;

                        /// probably not needed as the preview has not yet been added to thew township items
                        if (tsi is BaseAddon preview && preview.GetItemBool(ItemBoolTable.Preview))
                        {   // exploit prevention
                            System.Diagnostics.Debug.Assert(false); // we still want to know if it's possible
                            continue;
                        }

                        if (!IsInHouse(tsi))
                        {
                            if (tsi is StaticHouseHelper.FixerAddon)
                            {
                                ignore_list.Contains(tsi);
                                continue;
                            }
                            else if (tsi is BaseAddon ba)
                            {
#if true
                                Item addon = ba as Item;
                                Item deed;
                                if (!Utility.IsRedeedableAddon(addon, out deed))
                                {   // no deed for this addon, we will just grab the naked addon
                                    addon.SetLastMoved();
                                    toMove.Add(addon);
                                    ignore_list.Add(addon);
                                }
                                else
                                {   // add the deed for redeedable addon/trophy/wall hanger
                                    // For BaseAddon, we need to stuff the addon hue into the deed. This will be reflected when the addon is instantiated.
                                    if (deed is BaseAddonDeed) deed.Hue = addon.Hue;
                                    logger.Log(string.Format("Generating deed {0} from {1}", deed, addon));
                                    if (!addonDeedBox.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                        throw new ApplicationException(string.Format("Max items:{0} insufficient to store deeds in deed box:{1}.", addonDeedBox.MaxItems, addonDeedBox));
                                    logger.Log(string.Format("Dropping deed {0} into {1}", deed, addonDeedBox));

                                    addon.Delete();
                                    ignore_list.Add(addon);
                                }
#else
                                tsi.SetLastMoved();
                                toMove.Add(tsi);
                                ignore_list.Add(tsi);
#endif
                            }
                            else
                            {
                                // Credit townstone
                                if (ignore_list.Contains(tsi))
                                    continue;
                                else
                                    switch (DefTownshipCraft.CreditTownship(stone, tsi))
                                    {
                                        case DefTownshipCraft.CreditResult.Failed:
                                            {
                                                logger.Log(string.Format("Failed to credit township for {0}", tsi));
                                                tsi.Delete();
                                                ignore_list.Add(tsi);
                                                break;
                                            }
                                        case DefTownshipCraft.CreditResult.Partial:
                                            {
                                                logger.Log(string.Format("Partial to credit township for {0}", tsi));
                                                tsi.Delete();
                                                ignore_list.Add(tsi);
                                                break;
                                            }
                                        case DefTownshipCraft.CreditResult.Full:
                                            {
                                                logger.Log(string.Format("Full to credit township for {0}", tsi));
                                                tsi.Delete();
                                                ignore_list.Add(tsi);
                                                break;
                                            }
                                    }
                            }
                        }
                        else
                            ; // error
                    }

                    if (toMove.Count > 0 || addonDeedBox.Items.Count > 0)
                    {
                        foreach (Item item in toMove)
                            if (item.Deleted)
                                throw new ApplicationException("Trying to deed an item that has already been deleted.");
                            else
                                addonDeeds.Add(new TownshipItemRestorationDeed(stone, item));

                        foreach (var unit in addonDeeds)
                            logger.Log(string.Format("Packing {0} into deed {1}", unit.Item, unit));

                        foreach (var deed in addonDeedBox.Items)
                            logger.Log(string.Format("Packing deed {0} into deed box {1}", deed, addonDeedBox));
                    }
                }
                #endregion cleanup the township building items

                #region cleanup the township scaffolding
                {   // cleanup the township building items

                    List<Item> list = new List<Item>(stone.Scaffolds);
                    foreach (Item tsi in list)
                    {
                        if (ignore_list.Contains(tsi) || tsi.Deleted)
                            continue;

                        // Credit townstone
                        if (tsi is Scaffold scaffold)
                            switch (DefTownshipCraft.CreditTownship(stone, scaffold.ToBuild))
                            {
                                case DefTownshipCraft.CreditResult.Failed:
                                    {
                                        logger.Log(string.Format("Failed to credit township for {0}", tsi));
                                        tsi.Delete();
                                        ignore_list.Add(tsi);
                                        break;
                                    }
                                case DefTownshipCraft.CreditResult.Partial:
                                    {
                                        logger.Log(string.Format("Partial to credit township for {0}", tsi));
                                        tsi.Delete();
                                        ignore_list.Add(tsi);
                                        break;
                                    }
                                case DefTownshipCraft.CreditResult.Full:
                                    {
                                        logger.Log(string.Format("Full to credit township for {0}", tsi));
                                        tsi.Delete();
                                        ignore_list.Add(tsi);
                                        break;
                                    }
                            }
                        else
                            throw new ApplicationException(string.Format("Found {0} while looking for scaffolds.", tsi));
                    }
                }
                #endregion cleanup the township building items

                List<MovingCrate> movingCrates = new();

                #region Moving Crate Key
                uint keyValue = Key.RandomValue();
                Key key = new Key(KeyType.Magic, keyValue);
                key.LootType = LootType.Blessed;
                key.Name = string.Format("a moving crate key for {0}", stone.Guild.Abbreviation ?? stone.Guild.Name);
                #endregion Moving Crate Key

                #region Build Moving Crates
                {
                    if (livestockDeeds.Count > 0)
                    {
                        MovingCrate crate = new MovingCrate(livestockDeeds.Count, Utility.RandomSpecialHue("livestockDeeds"), stone);
                        crate.Label = "(livestock deeds)";
                        foreach (Item deed in livestockDeeds)
                            if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", livestockDeeds.Count));
                        ConfigureLock(crate, keyValue);
                        movingCrates.Add(crate);
                        logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
                    }
                    if (lockdownDeeds.Count > 0)
                    {
                        MovingCrate crate = new MovingCrate(lockdownDeeds.Count, Utility.RandomSpecialHue("lockdownDeeds"), stone);
                        crate.Label = "(lockdown deeds)";
                        foreach (Item deed in lockdownDeeds)
                            if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", lockdownDeeds.Count));
                        ConfigureLock(crate, keyValue);
                        movingCrates.Add(crate);
                        logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
                    }
                    if (NPCDeeds.Count > 0)
                    {
                        MovingCrate crate = new MovingCrate(NPCDeeds.Count, Utility.RandomSpecialHue("NPCDeeds"), stone);
                        crate.Label = "(NPC deeds)";
                        foreach (Item deed in NPCDeeds)
                            if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", NPCDeeds.Count));
                        ConfigureLock(crate, keyValue);
                        movingCrates.Add(crate);
                        logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
                    }
                    if (plantDeeds.Count > 0 || seedBox.Items.Count > 0)
                    {   // add one for the seedbox container itself
                        int item_count = plantDeeds.Count + seedBox.Items.Count + (seedBox.Items.Count > 0 ? 1 : 0);
                        MovingCrate crate = new MovingCrate(item_count, Utility.RandomSpecialHue("plantDeeds"), stone);
                        crate.Label = "(seeds & decorative plants)";
                        // first the seedbox
                        if (seedBox.Items.Count > 0)
                        {
                            if (!crate.TryDropItem(World.GetSystemAcct(), seedBox, sendFullMessage: false))
                                throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", item_count));

                            logger.Log(string.Format("Adding {0} to moving crate {1} {2}", seedBox, crate, crate.Label));
                        }
                        // now the deeds
                        if (plantDeeds.Count > 0)
                        {
                            foreach (Item deed in plantDeeds)
                                if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                    throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", item_count));

                            logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
                        }
                        ConfigureLock(crate, keyValue);
                        movingCrates.Add(crate);
                    }
                    if (addonDeeds.Count > 0 || addonDeedBox.Items.Count > 0)
                    {
                        int item_count = addonDeeds.Count + addonDeedBox.Items.Count + (addonDeedBox.Items.Count > 0 ? 1 : 0);
                        MovingCrate crate = new MovingCrate(item_count, Utility.RandomSpecialHue("addonDeeds"), stone);
                        crate.Label = "(addon deeds)";
                        // first the addonDeedBox
                        if (addonDeedBox.Items.Count > 0)
                        {
                            if (!crate.TryDropItem(World.GetSystemAcct(), addonDeedBox, sendFullMessage: false))
                                throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", item_count));

                            logger.Log(string.Format("Adding {0} to moving crate {1} {2}", addonDeedBox, crate, crate.Label));
                        }
                        // now the deeds
                        if (addonDeeds.Count > 0)
                        {
                            foreach (Item deed in addonDeeds)
                                if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                    throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", item_count));
                        }
                        ConfigureLock(crate, keyValue);
                        movingCrates.Add(crate);
                        logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
                    }

                }
                #endregion Build Moving Crates

                #region Townstone and Guild (Da stones baby, da stones!)
                {
                    if (movingCrates.Count > 0)
                    {
                        TownshipRestorationDeed townshipDeed = new TownshipRestorationDeed(stone, movingCrates, key);
                        switch (Utility.SecureGive(from, townshipDeed))
                        {
                            case Backpack:
                                {
                                    from.SendMessage("A township restoration deed was placed in your backpack.");
                                    logger.Log(string.Format("A township restoration deed was placed in the backpack of {0}", from));
                                    break;
                                }
                            case BankBox:
                                {
                                    from.SendMessage("A township restoration deed was placed in your bank box.");
                                    logger.Log(string.Format("A township restoration deed was placed in the bank box of {0}", from));
                                    break;
                                }
                            default:
                                {
                                    from.SendMessage("A township restoration deed was dropped at your feet.");
                                    logger.Log(string.Format("A township restoration deed was placed at the feet of {0} (locked down)", from));
                                    break;
                                }
                        }
                        switch (Utility.SecureGive(from, key))
                        {
                            case Backpack:
                                {
                                    from.SendMessage("The key to your moving crates was placed in your backpack.");
                                    logger.Log(string.Format("A moving crate key was placed in the backpack of {0}", from));
                                    break;
                                }
                            case BankBox:
                                {
                                    from.SendMessage("The key to your moving crates was placed in your bank box.");
                                    logger.Log(string.Format("A moving crate key was placed in the bank box of {0}", from));
                                    break;
                                }
                            default:
                                {
                                    from.SendMessage("The key to your moving crates dropped at your feet.");
                                    logger.Log(string.Format("A moving crate key was placed at the feet of {0} (locked down)", from));
                                    break;
                                }
                        }
                        SetTownshipStoneBool(TownshipStoneBoolTable.IsPackedUp, true);
                    }
                    else
                    {
                        from.SendMessage("Nothing to pack up.");
                        return false;
                    }
                }
                #endregion Townstone and Guild (Da stones baby, da stones!) 

                logger.Log(LogType.Mobile, from);
                logger.Log(string.Format("--township for {0} packed up.--", stone.Guild));
            }

            return true;
        }
        private static bool IsInHouse(Item item)
        {
            return BaseHouse.Find(item.Location, item.Map) != null;
        }
        private static void ConfigureLock(LockableContainer c, uint keyValue)
        {
            // LockLevel of 0 means that the door can't be picklocked
            // LockLevel of -255 means it's magic locked
            c.Locked = true;
            c.MaxLockLevel = 0; // ?
            c.LockLevel = 0;
            c.KeyValue = keyValue;
        }
        public bool IsPackedUp()
        {
            // packed up, but crates have not yet been created
            if (GetTownshipStoneBool(TownshipStoneBoolTable.IsPackedUp))
                return true;

            // If we have crates, they must be emptied
            foreach (Item item in World.Items.Values)
                if (item.Deleted == false && item is MovingCrate mc && mc.Property == this)
                    return true;
            return false;
        }
        #endregion PackUpTownship

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            int version = 19;
            writer.Write(version);                      // version
            writer.WriteEncodedInt((int)m_BoolTable);   // version 16 (always follows version

            // version 19
            writer.Write(m_FameAndTaxBankHTML);

            // version 18
            if (GetTownshipStoneBool(TownshipStoneBoolTable.HasLBTaxSubsidy))
                writer.Write(m_CurrentWeekOfYear);
            if (GetTownshipStoneBool(TownshipStoneBoolTable.HasLBFameSubsidy))
                writer.Write(m_LastLBFameSubsidy);

            // version 17
            if (GetTownshipStoneBool(TownshipStoneBoolTable.HasLBTaxSubsidy))
                writer.Write(m_LBTaxSubsidy);
            if (GetTownshipStoneBool(TownshipStoneBoolTable.HasLBFameSubsidy))
                writer.Write(m_LBFameSubsidy);

            writer.WriteMobileList(m_TownshipNPCs);

            writer.WriteEncodedInt(m_Livestock.Count);

            foreach (KeyValuePair<BaseCreature, Mobile> kvp in m_Livestock)
            {
                writer.Write((BaseCreature)kvp.Key);
                writer.Write((Mobile)kvp.Value);
            }

            writer.WriteMobileList(m_BuildingPermits);

            writer.WriteEncodedInt(m_LockdownRegistry.Count);

            foreach (var kvp in m_LockdownRegistry)
            {
                writer.Write(kvp.Key);
                kvp.Value.Serialize(writer);
            }

            //version 7 additions
            m_Stockpile.Serialize(writer);

            //version 6 additions
            m_ItemRegistry.Serialize(writer);

            //version 5 additions
            writer.Write(m_OutsideNPCInteractionAllowed);

            //version 4 additions:
            writer.Write(m_TownshipCenter);

            //version 3 additions:
            writer.Write(m_WeeksAtThisLevel);

            //version 2 additions
            writer.Write(m_LastActualActivityWeekTotal);

            //version 1 below:
            writer.Write((int)m_ActivityLevel);
            writer.Write((int)m_LastActualActivityLevel);
            writer.WriteDeltaTime(m_ALLastCalculated);

            writer.Write((int)m_LawLevel);

            writer.Write((int)m_GoldHeld);

            writer.Write((int)m_Deposits.Count);
            for (int i = 0; i < m_Deposits.Count; i++)
                m_Deposits[i].Serialize(writer);

            writer.Write((int)m_Withdrawals.Count);
            for (int i = 0; i < m_Withdrawals.Count; i++)
                m_Withdrawals[i].Serialize(writer);

            writer.Write((string)m_DailyFeesHTML);

            writer.WriteMobileList(m_Enemies, true);

            writer.Write((int)m_CurrentDay);
            m_VisitorCounts.Serialize(writer);
            writer.WriteDeltaTime(m_LastVisit);
            writer.WriteMobileList(m_TodaysVisitors);

            writer.Write((bool)m_Extended);
            writer.Write((DateTime)m_BuiltOn);
            writer.Write((BaseGuild)m_Guild);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            if (version >= 16) m_BoolTable = (TownshipStoneBoolTable)reader.ReadEncodedInt();   // must come after version

            switch (version)
            {
                case 19:
                    {
                        m_FameAndTaxBankHTML = reader.ReadString();
                        goto case 18;
                    }
                case 18:
                    {
                        if (GetTownshipStoneBool(TownshipStoneBoolTable.HasLBTaxSubsidy))
                            m_CurrentWeekOfYear = reader.ReadInt();
                        if (GetTownshipStoneBool(TownshipStoneBoolTable.HasLBFameSubsidy))
                            m_LastLBFameSubsidy = reader.ReadInt();
                        goto case 17;
                    }
                case 17:
                    {
                        if (GetTownshipStoneBool(TownshipStoneBoolTable.HasLBTaxSubsidy))
                            m_LBTaxSubsidy = reader.ReadInt();
                        if (GetTownshipStoneBool(TownshipStoneBoolTable.HasLBFameSubsidy))
                            m_LBFameSubsidy = reader.ReadInt();
                        goto case 16;
                    }
                case 16:
                    {   // moved 'always first'
                        //m_BoolTable = (TownshipStoneBoolTable)reader.ReadEncodedInt();
                        goto case 15;
                    }
                case 15:
                case 14:
                    {
                        m_TownshipNPCs = reader.ReadStrongMobileList();
                        goto case 13;
                    }
                case 13:
                    {
                        if (version >= 15)
                        {
                            int count = reader.ReadEncodedInt();

                            for (int i = 0; i < count; i++)
                            {
                                BaseCreature bc = reader.ReadMobile() as BaseCreature;
                                Mobile owner = reader.ReadMobile();

                                if (bc != null)
                                    m_Livestock[bc] = owner;
                            }
                        }
                        else
                        {
                            List<BaseCreature> livestock = reader.ReadStrongMobileList<BaseCreature>();

                            for (int i = 0; i < livestock.Count; i++)
                            {
                                BaseCreature bc = livestock[i];

                                if (bc != null)
                                {
                                    Mobile lastOwner = null;

                                    if (bc.Owners.Count != 0)
                                        lastOwner = (Mobile)bc.Owners[bc.Owners.Count - 1];

                                    m_Livestock[bc] = lastOwner;
                                }
                            }
                        }

                        goto case 12;
                    }
                case 12:
                    {
                        m_BuildingPermits = reader.ReadStrongMobileList();
                        goto case 11;
                    }
                case 11:
                case 10:
                case 9:
                case 8:
                    {
                        int count = reader.ReadEncodedInt();
                        for (int i = 0; i < count; i++)
                        {
                            Item item = reader.ReadItem();
                            LockDownContext context = new LockDownContext(reader);
                            if (item != null)
                            {
                                if (!m_LockdownRegistry.ContainsKey(item))
                                    m_LockdownRegistry.Add(item, context);
                                else
                                    ;// error
                                item.IsLockedDown = true;
                            }
                        }
                        goto case 7;
                    }
                case 7:
                    m_Stockpile.Deserialize(reader);
                    goto case 6;
                case 6:
                    m_ItemRegistry.Deserialize(reader);
                    goto case 5;
                case 5:
                    m_OutsideNPCInteractionAllowed = reader.ReadBool();
                    goto case 4;
                case 4:
                    m_TownshipCenter = reader.ReadPoint3D();
                    goto case 3;
                case 3:
                    m_WeeksAtThisLevel = reader.ReadInt();
                    goto case 2;
                case 2:
                    m_LastActualActivityWeekTotal = reader.ReadInt();
                    goto case 1;
                case 1:
                    m_ActivityLevel = (Township.ActivityLevel)reader.ReadInt();
                    m_LastActualActivityLevel = (Township.ActivityLevel)reader.ReadInt();
                    m_ALLastCalculated = reader.ReadDeltaTime();

                    m_LawLevel = (Township.LawLevel)reader.ReadInt();

                    m_GoldHeld = reader.ReadInt();

                    int depositsCount = reader.ReadInt();
                    for (int i = 0; i < depositsCount; i++)
                        m_Deposits.Add(new Transaction(reader, version < 10));
                    if (version < 10)
                        m_Deposits.Reverse();

                    int withdrawalsCount = reader.ReadInt();
                    for (int i = 0; i < withdrawalsCount; i++)
                        m_Withdrawals.Add(new Transaction(reader, version < 10));
                    if (version < 10)
                        m_Withdrawals.Reverse();

                    m_DailyFeesHTML = reader.ReadString();

                    m_Enemies = reader.ReadStrongMobileList();

                    m_CurrentDay = (DayOfWeek)reader.ReadInt();
                    m_VisitorCounts.Deserialize(reader);
                    m_LastVisit = reader.ReadDeltaTime();
                    m_TodaysVisitors = reader.ReadMobileList();

                    m_Extended = reader.ReadBool();
                    m_BuiltOn = reader.ReadDateTime();
                    m_Guild = reader.ReadGuild() as Guild;
                    break;
            }

            if (version < 4)
                m_TownshipCenter = this.Location;

            if (version < 9 && Guild != null)
                CustomRegion.GuildAlignment = Guild.Alignment;

            if (version < 11)
                Hue = Township.TownshipSettings.Hue;

            if (version < 14)
                ValidationQueue<TownshipStone>.Enqueue(this);
        }

        public void Validate(object state)
        {
            if (this.CustomRegion != null)
            {
                foreach (Mobile m in this.CustomRegion.Mobiles.Values)
                {
                    if (TownshipNPCHelper.IsTownshipNPC(m) && !m_TownshipNPCs.Contains(m))
                        m_TownshipNPCs.Add(m);
                }
            }

            foreach (BaseHouse house in this.TownshipHouses)
            {
                if (house.Owner != null && IsMemberOrAlly(house.Owner))
                {
                    foreach (Mobile m in house.Region.Mobiles.Values)
                    {
                        if (TownshipNPCHelper.IsTownshipNPC(m) && !m_TownshipNPCs.Contains(m))
                            m_TownshipNPCs.Add(m);
                    }
                }
            }
        }
    }

    public class TownshipAccessTarget : Target
    {
        private TownshipStone m_TownshipStone;
        private bool m_Unrestricted;

        public TownshipAccessTarget(TownshipStone ts, bool unrestricted)
            : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_TownshipStone = ts;
            m_Unrestricted = unrestricted;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive)
                return;

            Item item = targeted as Item;

            if (item == null || (item is Container && !(item is KeyRing)))
            {
                from.SendMessage("You can't set that to unrestricted access.");
            }
            else if (!item.IsLockedDown || item.IsSecure || !m_TownshipStone.IsLockedDown(item))
            {
                from.SendMessage("The item must be locked down in your township.");
            }
            else if (!item.IsAccessibleTo(from))
            {
                from.SendMessage("You cannot access that.");
            }
            else if (!m_TownshipStone.IsLockdownOwner(from, item))
            {
                from.SendMessage("This is not your item.");
            }
            else if (m_Unrestricted && item.IsTSItemFreelyAccessible)
            {
                from.SendMessage("That is already set to unrestricted access.");
            }
            else if (!m_Unrestricted && !item.IsTSItemFreelyAccessible)
            {
                from.SendMessage("That is already set to restricted access.");
            }
            else
            {
                item.IsTSItemFreelyAccessible = m_Unrestricted;

                if (m_Unrestricted)
                    from.SendMessage("That item is now set to unrestricted access.");
                else
                    from.SendMessage("That item is now set to restricted access.");
            }
        }
    }
}