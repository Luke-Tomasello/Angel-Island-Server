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

/* Server/EventSink.cs
 * CHANGELOG
 *  3/23/2024, Adam (BoatMovingEventHandler)
 *      Add and capture boat moving events. 
 *      In this way, we can simulate mobile OnMovement() (useful for seafaring events using our motion sensor)
 *  5/12/23, Yoar (MovementObservedEventHandler)
 *      Added MovementObservedEventHandler, invoked when one mobiles sees another mobile move
 *  4/26/23, Adam (InvokeBandageTargetRequest)
 *      Fix a copy pasta error where code was copied from another handler, but 
 *      included a check of HousePlaced instead of BandageTargetRequest
 *  9/6/22, Adam (WorldZoneActiveEventHandler)
 *      Called AFTER the state of the WorldZone.ActiveZone changes.
 *  8/26/22, Adam (HousePlacedEventHandler)
 *      Called when a house is placed.
 *  8/18/22, Adam(PreWorldLoadEventHandler)
 *      Add PreWorldLoadEventHandler
 *      This event is fired in WorldLoad BEFORE the world loads (mobiles, items, etc.)
 *      Certain modules, (like CoreAI.xml) must be loaded before the rest of the world loads.
 *      Example: CoreAI.xml contains patchBits that must be correctly interpreted when mobiles and items are loading.
 *  8/15/22, Adam
 *      Add BeforePlayerDeathEventHandler(BeforePlayerDeathEventArgs e)
 *	2/25/10, Adam
 *		Add an event for creature-death-loot-generation
 *	08/01/09, plasma
 *		Fixed recursion bug introduced with last change, caused by codegen, where exceptions are thrown in InvokeLogException
 *	07/20/09, plasma
 *		Refactored every handler except the guild one (46 in total) to use a more accepted practice of raising events.
 *		This stops an unhandled exception in a delegate causing the whole thing to bomb out and the rest not getting executed.
 *  7/5/08, Adam
 *      Add fall-through exception handling for event handlers
 *  5/2/08, Adam
 *		add LogException event
 *  1/8/08, Adam
 *		add WealthTracker event
 *	8/28/07, Adam
 *		Add AddItemEvent so we can record the item serial when a staffer uses [add item
 *	2/05/07 Taran Kain
 *		Added some flexibility to Fastwalk.
 */

using Server.Accounting;
using Server.Guilds;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Server
{
    public delegate void CharacterCreatedEventHandler(CharacterCreatedEventArgs e);
    public delegate void OpenDoorMacroEventHandler(OpenDoorMacroEventArgs e);
    public delegate void DoorUsedEventHandler(DoorUsedEventArgs e);
    public delegate void SpeechEventHandler(SpeechEventArgs e);
    public delegate void LoginEventHandler(LoginEventArgs e);
    public delegate void ServerListEventHandler(ServerListEventArgs e);
    public delegate void MovementEventHandler(MovementEventArgs e);
    public delegate void HungerChangedEventHandler(HungerChangedEventArgs e);
    public delegate void CrashedEventHandler(CrashedEventArgs e);
    public delegate void ShutdownEventHandler(ShutdownEventArgs e);
    public delegate void ShuttingDownEventHandler(ShuttingDownEventArgs e);
    public delegate void HelpRequestEventHandler(HelpRequestEventArgs e);
    public delegate void DisarmRequestEventHandler(DisarmRequestEventArgs e);
    public delegate void StunRequestEventHandler(StunRequestEventArgs e);
    public delegate void OpenSpellbookRequestEventHandler(OpenSpellbookRequestEventArgs e);
    public delegate void CastSpellRequestEventHandler(CastSpellRequestEventArgs e);
    public delegate void AnimateRequestEventHandler(AnimateRequestEventArgs e);
    public delegate void LogoutEventHandler(LogoutEventArgs e);
    public delegate void SocketConnectEventHandler(SocketConnectEventArgs e);
    public delegate void ConnectedEventHandler(ConnectedEventArgs e);
    public delegate void DisconnectedEventHandler(DisconnectedEventArgs e);
    public delegate void RenameRequestEventHandler(RenameRequestEventArgs e);
    public delegate void PlayerDeathEventHandler(PlayerDeathEventArgs e);
    public delegate void BeforePlayerDeathEventHandler(BeforePlayerDeathEventArgs e);
    public delegate void SpawnedMobileCreatedEventHandler(SpawnedMobileCreatedEventArgs e);
    public delegate void SpawnedMobileKilledEventHandler(SpawnedMobileKilledEventArgs e);
    public delegate void ContainerAddItemEventHandler(ContainerAddItemEventArgs e);
    public delegate void OnDragDropEventHandler(OnDragDropEventArgs e);
    public delegate void CreatureGenerateLootEventHandler(CreatureGenerateLootEventArgs e);
    public delegate void VirtueGumpRequestEventHandler(VirtueGumpRequestEventArgs e);
    public delegate void VirtueItemRequestEventHandler(VirtueItemRequestEventArgs e);
    public delegate void VirtueMacroRequestEventHandler(VirtueMacroRequestEventArgs e);
    public delegate void ChatRequestEventHandler(ChatRequestEventArgs e);
    public delegate void AccountLoginEventHandler(AccountLoginEventArgs e);
    public delegate void PaperdollRequestEventHandler(PaperdollRequestEventArgs e);
    public delegate void ProfileRequestEventHandler(ProfileRequestEventArgs e);
    public delegate void ChangeProfileRequestEventHandler(ChangeProfileRequestEventArgs e);
    public delegate void AggressiveActionEventHandler(AggressiveActionEventArgs e);
    public delegate void GameLoginEventHandler(GameLoginEventArgs e);
    public delegate void DeleteRequestEventHandler(DeleteRequestEventArgs e);
    public delegate void PreWorldLoadEventHandler();
    public delegate void WorldLoadEventHandler();
    public delegate void WorldSaveEventHandler(WorldSaveEventArgs e);
    public delegate void SetAbilityEventHandler(SetAbilityEventArgs e);
    public delegate void FastWalkEventHandler(FastWalkEventArgs e);
    public delegate void ServerStartedEventHandler();
    public delegate BaseGuild CreateGuildHandler(CreateGuildEventArgs e);
    public delegate bool OnSingleClickEventHandler(OnSingleClickEventArgs e);
    public delegate void GuildGumpRequestHandler(GuildGumpRequestArgs e);
    public delegate void QuestGumpRequestHandler(QuestGumpRequestArgs e);
    public delegate void ClientVersionReceivedHandler(ClientVersionReceivedArgs e);
    public delegate void AddItemEventHandler(AddItemEventArgs e);
    public delegate void WealthTrackerEventHandler(WealthTrackerEventArgs e);
    public delegate void LogExceptionEventHandler(LogExceptionEventArgs e);
    public delegate void DropMobileEventHandler(DropMobileEventArgs e);
    public delegate void HousePlacedEventHandler(HousePlacedEventArgs e);
    public delegate void BandageTargetRequestHandler(BandageTargetRequestEventArgs e);
    public delegate void MovementObservedEventHandler(MovementObservedEventArgs e);
    public delegate void BoatMovingEventHandler(BoatMovingEventArgs e);

    public class ClientVersionReceivedArgs : EventArgs
    {
        private NetState m_State;
        private ClientVersion m_Version;

        public NetState State { get { return m_State; } }
        public ClientVersion Version { get { return m_Version; } }

        public ClientVersionReceivedArgs(NetState state, ClientVersion cv)
        {
            m_State = state;
            m_Version = cv;
        }
    }

    public class CreateGuildEventArgs : EventArgs
    {
        private int m_Id;
        public int Id { get { return m_Id; } set { m_Id = value; } }

        public CreateGuildEventArgs(int id)
        {
            m_Id = id;
        }
    }
    public class OnSingleClickEventArgs : EventArgs
    {
        Mobile m_from;
        Item m_item;
        public Mobile Mobile { get { return m_from; } set { m_from = value; } }
        public Item Item { get { return m_item; } set { m_item = value; } }

        public OnSingleClickEventArgs(Mobile mobile, Item item)
        {
            m_from = mobile;
            m_item = item;
        }
    }

    public class GuildGumpRequestArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public GuildGumpRequestArgs(Mobile mobile)
        {
            m_Mobile = mobile;
        }
    }

    public class QuestGumpRequestArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public QuestGumpRequestArgs(Mobile mobile)
        {
            m_Mobile = mobile;
        }
    }

    public class SetAbilityEventArgs : EventArgs
    {
        private Mobile m_Mobile;
        private int m_Index;

        public Mobile Mobile { get { return m_Mobile; } }
        public int Index { get { return m_Index; } }

        public SetAbilityEventArgs(Mobile mobile, int index)
        {
            m_Mobile = mobile;
            m_Index = index;
        }
    }

    public class DeleteRequestEventArgs : EventArgs
    {
        private NetState m_State;
        private int m_Index;

        public NetState State { get { return m_State; } }
        public int Index { get { return m_Index; } }

        public DeleteRequestEventArgs(NetState state, int index)
        {
            m_State = state;
            m_Index = index;
        }
    }

    public class GameLoginEventArgs : EventArgs
    {
        private NetState m_State;
        private string m_Username;
        private string m_Password;
        private bool m_Accepted;
        private ALRReason m_RejectReason;
        private CityInfo[] m_CityInfo;

        public NetState State { get { return m_State; } }
        public string Username { get { return m_Username; } set { m_Username = value; } }
        public string Password { get { return m_Password; } set { m_Password = value; } }
        public bool Accepted { get { return m_Accepted; } set { m_Accepted = value; } }
        public ALRReason RejectReason { get { return m_RejectReason; } set { m_RejectReason = value; } }
        public CityInfo[] CityInfo { get { return m_CityInfo; } set { m_CityInfo = value; } }

        public GameLoginEventArgs(NetState state, string un, string pw)
        {
            m_State = state;
            m_Username = un;
            m_Password = pw;
        }
    }

    public class AggressiveActionEventArgs : EventArgs
    {
        private Mobile m_Aggressed;
        private Mobile m_Aggressor;
        private bool m_Criminal;

        public Mobile Aggressed { get { return m_Aggressed; } }
        public Mobile Aggressor { get { return m_Aggressor; } }
        public bool Criminal { get { return m_Criminal; } }

        private static Queue m_Pool = new Queue();

        public static AggressiveActionEventArgs Create(Mobile aggressed, Mobile aggressor, bool criminal)
        {
            AggressiveActionEventArgs args;

            if (m_Pool.Count > 0)
            {
                args = (AggressiveActionEventArgs)m_Pool.Dequeue();

                args.m_Aggressed = aggressed;
                args.m_Aggressor = aggressor;
                args.m_Criminal = criminal;
            }
            else
            {
                args = new AggressiveActionEventArgs(aggressed, aggressor, criminal);
            }

            return args;
        }

        private AggressiveActionEventArgs(Mobile aggressed, Mobile aggressor, bool criminal)
        {
            m_Aggressed = aggressed;
            m_Aggressor = aggressor;
            m_Criminal = criminal;
        }

        public void Free()
        {
            m_Pool.Enqueue(this);
        }
    }

    public class ProfileRequestEventArgs : EventArgs
    {
        private Mobile m_Beholder;
        private Mobile m_Beheld;

        public Mobile Beholder { get { return m_Beholder; } }
        public Mobile Beheld { get { return m_Beheld; } }

        public ProfileRequestEventArgs(Mobile beholder, Mobile beheld)
        {
            m_Beholder = beholder;
            m_Beheld = beheld;
        }
    }

    public class ChangeProfileRequestEventArgs : EventArgs
    {
        private Mobile m_Beholder;
        private Mobile m_Beheld;
        private string m_Text;

        public Mobile Beholder { get { return m_Beholder; } }
        public Mobile Beheld { get { return m_Beheld; } }
        public string Text { get { return m_Text; } }

        public ChangeProfileRequestEventArgs(Mobile beholder, Mobile beheld, string text)
        {
            m_Beholder = beholder;
            m_Beheld = beheld;
            m_Text = text;
        }
    }

    public class PaperdollRequestEventArgs : EventArgs
    {
        private Mobile m_Beholder;
        private Mobile m_Beheld;

        public Mobile Beholder { get { return m_Beholder; } }
        public Mobile Beheld { get { return m_Beheld; } }

        public PaperdollRequestEventArgs(Mobile beholder, Mobile beheld)
        {
            m_Beholder = beholder;
            m_Beheld = beheld;
        }
    }

    public class AccountLoginEventArgs : EventArgs
    {
        private NetState m_State;
        private string m_Username;
        private string m_Password;

        private bool m_Accepted;
        private ALRReason m_RejectReason;

        public NetState State { get { return m_State; } }
        public string Username { get { return m_Username; } }
        public string Password { get { return m_Password; } }
        public bool Accepted { get { return m_Accepted; } set { m_Accepted = value; } }
        public ALRReason RejectReason { get { return m_RejectReason; } set { m_RejectReason = value; } }

        public AccountLoginEventArgs(NetState state, string username, string password)
        {
            m_State = state;
            m_Username = username;
            m_Password = password;
        }
    }

    public class VirtueItemRequestEventArgs : EventArgs
    {
        private Mobile m_Beholder;
        private Mobile m_Beheld;
        private int m_GumpID;

        public Mobile Beholder { get { return m_Beholder; } }
        public Mobile Beheld { get { return m_Beheld; } }
        public int GumpID { get { return m_GumpID; } }

        public VirtueItemRequestEventArgs(Mobile beholder, Mobile beheld, int gumpID)
        {
            m_Beholder = beholder;
            m_Beheld = beheld;
            m_GumpID = gumpID;
        }
    }

    public class VirtueGumpRequestEventArgs : EventArgs
    {
        private Mobile m_Beholder, m_Beheld;

        public Mobile Beholder { get { return m_Beholder; } }
        public Mobile Beheld { get { return m_Beheld; } }

        public VirtueGumpRequestEventArgs(Mobile beholder, Mobile beheld)
        {
            m_Beholder = beholder;
            m_Beheld = beheld;
        }
    }

    public class VirtueMacroRequestEventArgs : EventArgs
    {
        private Mobile m_Mobile;
        private int m_VirtueID;

        public Mobile Mobile { get { return m_Mobile; } }
        public int VirtueID { get { return m_VirtueID; } }

        public VirtueMacroRequestEventArgs(Mobile mobile, int virtueID)
        {
            m_Mobile = mobile;
            m_VirtueID = virtueID;
        }
    }

    public class ChatRequestEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public ChatRequestEventArgs(Mobile mobile)
        {
            m_Mobile = mobile;
        }
    }

    public class PlayerDeathEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public PlayerDeathEventArgs(Mobile mobile)
        {
            m_Mobile = mobile;
        }
    }
    public class SpawnedMobileCreatedEventArgs : EventArgs
    {
        public Mobile Mobile;
        public SpawnedMobileCreatedEventArgs(Mobile m)
        {
            Mobile = m;
        }
    }
    public class ContainerAddItemEventArgs : EventArgs
    {
        private Server.Items.Container m_Container;
        public Server.Items.Container Container { get { return m_Container; } }
        private Item m_Item;
        public Item Item { get { return m_Item; } }

        public ContainerAddItemEventArgs(Items.Container container, Item item)
        {
            m_Container = container;
            m_Item = item;
        }
    }
    public class OnDragDropEventArgs : EventArgs
    {
        private Mobile m_From;
        private Mobile m_To;
        private Item m_Item;
        public Mobile From { get { return m_From; } }
        public Mobile To { get { return m_To; } }
        public Item Item { get { return m_Item; } }

        public OnDragDropEventArgs(Mobile from, Mobile to, Item item)
        {
            m_From = from;
            m_To = to;
            m_Item = item;
        }
    }
    public class SpawnedMobileKilledEventArgs : EventArgs
    {
        public SpawnedMobileKilledEventArgs()
        {

        }
    }
    public class SpawnedMobileKilledInfo : SpawnedMobileKilledEventArgs
    {
        private Mobile m_Mobile;
        private Map m_Map;
        private List<AggressorInfo> m_Aggressors;
        public Mobile Mobile { get { return m_Mobile; } }
        public List<AggressorInfo> Aggressors { get { return m_Aggressors; } }
        public Map Map { get { return m_Map; } }

        public SpawnedMobileKilledInfo(Mobile mobile, List<AggressorInfo> aggressors)
        {
            m_Mobile = mobile;
            m_Aggressors = aggressors;
            m_Map = m_Mobile.Map;
        }
    }
    public class SpawnedMobileKilled : SpawnedMobileKilledEventArgs
    {
        private Mobile m_Mobile;
        private Item m_Spawner;

        public Mobile Mobile { get { return m_Mobile; } }
        public Item Spawner { get { return m_Spawner; } }

        public SpawnedMobileKilled(Mobile mobile, Item Spawner)
        {
            m_Mobile = mobile;
            m_Spawner = Spawner;
        }
    }

    public class CreatureGenerateLootEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public CreatureGenerateLootEventArgs(Mobile mobile)
        {
            m_Mobile = mobile;
        }
    }

    public class RenameRequestEventArgs : EventArgs
    {
        private Mobile m_From, m_Target;
        private string m_Name;

        public Mobile From { get { return m_From; } }
        public Mobile Target { get { return m_Target; } }
        public string Name { get { return m_Name; } }

        public RenameRequestEventArgs(Mobile from, Mobile target, string name)
        {
            m_From = from;
            m_Target = target;
            m_Name = name;
        }
    }

    public class LogoutEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public LogoutEventArgs(Mobile m)
        {
            m_Mobile = m;
        }
    }

    public class SocketConnectEventArgs : EventArgs
    {
        private Socket m_Socket;
        private bool m_AllowConnection;

        public Socket Socket { get { return m_Socket; } }
        public bool AllowConnection { get { return m_AllowConnection; } set { m_AllowConnection = value; } }

        public SocketConnectEventArgs(Socket s)
        {
            m_Socket = s;
            m_AllowConnection = true;
        }
    }

    public class ConnectedEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public ConnectedEventArgs(Mobile m)
        {
            m_Mobile = m;
        }
    }

    public class DisconnectedEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public DisconnectedEventArgs(Mobile m)
        {
            m_Mobile = m;
        }
    }

    public class AnimateRequestEventArgs : EventArgs
    {
        private Mobile m_Mobile;
        private string m_Action;

        public Mobile Mobile { get { return m_Mobile; } }
        public string Action { get { return m_Action; } }

        public AnimateRequestEventArgs(Mobile m, string action)
        {
            m_Mobile = m;
            m_Action = action;
        }
    }

    public class CastSpellRequestEventArgs : EventArgs
    {
        private Mobile m_Mobile;
        private Item m_Spellbook;
        private int m_SpellID;

        public Mobile Mobile { get { return m_Mobile; } }
        public Item Spellbook { get { return m_Spellbook; } }
        public int SpellID { get { return m_SpellID; } }

        public CastSpellRequestEventArgs(Mobile m, int spellID, Item book)
        {
            m_Mobile = m;
            m_Spellbook = book;
            m_SpellID = spellID;
        }
    }
    public class BandageTargetRequestEventArgs : EventArgs
    {
        private Mobile m_Mobile;
        private Item m_Bandage;
        private Mobile m_Target;

        public Mobile Mobile { get { return m_Mobile; } }
        public Item Bandage { get { return m_Bandage; } }
        public Mobile Target { get { return m_Target; } }

        public BandageTargetRequestEventArgs(Mobile m, Item bandage, Mobile target)
        {
            m_Mobile = m;
            m_Bandage = bandage;
            m_Target = target;
        }
    }
    public class OpenSpellbookRequestEventArgs : EventArgs
    {
        private Mobile m_Mobile;
        private int m_Type;

        public Mobile Mobile { get { return m_Mobile; } }
        public int Type { get { return m_Type; } }

        public OpenSpellbookRequestEventArgs(Mobile m, int type)
        {
            m_Mobile = m;
            m_Type = type;
        }
    }

    public class StunRequestEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public StunRequestEventArgs(Mobile m)
        {
            m_Mobile = m;
        }
    }

    public class DisarmRequestEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public DisarmRequestEventArgs(Mobile m)
        {
            m_Mobile = m;
        }
    }

    public class HelpRequestEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public HelpRequestEventArgs(Mobile m)
        {
            m_Mobile = m;
        }
    }

    public class ShutdownEventArgs : EventArgs
    {
        public ShutdownEventArgs()
        {
        }
    }
    public class ShuttingDownEventArgs : EventArgs
    {
        public ShuttingDownEventArgs()
        {
        }
    }

    public class CrashedEventArgs : EventArgs
    {
        private Exception m_Exception;
        private bool m_Close;

        public Exception Exception { get { return m_Exception; } }
        public bool Close { get { return m_Close; } set { m_Close = value; } }

        public CrashedEventArgs(Exception e)
        {
            m_Exception = e;
        }
    }

    public class HungerChangedEventArgs : EventArgs
    {
        private Mobile m_Mobile;
        private int m_OldValue;

        public Mobile Mobile { get { return m_Mobile; } }
        public int OldValue { get { return m_OldValue; } }

        public HungerChangedEventArgs(Mobile mobile, int oldValue)
        {
            m_Mobile = mobile;
            m_OldValue = oldValue;
        }
    }

    public class MovementEventArgs : EventArgs
    {
        private Mobile m_Mobile;
        private Direction m_Direction;
        private bool m_Blocked;

        public Mobile Mobile { get { return m_Mobile; } }
        public Direction Direction { get { return m_Direction; } }
        public bool Blocked { get { return m_Blocked; } set { m_Blocked = value; } }

        private static Queue m_Pool = new Queue();

        public static MovementEventArgs Create(Mobile mobile, Direction dir)
        {
            MovementEventArgs args;

            if (m_Pool.Count > 0)
            {
                args = (MovementEventArgs)m_Pool.Dequeue();

                args.m_Mobile = mobile;
                args.m_Direction = dir;
                args.m_Blocked = false;
            }
            else
            {
                args = new MovementEventArgs(mobile, dir);
            }

            return args;
        }

        public MovementEventArgs(Mobile mobile, Direction dir)
        {
            m_Mobile = mobile;
            m_Direction = dir;
        }

        public void Free()
        {
            m_Pool.Enqueue(this);
        }
    }

    public class ServerListEventArgs : EventArgs
    {
        private NetState m_State;
        private IAccount m_Account;
        private bool m_Rejected;
        private List<ServerInfo> m_Servers;

        public NetState State { get { return m_State; } }
        public IAccount Account { get { return m_Account; } }
        public bool Rejected { get { return m_Rejected; } set { m_Rejected = value; } }
        public List<ServerInfo> Servers { get { return m_Servers; } }

        public void AddServer(string name, IPEndPoint address)
        {
            AddServer(name, 0, TimeZone.CurrentTimeZone, address);
        }

        public void AddServer(string name, int fullPercent, TimeZone tz, IPEndPoint address)
        {
            m_Servers.Add(new ServerInfo(name, fullPercent, tz, address));
        }

        public ServerListEventArgs(NetState state, IAccount account)
        {
            m_State = state;
            m_Account = account;
            m_Servers = new List<ServerInfo>();
        }
    }

    public struct SkillNameValue
    {
        private SkillName m_Name;
        private int m_Value;

        public SkillName Name { get { return m_Name; } }
        public int Value { get { return m_Value; } }

        public SkillNameValue(SkillName name, int value)
        {
            m_Name = name;
            m_Value = value;
        }
    }

    public class CharacterCreatedEventArgs : EventArgs
    {
        private NetState m_State;
        private IAccount m_Account;
        private CityInfo m_City;
        private SkillNameValue[] m_Skills;
        private int m_ShirtHue, m_PantsHue;
        private int m_HairID, m_HairHue;
        private int m_BeardID, m_BeardHue;
        private string m_Name;
        private bool m_Female;
        private int m_Hue;
        private int m_Str, m_Dex, m_Int;
        private int m_Profession;
        private Mobile m_Mobile;

        private Race m_Race;

        public NetState State { get { return m_State; } }
        public IAccount Account { get { return m_Account; } }
        public Mobile Mobile { get { return m_Mobile; } set { m_Mobile = value; } }
        public string Name { get { return m_Name; } }
        public bool Female { get { return m_Female; } }
        public int Hue { get { return m_Hue; } }
        public int Str { get { return m_Str; } }
        public int Dex { get { return m_Dex; } }
        public int Int { get { return m_Int; } }
        public CityInfo City { get { return m_City; } }
        public SkillNameValue[] Skills { get { return m_Skills; } }
        public int ShirtHue { get { return m_ShirtHue; } }
        public int PantsHue { get { return m_PantsHue; } }
        public int HairID { get { return m_HairID; } }
        public int HairHue { get { return m_HairHue; } }
        public int BeardID { get { return m_BeardID; } }
        public int BeardHue { get { return m_BeardHue; } }
        public int Profession { get { return m_Profession; } set { m_Profession = value; } }
        public Race Race { get { return m_Race; } }

        public CharacterCreatedEventArgs(NetState state, IAccount a, string name, bool female, int hue, int str, int dex, int intel, CityInfo city, SkillNameValue[] skills, int shirtHue, int pantsHue, int hairID, int hairHue, int beardID, int beardHue, int profession, Race race)
        {
            m_State = state;
            m_Account = a;
            m_Name = name;
            m_Female = female;
            m_Hue = hue;
            m_Str = str;
            m_Dex = dex;
            m_Int = intel;
            m_City = city;
            m_Skills = skills;
            m_ShirtHue = shirtHue;
            m_PantsHue = pantsHue;
            m_HairID = hairID;
            m_HairHue = hairHue;
            m_BeardID = beardID;
            m_BeardHue = beardHue;
            m_Profession = profession;
            m_Race = race;
        }
    }

    public class OpenDoorMacroEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public OpenDoorMacroEventArgs(Mobile mobile)
        {
            m_Mobile = mobile;
        }
    }

    public class DoorUsedEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public DoorUsedEventArgs(Mobile mobile)
        {
            m_Mobile = mobile;
        }
    }

    public class SpeechEventArgs : EventArgs
    {
        private Mobile m_Mobile;
        private string m_Speech;
        private MessageType m_Type;
        private int m_Hue;
        private int[] m_Keywords;
        private bool m_Handled;
        private bool m_Blocked;
        private bool m_Internal;

        public Mobile Mobile { get { return m_Mobile; } }
        public string Speech { get { return m_Speech; } set { m_Speech = value; } }
        public MessageType Type { get { return m_Type; } }
        public int Hue { get { return m_Hue; } }
        public int[] Keywords { get { return m_Keywords; } }
        public bool Handled { get { return m_Handled; } set { m_Handled = value; } }
        public bool Blocked { get { return m_Blocked; } set { m_Blocked = value; } }
        public bool Internal { get { return m_Internal; } set { m_Internal = value; } }

        public bool HasKeyword(int keyword)
        {
            for (int i = 0; i < m_Keywords.Length; ++i)
                if (m_Keywords[i] == keyword)
                    return true;

            return false;
        }
        public bool HasKeyword(string keyword)
        {
            return Regex.Matches(Speech, @"(\b" + keyword + @"\b)", RegexOptions.IgnoreCase).Count > 0;
        }
        public SpeechEventArgs(Mobile mobile, string speech, MessageType type, int hue, int[] keywords, bool isInternal)
        {
            m_Mobile = mobile;
            m_Speech = speech;
            m_Type = type;
            m_Hue = hue;
            m_Keywords = keywords;
            m_Internal = isInternal;    // called internal from the mobile, region, or item
        }

        public SpeechEventArgs(Mobile mobile, string speech, MessageType type, int hue, int[] keywords)
            : this(mobile, speech, type, hue, keywords, false)
        {

        }
        public bool WasNamed(Mobile m)
        {
            string name = m.Name;

            return (name != null && Insensitive.StartsWith(m_Speech, name));
        }
    }

    public class LoginEventArgs : EventArgs
    {
        private Mobile m_Mobile;

        public Mobile Mobile { get { return m_Mobile; } }

        public LoginEventArgs(Mobile mobile)
        {
            m_Mobile = mobile;
        }
    }

    public class WorldSaveEventArgs : EventArgs
    {
        private bool m_Msg;

        public bool Message { get { return m_Msg; } }

        public WorldSaveEventArgs(bool msg)
        {
            m_Msg = msg;
        }
    }

    public class AddItemEventArgs : EventArgs
    {
        private Item m_item;
        Mobile m_from;

        public Item item { get { return m_item; } }
        public Mobile from { get { return m_from; } }

        public AddItemEventArgs(Item item, Mobile from)
        {
            m_item = item;
            m_from = from;
        }
    }

    public class WealthTrackerEventArgs : EventArgs
    {
        private Item m_item;                // item in question
        private Mobile m_from;              // mobile involved (if any)
        private AuditType m_auditType;      // gold drop, check deposit, etc
        private object m_parent;            // parent or parent to-be

        public AuditType auditType { get { return m_auditType; } }
        public Item item { get { return m_item; } }
        public object parent { get { return m_parent; } }
        public Mobile from { get { return m_from; } }

        public WealthTrackerEventArgs(AuditType auditType, Item item, object parent, Mobile from)
        {
            m_auditType = auditType;
            m_item = item;
            m_parent = parent;
            m_from = from;
        }
    }

    public class LogExceptionEventArgs : EventArgs
    {
        private Exception m_exception;
        public Exception Exception { get { return m_exception; } }
        public LogExceptionEventArgs(Exception ex)
        {
            Diagnostics.LogHelper.LogException(ex);
            m_exception = ex;
        }
    }

    public class BeforePlayerDeathEventArgs : EventArgs
    {
        private Mobile m_from;
        private uint m_guid;
        private Account m_account;
        private DateTime m_timeOfDeath;
        private Serial m_house;
        private int m_followers;
        public Mobile Mobile { get { return m_from; } }
        public uint GUID { get { return m_guid; } }
        public Account Account { get { return m_account; } }
        public Serial House { get { return m_house; } }
        public DateTime TimeOfDeath { get { return m_timeOfDeath; } }
        public int Followers { get { return m_followers; } }
        public BeforePlayerDeathEventArgs(Mobile m, uint guid, Account acct, DateTime timeOfDeath, Serial house, int followers)
        {
            m_from = m;
            m_guid = guid;
            m_account = acct;
            m_timeOfDeath = timeOfDeath;
            m_house = house;
            m_followers = followers;
        }
    }

    public class DropMobileEventArgs : EventArgs
    {
        private Map m_map;
        public Map Map { get { return m_map; } }
        private int m_x;
        public int X { get { return m_x; } }
        private int m_y;
        public int Y { get { return m_y; } }
        public DropMobileEventArgs(Map map, int x, int y)
        {
            m_map = map;
            m_x = x;
            m_y = y;
        }
    }

    public class HousePlacedEventArgs : EventArgs
    {
        private Map m_map;
        public Map Map { get { return m_map; } }
        private Serial m_house;
        public Serial House { get { return m_house; } }
        public HousePlacedEventArgs(Map map, Serial house)
        {
            m_map = map;
            m_house = house;
        }
    }

    public class FastWalkEventArgs
    {
        private NetState m_State;
        private bool m_Blocked;
        private int m_FastWalkCount;

        public FastWalkEventArgs(NetState state, int count)
        {
            m_State = state;
            m_Blocked = false;
            m_FastWalkCount = count;
        }

        public FastWalkEventArgs(NetState state)
            : this(state, 0)
        {
        }

        public NetState NetState { get { return m_State; } }
        public bool Blocked { get { return m_Blocked; } set { m_Blocked = value; } }
        public int FastWalkCount { get { return m_FastWalkCount; } set { m_FastWalkCount = value; } }
    }

    public class MovementObservedEventArgs : EventArgs
    {
        private Mobile m_Observer;
        private Mobile m_Observed;
        private Point3D m_OldLocation;

        public Mobile Observer { get { return m_Observer; } }
        public Mobile Observed { get { return m_Observed; } }
        public Point3D OldLocation { get { return m_OldLocation; } }

        public MovementObservedEventArgs(Mobile observer, Mobile observed, Point3D oldLocation)
        {
            m_Observer = observer;
            m_Observed = observed;
            m_OldLocation = oldLocation;
        }
    }

    public class BoatMovingEventArgs : EventArgs
    {
        private Multis.BaseBoat m_BaseBoat;
        private Point3D m_OldLocation;
        public Multis.BaseBoat Boat { get { return m_BaseBoat; } }
        public Point3D OldLocation { get { return m_OldLocation; } }

        public BoatMovingEventArgs(Multis.BaseBoat boat, Point3D oldLocation)
        {
            m_BaseBoat = boat;
            m_OldLocation = oldLocation;
        }
    }

    public class EventSink
    {
        public static event CharacterCreatedEventHandler CharacterCreated;
        public static event OpenDoorMacroEventHandler OpenDoorMacroUsed;
        public static event DoorUsedEventHandler DoorUsed;
        public static event SpeechEventHandler Speech;
        public static event LoginEventHandler Login;
        public static event ServerListEventHandler ServerList;
        public static event MovementEventHandler Movement;
        public static event HungerChangedEventHandler HungerChanged;
        public static event CrashedEventHandler Crashed;
        public static event ShutdownEventHandler Shutdown;
        public static event ShuttingDownEventHandler ShuttingDown;
        public static event HelpRequestEventHandler HelpRequest;
        public static event DisarmRequestEventHandler DisarmRequest;
        public static event StunRequestEventHandler StunRequest;
        public static event OpenSpellbookRequestEventHandler OpenSpellbookRequest;
        public static event CastSpellRequestEventHandler CastSpellRequest;
        public static event AnimateRequestEventHandler AnimateRequest;
        public static event LogoutEventHandler Logout;
        public static event SocketConnectEventHandler SocketConnect;
        public static event ConnectedEventHandler Connected;
        public static event DisconnectedEventHandler Disconnected;
        public static event RenameRequestEventHandler RenameRequest;
        public static event PlayerDeathEventHandler PlayerDeath;
        public static event BeforePlayerDeathEventHandler BeforePlayerDeath;
        public static event SpawnedMobileCreatedEventHandler SpawnedMobileCreated;
        public static event SpawnedMobileKilledEventHandler SpawnedMobileKilled;
        public static event ContainerAddItemEventHandler ContainerAddItem;
        public static event OnDragDropEventHandler OnDragDrop;
        public static event CreatureGenerateLootEventHandler CreatureGenerateLoot;
        public static event VirtueGumpRequestEventHandler VirtueGumpRequest;
        public static event VirtueItemRequestEventHandler VirtueItemRequest;
        public static event VirtueMacroRequestEventHandler VirtueMacroRequest;
        public static event ChatRequestEventHandler ChatRequest;
        public static event AccountLoginEventHandler AccountLogin;
        public static event PaperdollRequestEventHandler PaperdollRequest;
        public static event ProfileRequestEventHandler ProfileRequest;
        public static event ChangeProfileRequestEventHandler ChangeProfileRequest;
        public static event AggressiveActionEventHandler AggressiveAction;
        public static event CommandEventHandler Command;
        public static event GameLoginEventHandler GameLogin;
        public static event DeleteRequestEventHandler DeleteRequest;
        public static event PreWorldLoadEventHandler PreWorldLoad;
        public static event WorldLoadEventHandler WorldLoad;
        public static event WorldSaveEventHandler WorldSave;
        public static event SetAbilityEventHandler SetAbility;
        public static event FastWalkEventHandler FastWalk;
        public static event CreateGuildHandler CreateGuild;
        public static event OnSingleClickEventHandler OnSingleClick;
        public static event ServerStartedEventHandler ServerStarted;
        public static event GuildGumpRequestHandler GuildGumpRequest;
        public static event QuestGumpRequestHandler QuestGumpRequest;
        public static event ClientVersionReceivedHandler ClientVersionReceived;
        public static event AddItemEventHandler AddItem;
        public static event WealthTrackerEventHandler WealthTracker;
        public static event LogExceptionEventHandler LogException;
        public static event DropMobileEventHandler DropMobile;
        public static event HousePlacedEventHandler HousePlaced;
        public static event BandageTargetRequestHandler BandageTargetRequest;
        public static event MovementObservedEventHandler MovementObserved;
        public static event BoatMovingEventHandler BoatMoving;

        public static BaseGuild InvokeCreateGuild(CreateGuildEventArgs e)
        {
            if (CreateGuild != null)
            {
                try { return CreateGuild(e); }
                catch (Exception ex)
                {
                    // Log an exception
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    return null;
                }
            }
            else
                return null;
        }

        public static bool? InvokeOnSingleClick(OnSingleClickEventArgs e)
        {
            bool? tmp = false;
            if (OnSingleClick != null)
            {
                try { return OnSingleClick(e); }
                catch (Exception ex)
                {
                    // Log an exception
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    return tmp.GetValueOrDefault();
                }
            }
            else
                return tmp.GetValueOrDefault();
        }

        public static void InvokeCharacterCreated(CharacterCreatedEventArgs e)
        {
            if (CharacterCreated != null)
            {
                foreach (CharacterCreatedEventHandler currentDelegate in CharacterCreated.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeOpenDoorMacroUsed(OpenDoorMacroEventArgs e)
        {
            if (OpenDoorMacroUsed != null)
            {
                foreach (OpenDoorMacroEventHandler currentDelegate in OpenDoorMacroUsed.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        public static void InvokeDoorUsed(DoorUsedEventArgs e)
        {
            if (DoorUsed != null)
            {
                foreach (DoorUsedEventHandler currentDelegate in DoorUsed.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }
        public static void InvokeSpeech(SpeechEventArgs e)
        {
            if (Speech != null)
            {
                foreach (SpeechEventHandler currentDelegate in Speech.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeLogin(LoginEventArgs e)
        {
            if (Login != null)
            {
                foreach (LoginEventHandler currentDelegate in Login.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeServerList(ServerListEventArgs e)
        {
            if (ServerList != null)
            {
                foreach (ServerListEventHandler currentDelegate in ServerList.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeMovement(MovementEventArgs e)
        {
            if (Movement != null)
            {
                foreach (MovementEventHandler currentDelegate in Movement.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeHungerChanged(HungerChangedEventArgs e)
        {
            if (HungerChanged != null)
            {
                foreach (HungerChangedEventHandler currentDelegate in HungerChanged.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeCrashed(CrashedEventArgs e)
        {
            if (Crashed != null)
            {
                foreach (CrashedEventHandler currentDelegate in Crashed.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeShutdown(ShutdownEventArgs e)
        {
            if (Shutdown != null)
            {
                foreach (ShutdownEventHandler currentDelegate in Shutdown.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        public static void InvokeShuttingDown(ShuttingDownEventArgs e)
        {
            if (ShuttingDown != null)
            {
                foreach (ShuttingDownEventHandler currentDelegate in ShuttingDown.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }
        public static void InvokeHelpRequest(HelpRequestEventArgs e)
        {
            if (HelpRequest != null)
            {
                foreach (HelpRequestEventHandler currentDelegate in HelpRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeDisarmRequest(DisarmRequestEventArgs e)
        {
            if (DisarmRequest != null)
            {
                foreach (DisarmRequestEventHandler currentDelegate in DisarmRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeStunRequest(StunRequestEventArgs e)
        {
            if (StunRequest != null)
            {
                foreach (StunRequestEventHandler currentDelegate in StunRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeOpenSpellbookRequest(OpenSpellbookRequestEventArgs e)
        {
            if (OpenSpellbookRequest != null)
            {
                foreach (OpenSpellbookRequestEventHandler currentDelegate in OpenSpellbookRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeCastSpellRequest(CastSpellRequestEventArgs e)
        {
            if (CastSpellRequest != null)
            {
                foreach (CastSpellRequestEventHandler currentDelegate in CastSpellRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeAnimateRequest(AnimateRequestEventArgs e)
        {
            if (AnimateRequest != null)
            {
                foreach (AnimateRequestEventHandler currentDelegate in AnimateRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeLogout(LogoutEventArgs e)
        {
            if (Logout != null)
            {
                foreach (LogoutEventHandler currentDelegate in Logout.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeSocketConnect(SocketConnectEventArgs e)
        {
            if (SocketConnect != null)
            {
                foreach (SocketConnectEventHandler currentDelegate in SocketConnect.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeConnected(ConnectedEventArgs e)
        {
            if (Connected != null)
            {
                foreach (ConnectedEventHandler currentDelegate in Connected.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeDisconnected(DisconnectedEventArgs e)
        {
            if (Disconnected != null)
            {
                foreach (DisconnectedEventHandler currentDelegate in Disconnected.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeRenameRequest(RenameRequestEventArgs e)
        {
            if (RenameRequest != null)
            {
                foreach (RenameRequestEventHandler currentDelegate in RenameRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        public static void InvokePlayerDeath(PlayerDeathEventArgs e)
        {
            if (PlayerDeath != null)
            {
                foreach (PlayerDeathEventHandler currentDelegate in PlayerDeath.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }
        public static void InvokeBeforePlayerDeath(BeforePlayerDeathEventArgs e)
        {
            if (BeforePlayerDeath != null)
            {
                foreach (BeforePlayerDeathEventHandler currentDelegate in BeforePlayerDeath.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        public static void InvokeCreatureGenerateLoot(CreatureGenerateLootEventArgs e)
        {
            if (CreatureGenerateLoot != null)
            {
                foreach (CreatureGenerateLootEventHandler currentDelegate in CreatureGenerateLoot.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeVirtueGumpRequest(VirtueGumpRequestEventArgs e)
        {
            if (VirtueGumpRequest != null)
            {
                foreach (VirtueGumpRequestEventHandler currentDelegate in VirtueGumpRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeVirtueItemRequest(VirtueItemRequestEventArgs e)
        {
            if (VirtueItemRequest != null)
            {
                foreach (VirtueItemRequestEventHandler currentDelegate in VirtueItemRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeVirtueMacroRequest(VirtueMacroRequestEventArgs e)
        {
            if (VirtueMacroRequest != null)
            {
                foreach (VirtueMacroRequestEventHandler currentDelegate in VirtueMacroRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeChatRequest(ChatRequestEventArgs e)
        {
            if (ChatRequest != null)
            {
                foreach (ChatRequestEventHandler currentDelegate in ChatRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeAccountLogin(AccountLoginEventArgs e)
        {
            if (AccountLogin != null)
            {
                foreach (AccountLoginEventHandler currentDelegate in AccountLogin.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokePaperdollRequest(PaperdollRequestEventArgs e)
        {
            if (PaperdollRequest != null)
            {
                foreach (PaperdollRequestEventHandler currentDelegate in PaperdollRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeProfileRequest(ProfileRequestEventArgs e)
        {
            if (ProfileRequest != null)
            {
                foreach (ProfileRequestEventHandler currentDelegate in ProfileRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeChangeProfileRequest(ChangeProfileRequestEventArgs e)
        {
            if (ChangeProfileRequest != null)
            {
                foreach (ChangeProfileRequestEventHandler currentDelegate in ChangeProfileRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeAggressiveAction(AggressiveActionEventArgs e)
        {
            if (AggressiveAction != null)
            {
                foreach (AggressiveActionEventHandler currentDelegate in AggressiveAction.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeCommand(CommandEventArgs e)
        {
            if (Command != null)
            {
                foreach (CommandEventHandler currentDelegate in Command.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeGameLogin(GameLoginEventArgs e)
        {
            if (GameLogin != null)
            {
                foreach (GameLoginEventHandler currentDelegate in GameLogin.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeDeleteRequest(DeleteRequestEventArgs e)
        {
            if (DeleteRequest != null)
            {
                foreach (DeleteRequestEventHandler currentDelegate in DeleteRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        public static void InvokePreWorldLoad()
        {
            if (WorldLoad != null)
            {
                foreach (PreWorldLoadEventHandler currentDelegate in PreWorldLoad.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        Utility.Monitor.WriteLine(ex.Message, ConsoleColor.Red);
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }
        public static void InvokeWorldLoad()
        {
            if (WorldLoad != null)
            {
                foreach (WorldLoadEventHandler currentDelegate in WorldLoad.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        Utility.Monitor.WriteLine(ex.Message, ConsoleColor.Red);
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeWorldSave(WorldSaveEventArgs e)
        {
            if (WorldSave != null)
            {
                foreach (WorldSaveEventHandler currentDelegate in WorldSave.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeSetAbility(SetAbilityEventArgs e)
        {
            if (SetAbility != null)
            {
                foreach (SetAbilityEventHandler currentDelegate in SetAbility.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeFastWalk(FastWalkEventArgs e)
        {
            if (FastWalk != null)
            {
                foreach (FastWalkEventHandler currentDelegate in FastWalk.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeServerStarted()
        {
            if (ServerStarted != null)
            {
                foreach (ServerStartedEventHandler currentDelegate in ServerStarted.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeGuildGumpRequest(GuildGumpRequestArgs e)
        {
            if (GuildGumpRequest != null)
            {
                foreach (GuildGumpRequestHandler currentDelegate in GuildGumpRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeQuestGumpRequest(QuestGumpRequestArgs e)
        {
            if (QuestGumpRequest != null)
            {
                foreach (QuestGumpRequestHandler currentDelegate in QuestGumpRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeClientVersionReceived(ClientVersionReceivedArgs e)
        {
            if (ClientVersionReceived != null)
            {
                foreach (ClientVersionReceivedHandler currentDelegate in ClientVersionReceived.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeAddItem(AddItemEventArgs e)
        {
            if (AddItem != null)
            {
                foreach (AddItemEventHandler currentDelegate in AddItem.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeWealthTracker(WealthTrackerEventArgs e)
        {
            if (WealthTracker != null)
            {
                foreach (WealthTrackerEventHandler currentDelegate in WealthTracker.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }


        public static void InvokeLogException(LogExceptionEventArgs e)
        {
            if (LogException != null)
            {
                foreach (LogExceptionEventHandler currentDelegate in LogException.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        //Do nothing to avoid recursion
                        Utility.Monitor.WriteLine(ex.Message, ConsoleColor.Red);
                    }
                }
            }
        }

        public static void InvokeDropMobile(DropMobileEventArgs e)
        {
            if (DropMobile != null)
            {
                foreach (DropMobileEventHandler currentDelegate in DropMobile.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        public static void InvokeBandageTargetRequest(BandageTargetRequestEventArgs e)
        {
            if (BandageTargetRequest != null)
            {
                foreach (BandageTargetRequestHandler currentDelegate in BandageTargetRequest.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }
        public static void InvokeHousePlaced(HousePlacedEventArgs e)
        {
            if (HousePlaced != null)
            {
                foreach (HousePlacedEventHandler currentDelegate in HousePlaced.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        public static void InvokeSpawnedMobileKilled(SpawnedMobileKilledEventArgs e)
        {
            if (SpawnedMobileKilled != null)
            {
                foreach (SpawnedMobileKilledEventHandler currentDelegate in SpawnedMobileKilled.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }
        public static void InvokeContainerAddItem(ContainerAddItemEventArgs e)
        {
            if (ContainerAddItem != null)
            {
                foreach (ContainerAddItemEventHandler currentDelegate in ContainerAddItem.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }
        public static void InvokeOnDragDrop(OnDragDropEventArgs e)
        {
            if (ContainerAddItem != null)
            {
                foreach (OnDragDropEventHandler currentDelegate in OnDragDrop.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }
        public static void InvokeSpawnedMobileCreated(SpawnedMobileCreatedEventArgs e)
        {
            if (SpawnedMobileCreated != null)
            {
                foreach (SpawnedMobileCreatedEventHandler currentDelegate in SpawnedMobileCreated.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        public static void InvokeMovementObserved(MovementObservedEventArgs e)
        {
            if (MovementObserved != null)
            {
                foreach (MovementObservedEventHandler currentDelegate in MovementObserved.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        public static void InvokeBoatMoving(BoatMovingEventArgs e)
        {
            if (BoatMoving != null)
            {
                foreach (BoatMovingEventHandler currentDelegate in BoatMoving.GetInvocationList())
                {
                    try
                    {
                        currentDelegate.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        // Log an exception
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        public static void Reset()
        {
            CharacterCreated = null;
            OpenDoorMacroUsed = null;
            DoorUsed = null;
            Speech = null;
            Login = null;
            ServerList = null;
            Movement = null;
            HungerChanged = null;
            Crashed = null;
            Shutdown = null;
            ShuttingDown = null;
            HelpRequest = null;
            DisarmRequest = null;
            StunRequest = null;
            OpenSpellbookRequest = null;
            CastSpellRequest = null;
            AnimateRequest = null;
            Logout = null;
            SocketConnect = null;
            Connected = null;
            Disconnected = null;
            RenameRequest = null;
            PlayerDeath = null;
            CreatureGenerateLoot = null;
            VirtueGumpRequest = null;
            VirtueItemRequest = null;
            VirtueMacroRequest = null;
            ChatRequest = null;
            AccountLogin = null;
            PaperdollRequest = null;
            ProfileRequest = null;
            ChangeProfileRequest = null;
            AggressiveAction = null;
            Command = null;
            GameLogin = null;
            DeleteRequest = null;
            WorldLoad = null;
            WorldSave = null;
            SetAbility = null;
            GuildGumpRequest = null;
            QuestGumpRequest = null;
            AddItem = null;
            WealthTracker = null;
            LogException = null;
            BeforePlayerDeath = null;
            DropMobile = null;
            MovementObserved = null;
        }
    }
}