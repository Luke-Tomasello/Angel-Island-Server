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

/* Scripts/Accounting/Account.cs
 * CHANGELOG
 *  7/1/2023, Adam (AccountFlags)
 *      Certain flags should transcend mobile creation. The following flags should follow any mobile created on the account
 *      FastWalk
 *      DenyAccessPublicContainer
 *      InvalidClient
 *      InvalidRazor
 *  5/11/23, Adam IPAddresses and MachineInfo
 *      Increase the number of elements stored: from 5 to 32 for IPAddresses, and from 5 to 16 for MachineInfo
 *      Also, if elements are trimmed, the oldest are trimmed, not the most recent like the old imp did.
 *  4/30/23, Adam (LoginStatus)
 *      We now record login errors during game login and post process them instead of blocking the connection.
 *      This is because the client does not process game login errors and instead hangs.
 *      The supported login errors are 
 *          accountConcurrentIPLimiter,
 *          accountTotalIPLimiter,
 *          accountHardwareLimiter,
 *          IPStillHot,
 *          hasAccess,
 *          checkPassword,
 *          banned,
 *      See also: EventSink_Connected in PlayerMobile.cs
 * 12/16/22, Adam (ExceedsMachineInfoLimit flag)
 *  If a player is able to skate around the IP Address limits on a shard (a VPN perhaps,) and the MachinInfo 
 *      is collected for that player, subsequent logins to that account will blocked as normal. However, since the MachinInfo 
 *      comes in after the account is created and while the player is making their first character, we must
 *      schedule a Cron job to kick them.
 *      When the MachinInfo is collected after account creation, we set the ExceedsMachineInfoLimit flag on the account. 
 *      The Cron job runs every 5 minutes and kicks these players.
 *  see also: CKickPlayers in Cron, CheckDisconnect() in MachineInfo
 * 12/15/22, Adam (HardwareHash)
 *  we now add each hash to the list of 'machines' this user has access to.
 *  'machines' in this context refers to the hardware hash returned from the UO client.
 *  We know of four possibilities: OSI hardware hash, AI built ClassicUO, and another physical machine.
 *  The fourth possibility is if CUO ever implements my packet 0xD9 'Spy on Client' packet. This packet
 *  differs from the 'AI built ClassicUO' in that the AI built ClassicUO packet contains the Boot Drive serial.
 *  Objective: We don't want a player able to switch between say OSI client and AI built ClassicUO to skirt 
 *  around our 'machine checks' by intentionally generating multiple hardware hashes. We will catalog all
 *  hardware hashes and consider all of them when deciding whether or not to allow another account to be created.
 * 8/18/22, Adam (Utc)
 *  Convert all read/write of times to handle UTC (the server format.)
 *  All DateTime objects are written as UTC.
 *  All DateTime objects are read as UTC and returned as LOCAL.
 *  Why: This insures that a Saves folder moved from production will run correctly on a local (developers) machine.
 *      Example: (old system) The AI server computer runs at UTC, but but time is saved in 'local' format. This works fine as long as the world files are saved and loaded
 *          on the server. But if you then move the world files to a local developer machine running, say Pacific Time, then when you read the world files all dateTime 
 *          objects will be off by 7-8 hours, depending on DST
 *      The above change fixes this problem by ensuring all DateTime objects are always written as UTC and converted to local on read.
 * 8/16/22, Adam
 *  Add [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)] to all properties (can now be viewed with [account)
 *  rewrite the bitfield stuff as real [Flags] for things like banned, doNotSendEmail, etc.
 * 8/15/22, Adam
 *      Mortalis:
 *      - add an Initializer function to register OnBeforePlayerDeathEventHandler()
 *      - OnBeforePlayerDeathEventHandler() to set the player GUID, house, followers, and time-of-death
 *      - add , house, followers, and time-of-death fields
 *      All of this is to enable the 'gear allocation' system for Mortalis. I.e., we reuse existing equipment that belongs to the player on new character creation
 * 8/9/22, Adam
 *      Add m_HardwareHashAcquired
 *      - set only once when the first HardwareHash is Acquired for this local account.
 *      - used in determining 'first' account to acquire a HardwareHash in HardwareLimiter.
 *      Note: I started out using the Created field in Account to determine which account will be bound to the shard,
 *      but abandoned that since the first Created may NOT be the account that gets the HardwareHash first. The theory
 *      being, that the account played most will get the HardwareHash first.
 *      Add SyncHardwareHashAcquired()
 * 8/3/22, Adam
 *      Add a carryover bankbox for players of Mortalis
 * 8/2/22, Yoar
 *      Added HardwareHash interaction with the DB.
 * 6/20/22, Yoar
 *      Instantiating an account via 'Account(string username, string password)' now updates the DB.
 * 6/20/22, Yoar
 *      Added 'GetRawCredentials' method which we call when we instantiate a new account using
 *      credentials from the accounts DB.
 * 6/19/22, Yoar
 *      Account DB overhaul.
 *      
 *      In the password getters, we sync the account with the accounts DB before returning the password.
 *      Therefore, DB credentials are prioritized over XML credentials.
 *      
 *      There is one instance outside of this class where we wish to access the accounts raw/unsynced
 *      credentials: While we're saving the account credentials to the accounts DB. For this, I added
 *      the 'GetRawCredentials' method.
 * 6/15/22, Yoar
 *      Simplified logic in 'CheckPassword'
 *      Redid constructor logic (less copy-pasta)
 * 8/31/2021, Pix:
 *      Added SeedAccountsFromLoginDB functionality for login server
 * 8/30/2021, Pix:
 *      Added logindb functionality.
 *	2/4/11, Adam
 *		1 character per account on Siege and Island Siege
 *	8/24/10, Adam
 *		Save/Restore the hash of the (previous) HardwareInfo (HardwareHash)
 *		This is used in the cases where the client fails to send a HardwareInfo packet; 
 *		i.e., we use the previous HardwareInfo hash to determine if the player might be multiclienting
 *	9/2/07, Adam
 *		Move GetAccountInfo() from AdminGump.cs to here
 *	01/02/07, Pix
 *		Fixed WatchExpire saving.
 *	11/19/06, Pix
 *		Watchlist enhancements
 *	11/06/06, Pix
 *		Added LastActivationResendTime to prevent spamming activation emails.
 *	10/14/06, Pix
 *		Added enumeration for bitfield, and methods to use them.
 *		Added DoNotSendEmail flag and property.
 *	8/13/06, Pix
 *		Now only keeps the last 5 IPs that have accessed the account (in order).
 *	6/29/05, Pix
 *		Added check for reset password length > 0
 *	6/28/05, Pix
 *		Added reset password field and functionality
 *	06/11/05, Pix
 *		Added Email History, Account Activated, Activation Key
 *  02/15/05, Pixie
 *		CHANGED FOR RUNUO 1.0.0 MERGE.
 *  6/14/04 Pix
 *		House decay modifications: 2 variables to keep track of 
 *		steps taken.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/8/04, pixie
 *		Added email field to account
 */

using Server.Misc;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Server.Accounting
{
    public class Account : IAccount
    {
        #region MORTALIS
        public static void Initialize()
        {
            EventSink.BeforePlayerDeath += new BeforePlayerDeathEventHandler(OnBeforePlayerDeathEventHandler);
        }
        private static void OnBeforePlayerDeathEventHandler(BeforePlayerDeathEventArgs e)
        {
            if (e.Account != null)
            {
                e.Account.GUID = e.GUID;
                e.Account.TimeOfDeath = e.TimeOfDeath;
                e.Account.House = e.House;
                e.Account.Followers = e.Followers;
            }
        }
        #endregion MORTALIS
        #region Account Infractions
        public enum AccountInfraction
        {
            none,
            // login states
            concurrentIPLimiter,
            totalIPLimiter,
            totalHardwareLimiter,
            IPStillHot,
            hasAccess,
            checkPassword,
            banned,
            TorExitNode,
        }
        [Flags]
        public enum IPFlags
        {
            None = 0x0,
            IsAnonymous = 0x01,
            IsAnonymousProxy = 0x02,
            IsAnonymousVpn = 0x04,
            IsHostingProvider = 0x08,
            IsLegitimateProxy = 0x10,
            IsPublicProxy = 0x20,
            IsResidentialProxy = 0x40,
            IsSatelliteProvider = 0x80,
            IsTorExitNode = 0x100,
        }
        public void SetFlag(IPFlags flag, bool value)
        {
            // Permanent flags are permanent. Can never be unset
            if (value)
                m_IPFlagsPermanent |= flag;

            // session flags change with each login
            //  Maybe they're being a good boy now?
            if (value)
                m_IPFlagsSession |= flag;
            else
                m_IPFlagsSession &= ~flag;
        }
        public enum IPFlagsState
        {
            Permanent,  // follows player around - serialized
            Session,    // this session only - not serialized
        }
        public bool GetFlag(IPFlags flag, IPFlagsState state = IPFlagsState.Session)
        {
            if (state == IPFlagsState.Session)
                // return flags for this session
                return ((m_IPFlagsSession & flag) != 0);
            else
                // return flags from the permanent record
                return ((m_IPFlagsPermanent & flag) != 0);
        }
        private AccountInfraction m_AccountInfraction = Account.AccountInfraction.none;
        private IPFlags m_IPFlagsPermanent = IPFlags.None;
        private IPFlags m_IPFlagsSession = IPFlags.None;    // not serialized
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.GameMaster)]
        public AccountInfraction InfractionStatus
        {
            get { return m_AccountInfraction; }
            set { m_AccountInfraction = value; }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.GameMaster)]
        public IPFlags IPFlagsPermanent
        {   // permanent
            get { return m_IPFlagsPermanent; }
            set { m_IPFlagsPermanent = value; }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.GameMaster)]
        public IPFlags IPFlagsSession
        {   // session
            get { return m_IPFlagsSession; }
            set { m_IPFlagsSession = value; }
        }
        #endregion Account Infractions

        private string m_Username, m_PlainPassword, m_CryptPassword;
        private AccessLevel m_AccessLevel;
        private AccountFlag m_Flags = AccountFlag.None;
        private DateTime m_Created, m_LastLogin;
        private ArrayList m_Comments;
        private ArrayList m_Tags;
        private Mobile[] m_Mobiles;
        private string[] m_IPRestrictions;
        private IPAddress[] m_LoginIPs;
        private List<int> m_Machines = new();
        private IPAddress m_LastGAMELogin;
        private string[] m_EmailHistory;
        private HardwareInfo m_HardwareInfo;
        private int m_HardwareHash;
        private DateTime m_HardwareHashAcquired = DateTime.MinValue;//Adam: FIRST time (only) this account Acquired a hardware hash
        private string m_WatchReason = "";
        private DateTime m_WatchExpire = DateTime.MinValue;

        private string m_EmailAddress;
        private bool m_bAccountActivated;
        private string m_ActivationKey;
        private string m_ResetPassword = "";
        private DateTime m_ResetPasswordRequestedTime;
        public DateTime LastActivationResendTime = DateTime.MinValue;
        //Pix: variables for keeping track of steps taken
        //  for use with refreshing houses.
        public DateTime m_STIntervalStart;
        public int m_STSteps;
        #region MORTALIS
        //Adam: Mortalis: your bankbox is saved and passed to the next character created on your account.
        private Serial m_BankBox = Serial.Zero;                 // Available AFTER DEATH
        private Serial m_House = Serial.Zero;                   // Available AFTER DEATH
        private DateTime m_TimeOfDeath = DateTime.MinValue;     // Available AFTER DEATH
        private int m_Followers;                                // Available AFTER DEATH
        private uint m_guid;                                    // Available AFTER DEATH (see the player mobile for current GUID)
        #endregion MORTALIS


        /// <summary>
        /// Deletes the account, all characters of the account, and all houses of those characters
        /// </summary>
        public void Delete()
        {
            for (int i = 0; i < m_Mobiles.Length; ++i)
            {
                Mobile m = this[i];

                if (m == null)
                    continue;

                ArrayList list = Multis.BaseHouse.GetHouses(m);

                for (int j = 0; j < list.Count; ++j)
                    ((Item)list[j]).Delete();

                m.Delete();

                m.Account = null;
                m_Mobiles[i] = null;
            }

            Accounts.Table.Remove(m_Username);
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public uint AccountCode
        { get { return (uint)Utility.GetStableHashCode(Username + Created.ToString(), version: 1); } }
        #region MORTALIS
        /// <summary>
        /// Available AFTER DEATH:
        /// Mortalis needs to know who previously owned this pet (after the current owner dies)
        ///     the GUID uniquely identifies a player, even after death
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public uint GUID
        {
            get { return m_guid; }
            set { m_guid = value; }
        }
        /// <summary>
        /// Available AFTER DEATH:
        /// Mortalis: BankBox carryover for players killed on Mortalis
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public Serial BankBox
        {
            get { return m_BankBox; }
            set { m_BankBox = value; }
        }
        /// <summary>
        /// Available AFTER DEATH:
        /// Mortalis: Followers this player had at the time of death
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int Followers
        {
            get { return m_Followers; }
            set { m_Followers = value; }
        }
        /// <summary>
        /// Available AFTER DEATH:
        /// Mortalis: remember the house oned OnBeforeDeath. We will need to search this for assets we may use to reequip the player
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public Serial House
        {
            get { return m_House; }
            set { m_House = value; }
        }
        /// <summary>
        /// Available AFTER DEATH:
        /// Date detailing when the (one-and-only) died.
        /// Used AccountHandler to override IPLimiter default functionally.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public DateTime TimeOfDeath
        {
            get { return m_TimeOfDeath; }
            set { m_TimeOfDeath = value; }
        }
        #endregion MORTALIS
        /// <summary>
        /// Date detailing when a hardware hash was FIRST acquired
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public DateTime HardwareHashAcquired
        {
            get { return m_HardwareHashAcquired; }
            // should never be set/reset. See HardwareHash for when it is set
            set { }
        }
        /// <summary>
        /// Object detailing information about the hardware of the last person to log into this account
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public HardwareInfo HardwareInfo
        {
            get { return m_HardwareInfo; }
            set { m_HardwareInfo = value; }
        }
        /// <summary>
        /// Hash detailing information about the hardware of the last person to log into this account
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int HardwareHash
        {
            get { SyncHardwareHash(); return m_HardwareHash; }
            set
            {
                SyncHardwareHashAcquired(value);
                // initialize the last known good hardware hash
                m_HardwareHash = value; if (Core.UseLoginDB) AccountsDatabase.SaveAccount(this);
                // now add this hash to the list of 'machines' this user has access to.
                if (!m_Machines.Contains(value))
                    m_Machines.Add(value);
            }
        }

        /// <summary>
        /// Synchronizes the hardware hash with the accounts DB and returns it.
        /// </summary>
        /// <returns></returns>
        private void SyncHardwareHash()
        {
            AccountsDBEntry? dbAccount = AccountsDatabase.GetAccount(m_Username);

            // sync hardware hash from DB
            if (dbAccount != null)
            {
                int hardwareHash = dbAccount.Value.HardwareHash;

                if (hardwareHash != 0)
                    this.HardwareHashRaw = hardwareHash;
            }
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int HardwareHashRaw
        {
            get { return m_HardwareHash; }
            set { m_HardwareHash = value; }
        }

        /// <summary>
        /// List of IP addresses for restricted access. '*' wildcard supported. If the array contains zero entries, all IP addresses are allowed.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public string[] IPRestrictions
        {
            get { return m_IPRestrictions; }
            set { m_IPRestrictions = value; }
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public string[] EmailHistory
        {
            get { return m_EmailHistory; }
            set { m_EmailHistory = value; }
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public string ResetPassword
        {
            get { SyncCredentials(); return m_ResetPassword; }
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public DateTime ResetPasswordRequestedTime
        {
            get { return m_ResetPasswordRequestedTime; }
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public bool AccountActivated
        {
            get { return m_bAccountActivated; }
            set { m_bAccountActivated = value; }
        }

        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public string ActivationKey
        {
            get { return m_ActivationKey; }
            set { m_ActivationKey = value; }
        }

        /// <summary>
        /// List of IP addresses which have successfully logged into this account.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public IPAddress[] LoginIPs
        {
            get { return m_LoginIPs; }
            set { m_LoginIPs = value; }
        }
        public List<int> Machines
        {
            get
            {
                // We need to ad HardwareHashRaw here since it comes from the database, yet
                //  Our saved machines for this machine may not yet have it.
                List<int> machines = new(m_Machines);
                if (HardwareHashRaw != 0 && !machines.Contains(HardwareHashRaw))
                    machines.Add(HardwareHashRaw);

                // ignore machines with 'zero' All accounts will have this
                machines = machines.Where(i => i != 0).ToList();
                return machines;
            }
        }
        public void ClearMachines()
        {
            HardwareHash = 0;
            // ignore machines with 'zero' All accounts will have this
            m_Machines = new();
            return;
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public IPAddress LastGAMELogin
        {
            get { return m_LastGAMELogin; }
            set { m_LastGAMELogin = value; }
        }
        /// <summary>
        /// List of account comments. Type of contained objects is AccountComment.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public ArrayList Comments
        {
            get { return m_Comments; }
        }
        /// <summary>
        /// List of account tags. Type of contained objects is AccountTag.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public ArrayList Tags
        {
            get { return m_Tags; }
        }
        /// <summary>
        /// Account username. Case insensitive validation.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public string Username
        {
            get { return m_Username; }
            set { m_Username = value; }
        }
        /// <summary>
        /// Account password. Plain text. Case sensitive validation. May be null.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public string PlainPassword
        {
            get { SyncCredentials(); return m_PlainPassword; }
        }
        /// <summary>
        /// Account password. Hashed with MD5. May be null.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public string CryptPassword
        {
            get { SyncCredentials(); return m_CryptPassword; }
        }
        /// <summary>
        /// Account Email.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public string EmailAddress
        {
            get { return m_EmailAddress; }
            set { m_EmailAddress = value; }
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public string WatchReason
        {
            get { return m_WatchReason; }
            set { m_WatchReason = value; }
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public DateTime WatchExpire
        {
            get { return m_WatchExpire; }
            set { m_WatchExpire = value; }
        }
        /// <summary>
        /// Initial AccessLevel for new characters created on this account.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public AccessLevel AccessLevel
        {
            get { return AccessLevelInternal; }
            set
            {
                // Don't allow changing access level to Owner or System.
                if (value == AccessLevel.Owner || value == AccessLevel.System) return;

                AccessLevelInternal = value;
            }
        }
        // do not make a command, this is for programmatic use only.
        public AccessLevel AccessLevelInternal
        {
            get
            {
                return m_AccessLevel;
            }
            set
            {
                if (m_AccessLevel != value)
                {
                    m_AccessLevel = value;
                }
            }
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public bool DoNotSendEmail
        {
            get
            {
                return GetFlag(AccountFlag.DoNotSendEmail);
            }
            set
            {
                SetFlag(AccountFlag.DoNotSendEmail, value);
            }
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public bool Watched
        {
            get
            {
                return GetFlag(AccountFlag.Watched);
            }
            set
            {
                SetFlag(AccountFlag.Watched, value);
            }
        }
        /// <summary>
        /// Gets or sets a flag indiciating if this account is banned.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public bool Banned
        {
            get
            {
                bool isBanned = GetFlag(AccountFlag.Banned); //GetFlag( 0 );

                if (!isBanned)
                    return false;

                DateTime banTime;
                TimeSpan banDuration;

                if (GetBanTags(out banTime, out banDuration))
                {
                    if (banDuration != TimeSpan.MaxValue && DateTime.UtcNow >= (banTime + banDuration))
                    {
                        SetUnspecifiedBan(null); // clear
                        Banned = false;
                        return false;
                    }
                }

                return true;
            }
            set
            {
                //SetFlag( 0, value ); 
                SetFlag(AccountFlag.Banned, value);
            }
        }
        /// <summary>
        /// The date and time of when this account was created.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public DateTime Created
        {
            get { return m_Created; }
        }
        /// <summary>
        /// Gets or sets the date and time when this account was last accessed.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public DateTime LastLogin
        {
            get { return m_LastLogin; }
            set { m_LastLogin = value; }
        }

        #region MORTALIS
        /// <summary>
        /// Synchronizes the hardware hash acquired date.
        /// </summary>
        public void SyncHardwareHashAcquired(int hardwareHash)
        {
            // only record the first time the m_HardwareHash is set.
            if (m_HardwareHashAcquired == DateTime.MinValue)
                m_HardwareHashAcquired = DateTime.UtcNow;
            // RESET: should never happen in normal processing (hash is never set to zero)
            //  this is here so we can clear the m_HardwareHashAcquired in PlayerMobile (when HardwareHash is manually set) for testing purposes
            //  or a possible administrative 'reset' of the account 'bound' to a particular machine
            if (hardwareHash == 0)
                m_HardwareHashAcquired = DateTime.MinValue;
        }
        #endregion MORTALIS
        #region FLAGS
        [Flags]
        public enum AccountFlag
        {
            None = 0x0,
            Banned = 0x01,
            DoNotSendEmail = 0x02,
            Watched = 0x04,
            // because machine info comes after character creation, we need to set this to kick them later
            //  Subsequent logins will be blocked, but initial creation will otherwise slip through.
            //  See Cron jobs for the kicker.
            ExceedsMachineInfoLimit = 0x08,
            FastWalk = 0x10,
            DenyAccessPublicContainer = 0x20,
            InvalidClient = 0x40,
            InvalidRazor = 0x80,
        }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public AccountFlag Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }
        public void SetFlag(AccountFlag flag, bool value)
        {
            if (value)
                m_Flags |= flag;
            else
                m_Flags &= ~flag;
        }

        public bool GetFlag(AccountFlag flag)
        {
            return ((m_Flags & flag) != 0);
        }
        #endregion FLAGS
        /// <summary>
        /// Adds a new tag to this account. This method does not check for duplicate names.
        /// </summary>
        /// <param name="name">New tag name.</param>
        /// <param name="value">New tag value.</param>
        public void AddTag(string name, string value)
        {
            m_Tags.Add(new AccountTag(name, value));
        }

        /// <summary>
        /// Removes all tags with the specified name from this account.
        /// </summary>
        /// <param name="name">Tag name to remove.</param>
        public void RemoveTag(string name)
        {
            for (int i = m_Tags.Count - 1; i >= 0; --i)
            {
                if (i >= m_Tags.Count)
                    continue;

                AccountTag tag = (AccountTag)m_Tags[i];

                if (tag.Name == name)
                    m_Tags.RemoveAt(i);
            }
        }

        /// <summary>
        /// Modifies an existing tag or adds a new tag if no tag exists.
        /// </summary>
        /// <param name="name">Tag name.</param>
        /// <param name="value">Tag value.</param>
        public void SetTag(string name, string value)
        {
            for (int i = 0; i < m_Tags.Count; ++i)
            {
                AccountTag tag = (AccountTag)m_Tags[i];

                if (tag.Name == name)
                {
                    tag.Value = value;
                    return;
                }
            }

            AddTag(name, value);
        }

        /// <summary>
        /// Gets the value of a tag -or- null if there are no tags with the specified name.
        /// </summary>
        /// <param name="name">Name of the desired tag value.</param>
        public string GetTag(string name)
        {
            for (int i = 0; i < m_Tags.Count; ++i)
            {
                AccountTag tag = (AccountTag)m_Tags[i];

                if (tag.Name == name)
                    return tag.Value;
            }

            return null;
        }

        public void SetUnspecifiedBan(Mobile from)
        {
            SetBanTags(from, DateTime.MinValue, TimeSpan.Zero);
        }

        public void SetBanTags(Mobile from, DateTime banTime, TimeSpan banDuration)
        {
            if (from == null)
                RemoveTag("BanDealer");
            else
                SetTag("BanDealer", from.ToString());

            if (banTime == DateTime.MinValue)
                RemoveTag("BanTime");
            else
                SetTag("BanTime", XmlConvert.ToString(banTime, XmlDateTimeSerializationMode.Utc));

            if (banDuration == TimeSpan.Zero)
                RemoveTag("BanDuration");
            else
                SetTag("BanDuration", banDuration.ToString());
        }

        public bool GetBanTags(out DateTime banTime, out TimeSpan banDuration)
        {
            string tagTime = GetTag("BanTime");
            string tagDuration = GetTag("BanDuration");

            if (tagTime != null)
                banTime = Accounts.GetDateTime(tagTime, DateTime.MinValue);
            else
                banTime = DateTime.MinValue;

            if (tagDuration == "Infinite")
            {
                banDuration = TimeSpan.MaxValue;
            }
            else if (tagDuration != null)
            {
                try { banDuration = TimeSpan.Parse(tagDuration); }
                catch { banDuration = TimeSpan.Zero; }
            }
            else
            {
                banDuration = TimeSpan.Zero;
            }

            return (banTime != DateTime.MinValue && banDuration != TimeSpan.Zero);
        }

        public AccessLevel GetAccessLevel()
        {
            bool online;
            AccessLevel accessLevel;
            return GetAccountInfo(out accessLevel, out online);
        }

        public AccessLevel GetAccountInfo(out AccessLevel accessLevel, out bool online)
        {
            accessLevel = this.AccessLevel;
            online = false;

            for (int j = 0; j < 5; ++j)
            {
                Mobile check = this[j];

                if (check == null)
                    continue;

                if (check.AccessLevel > accessLevel)
                    accessLevel = check.AccessLevel;

                if (check.NetState != null)
                    online = true;
            }

            return accessLevel;
        }

        /// <summary>
        /// Pull account credentials from the accounts DB.
        /// </summary>
        public void SyncCredentials()
        {
            AccountsDBEntry? dbAccount = AccountsDatabase.GetAccount(m_Username);

            if (dbAccount != null)
                SetRawCredentials(dbAccount.Value.CryptPassword, dbAccount.Value.PlainPassword, dbAccount.Value.ResetPassword);
        }

        /// <summary>
        /// Gets the account's raw credentials as out parameters.
        /// Note that calling this method does not trigger a resync of the account credentials via <see cref="SyncCredentials"/>.
        /// </summary>
        /// <returns></returns>
        public void GetRawCredentials(out string cryptPassword, out string plainPassword, out string resetPassword)
        {
            cryptPassword = m_CryptPassword;
            plainPassword = m_PlainPassword;
            resetPassword = m_ResetPassword;
        }

        public void SetRawCredentials(string cryptPassword, string plainPassword, string resetPassword)
        {
            m_CryptPassword = cryptPassword;
            m_PlainPassword = plainPassword;
            m_ResetPassword = resetPassword;
        }

        private static MD5CryptoServiceProvider m_HashProvider;
        private static byte[] m_HashBuffer;

        public static string HashPassword(string plainPassword)
        {
            if (m_HashProvider == null)
                m_HashProvider = new MD5CryptoServiceProvider();

            if (m_HashBuffer == null)
                m_HashBuffer = new byte[256];

            int length = Encoding.ASCII.GetBytes(plainPassword, 0, plainPassword.Length > 256 ? 256 : plainPassword.Length, m_HashBuffer, 0);
            byte[] hashed = m_HashProvider.ComputeHash(m_HashBuffer, 0, length);

            return BitConverter.ToString(hashed);
        }

        public void SetPassword(string plainPassword)
        {
            SetPassword(plainPassword, false);
        }

        public void SetPassword(string plainPassword, bool saveDbAccount)
        {
            SyncCredentials();

            if (AccountHandler.ProtectPasswords)
            {
                m_CryptPassword = HashPassword(plainPassword);
                m_PlainPassword = null;
            }
            else
            {
                m_PlainPassword = plainPassword;
                m_CryptPassword = null;
            }

            if (saveDbAccount && Core.UseLoginDB)
                AccountsDatabase.SaveAccount(this);
        }

        public void SetResetPassword(string resetPassword)
        {
            SyncCredentials();

            m_ResetPasswordRequestedTime = DateTime.UtcNow;
            m_ResetPassword = resetPassword;

            if (Core.UseLoginDB)
                AccountsDatabase.SaveAccount(this);
        }



        public bool CheckPassword(string plainPassword)
        {
            // always reject null, empty or whitespace passwords
            if (String.IsNullOrWhiteSpace(plainPassword))
                return false;

            SyncCredentials();

            bool passwordValid = false;
            bool saveDbAccount = false;

            if (m_PlainPassword == plainPassword)
            {
                passwordValid = true;

                if (m_ResetPassword != null)
                {
                    saveDbAccount = true;

                    m_ResetPassword = null;
                }
            }
            else if (m_CryptPassword == HashPassword(plainPassword))
            {
                passwordValid = true;

                if (m_ResetPassword != null)
                {
                    saveDbAccount = true;

                    m_ResetPassword = null;
                }
            }
            else if (m_ResetPassword == plainPassword)
            {
                passwordValid = true;
                saveDbAccount = true;

                if (AccountHandler.ProtectPasswords)
                {
                    m_CryptPassword = HashPassword(plainPassword);
                    m_PlainPassword = null;
                }
                else
                {
                    m_PlainPassword = plainPassword;
                    m_CryptPassword = null;
                }

                m_ResetPassword = null;
            }

            if (saveDbAccount && Core.UseLoginDB)
                AccountsDatabase.SaveAccount(this);

            return passwordValid;
        }

        /// <summary>
        /// Constructs a new Account instance with a specific username and password. Intended to be only called from Accounts.AddAccount.
        /// </summary>
        /// <param name="username">Initial username for this account.</param>
        /// <param name="password">Initial password for this account.</param>
        public Account(string username, string password)
            : this(username)
        {
            SetPassword(password, true);
        }

        public Account(string username)
        {
            m_Username = username;

            m_AccessLevel = AccessLevel.Player;

            m_Created = m_LastLogin = DateTime.UtcNow;

            m_Comments = new ArrayList();
            m_Tags = new ArrayList();

            m_Mobiles = new Mobile[5];

            m_IPRestrictions = new string[0];
            m_EmailHistory = new string[0];
            m_LoginIPs = new IPAddress[0];
            m_bAccountActivated = false;
            m_ActivationKey = "";
        }

        /// <summary>
        /// Deserializes an Account instance from an xml element. Intended only to be called from Accounts.Load.
        /// </summary>
        /// <param name="node">The XmlElement instance from which to deserialize.</param>
        public Account(XmlElement node)
        {
            m_Username = Accounts.GetText(node["username"], "empty");

            string plainPassword = Accounts.GetText(node["password"], null);
            string cryptPassword = Accounts.GetText(node["cryptPassword"], null);

            if (AccountHandler.ProtectPasswords)
            {
                if (cryptPassword != null)
                    m_CryptPassword = cryptPassword;
                else if (plainPassword != null)
                    SetPassword(plainPassword);
                else
                    SetPassword("empty");
            }
            else
            {
                if (plainPassword == null)
                    plainPassword = "empty";

                SetPassword(plainPassword);
            }
            #region MORTALIS
            m_BankBox = (Serial)Accounts.GetInt32(Accounts.GetText(node["bankBox"], "0"), 0);
            m_House = (Serial)Accounts.GetInt32(Accounts.GetText(node["house"], "0"), 0);
            m_TimeOfDeath = Accounts.GetDateTime(Accounts.GetText(node["timeOfDeath"], null), DateTime.MinValue);
            m_Followers = Accounts.GetInt32(Accounts.GetText(node["followers"], "0"), 0);
            m_guid = (UInt32)Accounts.GetUInt32(Accounts.GetText(node["guid"], "0"), 0);
            #endregion MORTALIS
            m_AccountInfraction = (AccountInfraction)Enum.Parse(typeof(AccountInfraction), Accounts.GetText(node["accountinfraction"], "none"), true);
            m_IPFlagsPermanent = (IPFlags)Accounts.GetUInt32(Accounts.GetText(node["IPFlags"], "0"), 0);
            m_HardwareHashAcquired = Accounts.GetDateTime(Accounts.GetText(node["hardwareHashAcquired"], null), DateTime.MinValue);
            m_AccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), Accounts.GetText(node["accessLevel"], "Player"), true);
            m_Flags = (AccountFlag)Accounts.GetInt32(Accounts.GetText(node["flags"], "0"), 0);
            m_Created = Accounts.GetDateTime(Accounts.GetText(node["created"], null), DateTime.UtcNow);
            m_LastLogin = Accounts.GetDateTime(Accounts.GetText(node["lastLogin"], null), DateTime.UtcNow);

            m_EmailAddress = Accounts.GetText(node["email"], "empty");

            m_WatchReason = Accounts.GetText(node["watchreason"], "");
            m_WatchExpire = Accounts.GetDateTime(Accounts.GetText(node["watchexpiredate"], null), DateTime.MinValue);

            m_HardwareHash = Accounts.GetInt32(Accounts.GetText(node["HardwareHash"], "0"), 0);

            m_Mobiles = LoadMobiles(node);
            m_Comments = LoadComments(node);
            m_Tags = LoadTags(node);
            m_LoginIPs = LoadAddressList(node);
            m_Machines = LoadMachineList(node);
            m_IPRestrictions = LoadAccessCheck(node);
            m_EmailHistory = LoadEmailHistory(node);

            m_bAccountActivated = Accounts.GetBool(node["accountactivated"], false);
            m_ActivationKey = Accounts.GetText(node["activationkey"], "");
            m_ResetPassword = Accounts.GetText(node["resetpassword"], null);
            m_ResetPasswordRequestedTime = Accounts.GetDateTime(Accounts.GetText(node["resetpwdtime"], null), DateTime.MinValue);

            for (int i = 0; i < m_Mobiles.Length; ++i)
            {
                if (m_Mobiles[i] != null)
                    m_Mobiles[i].Account = this;
            }

            //Pix added 2010.11.19
            try
            {   // Explicitly handle the null entry error
                string tip = Accounts.GetText(node["lastgameloginip"], null);
                if (tip != null)
                    if (IPAddress.TryParse(tip, out m_LastGAMELogin) == false)
                        Console.WriteLine("Error: while parsing LastGameLoginIP");
            }
            catch
            {
                Console.WriteLine("Error: Caught exception while loading LastGAMELoginIP");
            }
        }

        /// <summary>
        /// Deserializes a list of string values from an xml element. Null values are not added to the list.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>String list. Value will never be null.</returns>
        public static string[] LoadAccessCheck(XmlElement node)
        {
            string[] stringList;
            XmlElement accessCheck = node["accessCheck"];

            if (accessCheck != null)
            {
                ArrayList list = new ArrayList();

                foreach (XmlElement ip in accessCheck.GetElementsByTagName("ip"))
                {
                    string text = Accounts.GetText(ip, null);

                    if (text != null)
                        list.Add(text);
                }

                stringList = (string[])list.ToArray(typeof(string));
            }
            else
            {
                stringList = new string[0];
            }

            return stringList;
        }

        public static string[] LoadEmailHistory(XmlElement node)
        {
            string[] stringList;
            XmlElement emailHistory = node["emailHistory"];

            if (emailHistory != null)
            {
                ArrayList list = new ArrayList();

                foreach (XmlElement address in emailHistory.GetElementsByTagName("addr"))
                {
                    string text = Accounts.GetText(address, null);

                    if (text != null)
                        list.Add(text);
                }

                stringList = (string[])list.ToArray(typeof(string));
            }
            else
            {
                stringList = new string[0];
            }

            return stringList;
        }


        /// <summary>
        /// Deserializes a list of IPAddress values from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Address list. Value will never be null.</returns>
        public static IPAddress[] LoadAddressList(XmlElement node)
        {
            IPAddress[] list;
            XmlElement addressList = node["addressList"];

            if (addressList != null)
            {
                int count = Accounts.GetInt32(Accounts.GetAttribute(addressList, "count", "0"), 0);

                list = new IPAddress[count];

                count = 0;

                foreach (XmlElement ip in addressList.GetElementsByTagName("ip"))
                {
                    try
                    {
                        if (count < list.Length)
                        {
                            list[count] = IPAddress.Parse(Accounts.GetText(ip, null));
                            count++;
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }

                if (count != list.Length)
                {
                    IPAddress[] old = list;
                    list = new IPAddress[count];

                    for (int i = 0; i < count && i < old.Length; ++i)
                        list[i] = old[i];
                }
            }
            else
            {
                list = new IPAddress[0];
            }

            return list;
        }

        public static List<int> LoadMachineList(XmlElement node)
        {
            List<int> list = new();
            XmlElement machineList = node["machineList"];

            if (machineList != null)
            {
                foreach (XmlElement hash in machineList.GetElementsByTagName("hash"))
                {
                    try
                    {
                        list.Add(int.Parse(Accounts.GetText(hash, null)));
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }

        /// <summary>
        /// Deserializes a list of AccountTag instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Tag list. Value will never be null.</returns>
        public static ArrayList LoadTags(XmlElement node)
        {
            ArrayList list = new ArrayList();
            XmlElement tags = node["tags"];

            if (tags != null)
            {
                foreach (XmlElement tag in tags.GetElementsByTagName("tag"))
                {
                    try { list.Add(new AccountTag(tag)); }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }

        /// <summary>
        /// Deserializes a list of AccountComment instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Comment list. Value will never be null.</returns>
        public static ArrayList LoadComments(XmlElement node)
        {
            ArrayList list = new ArrayList();
            XmlElement comments = node["comments"];

            if (comments != null)
            {
                foreach (XmlElement comment in comments.GetElementsByTagName("comment"))
                {
                    try { list.Add(new AccountComment(comment)); }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }

        /// <summary>
        /// Deserializes a list of Mobile instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement instance from which to deserialize.</param>
        /// <returns>Mobile list. Value will never be null.</returns>
        public static Mobile[] LoadMobiles(XmlElement node)
        {
            Mobile[] list;
            XmlElement chars = node["chars"];

            if (chars == null)
            {
                list = new Mobile[5];
            }
            else
            {
                int length = Accounts.GetInt32(Accounts.GetAttribute(chars, "length", "5"), 5);

                list = new Mobile[length];

                foreach (XmlElement ele in chars.GetElementsByTagName("char"))
                {
                    try
                    {
                        int index = Accounts.GetInt32(Accounts.GetAttribute(ele, "index", "0"), 0);
                        int serial = Accounts.GetInt32(Accounts.GetText(ele, "0"), 0);

                        if (index >= 0 && index < list.Length)
                            list[index] = World.FindMobile(serial);
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }

        /// <summary>
        /// Checks if a specific NetState is allowed access to this account.
        /// </summary>
        /// <param name="ns">NetState instance to check.</param>
        /// <returns>True if allowed, false if not.</returns>
        public bool HasAccess(NetState ns)
        {
            if (ns == null)
                return false;

            AccessLevel level = Misc.AccountHandler.LockdownLevel;

            if (level > AccessLevel.Player)
            {
                bool hasAccess = false;

                if (m_AccessLevel >= level)
                {
                    hasAccess = true;
                }
                else
                {
                    for (int i = 0; !hasAccess && i < 5; ++i)
                    {
                        Mobile m = this[i];

                        if (m != null && m.AccessLevel >= level)
                            hasAccess = true;
                    }
                }

                if (!hasAccess)
                    return false;
            }

            IPAddress ipAddress;

            try { ipAddress = ((IPEndPoint)ns.Socket.RemoteEndPoint).Address; }
            catch { return false; }

            bool accessAllowed = (m_IPRestrictions.Length == 0);

            for (int i = 0; !accessAllowed && i < m_IPRestrictions.Length; ++i)
                accessAllowed = Utility.IPMatch(m_IPRestrictions[i], ipAddress);

            return accessAllowed;
        }

        /// <summary>
        /// The purpose of this is to log ONLY the game login, not the account login or anything else
        /// </summary>
        /// <param name="ns"></param>
        public void LogGAMELogin(NetState ns)
        {
            if (ns == null)
                return;

            IPAddress ipAddress;

            try { ipAddress = ((IPEndPoint)ns.Socket.RemoteEndPoint).Address; }
            catch { return; }

            LastGAMELogin = ipAddress;
        }

        /// <summary>
        /// For use in admin console to clear the game login IP
        /// </summary>
        public void ClearGAMELogin()
        {
            LastGAMELogin = null;
        }

        /// <summary>
        /// Records the IP address of 'ns' in its 'LoginIPs' list.
        /// </summary>
        /// <param name="ns">NetState instance to record.</param>
        public void LogAccess(NetState ns)
        {
            if (ns == null)
                return;

            IPAddress ipAddress;

            try { ipAddress = ((IPEndPoint)ns.Socket.RemoteEndPoint).Address; }
            catch { return; }

            bool contains = false;
            int containsAt = 0;

            for (int i = 0; !contains && i < m_LoginIPs.Length; ++i)
            {
                contains = m_LoginIPs[i].Equals(ipAddress);
                if (contains)
                {
                    containsAt = i;
                }
            }

            //			if ( contains )
            //				return;

            //PIX: now we have the IP list be in the order that the account was accessed
            IPAddress[] old = m_LoginIPs;

            if (contains)
            {
                m_LoginIPs = new IPAddress[old.Length];

                //Add current IP to beginning of list
                m_LoginIPs[0] = ipAddress;

                int j = 1;
                for (int i = 0; i < old.Length; ++i)
                {
                    if (i == containsAt)
                    {
                        //skip
                    }
                    else
                    {
                        m_LoginIPs[j] = old[i];
                        j++;
                    }
                }
            }
            else
            {
                m_LoginIPs = new IPAddress[old.Length + 1];

                //Add new IP to beginning of list
                m_LoginIPs[0] = ipAddress;

                for (int i = 0; i < old.Length; ++i)
                    m_LoginIPs[i + 1] = old[i];
            }

        }

        /// <summary>
        /// Checks if a specific NetState is allowed access to this account. If true, the NetState IPAddress is added to the address list.
        /// </summary>
        /// <param name="ns">NetState instance to check.</param>
        /// <returns>True if allowed, false if not.</returns>
        public bool CheckAccess(NetState ns)
        {
            if (!HasAccess(ns))
                return false;

            LogAccess(ns);
            return true;
        }

        /// <summary>
        /// Serializes this Account instance to an XmlTextWriter.
        /// </summary>
        /// <param name="xml">The XmlTextWriter instance from which to serialize.</param>
        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("account");

            xml.WriteStartElement("username");
            xml.WriteString(m_Username);
            xml.WriteEndElement();
            #region MORTALIS
            if (m_Followers != 0)
            {
                xml.WriteStartElement("followers");
                xml.WriteString(XmlConvert.ToString(m_Followers));
                xml.WriteEndElement();
            }
            if (m_guid != 0)
            {
                xml.WriteStartElement("guid");
                xml.WriteString(XmlConvert.ToString(m_guid));
                xml.WriteEndElement();
            }
            if (m_House != Serial.Zero)
            {
                xml.WriteStartElement("house");
                xml.WriteString(XmlConvert.ToString(m_House));
                xml.WriteEndElement();
            }

            if (m_BankBox != Serial.Zero)
            {
                xml.WriteStartElement("bankBox");
                xml.WriteString(XmlConvert.ToString(m_BankBox));
                xml.WriteEndElement();
            }
            if (m_TimeOfDeath != DateTime.MinValue)
            {
                xml.WriteStartElement("timeOfDeath");
                xml.WriteString(XmlConvert.ToString(m_TimeOfDeath, XmlDateTimeSerializationMode.Utc));
                xml.WriteEndElement();
            }
            #endregion MORTALIS
            if (m_HardwareHashAcquired != DateTime.MinValue)
            {
                xml.WriteStartElement("hardwareHashAcquired");
                xml.WriteString(XmlConvert.ToString(m_HardwareHashAcquired, XmlDateTimeSerializationMode.Utc));
                xml.WriteEndElement();
            }
            if (m_HardwareHash != 0)
            {
                xml.WriteStartElement("HardwareHash");
                xml.WriteString(XmlConvert.ToString(m_HardwareHash));
                xml.WriteEndElement();
            }
            if (m_PlainPassword != null)
            {
                xml.WriteStartElement("password");
                xml.WriteString(m_PlainPassword);
                xml.WriteEndElement();
            }

            if (m_CryptPassword != null)
            {
                xml.WriteStartElement("cryptPassword");
                xml.WriteString(m_CryptPassword);
                xml.WriteEndElement();
            }

            xml.WriteStartElement("email");
            xml.WriteString(m_EmailAddress);
            xml.WriteEndElement();

            if (Watched)
            {
                xml.WriteStartElement("watchreason");
                xml.WriteString(m_WatchReason);
                xml.WriteEndElement();

                xml.WriteStartElement("watchexpiredate");
                xml.WriteString(XmlConvert.ToString(m_WatchExpire, XmlDateTimeSerializationMode.Utc));
                xml.WriteEndElement();
            }

            xml.WriteStartElement("accountactivated");
            xml.WriteString((m_bAccountActivated ? "true" : "false"));
            xml.WriteEndElement();

            xml.WriteStartElement("activationkey");
            xml.WriteString(m_ActivationKey);
            xml.WriteEndElement();

            xml.WriteStartElement("resetpassword");
            xml.WriteString(m_ResetPassword);
            xml.WriteEndElement();

            xml.WriteStartElement("resetpwdtime");
            xml.WriteString(XmlConvert.ToString(m_ResetPasswordRequestedTime, XmlDateTimeSerializationMode.Utc));
            xml.WriteEndElement();

            if (m_AccountInfraction != AccountInfraction.none)
            {
                xml.WriteStartElement("accountinfraction");
                xml.WriteString(m_AccountInfraction.ToString());
                xml.WriteEndElement();
            }
            if (m_IPFlagsPermanent != IPFlags.None)
            {
                xml.WriteStartElement("IPFlags");
                xml.WriteString(((int)m_IPFlagsPermanent).ToString());
                xml.WriteEndElement();
            }
            if (m_AccessLevel != AccessLevel.Player)
            {
                xml.WriteStartElement("accessLevel");
                xml.WriteString(m_AccessLevel.ToString());
                xml.WriteEndElement();
            }

            if (m_Flags != 0)
            {
                xml.WriteStartElement("flags");
                xml.WriteString(XmlConvert.ToString((int)m_Flags));
                xml.WriteEndElement();
            }

            xml.WriteStartElement("created");
            xml.WriteString(XmlConvert.ToString(m_Created, XmlDateTimeSerializationMode.Utc));
            xml.WriteEndElement();

            xml.WriteStartElement("lastLogin");
            xml.WriteString(XmlConvert.ToString(m_LastLogin, XmlDateTimeSerializationMode.Utc));
            xml.WriteEndElement();

            xml.WriteStartElement("chars");

            xml.WriteAttributeString("length", m_Mobiles.Length.ToString());

            for (int i = 0; i < m_Mobiles.Length; ++i)
            {
                Mobile m = m_Mobiles[i];

                if (m != null && !m.Deleted)
                {
                    xml.WriteStartElement("char");
                    xml.WriteAttributeString("index", i.ToString());
                    xml.WriteString(m.Serial.Value.ToString());
                    xml.WriteEndElement();
                }
            }

            xml.WriteEndElement();

            if (m_Comments.Count > 0)
            {
                xml.WriteStartElement("comments");

                for (int i = 0; i < m_Comments.Count; ++i)
                    ((AccountComment)m_Comments[i]).Save(xml);

                xml.WriteEndElement();
            }

            if (m_Tags.Count > 0)
            {
                xml.WriteStartElement("tags");

                for (int i = 0; i < m_Tags.Count; ++i)
                    ((AccountTag)m_Tags[i]).Save(xml);

                xml.WriteEndElement();
            }

            if (m_LoginIPs.Length > 0)
            {
                xml.WriteStartElement("addressList");

                int maxcount = 32;  // max we care about
                // array is already sorted, most recent first. Take the max from the list
                List<IPAddress> loginIPs = m_LoginIPs.ToList().Take(maxcount).ToList();
                xml.WriteAttributeString("count", m_LoginIPs.Length.ToString());
                for (int i = 0; i < loginIPs.Count; ++i)
                {
                    xml.WriteStartElement("ip");
                    xml.WriteString(loginIPs[i].ToString());
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }

            if (m_Machines.Count > 0)
            {
                xml.WriteStartElement("machineList");
                int maxcount = 16;   // max we care about
                // get the last ones added, most recent
                m_Machines = m_Machines.Skip(Math.Max(0, m_Machines.Count() - maxcount)).ToList();
                xml.WriteAttributeString("count", maxcount.ToString());
                for (int i = 0; i < m_Machines.Count; ++i)
                {
                    xml.WriteStartElement("hash");
                    xml.WriteString(m_Machines[i].ToString());
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }

            if (m_IPRestrictions.Length > 0)
            {
                xml.WriteStartElement("accessCheck");

                for (int i = 0; i < m_IPRestrictions.Length; ++i)
                {
                    xml.WriteStartElement("ip");
                    xml.WriteString(m_IPRestrictions[i]);
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }

            if (m_EmailHistory.Length > 0)
            {
                xml.WriteStartElement("emailHistory");

                for (int i = 0; i < m_EmailHistory.Length; i++)
                {
                    xml.WriteStartElement("addr");
                    xml.WriteString(m_EmailHistory[i]);
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }

            if (m_LastGAMELogin != null && !m_LastGAMELogin.Equals(IPAddress.None))
            {
                xml.WriteStartElement("lastgameloginip");
                xml.WriteString(m_LastGAMELogin.ToString());
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
        }

        /// <summary>
        /// Gets the current number of characters on this account.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int Count
        {
            get
            {
                int count = 0;

                for (int i = 0; i < this.Length; ++i)
                {
                    if (this[i] != null)
                        ++count;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the maximum amount of characters allowed to be created on this account. Values other than 1, 5, or 6 are not supported.
        /// </summary>
        [CommandProperty(AccessLevel.Counselor)]
        public int Limit
        {
            get
            {
                // 1 character per account on Siege, Mortalis!
                if (Core.RuleSets.SiegeRules() || Core.RuleSets.MortalisRules())
                {
                    return 1;
                }

                return 5;
            }
        }

        /// <summary>
        /// Gets the maxmimum amount of characters that this account can hold.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int Length
        {
            get { return m_Mobiles.Length; }
        }

        /// <summary>
        /// Gets or sets the character at a specified index for this account. Out of bound index values are handled; null returned for get, ignored for set.
        /// </summary>
        public Mobile this[int index]
        {
            get
            {
                if (index >= 0 && index < m_Mobiles.Length)
                {
                    Mobile m = m_Mobiles[index];

                    if (m != null && m.Deleted)
                    {
                        m.Account = null;
                        m_Mobiles[index] = m = null;
                    }

                    return m;
                }

                return null;
            }
            set
            {
                if (index >= 0 && index < m_Mobiles.Length)
                {
                    if (m_Mobiles[index] != null)
                        m_Mobiles[index].Account = null;

                    m_Mobiles[index] = value;

                    if (m_Mobiles[index] != null)
                        m_Mobiles[index].Account = this;
                }
            }
        }

        public override string ToString()
        {
            return m_Username;
        }
    }
}