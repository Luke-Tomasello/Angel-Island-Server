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

/* Scripts\Mobiles\Vendors\BaseVendor.Personalty.cs
 * Changelog
 *  7/21/2026, Adam
 *  Created.
 */
using Server.Engines.Alignment;
using Server.Engines.CourierSystem;
using Server.Items;
using Server.Regions;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public enum VendorPersonality
    {
        None,

        OrcEnemy,
        OrcSympathizer,
        OrcSpy,

        HumanEnemy,
        HumanSympathizer,
        HumanSpy
    }

    public enum VendorInteraction
    {
        Trade,
        Refuse,
        Attack,

        HumanInformant,
        OrcInformant
    }

    public abstract partial class BaseVendor : BaseCreature, IVendor
    {
        private static readonly List<BaseVendor> m_OrcNetwork = new();
        private static readonly List<BaseVendor> m_HumanNetwork = new();
        public static IReadOnlyList<BaseVendor> OrcNetwork => m_OrcNetwork;
        public static IReadOnlyList<BaseVendor> HumanNetwork => m_HumanNetwork;
        private readonly Memory m_ConversationMemory = new Memory();
        private readonly Memory m_ReferralMemory = new Memory();

        public virtual VendorPersonality Personality
        {
            get
            {
                if (!ValidForPersonality)
                    return VendorPersonality.None;

                return IntrinsicPersonality;
            }
        }

        /// <summary>
        /// The personality implied by this vendor's Serial, independent of whether
        /// the vendor is currently ValidForPersonality (e.g. has a district).
        /// </summary>
        public VendorPersonality IntrinsicPersonality
        {
            get
            {
                switch (Serial.Value % 20)
                {
                    case 0:
                        return VendorPersonality.OrcEnemy;

                    case 1:
                    case 2:
                        return VendorPersonality.OrcSympathizer;

                    case 3:
                        return VendorPersonality.HumanEnemy;

                    case 4:
                    case 5:
                        return VendorPersonality.HumanSympathizer;

                    case 6:
                        return VendorPersonality.HumanSpy;

                    case 7:
                        return VendorPersonality.OrcSpy;

                    default:
                        return VendorPersonality.None;
                }
            }
        }

        internal static bool IsOrcAligned(Mobile mobile)
        {
            if (mobile == null)
                return false;

            if (AlignmentSystem.Enabled &&
                AlignmentSystem.Find(mobile, true) == AlignmentType.Orc)
            {
                return true;
            }

            bool useMask =
                (Core.RuleSets.SeasonsRules(2, 3) || Core.RuleSets.UpheavalRules()) ||
                (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled);

            if (useMask &&
                mobile.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
            {
                return true;
            }

            return false;
        }
        private bool IsUnderHumanControl
        {
            get
            {
                return District != null && District.IsConquered;
            }
        }

        private bool IsUnderOrcControl
        {
            get
            {
                return District != null && !District.IsConquered;
            }
        }

        public virtual bool Interact(Mobile from, out string message)
        {
            message = null;

            // Vendors outside the territory system behave normally.
            if (District == null)
                return true;

            bool orcAligned = IsOrcAligned(from);

            // Aggressive personalities take precedence over commerce.
            if (Personality == VendorPersonality.OrcEnemy && orcAligned)
            {
                message = "You'll get nothing from me, orc!";

                Combatant = from;
                AggressiveAction(from);

                return false;
            }

            if (Personality == VendorPersonality.HumanEnemy && !orcAligned)
            {
                message = "Your kind is not welcome here!";

                Combatant = from;
                AggressiveAction(from);

                return false;
            }

            if (IsUnderHumanControl)
            {
                // Humans may trade normally.
                if (!orcAligned)
                    return true;

                // Orcs require a sympathetic vendor.
                if (IsOrcSympathizer)
                {
                    message = "Keep your voice down.";
                    return true;
                }

                message = "I will not trade with your kind.";
                return false;
            }

            if (IsUnderOrcControl)
            {
                // Orcs may trade normally.
                if (orcAligned)
                    return true;

                // Humans require a sympathetic vendor.
                if (IsHumanSympathizer)
                {
                    message = "Quickly, before anyone notices.";
                    return true;
                }

                message = "I cannot serve you while the orcs control this town.";
                return false;
            }

            return true;
        }

        private bool IsOrcSympathizer
        {
            get
            {
                return Personality == VendorPersonality.OrcSympathizer ||
                       Personality == VendorPersonality.OrcSpy;
            }
        }

        private bool IsHumanSympathizer
        {
            get
            {
                return Personality == VendorPersonality.HumanSympathizer ||
                       Personality == VendorPersonality.HumanSpy;
            }
        }

        private static readonly Memory m_PersonalityMemory = new Memory();

        /*
         * Courier Throttles
         *
         * Courier assignments have two independent throttles. Both are started only
         * when an assignment has been successfully handed out (not on delivery, and
         * not on a failed request), so discarding the courier item consumes them.
         *
         * 1. Player cooldown (m_CourierCooldownMemory, keyed by the player)
         *    Limits DEMAND. Once a player accepts an assignment, every vendor refuses
         *    that player until the cooldown expires. This prevents one player from
         *    rapidly farming the network by moving vendor to vendor, rerolling
         *    destinations/reward levels by discarding courier items, or cheaply
         *    exhausting every vendor's work and denying it to other players.
         *    Compare the escort system's per-player EscortDelay.
         *
         * 2. Vendor restock (m_CourierRestockMemory, keyed by the source vendor)
         *    Limits SUPPLY. Once a vendor hands out an assignment, that vendor has no
         *    more courier work for anyone until it restocks, just as an escort is no
         *    longer available once taken, or a vendor's stock is gone until replenished.
         *    Players remain free to seek work from another vendor (subject to 1).
         *    Restock only affects offering work; a restocking vendor is still a valid
         *    courier destination and referral, so it is deliberately not part of
         *    ValidForPersonality or network registration.
         *
         * These controls are intentionally separate and neither is redundant:
         *  - Without the player cooldown, vendor restock alone does not stop farming;
         *    a player simply moves to the next vendor with work available.
         *  - Without the vendor restock, any number of players could line up behind
         *    one another and each receive work from the same vendor.
         *
         * Both are runtime Memory only and reset on server restart.
         */
        private static readonly Memory m_CourierCooldownMemory = new Memory();
        private static double CourierCooldownSeconds
        {
            get { return Core.Debug ? 10.0 : 600.0; }
        }

        private static readonly Memory m_CourierRestockMemory = new Memory();
        private static double CourierRestockSeconds
        {
            get { return Core.Debug ? 10.0 : 600.0; }
        }

        #region Courier Unwind Support
        /*
         * Owner testing support for [UnwindCourierTransactions]; not used in normal play.
         * Each restock entry's Context is the player whose assignment started it, so a
         * player's restocks can be identified even after the courier item is gone.
         */

        internal static bool ClearCourierCooldown(Mobile player)
        {
            if (player == null || !m_CourierCooldownMemory.Recall(player))
                return false;

            m_CourierCooldownMemory.Forget(player);
            return true;
        }

        internal static bool ClearCourierRestock(BaseVendor vendor)
        {
            if (vendor == null || !m_CourierRestockMemory.Recall(vendor))
                return false;

            m_CourierRestockMemory.Forget(vendor);
            return true;
        }

        /// <summary>
        /// Clears only the vendor restocks started by this player's assignments.
        /// </summary>
        internal static int ClearCourierRestocksFor(Mobile player)
        {
            if (player == null)
                return 0;

            m_CourierRestockMemory.TidyMemory();

            List<object> vendors = new List<object>();

            foreach (object key in m_CourierRestockMemory.MemoryCache.Keys)
            {
                if (m_CourierRestockMemory.MemoryCache[key] is Memory.ObjectMemory om && om.Context == player)
                    vendors.Add(key);
            }

            foreach (object vendor in vendors)
                m_CourierRestockMemory.Forget(vendor);

            return vendors.Count;
        }

        internal static void ResetAllCourierThrottles(out int cooldownsCleared, out int restocksCleared)
        {
            m_CourierCooldownMemory.TidyMemory();
            m_CourierRestockMemory.TidyMemory();

            cooldownsCleared = m_CourierCooldownMemory.Count;
            restocksCleared = m_CourierRestockMemory.Count;

            m_CourierCooldownMemory.WipeMemory();
            m_CourierRestockMemory.WipeMemory();
        }

        #endregion Courier Unwind Support

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (Deleted || !Alive || m == null || !m.Player || m.Hidden)
                return;

            // Only trigger when the player enters our proximity.
            if (!InRange(m, 4) || InRange(oldLocation, 4))
                return;

            string? text = GetAmbientMessage(m);

            // Nothing to say? Don't start the cooldown.
            if (string.IsNullOrEmpty(text))
                return;

            if (!m_PersonalityMemory.Recall(m))
            {
                m_PersonalityMemory.Remember(m, seconds: Core.Debug ? 10 : 900.0); // 15 minutes
                Say(text);
            }
        }

        #region Messages
        // === HUMAN-CONTROLLED TOWNS ===

        private static readonly string[] OrcSympathizerHumanControl =
        {
            "Keep your voice down. Some of us preferred things as they were.",
            "Not everyone welcomes the return of the old order.",
            "The occupation brought more stability than people admit.",
            "Some changes are harder to undo than others."
        };

        private static readonly string[] HumanSympathizerHumanControl =
        {
            "Things are finally returning to normal.",
            "The rightful order is slowly being restored.",
            "It's good to see honest folk again.",
            "We endured, and now we rebuild."
        };

        private static readonly string[] OrcSpyHumanControl =
        {
            "One hears interesting things from travelers.",
            "The wind carries many secrets these days.",
            "Information is more valuable than gold.",
            "I see much that others do not."
        };

        private static readonly string[] HumanSpyHumanControl =
        {
            "Not everyone is who they appear to be.",
            "Trust is a rare commodity these days.",
            "The occupation has changed many faces.",
            "Keep your wits about you, traveler."
        };


        // === ORC-CONTROLLED TOWNS ===

        private static readonly string[] OrcSympathizerOrcControl =
        {
            "The orcs keep the roads safer than they once were.",
            "Life has improved since the occupation.",
            "Some things are better left unchanged.",
            "Order has been restored under strong rule.",
            "The old chaos is finally gone."
        };

        private static readonly string[] HumanSympathizerOrcControl =
        {
            "There are still friends to be found.",
            "Not all of us welcomed the change.",
            "The night is darkest before the dawn.",
            "Hope remains in quiet hearts.",
            "We bide our time."
        };

        private static readonly string[] OrcSpyOrcControl =
        {
            "Our friends are never far away.",
            "The clans grow stronger every day.",
            "The future belongs to the strong.",
            "Blood and steel will decide our fate."
        };

        private static readonly string[] HumanSpyOrcControl =
        {
            "Keep your eyes open.",
            "Nothing is as permanent as it seems.",
            "The occupiers grow complacent.",
            "Patience is our greatest weapon."
        };

        #endregion Messages

        private static string? GetRandomMessage(string[]? messages)
        {
            if (messages == null || messages.Length == 0)
                return null;

            return messages[Utility.Random(messages.Length)];
        }


        protected virtual string? GetAmbientMessage(Mobile from)
        {
            if (from == null || District == null)
                return null;

            bool orc = IsOrcAligned(from);
            string[]? messages = null;

            if (IsUnderHumanControl)
            {
                switch (Personality)
                {
                    case VendorPersonality.OrcSympathizer:
                        if (orc)
                            messages = OrcSympathizerHumanControl;
                        break;

                    case VendorPersonality.HumanSympathizer:
                        if (!orc)
                            messages = HumanSympathizerHumanControl;
                        break;

                    case VendorPersonality.OrcSpy:
                        if (orc)
                            messages = OrcSpyHumanControl;
                        break;

                    case VendorPersonality.HumanSpy:
                        if (!orc)
                            messages = HumanSpyHumanControl;
                        break;
                }
            }
            else if (IsUnderOrcControl)
            {
                switch (Personality)
                {
                    case VendorPersonality.OrcSympathizer:
                        if (orc)
                            messages = OrcSympathizerOrcControl;
                        break;

                    case VendorPersonality.HumanSympathizer:
                        if (!orc)
                            messages = HumanSympathizerOrcControl;
                        break;

                    case VendorPersonality.OrcSpy:
                        if (orc)
                            messages = OrcSpyOrcControl;
                        break;

                    case VendorPersonality.HumanSpy:
                        if (!orc)
                            messages = HumanSpyOrcControl;
                        break;
                }
            }

            return GetRandomMessage(messages);
        }

        /*
         * Network membership represents a vendor's stable, intrinsic personality
         * (Serial-derived), not its current validity. Whether a registered vendor
         * may participate right now (district membership, etc.) is decided live by
         * the network consumers via ValidForPersonality. This removes any dependency
         * on TerritoryControl districts existing at the moment of registration.
         */
        public static void RegisterPersonality(BaseVendor vendor)
        {
            if (vendor == null || vendor.Deleted)
                return;

            switch (vendor.IntrinsicPersonality)
            {
                case VendorPersonality.OrcSympathizer:
                case VendorPersonality.OrcSpy:
                    if (!m_OrcNetwork.Contains(vendor))
                        m_OrcNetwork.Add(vendor);
                    break;

                case VendorPersonality.HumanSympathizer:
                case VendorPersonality.HumanSpy:
                    if (!m_HumanNetwork.Contains(vendor))
                        m_HumanNetwork.Add(vendor);
                    break;
            }
        }

        public static void UnregisterPersonality(BaseVendor vendor)
        {
            if (vendor == null)
                return;

            m_OrcNetwork.Remove(vendor);
            m_HumanNetwork.Remove(vendor);
        }

        public bool ValidForPersonality
        {
            get
            {
                if (Deleted)
                    return false;

                if (District == null)
                    return false;

                if (Region is HouseRegion)
                    return false;

                if (TemplateCopy)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Mirrors the vendor-type eligibility applied at the RegisterPersonality call sites
        /// (vendors with buy info, plus guildmasters). Used by diagnostics.
        /// </summary>
        public bool PersonalityRegistrationEligible
        {
            get { return HasBuyInfo() || this is BaseGuildmaster; }
        }

        public virtual bool TryGiveReferral(Mobile from, out string message)
        {
            message = null;

            // Not a spy or sympathizer, so allow normal speech processing.
            if (!IsOrcSympathizer && !IsHumanSympathizer)
                return false;

            if (!CanConverse(from))
            {
                message = "We have spoken enough for now.";
                return true;
            }

            // Wrong allegiance: the vendor deliberately refuses.
            if (!CanRequestInformation(from))
            {
                message = "I have nothing to say to you.";
                return true;
            }

            BaseVendor? referral = FindReferral(from);

            if (referral == null)
            {
                message = "I know of no one who can help you.";
                return true;
            }

            message = GetReferralMessage(from, referral);
            return true;
        }

        private static readonly string[] m_InfoKeywords =
        {
            "info",
            "information",
            "rumor",
            "rumors",
            "gossip",
            "news",
            "help",
            "friend",
            "friends",
            "contact",
            "contacts",
            "resistance",
            "rebel",
            "rebels",
            "loyalist",
            "loyalists"
        };

        private static readonly string[] m_InfoPhrases =
        {
            "what do you know",
            "know anything",
            "heard anything",
            "who can i trust",
            "can you help me",
            "know someone",
            "looking for friends",
            "anyone i can trust"
        };

        public virtual bool IsInformationRequest(string speech)
        {
            if (string.IsNullOrWhiteSpace(speech))
                return false;

            speech = speech.ToLowerInvariant();

            foreach (string phrase in m_InfoPhrases)
            {
                if (speech.Contains(phrase))
                    return true;
            }

            foreach (string keyword in m_InfoKeywords)
            {
                if (speech.Contains(keyword))
                    return true;
            }

            return false;
        }

        public virtual bool CanRequestInformation(Mobile from)
        {
            bool orc = IsOrcAligned(from);

            switch (Personality)
            {
                case VendorPersonality.OrcSympathizer:
                case VendorPersonality.OrcSpy:
                    return orc;

                case VendorPersonality.HumanSympathizer:
                case VendorPersonality.HumanSpy:
                    return !orc;

                default:
                    return false;
            }
        }

        protected virtual BaseVendor? FindReferral(Mobile from)
        {
            bool orc = IsOrcAligned(from);

            IReadOnlyList<BaseVendor> network = orc
                ? OrcNetwork
                : HumanNetwork;

            BaseVendor? lastReferral = null;

            Memory.ObjectMemory? om = m_ReferralMemory.Recall(from as object);

            if (om != null)
                lastReferral = om.Context as BaseVendor;

            List<BaseVendor> local = new List<BaseVendor>();
            List<BaseVendor> remote = new List<BaseVendor>();

            foreach (BaseVendor vendor in network)
            {
                if (vendor == null || vendor.Deleted)
                    continue;

                if (vendor.Map != from.Map)
                    continue;

                if (!vendor.ValidForPersonality)
                    continue;

                if (vendor == this)
                    continue;

                if (vendor == lastReferral)
                    continue;

                // Refer the player to a different vendor profession/inventory.
                if (vendor.GetType() == GetType())
                    continue;

                if (vendor.District == District)
                    local.Add(vendor);
                else
                    remote.Add(vendor);
            }

            List<BaseVendor> candidates =
                local.Count > 0 ? local : remote;

            if (candidates.Count == 0)
                return null;

            BaseVendor referral =
                candidates[Utility.Random(candidates.Count)];

            if (om == null)
            {
                m_ReferralMemory.Remember(
                    from,
                    referral,
                    Core.Debug ? 30.0 : 1800.0);
            }
            else
            {
                m_ReferralMemory.Refresh(
                    from,
                    referral,
                    Core.Debug ? 30.0 : 1800.0);
            }

            return referral;
        }

        protected virtual string GetReferralMessage(Mobile from, BaseVendor referral)
        {
            if (referral == null)
                return "I'm afraid I know no one who can help you.";

            string who = $"{referral.Name}, the {referral.Title}";
            string where = referral.District != null
                ? $"in {referral.District.Name}"
                : "nearby";

            switch (Personality)
            {
                case VendorPersonality.HumanSympathizer:
                case VendorPersonality.OrcSympathizer:
                    return Utility.Random(4) switch
                    {
                        0 => $"You might try speaking with {who} {where}.",
                        1 => $"I've heard {who} {where} shares our views.",
                        2 => $"Perhaps {who} {where} can be of assistance.",
                        _ => $"Seek out {who} {where}, but be discreet."
                    };

                case VendorPersonality.HumanSpy:
                case VendorPersonality.OrcSpy:
                    return Utility.Random(4) switch
                    {
                        0 => $"Find {who} {where}. Tell no one I sent you.",
                        1 => $"{who} {where} is one of us.",
                        2 => $"If anyone can help, it's {who} {where}.",
                        _ => $"Speak quietly with {who} {where}. They'll understand."
                    };
            }

            return "I have nothing further to say.";
        }

        protected virtual bool CanConverse(Mobile from)
        {
            if (from == null)
                return false;

            if (m_ConversationMemory.Recall(from))
                return false;

            m_ConversationMemory.Remember(
                from,
                Core.Debug ? 10.0 : 60.0);

            return true;
        }

        #region Courier Delivery

        private bool HandleCourierDelivery(Mobile from, VendorCourierItem courier)
        {
            if (from == null || from.Deleted || courier == null || courier.Deleted)
                return false;

            if (courier.Expired)
            {
                SayTo(from, "You are too late. This is no longer of use.");

                courier.Delete();

                return true;
            }

            if (courier.DestinationVendorSerial != Serial)
            {
                SayTo(from, "This is not meant for me.");

                return false;
            }

            if (courier.CourierSerial != from.Serial)
            {
                SayTo(from, "This was not entrusted to you.");

                return false;
            }

            Item reward = courier.RewardItem;

            if (reward == null || reward.Deleted)
            {
                SayTo(from, "Something is wrong. The promised payment is not here.");

                Console.WriteLine(
                    "Courier reward missing: courier item {0}, player {1} ({2}), destination {3} ({4}), reward serial {5}",
                    courier.Serial,
                    from,
                    from.Serial,
                    this,
                    Serial,
                    courier.RewardItemSerial);

                // cleanup
                courier.Invalidate();

                return true;
            }

            CompleteCourierDelivery(from, courier);

            return true;
        }
        private void CompleteCourierDelivery(
            Mobile player,
            VendorCourierItem courier)
        {
            /*
             * ReleaseReward marks redemption and severs the courier item's
             * ownership before the contract is deleted.
             */
            Item reward = courier.ReleaseReward();

            courier.Delete();

            if (reward == null || reward.Deleted)
            {
                SayTo(player, "Something is wrong. Your payment could not be recovered.");
                return;
            }

            // mark as no linger internal map storage.
            reward.IsIntMapStorage = false;

            if (!player.AddToBackpack(reward))
            {
                reward.MoveToWorld(player.Location, player.Map);
                player.SendMessage("Your reward has been placed at your feet.");
            }

            SayTo(player, Utility.Random(4) switch
            {
                0 => "Good. I was expecting this.",
                1 => "At last! I was beginning to wonder.",
                2 => "You have done well.",
                _ => "Excellent. This arrived safely."
            });
        }

        private static readonly string[] m_CourierKeywords =
        {
            "task",
            "job",
            "delivery"
        };

        public virtual bool IsCourierRequest(string speech)
        {
            if (string.IsNullOrWhiteSpace(speech))
                return false;

            string[] words = speech
                .ToLowerInvariant()
                .Split(new char[]
                {
                    ' ', '\t', '\r', '\n',
                    '.', ',', '!', '?', ';', ':',
                    '\'', '"'
                }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                foreach (string keyword in m_CourierKeywords)
                {
                    if (word == keyword)
                        return true;
                }
            }

            return false;
        }

        #region Courier Speech Intent

        /*
         * Courier speech uses a three-level intent model, following the
         * existing IntelligentDialogue.WhenToSpeakRules pattern:
         *
         *  1. The vendor was named ("Gorman, any work?")         -> respond
         *  2. The whole utterance is a clear courier request      -> respond
         *     ("job", "any job?", "do you have a task?", "I need a job")
         *  3. A courier keyword merely appears in the speech      -> respond only if
         *     ("nice job", "that delivery was late")                 no other player is
         *                                                             close enough to be
         *                                                             the intended listener
         *
         * Ordinary vendors stay silent so the speech falls through to normal
         * handling. Unless named, a sympathizer of the other allegiance also stays
         * silent, so it neither claims the speech before a friendly sympathizer
         * hears it nor reveals itself.
         */

        // Conversational proximity, not audibility (compare NearByExcluding's 5 tiles).
        private const int CourierConversationRange = 5;

        private static readonly char[] m_CourierSpeechSeparators =
        {
            ' ', '\t', '\r', '\n',
            '.', ',', '!', '?', ';', ':',
            '\'', '"'
        };

        private static readonly string[] m_CourierRequestNouns =
        {
            "job", "jobs",
            "task", "tasks",
            "delivery", "deliveries",
            "work"
        };

        // A clear request is one of these prefixes followed only by a request noun.
        private static readonly string[] m_CourierRequestPrefixes =
        {
            "",
            "any",
            "do you have",
            "do you have a",
            "do you have any",
            "i need",
            "i need a",
            "i need some"
        };

        /// <summary>
        /// Handles possible courier speech. Returns true if this vendor responded
        /// (the caller should then mark the speech handled), false to let the
        /// speech fall through.
        /// </summary>
        public bool HandleCourierSpeech(Mobile from, string speech, bool named)
        {
            if (from == null)
                return false;

            if (!IsOrcSympathizer && !IsHumanSympathizer)
                return false;

            bool clearRequest = IsClearCourierRequest(speech);

            if (!clearRequest && !IsCourierRequest(speech))
                return false;

            if (!named && !clearRequest && HasNearbyConversationPartner(from))
                return false;

            if (!named && !CanRequestInformation(from))
                return false;

            TryOfferCourierAssignment(from);
            return true;
        }

        private static bool IsClearCourierRequest(string speech)
        {
            if (string.IsNullOrWhiteSpace(speech))
                return false;

            string[] words = speech
                .ToLowerInvariant()
                .Split(m_CourierSpeechSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return false;

            if (Array.IndexOf(m_CourierRequestNouns, words[words.Length - 1]) < 0)
                return false;

            string prefix = string.Join(" ", words, 0, words.Length - 1);

            return Array.IndexOf(m_CourierRequestPrefixes, prefix) >= 0;
        }

        /// <summary>
        /// True if another living, visible, non-staff player is close enough to
        /// the speaker to plausibly be the one being spoken to.
        /// </summary>
        private static bool HasNearbyConversationPartner(Mobile speaker)
        {
            Map map = speaker.Map;

            if (map == null || map == Map.Internal)
                return false;

            IPooledEnumerable eable = map.GetMobilesInRange(speaker.Location, CourierConversationRange);

            try
            {
                foreach (Mobile m in eable)
                {
                    if (m == speaker || m.Deleted || m is not PlayerMobile)
                        continue;

                    if (!m.Alive || m.Hidden || m.AccessLevel > AccessLevel.Player)
                        continue;

                    return true;
                }
            }
            finally
            {
                eable.Free();
            }

            return false;
        }

        #endregion

        private bool CanOfferCourierAssignment(
            Mobile player,
            out string refusal)
        {
            refusal = null;

            if (player == null || player.Deleted || !player.Alive)
                return false;

            if (player is not PlayerMobile pm)
                return false;

            if (!ValidForPersonality)
                return false;

            if (!IsOrcSympathizer && !IsHumanSympathizer)
            {
                refusal = "I have no work for you.";
                return false;
            }

            if (!CanRequestInformation(player))
            {
                refusal = "I have nothing to discuss with you.";
                return false;
            }

            if (pm.HasActiveCourierAssignment)
            {
                refusal = "You already look occupied. Finish what you have begun.";
                return false;
            }

            if (m_CourierCooldownMemory.Recall(player))
            {
                refusal = "I have nothing for you just now. Come back later.";
                return false;
            }

            if (m_CourierRestockMemory.Recall(this))
            {
                refusal = "My last parcel has already gone out. Come back later.";
                return false;
            }

            if (player.Backpack == null)
            {
                refusal = "You have nowhere to carry what I would entrust to you.";
                return false;
            }

            return true;
        }

        protected internal virtual BaseVendor? FindCourierDestination(Mobile player)
        {
            List<BaseVendor> candidates =
                GetCourierDestinationCandidates(player);

            if (candidates.Count == 0)
                return null;

            return candidates[Utility.Random(candidates.Count)];
        }
        public List<BaseVendor> GetCourierDestinationCandidates(Mobile player)
        {
            List<BaseVendor> sameProfession = new List<BaseVendor>();
            List<BaseVendor> fallback = new List<BaseVendor>();

            if (player == null || player.Deleted)
                return sameProfession;

            bool orcAligned = IsOrcAligned(player);

            IReadOnlyList<BaseVendor> network = orcAligned
                ? OrcNetwork
                : HumanNetwork;

            Type sourceType = GetType();

            foreach (BaseVendor vendor in network)
            {
                if (vendor == null)
                    continue;

                if (vendor.Deleted)
                    continue;

                if (!vendor.ValidForPersonality)
                    continue;

                if (vendor == this)
                    continue;

                if (!(vendor.Map == this.Map && vendor.Map == player.Map))
                    continue;

                Type destinationType = vendor.GetType();

                /*
                 * Prefer vendors in the same inheritance family.
                 *
                 * This naturally groups classes such as Minter : Banker,
                 * while unrelated sibling classes (e.g. Blacksmith and
                 * Weaponsmith) remain distinct.
                 */
                bool related =
                    sourceType.IsAssignableFrom(destinationType) ||
                    destinationType.IsAssignableFrom(sourceType);

                if (related)
                    sameProfession.Add(vendor);
                else
                    fallback.Add(vendor);
            }

            /*
             * Use a same-profession destination whenever possible.
             * If none exist, fall back to any friendly vendor.
             */
            return sameProfession.Count > 0
                ? sameProfession
                : fallback;
        }

        public virtual bool TryOfferCourierAssignment(Mobile player)
        {
            if (!CanOfferCourierAssignment(player, out string refusal))
            {
                if (!string.IsNullOrEmpty(refusal))
                    SayTo(player, refusal);

                return false;
            }

            BaseVendor? destination = FindCourierDestination(player);

            if (destination == null)
            {
                SayTo(player, "I have nothing that requires your attention just now.");
                return false;
            }

            Loot.ImbueLevel rewardLevel =
                CourierRewardCalculator.Calculate(
                    this,
                    destination,
                    player);

            Item reward =
                CourierRewardFactory.CreateReward(destination, rewardLevel);

            reward.MoveToIntStorage();

            string destinationDescription =
                GetCourierDestinationDescription(destination);

            VendorCourierItem courier = new VendorCourierItem(
                player,
                this,
                destination,
                reward,
                rewardLevel,
                TimeSpan.FromHours(12.0),
                CourierAssignmentType.Ledger,
                destinationDescription);

            if (!player.AddToBackpack(courier))
            {
                courier.Delete();

                SayTo(
                    player,
                    "You have no room to carry what I would entrust to you.");

                return false;
            }

            PlayerMobile? pm = player as PlayerMobile;

            if (pm == null || !pm.SetActiveCourierAssignment(courier))
            {
                /*
                 * This should normally be impossible because eligibility was
                 * already checked. Delete the courier to clean up its reward.
                 */
                courier.Delete();

                SayTo(
                    player,
                    "I cannot entrust this to you just now.");

                return false;
            }

            // Assignment successfully handed out: start both throttles (see Courier Throttles).
            m_CourierCooldownMemory.Remember(player, CourierCooldownSeconds);
            m_CourierRestockMemory.Remember(this, player, CourierRestockSeconds);   // context: whose assignment started it

            SayTo(
                player,
                $"Take this to {destinationDescription}. Do not lose it.");

            SayTo(
                player,
                $"The pay is {CourierRewardCalculator.GetPayDescription(rewardLevel)}.");

            return true;
        }

        private static string GetCourierDestinationDescription(BaseVendor vendor)
        {
            if (vendor == null)
                return "an unknown recipient";

            string who;

            if (!string.IsNullOrWhiteSpace(vendor.Title))
                who = $"{vendor.Name}, {vendor.Title}";
            else
                who = vendor.Name;

            if (vendor.District != null &&
                !string.IsNullOrWhiteSpace(vendor.District.Name))
            {
                return $"{who} in {vendor.District.Name}";
            }

            return who;
        }
        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public string CourierRewardTypes
        {
            get
            {
                List<CourierRewardType> types =
                    CourierRewardInventory.GetRewardTypes(this);

                if (types.Count == 0)
                    return CourierRewardType.Gold.ToString();

                return string.Join(", ", types);
            }
        }
    }
}
