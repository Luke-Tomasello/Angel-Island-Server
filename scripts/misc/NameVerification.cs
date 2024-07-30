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

/* Misc/NameVerification.cs
 * CHANGELOG:
 *  3/25/07, Pix
 *		Commented out smerx from the disallowed names list
 *  5/02/06, Kit
 *		Added name "mortal" to name disallow list.
 *	3/26/05, Adam
 *		Add a new BuildList() function that builds a list of disallowed words.
 *		We also updated the standard constructor Validate() to use BuildList() to build
 *		the complete 'default' set of disallowed words.
 *	6/18/04 - Old Salty:
 * 		Made the protected names more specific, added a few more
 *	6/15/04 - Old Salty: 
 * 		Added staff and protected names, and a few more vulgar words
 */

namespace Server.Misc
{
    public class NameVerification
    {
        public static readonly char[] SpaceDashPeriodQuote = new char[]
            {
                ' ', '-', '.', '\''
            };

        public static readonly char[] Empty = new char[0];

        public static void Initialize()
        {
            CommandSystem.Register("ValidateName", AccessLevel.Administrator, new CommandEventHandler(ValidateName_OnCommand));
        }

        [Usage("ValidateName")]
        [Description("Checks the result of NameValidation on the specified name.")]
        public static void ValidateName_OnCommand(CommandEventArgs e)
        {
            if (Validate(e.ArgString, 2, 16, true, true, true, 1, SpaceDashPeriodQuote))
                e.Mobile.SendMessage(0x59, "That name is considered valid.");
            else
                e.Mobile.SendMessage(0x22, "That name is considered invalid.");
        }

        public static string[] BuildList(bool Curse, bool Names, bool Titles, bool Special)
        {
            int length = 0;
            if (Curse) { length += m_Curse.Length; }
            if (Names) { length += m_Names.Length; }
            if (Titles) { length += m_Titles.Length; }
            if (Special) { length += m_Special.Length; }
            string[] disallowed = new string[length];
            int off = 0;
            if (Curse) { m_Curse.CopyTo(disallowed, off); off += m_Curse.Length; }
            if (Names) { m_Names.CopyTo(disallowed, off); off += m_Names.Length; }
            if (Titles) { m_Titles.CopyTo(disallowed, off); off += m_Titles.Length; }
            if (Special) { m_Special.CopyTo(disallowed, off); off += m_Special.Length; }
            return disallowed;
        }

        public static bool Validate(string name, int minLength, int maxLength, bool allowLetters, bool allowDigits, bool noExceptionsAtStart, int maxExceptions, char[] exceptions)
        {
            string[] disallowed = BuildList(true, true, true, true);
            return Validate(name, minLength, maxLength, allowLetters, allowDigits, noExceptionsAtStart, maxExceptions, exceptions, disallowed, m_StartDisallowed);
        }

        public static bool Validate(string name, int minLength, int maxLength, bool allowLetters, bool allowDigits, bool noExceptionsAtStart, int maxExceptions, char[] exceptions, string[] disallowed)
        {
            return Validate(name, minLength, maxLength, allowLetters, allowDigits, noExceptionsAtStart, maxExceptions, exceptions, disallowed, m_StartDisallowed);
        }

        public static bool Validate(string name, int minLength, int maxLength, bool allowLetters, bool allowDigits, bool noExceptionsAtStart, int maxExceptions, char[] exceptions, string[] disallowed, string[] startDisallowed)
        {
            if (name.Length < minLength || name.Length > maxLength)
                return false;

            int exceptCount = 0;

            name = name.ToLower();

            if (!allowLetters || !allowDigits || (exceptions.Length > 0 && (noExceptionsAtStart || maxExceptions < int.MaxValue)))
            {
                for (int i = 0; i < name.Length; ++i)
                {
                    char c = name[i];

                    if (c >= 'a' && c <= 'z')
                    {
                        if (!allowLetters)
                            return false;

                        exceptCount = 0;
                    }
                    else if (c >= '0' && c <= '9')
                    {
                        if (!allowDigits)
                            return false;

                        exceptCount = 0;
                    }
                    else
                    {
                        bool except = false;

                        for (int j = 0; !except && j < exceptions.Length; ++j)
                            if (c == exceptions[j])
                                except = true;

                        if (!except || (i == 0 && noExceptionsAtStart))
                            return false;

                        if (exceptCount++ == maxExceptions)
                            return false;
                    }
                }
            }

            for (int i = 0; i < disallowed.Length; ++i)
            {
                int indexOf = name.IndexOf(disallowed[i]);

                if (indexOf == -1)
                    continue;

                bool badPrefix = (indexOf == 0);

                for (int j = 0; !badPrefix && j < exceptions.Length; ++j)
                    badPrefix = (name[indexOf - 1] == exceptions[j]);

                if (!badPrefix)
                    continue;

                bool badSuffix = ((indexOf + disallowed[i].Length) >= name.Length);

                for (int j = 0; !badSuffix && j < exceptions.Length; ++j)
                    badSuffix = (name[indexOf + disallowed[i].Length] == exceptions[j]);

                if (badSuffix)
                    return false;
            }

            for (int i = 0; i < startDisallowed.Length; ++i)
            {
                if (name.StartsWith(startDisallowed[i]))
                    return false;
            }

            return true;
        }

        private static string[] m_StartDisallowed = new string[]
            {
                "seer",
                "counselor",
                "gm",
                "lady",
                "lord",
                "owner"
            };

        private static string[] m_Curse = new string[]
            {
                "jigaboo",
                "chigaboo",
                "wop",
                "kyke",
                "kike",
                "tit",
                "spic",
                "prick",
                "piss",
                "lezbo",
                "lesbo",
                "felatio",
                "dyke",
                "dildo",
                "chinc",
                "chink",
                "cunnilingus",
                "cum",
                "cocksucker",
                "cock",
                "clitoris",
                "clit",
                "ass",
                "hitler",
                "penis",
                "nigga",
                "nigger",
                "klit",
                "kunt",
                "jiz",
                "jism",
                "jerkoff",
                "jackoff",
                "goddamn",
                "fag",
                "blowjob",
                "bitch",
                "asshole",
                "dick",
                "pussy",
                "snatch",
                "cunt",
                "twat",
                "shit",
                "fuck",
                "faggot",
                "fagot",
                "fagget",
                "faget",
                "fucker",
                "fucking",
            };

        private static string[] m_Names = new string[]
            {
                // staff
                "adam ant",
                //"jade",
                //"mith",
                "yoar",
                "snafu",
                //"pixie",
                //"liberation",

                // special Adam Ant characters
                "adamodious anthias",
                "lucanis anthias",
                "kahn anthias",
                "captain anthias",

                //? 
                "sephirus",
                "redstone",
                "elsa",

                // lore
                "dupre",
                "lor",
                "kat",
                "shamino",
                "iolo",

                // council names - not sure they should be protected
                "etheorious moori",
                "luscious moori",
                "broderick sway",
                "keras moiras",
                "erinyes furiae",
                "heremod furiae",
                "hrothgar wolfson",
                "belk baranow",
            };

        private static string[] m_Titles = new string[]
            {
                "tailor",
                "smith",
                "scholar",
                "rogue",
                "novice",
                "neophyte",
                "merchant",
                "medium",
                "master",
                "mage",
                "journeyman",
                "grandmaster",
                "fisherman",
                "expert",
                "chef",
                "carpenter",
                "british",
                "blackthorne",
                "blackthorn",
                "beggar",
                "archer",
                "apprentice",
                "adept"
            };

        private static string[] m_Special = new string[]
            {
                "lb",
                "gamemaster",
                "frozen",
                "squelched",
                "invulnerable",
                "osi",
                "origin",

                // special designation
                "mortal",
                "system",
            };
    }
}