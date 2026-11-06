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

/* Scripts\Mobiles\Monsters\Polymorphs\Doppelganger.cs
 * ChangeLog
 *  10/29/2026, Adam
 *      The player history archive (OwnerTools.PlayerHistory.cs, Data\AIPlayerHistory): every Doppelganger is a player of
 *      our shards, past or present, from the archive; no one is copied live. The archive deals them out (well-played
 *      characters tend to come first, no repeats until everyone has had a turn). Dressed from the record (type and hue);
 *      no title (the Debug build's "melee"/"mage" title is gone too); their guild tag ("[ABC]") shows over their name on
 *      click, as a guilded player's does. Loot is inspired by their worn items and
 *      backpack on record (the old records have no backpack: reagents for mages, potions). A record of a player still on
 *      this shard keeps the slain notice and mirror taunt; anyone else is an old-timer, with their own taunts ("Remember
 *      me? I walked these streets in 2018."). [PlayerInfo shows who they were. Version 3 saves the name, sex and year,
 *      version 4 the record's shard and serial; a version 2 Doppelganger takes its name from its live player.
 *  10/28/2026, Adam
 *      Loot rework: nothing the copied player owns is copied any more. Appearance is still an exact copy and the corpse stays
 *      dressed, but worn items are NormalizeOnLift (shard standard when lifted); mismatched hand items are deleted.
 *      Loot is 'inspired by' the player: up to 3 categories found on them (reagents, gems, magery scrolls circle 7 or lower,
 *      potions, weapon and armor types) are made fresh at Brigand/Lich levels (100-250 gold at spawn; the rest at death).
 *      Only vendor-standard weapon/armor/potion types are made; moonstones and stonebags are ignored. 3% keepsake (a
 *      worthless trophy named for the player). The player is told when a player or pet slays their Doppelganger
 *      (once per 10 minutes). Each Doppelganger now keeps its own player (was a shared static: Doppelgangers spawned
 *      together all copied the last one); version 2 saves it. A missing player no longer creates a placeholder PlayerMobile.
 *      Taunts (The Pumpkin King champ only): a Doppelganger that notices a player may taunt them (10%; always when a player
 *      meets their own copy). One taunt per champ every 25 seconds, a player left alone 5 minutes, no line twice in a row.
 *      Staff are taunted too (testing); hidden mobiles are not. Lore taunts: "Jaskar remembers." and "He planted the
 *      seeds. We wear the faces." In The Pumpkin King champ, 5% drop a blessed Halloween cake.
 *  10/12/21, Adam
 *      added [Doppelganger command
 *  9/18/21, Adam
 *      Created.
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Engines.BulkOrders;
using Server.Items;
using Server.Misc;
using Server.Multis.Deeds;
using Server.Multis.StaticHousing;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Mobiles
{
    public class Doppelganger : BasePolymorphic
    {
        public new static void Initialize()
        {
            Server.CommandSystem.Register("Doppelganger", AccessLevel.Owner, new CommandEventHandler(Doppelganger_OnCommand));
        }

        [Usage("Doppelganger <target from> <target to>")]
        [Description("Copies all clothes and features from one mobile to another.")]
        private static void Doppelganger_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.Target = new TargetFrom();
                e.Mobile.SendMessage("Target the mobile to copy from.");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private class TargetFrom : Target
        {
            public TargetFrom()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is Mobile)
                {
                    from.Target = new TargetTo(targ);
                    from.SendMessage("Target the mobile to copy to.");
                }
                else
                {
                    from.SendMessage("That is not a mobile.");
                }
                return;
            }
        }

        private class TargetTo : Target
        {
            Mobile m_from = null;
            public TargetTo(object targ)
                : base(15, false, TargetFlags.None)
            {
                m_from = targ as Mobile;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is Mobile)
                {
                    Mobile? m_to = targ as Mobile;
                    // copy the clothes, jewelry, everything
                    Utility.CopyLayers(m_to, m_from, CopyLayerFlags.Default);

                    // now everything else
                    m_to.Name = m_from.Name;
                    m_to.Hue = m_from.Hue;
                    m_to.Body = (m_from.Female) ? 401 : 400;    // get the correct body
                    m_to.Female = m_from.Female;                // get the correct sex
                    m_to.Title = m_from.Title;                  // get the title
                }
                else
                {
                    from.SendMessage("That is not a mobile.");
                }
                return;
            }
        }

        // this Doppelganger's player comes from the player history archive (no one is copied live). m_PlayerToCopy is that
        //  player only when the record is from this shard and they are still here (slain notice, mirror taunt)
        private PlayerMobile? m_PlayerToCopy;
        private PlayerMobile? PlayerToCopy { get { return m_PlayerToCopy; } }

        #region Player History
        /* Doppelgangers are the players of our shards, from the player history archive (Data\AIPlayerHistory, see
         *  OwnerTools.PlayerHistory.cs). The archive deals them out: well-played characters tend to come first, and no one
         *  repeats until everyone has had a turn. A player of another shard, or one no longer here, is an 'old-timer' (their
         *  own taunts; no slain notice or mirror taunt). What we need to stay dressed and to taunt is saved; the record itself
         *  is looked up again after a restart ([PlayerInfo).
         */
        private PlayerHistory.Record? m_Record;
        private string? m_CopyShard;
        private int m_CopySerial;
        private string? m_CopyName;
        private bool m_CopyFemale;
        private int m_CopyYear;

        // for [PlayerInfo
        public PlayerHistory.Record? History { get { return m_Record ??= PlayerHistory.Find(m_CopyShard, m_CopySerial, m_CopyName); } }

        private bool IsOldTimer { get { return m_CopyName != null && PlayerToCopy == null; } }
        private string? CopyName { get { return m_CopyName; } }
        private bool CopyFemale { get { return m_CopyFemale; } }
        private string CopyLabel { get { return m_CopyName != null ? string.Format("\"{0}\" ({1} {2})", m_CopyName, m_CopyShard, m_CopyYear) : "(no one)"; } }

        private void PickCopy()
        {
            PlayerHistory.Record? r = PlayerHistory.Pick();
            if (r == null)
                return;     // the archive is empty (Data\AIPlayerHistory missing)

            m_Record = r;
            m_CopyShard = r.Shard;
            m_CopySerial = r.Serial;
            m_CopyName = r.Name.Trim();
            m_CopyFemale = r.Female;
            m_CopyYear = r.Year;
            m_PlayerToCopy = PlayerHistory.LiveMatch(r);
        }
        #endregion Player History

        [Constructable]
        public Doppelganger()
            : base()
        {
            SpeechHue = Utility.RandomSpeechHue();

            Fame = 1000;
            Karma = -1000;

            PickCopy();     // a live player, or an old-timer
        }

        public Doppelganger(Serial serial)
            : base(serial)
        {
        }

        public override void InitStats()
        {
            base.InitStats();
        }
        public override void InitBody()
        {
            PlayerHistory.Record? r = m_Record;
            if (r == null)
                return;     // no one to copy (the archive is empty)

            // the clothes, jewelry, everything, made from the record (type and hue)
            Nuke.RestoreLayers(this, r.Layers);

            // make sure they stay on the corpse
            DeNewbieLayers(this);

            // the corpse stays dressed, but anything lifted becomes shard standard (no magic, charges, hue, rare type)
            foreach (Item item in Utility.GetLayerItems(this))
                item.SetItemBool(Item.ItemBoolTable.NormalizeOnLift, true);

            // now everything else
            this.Name = r.Name.Trim();
            this.Hue = r.Hue;
            this.Body = (r.Female) ? 401 : 400;     // get the correct body
            this.Female = r.Female;                 // get the correct death sound
        }
        public override void InitOutfit()
        {
            EnsureBackpack(this);

            Item? oneHanded = FindItemOnLayer(Layer.OneHanded);
            Item? twoHanded = FindItemOnLayer(Layer.TwoHanded);

            BaseWeapon? heldWeapon = (oneHanded as BaseWeapon) ?? (twoHanded as BaseWeapon);
            BaseRanged? heldRanged = heldWeapon as BaseRanged;
            Spellbook? heldSpellbook = (oneHanded as Spellbook) ?? (twoHanded as Spellbook);

            // If VA was not explicitly configured, assign a randomized baseline
            VirtualArmor = (VirtualArmor == 0) ? Utility.RandomMinMax(10, 16) : VirtualArmor;

            // Mapper: (re)Map everything so that we can control the power of the spawn
            switch (AI)
            {
                case AIType.AI_Archer:
                    {
                        break;
                    }
                case AIType.AI_HumanMelee:
                case AIType.AI_Melee:
                    {
                        if (Utility.RandomChance(10))
                            AI = AIType.AI_HumanMelee;
                        else
                            AI = AIType.AI_Melee;

                        break;
                    }
                case AIType.AI_Hybrid:
                case AIType.AI_BaseHybrid:
                    {
                        if (Utility.RandomChance(10))
                            AI = AIType.AI_Hybrid;
                        else
                            AI = AIType.AI_BaseHybrid;

                        break;
                    }

                case AIType.AI_HumanMage:
                case AIType.AI_Mage:
                    {
                        if (Utility.RandomChance(10))
                            AI = AIType.AI_HumanMage;
                        else
                            AI = AIType.AI_Mage;

                        break;
                    }

                default:
                    {
                        System.Diagnostics.Debug.Assert(false);
                        break;
                    }
            }

            switch (AI)
            {
                case AIType.AI_Archer:
                    {
                        if (heldRanged != null)
                        {
                            CleanupHands(oneHanded, twoHanded, heldRanged);
                        }
                        else
                        {
                            CleanupHands(oneHanded, twoHanded, keep: null);
                            AssignArcherWeapon();
                        }
                        break;
                    }

                case AIType.AI_Melee:
                case AIType.AI_HumanMelee:
                case AIType.AI_BaseHybrid:
                case AIType.AI_Hybrid:
                    {
                        if (heldWeapon != null && heldRanged == null)
                        {
                            CleanupHands(oneHanded, twoHanded, heldWeapon);
                        }
                        else
                        {
                            CleanupHands(oneHanded, twoHanded, keep: null);
                            AssignMeleeWeapon();
                        }
                        break;
                    }

                case AIType.AI_Mage:
                case AIType.AI_HumanMage:
                    {
                        if (heldSpellbook != null)
                        {
                            CleanupHands(oneHanded, twoHanded, heldSpellbook);
                        }
                        else
                        {
                            CleanupHands(oneHanded, twoHanded, keep: null);
                            AddItem(new Spellbook());
                        }
                        break;
                    }

                default:
                    {
                        System.Diagnostics.Debug.Assert(false);
                        CleanupHands(oneHanded, twoHanded, keep: null);
                        break;
                    }
            }

            // no title: their guild tag shows over their name (OnSingleClick)
            Title = null;

            if (UsesBandages && !Utility.HasType(this, typeof(Bandage)))
                PackItem(new Bandage(Utility.RandomMinMax(6, 12)), lootType: LootType.UnStealable);

            if (UsesPotions && !Utility.HasType(this, typeof(BasePotion)))
                PackStrongPotions(6, 12);

            if (true /* this really needs to be an ability */ && !Utility.HasType(this, typeof(Pouch)))
                PackItem(new Pouch(), lootType: LootType.UnStealable);
        }

        private void CleanupHands(Item? oneHanded, Item? twoHanded, Item? keep)
        {
            CleanupHandItem(oneHanded, keep);
            CleanupHandItem(twoHanded, keep);
        }

        private void CleanupHandItem(Item? item, Item? keep)
        {
            if (item == null || item == keep)
                return;

            // a copy of the player's item: never loot
            item.Delete();
        }

        private void AssignMeleeWeapon()
        {
            switch (Utility.Random(7))
            {
                case 0: AddItem(new Longsword()); break;
                case 1: AddItem(new Cutlass()); break;
                case 2: AddItem(new Broadsword()); break;
                case 3: AddItem(new Axe()); break;
                case 4: AddItem(new Club()); break;
                case 5: AddItem(new Dagger()); break;
                case 6: AddItem(new Spear()); break;
            }
        }

        private void AssignArcherWeapon()
        {
            switch (Utility.Random(6))
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    AddItem(new Bow());
                    break;
                case 4:
                    AddItem(new Crossbow());
                    break;
                case 5:
                    AddItem(new HeavyCrossbow());
                    break;
            }
        }

        // their guild tag over their name, as a guilded player's shows ("[ABC]")
        public override void OnSingleClick(Mobile from)
        {
            string? abbr = History?.GuildAbbr?.Trim();
            if (!Deleted && !string.IsNullOrEmpty(abbr))
                PrivateOverheadMessage(MessageType.Regular, SpeechHue, true, string.Format("[{0}]", abbr), from.NetState);

            base.OnSingleClick(from);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);
            Effects.SendLocationEffect(c.Location, c.Map, 0x113A, 10, 20);
            Effects.PlaySound(c.Location, c.Map, 0x231);
            NotifySlain();

            // The Pumpkin King champ: 5% Halloween cake (into the corpse: a blessed item in our pack would stay with us)
            if (InTauntChamp() && Utility.Chance(0.05))
                c.DropItem(ThePumpkinKing.MakeHalloweenCake());
        }

        #region Generate Loot
        /* Loot is 'inspired by' the copied player: the categories (and item types) found on them decide what kinds of things
         *  drop, but every item is made fresh by us. Nothing the player owns is ever copied, so loading up a character gains
         *  nothing. Gold drops at spawn (like the Lich); everything else at death.
         */
        private enum LootCategory { Reagent, Gem, Scroll, Potion, Weapon, Armor, Other }
        private const int MaxInspiredPicks = 3;
        private const double KeepsakeChance = 0.03;

        public override void GenerateLoot()
        {
            if (Spawning)
            {
                PackGold(100, 250);
                return;
            }

            LogHelper logger = new LogHelper("DoppelgangerLoot.log", false);
            try
            {
                Dictionary<LootCategory, List<Type?>> found = ScanRecord();
                List<LootCategory> picks = new List<LootCategory>(found.Keys);
                Utility.Shuffle(picks);

                for (int i = 0; i < picks.Count && i < MaxInspiredPicks; i++)
                    GenerateCategory(picks[i], found[picks[i]], logger);

                if (Utility.Chance(KeepsakeChance) && MakeKeepsake() is Item keepsake)
                {
                    PackItem(keepsake);
                    logger.Log(LogType.Text, string.Format("{0}: keepsake '{1}'.", CopyLabel, keepsake.Name));
                }
            }
            finally
            {
                logger.Finish();
            }
        }

        // the categories on the player: what we wear (made from their record) and their backpack on record. A record with
        //  no backpack (the old 2016-2024 files) gets a kit for how we fight (reagents for mages; potions).
        //  A null type means 'any' (a weapon or armor type vendors don't sell: we make a random one instead)
        private static readonly Type[] DefaultKitPotions = { typeof(HealPotion), typeof(CurePotion), typeof(RefreshPotion) };
        private Dictionary<LootCategory, List<Type?>> ScanRecord()
        {
            Dictionary<LootCategory, List<Type?>> found = new();
            foreach (Item item in Utility.GetLayerItems(this))
                ScanType(item.GetType(), found);

            PlayerHistory.Record? r = History;
            if (r != null && !string.IsNullOrEmpty(r.Backpack))
                foreach (Type type in r.BackpackTypes())
                    ScanType(type, found);
            else
            {
                if (AI == AIType.AI_Mage || AI == AIType.AI_HumanMage)
                    foreach (Type type in Loot.RegTypes)
                        AddFound(found, LootCategory.Reagent, type);

                foreach (Type type in DefaultKitPotions)
                    AddFound(found, LootCategory.Potion, type);
            }

            return found;
        }

        // moonstones and stonebags are ignored; containers were flattened when the record was made
        private static void ScanType(Type type, Dictionary<LootCategory, List<Type?>> found)
        {
            if (typeof(Moonstone).IsAssignableFrom(type) || typeof(Stonebag).IsAssignableFrom(type) || typeof(Container).IsAssignableFrom(type))
                return;

            int scroll = Array.IndexOf(Loot.RegularScrollTypes, type);

            if (typeof(BaseReagent).IsAssignableFrom(type) && Array.IndexOf(Loot.RegTypes, type) >= 0)
                AddFound(found, LootCategory.Reagent, type);
            else if (Array.IndexOf(Loot.GemTypes, type) >= 0)
                AddFound(found, LootCategory.Gem, type);
            else if (scroll >= 0 && scroll < 56)                            // magery, circle 7 or lower
                AddFound(found, LootCategory.Scroll, type);
            else if (typeof(BasePotion).IsAssignableFrom(type) && IsVendorStandard(type))
                AddFound(found, LootCategory.Potion, type);
            else if (typeof(BaseWeapon).IsAssignableFrom(type))
                AddFound(found, LootCategory.Weapon, IsVendorStandard(type) ? type : null);
            else if (typeof(BaseArmor).IsAssignableFrom(type))
                AddFound(found, LootCategory.Armor, IsVendorStandard(type) ? type : null);
            else
                AddFound(found, LootCategory.Other, null);
        }

        private static void AddFound(Dictionary<LootCategory, List<Type?>> found, LootCategory category, Type? type)
        {
            if (!found.TryGetValue(category, out List<Type?>? list))
                found[category] = list = new List<Type?>();

            if (!list.Contains(type))
                list.Add(type);
        }

        private static bool IsVendorStandard(Type type)
        {
            return BaseVendor.StandardPricingDictionary.ContainsKey(type);
        }

        private void GenerateCategory(LootCategory category, List<Type?> types, LogHelper logger)
        {
            switch (category)
            {
                case LootCategory.Reagent:
                    {   // 2-3 of their kinds, about 8-12 in all (the Lich's 8-12 reagents)
                        List<Type?> kinds = new List<Type?>(types);
                        Utility.Shuffle(kinds);
                        int count = Math.Min(kinds.Count, Utility.RandomMinMax(2, 3));
                        int total = Utility.RandomMinMax(8, 12);
                        for (int i = 0; i < count; i++)
                            PackFresh(kinds[i], Math.Max(1, total / count), logger);
                        break;
                    }
                case LootCategory.Gem:
                    if (Utility.Chance(0.90))
                        PackFresh(types[Utility.Random(types.Count)], 1, logger);
                    break;
                case LootCategory.Scroll:
                    if (Utility.Chance(0.90))
                        PackFresh(types[Utility.Random(types.Count)], 1, logger);
                    break;
                case LootCategory.Potion:
                    PackFresh(types[Utility.Random(types.Count)], Utility.RandomMinMax(1, 2), logger);
                    break;
                case LootCategory.Weapon:
                    {   // the Lich's PackMagicEquipment(1, 2): 15% magic weapon
                        Type? type = types[Utility.Random(types.Count)];
                        Item? weapon = type != null ? Activator.CreateInstance(type) as Item : Loot.RandomWeapon();
                        if (weapon != null)
                        {
                            if (Utility.Chance(0.15))
                                weapon = Loot.ImbueWeaponOrArmor(weapon, 1, 2);
                            PackItem(weapon);
                            LogFresh(weapon, logger);
                        }
                        break;
                    }
                case LootCategory.Armor:
                    {   // the Lich's PackMagicEquipment(1, 2): 30% magic armor
                        Type? type = types[Utility.Random(types.Count)];
                        Item? armor = type != null ? Activator.CreateInstance(type) as Item : Loot.RandomArmor();
                        if (armor != null)
                        {
                            if (Utility.Chance(0.30))
                                armor = Loot.ImbueWeaponOrArmor(armor, 1, 2);
                            PackItem(armor);
                            LogFresh(armor, logger);
                        }
                        break;
                    }
                case LootCategory.Other:
                    // nothing of theirs: the Lich's 5% magic item
                    PackMagicItem(1, 1, 0.05);
                    break;
            }
        }

        // a fresh item of this type (never the player's): one stack, or separate items if the type does not stack
        private void PackFresh(Type? type, int amount, LogHelper logger)
        {
            if (type == null || amount < 1)
                return;

            Item? item = Activator.CreateInstance(type) as Item;
            if (item == null)
                return;

            if (item.Stackable)
                item.Amount = amount;
            else
                for (int i = 1; i < amount; i++)
                    if (Activator.CreateInstance(type) is Item extra)
                        PackItem(extra);

            PackItem(item);
            LogFresh(item, logger, amount);
        }

        private void LogFresh(Item item, LogHelper logger, int amount = 1)
        {
            logger.Log(LogType.Text, string.Format("{0}: inspired {1} x{2}.", CopyLabel, item.GetType().Name, amount));
        }

        // a worthless, named trophy of the copied player
        private Item? MakeKeepsake()
        {
            string? name = CopyName;
            if (string.IsNullOrEmpty(name))
                return null;

            Item keepsake;
            switch (Utility.Random(5))
            {
                default:
                case 0:
                    keepsake = new Item(0xDF8);
                    keepsake.Name = string.Format("a lock of {0}'s hair", name);
                    keepsake.Hue = FindItemOnLayer(Layer.Hair)?.Hue ?? 0;
                    break;
                case 1:
                    keepsake = new Item(0xE34);
                    keepsake.Name = string.Format("a torn page from {0}'s journal", name);
                    break;
                case 2:
                    keepsake = new Item(0xEED);
                    keepsake.Name = string.Format("{0}'s lucky coin", name);
                    break;
                case 3:
                    keepsake = new Item(0x14F0);
                    keepsake.Name = string.Format("{0}'s calling card", name);
                    break;
                case 4:
                    keepsake = new Item(0x1BD1);
                    keepsake.Name = string.Format("{0}'s quill feather", name);
                    break;
            }

            keepsake.Weight = 1.0;
            keepsake.LootType = LootType.Regular;
            return keepsake;
        }
        #endregion Generate Loot

        #region Slain Notice
        // the copied player is told (once per 10 minutes) when a player, or a player's pet, slays their Doppelganger
        private static readonly Dictionary<Mobile, DateTime> m_LastSlainNotice = new();
        private static readonly TimeSpan SlainNoticeInterval = TimeSpan.FromMinutes(10);

        private void NotifySlain()
        {
            PlayerMobile? pm = PlayerToCopy;
            if (pm == null || pm.Deleted || pm.NetState == null)
                return;

            Mobile? killer = LastKiller ?? FindMostRecentDamager(false);
            if (killer is BaseCreature bc && bc.Controlled && bc.ControlMaster != null)
                killer = bc.ControlMaster;

            if (killer is not PlayerMobile)
                return;

            if (m_LastSlainNotice.TryGetValue(pm, out DateTime last) && DateTime.UtcNow < last + SlainNoticeInterval)
                return;

            m_LastSlainNotice[pm] = DateTime.UtcNow;
            pm.SendMessage("Your Doppelganger has been slain by {0}.", killer.Name);
        }
        #endregion Slain Notice

        #region Taunts
        /* Immersion (The Pumpkin King champ only): a Doppelganger that notices a player may taunt them.
         *  10% per notice (always, for a player meeting their own copy); one taunt per champ every 25 seconds; a taunted
         *  player is left alone for 5 minutes by the whole champ; never the same line twice in a row. Memory only.
         *  {0} = player, {1} = town, {2} = the player we look like, {3} = his/her (that player), {4} = an old-timer's year
         */
        private const string TauntChamp = "ThePumpkinKing";
        private const double TauntChance = 0.10;
        private const int TauntRange = 12;
        private static readonly TimeSpan TauntChampInterval = TimeSpan.FromSeconds(25);
        private static readonly TimeSpan TauntPlayerInterval = TimeSpan.FromMinutes(5);

        private static readonly string[] TauntGeneral =
        {
            "{0}, I see you!",
            "Turn back while you still can.",
            "You should not have come here, {0}.",
            "We have been waiting for you.",
            "Run, little {0}. Run.",
            "Your soul smells of fear.",
            "None who enter leave unchanged.",
            "The Pumpkin King is coming. Beware!",
            "Our King rises, {0}. Kneel or rot.",
            "The harvest is near, and you are the crop.",
            "Can you hear the vines creeping, {0}?",
            "When the King comes, the bones will dance.",
            "Every gourd hides a grin. Every grin, a blade.",
            "Hallow's Eve belongs to the King!",
            "Jaskar remembers.",
            "He planted the seeds. We wear the faces.",
        };
        private static readonly string[] TauntTown =
        {
            "{1} is ours now. You best be going.",
            "{1} will burn by the light of the jack-o'-lanterns.",
        };
        private static readonly string[] TauntFriend =
        {
            "{2} sends {3} regards.",
            "Don't you recognize me, {0}? It's {2}!",
        };
        private static readonly string[] TauntMirror =
        {
            "Look familiar, {0}?",
            "I wear it better, {0}.",
            "Only one of us leaves here, {0}.",
        };
        private static readonly string[] TauntOldTimer =
        {
            "Remember me, {0}? I walked these streets in {4}.",
            "Britain has changed since my day.",
            "I was gone a long time, {0}. The King brought me back.",
            "{4} was a good year, {0}. Then the King found me.",
        };

        private class TauntState
        {
            public DateTime NextTaunt = DateTime.MinValue;
            public Dictionary<Mobile, DateTime> Taunted = new();
            public Dictionary<Mobile, string> LastLine = new();
        }
        private static readonly Dictionary<Item, TauntState> m_TauntStates = new();

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            try
            {
                TryTaunt(m);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        // spawned by a ChampEngine running The Pumpkin King
        private bool InTauntChamp()
        {
            return SpawnerHandle is Server.Engines.ChampionSpawn.ChampEngine champ && !champ.Deleted &&
                string.Equals(champ.DefinitionName, TauntChamp, StringComparison.OrdinalIgnoreCase);
        }

        private void TryTaunt(Mobile m)
        {
            if (!(m is PlayerMobile) || !m.Alive || m.Hidden)    // staff too (testing); hidden staff are unseen
                return;

            if (Deleted || !Alive || Hidden || !InTauntChamp() || !(SpawnerHandle is Server.Engines.ChampionSpawn.ChampEngine champ))
                return;

            if (!m_TauntStates.TryGetValue(champ, out TauntState? state))
                m_TauntStates[champ] = state = new TauntState();

            DateTime now = DateTime.UtcNow;
            if (now < state.NextTaunt)
                return;
            if (state.Taunted.TryGetValue(m, out DateTime last) && now < last + TauntPlayerInterval)
                return;
            if (!InRange(m, TauntRange) || !InLOS(m))
                return;

            bool mirror = m == PlayerToCopy;
            if (!mirror && !Utility.Chance(TauntChance))
                return;

            List<string> lines = new List<string>();
            if (mirror)
                lines.AddRange(TauntMirror);
            else
            {
                lines.AddRange(TauntGeneral);
                if (m.Region is Server.Regions.GuardedRegion town && town.IsGuarded && !string.IsNullOrEmpty(town.Name))
                    lines.AddRange(TauntTown);
                if (CopyName != null)
                    lines.AddRange(TauntFriend);
                if (IsOldTimer)
                    lines.AddRange(TauntOldTimer);
            }

            if (state.LastLine.TryGetValue(m, out string? previous) && lines.Count > 1)
                lines.Remove(previous);

            string line = lines[Utility.Random(lines.Count)];
            string copy = CopyName ?? string.Empty;
            string hisHer = CopyName != null && CopyFemale ? "her" : "his";

            state.NextTaunt = now + TauntChampInterval;
            state.Taunted[m] = now;
            state.LastLine[m] = line;

            Direction = GetDirectionTo(m);
            Say(string.Format(line, m.Name, m.Region?.Name ?? string.Empty, copy, hisHer, m_CopyYear));
        }
        #endregion Taunts

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 4;
            writer.Write(version); // version

            // version 4: the player history record (shard and serial; the name is below)
            writer.Write(m_CopyShard ?? string.Empty);
            writer.Write(m_CopySerial);

            // version 3: the player we copy
            writer.Write(m_CopyName ?? string.Empty);
            writer.Write(m_CopyFemale);
            writer.Write(m_CopyYear);

            // version 2: this Doppelganger's own player
            writer.Write(m_PlayerToCopy);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 4:
                    m_CopyShard = reader.ReadString();
                    if (string.IsNullOrEmpty(m_CopyShard))
                        m_CopyShard = null;
                    m_CopySerial = reader.ReadInt();
                    goto case 3;
                case 3:
                    m_CopyName = reader.ReadString();
                    if (string.IsNullOrEmpty(m_CopyName))
                        m_CopyName = null;
                    m_CopyFemale = reader.ReadBool();
                    m_CopyYear = reader.ReadInt();
                    goto case 2;
                case 2:
                    m_PlayerToCopy = reader.ReadMobile() as PlayerMobile;
                    break;
                case 1:
                    reader.ReadMobile();    // the shared static at save time, not this Doppelganger's player: discarded
                    break;
                case 0:
                    break;
            }
            // no player: no slain notice and no 'inspired' loot (a placeholder PlayerMobile is no longer created)

            // version 2 (copied live): that player is who we are
            if (m_CopyName == null && m_PlayerToCopy != null && !m_PlayerToCopy.Deleted)
            {
                m_CopyName = m_PlayerToCopy.Name;
                m_CopyFemale = m_PlayerToCopy.Female;
                m_CopyShard = PlayerHistory.ThisShard;
                m_CopySerial = m_PlayerToCopy.Serial.Value;
            }
        }
    }
}
