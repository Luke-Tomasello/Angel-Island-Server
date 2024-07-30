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

/*
    Unusual chests and crates in all dungeons: System Overview
    Throughout, I will be referring to ‘chests’, but this also includes the three ‘crate’ types as well.
    •	Just walking by with no other skills will reveal the chest
    •	DH allows you to scan an area by clicking the floor nearby – gives a distance advantage over having to walk up to the chest.
    •	All player-owned “unusual keys” have been converted to three different colors: Red, Blue and Yellow. Red is the rarest, then Blue, then Yellow.
    3% of your keys will have been turned Red, 20% Blue, and the rest Yellow
    •	With the exception of some ‘box’ types, virtually all dungeon crates, and chests are now participating in the system.
    •	Role Play Aspect: These crates were left behind many years ago by pirates, and so, the locks are rusty and will break your key. (keys are single use)
    •	Rares: Yellow chests will Yield a rare maybe 3-5% of the time. Blue 10-15%, and Red ~50%.
    Other goodies will drop as well, scaled by the above mentioned color codes. 
    •	Math facts: as of the time of this writing, there are 689 eligible containers in dungeons.  Of those 689, 136 are ‘unusuals’, or 19.74% of all eligible containers are “unusual”.
    •	Automatic recycling system:
        o	All unusuals have a ~4 hour decay cycle. That is, every 4 hours, these containers despawn, and are replaced with their static counterparts.
        o	When a container is opened, it immediately begins to decay. The decay for an opened container is ~15 minutes. Again, once it decays, it will be replaced with its static counterpart.
        o	This recycling system means the ‘look’ of the dungeon never changes. we don’t add and remove chests.. there will be a constant 689 chests and any given moment.
    •	Key drops have been greatly reduced, so treasure the keys you currently own.
    •	Unusuals cannot be picked and magic does not work on them either. They are not trapped. And no skills are required to open them .. just a key.
    See Also: Scripts\Engines\Spawner\UnusualContainerSpawner.cs
 */

/* Scripts/Items/Containers/Container.cs
 * ChangeLog:
 *  4/22/23, Adam (UnusualChestRules())
 *      For shards other than Angel Island, we simply convert unusual chests to generic dungeon chests.
 *  8/22/22, Adam
 *      Delete unused WallPlaqueAddonDeeds
 *  6/15/22, Adam
 *      In OnDelete we generate an error message of the 'OldCrate' got unexpectedly deleted.
 *      This error can be ignored during a shard wipe.
 *	11/8/21, Adam
 *	    Initial checkin
 */

using Server.Diagnostics;
using Server.Engines.PartySystem;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class BaseUnusualContainer : LockableContainer, IDetectable
    {
        BaseContainer m_oldContainer;
#if DEBUG
        DateTime m_deleteTime = DateTime.UtcNow + TimeSpan.FromMinutes(Utility.RandomMinMax(5, 20));       // quick decay for testing
#else
        DateTime m_deleteTime = DateTime.UtcNow + TimeSpan.FromMinutes(Utility.RandomMinMax(60 * 2, 60 * 6));  // 2-6 hours
#endif
        private int Level
        {
            get
            {
                return
                        KeyValue == Loot.RedKeyValue ? 3 :
                        KeyValue == Loot.BlueKeyValue ? 2 :
                        KeyValue == Loot.YellowKeyValue ? 1 : 0;
            }
        }
        private Timer m_timer = null;
        [CommandProperty(AccessLevel.GameMaster)]
        public Item OldContainer { get { return m_oldContainer; } }
        public BaseUnusualContainer(int GraphicID, BaseContainer c, uint keyValue)
            : base(GraphicID)
        {
            m_oldContainer = c;
            this.Direction = c.Direction;
            this.Location = m_oldContainer.Location;
            this.Movable = m_oldContainer.Movable;
            this.KeyValue = keyValue;
            c.MoveToIntStorage(true);
            m_timer = Timer.DelayCall(m_deleteTime - DateTime.UtcNow, new TimerCallback(TryDelete));
            Fill(this, keyValue);
        }
        public void TryDelete()
        {
            bool mobileNearby = false;
            if (this != null && this.Deleted == false)
            {
                IPooledEnumerable eable = this.GetMobilesInRange(4);
                foreach (Mobile m in eable)
                    if (m is PlayerMobile)
                    {
                        m_timer = null;
                        m_deleteTime = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                        m_timer = Timer.DelayCall(m_deleteTime - DateTime.UtcNow, new TimerCallback(TryDelete));
                        mobileNearby = true;
                        break;
                    }
                eable.Free();
            }

            if (this != null && this.Deleted == false && mobileNearby == false)
                this.Delete();
        }
        public BaseUnusualContainer(Serial serial)
            : base(serial)
        {
        }
        public bool OnDetect(Mobile m)
        {
            if (Core.RuleSets.UnusualChestRules())
                if (m is PlayerMobile && m.Alive && GetDistanceToSqrt(m) <= 8 && Locked)
                {
                    int hue;
                    string text = GetNameText(out hue);
                    this.PublicOverheadMessage(MessageType.Regular, hue, false, text);
                    return true;
                }
            return false;
        }
        public override bool HandlesOnMovement { get { return true; } }
        private Memory m_mobileIgnore = new Memory();
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (Core.RuleSets.UnusualChestRules())
                if (m is PlayerMobile && m.Alive && GetDistanceToSqrt(m) <= 2 && Locked)
                {
                    if (m_mobileIgnore.Recall(m) == false)
                    {
                        m_mobileIgnore.Remember(m, 7);
                        int hue;
                        string text = GetNameText(out hue);
                        this.PublicOverheadMessage(MessageType.Regular, hue, false, text);
                    }
                }
        }
        public override void OnSingleClick(Mobile from)
        {
            if (Core.RuleSets.UnusualChestRules())
            {
                int hue;
                string text = GetNameText(out hue);
                LabelToHued(from, string.Format(text), hue);
            }
            else
                base.OnSingleClick(from);
        }
        private Mobile m_Owner = null;
        public override void Open(Mobile from)
        {
            base.Open(from);
            if (m_unlocked == true)
            {
                m_Owner = from;
                m_unlocked = false; // this is our latch to indicate first and only unlock
                // stop the 4 hour decay, and replace it with a 15 minute decay
                if (m_timer != null && m_timer.Running)
                    m_timer.Stop();
                m_timer = null;
                m_deleteTime = DateTime.UtcNow + TimeSpan.FromMinutes(15);
                m_timer = Timer.DelayCall(m_deleteTime - DateTime.UtcNow, new TimerCallback(TryDelete));
                if (GetType().Name.ToLower().Contains("crate"))
                    from.SendMessage("The crate begins to decay.");
                else
                    from.SendMessage("The chest begins to decay.");

                LogHelper logger = new LogHelper("UnusualContainers.log", World.GetAdminAcct(), false, true);
                logger.Log(LogType.Mobile, from, string.Format("Opened box {0} with keyValue {1:X}", this, this.KeyValue));
                logger.Finish();
            }

        }
        private bool m_unlocked = false;    // not serialized.
        public override bool Locked
        {
            get
            {
                return base.Locked;
            }
            set
            {
                base.Locked = value;
                if (value == false)
                    m_unlocked = true;
            }
        }
        public override void OnDelete()
        {
            if (m_oldContainer != null)
            {
                m_oldContainer.RetrieveItemFromIntStorage(m_oldContainer.Location, this.Map);
            }
            if (m_oldContainer == null || m_oldContainer.Deleted == true)
            {
                Utility.PushColor(ConsoleColor.Red);
                // Adam, 6/15/2022: Not to worry if we are doing a shard wipe
                if (!Core.RuleSets.LoginServerRules())
                    Console.WriteLine("Error: OldCrate unexpectedly deleted.");
                Utility.PopColor();
            }
            base.OnDelete();
        }
        #region lift management
        private List<Item> m_Lifted = new List<Item>();
        private bool CheckLoot(Mobile m, bool criminalAction)
        {
            if (m_Owner == null || m == m_Owner)
                return true;

            Party p = Party.Get(m_Owner);

            if (p != null && p.Contains(m))
                return true;

            Map map = this.Map;

            if (map != null && (map.Rules & MapRules.HarmfulRestrictions) == 0)
            {
                if (criminalAction)
                    m.CriminalAction(true);
                else
                    m.SendLocalizedMessage(1010630); // Taking someone else's treasure is a criminal offense!

                return true;
            }

            m.SendLocalizedMessage(1010631); // You did not discover this chest!
            return false;
        }
        public override bool CheckItemUse(Mobile from, Item item)
        {
            return CheckLoot(from, item != this) && base.CheckItemUse(from, item);
        }
        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {   // Thwart lift macros
            if (LiftMemory.Recall(from))
            {   // throttle
                from.SendMessage("You thrust your hand into the chest but come up empty handed.");
                reject = LRReason.Inspecific;
                return false;
            }
            else
                LiftMemory.Remember(from, 1.8);

            return CheckLoot(from, true) && base.CheckLift(from, item, ref reject);
        }
        public override void OnItemLifted(Mobile from, Item item)
        {
            bool notYetLifted = !m_Lifted.Contains(item);
            BaseCreature witness = null;

            if (notYetLifted)
            {
                // even though unusual chests don't spawn guardians, still leverage them if they 
                //  happen to be in the area.
                if (Utility.RandomDouble() < 0.30 && CheckGuardian(from, out witness))
                {
                    from.SendMessage("You have been discovered stealing pirate booty!");
                    if (from.Hidden)
                        from.RevealingAction();

                    // attack kin to make them come after you
                    from.DoHarmful(witness);
                }
                // adam: we want to reveal the looter about level * 3% of the time (per item looted)
                // for chest levels 1-5, this works out to: 3%, 6%, 9%, 12%, 15%
                else if ((from.Hidden) && (Utility.RandomDouble() < (0.025 * Level)))
                {
                    from.SendMessage("You have been revealed!");
                    from.RevealingAction();
                }
            }

            base.OnItemLifted(from, item);
        }
        public bool CheckThief(Mobile from, out BaseCreature witness)
        {
            // currently we don't implement any kin thief checks
            witness = null;
            return false;
        }
        /*
		 * Check to see if there is a guardian nearby, and if so, provide a good chance
		 * that he will catch you stealing booty. The chance to 'catch you' is based on the number
		 * of monsters near you. Each nearby monster will decrease the chance to catch you by 10%.
		 * This is not much as you start at 100% and it would take 3+ monsters to even give you a small chance at 
		 * slipping past the guardian 'on this lift' <-- Remember, it's a per lift check
		 * Special note on these Unusual Containers: We don't spawn guardians, but since there is likly
		 *  guardians if the area, we'll use them if they are around.
		 */
        private bool CheckGuardian(Mobile from, out BaseCreature witness)
        {
            witness = null;

            if (from == null || !(from is PlayerMobile))
                return false;

            PlayerMobile pm = (PlayerMobile)from;

            // start with a 100% chance to get attacked by pirate
            double chance = 1.0;
            IPooledEnumerable eable = pm.GetMobilesInRange(12);
            foreach (Mobile m in eable)
            {
                if (m == null || !(m is BaseCreature))
                    continue;

                BaseCreature bc = (BaseCreature)m;
                if (bc.Controlled == true || bc.Summoned == true)
                    continue;

                if (from.CanSee(bc) == false)
                    continue;

                // if it carries the Guardian Use property, it's a Guardian
                if (Property.FindUse(bc, Use.IsGuardian))
                    witness = bc;
                else
                    // reduce chance by 10% for each nearby mobile that is not a guardian
                    chance *= .9;
            }
            eable.Free();

            // see if user gets a pass
            if (Utility.RandomDouble() > chance)
                witness = null;

            return witness == null ? false : true;
        }
        #endregion lift management
        #region Serialization
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 2;
            writer.Write(version); // version
            switch (version)
            {
                case 2:
                    {
                        writer.Write(m_Owner);
                        goto case 1;
                    }
                case 1:
                    {
                        writer.WriteDeltaTime(m_deleteTime);
                        writer.Write(m_oldContainer);
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 2:
                    {
                        m_Owner = reader.ReadMobile();
                        goto case 1;
                    }
                case 1:
                    {
                        m_deleteTime = reader.ReadDeltaTime();
                        if (m_deleteTime <= DateTime.UtcNow)
                        {   // if the time has already passed, give them a fresh 15 minute timer
                            m_deleteTime = DateTime.UtcNow + TimeSpan.FromMinutes(15.0);
                            //m_timer = new DeleteTimer(this, m_deleteTime);
                            //m_timer.Start();
                            m_timer = Timer.DelayCall(m_deleteTime - DateTime.UtcNow, new TimerCallback(TryDelete));
                        }
                        else
                        {
                            m_timer = Timer.DelayCall(m_deleteTime - DateTime.UtcNow, new TimerCallback(TryDelete));
                            //m_timer = new DeleteTimer(this, m_deleteTime);
                            //m_timer.Start();
                        }

                        m_oldContainer = (BaseContainer)reader.ReadItem();
                        if (m_oldContainer == null || m_oldContainer.Deleted == true)
                        {
                            Utility.PushColor(ConsoleColor.Red);
                            Console.WriteLine("Error: reading null or deleted OldCrate .");
                            Utility.PopColor();
                        }
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }
        #endregion Serialization
        #region loot

        public void Fill(LockableContainer cont, uint keyValue)
        {
            if (Core.RuleSets.UnusualChestRules())
            {
                #region setup
                int level = Level;

                #endregion setup
                #region RareFactoryItem
                // Rares: Yellow chests will Yield a rare maybe 3-5% of the time. Blue 10-15%, and Red ~50%.
                switch (level)
                {   // determin the loot level
                    case 3: // red
                        {
                            cont.DropItem(Loot.RareFactoryItem(.5, Loot.RareType.UnusualChestDrop));
                            break;
                        }
                    case 2: // blue
                        {
                            cont.DropItem(Loot.RareFactoryItem(.15, Loot.RareType.UnusualChestDrop));
                            break;
                        }
                    case 1: // yellow
                        {
                            cont.DropItem(Loot.RareFactoryItem(.04, Loot.RareType.UnusualChestDrop));
                            break;
                        }
                }
                #endregion RareFactoryItem
                #region gold
                // gold
                for (int ix = 0; ix < Level; ix++)
                    cont.DropItem(new Gold(Utility.RandomMinMax(100 * (level - 1), 100 * (level + 1))));
                #endregion gold
                #region scrolls
                // scrolls
                for (int i = 0; i < level * 3; ++i)
                {
                    int minCircle = level;
                    int maxCircle = (level + 5);
                    PackScroll(cont, minCircle, maxCircle);
                }
                #endregion scrolls
                #region treasuremaps
                // treasuremap
                if (Utility.RandomDouble() < (0.01 * level))
                {
                    int mlevel = level + 2;

                    //	20% chance to get a treasure map one level better than the level of this chest
                    if (Utility.RandomDouble() < 0.20)
                        mlevel += (level < 5) ? 1 : 0;  // bump up the map level by one

                    TreasureMap map = new TreasureMap(mlevel, Map.Felucca);
                    cont.DropItem(map);             // drop it baby!
                }
                #endregion treasuremaps
                #region floor tiles
                // something new! Players like special fishing nets for floor deco, so let's give them something worthwhile
                Bag bag = new Bag();
                // 0xDCA is the special fishing net graphic, the others are 'woven mat'
                int itemId = Utility.RandomList(0xDCA, 0x11E6, 0x11E7, 0x11E8, 0x11E9);
                int hue = Utility.RandomCraftMetalHue();     // all floor tiles will be the same color (helps players understand the utility of them.)
                for (int ix = 0; ix < level * 3; ix++)
                {
                    Item floorTile = new Item(itemId);
                    floorTile.Hue = hue;
                    floorTile.Name = "floor tile";
                    floorTile.Weight = 1;
                    bag.DropItem(floorTile);
                }
                cont.DropItem(bag);
                #endregion floor tiles
                #region musushroos
                for (int ix = 0; ix < level; ix++)
                {
                    switch (ix)
                    {
                        case 0: cont.DropItem(new PointyMushroom()); break;
                        case 1: cont.DropItem(new FlatMushroom()); break;
                        case 2: cont.DropItem(new RedMushroom()); break;
                    }
                }
                cont.DropItem(new WhiteMushroom());
                #endregion musushroos
                #region wall signs
                // wall signs - there's probably a better way to do this
                int[,] a = new int[56, 2] {
                    { 0xB95, 0xB96 }, { 0xC43, 0xC44 },
                    { 0xBA3, 0xBA4 },{ 0xBA5, 0xBA6 },{ 0xBA7, 0xBA8 },{ 0xBA9, 0xBAA },{ 0xBAB, 0xBAC },{ 0xBAD, 0xBAE },{ 0xBAF, 0xBB0 },{ 0xBB1, 0xBB2 },
                    { 0xBB3, 0xBB4 },{ 0xBB5, 0xBB6 },{ 0xBB7, 0xBB8 },{ 0xBB9, 0xBBA },{ 0xBBB, 0xBBC },{ 0xBBD, 0xBBE },{ 0xBBF, 0xBC0 },{ 0xBC1, 0xBC2 },
                    { 0xBC3, 0xBC4 },{ 0xBC5, 0xBC6 },{ 0xBC7, 0xBC8 },{ 0xBC9, 0xBCA },{ 0xBCB, 0xBCC },{ 0xBCD, 0xBCE },{ 0xBCF, 0xBD0 },{ 0xBD1, 0xBD2 },
                    { 0xBD3, 0xBD4 },{ 0xBD5, 0xBD6 },{ 0xBD7, 0xBD8 },{ 0xBD9, 0xBDA },{ 0xBDB, 0xBDC },{ 0xBDD, 0xBDE },{ 0xBDF, 0xBE0 },{ 0xBE1, 0xBE2 },
                    { 0xBE3, 0xBE4 },{ 0xBE5, 0xBE6 },{ 0xBE7, 0xBE8 },{ 0xBE9, 0xBEA },{ 0xBEB, 0xBEC },{ 0xBED, 0xBEE },{ 0xBEF, 0xBF0 },{ 0xBF1, 0xBF2 },
                    { 0xBF3, 0xBF4 },{ 0xBF5, 0xBF6 },{ 0xBF7, 0xBF8 },{ 0xBF9, 0xBFA },{ 0xBFB, 0xBFC },{ 0xBFD, 0xBFE },{ 0xBFF, 0xC00 },{ 0xC01, 0xC02 },
                    { 0xC03, 0xC04 },{ 0xC05, 0xC06 },{ 0xC07, 0xC08 },{ 0xC09, 0xC0A },{ 0xC0B, 0xC0C },{ 0xC0D, 0xC0E }
                };

                int tindex = a.Length / 2;
                tindex = Utility.Random(tindex);
                var south = a[tindex, 0];
                var east = a[tindex, 1];
                WallPlaqueAddonDeed deed;
                if (Utility.RandomBool())
                    deed = new WallPlaqueAddonDeed(south, Direction.South);
                else
                    deed = new WallPlaqueAddonDeed(east, Direction.East);
                if (level > 1)
                {
                    if (Utility.RandomBool())
                        cont.DropItem(deed);
                    else
                        deed.Delete();
                }
                else if (Utility.Random(5) == 0)
                    cont.DropItem(deed);
                else
                    deed.Delete();
                #endregion wall signs
                #region seasonal
                // snow piles
                if (Utility.DayMonthCheck(12, 1, 30, Utility.DayMonthType.Surrounding))
                {   // December 1st +- 30 days (inclusive)
                    for (int ix = 0; ix < level; ix++)
                        cont.DropItem(new SnowPile());
                }
                // fireworks wands
                if (Utility.DayMonthCheck(7, 4, 3, Utility.DayMonthType.Before))
                {   // july 4th - 3 days (inclusive)
                    for (int ix = 0; ix < level; ix++)
                        cont.DropItem(new FireworksWand());
                }

                #endregion seasonal
                #region misc
                TreasureMapChest.PackRegs(cont, level * 10);
                TreasureMapChest.PackGems(cont, level * 5);
                #endregion misc
            }
            else
            {   // on regular shards, we simply convert the loot to an equivalent dungeon chest
                //  there are no unusual keys, and no OnDetect processing. 
                int level = Level == 0 ? 1 : Level;
                DungeonTreasureChest.InitializeTrap(cont, level);
                DungeonTreasureChest.Fill(cont, level);
            }
        }
        #endregion loot
        #region Utils
        private string GetNameText(out int hue)
        {
            string type = "";
            if (this.GetType().Name.ToLower().Contains("chest"))
                type = "chest";
            else
                type = "crate";
            hue = 0;
            if (m_oldContainer == null)
                return string.Format("an unusual {0} (unlinked)", type);
            else if (KeyValue == Loot.RedKeyValue || KeyValue == Loot.BlueKeyValue || KeyValue == Loot.YellowKeyValue)
            {
                hue = Loot.KeyHueLookup[KeyValue];
                return string.Format("an unusual {0}", type);
            }
            else
                return string.Format("an unusual {0} (not keyed)", type);
        }
        public static void PackScroll(LockableContainer cont, int minCircle, int maxCircle)
        {
            PackScroll(cont, Utility.RandomMinMax(minCircle, maxCircle));
        }

        public static void PackScroll(LockableContainer cont, int circle)
        {
            int min = (circle - 1) * 8;

            cont.DropItem(Loot.RandomScroll(min, min + 7, SpellbookType.Regular));
        }

        public static void PackMagicItem(LockableContainer cont, int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            Item item = Loot.RandomClothingOrJewelry(must_support_magic: true);

            if (item == null)
                return;

            if (item is BaseClothing)
                ((BaseClothing)item).SetRandomMagicEffect(minLevel, maxLevel);
            else if (item is BaseJewel)
                ((BaseJewel)item).SetRandomMagicEffect(minLevel, maxLevel);

            cont.DropItem(item);
        }
        #endregion Utils
    }

    #region supported chest types
    [Flipable(0x9A9, 0xE7E)]
    public class UnusualSmallCrate : BaseUnusualContainer
    {
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds { get { return new Rectangle2D(20, 10, 150, 90); } }
        public UnusualSmallCrate(BaseContainer c, uint keyValue)
            : base(0x9A9, c, keyValue)
        {
            Weight = 2;
        }
        public UnusualSmallCrate(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    [Flipable(0xE3F, 0xE3E)]
    public class UnusualMediumCrate : BaseUnusualContainer
    {
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds { get { return new Rectangle2D(20, 10, 150, 90); } }
        public UnusualMediumCrate(BaseContainer c, uint keyValue)
            : base(0xE3F, c, keyValue)
        {
            Weight = 2;
        }
        public UnusualMediumCrate(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    [FlipableAttribute(0xe3c, 0xe3d)]
    public class UnusualLargeCrate : BaseUnusualContainer
    {
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds { get { return new Rectangle2D(20, 10, 150, 90); } }
        public UnusualLargeCrate(BaseContainer c, uint keyValue)
            : base(0xe3c, c, keyValue)
        {
            Weight = 2;
        }
        public UnusualLargeCrate(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    [Flipable(0xe43, 0xe42)]
    public class UnusualWoodenChest : BaseUnusualContainer
    {
        public override int DefaultGumpID { get { return 0x49; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds { get { return new Rectangle2D(18, 105, 144, 73); } }
        public UnusualWoodenChest(BaseContainer c, uint keyValue)
            : base(0xe43, c, keyValue)
        {
            Weight = 2;
        }
        public UnusualWoodenChest(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    [Flipable(0xE41, 0xE40)]
    public class UnusualMetalGoldenChest : BaseUnusualContainer
    {
        public override int DefaultGumpID { get { return 0x42; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds { get { return new Rectangle2D(18, 105, 144, 73); } }
        public UnusualMetalGoldenChest(BaseContainer c, uint keyValue)
            : base(0xE41, c, keyValue)
        {
            Weight = 2;
        }
        public UnusualMetalGoldenChest(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    [Flipable(0x9AB, 0xE7C)]
    public class UnusualMetalChest : BaseUnusualContainer
    {
        public override int DefaultGumpID { get { return 0x4A; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override Rectangle2D Bounds { get { return new Rectangle2D(18, 105, 144, 73); } }
        public UnusualMetalChest(BaseContainer c, uint keyValue)
            : base(0x9AB, c, keyValue)
        {
            Weight = 2;
        }
        public UnusualMetalChest(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
    #endregion supported chest types
    #region BagOfKeys
    public class BagOfKeys : Bag
    {
        [Constructable]
        public BagOfKeys()
            : this(10)
        {
        }

        [Constructable]
        public BagOfKeys(int amount)
        {
            for (int ix = 0; ix < amount; ix++)
            {
                Key key = new Key(KeyType.Magic);
                key.Hue = Loot.KeyHueLookup[Loot.RedKeyValue];
                key.KeyValue = Loot.RedKeyValue;
                key.Name = "an unusual key";
                DropItem(key);

                key = new Key(KeyType.Magic);
                key.Hue = Loot.KeyHueLookup[Loot.BlueKeyValue];
                key.KeyValue = Loot.BlueKeyValue;
                key.Name = "an unusual key";
                DropItem(key);

                key = new Key(KeyType.Magic);
                key.Hue = Loot.KeyHueLookup[Loot.YellowKeyValue];
                key.KeyValue = Loot.YellowKeyValue;
                key.Name = "an unusual key";
                DropItem(key);
            }
        }

        public BagOfKeys(Serial serial)
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
    }
    #endregion BagOfKeys
}