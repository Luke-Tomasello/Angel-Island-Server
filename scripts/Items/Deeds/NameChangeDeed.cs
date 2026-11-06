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

/* Items/Deeds/NameChangeDeed.cs
 * ChangeLog:
 *	8/26/07, Adam
 *		Double check that a name change is in your backpack when it is applied, this stops
 *		Players from getting the rename prompt and handing the deed to a friend.
 *	2/23/05, erlein
 *		Changed output format of log entry to include date/time of change.
 *  2/15/05, erlein
 *		Altered so is automatic.. added prompt which includes warning,
 *		regular expression to check for weird chars, NameVerification
 *		validate check and log entry to track name changes ("logs/namechange.log").
 *  8/27/04, Adam
 *		Add message when double clicked.
 */

using Server.Accounting;
using Server.Engines;
using Server.Engines.Seasons;
using Server.Misc;
using Server.Prompts;
using System;
using System.IO;

namespace Server.Items
{
    public class NameChangeDeed : Item
    {
        [Constructable]
        public NameChangeDeed()
            : base(0x14F0)
        {
            base.Weight = 1.0;
            base.Name = "a name change deed";
        }

        public NameChangeDeed(Serial serial)
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
            // Make sure is in pack
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);  // Must be in pack to use!!
                return;
            }

            // Do namechange
            from.SendMessage("Please choose your new name. There will be no refunds for a poorly selected name.");
            from.Prompt = new RenamePrompt(from, this);

        }

        private class RenamePrompt : Prompt
        {
            private Mobile m_from;
            private NameChangeDeed m_ncdeed;

            public RenamePrompt(Mobile from, NameChangeDeed ncdeed)
            {
                m_from = from;
                m_ncdeed = ncdeed;
            }
            public override void OnResponse(Mobile from, string text)
            {
                NameChange.PerformNameChange(from, m_ncdeed, text, updateSeasonalAccount: Core.RuleSets.SeasonsRules());
            }
        }
    }

    public class PersonalNameChangeDeed : Item
    {
        private Serial m_ownerSerial = Serial.MinusOne;

        [Constructable]
        public PersonalNameChangeDeed()
            : base(0x14F0)
        {
            base.Weight = 1.0;
            base.Name = "a personal name change deed";
        }

        public PersonalNameChangeDeed(Serial serial)
            : base(serial)
        {
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int OwnerSerial
        {
            get
            {
                return m_ownerSerial;
            }
            set
            {
                m_ownerSerial = value;
                InvalidateProperties();
            }
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
            if (Deleted || from == null)
                return;

            // Make sure is in pack
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);  // Must be in pack to use!!
                return;
            }

            if (from.Serial != this.m_ownerSerial)
            {
                from.SendMessage("This is not your name change deed.");
                return;
            }

            // Do namechange
            from.SendMessage("Please choose your new name.");
            from.Prompt = new RenamePrompt(from, this);

        }

        private class RenamePrompt : Prompt
        {
            private Mobile m_from;
            private PersonalNameChangeDeed m_ncdeed;

            public RenamePrompt(Mobile from, PersonalNameChangeDeed ncdeed)
            {
                m_from = from;
                m_ncdeed = ncdeed;
            }
            public override void OnResponse(Mobile from, string text)
            {
                NameChange.PerformNameChange(from, m_ncdeed, text, updateSeasonalAccount: Core.RuleSets.SeasonsRules());
            }
        }
    }
    internal static class NameChange
    {
        public static void PerformNameChange(Mobile from, Item deed, string newName, bool updateSeasonalAccount = false)
        {
            if (from.Account is not Account acct)
                return;

            // Basic deed validity checks
            if (deed == null || deed.Deleted || !deed.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Must be in pack to use!!
                return;
            }

            // Normalize candidate
            string candidate = (newName ?? string.Empty).Trim();
            if (candidate.Length == 0)
            {
                from.SendMessage("Please enter a name.");
                return;
            }

            // -------- Syntax validation (friendly reasons) --------
            bool allowDigits = !Core.RuleSets.MortalisStyleRules();
            var disallowed = Server.Misc.NameVerification.BuildList(Curse: true, Names: true, Titles: true, Special: true);

            if (!Server.Misc.NameVerification.TryValidateSyntax(
                    candidate,
                    minLength: 2,
                    maxLength: 16,
                    allowLetters: true,
                    allowDigits: allowDigits,
                    noExceptionsAtStart: true,
                    maxExceptions: 1,
                    exceptions: Server.Misc.NameVerification.SpaceDashPeriodQuote,
                    disallowed: disallowed,
                    startDisallowed: Server.Misc.NameVerification.StartDisallowed,
                    out var err,
                    out var msg))
            {
                from.SendMessage(msg ?? "That name is not allowed here.");
                return;
            }

            // -------- Mortalis/Seasons uniqueness enforcement --------
            if (Core.RuleSets.MortalisStyleRules())
            {
                if (NameVerification.IsNameConflict(from, candidate, out var self))
                {
                    from.SendMessage(self
                        ? "You are already using that name."
                        : "That name is already reserved by another account.");
                    return;
                }
            }

            string oldName = from.Name;

            // -------- Passed: commit the change --------
            try
            {
                using (var log = new StreamWriter("logs/namechange.log", true))
                    log.WriteLine("{0:u}: {1},{2} -> {3}", DateTime.UtcNow, from.Account, oldName, candidate);
            }
            catch { } // ignore logging errors

            // Relinquish old name and claim new name
            ReservedNames.ReplaceName(acct, oldName, candidate);

            // Optional: Update seasonal account
            if (updateSeasonalAccount)
                SeasonManager.ReplaceName(acct.GUID, oldName, candidate);

            // Assign the new name
            from.Name = candidate;

            from.SendMessage("You have successfully changed your name to {0}.", candidate);

            // Consume the deed
            deed.Delete();
        }
    }
}

