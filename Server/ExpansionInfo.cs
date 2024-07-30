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

/* CHANGELOG
 * Server\ExpansionInfo.cs
 * 11/27/22, Adam
 *      RunUO used the 'table order' of these expansions to determine Expansion Features, i.e., does Expansion Y Support X, is table[UOR] >= table[ML]?
 *      This model breaks down when a shard administrator tries to add an expansion. At first you may try to reorder the list to fit your new expansion.
 *      Instead, we added 'Publish Date' to act as a better measure of comparison. The comparisons are done in Main.cs. See CheckExpansion()
 */

using System;

namespace Server
{
#if true
    public enum Expansion
    {
        None,
        T2A,    // Released on October 24, 1998, The Second Age (T2A) was UO's first expansion.
        UOR,    // Released on May 4, 2000, Renaissance (UOR) was UO's second expansion. It completely changed the way the game functioned with the introduction of Trammel.
        UOTD,   // Released on March 27, 2001, Third Dawn changed the way player saw UO by adding the 3D client
        LBR,    // Released on February 24, 2002, Lord Blackthorn's Revenge was the forth expansion and the first to come with no land/dungeon expansion.
        AOS,    // Release on February 11, 2003, Age of Shadows is considered the most impactful expansion in UO history. I
        SE,     // Released on November 2, 2004, Samurai Empire was considered a modest expansion compared with its predecessor.
        ML,     // Released on August 30, 2005, Mondain's Legacy was the developers attempted to get back to UO's roots by focusing on current content. 
        /*KR*/    // Released in 2007, Kingdom Reborn was a graphic and user interface upgrade, an attempt to offer a modern MMORPG client that could also run on lower-end computers.
        SA,     // Stygian Abyss was released in September 2009 and features a new playable Gargoyle Race as well as the Stygian Abyss itself
        HS,      // Released in October 2010, Sosaria was beset with pirates, cannon, new ships and even a floating village. 
        // custom
        AI,
        SP,
        REN
    }

    [Flags]
    public enum ClientFlags
    {
        None = 0x00000000,
        Felucca = 0x00000001,
        Trammel = 0x00000002,
        Ilshenar = 0x00000004,
        Malas = 0x00000008,
        Tokuno = 0x00000010,
        TerMur = 0x00000020,
        Unk1 = 0x00000040,
        Unk2 = 0x00000080,
        UOTD = 0x00000100
    }

    [Flags]
    public enum FeatureFlags
    {
        None = 0x00000000,
        T2A = 0x00000001,
        UOR = 0x00000002,
        UOTD = 0x00000004,
        LBR = 0x00000008,
        AOS = 0x00000010,
        SixthCharacterSlot = 0x00000020,
        SE = 0x00000040,
        ML = 0x00000080,
        EigthAge = 0x00000100,
        NinthAge = 0x00000200, /* Crystal/Shadow Custom House Tiles */
        TenthAge = 0x00000400,
        IncreasedStorage = 0x00000800, /* Increased Housing/Bank Storage */
        SeventhCharacterSlot = 0x00001000,
        RoleplayFaces = 0x00002000,
        TrialAccount = 0x00004000,
        LiveAccount = 0x00008000,
        SA = 0x00010000,
        HS = 0x00020000,
        Gothic = 0x00040000,
        Rustic = 0x00080000,

        ExpansionNone = None,
        ExpansionT2A = T2A,
        ExpansionUOR = ExpansionT2A | UOR,
        ExpansionUOTD = ExpansionUOR | UOTD,
        ExpansionLBR = ExpansionUOTD | LBR,
        ExpansionAOS = ExpansionLBR | AOS | LiveAccount,
        ExpansionSE = ExpansionAOS | SE,
        ExpansionML = ExpansionSE | ML | NinthAge,
        ExpansionSA = ExpansionML | SA | Gothic | Rustic,
        ExpansionHS = ExpansionSA | HS,
        // custom
        ExpansionAI = ExpansionLBR,
        ExpansionSP = ExpansionT2A,
        //ExpansionT2A | HS,        // old ancient wyrm
        //ExpansionT2A | SE,        // old ancient wyrm
        //ExpansionT2A | AOS,       // old ancient wyrm
        //ExpansionT2A | LBR,       // new ancient wyrm
        //ExpansionT2A | UOTD,      // old ancient wyrm         
        //ExpansionUOR,             // new ancient wyrm
        //ExpansionT2A,             // old ancient wyrm
        //ExpansionNone,            // old ancient wyrm
        //ExpansionUOTD,            // new ancient wyrm
        //ExpansionT2A | UOR,       // new ancient wyrm
        //UOTD,//UOR,//ExpansionT2A,// old ancient wyrm
        //ExpansionAI,              // new ancient wyrm
        ExpansionREN = ExpansionAI,
    }

    [Flags]
    public enum CharacterListFlags
    {
        None = 0x00000000,
        Unk1 = 0x00000001,
        OverwriteConfigButton = 0x00000002,
        OneCharacterSlot = 0x00000004,
        ContextMenus = 0x00000008,
        SlotLimit = 0x00000010,
        AOS = 0x00000020,
        SixthCharacterSlot = 0x00000040,
        SE = 0x00000080,
        ML = 0x00000100,
        Unk2 = 0x00000200,
        UO3DClientType = 0x00000400,
        Unk3 = 0x00000800,
        SeventhCharacterSlot = 0x00001000,
        Unk4 = 0x00002000,
        NewMovementSystem = 0x00004000,
        NewFeluccaAreas = 0x00008000,

        ExpansionNone = ContextMenus, //
        ExpansionT2A = ContextMenus, //
        ExpansionUOR = ContextMenus, // None (unused by AI Renaissance, we have our own version)
        ExpansionUOTD = ContextMenus, //
        ExpansionLBR = ContextMenus, //
        ExpansionAOS = ContextMenus | AOS,
        ExpansionSE = ExpansionAOS | SE,
        ExpansionML = ExpansionSE | ML,
        ExpansionSA = ExpansionML,
        ExpansionHS = ExpansionSA,
        // custom
        // we may want to revisit these definitions.
        ExpansionAI = ExpansionLBR,
        ExpansionSP = (ExpansionT2A & ~ContextMenus) | OneCharacterSlot,
        ExpansionREN = ExpansionAI & ~ContextMenus,
    }

    public class ExpansionInfo
    {
        public static ExpansionInfo[] Table { get { return m_Table; } }
        private static ExpansionInfo[] m_Table = new ExpansionInfo[]
        {


            new ExpansionInfo( 0, "None",new DateTime(1999, 1, 19),                     ClientFlags.None,
                FeatureFlags.ExpansionNone, CharacterListFlags.ExpansionNone,       0x0000 ),
            new ExpansionInfo( 1, "The Second Age",new DateTime(1998, 10, 1),           ClientFlags.Felucca,
                FeatureFlags.ExpansionT2A,  CharacterListFlags.ExpansionT2A,        0x0000 ),
            // note, we don't use this Renaissance, but instead the AI version below, #12
            new ExpansionInfo( 2, "Renaissance_unused",new DateTime(2000, 5, 4),        ClientFlags.Trammel,
                FeatureFlags.ExpansionUOR,  CharacterListFlags.ExpansionUOR,        0x0000 ),
            new ExpansionInfo( 3, "Third Dawn",new DateTime(2001, 3, 7),                ClientFlags.Ilshenar,
                FeatureFlags.ExpansionUOTD, CharacterListFlags.ExpansionUOTD,       0x0000 ),
            new ExpansionInfo( 4, "Blackthorn's Revenge",new DateTime(2002, 2, 24),     ClientFlags.Ilshenar,
                FeatureFlags.ExpansionLBR,  CharacterListFlags.ExpansionLBR,        0x0000 ),
            new ExpansionInfo( 5, "Age of Shadows",new DateTime(2003, 2, 11),           ClientFlags.Malas,
                FeatureFlags.ExpansionAOS,  CharacterListFlags.ExpansionAOS,        0x0000 ),
            new ExpansionInfo( 6, "Samurai Empire",new DateTime(2004, 11, 2),           ClientFlags.Tokuno,
                FeatureFlags.ExpansionSE,   CharacterListFlags.ExpansionSE,         0x00C0 ), // 0x20 | 0x80
            new ExpansionInfo( 7, "Mondain's Legacy",new DateTime(2005, 8, 30),       new ClientVersion( "5.0.0a" ),
                FeatureFlags.ExpansionML,   CharacterListFlags.ExpansionML,         0x02C0 ), // 0x20 | 0x80 | 0x200
            new ExpansionInfo( 8, "Stygian Abyss",new DateTime(2009, 9, 9),            ClientFlags.TerMur,
                FeatureFlags.ExpansionSA,   CharacterListFlags.ExpansionSA,         0xD02C0 ), // 0x20 | 0x80 | 0x200 | 0x10000 | 0x40000 | 0x80000
            new ExpansionInfo( 9, "High Seas",new DateTime(2010, 10, 12),              new ClientVersion( "7.0.9.0" ),
                FeatureFlags.ExpansionHS,   CharacterListFlags.ExpansionHS,         0xD02C0 ), // 0x20 | 0x80 | 0x200 | 0x10000 | 0x40000 | 0x80000
            // custom expansions below this line: RunUO uses the order of these to determine Expansion Features, i.e., does Expansion Support X
            //  We added Publish Date to act as a better measure of comparison. Comparisons contained in Core. I.e., Core.ML)
            new ExpansionInfo( 10, "Angel Island", new DateTime(2002, 1, 9),             ClientFlags.Felucca,
                FeatureFlags.ExpansionAI,   CharacterListFlags.ExpansionAI,         0x2E0 ),
            new ExpansionInfo( 11, "Siege Perilous",new DateTime(2000, 5, 4),            ClientFlags.Felucca,
                FeatureFlags.ExpansionSP,   CharacterListFlags.ExpansionSP,         0x2E0 ),
            new ExpansionInfo( 12, "Renaissance",new DateTime(2000, 5, 4),               ClientFlags.Felucca,
                FeatureFlags.ExpansionREN,  CharacterListFlags.ExpansionREN,        0x2E0 ),
        };

        private string m_Name;
        private int m_ID, m_CustomHousingFlag;
        private DateTime m_Publish;
        private ClientFlags m_ClientFlags;
        private FeatureFlags m_SupportedFeatures;
        private CharacterListFlags m_CharListFlags;

        private ClientVersion m_RequiredClient; // Used as an alternative to the flags

        public string Name { get { return m_Name; } }
        public int ID { get { return m_ID; } }
        public DateTime Publish { get { return m_Publish; } }
        public ClientFlags ClientFlags { get { return m_ClientFlags; } }
        public FeatureFlags SupportedFeatures { get { return m_SupportedFeatures; } }
        public CharacterListFlags CharacterListFlags { get { return m_CharListFlags; } }
        public int CustomHousingFlag { get { return m_CustomHousingFlag; } }
        public ClientVersion RequiredClient { get { return m_RequiredClient; } }

        public ExpansionInfo(int id, string name, DateTime publishDate, ClientFlags clientFlags, FeatureFlags supportedFeatures, CharacterListFlags charListFlags, int customHousingFlag)
        {
            m_Name = name;
            m_ID = id;
            m_Publish = publishDate;
            m_ClientFlags = clientFlags;
            m_SupportedFeatures = supportedFeatures;
            m_CharListFlags = charListFlags;
            m_CustomHousingFlag = customHousingFlag;
        }

        public ExpansionInfo(int id, string name, DateTime publishDate, ClientVersion requiredClient, FeatureFlags supportedFeatures, CharacterListFlags charListFlags, int customHousingFlag)
        {
            m_Name = name;
            m_ID = id;
            m_Publish = publishDate;
            m_SupportedFeatures = supportedFeatures;
            m_CharListFlags = charListFlags;
            m_CustomHousingFlag = customHousingFlag;
            m_RequiredClient = requiredClient;
        }

        public static ExpansionInfo GetInfo(Expansion ex)
        {
            return GetInfo((int)ex);
        }

        public static ExpansionInfo GetInfo(int ex)
        {
            int v = (int)ex;

            if (v < 0 || v >= m_Table.Length)
                v = 0;

            return m_Table[v];
        }

        public static ExpansionInfo CurrentExpansion { get { return GetInfo(Core.Expansion); } }

        public override string ToString()
        {
            return m_Name;
        }
    }
#else
public enum Expansion
{
None,
/*
T2A,
UOR,
LBR,
UOTD,
*/
        AOS,
        SE,
        ML,
        reserved1,
        reserved2,
        AngelIsland,
        UOSiegePerilous
    }
    public class ExpansionInfo
    {
        private string m_Name;
        private int m_ID, m_NetStateFlag, m_SupportedFeatures, m_CharListFlags, m_CustomHousingFlag;

        private ClientVersion m_RequiredClient; //Used as an alternative to the flags

        public string Name { get { return m_Name; } }
        public int ID { get { return m_ID; } }
        public int NetStateFlag { get { return m_NetStateFlag; } }
        public int SupportedFeatures { get { return m_SupportedFeatures; } }
        public int CharacterListFlags { get { return m_CharListFlags; } }
        public int CustomHousingFlag { get { return m_CustomHousingFlag; } }
        public ClientVersion RequiredClient { get { return m_RequiredClient; } }

        public ExpansionInfo(int id, string name, int netStateFlag, int supportedFeatures, int charListFlags, int customHousingFlag)
        {
            m_Name = name;
            m_ID = id;
            m_NetStateFlag = netStateFlag;
            m_SupportedFeatures = supportedFeatures;
            m_CharListFlags = charListFlags;
            m_CustomHousingFlag = customHousingFlag;
        }

        public ExpansionInfo(int id, string name, ClientVersion requiredClient, int supportedFeatures, int charListFlags, int customHousingFlag)
        {
            m_Name = name;
            m_ID = id;
            m_SupportedFeatures = supportedFeatures;
            m_CharListFlags = charListFlags;
            m_CustomHousingFlag = customHousingFlag;
            m_RequiredClient = requiredClient;
        }

        public static ExpansionInfo[] Table { get { return m_Table; } }
        private static ExpansionInfo[] m_Table = new ExpansionInfo[]
            {
                //PIX: we need to specify our stuff, not what OSI defines
				new ExpansionInfo( 0, "None"           , 0x00,                             0x0003, 0x008, 0x00 ),
                new ExpansionInfo( 1, "Age of Shadows"  , 0x08,                             0x801F, 0x028, 0x20 ),
                new ExpansionInfo( 2, "Samurai Empire"  , 0x10,                             0x805F, 0x0A8, 0x60 ),	//0x40 | 0x20 = 0x60
				new ExpansionInfo( 3, "Mondain's Legacy", new ClientVersion( "5.0.0a" ),    0x82DF, 0x1A8, 0x2E0 ),	//0x280 | 0x60 = 0x2E0
                new ExpansionInfo( 4, "Reserved1"       , 0x00,                             0x82DF, 0x1A8, 0x2E0 ),
                new ExpansionInfo( 5, "Reserved2"       , 0x00,                             0x82DF, 0x1A8, 0x2E0 ),
                new ExpansionInfo( 6, "Angel Island"    , 0x00,                             0x81DF, 0x008, 0x2E0 ),
                new ExpansionInfo( 7, "Siege Perilous"  , 0x00,                             0x81DF, 0x008, 0x2E0 )

				//0x200 + 0x400 for KR?
			};

        public static ExpansionInfo GetInfo(Expansion ex)
        {
            return GetInfo((int)ex);
        }

        public static ExpansionInfo GetInfo(int ex)
        {
            int v = (int)ex;

            if (v < 0 || v >= m_Table.Length)
                v = 0;

            return m_Table[v];
        }

        public static ExpansionInfo CurrentExpansion { get { return GetInfo(Core.Expansion); } }

        public override string ToString()
        {
            return m_Name;
        }
    }
#endif
}