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

/* Scripts/Engines/AI/AI/VendorAI.cs
 * CHANGELOG
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Engines;
using Server.Items;
using Server.Network;
using System;

//
// This is a first simple AI
//
//

namespace Server.Mobiles
{
    public class VendorAI : BaseAI
    {
        public VendorAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool DoActionWander()
        {
            m_Mobile.DebugSay(DebugFlags.AI, "I'm fine");

            if (m_Mobile.Combatant != null)
            {

                m_Mobile.DebugSay(DebugFlags.AI, "{0} is attacking me", m_Mobile.Combatant.Name);

                m_Mobile.Say(Utility.RandomList(1005305, 501603));

                // On siege vendors fight back.
                // In the vendor class we will trap OnActionCombat and switch to Melee AI
                //	we do this switch so as not to rewrite additional complexities into either melee or vendor AIs
                //	We will also trap OnActionWander in the vendor class to return the AI to standard vendor AI
                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisStyleRules())
                {
                    m_Mobile.FocusMob = m_Mobile.Combatant;
                    Action = ActionType.Combat;
                }
                else
                    Action = ActionType.Flee;
            }
            else
            {
                if (m_Mobile.FocusMob != null)
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "{0} has talked to me", m_Mobile.FocusMob.Name);

                    Action = ActionType.Interact;
                }
                else
                {
                    m_Mobile.Warmode = false;

                    base.DoActionWander();
                }
            }

            return true;
        }

        public override bool DoActionInteract()
        {
            Mobile customer = m_Mobile.FocusMob;

            if (m_Mobile.Combatant != null)
            {

                m_Mobile.DebugSay(DebugFlags.AI, "{0} is attacking me", m_Mobile.Combatant.Name);

                m_Mobile.Say(Utility.RandomList(1005305, 501603));

                Action = ActionType.Flee;

                return true;
            }

            if (customer == null || customer.Deleted || customer.Map != m_Mobile.Map)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "My customer has disapeared");
                m_Mobile.FocusMob = null;

                Action = ActionType.Wander;
            }
            else
            {
                if (customer.InRange(m_Mobile, m_Mobile.RangeFight))
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "I am with {0}", customer.Name);

                    m_Mobile.Direction = m_Mobile.GetDirectionTo(customer);
                }
                else
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "{0} is gone", customer.Name);

                    m_Mobile.FocusMob = null;

                    Action = ActionType.Wander;
                }
            }

            return true;
        }

        public override bool DoActionGuard()
        {
            m_Mobile.FocusMob = m_Mobile.Combatant;
            return base.DoActionGuard();
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(m_Mobile, 4))
                return true;

            return base.HandlesOnSpeech(from);
        }
        private static SkillName[] m_KeywordTable = new SkillName[]
        {
            SkillName.Parry,
            SkillName.Healing,
            SkillName.Hiding,
            SkillName.Stealing,
            SkillName.Alchemy,
            SkillName.AnimalLore,
            SkillName.ItemID,
            SkillName.ArmsLore,
            SkillName.Begging,
            SkillName.Blacksmith,
            SkillName.Fletching,
            SkillName.Peacemaking,
            SkillName.Camping,
            SkillName.Carpentry,
            SkillName.Cartography,
            SkillName.Cooking,
            SkillName.DetectHidden,
            SkillName.Discordance,//??
            SkillName.EvalInt,
            SkillName.Fishing,
            SkillName.Provocation,
            SkillName.Lockpicking,
            SkillName.Magery,
            SkillName.MagicResist,
            SkillName.Tactics,
            SkillName.Snooping,
            SkillName.RemoveTrap,
            SkillName.Musicianship,
            SkillName.Poisoning,
            SkillName.Archery,
            SkillName.SpiritSpeak,
            SkillName.Tailoring,
            SkillName.AnimalTaming,
            SkillName.TasteID,
            SkillName.Tinkering,
            SkillName.Veterinary,
            SkillName.Forensics,
            SkillName.Herding,
            SkillName.Tracking,
            SkillName.Stealth,
            SkillName.Inscribe,
            SkillName.Swords,
            SkillName.Macing,
            SkillName.Fencing,
            SkillName.Wrestling,
            SkillName.Lumberjacking,
            SkillName.Mining,
            SkillName.Meditation
        };
        // Vendor specific dialog
        public override void OnSpeech(SpeechEventArgs e)
        {
            if (e == null || e.Handled)
                return;

            if (m_Mobile is not BaseVendor vendor)
                return;

            Mobile from = e.Mobile;
            if (from == null || from.Deleted)
                return;

            // =====================================================================
            // 1) Vendor Buy/Sell (range 4)
            // =====================================================================
            if (from.InRange(m_Mobile, 4))
            {
                bool isVendorSell = e.HasKeyword(0x14D); // *vendor sell*
                bool isVendorBuy = e.HasKeyword(0x3C);  // *vendor buy*
                bool isNamedBuySell =
                    WasNamed(e.Speech) && (e.HasKeyword(0x171) /*buy*/ || e.HasKeyword(0x177) /*sell*/);

                bool isVendorIntent = isVendorSell || isVendorBuy || isNamedBuySell;

                if (isVendorIntent)
                {
                    #region Vendor-in-conflict
                    // NOTE: In normal operation, this guard is rarely exercised in VendorAI.
                    //
                    // When a vendor becomes involved in combat, the engine typically swaps the vendor to MeleeAI
                    // (and therefore routes speech handling through BaseAI / MeleeAI rather than VendorAI).
                    // As a result, a vendor who is "busy fighting" is usually not processed here at all.
                    //
                    // We keep this check as "belt and suspenders" coverage for edge cases:
                    //  - transient AI transitions (VendorAI <-> MeleeAI) during combat state changes
                    //  - unusual vendors that remain BaseVendor yet briefly retain VendorAI while having a Combatant
                    //  - any future customizations where vendors can fight without swapping AI
                    //
                    // In short: combat vendors are handled in BaseAI; this is defensive.
                    if (DenyIfBusyFighting(e, includeMeleeAIProxy: true))
                        return;
                    #endregion Vendor-in-conflict

                    e.Handled = true;

                    if (isVendorSell || (isNamedBuySell && e.HasKeyword(0x177))) // *sell*
                    {
                        if (!vendor.IsBuying(from))
                        {
                            vendor.NotBuyingMessage(from);
                            m_Mobile.FocusMob = from;
                            return;
                        }

                        vendor.VendorSell(from);
                    }
                    else if (isVendorBuy || (isNamedBuySell && e.HasKeyword(0x171))) // *buy*
                    {
                        if (!vendor.IsSelling(from))
                        {
                            vendor.NotSellingMessage(from);
                            m_Mobile.FocusMob = from;
                            return;
                        }

                        vendor.VendorBuy(from);
                    }

                    m_Mobile.FocusMob = from;
                    return;
                }
            }

            // =====================================================================
            // 2) Standard vendor speech: Move / Time / Train (range 3, in-town humans)
            // =====================================================================
            if (!from.Alive)
                return;

            if (!from.InRange(m_Mobile.Location, 3))
                return;

            if (!m_Mobile.IsHumanInTown())
                return;

            // -------------------------------------------------------------
            // Move
            // -------------------------------------------------------------
            if (e.HasKeyword(0x9D) && WasNamed(e.Speech)) // *move*
            {
                e.Handled = true;

                // Keep original semantics (Combatant-only) unless you want the AI_Melee proxy here too.
                if (!DenyIfBusyFighting(e, includeMeleeAIProxy: false))
                {
                    // "Excuse me?"
                    m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501516);
                    WalkRandomInHome(2, 2, 1, evenIfStationed: true);
                }

                return;
            }

            // -------------------------------------------------------------
            // Courier Request
            // -------------------------------------------------------------
            // Only a responding sympathizer claims the speech; ordinary vendors
            //  stay silent and it falls through (see BaseVendor.HandleCourierSpeech).
            if (vendor.HandleCourierSpeech(e.Mobile, e.Speech, WasNamed(e.Speech)))
            {
                e.Handled = true;
                return;
            }


            // -------------------------------------------------------------
            // Information / referrals
            // -------------------------------------------------------------
            if (vendor.IsInformationRequest(e.Speech))
            {
                if (vendor.TryGiveReferral(e.Mobile, out string message))
                {
                    e.Handled = true;

                    if (!string.IsNullOrEmpty(message))
                        m_Mobile.Say(message);

                    return;
                }
            }

            // -------------------------------------------------------------
            // Time
            // -------------------------------------------------------------
            if (e.HasKeyword(0x9E) && WasNamed(e.Speech)) // *time*
            {
                e.Handled = true;

                if (!DenyIfBusyFighting(e, includeMeleeAIProxy: false))
                {
                    Clock.GetTime(m_Mobile, out int generalNumber, out string exactTime);
                    m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, generalNumber);
                }

                return;
            }

            // -------------------------------------------------------------
            // Train: list what the vendor can teach (keyword model)
            // -------------------------------------------------------------
            if (e.HasKeyword(0x6C) && WasNamed(e.Speech)) // *train*
            {
                e.Handled = true;

                if (DenyIfBusyFighting(e, includeMeleeAIProxy: false))
                    return;

                bool foundSomething = false;

                Skills ourSkills = m_Mobile.Skills;
                Skills theirSkills = from.Skills;

                for (int i = 0; i < ourSkills.Length && i < theirSkills.Length; ++i)
                {
                    Skill skill = ourSkills[i];
                    Skill theirSkill = theirSkills[i];

                    if (skill != null && theirSkill != null && skill.Base >= 60.0 && m_Mobile.CheckTeach(skill.SkillName, from))
                    {
                        double toTeach = skill.Base / 3.0;
                        if (toTeach > 42.0)
                            toTeach = 42.0;

                        if (toTeach > theirSkill.Base)
                        {
                            int number = 1043059 + i;
                            if (number > 1043107)
                                continue;

                            if (!foundSomething)
                                m_Mobile.Say(1043058); // I can train the following:

                            m_Mobile.Say(number);
                            foundSomething = true;
                        }
                    }
                }

                if (!foundSomething)
                    m_Mobile.Say(501505); // Alas, I cannot teach thee anything.

                return;
            }

            // -------------------------------------------------------------
            // Train/Teach: free-form phrase parsing ("Bobby teach animal lore 100")
            // -------------------------------------------------------------
            if (e.HasKeyword("train") || e.HasKeyword("teach"))
            {
                // Only claim it if parsing succeeds; otherwise let other speech systems see it.
                if (!TryParseTrainTeach(e.Speech, AnyName(e.Speech), out var skillname, out var amount))
                    return;

                e.Handled = true;

                if (DenyIfBusyFighting(e, includeMeleeAIProxy: false))
                    return;

                if (m_Mobile is BaseCreature bc)
                {
                    int score;
                    string realName = IntelligentDialogue.Levenshtein.BestEnumMatch(typeof(SkillName), skillname, out score);

                    if (string.IsNullOrWhiteSpace(realName) || score > 5)
                        return;

                    SkillName sn = (SkillName)Enum.Parse(typeof(SkillName), realName, true);

                    if (amount <= 0)
                    {
                        bc.Teach(sn, from, 0, doTeach: false);
                        return;
                    }

                    int cost = 0;
                    if (bc.CheckTeachSkills(sn, from, amount, ref cost, false) == BaseCreature.TeachResult.Success)
                    {
                        Container cont = from.Backpack;
                        if (cont != null)
                        {
                            if (vendor.ConsumeTotal(from, cont, typeof(UnemploymentCheck), null, cost) ||
                                vendor.ConsumeTotal(from, cont, typeof(TrainingVoucher), null, cost) ||
                                vendor.ConsumeTotal(from, cont, typeof(CivicBond), new[] { CivicBondKeys.TrainingVoucher }, cost) ||
                                cont.ConsumeTotal(typeof(Gold), cost))
                            {
                                bc.Teach(sn, from, amount, doTeach: true);
                            }
                            else
                            {
                                vendor.SayTo(from, 500192); // Begging thy pardon, but thou canst not afford that.
                            }
                        }
                    }
                    else
                    {
                        bc.Teach(sn, from, 0, doTeach: false);
                    }
                }

                return;
            }

            // -------------------------------------------------------------
            // Train <skill>: keyword-table model (only when vendor was named)
            // -------------------------------------------------------------
            if (WasNamed(e.Speech))
            {
                SkillName toTrain = (SkillName)(-1);

                for (int i = 0; toTrain == (SkillName)(-1) && i < e.Keywords.Length; ++i)
                {
                    int keyword = e.Keywords[i];

                    if (keyword == 0x154)
                    {
                        toTrain = SkillName.Anatomy;
                    }
                    else if (keyword >= 0x6D && keyword <= 0x9C)
                    {
                        int index = keyword - 0x6D;

                        if (index >= 0 && index < m_KeywordTable.Length)
                            toTrain = m_KeywordTable[index];
                    }
                }

                if (toTrain != (SkillName)(-1))
                {
                    e.Handled = true;

                    if (DenyIfBusyFighting(e, includeMeleeAIProxy: false))
                        return;

                    Skill skill = m_Mobile.Skills[toTrain];

                    if (skill == null || skill.Base < 60.0 || !m_Mobile.CheckTeach(toTrain, from))
                        m_Mobile.Say(501507); // 'Tis not something I can teach thee of.
                    else
                        m_Mobile.Teach(toTrain, from, 0, false);

                    return;
                }
            }
        }

        /// <summary>
        /// Common vendor gate: if the vendor is busy fighting, consume the speech and emit the OSI message.
        /// </summary>
        /// <param name="e">Speech event.</param>
        /// <param name="includeMeleeAIProxy">
        /// If true, also treat AI_Melee as "busy fighting" (some vendors swap AI while in combat).
        /// </param>
        /// <returns>True if denied (and speech was handled); false if vendor may proceed.</returns>
        private bool DenyIfBusyFighting(SpeechEventArgs e, bool includeMeleeAIProxy)
        {
            if (e == null)
                return true;

            bool inCombat = (m_Mobile.Combatant != null) ||
                            (includeMeleeAIProxy && m_Mobile.AI == AIType.AI_Melee);

            if (!inCombat)
                return false;

            e.Handled = true;

            // "I am too busy fighting to deal with thee!"
            m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
            return true;
        }

        private static bool TryParseTrainTeach(string speech, bool wasNamed, out string skillName, out int amount)
        {
            skillName = null;
            amount = 0;

            if (string.IsNullOrWhiteSpace(speech))
                return false;

            var tokens = speech.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return false;

            int i = 0;

            // 1) Optional NPC name — discard if present
            if (wasNamed)
            {
                i++;
                if (i >= tokens.Length) return false;
            }

            // 2) Must be "train" or "teach"
            var verb = tokens[i].ToLowerInvariant();
            if (verb != "train" && verb != "teach")
                return false;

            // 3) Optional trailing amount (plain integer, no brackets)
            int last = tokens.Length - 1;
            bool hasAmount = int.TryParse(tokens[last], out int parsedAmount);

            // 4) Skill name = tokens between verb and (optional) amount
            int skillStart = i + 1;
            int skillEndExclusive = hasAmount ? last : tokens.Length;

            if (skillStart >= skillEndExclusive) // must have at least one skill token
                return false;

            skillName = string.Join(" ", tokens, skillStart, skillEndExclusive - skillStart);
            amount = hasAmount ? parsedAmount : 0;

            return true;
        }

        #region Serialize
        private SaveFlags m_flags;

        [Flags]
        private enum SaveFlags
        {   // 0x00 - 0x800 reserved for version
            unused = 0x1000
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;                                // current version (up to 4095)
            m_flags = m_flags | (SaveFlags)version;         // save the version and flags
            writer.Write((int)m_flags);

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            m_flags = (SaveFlags)reader.ReadInt();              // grab the version an flags
            int version = (int)(m_flags & (SaveFlags)0xFFF);    // maskout the version

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
            switch (version)
            {
                default: break;
            }

        }
        #endregion Serialize
    }
}