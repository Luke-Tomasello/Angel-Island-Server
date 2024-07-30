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

/* Scripts/Items/Misc/Corpses/Corpse.cs
 * ChangeLog
 *  5/30/2024, Replace various elemental corpse graphics with that of a slime.
 *      elemental graphics seem to be bugged (verified in RunUO) such that the click range for the corpse is much larger than the actual corpse.
 *  6/7/2020, Yoar & Adam
 *      Replace the code for SendInfoTo() to updated RunUO.
 *          Fixes a bug with OSI clients getting the correct corpse info.
 *  6/7/23, Yoar (CorpseContent6017)
 *      Added CorpseContent6017 packet to support 6.0.1.7 clients
 *  9/27/21, Adam (IsGuardIgnore())
 *      Call IsGuardIgnore() when looting to ensure looters are not criminal when looting
 *  8/5/21, Adam
 *      m_StaticCorpse was not being serialized, so after a server restart, once static corpses were lootable.
 *      Resolution: serialize m_StaticCorpse
 *	3/6/0, Adam,
 *		Add a notion of FriendlyFire so that we can track when a player was killed by a shared account
 *		(so we can deny a bounty)
 *	4/9/09, Adam
 *		Add CheckLooting() to check the region rules re looting. 
 *	2/16/08, Pix
 *		Added call to DoSpecialContainerUpdateForEquippingCorpses() function.
 *   06/29/06, Kit
 *		removed previous check for custom mobile name, now handled by templates
 *		Updated to work with name templated names
 *	03/16/06, weaver
 *		Added check for custom mobile name to GetCorpseName().
 *  10/06/06 Taran Kain
 *		Added check for StaticCorpse in Carve() to prevent carving up static corpses.
 *  09/06/05 Taran Kain
 *		Added StaticCorpse property and check in CheckItemUse, to see if we want this corpse lootable
 *	2/17/05, mith
 *		Changed inheritance from Container to BaseContainer to fix ownership bugs in 1.0.0
 *  11/26/04, Froste
 *      Reverted previous fix due to a bug with pets dropping fresh loot on every death, temporary measure
 *  11/16/04, Froste
 *      Commented out the line that was preventing bonded packies from dropping their pack when they die
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *  5/20/04, Pixie
 *		Changed so the carved head records the information for the bounty system.
 *	5/2/04, mith
 *		Modified TurnToBones() to make bones Criminal upon decaying to bones. This doesn't affect the player's criminality, only whether or not it's a criminal action to loot bones.
 *		It's still criminal to loot a corpse, just not bones.
 */

using Server.ContextMenus;
using Server.Engines.PartySystem;
using Server.Engines.Quests;
using Server.Engines.Quests.Doom;
using Server.Engines.Quests.Haven;
using Server.Guilds;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public class Corpse : BaseContainer, ICarvable
    {
        private Mobile m_Owner;             // Whos corpse is this?
        private Mobile m_Killer;                // Who killed the owner?
        private bool m_Carved;              // Has this corpse been carved?

        private ArrayList m_Looters;                // Who's looted this corpse?
        private ArrayList m_EquipItems;         // List of items equiped when the owner died. Ingame, these items display /on/ the corpse, not just inside
        private ArrayList m_Aggressors;         // Anyone from this list will be able to loot this corpse; we attacked them, or they attacked us when we were freely attackable

        private string m_CorpseName;            // Value of the CorpseNameAttribute attached to the owner when he died -or- null if the owner had no CorpseNameAttribute; use "the remains of ~name~"
        private bool m_NoBones;             // If true, this corpse will not turn into bones

        private bool m_VisitedByTaxidermist;    // Has this corpse yet been visited by a taxidermist?
        private bool m_Channeled;           // Has this corpse yet been used to channel spiritual energy? (AOS Spirit Speak)
        private bool m_Reanimated;

        // For notoriety:
        private AccessLevel m_AccessLevel;          // Which AccessLevel the owner had when he died
        private Guild m_Guild;              // Which Guild the owner was in when he died
        private int m_Kills;                // How many kills the owner had when he died
        private bool m_Criminal;                // Was the owner criminal when he died?

        private DateTime m_TimeOfDeath;         // What time was this corpse created?

        private bool m_StaticCorpse;            // Is this a static corpse? (DeadMiner/DeadGuard/etc)

        public static readonly TimeSpan MonsterLootRightSacrifice = TimeSpan.FromMinutes(2.0);

        [CommandProperty(AccessLevel.GameMaster)]
        public bool StaticCorpse
        {
            get { return m_StaticCorpse; }
            set { m_StaticCorpse = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TimeOfDeath
        {
            get { return m_TimeOfDeath; }
            set { m_TimeOfDeath = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Carved
        {
            get { return m_Carved; }
            set { m_Carved = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool VisitedByTaxidermist
        {
            get { return m_VisitedByTaxidermist; }
            set { m_VisitedByTaxidermist = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Channeled
        {
            get { return m_Channeled; }
            set { m_Channeled = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Reanimated
        {
            get { return m_Reanimated; }
            set { m_Reanimated = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AccessLevel AccessLevel
        {
            get { return m_AccessLevel; }
        }

        public ArrayList Aggressors
        {
            get { return m_Aggressors; }
        }

        public ArrayList Looters
        {
            get { return m_Looters; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Killer
        {
            get { return m_Killer; }
        }

        public ArrayList EquipItems
        {
            get { return m_EquipItems; }
        }

        public Guild Guild
        {
            get { return m_Guild; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Kills
        {
            get { return m_Kills; }
            set { m_Kills = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Criminal
        {
            get { return m_Criminal; }
            set { m_Criminal = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
        }

        public void TurnToBones()
        {
            if (Deleted)
                return;

            ProcessDelta();
            SendRemovePacket();
            ItemID = Utility.Random(0xECA, 9); // bone graphic
            Hue = 0;
            ProcessDelta();

            m_NoBones = true;
            this.Criminal = true;
            BeginDecay(m_BoneDecayTime);

            /*DecayedCorpse c = new DecayedCorpse( Name );

			c.MoveToWorld( Location, Map );

			ArrayList list = Items;

			for ( int i = list.Count - 1; i >= 0; --i )
			{
				if ( i < list.Count )
					c.AddItem( (Item)list[i] );
			}

			Delete();*/
        }

        private static TimeSpan m_DefaultDecayTime = TimeSpan.FromMinutes(7.0);
        private static TimeSpan m_BoneDecayTime = TimeSpan.FromMinutes(7.0);

        private Timer m_DecayTimer;
        private DateTime m_DecayTime;

        private bool m_FriendlyFire = false;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool FriendlyFire { get { return m_FriendlyFire; } }

        public void BeginDecay(TimeSpan delay)
        {
            if (m_DecayTimer != null)
                m_DecayTimer.Stop();

            m_DecayTime = DateTime.UtcNow + delay;

            m_DecayTimer = new InternalTimer(this, delay);
            m_DecayTimer.Start();
        }

        public override void OnAfterDelete()
        {
            if (m_DecayTimer != null)
                m_DecayTimer.Stop();

            m_DecayTimer = null;
        }

        private class InternalTimer : Timer
        {
            private Corpse m_Corpse;

            public InternalTimer(Corpse c, TimeSpan delay)
                : base(delay)
            {
                m_Corpse = c;
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                if (!m_Corpse.m_NoBones)
                    m_Corpse.TurnToBones();
                else
                    m_Corpse.Delete();
            }
        }

        public static string GetCorpseName(Mobile m)
        {
            Type t = m.GetType();

            object[] attrs = t.GetCustomAttributes(typeof(CorpseNameAttribute), true);

            if (attrs != null && attrs.Length > 0)
            {
                CorpseNameAttribute attr = attrs[0] as CorpseNameAttribute;

                if (attr != null)
                {
                    if (m is BaseCreature)
                    {
                        // Does it have a spawner?
                        if (((BaseCreature)m).Spawner != null)
                        {
                            // Do we have a custom name we need to use?
                            Spawner sp = ((BaseCreature)m).Spawner;

                            if (sp.TemplateEnabled && sp.TemplateMobile != null)
                                // Let the system catch and handle this
                                return null;
                        }
                    }
                    return attr.Name;
                }
            }

            return null;
        }

        public new static void Initialize()
        {
            Mobile.CreateCorpseHandler += new CreateCorpseHandler(Mobile_CreateCorpseHandler);
        }

        public static Container Mobile_CreateCorpseHandler(Mobile owner, ArrayList initialContent, ArrayList equipItems)
        {
            bool shouldFillCorpse = true;

            //if ( owner is BaseCreature )
            //	shouldFillCorpse = !((BaseCreature)owner).IsBonded;

            //if ( owner is BaseCreature )
            //	shouldFillCorpse = !((BaseCreature)owner).IsBonded;

            Corpse c;
            if (owner is MilitiaFighter)
                c = new MilitiaFighterCorpse(owner, shouldFillCorpse ? equipItems : new ArrayList());
            else
                c = new Corpse(owner, shouldFillCorpse ? equipItems : new ArrayList());

            if (shouldFillCorpse)
            {
                for (int i = 0; i < initialContent.Count; ++i)
                {
                    Item item = (Item)initialContent[i];

                    if (Core.RuleSets.AOSRules() && owner.Player && item.Parent == owner.Backpack)
                        c.AddItem(item);
                    else
                        c.DropItem(item);

                    if (owner.Player && Core.RuleSets.AOSRules())
                        c.SetRestoreInfo(item, item.Location);
                }
            }
            else
            {
                c.Carved = true; // TODO: Is it needed?
            }

            Point3D loc = owner.Location;
            Map map = owner.Map;

            if (map == null || map == Map.Internal)
            {
                loc = owner.LogoutLocation;
                map = owner.LogoutMap;
            }

            c.MoveToWorld(loc, map);

            return c;
        }

        public override bool IsPublicContainer { get { return true; } }

        public override int DefaultGumpID { get { return 0x9; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(20, 85, 104, 111); }
        }
        private static Body GetCorpseBody(Mobile owner)
        {
            int body = owner.BodyValue;
            switch (body)
            {
                case 0x10:  /*water elemental*/
                case 0x9E:  /*acid/toxic elemental*/
                case 0xF:   /*fire elemental*/
                case 13:    /*an air elemental*/
                case 159:   /*a blood elemental*/
                case 131:   /*an efreet*/
                case 162:   /*a poison elemental*/
                case 163:   /*a snow elemental*/
                    return new Body(0x33);  // slime - these guys have a messed up corpse.. the click rect is like 4 square tiles

                default:
                    return owner.Body;      // standard corpse mechanism
            }
        }
        private static int GetCorpseHue(Mobile owner)
        {
            switch (owner.BodyValue)
            {
                case 0x10:  /*water elemental*/
                    {
                        return Utility.RandomSpecialHue(Utility.ColorSelect.Blue, owner.GetType().Name);
                    }
                case 0x9E:  /*acid/toxic elemental*/
                    {
                        return Utility.RandomSpecialHue(Utility.ColorSelect.Green, owner.GetType().Name);
                    }
                case 0xF:   /*fire elemental*/
                    {
                        return Utility.RandomSpecialHue(Utility.ColorSelect.Gold, owner.GetType().Name);
                    }
                case 13:    /*an air elemental*/
                    {
                        return Utility.RandomSpecialHue(Utility.ColorSelect.ShadowIron, owner.GetType().Name);
                    }
                case 159:   /*a blood elemental*/
                    {
                        return Utility.RandomSpecialHue(Utility.ColorSelect.Red, owner.GetType().Name);
                    }
                case 131:   /*an efreet*/
                    {
                        return Utility.RandomSpecialHue(Utility.ColorSelect.Red, owner.GetType().Name);
                    }
                case 162:   /*a poison elemental*/
                    {
                        return Utility.RandomSpecialHue(Utility.ColorSelect.Green, owner.GetType().Name);
                    }
                case 163:   /*a snow elemental*/
                    {
                        return 0x47E;           // true white;
                    }

                default:
                    return owner.Hue;
            }
        }
        public Corpse(Mobile owner, ArrayList equipItems)
            : base(0x2006)
        {
            // special corpses are some of the elementals, like water elemental. The client believes the corpse is much bigger than it is.
            //  So we replace the elemental corpse with a slime corps and hue it accordingly
            Stackable = true;               // To suppress console warnings, stackable must be true
            Amount = GetCorpseBody(owner);  // protocol defines that for itemid 0x2006, amount=body
            Stackable = false;

            Movable = false;
            Hue = GetCorpseHue(owner);
            Direction = owner.Direction;
            Name = owner.Name;

            m_Owner = owner;

            m_CorpseName = GetCorpseName(owner);

            m_TimeOfDeath = DateTime.UtcNow;

            m_AccessLevel = owner.AccessLevel;
            m_Guild = owner.Guild as Guild;
            m_Kills = owner.LongTermMurders;
            m_Criminal = owner.Criminal;

#if false
			// This corpse does not turn to bones if:
			//    (the owner is not a player) and (the owner doesn't have a human body)
			m_NoBones = !owner.Player && !owner.Body.IsHuman;
#else
            // This corpse does not turn to bones if:
            //    (the owner is not a player)
            m_NoBones = !owner.Player;
#endif

            m_Looters = new ArrayList();
            m_EquipItems = equipItems;

            m_Aggressors = new ArrayList(owner.Aggressors.Count + owner.Aggressed.Count);
            bool addToAggressors = !(owner is BaseCreature);

            TimeSpan lastTime = TimeSpan.MaxValue;

            for (int i = 0; i < owner.Aggressors.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)owner.Aggressors[i];

                if ((DateTime.UtcNow - info.LastCombatTime) < lastTime)
                {
                    m_Killer = info.Attacker;
                    lastTime = (DateTime.UtcNow - info.LastCombatTime);
                }

                if (addToAggressors && !info.CriminalAggression)
                    m_Aggressors.Add(info.Attacker);
            }

            for (int i = 0; i < owner.Aggressed.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)owner.Aggressed[i];

                if ((DateTime.UtcNow - info.LastCombatTime) < lastTime)
                {
                    m_Killer = info.Defender;
                    lastTime = (DateTime.UtcNow - info.LastCombatTime);
                }

                if (addToAggressors)
                    m_Aggressors.Add(info.Defender);
            }

            // Adam: Now that we have the killer, see if the Killer is on a shared account with the player killed
            //	we will pass this information along to the bounty system to prevent bounty farming by the murderer.
            if (m_Killer != null)
            {   // no need to serialize this value as it's only good as long as the corpse lasts
                m_FriendlyFire = BountySystem.BountyKeeper.SharedAccount(m_Owner, m_Killer);
            }

            if (!addToAggressors)
            {
                BaseCreature bc = (BaseCreature)owner;

                if (bc.Controlled && bc.ControlMaster != null)
                    m_Aggressors.Add(bc.ControlMaster);
                else if (bc.Summoned && bc.SummonMaster != null)
                    m_Aggressors.Add(bc.SummonMaster);

                List<DamageStore> rights = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);
                for (int i = 0; i < rights.Count; ++i)
                {
                    DamageStore ds = (DamageStore)rights[i];

                    if (ds.m_HasRight)
                        m_Aggressors.Add(ds.m_Mobile);
                }
            }

            BeginDecay(m_DefaultDecayTime);
        }

        public Corpse(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)11); // version

            writer.Write(m_StaticCorpse);

            writer.WriteDeltaTime(m_TimeOfDeath);

            ArrayList list = (m_RestoreTable == null ? null : new ArrayList(m_RestoreTable));
            int count = (list == null ? 0 : list.Count);

            writer.Write(count);

            for (int i = 0; list != null && i < list.Count; ++i)
            {
                DictionaryEntry de = (DictionaryEntry)list[i];
                Item item = (Item)de.Key;
                Point3D loc = (Point3D)de.Value;

                writer.Write(item);

                if (item.Location == loc)
                {
                    writer.Write(false);
                }
                else
                {
                    writer.Write(true);
                    writer.Write(loc);
                }
            }

            writer.Write(m_VisitedByTaxidermist);

            writer.Write(m_DecayTimer != null);

            if (m_DecayTimer != null)
                writer.WriteDeltaTime(m_DecayTime);

            writer.WriteMobileList(m_Looters);
            writer.Write(m_Killer);

            writer.Write((bool)m_Carved);

            writer.WriteMobileList(m_Aggressors);

            writer.Write(m_Owner);

            writer.Write(m_NoBones);

            writer.Write((string)m_CorpseName);

            writer.Write((int)m_AccessLevel);
            writer.Write((Guild)m_Guild);
            writer.Write((int)m_Kills);
            writer.Write((bool)m_Criminal);

            writer.Write((int)m_EquipItems.Count);

            for (int i = 0; i < m_EquipItems.Count; ++i)
                writer.Write((Item)m_EquipItems[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {

                case 11:
                    {
                        m_StaticCorpse = reader.ReadBool();

                        goto case 10;
                    }
                case 10:
                    {
                        m_TimeOfDeath = reader.ReadDeltaTime();

                        goto case 9;
                    }
                case 9:
                    {
                        int count = reader.ReadInt();

                        for (int i = 0; i < count; ++i)
                        {
                            Item item = reader.ReadItem();

                            if (reader.ReadBool())
                                SetRestoreInfo(item, reader.ReadPoint3D());
                            else if (item != null)
                                SetRestoreInfo(item, item.Location);
                        }

                        goto case 8;
                    }
                case 8:
                    {
                        m_VisitedByTaxidermist = reader.ReadBool();

                        goto case 7;
                    }
                case 7:
                    {
                        if (reader.ReadBool())
                            BeginDecay(reader.ReadDeltaTime() - DateTime.UtcNow);

                        goto case 6;
                    }
                case 6:
                    {
                        m_Looters = reader.ReadMobileList();
                        m_Killer = reader.ReadMobile();

                        goto case 5;
                    }
                case 5:
                    {
                        m_Carved = reader.ReadBool();

                        goto case 4;
                    }
                case 4:
                    {
                        m_Aggressors = reader.ReadMobileList();

                        goto case 3;
                    }
                case 3:
                    {
                        m_Owner = reader.ReadMobile();

                        goto case 2;
                    }
                case 2:
                    {
                        m_NoBones = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        m_CorpseName = reader.ReadString();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 10)
                            m_TimeOfDeath = DateTime.UtcNow;

                        if (version < 7)
                            BeginDecay(m_DefaultDecayTime);

                        if (version < 6)
                            m_Looters = new ArrayList();

                        if (version < 4)
                            m_Aggressors = new ArrayList();

                        m_AccessLevel = (AccessLevel)reader.ReadInt();
                        reader.ReadInt(); // guild reserve
                        m_Kills = reader.ReadInt();
                        m_Criminal = reader.ReadBool();

                        int count = reader.ReadInt();

                        m_EquipItems = new ArrayList(count);

                        for (int i = 0; i < count; ++i)
                        {
                            Item item = reader.ReadItem();

                            if (item != null)
                                m_EquipItems.Add(item);
                        }

                        break;
                    }
            }
        }

        public override void SendInfoTo(NetState state, bool sendOplPacket)
        {
            base.SendInfoTo(state, sendOplPacket);

            if (((Body)Amount).IsHuman && (ItemID == 0x2006))
            {
                if (state.ContainerGridLines)
                    state.Send(new CorpseContent6017(state.Mobile, this));
                else
                    state.Send(new CorpseContent(state.Mobile, this));

                state.Send(new CorpseEquip(state.Mobile, this));
            }
        }

        public bool IsCriminalAction(Mobile from)
        {
            if (from == m_Owner || from.AccessLevel >= AccessLevel.GameMaster)
                return false;

            if (m_Owner is PlayerVendor && from == ((PlayerVendor)m_Owner).Owner)
                return false;

            Party p = Party.Get(m_Owner);

            if (p != null && p.Contains(from))
            {
                PartyMemberInfo pmi = p[m_Owner];

                if (pmi != null && pmi.CanLoot)
                    return false;
            }

            return (NotorietyHandlers.CorpseNotoriety(from, this) == Notoriety.Innocent);
        }

        public bool CheckLooting(Mobile from)
        {
            #region Static Region
            StaticRegion sr = StaticRegion.FindStaticRegion(this);
            if (sr != null && sr.BlockLooting && from.AccessLevel == AccessLevel.Player)
                return false;
            #endregion

            return true;
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            if (!base.CheckItemUse(from, item))
                return false;

            if (m_StaticCorpse)
                return false;

            if (!IsCriminalAction(from))
                return true;

            if (item != this)
            {
                Map map = this.Map;

                if (map == null || (map.Rules & MapRules.HarmfulRestrictions) != 0)
                    return false;

                if (CheckLooting(from) == false)
                    return false;
            }

            return true;
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (!base.CheckLift(from, item, ref reject))
                return false;

            if (m_StaticCorpse)
                return false;

            if (!IsCriminalAction(from))
                return true;

            Map map = this.Map;

            if (map == null || (map.Rules & MapRules.HarmfulRestrictions) != 0)
                return false;

            if (CheckLooting(from) == false)
                return false;

            return true;
        }

        public override void OnItemUsed(Mobile from, Item item)
        {
            base.OnItemUsed(from, item);

            // 6/29/2023, Adam: Opening a corpse is not criminal, nor a RevealingAction.
            // this logic is handled in OnItemLifted()
#if false
            if (from != m_Owner)
                //if (IsCriminalAction(from)/*adam*/)
                from.RevealingAction();
                

            // Looting an evil corpse will make you gray to all evils for two minutes as well. 
            if (item != this && IsCriminalAction(from) && !from.Evil && !from.Hero && m_Owner is PlayerMobile && (m_Owner as PlayerMobile).Evil)
                from.ExpirationFlags.Add(new Mobile.ExpirationFlag(from, Mobile.ExpirationFlagID.EvilNoto, TimeSpan.FromMinutes(2)));
            else if (item != this && IsCriminalAction(from))
                from.CriminalAction(true);

            if (!m_Looters.Contains(from))
                m_Looters.Add(from);
#endif
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            base.OnItemLifted(from, item);

            if (item != this && from != m_Owner)
                from.RevealingAction();

            // Looting an evil corpse will make you gray to all evils for two minutes as well. 
            if (item != this && IsCriminalAction(from) && !from.Evil && !from.Hero && m_Owner is PlayerMobile && (m_Owner as PlayerMobile).Evil)
                from.ExpirationFlags.Add(new Mobile.ExpirationFlag(from, Mobile.ExpirationFlagID.EvilNoto, TimeSpan.FromMinutes(2)));
            else if (item != this && IsCriminalAction(from) && !IsGuardIgnore())
                from.CriminalAction(true);

            if (!m_Looters.Contains(from))
                m_Looters.Add(from);
        }

        private bool IsGuardIgnore()
        {
            if (m_Owner is BaseCreature bc)
                return bc.GuardIgnore;

            return false;
        }

        private class OpenCorpseEntry : ContextMenuEntry
        {
            public OpenCorpseEntry()
                : base(6215, 2)
            {
            }

            public override void OnClick()
            {
                Corpse corpse = Owner.Target as Corpse;

                if (corpse != null && Owner.From.CheckAlive())
                    corpse.Open(Owner.From, false);
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (Core.RuleSets.AOSRules() && m_Owner == from && from.Alive)
                list.Add(new OpenCorpseEntry());
        }

        private Hashtable m_RestoreTable;

        public bool GetRestoreInfo(Item item, ref Point3D loc)
        {
            if (m_RestoreTable == null || item == null)
                return false;

            object obj = m_RestoreTable[item];

            if (obj == null)
                return false;

            loc = (Point3D)obj;
            return true;
        }

        public void SetRestoreInfo(Item item, Point3D loc)
        {
            if (item == null)
                return;

            if (m_RestoreTable == null)
                m_RestoreTable = new Hashtable();

            m_RestoreTable[item] = loc;
        }

        public void ClearRestoreInfo(Item item)
        {
            if (m_RestoreTable == null || item == null)
                return;

            m_RestoreTable.Remove(item);

            if (m_RestoreTable.Count == 0)
                m_RestoreTable = null;
        }

        public virtual void Open(Mobile from, bool checkSelfLoot)
        {
            if (from.AccessLevel > AccessLevel.Player || from.InRange(this.GetWorldLocation(), 2))
            {
                bool selfLoot = (checkSelfLoot && (from == m_Owner));

                if (selfLoot)
                {
                    ArrayList items = new ArrayList(this.Items);

                    bool gathered = false;
                    bool didntFit = false;

                    Container pack = from.Backpack;

                    bool checkRobe = true;

                    for (int i = 0; !didntFit && i < items.Count; ++i)
                    {
                        Item item = (Item)items[i];
                        Point3D loc = item.Location;

                        if ((item.Layer == Layer.Hair || item.Layer == Layer.FacialHair) || !item.Movable || !GetRestoreInfo(item, ref loc))
                            continue;

                        if (checkRobe)
                        {
                            DeathRobe robe = from.FindItemOnLayer(Layer.OuterTorso) as DeathRobe;

                            if (robe != null)
                            {
                                Map map = from.Map;

                                if (map != null && map != Map.Internal)
                                    robe.MoveToWorld(from.Location, map);
                            }
                        }

                        if (m_EquipItems.Contains(item) && from.EquipItem(item))
                        {
                            gathered = true;
                        }
                        else if (pack != null && pack.CheckHold(from, item, false, true))
                        {
                            item.Location = loc;
                            pack.AddItem(item);
                            gathered = true;
                        }
                        else
                        {
                            didntFit = true;
                        }
                    }

                    if (gathered && !didntFit)
                    {
                        m_Carved = true;

                        if (ItemID == 0x2006)
                        {
                            ProcessDelta();
                            SendRemovePacket();
                            ItemID = Utility.Random(0xECA, 9); // bone graphic
                            Hue = 0;
                            ProcessDelta();
                        }

                        from.PlaySound(0x3E3);
                        from.SendLocalizedMessage(1062471); // You quickly gather all of your belongings.
                        return;
                    }

                    if (gathered && didntFit)
                        from.SendLocalizedMessage(1062472); // You gather some of your belongings. The rest remain on the corpse.
                }

                if (IsCriminalAction(from))
                {
                    Map map = this.Map;

                    if (map == null || (map.Rules & MapRules.HarmfulRestrictions) != 0)
                    {
                        if (m_Owner == null || !m_Owner.Player)
                            from.SendLocalizedMessage(1005035); // You did not earn the right to loot this creature!
                        else
                            from.SendLocalizedMessage(1010049); // You may not loot this corpse.

                        return;
                    }
                    else
                    {
                        if (m_Owner == null || !m_Owner.Player)
                            from.SendLocalizedMessage(1005036); // Looting this monster corpse will be a criminal act!
                        else if (!from.Evil && !from.Hero && m_Owner is PlayerMobile && (m_Owner as PlayerMobile).Evil)
                            from.SendMessage("Looting this corpse will set evil upon you!"); // Looting this corpse will set evil upon you!
                        else
                            from.SendLocalizedMessage(1005038); // Looting this corpse will be a criminal act!
                    }
                }

                PlayerMobile player = from as PlayerMobile;

                if (player != null)
                {
                    QuestSystem qs = player.Quest;

                    if (qs is UzeraanTurmoilQuest)
                    {
                        GetDaemonBoneObjective obj = qs.FindObjective(typeof(GetDaemonBoneObjective)) as GetDaemonBoneObjective;

                        if (obj != null && obj.CorpseWithBone == this && (!obj.Completed || UzeraanTurmoilQuest.HasLostDaemonBone(player)))
                        {
                            Item bone = new QuestDaemonBone();

                            if (player.PlaceInBackpack(bone))
                            {
                                obj.CorpseWithBone = null;
                                player.SendLocalizedMessage(1049341, "", 0x22); // You rummage through the bones and find a Daemon Bone!  You quickly place the item in your pack.

                                if (!obj.Completed)
                                    obj.Complete();
                            }
                            else
                            {
                                bone.Delete();
                                player.SendLocalizedMessage(1049342, "", 0x22); // Rummaging through the bones you find a Daemon Bone, but can't pick it up because your pack is too full.  Come back when you have more room in your pack.
                            }

                            return;
                        }
                    }
                    else if (qs is TheSummoningQuest)
                    {
                        VanquishDaemonObjective obj = qs.FindObjective(typeof(VanquishDaemonObjective)) as VanquishDaemonObjective;

                        if (obj != null && obj.Completed && obj.CorpseWithSkull == this)
                        {
                            GoldenSkull sk = new GoldenSkull();

                            if (player.PlaceInBackpack(sk))
                            {
                                obj.CorpseWithSkull = null;
                                player.SendLocalizedMessage(1050022); // For your valor in combating the devourer, you have been awarded a golden skull.
                                qs.Complete();
                            }
                            else
                            {
                                sk.Delete();
                                player.SendLocalizedMessage(1050023); // You find a golden skull, but your backpack is too full to carry it.
                            }
                        }
                    }
                }

                base.OnDoubleClick(from);

                // 6/29/2023, Adam: Opening a corpse is not criminal, nor a RevealingAction.
                // this logic is handled in OnItemLifted()
#if false
                if (from != m_Owner)
                    from.RevealingAction();
#endif
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            Open(from, Core.RuleSets.AOSRules());
        }

        public override bool CheckContentDisplay(Mobile from)
        {
            return false;
        }

        public override bool DisplaysContent { get { return false; } }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (ItemID == 0x2006) // Corpse form
            {
                if (m_CorpseName != null)
                    list.Add(m_CorpseName);
                else
                    list.Add(1046414, this.Name); // the remains of ~1_NAME~
            }
            else // Bone form
            {
                list.Add(1046414, this.Name); // the remains of ~1_NAME~
            }
        }

        public override void OnAosSingleClick(Mobile from)
        {
            int hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));
            ObjectPropertyList opl = this.PropertyList;

            if (opl.Header > 0)
                from.Send(new MessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, opl.Header, Name, opl.HeaderArgs));
        }

        public override void OnSingleClick(Mobile from)
        {
            int hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));

            if (ItemID == 0x2006) // Corpse form
            {
                if (m_CorpseName != null)
                    from.Send(new AsciiMessage(Serial, ItemID, MessageType.Label, hue, 3, "", m_CorpseName));
                else
                    from.Send(new MessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, 1046414, "", Name));
            }
            else // Bone form
            {
                from.Send(new MessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, 1046414, "", Name));
            }
        }

        public void Carve(Mobile from, Item item)
        {
            Mobile dead = m_Owner;

            if (m_Carved || dead == null || StaticCorpse)
            {
                from.SendLocalizedMessage(500485); // You see nothing useful to carve from the corpse.
            }
            else if (((Body)Amount).IsHuman && (ItemID == 0x2006))
            {
                new Blood(0x122D).MoveToWorld(Location, Map);

                if (dead is PlayerMobile)
                {
                    new Head(dead.Name, (PlayerMobile)dead).MoveToWorld(Location, Map);
                    new Torso(((PlayerMobile)dead).IOBAlignment).MoveToWorld(Location, Map);
                    new LeftLeg(((PlayerMobile)dead).IOBAlignment).MoveToWorld(Location, Map);
                    new LeftArm(((PlayerMobile)dead).IOBAlignment).MoveToWorld(Location, Map);
                    new RightLeg(((PlayerMobile)dead).IOBAlignment).MoveToWorld(Location, Map);
                    new RightArm(((PlayerMobile)dead).IOBAlignment).MoveToWorld(Location, Map);
                }
                else
                {
                    new Head(dead.Name).MoveToWorld(Location, Map);
                    new Torso().MoveToWorld(Location, Map);
                    new LeftLeg().MoveToWorld(Location, Map);
                    new LeftArm().MoveToWorld(Location, Map);
                    new RightLeg().MoveToWorld(Location, Map);
                    new RightArm().MoveToWorld(Location, Map);
                }

                m_Carved = true;

                ProcessDelta();
                SendRemovePacket();
                ItemID = Utility.Random(0xECA, 9); // bone graphic
                Hue = 0;
                ProcessDelta();

                if (IsCriminalAction(from))
                    from.CriminalAction(true);
            }
            else if (dead is BaseCreature)
            {
                ((BaseCreature)dead).OnCarve(from, this, item);
            }
            else
            {
                from.SendLocalizedMessage(500485); // You see nothing useful to carve from the corpse.
            }
        }
    }
}