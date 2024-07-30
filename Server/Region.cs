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

/* Server/Region.cs
 * CHANGELOG:
 *  8/14/2023, Adam (MatchingRegions)
 *      MatchingRegions verifies all regions match between the player and the NPC (banker for instance.)
 *      A good example is standing in Wind Park, and yelling over the cave wall there to town to bank.
 *          this.Say("I will not do business with an exploiter!");
 *  5/30/2023, Adam (OnEnter/OnExit)
 *      Remove messages - our static regions handle this.
 *  1/3/23, Yoar
 *      Added Coords, InnBounds setters - Needed to dynamically copy region props
 *  11/13/22, Adam (GetLogoutDelay)
 *      Siege: Murderers and criminals will not be able to kick/ban others from their home, nor will they be able to log out instantly
 *              https://www.uoguide.com/Siege_Perilous
 *  10/7/22, Adam
 *      Port to RunUO 2.6 Compatibility
 *          I didn't port regions over, but rather made our regions compatible with RunUO 2.6 Map and Sector.
 *          - needs testing
 *  9/24/22, Yoar
 *      Converted Coords/Innbounds ArrayList to List<Rectangle3D>
 *  9/15/22, Yoar
 *      Added PropertyObject attribute and exposed some props using CommandProperty
 *      Implemented Serialize, Deserialize
 *  3/20/22, Adam (RegisterOnSpeech)
 *      items can register themselves to receive a copy of whatever text was spoken in that region.
 *  1/14/22, Adam
 *      Added attribute load failure for "music" and "priority" instead of relying on exception handling.
 *	2/21/11, Adam
 *		Renamed from base region to region to better match the RunUO file structure.
 *		Please see the file Scripts/Regions/BaseRegion.cs for additional comments.
 *	6/27/10, Adam
 *		Add the notion of Smart Guards to defend against in-town griefing (exp pots, and melee attacks)
 *	6/21/10, Adam
 *		Added IsGuarded to list of IS attributes
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 * 4/10/10, Adam
 *		Added new region overridables
 *		KeepsItemsOnDeath
 *			Yep, players keep all their loot
 *		OnAfterDeath
 *			Oppertunity to Modify 'ghost', like set frozen which doesn't work in OnDeath since that is cleared after the region
 *			method is called.
 *	04/24/09, plasma
 *		- Prevent all XML regions loading
 *		- Created a new method that returns a List<Region> cache using the xml data
 *			Have taken care to avoid AddRegion() and RemoveRegion() being called due to region props!
 *		- Some overloads to allow a single region to be found and returned
 *	7/7/08, Adam
 *		Replace exception logic for Map.Parse() with normal error handling. (No longer throws an exception.)
 *	5/11/08, Adam
 *		Performance: Convert Regions to use HashTables instead of ArrayLists
 *	10/10/2006, Pix
 *		Added static IsInNoMurderZone for nested regions
 *  3/25/07, Adam
 *      Add FindAll() function to locate all regions at a given point.
 *  7/29/06, Kit
 *		Added virtual EquipItem call.
 *	6/15/06, Pix
 *		Added virtual IsNoMurderZone property.
 */

using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Server
{
    public enum MusicName
    {
        Invalid = -1,
        OldUlt01 = 0,
        Create1,
        DragFlit,
        OldUlt02,
        OldUlt03,
        OldUlt04,
        OldUlt05,
        OldUlt06,
        Stones2,
        Britain1,
        Britain2,
        Bucsden,
        Jhelom,
        LBCastle,
        Linelle,
        Magincia,
        Minoc,
        Ocllo,
        Samlethe,
        Serpents,
        Skarabra,
        Trinsic,
        Vesper,
        Wind,
        Yew,
        Cave01,
        Dungeon9,
        Forest_a,
        InTown01,
        Jungle_a,
        Mountn_a,
        Plains_a,
        Sailing,
        Swamp_a,
        Tavern01,
        Tavern02,
        Tavern03,
        Tavern04,
        Combat1,
        Combat2,
        Combat3,
        Approach,
        Death,
        Victory,
        BTCastle,
        Nujelm,
        Dungeon2,
        Cove,
        Moonglow,
        Zento,
        TokunoDungeon
    }

    public enum RegionPriorityType
    {
        Highest = 0x96,
        House = 0x96,
        High = 0x90,        // Angel Island Prison
        Medium = 0x64,      // Stronghold regions
        Low = 0x60,
        Inn = 0x33,
        Town = 0x32,        // default priority
        TownLow = 0x31,     // Jhelom Islands
        Moongates = 0x28,
        GreenAcres = 0x1,
        Lowest = 0x0
    }

    [PropertyObject]
    public class Region : IComparable
    {
        private int m_Priority;
        private List<Rectangle3D> m_Coords;
        private List<Rectangle3D> m_InnBounds;
        private Map m_Map;
        private string m_Name;
        private Region m_Parent;            // added for RunUO compatibility, but never set
        private string m_Prefix;
        private Point3D m_GoLoc;
        private int m_UId;
        private bool m_Load;
        private Dictionary<Serial, Mobile> m_Players;
        private Dictionary<Serial, Mobile> m_Mobiles;
        private MusicName m_Music = MusicName.Invalid;

        public static void Initialize()
        {
            CommandSystem.Register("RegionInfo", AccessLevel.Administrator, new CommandEventHandler(RegionInfo_OnCommand));
        }

        [Usage("RegionInfo")]
        [Description("Returns the info related to this region.")]
        public static void RegionInfo_OnCommand(CommandEventArgs e)
        {
            ArrayList list = Region.FindAll(e.Mobile.Location, e.Mobile.Map);
            foreach (Region rx in list)
            {
                DumpRegionInfo(e.Mobile, rx);
                e.Mobile.SendMessage("---");
            }

            e.Mobile.SendMessage("Done.");
        }
        private static void DumpRegionInfo(Mobile m, Region rx)
        {
            if (m.Map != null)
                m.SendMessage("Region Info for Region '{0}'", rx == m.Map.DefaultRegion ? "DefaultRegion" : rx.Name);
            else
                m.SendMessage("You are in the default region for the null map");
            m.SendMessage("{0} {1}", rx.Coords.Count, rx.Coords.Count > 1 ? "different rectangles" : "rectangle");
            int mLocation = 0;
            for (int ix = 0; ix < rx.Coords.Count; ix++)
            {
                m.SendMessage("{0}. {1}", ix + 1, rx.Coords[ix].ToString());
                if (rx.Coords[ix].Contains(m.Location))
                    mLocation = ix + 1;
            }
            m.SendMessage("You are in rectangle #{0}", mLocation);
        }
        public int CompareTo(object o)
        {
            if (!(o is Region))
                return 0;

            Region r = (Region)o;

            int a = r.m_Priority;
            int b = m_Priority;

            if (a < b)
                return -1;
            else if (a > b)
                return 1;
            else
                return 0;

            /*if ( o is Region )
			{
				return ((Region)o).Priority.CompareTo( Priority );
			} 
			else
				return 0;*/
        }

        /*private Region( string prefix, string name, Map map, int uid ) : this(prefix,name,map)
		{
			m_UId = uid | 0x40000000;
		}*/
        public Region(string name, Map map, int priority, params Rectangle3D[] area)
        {
            m_Prefix = "";
            m_Name = name;
            m_Map = map;

            m_Priority = priority;

            m_Coords = new List<Rectangle3D>(area);
            m_InnBounds = new List<Rectangle3D>();

            m_GoLoc = Point3D.Zero;

            m_Players = new Dictionary<Serial, Mobile>();
            m_Mobiles = new Dictionary<Serial, Mobile>();

            m_Load = true;

            m_UId = m_RegionUID++;
        }
        public Region(string prefix, string name, Map map)
        {
            m_Prefix = prefix;
            m_Name = name;
            m_Map = map;

            m_Priority = Region.LowestPriority;

            m_Coords = new List<Rectangle3D>();
            m_InnBounds = new List<Rectangle3D>();

            m_GoLoc = Point3D.Zero;

            m_Players = new Dictionary<Serial, Mobile>();
            m_Mobiles = new Dictionary<Serial, Mobile>();

            m_Load = true;

            m_UId = m_RegionUID++;
        }

        public virtual bool IsNoMurderZone
        {
            get
            {
                return false;
            }
        }

        #region Serialization

        public virtual void Serialize(GenericWriter writer)
        {
            writer.Write((int)1); // version

            writer.Write((int)m_Priority);
            WriteArea(m_Coords, writer);
            WriteArea(m_InnBounds, writer);
            writer.Write((Map)m_Map);
            writer.Write((string)m_Name);
            writer.Write((string)m_Prefix);
            writer.Write((Point3D)m_GoLoc);
            writer.Write((int)m_Music);
        }

        public virtual void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Priority = reader.ReadInt();
                        m_Coords.AddRange(ReadArea(reader));
                        m_InnBounds = ReadArea(reader);
                        m_Map = reader.ReadMap();
                        m_Name = reader.ReadString();
                        m_Prefix = reader.ReadString();
                        m_GoLoc = reader.ReadPoint3D();
                        m_Music = (MusicName)reader.ReadInt();
                        break;
                    }
            }
        }

        private static void WriteArea(List<Rectangle3D> area, GenericWriter writer)
        {
            foreach (Rectangle3D rect in area)
            {
                writer.Write((byte)0x2);
                writer.Write((Rectangle3D)rect);
            }

            writer.Write((byte)0x0); // end token
        }

        private static List<Rectangle3D> ReadArea(GenericReader reader)
        {
            List<Rectangle3D> list = new List<Rectangle3D>();

            byte token;

            while ((token = reader.ReadByte()) != 0x0)
            {
                switch (token)
                {
                    case 0x1: list.Add(ConvertTo3D(reader.ReadRect2D())); break;
                    case 0x2: list.Add(reader.ReadRect3D()); break;
                }
            }

            return list;
        }

        #endregion

        public virtual void MakeGuard(Mobile focus)
        {
        }

        public virtual Type GetResource(Type type)
        {
            return type;
        }

        public virtual bool KeepsItemsOnDeath()
        {   // special 'keep loot' area like the CTF region
            return false;
        }

        public virtual bool CanUseStuckMenu(Mobile m, bool quiet = false)
        {
            return true;
        }

        public virtual void OnAggressed(Mobile aggressor, Mobile aggressed, bool criminal)
        {
        }

        public virtual void OnDidHarmful(Mobile harmer, Mobile harmed)
        {
        }

        public virtual void OnGotHarmful(Mobile harmer, Mobile harmed)
        {
        }

        public virtual void OnPlayerAdd(Mobile m)
        {
        }

        public virtual void OnPlayerRemove(Mobile m)
        {
        }

        public virtual void OnMobileAdd(Mobile m)
        {
        }

        public virtual void OnMobileRemove(Mobile m)
        {
        }

        public virtual bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            return true;
        }

        public virtual void OnLocationChanged(Mobile m, Point3D oldLocation)
        {
        }

        public virtual void PlayMusic(Mobile m)
        {
            if (m_Music != MusicName.Invalid && m.NetState != null)
            {
                Utility.SoundCanvas[m] = m_Music;
                m.Send(Network.PlayMusic.GetInstance(m_Music));
            }
        }

        public virtual void StopMusic(Mobile m)
        {
            if (m_Music != MusicName.Invalid && m.NetState != null)
                m.Send(Network.PlayMusic.InvalidInstance);
        }

        public void InternalEnter(Mobile m)
        {
            if (m.Player && !m_Players.ContainsKey(m.Serial))
            {
                m_Players[m.Serial] = m;

                m.CheckLightLevels(false);

                //m.Send( new Network.GlobalLightLevel( (int)LightLevel( m, Map.GlobalLight ) ) );//update the light level
                //m.Send( new Network.PersonalLightLevel( m ) );

                OnPlayerAdd(m);
            }

            if (!m_Mobiles.ContainsKey(m.Serial))
            {
                m_Mobiles[m.Serial] = m;

                OnMobileAdd(m);
            }

            //OnEnter(m);
            PlayMusic(m);
        }

        public virtual void OnEnter(Mobile m)
        {
            // 5/30/2023, Adam: Our StaticRegions now take care of this
#if false
            string s = ToString();
            if (m.Map != Map.Internal && m.Map != null)
                // "(null):" == unnamed region.
                if (!string.IsNullOrEmpty(s) && !s.Contains("(null):") && this is not Server.Regions.HouseRegion)
                    m.SendMessage("You have entered {0}", this);
#endif
            InternalEnter(m);
        }

        public void InternalExit(Mobile m)
        {
            // Adam's new fast version!
            if (m.Player && m_Players.ContainsKey(m.Serial))
            {
                m_Players.Remove(m.Serial);
                OnPlayerRemove(m);
            }

            if (m_Mobiles.ContainsKey(m.Serial))
            {
                m_Mobiles.Remove(m.Serial);
                OnMobileRemove(m);
            }

            //OnExit(m);
            StopMusic(m);
        }

        public virtual void OnExit(Mobile m)
        {
            // 5/30/2023, Adam: Our StaticRegions now take care of this
#if false
            string s = ToString();
            if (m.Map != Map.Internal && m.Map != null)
                // "(null):" == unnamed region.
                if (!string.IsNullOrEmpty(s) && !s.Contains("(null):") && this is not Server.Regions.HouseRegion)
                    m.SendMessage("You have left {0}", this);
#endif
            InternalExit(m);
        }

        public virtual bool OnTarget(Mobile m, Target t, object o)
        {
            return true;
        }

        public virtual bool OnCombatantChange(Mobile m, Mobile Old, Mobile New)
        {
            return true;
        }

        public virtual bool AllowHousing(Point3D p)
        {
            return true;
        }

        public virtual bool SendInaccessibleMessage(Item item, Mobile from)
        {
            return false;
        }

        public virtual bool CheckAccessibility(Item item, Mobile from)
        {
            return true;
        }

        public virtual bool OnDecay(Item item)
        {
            return true;
        }

        public virtual bool AllowHarmful(Mobile from, Mobile target)
        {
            if (Mobile.AllowHarmfulHandler != null)
                return Mobile.AllowHarmfulHandler(from, target);

            return true;

            /*if ( (Map.Rules & MapRules.HarmfulRestrictions) != 0 )
				return false;
			else
				return true;*/
        }

        public virtual void OnCriminalAction(Mobile m, bool message)
        {
            if (message)
                m.SendLocalizedMessage(1005040); // You've committed a criminal act!!
        }

        public virtual bool AllowBenificial(Mobile from, Mobile target)
        {
            if (Mobile.AllowBeneficialHandler != null)
                return Mobile.AllowBeneficialHandler(from, target);

            return true;

            /*if ( (Map.Rules & MapRules.BeneficialRestrictions) != 0 )
			{
				int n = Notoriety.Compute( from, target );

				if (n == Notoriety.Criminal || n == Notoriety.Murderer)
				{
					return false;
				}
				else if ( target.Guild != null && target.Guild.Type != Guilds.GuildType.Regular )//disallow Chaos/order for healing each other or being healed by blues
				{
					if ( from.Guild == null || from.Guild.Type != target.Guild.Type )
						return false;
				}
			}
			return true;*/
        }

        public virtual void OnBenificialAction(Mobile helper, Mobile target)
        {
        }

        public virtual void OnGotBenificialAction(Mobile helper, Mobile target)
        {
        }

        public virtual bool IsInInn(Point3D p)
        {
            foreach (Rectangle3D rect in m_InnBounds)
            {
                if (rect.Contains(p))
                    return true;
            }

            return false;
        }

        private static TimeSpan m_InnLogoutDelay = TimeSpan.Zero;
        private static TimeSpan m_GMLogoutDelay = TimeSpan.FromSeconds(10.0);
        private static TimeSpan m_DefaultLogoutDelay = TimeSpan.FromMinutes(5.0);

        public static readonly int DefaultPriority = 50;

        public static TimeSpan InnLogoutDelay
        {
            get { return m_InnLogoutDelay; }
            set { m_InnLogoutDelay = value; }
        }

        public static TimeSpan GMLogoutDelay
        {
            get { return m_GMLogoutDelay; }
            set { m_GMLogoutDelay = value; }
        }

        public static TimeSpan DefaultLogoutDelay
        {
            get { return m_DefaultLogoutDelay; }
            set { m_DefaultLogoutDelay = value; }
        }

        public virtual TimeSpan GetLogoutDelay(Mobile m)
        {
            // Murderers and criminals will not be able to kick/ban others from their home, nor will they be able to log out instantly
            // https://www.uoguide.com/Siege_Perilous
            if (m.Criminal || m.IsMurderer && Core.RuleSets.SiegeStyleRules())
                return m_DefaultLogoutDelay;
            if (m.Aggressors.Count == 0 && m.Aggressed.Count == 0 && IsInInn(m.Location))
                return m_InnLogoutDelay;
            else if (m.AccessLevel >= AccessLevel.GameMaster)
                return m_GMLogoutDelay;
            else
                return m_DefaultLogoutDelay;
        }
        internal static void OnRegionChange(Mobile m, Region oldRegion, Region newRegion)
        {
            if (newRegion != null && m.NetState != null)
            {
                m.CheckLightLevels(false);

                if (oldRegion == null || oldRegion.Music != newRegion.Music)
                {
#if TODO
                    m.Send(PlayMusic.GetInstance(newRegion.Music));
#else
                    m.Send(Network.PlayMusic.GetInstance(newRegion.Music));
#endif
                }
            }

            Region oldR = oldRegion;
            Region newR = newRegion;
#if TODO
            // We don't have child regions on AI.. yet
            while (oldR != newR)
            {
                int oldRChild = (oldR != null ? oldR.ChildLevel : -1);
                int newRChild = (newR != null ? newR.ChildLevel : -1);

                if (oldRChild >= newRChild)
                {
                    oldR.OnExit(m);
                    oldR = oldR.Parent;
                }

                if (newRChild >= oldRChild)
                {
                    newR.OnEnter(m);
                    newR = newR.Parent;
                }
            }
#else
            // I guess
            oldR.OnExit(m);
            newR.OnEnter(m);
#endif
        }
        // adam: we no longer use regions for every different kind of area, but instead use the notion
        //	of 'rule sets'. 
        public virtual bool IsDungeonRules { get { return false; } }
        public virtual bool IsJailRules { get { return false; } }
        public virtual bool IsHouseRules { get { return false; } }
        public virtual bool IsGreenAcresRules { get { return false; } }
        public virtual bool IsAngelIslandRules { get { return false; } }
        public virtual bool IsTownRules { get { return false; } }
        public virtual bool IsGuarded { get { return false; } set {; } }
        public virtual bool IsSmartGuards { get { return false; } set {; } }
        public virtual bool RespectGuardIgnore
        {
            get
            {   // 5/9/23, Adam, hack: we need Brit guards to protect the city during the Taking Back Sosaria event,
                //  but the implications of making this more general solution is not immediately possible due to time constraints.
                // I.e., we don't want effect Angel Island's use of GuardIgnore.
                if (!string.IsNullOrEmpty(Name) && Core.RuleSets.SiegeStyleRules() && (Name.Equals("Britain Farms") || Name.Equals("Britain")))
                    return false;
                else
                    return true;
            }
        }
        public virtual void AlterLightLevel(Mobile m, ref int global, ref int personal)
        {
        }

        /*public virtual double LightLevel( Mobile m, double level )
		{
			return level;
		}*/

        public virtual void SpellDamageScalar(Mobile caster, Mobile target, ref double damage)
        {
        }

        // items can register themselves to receive a copy of whatever text was spoken in that region.
        public List<Item> RegisterOnSpeech = new List<Item>();

        public virtual void OnSpeech(SpeechEventArgs args)
        {
            foreach (Item item in RegisterOnSpeech)
                if (item != null && item.Deleted == false)
                    item.OnSpeech(args);
        }

        public virtual bool AllowSpawn()
        {
            return true;
        }

        public virtual bool OnSkillUse(Mobile m, int Skill)
        {
            return true;
        }

        public virtual bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            return true;
        }

        public virtual void OnSpellCast(Mobile m, ISpell s)
        {
        }

        public virtual bool EquipItem(Mobile m, Item item)
        {
            return true;
        }

        public virtual void OnEquipmentAdded(object parent, Item item)
        {
        }
        public virtual void OnEquipmentRemoved(object parent, Item item)
        {
        }
        public virtual bool OnResurrect(Mobile m)
        {
            return true;
        }

        public virtual bool OnDeath(Mobile m)
        {
            return true;
        }

        public virtual void OnAfterDeath(Mobile m)
        {
        }

        public virtual bool OnDamage(Mobile m, ref int Damage)
        {
            return true;
        }

        public virtual bool OnHeal(Mobile m, ref int Heal)
        {
            return true;
        }

        public virtual bool OnDoubleClick(Mobile m, object o)
        {
            return true;
        }

        public virtual bool OnSingleClick(Mobile m, object o)
        {
            return true;
        }

        //Should this region be loaded from the xml?
        public bool LoadFromXml
        {
            get
            {
                return m_Load;
            }
            set
            {
                m_Load = value;
            }
        }

        //does this region save?
        public virtual bool Saves
        {
            get
            {
                return false;
            }
        }

        public Dictionary<Serial, Mobile> Mobiles
        {
            get
            {
                return m_Mobiles;
            }
        }

        public Dictionary<Serial, Mobile> Players
        {
            get
            {
                return m_Players;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Prefix
        {
            get
            {
                return m_Prefix;
            }
            set
            {
                m_Prefix = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MusicName Music
        {
            get { return m_Music; }
            set
            {
                if (Music != value)
                {
                    m_Music = value;

                    foreach (Mobile m in Players.Values)
                    {
                        if (m.NetState != null)
                            PlayMusic(m);
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D GoLocation
        {
            get
            {
                return m_GoLoc;
            }
            set
            {
                m_GoLoc = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map Map
        {
            get { return m_Map; }
            set
            {
                if (m_Map != value)
                {
                    bool update = Regions.Contains(this);

                    if (update)
                        RemoveRegion(this);

                    m_Map = value;

                    if (update)
                        AddRegion(this);
                }
            }
        }

        public List<Rectangle3D> Coords
        {
            get
            {
                return m_Coords;
            }
            set
            {
                m_Coords = value;
            }
        }

        public List<Rectangle3D> InnBounds
        {
            get
            {
                return m_InnBounds;
            }
            set
            {
                m_InnBounds = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Priority
        {
            get
            {
                return m_Priority;
            }
            set
            {
                if (m_Priority != value)
                    m_Priority = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public RegionPriorityType PriorityType
        {
            get { return (RegionPriorityType)m_Priority; }
            set { this.Priority = (int)value; }
        }

        public int UId
        {
            get
            {
                return m_UId;
            }
        }

        public static readonly int DefaultMinZ = sbyte.MinValue;
        public static readonly int DefaultMaxZ = sbyte.MaxValue + 1;

        public static Rectangle3D ConvertTo3D(Rectangle2D rect2D)
        {
            return new Rectangle3D(new Point3D(rect2D.Start, DefaultMinZ), new Point3D(rect2D.End, DefaultMaxZ));
        }

        public static List<Rectangle3D> ConvertTo3D(List<Rectangle2D> area2D)
        {
            List<Rectangle3D> area3D = new List<Rectangle3D>();

            for (int i = 0; i < area2D.Count; i++)
                area3D.Add(ConvertTo3D(area2D[i]));

            return area3D;
        }

        private int m_MinZ = DefaultMinZ;
        private int m_MaxZ = DefaultMaxZ;

        [CommandProperty(AccessLevel.GameMaster)]
        public int MinZ
        {
            get { return m_MinZ; }
            set
            {
                if (m_MinZ != value)
                {
                    bool update = Regions.Contains(this);

                    if (update)
                        RemoveRegion(this);

                    m_MinZ = value;

                    if (update)
                        AddRegion(this);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxZ
        {
            get { return m_MaxZ; }
            set
            {
                if (m_MaxZ != value)
                {
                    bool update = Regions.Contains(this);

                    if (update)
                        RemoveRegion(this);

                    m_MaxZ = value;

                    if (update)
                        AddRegion(this);
                }
            }
        }

        public bool Contains(Point3D p)
        {
            foreach (Rectangle3D rect in m_Coords)
            {
                if (rect.Contains(p))
                    return true;
            }

            return false;
        }

        public bool Contains(Point2D p)
        {
            return Contains(new Point3D(p.X, p.Y, Utility.GetAverageZ(this.Map, p.X, p.Y)));
        }

        public bool Contains(Rectangle2D p)
        {
            foreach (Rectangle3D rect in m_Coords)
            {
                if (rect.Contains(new Point3D(p.X, p.Y, 0)))
                    return true;
            }

            return false;
        }

        public bool IsChildOf(Region region)
        {
            if (region == null)
                return false;

            Region p = m_Parent;

            while (p != null)
            {
                if (p == region)
                    return true;

                p = p.m_Parent;
            }

            return false;
        }

        public Region GetRegion(Type regionType)
        {
            if (regionType == null)
                return null;

            Region r = this;

            do
            {
                if (regionType.IsAssignableFrom(r.GetType()))
                    return r;

                r = r.m_Parent;
            }
            while (r != null);

            return null;
        }

        public Region GetRegion(string regionName)
        {
            if (regionName == null)
                return null;

            Region r = this;

            do
            {
                if (r.m_Name == regionName)
                    return r;

                r = r.m_Parent;
            }
            while (r != null);

            return null;
        }

        public bool IsPartOf(Region region)
        {
            if (this == region)
                return true;

            return IsChildOf(region);
        }

        public bool IsPartOf(Type regionType)
        {
            return (GetRegion(regionType) != null);
        }

        public bool IsPartOf(string regionName)
        {
            return (GetRegion(regionName) != null);
        }

        public override string ToString()
        {
            if (Prefix != "")
                return string.Format("{0} {1}", Prefix, Name);
            else
                return Name;
        }

        public static bool IsNull(Region r)
        {
            return Object.ReferenceEquals(r, null);
        }

        //high priorities first (high priority is less than low priority)
        public static bool operator <(Region l, Region r)
        {
            if (IsNull(l) && IsNull(r))
                return false;
            else if (IsNull(l))
                return true;
            else if (IsNull(r))
                return false;

            return l.Priority > r.Priority;
        }

        public static bool operator >(Region l, Region r)
        {
            if (IsNull(l) && IsNull(r))
                return false;
            else if (IsNull(l))
                return false;
            else if (IsNull(r))
                return true;

            return l.Priority < r.Priority;
        }

        public static bool operator ==(Region l, Region r)
        {
            if (IsNull(l))
                return IsNull(r);
            else if (IsNull(r))
                return false;

            return l.UId == r.UId;
        }

        public static bool operator !=(Region l, Region r)
        {
            if (IsNull(l))
                return !IsNull(r);
            else if (IsNull(r))
                return true;

            return l.UId != r.UId;
        }

        public override bool Equals(object o)
        {
            if (!(o is Region) || o == null)
                return false;
            else
                return ((Region)o) == this;
        }

        public override int GetHashCode()
        {
            return m_UId;
        }


        public const int LowestPriority = 0;
        public const int HighestPriority = 150;

        public const int TownPriority = 50;
        public const int HousePriority = HighestPriority;
        public const int InnPriority = TownPriority + 1;

        private static int m_RegionUID = 1;//use to give each region a unique identifier number (to check for equality)

        public bool IsDefault { get { return (this == m_Map.DefaultRegion); } }

        public void UpdateRegion()
        {
            if (Regions.Contains(this))
            {
                RemoveRegion(this);
                AddRegion(this);
            }
        }

        public void Unregister()
        {
            if (m_Map == null)
                return;

            foreach (Rectangle3D rect in m_Coords)
            {
                Point2D start = m_Map.Bound(new Point2D(rect.Start));
                Point2D end = m_Map.Bound(new Point2D(rect.End));

                Sector startSector = m_Map.GetSector(start);
                Sector endSector = m_Map.GetSector(end);

                for (int x = startSector.X; x <= endSector.X; ++x)
                    for (int y = startSector.Y; y <= endSector.Y; ++y)
                        m_Map.GetRealSector(x, y).OnLeave(this);
            }
        }

        public void Register()
        {
            if (m_Map == null)
                return;

            foreach (Rectangle3D rect in m_Coords)
            {
                Point2D start = m_Map.Bound(new Point2D(rect.Start));
                Point2D end = m_Map.Bound(new Point2D(rect.End));

                Sector startSector = m_Map.GetSector(start);
                Sector endSector = m_Map.GetSector(end);

                for (int x = startSector.X; x <= endSector.X; ++x)
                    for (int y = startSector.Y; y <= endSector.Y; ++y)
#if TODO
                        m_Map.GetRealSector(x, y).OnEnter(this);
#else
                        m_Map.GetRealSector(x, y).OnEnter(this, rect);
#endif
            }
        }

        public static void AddRegion(Region region)
        {
            //Plasma: add in ref check to prevent duplicate regions being added! lol.
            //PS - yes, yes duplicates do get added ;p
            foreach (Region r in Regions)
                if (ReferenceEquals(r, region))
                {
                    Utility.ConsoleWriteLine("Warning: Duplicate region being added {0}", ConsoleColor.Red, region);
                    //return;
                }

            if (m_Regions.ContainsKey(region.UId))
            {
                Utility.ConsoleWriteLine("Error: Ignoring region with duplicate UId being added {0}", ConsoleColor.Red, region);
                return;
            }

            // region regions are keyed on UID
            m_Regions.Add(region.UId, region);

            region.Register();

            // add it
            // Map regions are keyed on name, so no duplicates
            if (region.Map != null)
                region.Map.RegisterRegion(region);
        }

        public static void RemoveRegion(Region region)
        {
            m_Regions.Remove(region.UId);

            region.Unregister();
            if (region.Map != null)
                region.Map.UnregisterRegion(region);

            ArrayList list = new ArrayList(region.Mobiles.Values);

            for (int i = 0; i < list.Count; ++i)
                ((Mobile)list[i]).ForceRegionReEnter(false);
        }

        private static Dictionary<int, Region> m_Regions = new();
        public static ReadOnlyCollection<Region> Regions
        {
            get
            {
                List<Region> list = new List<Region>(m_Regions.Values);
                return list.AsReadOnly();
            }
        }
        public static Region FindByName(string name, Map map)
        {
            if (map != null && map != Map.Internal)
                foreach (Region rx in Regions)
                    if (rx != null && rx.Map == map && rx.Name.ToLower() == name.ToLower())
                        return rx;

            return null;
        }
        public static Region FindByUId(int uid)
        {
            if (m_Regions.ContainsKey(uid))
                return m_Regions[uid];

            return null;
        }

        // the sector.Regions list is sorted on priority, so the first found is the highest priority
        /*public static Region Find(Point3D p, Map map)
        {
            return Find(p, map, null);
        }*/

        public static Region Find(Point3D p, Map map, Region ignore = null)
        {
            // get a priority sorted list of regions
            ArrayList list = FindAll(p, map);

            // now remove the 'ignored' one
            if (list.Count > 0 && ignore is not null)
            {
                for (int ix = 0; ix < list.Count; ix++)
                {
                    Region rx = list[ix] as Region;
                    if (ignore.UId == rx.UId)
                    {
                        list.RemoveAt(ix);
                        break;
                    }
                }
            }
            /*
            if (map == null)
                return Map.Internal.DefaultRegion;

            Sector sector = map.GetSector(p);
            List<RegionRect> list = sector.RegionRects;

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].Region is Region)
                {
                    // allow the caller to specify a region to ignore
                    if (ignore != null && list[i].Region == ignore)
                        continue;

                    Region region = list[i].Region;
                    if (region.Contains(p))
                        return region;
                }
            }
            */
            return list.Count > 0 ? list[0] as Region : map.DefaultRegion;
        }

        public virtual bool IsMobileCountable(Mobile aggressor)
        {
            return true;
        }

        #region Nested Region Resolver Static Functions

        public static bool IsInitialAggressionNotCountable(Mobile aggressor, Mobile defender)
        {
            bool bReturn = false;

            //if defender is in no murder zone, then not countable
            if (IsPointInNoMurderZone(defender.Location, defender.Map))
            {
                bReturn = true;
            }
            else if (!IsMobileCountableAtPoint(aggressor, aggressor.Location, aggressor.Map))
            {
                bReturn = true;
            }

            return bReturn;
        }

        private static bool IsMobileCountableAtPoint(Mobile m, Point3D p, Map map)
        {
            if (map == null)
                return true;

            Sector sector = map.GetSector(p);
            List<RegionRect> list = sector.RegionRects;

            bool bReturn = true;

            for (int i = 0; i < list.Count; ++i)
            {
                Region region = list[i].Region;

                if (region.Contains(p))
                {
                    if (region.IsMobileCountable(m) == false)
                    {
                        bReturn = false;
                    }
                }
            }

            return bReturn;
        }

        private static bool IsPointInNoMurderZone(Point3D p, Map map)
        {
            if (map == null)
                return false;

            Sector sector = map.GetSector(p);
            List<RegionRect> list = sector.RegionRects;

            bool bReturn = false;

            for (int i = 0; i < list.Count; ++i)
            {
                Region region = list[i].Region;

                if (region.Contains(p))
                {
                    if (region.IsNoMurderZone)
                    {
                        bReturn = true;
                    }
                }
            }

            return bReturn;
        }

        #endregion


        public static bool IsHighestPriority(Region r, Point3D p, Map map)
        {
            return r == GetHighestPriority(p, map);
        }

        public static Region GetHighestPriority(Point3D p, Map map)
        {   // will always contain at least the default region
            //  FindAll is already sorted on Priority
            ArrayList list = FindAll(p, map);
            return list[0] as Region;
        }
        public static bool MatchingRegions(Mobile m1, Mobile m2)
        {   // all regions much match
            List<int> ids1 = new();
            foreach (Region rx in Region.FindAll(m1.Location, m1.Map))
                if (rx != null)
                    ids1.Add(rx.UId);

            List<int> ids2 = new();
            foreach (Region rx in Region.FindAll(m2.Location, m2.Map))
                if (rx != null)
                    ids2.Add(rx.UId);

            return ids1.Except(ids2).ToList().Count == 0;
        }
        public static ArrayList FindAll(Point3D p, Map map)
        {
            List<Region> all = new();

            if (map == null)
            {
                all.Add(Map.Internal.DefaultRegion);
                return new ArrayList(all);
            }

            Sector sector = map.GetSector(p);
            List<RegionRect> list = sector.RegionRects;

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].Region is Region)
                {
                    Region region = list[i].Region;
                    if (region.Contains(p))
                        // don't add duplicates
                        if (all.FirstOrDefault(o => o.UId == region.UId) == null)
                            all.Add(region);
                }
            }

            all.Add(map.DefaultRegion);
            // sort highest => lowest
            all.Sort((x, y) => y.Priority.CompareTo(x.Priority));
            return new ArrayList(all);
        }
    }
}