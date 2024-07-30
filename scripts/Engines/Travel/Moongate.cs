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

/* scripts\Engines\Travel\Moongate.cs
 *	ChangeLog:
 *	7/27/2023, Adam
 *	    Siege: Allow gate while dragging an object
 *	5/25/2023, Adam (DurationOverride)
 *	    Surface Event.DurationOverride for owner use.
 *	8/28/22, Yoar
 *	    You can no longer use moongates while holding a sigil.
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *	5/14/09, Plasma
 *		Added kinOnly filter
 *	1/7/09, Adam
 *		Remove LootType.Special/Internal from the NOT RestrictedItem() list
 *	7/26/07, Adam
 *		Add the DestinationOverride property.
 *			DestinationOverride lets us go places usually restricted by CheckTravel()
 *			useful for staff created gates.
 *	2/10/06, Adam
 *		1. added item.ItemID == 8270(DeathShroud) to EmptyBackpack exclusion check.
 *		Oddly, we dressing the ghost in an item.ItemID == 8270 and not a DeathShroud.
 *		2. make the Moongate.RestrictedItem() public so it can be reused in AITeleporters as it is the same item list
 * 2/3/06, Pix
 *		Added DeathShroud to EmptyBackpack exclusion check.
 *	1/30/06, Pix
 *		Added TravelRules system as well as the first set of restrictions:
 *		MortalsOnly, GhostsOnly, and EmptyBackpack
 * 1/4/06, Adam
 *		Reverse the 'drop holding' change of 01/03/06
 *		for now, this an allowed means of transportation of heavy objects
 *	01/03/06, Pix
 *		Gate user now drops what he's holding on cursor when he gets teleported.
 * 11/30/04, Pix
 *		Made it so criminals couldn't use moongates (either through doubleclicking
 *		or moving over).
*  6/5/04, Pix
*		Merged in 1.0RC0 code.
 *	5/26/04 smerX
 *		Added functionality for tamed pets
 *		Added another format for the CheckGate() method
 *	4/xx/04 smerX
 *		Removed gate gump
 */

using Server.Engines;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Server.Spells;
using System;

namespace Server.Items
{
    [DispellableFieldAttribute]
    public class Moongate : Item
    {
        private Point3D m_Target;
        private Map m_TargetMap;
        private bool m_bDispellable;

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest
        {
            get
            {
                return m_Target;
            }
            set
            {
                m_Target = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map MapDest
        {
            get
            {
                return m_TargetMap;
            }
            set
            {
                m_TargetMap = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Dispellable
        {
            get
            {
                return m_bDispellable;
            }
            set
            {
                m_bDispellable = value;
            }
        }
        public virtual bool ShowFeluccaWarning { get { return false; } }

        [Constructable]
        public Moongate()
            : this(Point3D.Zero, null)
        {
            m_bDispellable = true;
        }

        [Constructable]
        public Moongate(bool bDispellable)
            : this(Point3D.Zero, null)
        {
            m_bDispellable = bDispellable;
        }

        [Constructable]
        public Moongate(Point3D target, Map targetMap)
            : base(0xF6C)
        {
            Movable = false;
            Light = LightType.Circle300;

            m_Target = target;
            m_TargetMap = targetMap;
        }
        public Moongate(Serial serial)
            : base(serial)
        {
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (!from.Player)
                return;

            if (from.InRange(GetWorldLocation(), 1))
            {
                if (from.Criminal)
                {
                    from.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                }
                else if (from.CheckState(Mobile.ExpirationFlagID.EvilCrim))
                {
                    from.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                }
                else
                {
                    CheckGate(from, 1);
                }
            }
            else
                from.SendLocalizedMessage(500446); // That is too far away.
        }
        public override bool OnMoveOver(Mobile m)
        {
            if (m.Player)
            {
                if (m.Criminal)
                {
                    m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                }
                else if (m.CheckState(Mobile.ExpirationFlagID.EvilCrim))
                {
                    m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                }
                else
                {
                    CheckGate(m, 0, TimeSpan.FromSeconds(1.0));
                }
            }
            else if (m is BaseCreature && ((BaseCreature)m).Controlled)
            {
                CheckGate(m, 0, TimeSpan.Zero);
            }

            return true;
        }
        public virtual void CheckGate(Mobile m, int range)
        {
            new DelayTimer(m, this, range, TimeSpan.FromSeconds(1.0)).Start();
        }
        public virtual void CheckGate(Mobile m, int range, TimeSpan delay)
        {
            new DelayTimer(m, this, range, delay).Start();
        }
        public virtual void UseGate(Mobile m)
        {
            if (Factions.Sigil.ExistsOn(m))
            {
                m.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
            }
            else if (Engines.Alignment.TheFlag.ExistsOn(m))
            {
                m.SendMessage("You can't do that while carrying the flag.");
            }
            //else if ( m.Murderer && m_TargetMap != Map.Felucca )
            //{
            //m.SendLocalizedMessage( 1019004 ); // You are not allowed to travel there.
            //}
            else if (m.Spell != null)
            {
                m.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
            }
            // some mobs are afraid of magic and will not enter!
            else if (m is BaseCreature bc && bc.OnMagicTravel() == BaseCreature.TeleportResult.AnyRejected)
            {   // message issued in OnMagicTravel()
                ; // we're not traveling like this
            }
            else if (m.CheckHolding() && Core.RuleSets.AngelIslandRules())
            {   // no Cliloc
                // m.SendLocalizedMessage(1071955); // You cannot teleport while dragging an object.
                m.SendMessage("You cannot teleport while dragging an object.");
            }
            else if (m_TargetMap != null && m_TargetMap != Map.Internal)
            {
                bool jail;
                if (SpellHelper.CheckTravel(m_TargetMap, m_Target, TravelCheckType.GateTo, m, out jail))
                {
                    BaseCreature.TeleportPets(m, m_Target, m_TargetMap);
                    m.MoveToWorld(m_Target, m_TargetMap);
                    m.Send(new PlaySound(0x20E, m.Location));
                }
                else
                    m.SendMessage("This moongate does not seem to go anywhere.");
            }
            else
            {
                m.SendMessage("This moongate does not seem to go anywhere.");
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // version 3: remove MoongateTravelRules

            writer.Write(m_Target);
            writer.Write(m_TargetMap);
            writer.Write(m_bDispellable);

            // Version 2
            // writer.Write((int)m_SpecialAccess); removed in version 3
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 3:
                    {
                        m_Target = reader.ReadPoint3D();
                        m_TargetMap = reader.ReadMap();
                        m_bDispellable = reader.ReadBool();
                        break;
                    }
                default:
                    {   // old style - obsolete
                        m_Target = reader.ReadPoint3D();
                        m_TargetMap = reader.ReadMap();

                        if (version >= 1)
                            m_bDispellable = reader.ReadBool();

                        if (version == 2)
                            reader.ReadInt(); // MoongateTravelRules removed in version 3
                        break;
                    }
            }
        }

        public virtual bool ValidateUse(Mobile from, bool message)
        {
            if (from.Deleted || this.Deleted)
                return false;

            if (from.Map != this.Map || !from.InRange(this, 1))
            {
                if (message)
                    from.SendLocalizedMessage(500446); // That is too far away.

                return false;
            }

            return true;
        }

        #region Trammy gump
        public virtual void BeginConfirmation(Mobile from)
        {
            // removed trammy confirm gump below
            bool bUseConfirmGump = false;

            if (bUseConfirmGump && IsInTown(from.Location, from.Map) && !IsInTown(m_Target, m_TargetMap))
            {
                from.Send(new PlaySound(0x20E, from.Location));
                from.CloseGump(typeof(MoongateConfirmGump));
                from.SendGump(new MoongateConfirmGump(from, this));
            }
            else
            {
                EndConfirmation(from);
            }
        }
        public virtual void EndConfirmation(Mobile from)
        {
            if (!ValidateUse(from, true))
                return;

            UseGate(from);
        }
        public virtual void DelayCallback(Mobile from, int range)
        {
            if (!ValidateUse(from, false) || !from.InRange(this, range))
                return;

            if (m_TargetMap != null)
                BeginConfirmation(from);
            else
                from.SendMessage("This moongate does not seem to go anywhere.");
        }
        public static bool IsInTown(Point3D p, Map map)
        {
            if (map == null)
                return false;

            GuardedRegion reg = Region.Find(p, map) as GuardedRegion;

            return (reg != null && reg.IsGuarded && !(reg is Engines.ConPVP.SafeZone));
        }
        private class DelayTimer : Timer
        {
            private Mobile m_From;
            private Moongate m_Gate;
            private int m_Range;

            public DelayTimer(Mobile from, Moongate gate, int range, TimeSpan delay)
                : base(delay)
            {
                m_From = from;
                m_Gate = gate;
                m_Range = range;
            }

            protected override void OnTick()
            {
                m_Gate.DelayCallback(m_From, m_Range);
            }
        }
        #endregion Trammy gump
    }
    #region Trammy gump
    public class ConfirmationMoongate : Moongate
    {
        private int m_GumpWidth;
        private int m_GumpHeight;

        private int m_TitleColor;
        private int m_MessageColor;

        private int m_TitleNumber;
        private int m_MessageNumber;

        private string m_MessageString;

        [CommandProperty(AccessLevel.GameMaster)]
        public int GumpWidth
        {
            get { return m_GumpWidth; }
            set { m_GumpWidth = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GumpHeight
        {
            get { return m_GumpHeight; }
            set { m_GumpHeight = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TitleColor
        {
            get { return m_TitleColor; }
            set { m_TitleColor = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageColor
        {
            get { return m_MessageColor; }
            set { m_MessageColor = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TitleNumber
        {
            get { return m_TitleNumber; }
            set { m_TitleNumber = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageNumber
        {
            get { return m_MessageNumber; }
            set { m_MessageNumber = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string MessageString
        {
            get { return m_MessageString; }
            set { m_MessageString = value; }
        }

        [Constructable]
        public ConfirmationMoongate()
            : this(Point3D.Zero, null)
        {
        }

        [Constructable]
        public ConfirmationMoongate(Point3D target, Map targetMap)
            : base(target, targetMap)
        {
        }

        public ConfirmationMoongate(Serial serial)
            : base(serial)
        {
        }

        public virtual void Warning_Callback(Mobile from, bool okay, object state)
        {
            if (okay)
                EndConfirmation(from);
        }

        public override void BeginConfirmation(Mobile from)
        {
            if (m_GumpWidth > 0 && m_GumpHeight > 0 && m_TitleNumber > 0 && (m_MessageNumber > 0 || m_MessageString != null))
            {
                from.CloseGump(typeof(WarningGump));
                from.SendGump(new WarningGump(m_TitleNumber, m_TitleColor, m_MessageString == null ? (object)m_MessageNumber : (object)m_MessageString, m_MessageColor, m_GumpWidth, m_GumpHeight, new WarningGumpCallback(Warning_Callback), from));
            }
            else
            {
                base.BeginConfirmation(from);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteEncodedInt(m_GumpWidth);
            writer.WriteEncodedInt(m_GumpHeight);

            writer.WriteEncodedInt(m_TitleColor);
            writer.WriteEncodedInt(m_MessageColor);

            writer.WriteEncodedInt(m_TitleNumber);
            writer.WriteEncodedInt(m_MessageNumber);

            writer.Write(m_MessageString);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_GumpWidth = reader.ReadEncodedInt();
                        m_GumpHeight = reader.ReadEncodedInt();

                        m_TitleColor = reader.ReadEncodedInt();
                        m_MessageColor = reader.ReadEncodedInt();

                        m_TitleNumber = reader.ReadEncodedInt();
                        m_MessageNumber = reader.ReadEncodedInt();

                        m_MessageString = reader.ReadString();

                        break;
                    }
            }
        }
    }
    public class ConfirmationSungate : Sungate
    {
        private int m_GumpWidth;
        private int m_GumpHeight;

        private int m_TitleColor;
        private int m_MessageColor;

        private int m_TitleNumber;
        private int m_MessageNumber;

        private string m_MessageString;

        [CommandProperty(AccessLevel.GameMaster)]
        public int GumpWidth
        {
            get { return m_GumpWidth; }
            set { m_GumpWidth = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GumpHeight
        {
            get { return m_GumpHeight; }
            set { m_GumpHeight = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TitleColor
        {
            get { return m_TitleColor; }
            set { m_TitleColor = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageColor
        {
            get { return m_MessageColor; }
            set { m_MessageColor = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TitleNumber
        {
            get { return m_TitleNumber; }
            set { m_TitleNumber = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageNumber
        {
            get { return m_MessageNumber; }
            set { m_MessageNumber = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string MessageString
        {
            get { return m_MessageString; }
            set { m_MessageString = value; }
        }

        [Constructable]
        public ConfirmationSungate()
            : this(Point3D.Zero, null)
        {
        }

        [Constructable]
        public ConfirmationSungate(Point3D target, Map targetMap)
            : base(target, targetMap, dispellable: false)
        {
        }

        public ConfirmationSungate(Serial serial)
            : base(serial)
        {
        }

        public virtual void Warning_Callback(Mobile from, bool okay, object state)
        {
            if (okay)
                EndConfirmation(from);
        }

        public void BeginConfirmation(Mobile from)
        {
            if (m_GumpWidth > 0 && m_GumpHeight > 0 && m_TitleNumber > 0 && (m_MessageNumber > 0 || m_MessageString != null))
            {
                from.CloseGump(typeof(WarningGump));
                from.SendGump(new WarningGump(m_TitleNumber, m_TitleColor, m_MessageString == null ? (object)m_MessageNumber : (object)m_MessageString, m_MessageColor, m_GumpWidth, m_GumpHeight, new WarningGumpCallback(Warning_Callback), from));
            }
#if false
            else
            {
                base.BeginConfirmation(from);
            }
#endif
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteEncodedInt(m_GumpWidth);
            writer.WriteEncodedInt(m_GumpHeight);

            writer.WriteEncodedInt(m_TitleColor);
            writer.WriteEncodedInt(m_MessageColor);

            writer.WriteEncodedInt(m_TitleNumber);
            writer.WriteEncodedInt(m_MessageNumber);

            writer.Write(m_MessageString);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_GumpWidth = reader.ReadEncodedInt();
                        m_GumpHeight = reader.ReadEncodedInt();

                        m_TitleColor = reader.ReadEncodedInt();
                        m_MessageColor = reader.ReadEncodedInt();

                        m_TitleNumber = reader.ReadEncodedInt();
                        m_MessageNumber = reader.ReadEncodedInt();

                        m_MessageString = reader.ReadString();

                        break;
                    }
            }
        }
        #region Trammy gump
#if false
        public virtual void BeginConfirmation(Mobile from)
        {
            // removed trammy confirm gump below
            bool bUseConfirmGump = false;

            if (bUseConfirmGump && IsInTown(from.Location, from.Map) && !IsInTown(m_Target, m_TargetMap))
            {
                from.Send(new PlaySound(0x20E, from.Location));
                from.CloseGump(typeof(MoongateConfirmGump));
                from.SendGump(new MoongateConfirmGump(from, this));
            }
            else
            {
                EndConfirmation(from);
            }
        }
#endif
        public virtual void EndConfirmation(Mobile from)
        {
            if (!ValidateUse(from, true))
                return;

            TryUseTeleport(from);
        }
        public virtual void DelayCallback(Mobile from, int range)
        {
            if (!ValidateUse(from, false) || !from.InRange(this, range))
                return;

            if (base.MapDest != null)
                BeginConfirmation(from);
            else
                from.SendMessage("This moongate does not seem to go anywhere.");
        }
        public static bool IsInTown(Point3D p, Map map)
        {
            if (map == null)
                return false;

            GuardedRegion reg = Region.Find(p, map) as GuardedRegion;

            return (reg != null && reg.IsGuarded && !(reg is Engines.ConPVP.SafeZone));
        }
        private class DelayTimer : Timer
        {
            private Mobile m_From;
            private Moongate m_Gate;
            private int m_Range;

            public DelayTimer(Mobile from, Moongate gate, int range, TimeSpan delay)
                : base(delay)
            {
                m_From = from;
                m_Gate = gate;
                m_Range = range;
            }

            protected override void OnTick()
            {
                m_Gate.DelayCallback(m_From, m_Range);
            }
        }
        #endregion Trammy gump
    }
    public class EventConfirmationSungate : ConfirmationSungate
    {
        private Event m_event;
        #region Event Properties
        [CommandProperty(AccessLevel.GameMaster)]
        public bool TimerRunning
        {
            get { return m_event.TimerRunning; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EventRunning
        {
            get { return m_event.EventRunning; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string EventStart
        {
            get { return m_event.EventStart; }
            set { m_event.EventStart = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string EventEnd
        {
            get { return m_event.EventEnd; }
            set { m_event.EventEnd = value; }
        }
        [CommandProperty(AccessLevel.Seer)]
        public int OffsetFromUTC
        {
            get { return m_event.OffsetFromUTC; }
            set { m_event.OffsetFromUTC = value; InvalidateProperties(); }
        }
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan Countdown
        {
            get { return m_event.Countdown; }
            set { m_event.Countdown = value; InvalidateProperties(); }
        }
        [CommandProperty(AccessLevel.Seer)]
        public TimeSpan Duration
        {
            get { return m_event.Duration; }
            set { m_event.Duration = value; InvalidateProperties(); }
        }
        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public bool DurationOverride
        {
            get { return m_event.DurationOverride; }
            set { m_event.DurationOverride = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Timezone
        {
            get
            { return m_event.Timezone; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EventDebug
        {
            get { return m_event.EventDebug; }
            set { m_event.EventDebug = value; }
        }
        public string EventTimeRemaining
        {
            get { return m_event.EventTimeRemaining; }
        }
        #endregion Event Properties
        #region hidden properties
        // don't make these a command property, we don't want GM's starting/stopping in this way
        public override bool Running
        {
            get { return base.Running; }
            set
            {
                base.Running = value;
                InvalidateProperties();
            }
        }
        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                base.Visible = value;
                InvalidateProperties();
            }
        }
        #endregion hidden properties
        [Constructable]
        public EventConfirmationSungate()
            : base()
        {
            m_event = new Event(this, null, EventStarted, EventEnded);
            base.Running = false;
            base.Visible = false;
        }
        public EventConfirmationSungate(Serial serial)
            : base(serial)
        {

        }
        public void EventStarted(object o)
        {
            base.Running = true;
            base.Visible = true;
            if (Core.Debug && false)
                Utility.ConsoleWriteLine(String.Format("{0} got 'Event started' event.", this), ConsoleColor.Yellow);
        }
        public void EventEnded(object o)
        {
            base.Running = false;
            base.Visible = false;
            if (Core.Debug && false)
                Utility.ConsoleWriteLine(String.Format("{0} got 'Event ended' event.", this), ConsoleColor.Yellow);
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);   // version
            m_event.Serialize(writer);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_event = new Event(this, null, EventStarted, EventEnded);
                        m_event.Deserialize(reader);
                        break;
                    }
            }
        }
    }
    public class MoongateConfirmGump : Gump
    {
        private Mobile m_From;
        private Moongate m_Gate;

        public MoongateConfirmGump(Mobile from, Moongate gate)
            : base(Core.RuleSets.AOSRules() ? 110 : 20, Core.RuleSets.AOSRules() ? 100 : 30)
        {
            m_From = from;
            m_Gate = gate;

            if (Core.RuleSets.AOSRules())
            {
                Closable = false;

                AddPage(0);

                AddBackground(0, 0, 420, 280, 5054);

                AddImageTiled(10, 10, 400, 20, 2624);
                AddAlphaRegion(10, 10, 400, 20);

                AddHtmlLocalized(10, 10, 400, 20, 1062051, 30720, false, false); // Gate Warning

                AddImageTiled(10, 40, 400, 200, 2624);
                AddAlphaRegion(10, 40, 400, 200);

                if (from.Map != Map.Felucca && gate.MapDest == Map.Felucca && gate.ShowFeluccaWarning)
                    AddHtmlLocalized(10, 40, 400, 200, 1062050, 32512, false, true); // This Gate goes to Felucca... Continue to enter the gate, Cancel to stay here
                else
                    AddHtmlLocalized(10, 40, 400, 200, 1062049, 32512, false, true); // Dost thou wish to step into the moongate? Continue to enter the gate, Cancel to stay here

                AddImageTiled(10, 250, 400, 20, 2624);
                AddAlphaRegion(10, 250, 400, 20);

                AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767, false, false); // OKAY

                AddButton(210, 250, 4005, 4007, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767, false, false); // CANCEL
            }
            else
            {
                AddPage(0);

                AddBackground(0, 0, 420, 400, 5054);
                AddBackground(10, 10, 400, 380, 3000);

                AddHtml(20, 40, 380, 60, @"Dost thou wish to step into the moongate? Continue to enter the gate, Cancel to stay here", false, false);

                AddHtmlLocalized(55, 110, 290, 20, 1011012, false, false); // CANCEL
                AddButton(20, 110, 4005, 4007, 0, GumpButtonType.Reply, 0);

                AddHtmlLocalized(55, 140, 290, 40, 1011011, false, false); // CONTINUE
                AddButton(20, 140, 4005, 4007, 1, GumpButtonType.Reply, 0);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 1)
                m_Gate.EndConfirmation(m_From);
        }
    }
    #endregion Trammy gump
}