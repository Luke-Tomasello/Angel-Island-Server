/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

using Server.Accounting;
using Server.Engines;
using System;
using System.Collections.Generic;

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
            return TryValidateSyntax(
                name, minLength, maxLength,
                allowLetters, allowDigits,
                noExceptionsAtStart, maxExceptions,
                exceptions, disallowed, startDisallowed,
                out _, out _);
        }
        /// <summary>
        /// Determines whether a candidate character name conflicts with any active reservation source.
        ///
        /// Reservation sources are composed from two layers:
        /// - Live character names (always block, even on the same account)
        /// - ReservedNames registry (allow reclaim by owning account)
        ///
        /// Behavior:
        /// - A name is considered in conflict if it is:
        ///   • currently in use by any live character (hard block), OR
        ///   • reserved by another account in the ReservedNames registry
        ///
        /// - Reserved names owned by the same account do NOT block,
        ///   allowing identity reclaim on shards that enforce reserved names.
        ///
        /// - The <paramref name="self"/> out parameter indicates that the conflict is due to
        ///   a live character on the same account already using the name. This is used strictly
        ///   for messaging purposes and does not override the conflict.
        ///
        /// Notes:
        /// - Live-name collisions always block, regardless of ownership.
        /// - Reserved-name collisions are ownership-aware and allow reclaim by the owning account.
        /// - Shards that do not care about reserved names may still generate the registry without
        ///   changing the behavior of this function unless they call it.
        /// </summary>
        public static bool IsNameConflict(Mobile m, string candidateName, out bool self)
        {
            self = false;

            if (m == null || string.IsNullOrWhiteSpace(candidateName))
                return false;

            Account? acct = m.Account as Account;
            if (acct == null)
                return false;

            string candidate = candidateName.Trim();

            // Live names always block, even when they belong to this same account.
            Account? liveOwner = AccountHandler.GetLiveAccountByReservedName(candidate);
            bool blockedByLive = liveOwner != null;

            // For messaging only: distinguish "your own live name is already in use."
            self = liveOwner != null && liveOwner == acct;

            // Reserved names are ownership-aware and allow reclaim by the owning account.
            string? reservedOwner = ReservedNames.LookupOwner(candidate);

            bool reservedBySomeoneElse =
                !string.IsNullOrWhiteSpace(reservedOwner) &&
                !string.Equals(reservedOwner, acct.Username, StringComparison.OrdinalIgnoreCase);

            return blockedByLive || reservedBySomeoneElse;
        }

        public enum NameValidationError
        {
            None = 0,
            TooShort,
            TooLong,
            LettersNotAllowed,
            DigitsNotAllowed,
            InvalidCharacter,        // includes “exception not allowed” or “exception at start”
            TooManyExceptions,
            DisallowedWord,          // disallowed substring with valid exception boundaries
            StartsWithDisallowed
        }

        public static bool TryValidateSyntax(
            string name,
            int minLength,
            int maxLength,
            bool allowLetters,
            bool allowDigits,
            bool noExceptionsAtStart,
            int maxExceptions,
            char[] exceptions,
            string[] disallowed,
            string[] startDisallowed,
            out NameValidationError error,
            out string message)
        {
            error = NameValidationError.None;
            message = null;

            if (name == null)
            {
                error = NameValidationError.TooShort;
                message = $"Name must be at least {minLength} characters.";
                return false;
            }

            string original = name;            // for messages if you want
            name = name.Trim();

            if (name.Length < minLength)
            {
                error = NameValidationError.TooShort;
                message = $"Name must be at least {minLength} characters.";
                return false;
            }

            if (name.Length > maxLength)
            {
                error = NameValidationError.TooLong;
                message = $"Name must be no more than {maxLength} characters.";
                return false;
            }

            int exceptCount = 0;
            name = name.ToLower(); // matches your current behavior

            // Character class validation
            if (!allowLetters || !allowDigits || (exceptions.Length > 0 && (noExceptionsAtStart || maxExceptions < int.MaxValue)))
            {
                for (int i = 0; i < name.Length; ++i)
                {
                    char c = name[i];

                    if (c >= 'a' && c <= 'z')
                    {
                        if (!allowLetters)
                        {
                            error = NameValidationError.LettersNotAllowed;
                            message = "Letters (a–z) are not allowed.";
                            return false;
                        }
                        exceptCount = 0;
                    }
                    else if (c >= '0' && c <= '9')
                    {
                        if (!allowDigits)
                        {
                            error = NameValidationError.DigitsNotAllowed;
                            message = "Digits (0–9) are not allowed.";
                            return false;
                        }
                        exceptCount = 0;
                    }
                    else
                    {
                        bool isException = false;

                        for (int j = 0; !isException && j < exceptions.Length; ++j)
                            isException = (c == exceptions[j]);

                        if (!isException)
                        {
                            error = NameValidationError.InvalidCharacter;
                            message = BuildInvalidCharMessage(allowLetters, allowDigits, exceptions);
                            return false;
                        }

                        if (i == 0 && noExceptionsAtStart)
                        {
                            error = NameValidationError.InvalidCharacter;
                            message = $"Name may not start with {FormatExceptions(exceptions)}.";
                            return false;
                        }

                        if (exceptCount++ == maxExceptions)
                        {
                            error = NameValidationError.TooManyExceptions;
                            message = $"Name may contain at most {maxExceptions} of {FormatExceptions(exceptions)} in a row.";
                            return false;
                        }
                    }
                }
            }

            // Disallowed substrings that are “bounded” by exceptions or edges
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
                {
                    error = NameValidationError.DisallowedWord;
                    message = $"The sequence \"{disallowed[i]}\" is not allowed in names.";
                    return false;
                }
            }

            // Disallowed prefixes
            for (int i = 0; i < startDisallowed.Length; ++i)
            {
                if (name.StartsWith(startDisallowed[i]))
                {
                    error = NameValidationError.StartsWithDisallowed;
                    message = $"Names may not start with \"{startDisallowed[i]}\".";
                    return false;
                }
            }

            error = NameValidationError.None;
            message = null;
            return true;
        }
        private static string BuildInvalidCharMessage(bool allowLetters, bool allowDigits, char[] exceptions)
        {
            string ex = FormatExceptions(exceptions);

            if (allowLetters && allowDigits)
                return $"Only letters, digits, or {ex} may be used in a name.";
            if (allowLetters && !allowDigits)
                return $"Only letters or {ex} may be used in a name.";
            if (!allowLetters && allowDigits)
                return $"Only digits or {ex} may be used in a name.";

            return $"Only {ex} may be used in a name.";
        }

        // Helper to present exceptions as a friendly set (e.g., "space, '-', '.', '''")
        private static string FormatExceptions(char[] exceptions)
        {
            if (exceptions == null || exceptions.Length == 0)
                return "no special characters";

            // Map common set SpaceDashPeriodQuote to friendly text
            // { ' ', '-', '.', '\'' } -> "space, '-', '.', '''"
            var parts = new List<string>(exceptions.Length);
            for (int i = 0; i < exceptions.Length; i++)
            {
                char c = exceptions[i];
                if (c == ' ')
                    parts.Add("space");
                else
                    parts.Add("'" + c.ToString() + "'");
            }
            return string.Join(", ", parts);
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
        public static string[] StartDisallowed
        {
            get { return m_StartDisallowed; }
        }
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
                // exstaff
                "jade",
                "mith",
                "yoar",
                "snafu",
                "pixie",
                "liberation",

                // scam names
                "connection lost",

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