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

/* Scripts/Items/Containers/TrapableContainer.cs
 * CHANGELOG:
 *  7/17/2023, Adam (AssignAggressorAndRegister)
 *      1. Revert change of 7/7/2023
 *      2. Create a persistent table of auto-reset traps and who has use it to report a murder.
 *          After the first report, that same trap (unless retrapped,) cannot be use by that same victim to report murder.
 *          This system does away with the undesirable quality of allowing the recipient of the damage to give unlimited murders
 *  7/7/2023, Adam (auto-reset trap hack)
 *      auto reset traps have the undesirable quality of allowing the recipient of the damage to give unlimited murders
 *      So... if (bAutoReset && PlayerCrafted && Trapper != null) Trapper = null; // trapper no longer responsible.
 *  5/22/23, Yoar
 *      Cleaned up usage of serialization flags
 *      Added guild-alignment-only containers
 *  3/21/23, Adam (old style tinker traps)
 *      oldstyle traps on siege 'auto reset'
 *      To be honest, I'm not sure if this was all oldstyle traps, or just some version from Siege.
 *      But the folks playing Siege now seem to be pretty sure this is how they worked.
 *  3/19/23, Adam
 *      RunUO says 5, 15 damage * m_TrapLevel
 *      http://replay.waybackmachine.org/20020402172114/http://uo.stratics.com/content/guides/tinkertraps/trapessay.shtml
 *          "Zip does not do it. Sorry but a dart trap is more on the order of a mosquito bite � annoying but little else."
 *      Those players at the time confirm this, that is, dart traps deal little damage.
 *      RunUO seems to have this wrong. Using a minimum tinker skill dart trap dealt me 45hp damage on the first try.
 *      I will instead mod this formula to 'm_TrapLevel' level damage. That is, a 30 skill tinker made trap will deal 3hp damage
 *      (Similar to a trapped pouch)
 *	3/28/10, adam
 *		Added an auto-reset mechanism to LockableContainers for resetting the trap and lock after a timeout period.
 *		Note: because of the way trapped containers are untrapped via RemoveTrap (power and traptype are cleared)
 *			the autoreset doesn't kick in until the Locked value is set to false.
 *	2/2/07, Pix
 *		Changed animations for explosion and poison traps to be at acceptable z levels.
 *	3/4/06, Pix
 *		Now staff never trip traps.
 *	5/9/05, Adam
 *		Push the Deco flag down to the Container level
 *		Pack old property from serialization routines.
 *	10/30/04, Darva
 *		Fixed CanDetonate
 *	10/25/04, Pix
 *		Reversed the change to m_Enabled made on 10/19.
 *		Also, now serialize/deserialize the m_Enabled flag.
 *	10/23/04, Darva
 *			Added CanDetonate check, which currently stops the trap from going
 *			off if it's on a vendor.
 *    10/19/04, Darva
 *			Set m_Enabled to false after trap goes off.
 *	9/25/04, Adam
 *		Create Version 3 of TrapableContainers that support the Deco attribute.
 *			Most/many containers are derived from TrapableContainer, so they will all get 
 *			the benefits for free.
 *	9/1/04, Pixie
 *		Added TinkerTrapableAttribute so we can mark containers as tinkertrapable or not.
 *  8/8/04, Pixie
 *		Added functionality for tripping the trap if you fail to disarm it.
 *	5/22/2004, Pixie
 *		made tinker traps one-use only
 *	5/22/2004, Pixie
 *		Tweaked poison trap levels up (now GM tinkers always make lethal poison traps)
 *		Changed so tinker-made traps don't get disabled when they're tripped.
 *		Changed sound effects to the right ones for dart/poison traps
 *  5/18/2004, Pixie
 *		Fixed re-enabling of tinker traps, added values to
 *		serialize/deserialize
 *	5/18/2004, Pixie
 *		Added Handling of tinker traps, added dart and poison traps
 */

using Server.Engines.Alignment;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Items
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TinkerTrapableAttribute : System.Attribute
    {
        public TinkerTrapableAttribute()
        {
        }
    }

    public enum TrapType
    {
        None,
        MagicTrap,
        ExplosionTrap,
        DartTrap,
        PoisonTrap
    }

    public abstract class TrapableContainer : BaseContainer, ITelekinesisable
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }

        private TrapType m_TrapType;
        private int m_TrapPower;
        private int m_TrapLevel;
        private bool m_Enabled;
        private TrapType m_OldTrapType;
        private int m_OldTrapPower;
        private Mobile m_Trapper = null;        // tinker that will take the murder count (< publish 4)
        private Mobile m_Owner = null;          // last person to lock the chest (>= publish 4)
        private IOBAlignment m_IOBAlignment;    // kin-only access
        private AlignmentType m_GuildAlignment; // alignment-only access

        /// <summary>
        /// Tinker that trapped this box somewhere other than on the floor of his house or boat deck (< publish 4)
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Trapper
        {
            get
            {
                return m_Trapper;
            }
            set
            {
                m_Trapper = value;
                ResetMurderOpportunity();
            }
        }

        /// <summary>
        /// Last person to lock the chest (>= publish 4)
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get
            {
                return m_Owner;
            }
            set
            {
                m_Owner = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TrapType TrapType
        {
            get
            {
                return m_TrapType;
            }
            set
            {
                m_TrapType = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TrapPower
        {
            get
            {
                return m_TrapPower;
            }
            set
            {
                m_TrapPower = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TrapLevel
        {
            get
            {
                return m_TrapLevel;
            }
            set
            {
                m_TrapLevel = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual IOBAlignment IOBAlignment
        {
            get { return m_IOBAlignment; }
            set { m_IOBAlignment = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AlignmentType GuildAlignment
        {
            get { return m_GuildAlignment; }
            set { m_GuildAlignment = value; }
        }

        public virtual bool TrapOnOpen { get { return true; } }

        // TrapSensitivity modifies the chance to trip the trap when someone fails to disarm it
        public virtual double TrapSensitivity
        {
            get { return 1.0; }
        }

        // auto reset traps will call this to store the current settings for restoration later
        public void RememberTrap()
        {
            // remember the last trap power for auto-reset functionality
            m_OldTrapPower = m_TrapPower;
            m_OldTrapType = m_TrapType;
        }

        // okay, reset the trap based on stored settings.
        public void ResetTrap()
        {   // don't turn on the enabled flag since disarm trap only changes power and type :\
            m_TrapPower = m_OldTrapPower;
            m_TrapType = m_OldTrapType;
        }

        public TrapableContainer(int itemID)
            : base(itemID)
        {
            m_Enabled = true;
            //m_TinkerMade = false;
        }

        public TrapableContainer(Serial serial)
            : base(serial)
        {
            m_Enabled = true;
            //m_TinkerMade = false;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TrapEnabled
        {
            get
            {
                return m_Enabled && m_TrapType != TrapType.None;
            }
            set
            {
                m_Enabled = value;
            }
        }

        /*[CommandProperty(AccessLevel.GameMaster)]
		public bool TinkerMadeTrap
		{
			get { return m_TinkerMade; }
			set { m_TinkerMade = value; }
		}*/

        private bool CanDetonate(Mobile from)
        {
            object rootParent = RootParent;

            if (rootParent is PlayerVendor)
                return false;

            return true;
        }

        public void SendMessageTo(Mobile to, int number, int hue)
        {
            if (Deleted || !to.CanSee(this))
                return;

            to.Send(new Network.MessageLocalized(Serial, ItemID, Network.MessageType.Regular, hue, 3, number, "", ""));
        }

        private void SendMessageTo(Mobile to, string text, int hue)
        {
            if (Deleted || !to.CanSee(this))
                return;

            to.Send(new Network.UnicodeMessage(Serial, ItemID, Network.MessageType.Regular, hue, 3, "ENU", "", text));
        }

        public virtual bool AutoResetTrap
        {
            /* 5/21/23, Yoar
             * 
             * Old-style tinker traps auto-reset:
             * "If I trap a chest, it will explode and kill most anybody who opens it. It will
             * remain this way until the key is used on it."
             * https://uo.stratics.com/content/guides/tinkertraps/trapessay.shtml
             * 
             * Treasure maps do not:
             * "Be aware that you do NOT need Remove Trap or Detect Hidden to get at any of these
             * chest's contents. Pick them, move at least 7 tiles away, cast telekinesis, and hit
             * your last target macro (you have one right?). Boom or Hiss, the trap goes off, and
             * you get your loot."
             * http://web.archive.org/web/20010302164041/http://www.uopowergamers.com:80/skills/lockpicking.html
             */
            get { return (Core.OldStyleTinkerTrap && IsTinkerTrapped); }
        }

        public bool IsTinkerTrapped
        {
            /* 5/21/23, Yoar: Let's assume that all player-crafted containers are tinker-trapped
             * Exception: Magic trap
             */
            get { return (PlayerCrafted && m_TrapType != TrapType.None && m_TrapType != TrapType.MagicTrap); }
        }

        public bool ExecuteTrap(Mobile from)
        {
            return ExecuteTrap(from, AutoResetTrap);
        }

        public virtual bool ExecuteTrap(Mobile from, bool bAutoReset)
        {
            Point3D loc = this.GetWorldLocation();
            Map facet = this.Map;

            if (m_TrapType != TrapType.None && m_Enabled && CanDetonate(from))
            {
                if (from.AccessLevel >= AccessLevel.GameMaster)
                {
                    SendMessageTo(from, "That is trapped, but you open it with your godly powers.", 0x3B2);
                    return false;
                }

                switch (m_TrapType)
                {
                    case TrapType.ExplosionTrap:
                        {
                            SendMessageTo(from, 502999, 0x3B2); // You set off a trap!

                            if (from.InRange(loc, 3))
                            {
                                int damage;

                                // RunUO says 10, 30, but stratics says 5, 15
                                // http://replay.waybackmachine.org/20020402172114/http://uo.stratics.com/content/guides/tinkertraps/trapessay.shtml
                                if (m_TrapLevel > 0)
                                    damage = Utility.RandomMinMax(5, 15) * m_TrapLevel;
                                else
                                    damage = m_TrapPower;

                                // 5/21/23, Yoar: Let's do at least 1 point of damage
                                if (damage <= 0)
                                    damage = 1;

                                //if (m_Trapper != null && !m_Trapper.Deleted)
                                //from.Aggressors.Add(AggressorInfo.Create(m_Trapper, from, true));
                                AssignAggressorAndRegister(from, m_Trapper, bAutoReset);

                                AOS.Damage(from, damage, 0, 100, 0, 0, 0, this);

                                // Your skin blisters from the heat!
                                from.LocalOverheadMessage(Network.MessageType.Regular, 0x2A, 503000);
                            }

                            Effects.SendLocationEffect(loc, facet, 0x36BD, 15, 10);
                            Effects.PlaySound(loc, facet, 0x307);

                            break;
                        }
                    case TrapType.MagicTrap:
                        {
                            if (from.InRange(loc, 1))
                            {
                                int damage = m_TrapPower;

                                // 5/21/23, Yoar: Let's do at least 1 point of damage
                                if (damage <= 0)
                                    damage = 1;

                                from.Damage(damage, this);
                            }

                            Effects.PlaySound(loc, Map, 0x307);

                            Effects.SendLocationEffect(new Point3D(loc.X - 1, loc.Y, loc.Z), Map, 0x36BD, 15);
                            Effects.SendLocationEffect(new Point3D(loc.X + 1, loc.Y, loc.Z), Map, 0x36BD, 15);

                            Effects.SendLocationEffect(new Point3D(loc.X, loc.Y - 1, loc.Z), Map, 0x36BD, 15);
                            Effects.SendLocationEffect(new Point3D(loc.X, loc.Y + 1, loc.Z), Map, 0x36BD, 15);

                            Effects.SendLocationEffect(new Point3D(loc.X + 1, loc.Y + 1, loc.Z + 11), Map, 0x36BD, 15);

                            break;
                        }
                    case TrapType.DartTrap:
                        {
                            SendMessageTo(from, 502999, 0x3B2); // You set off a trap!

                            if (from.InRange(loc, 3))
                            {
                                int damage;

                                // RunUO says 5, 15
                                // http://replay.waybackmachine.org/20020402172114/http://uo.stratics.com/content/guides/tinkertraps/trapessay.shtml
                                //  "Zip does not do it. Sorry but a dart trap is more on the order of a mosquito bite � annoying but little else."
                                // Those players at the time confirm this, that is, dart traps deal little damage.
                                // RunUO seems to have this wrong. Using a minimum tinkerskill dart trap dealt me 45hp damage on the first try.
                                //  I will instead mod this formula to 'm_TrapLevel' level damage. That is, a 30 skill tinker made trap will deal 3hp damage
                                if (m_TrapLevel > 0)
                                    //  old RunUO formula (too much damage)
                                    //damage = Utility.RandomMinMax(5, 15) * m_TrapLevel;
                                    //  New dart trap damage. e.g., "a mosquito bite"
                                    damage = m_TrapLevel;
                                else
                                    damage = m_TrapPower;

                                // 5/21/23, Yoar: Let's do at least 1 point of damage
                                if (damage <= 0)
                                    damage = 1;

                                //if (m_Trapper != null && !m_Trapper.Deleted)
                                //from.Aggressors.Add(AggressorInfo.Create(m_Trapper, from, true));
                                AssignAggressorAndRegister(from, m_Trapper, bAutoReset);

                                AOS.Damage(from, damage, 100, 0, 0, 0, 0, this);

                                // A dart imbeds itself in your flesh!
                                from.LocalOverheadMessage(Network.MessageType.Regular, 0x62, 502998);
                            }

                            Effects.PlaySound(loc, facet, 0x223);

                            break;
                        }
                    case TrapType.PoisonTrap:
                        {
                            SendMessageTo(from, 502999, 0x3B2); // You set off a trap!

                            if (from.InRange(loc, 3))
                            {
                                Poison poison;

                                //if (m_Trapper != null && !m_Trapper.Deleted)
                                //from.Aggressors.Add(AggressorInfo.Create(m_Trapper, from, true));
                                AssignAggressorAndRegister(from, m_Trapper, bAutoReset);

                                if (m_TrapLevel > 0)
                                {
                                    poison = Poison.GetPoison(Math.Max(0, Math.Min(4, m_TrapLevel - 1)));
                                }
                                else
                                {
                                    AOS.Damage(from, m_TrapPower, 0, 0, 0, 100, 0, this);
                                    poison = Poison.Greater;
                                }

                                from.ApplyPoison(from, poison);

                                // You are enveloped in a noxious green cloud!
                                from.LocalOverheadMessage(Network.MessageType.Regular, 0x44, 503004);
                            }

                            Effects.SendLocationEffect(loc, facet, 0x113A, 10, 20);
                            Effects.PlaySound(loc, facet, 0x231);

                            break;
                        }
                }

                // new style tinker traps remain trapped (auto reset)
                if (!bAutoReset)
                    m_TrapType = TrapType.None;

                return true;
            }

            return false;
        }
        private static void Defrag()
        {
            List<Serial> list = new List<Serial>();
            foreach (var key in AutoResetTrapRegistry.Keys)
                if (World.FindItem(key) == null || World.FindItem(key).Deleted || World.FindItem(key) is not TrapableContainer)
                    list.Add(key);
                else
                    foreach (var record in AutoResetTrapRegistry[key])
                        if (record.Attacker == null || record.Attacker.Deleted || record.Defender == null || record.Defender.Deleted)
                            list.Add(key);

            foreach (var key in list)
                AutoResetTrapRegistry.Remove(key);
        }
        private void AssignAggressorAndRegister(Mobile from, Mobile trapper, bool bAutoReset)
        {
            Defrag();

            if (m_Trapper == null || m_Trapper.Deleted || !bAutoReset)
                return;

            if (AutoResetTrapRegistry.ContainsKey(this.Serial))
            {   // okay, we know about this container

                // do we have a matching Attacker and Defender?
                foreach (var data in AutoResetTrapRegistry[this.Serial])
                {
                    if (data.Attacker == trapper && data.Defender == from)
                    {   // we have been here before. Unless the trapper has retrapped the container, the same player cannot report again
                        from.Aggressors.Add(AggressorInfo.Create(trapper, from, criminal: data.ReportMurderOpportunity));
                        // you get only one shot to report unless the trapper retraps the container
                        data.ReportMurderOpportunity = false;
                        return;
                    }
                }

                // we don't have this record, create one
                AutoResetTrapRegistryInfo info = new AutoResetTrapRegistryInfo(m_Trapper, from);
                AutoResetTrapRegistry[this.Serial].Add(info);
                // first time, the player can report the action (murder)
                from.Aggressors.Add(AggressorInfo.Create(trapper, from, criminal: info.ReportMurderOpportunity));
                info.ReportMurderOpportunity = false;
                return;
            }
            else
            {   // we've not seen this container before. Create a new record
                AutoResetTrapRegistryInfo info = new AutoResetTrapRegistryInfo(m_Trapper, from);
                AutoResetTrapRegistry.Add(this.Serial, new List<AutoResetTrapRegistryInfo>() { info });
                from.Aggressors.Add(AggressorInfo.Create(trapper, from, criminal: info.ReportMurderOpportunity));
                info.ReportMurderOpportunity = false;
                return;
            }
        }
        public class AutoResetTrapRegistryInfo
        {
            public Mobile Attacker;
            public Mobile Defender;
            public bool ReportMurderOpportunity;
            public AutoResetTrapRegistryInfo(Mobile attacker, Mobile defender, bool opportunity = true)
            {
                Attacker = attacker;
                Defender = defender;
                ReportMurderOpportunity = opportunity;
            }
        }
        protected static Dictionary<Serial, List<AutoResetTrapRegistryInfo>> AutoResetTrapRegistry = new();
        private void ResetMurderOpportunity()
        {
            if (AutoResetTrapRegistry.ContainsKey(this.Serial))
                // reseting the trap, allows another murder count to be given
                foreach (var record in AutoResetTrapRegistry[this.Serial])
                    record.ReportMurderOpportunity = true;
        }
        public virtual void OnTelekinesis(Mobile from)
        {
            Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x376A, 9, 32, 5022);
            Effects.PlaySound(Location, Map, 0x1F5);

            // adam: this checks makes sure that if this is a kin chest, it can only be accessed by those kin
            if (CheckKin(from) == false)
            {
                SendMessageTo(from, "Your alignment prevents you from accessing this container.", 0x3B2);
                return;
            }

            if (!CheckGuildAlignment(from))
            {
                SendMessageTo(from, "Your guild alignment prevents you from accessing this container.", 0x3B2);
                return;
            }

            if (Core.RuleSets.SiegeStyleRules() && this.TrapEnabled)
                // Telekinesis will not work on trapped or locked chests
                // https://www.uoguide.com/Siege_Perilous
                return;

            if (!this.TrapOnOpen || !ExecuteTrap(from))
                base.DisplayTo(from);
        }

        public override void Open(Mobile from)
        {
            // adam: this checks makes sure that if this is a kin chest, it can only be accessed by those kin
            if (CheckKin(from) == false)
            {
                SendMessageTo(from, "Your alignment prevents you from accessing this container.", 0x3B2);
                return;
            }

            if (!CheckGuildAlignment(from))
            {
                SendMessageTo(from, "Your guild alignment prevents you from accessing this container.", 0x3B2);
                return;
            }

            // new style tinker traps allow the owner to simply open the chest
            if ((from == this.Owner && Core.NewStyleTinkerTrap) || !this.TrapOnOpen || !ExecuteTrap(from))
                base.Open(from);
        }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            if (CheckKin(m) == false)
            {
                SendMessageTo(m, "Your alignment prevents you from storing anything here.", 0x3B2);
                return false;
            }

            if (!CheckGuildAlignment(m))
            {
                SendMessageTo(m, "Your guild alignment prevents you from accessing this container.", 0x3B2);
                return false;
            }

            return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
        }

        public bool CheckKin(Mobile m)
        {
            // this checks makes sure that if this is a kin chest, it can only be accessed by those kin
            if (m_IOBAlignment != IOBAlignment.None && m is PlayerMobile && (m as PlayerMobile).IOBAlignment != m_IOBAlignment && (m as PlayerMobile).AccessLevel == AccessLevel.Player)
                return false;

            return true;
        }

        public bool CheckGuildAlignment(Mobile m)
        {
            if (m_GuildAlignment != AlignmentType.None && AlignmentSystem.Find(m) != m_GuildAlignment && m.AccessLevel == AccessLevel.Player)
                return false;

            return true;
        }

#if old
		public override void OnDoubleClick(Mobile from)
		{
			if (from.AccessLevel > AccessLevel.Player || from.InRange(this.GetWorldLocation(), 2))
			{
				if (!ExecuteTrap(from))
					base.OnDoubleClick(from);
			}
			else
			{
				from.SendLocalizedMessage(500446); // That is too far away.
			}
		}
#endif

        [Flags]
        private enum SaveFlag
        {
            None = 0x0,
            HasOBAlignment = 0x01,
            GuildAlignment = 0x02,
            AutoResetTrapRegistry = 0x04,
        }

        private void SetFlag(ref SaveFlag flags, SaveFlag flag, bool value)
        {
            if (value)
                flags |= flag;
            else
                flags &= ~flag;
        }

        private bool GetFlag(SaveFlag flags, SaveFlag flag)
        {
            return ((flags & flag) != 0);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)10); // version

            /* all serialization must follow flags code */
            // version 10
            SaveFlag saveFlags = SaveFlag.None;
            SetFlag(ref saveFlags, SaveFlag.HasOBAlignment, m_IOBAlignment != IOBAlignment.None);
            SetFlag(ref saveFlags, SaveFlag.GuildAlignment, m_GuildAlignment != AlignmentType.None);
            SetFlag(ref saveFlags, SaveFlag.AutoResetTrapRegistry, AutoResetTrapRegistry.Count > 0);
            writer.Write((int)saveFlags);

            // version 10a
            if (GetFlag(saveFlags, SaveFlag.HasOBAlignment))
                writer.Write((int)m_IOBAlignment);  // kin-only chest

            if (GetFlag(saveFlags, SaveFlag.GuildAlignment))
                writer.Write((byte)m_GuildAlignment);

            // version 9
            writer.Write(m_Owner);                  // last person to lock chest (>= publish 4)

            // version 8
            writer.Write(m_Trapper);                // tinker (< publish 4)

            // version 7
            writer.Write((int)m_TrapLevel);

            // version 6
            writer.Write((int)m_OldTrapPower);

            // version 5
            writer.Write((bool)m_Enabled);
            //writer.Write( (bool) false );		// removed in version 5 
            //writer.Write((bool)false);		// m_TinkerMade (obsolete) removed in version 7
            writer.Write((int)m_OldTrapType);
            writer.Write((int)m_TrapPower);
            writer.Write((int)m_TrapType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt(); // version
            SaveFlag saveFlags = (SaveFlag)reader.ReadInt();
            /* all serialization must follow flags code */

            switch (version)
            {
                case 10:
                    {
                        if (GetFlag(saveFlags, SaveFlag.HasOBAlignment))
                            m_IOBAlignment = (IOBAlignment)reader.ReadInt();

                        if (GetFlag(saveFlags, SaveFlag.GuildAlignment))
                            m_GuildAlignment = (AlignmentType)reader.ReadByte();

                        goto case 9;
                    }
                case 9:
                    {
                        m_Owner = reader.ReadMobile();
                        goto case 8;
                    }
                case 8:
                    {
                        m_Trapper = reader.ReadMobile();
                        goto case 7;
                    }
                case 7:
                    {
                        m_TrapLevel = reader.ReadInt();
                        goto case 6;
                    }
                case 6:
                    {
                        m_OldTrapPower = reader.ReadInt();
                        goto case 5;
                    }
                case 5:
                    {
                        goto case 4;
                    }
                case 4:
                    {
                        m_Enabled = reader.ReadBool();
                        goto case 3;
                    }
                case 3:
                    {
                        if (version < 5)
                            reader.ReadBool();  // deco field
                        goto case 2;
                    }
                case 2:
                    {
                        if (version <= 7)
                            reader.ReadBool();  // m_TinkerMade

                        m_OldTrapType = (TrapType)reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_TrapPower = reader.ReadInt();
                        if (version < 6)
                            m_OldTrapPower = m_TrapPower;
                        goto case 0;
                    }

                case 0:
                    {
                        m_TrapType = (TrapType)reader.ReadInt();

                        break;
                    }
            }

            if (version < 7)
            {   // I guess this is reasonable
                // Example: a level 5 trap is TrapPower 5*25, and the TrapLevel is 5
                //	therfore a reasonable m_TrapLevel is TrapPower / 25
                m_TrapLevel = TrapPower / 25;
            }
        }
        #region Serialization
        public static void Load()
        {
            if (!File.Exists("Saves/AutoResetTrapRegistry.bin"))
                return;

            Console.WriteLine("AutoResetTrapRegistry...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/AutoResetTrapRegistry.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Serial key = (Serial)reader.ReadInt();
                                int nelts = reader.ReadInt();
                                if (World.FindItem(key) != null && World.FindItem(key) is TrapableContainer)
                                    for (int jx = 0; jx < nelts; jx++)
                                    {
                                        if (AutoResetTrapRegistry.ContainsKey(key))
                                        {
                                            AutoResetTrapRegistry[key].Add(new AutoResetTrapRegistryInfo(attacker: reader.ReadMobile(), defender: reader.ReadMobile(), opportunity: reader.ReadBool()));
                                        }
                                        else
                                        {
                                            AutoResetTrapRegistry.Add(key, new List<AutoResetTrapRegistryInfo>());
                                            AutoResetTrapRegistry[key].Add(new AutoResetTrapRegistryInfo(attacker: reader.ReadMobile(), defender: reader.ReadMobile(), opportunity: reader.ReadBool()));
                                        }
                                    }
                            }
                            break;
                        }

                    default:
                        {
                            throw new Exception("Invalid AutoResetTrapRegistry.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading AutoResetTrapRegistry.bin, using default values:", ConsoleColor.Red);
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("AutoResetTrapRegistry Saving...");

            // cleanup AutoResetTrapRegistry
            Defrag();

            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/AutoResetTrapRegistry.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(AutoResetTrapRegistry.Count);
                            foreach (var record in AutoResetTrapRegistry)
                            {
                                writer.Write(record.Key);
                                writer.Write(record.Value.Count);
                                foreach (var data in record.Value)
                                {
                                    writer.Write(data.Attacker);
                                    writer.Write(data.Defender);
                                    writer.Write(data.ReportMurderOpportunity);
                                }
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing AutoResetTrapRegistry.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
    }
}