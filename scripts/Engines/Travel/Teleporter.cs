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

/* scripts\Engines\Travel\Teleporter.cs
 * CHANGELOG:
 *  10/18/26, Adam (confinement Stage 5)
 *      TestCenterTeleporter: a misplaced entry portal (not outside Test Center, or not leading into it) or exit portal (not
 *      in Test Center, or one that would put the original back in Test Center) is refused and reported
 *      (TestCenterPolicy.ValidatePortal / ReportPortal) instead of being used. The misuse branches go through
 *      TestCenterPolicy.Enforce (the same penalties; first confinement wins). Staff still travel as themselves.
 *      FindOriginal pulled out of HandleExitTestCenter; the exit broadcast no longer dereferences a missing original.
 *  10/17/26, Adam (confinement admission refactor R1)
 *      BankThese overload that reports what it did (items banked, items refused) and can keep refused items with the
 *      player instead of dropping them at their feet. The original BankThese (and BankEverything) call it unchanged.
 *  12/30/2023, Adam 
 *      Remove the ability for mounted players to pass through a 'no pets' teleporter.
 *      Note: I'm leaving it alone for now, but will auto-patch after the current events completes 1/6/2024 
 *  7/7/2023, Adam (Pets)
 *      Disallow mounted players on devices that do not allow 'pets'
 *  5/31/2023, Adam
 *      allow using teleporters while holding a spell (not the case for IsGate)
 *  5/25/2023, Adam (CheckSpecialRestrictions())
 *      In CheckSpecialRestrictions(), we now issue the following message to staff as a reminder:
 *          "This item is not useable by all, but your godly powers allow you to transcend the laws of Britannia."
 *  3/17/23, Adam (DirectionalEnable)
 *      When UseDirectional is enables, players can only be teleported if they enter the teleporter
 *      from the same direction the teleporter is facing.
 *      For instance, if the teleporter is facing north (Direction == North,) the player will only be 
 *      teleported if they too are moving north.
 *      Special Cases handled: 
 *      1. They are standing on the teleporter but facing the wrong direction. Then they only 'turn' the correct direction and will be teleported.
 *      2. They were teleported onto another 'DirectionalEnable' teleporter. In this case, there was no OnMoveOver, so we explicitly issue one.
 *          (See #1 above)
 *  1/2/23, Adam
 *      Move ValidateUse down from Sungate to the base Teleporter
 *      (Sungates are Teleporters!)
 *  9/2/22, Yoar (WorldZone)
 *      Added WorldZone check in order to contain players within the world zone.
 *	4/15/08, Adam
 *		Add an AccessLevel property so we can have restricted access teleporters to areas under construction or that are only 
 *			available during certain times of the year. 
 *			We'll probably use this at least during construction and test of the Summer Champ in Ishlenar. We may also set it to
 *			no access in non summer months
 *  06/26/06, Kit
 *		Added Bool TeleportPets, added new string for msg that pets cant use.
 *  06/03/06, Kit
 *		Added additional destination point3d, now choose random one if multiple are filled in.
 *		added ability to define rect and teleport to random point in rect that can spawn mobile.
 *  04/13/06, Kit
 *		Added bool Criminal for checking to only transport non criminals on normal teleporters.
 *	6/1/05, erlein
 *		- Added SparkleEffect property which, if enabled, will send sparklies
 *		when player steps on + keep resending every second leading to end
 *		of teleporter delay period.
 *		- Added string property which passes message to player when they step
 *		on the teleporter.
 *	5/8/05, Pix
 *		Made teleporters with delays work.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Scripts.Misc;
using Server.Accounting;
using Server.Commands;
using Server.Engines;
using Server.Engines.Seasons;
using Server.Items.Triggers;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Items
{
    [Flags]
    public enum TeleporterFlags
    {
        None = 0,

        GhostsOnly = 1 << 0,
        MortalsOnly = 1 << 1,
        EmptyBackpack = 1 << 2,
        DestinationOverride = 1 << 3,
        KinOnly = 1 << 4,
        Criminals = 1 << 5,
        Pets = 1 << 6,
        SparkleEffect = 1 << 7,
        SourceEffect = 1 << 8,
        DestEffect = 1 << 9,
        Running = 1 << 10,
        DropDragging = 1 << 11,
        Creatures = 1 << 12,
        Sparkle = 1 << 13,
        SparkleHue = 1 << 14,
        UseDirectional = 1 << 15,
        TicketPasscode = 1 << 16,
        AllowHolidayPets = 1 << 17,
        EverythingToBank = 1 << 18,
        StablePets = 1 << 19,
        NoGhosts = 1 << 20
    }

    public partial class Teleporter : Item, ITriggerable
    {
        public override Item Dupe(int amount)
        {
            Teleporter new_item = new Teleporter();
            Utility.CopyProperties(this, new_item);
            return base.Dupe(new_item, amount);
        }
        #region props
        private Point3D m_PointDest;
        private Point3D m_PointDest2;
        private Point3D m_PointDest3;
        private Point3D m_PointDest4;
        private Point3D m_PointDest5;
        private Point2D m_RectStart;
        private Point2D m_RectEnd;

        private Map m_MapDest;
        private string m_DelayMessage;
        private int m_SoundID;
        private Item? m_Sparkle;
        private int m_SparkleHue;
        private TimeSpan m_Delay;
        private string? m_PetMessage;
        private AccessLevel m_AccessLevel;
        private string? m_TicketPasscode;
        public override void OnMapChange()
        {   // you're my sparkle! Follow me wherever I should lead.
            base.OnMapChange();
            if (ValidSparkle())
                m_Sparkle.MoveToWorld(this.Location, this.Map);
        }
        public override bool Inform(Mobile from, InformType type, Item item)
        {
            if (type == InformType.SingleClick)
                if (Name != null)
                {
                    if (ValidSparkle())
                        m_Sparkle.LabelTo(from, Name);
                    else
                        this.LabelTo(from, Name);
                    return true;
                }
                else
                {
                    if (ValidSparkle())
                        m_Sparkle.LabelTo(from, (MapDest ?? this.Map).ToString());
                    else
                        this.LabelTo(from, (MapDest ?? this.Map).ToString());

                    return true;
                }
            return base.Inform(from, type, item);
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Creatures
        {
            get { return GetSpecialFlag(TeleporterFlags.Creatures); }
            set { SetSpecialFlag(TeleporterFlags.Creatures, value); InvalidateProperties(); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Sparkle
        {
            get
            {
                if (GetSpecialFlag(TeleporterFlags.Sparkle))
                    if (!ValidSparkle())
                    {   // staff deleted the sparkle
                        SetSpecialFlag(TeleporterFlags.Sparkle, false);
                        InvalidateProperties();
                    }

                return GetSpecialFlag(TeleporterFlags.Sparkle);
            }
            set
            {
                if (value == true)
                {
                    if (ValidSparkle())
                    {   // do little, we already have a sparkle
                        if (this is EventTeleporter etb)
                            m_Sparkle.Visible = etb.Running;
                        else
                            m_Sparkle.Visible = true;
                    }
                    else
                        InitializeSparkle(new Item(0x373A));
                }
                else if (ValidSparkle())
                {
                    InitializeSparkle(null);
                }
                SetSpecialFlag(TeleporterFlags.Sparkle, value); InvalidateProperties();
            }
        }
        private bool ValidSparkle()
        {
            return ValidItem(m_Sparkle);
        }

        private bool ValidItem([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] Item? item)
        {
            return item != null && !item.Deleted;
        }

        private bool InitializeSparkle(Item? value)
        {
            // No valid sparkle provided: remove any existing sparkle and clear state.
            if (!ValidItem(value))
            {
                if (ValidSparkle())
                {
                    m_Sparkle.Delete();
                    m_Sparkle = null;
                }

                SetSpecialFlag(TeleporterFlags.Sparkle, false);
                return false;
            }

            // Replace any existing sparkle with the new one.
            if (ValidSparkle() && m_Sparkle != value)
                m_Sparkle.Delete();

            m_Sparkle = value;

            // Preserve an explicitly configured sparkle hue; otherwise adopt the item's current hue.
            if (m_SparkleHue == 0)
                m_SparkleHue = m_Sparkle.Hue;

            m_Sparkle.Hue = m_SparkleHue;
            m_Sparkle.Movable = false;          // sparkle is teleporter-owned and should not be movable
            m_Sparkle.InformNeighbor = true;

            // Event teleporters only show sparkle while running; normal teleporters always show it.
            if (this is EventTeleporter etb)
                m_Sparkle.Visible = etb.Running;
            else
                m_Sparkle.Visible = true;

            // Keep the sparkle anchored to the teleporter.
            m_Sparkle.MoveToWorld(this.Location, this.Map);

            SetSpecialFlag(TeleporterFlags.Sparkle, true);
            SetSpecialFlag(TeleporterFlags.SparkleHue, m_SparkleHue != 0);

            return true;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int SparkleHue
        {
            get
            {
                if (GetSpecialFlag(TeleporterFlags.Sparkle))
                    if (!ValidSparkle())
                    {   // staff deleted the sparkle
                        SetSpecialFlag(TeleporterFlags.Sparkle, false);
                        SetSpecialFlag(TeleporterFlags.SparkleHue, false);
                        m_SparkleHue = 0;
                        InvalidateProperties();
                    }

                return m_SparkleHue;
            }
            set
            {
                if (GetSpecialFlag(TeleporterFlags.Sparkle) && ValidSparkle())
                {
                    SetSpecialFlag(TeleporterFlags.SparkleHue, value != 0);
                    m_Sparkle.Hue = value;
                }
                else
                {   // so that we don't try to serialize
                    SetSpecialFlag(TeleporterFlags.Sparkle, false);
                    SetSpecialFlag(TeleporterFlags.SparkleHue, false);
                    m_SparkleHue = 0;
                }

                m_SparkleHue = value; InvalidateProperties();
            }
        }
        public Item SparkleItem
        {
            set
            {
                InitializeSparkle(value);
                InvalidateProperties();
            }
            get { return m_Sparkle; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return GetSpecialFlag(TeleporterFlags.SourceEffect); }
            set { SetSpecialFlag(TeleporterFlags.SourceEffect, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Criminals
        {
            get { return GetSpecialFlag(TeleporterFlags.Criminals); }
            set { SetSpecialFlag(TeleporterFlags.Criminals, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AccessLevel AccessLevel
        {
            get { return m_AccessLevel; }
            set { m_AccessLevel = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Pets
        {
            get { return GetSpecialFlag(TeleporterFlags.Pets); }
            set { SetSpecialFlag(TeleporterFlags.Pets, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string? NoTeleportPetMessage
        {
            get { return m_PetMessage; }
            set { m_PetMessage = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TicketPasscodeText
        {
            get { return m_TicketPasscode; }
            set { m_TicketPasscode = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return GetSpecialFlag(TeleporterFlags.DestEffect); }
            set { SetSpecialFlag(TeleporterFlags.DestEffect, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SparkleEffect
        {
            get { return GetSpecialFlag(TeleporterFlags.SparkleEffect); }
            set { SetSpecialFlag(TeleporterFlags.SparkleEffect, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DelayMessage
        {
            get { return m_DelayMessage; }
            set { m_DelayMessage = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Running
        {
            get { return GetSpecialFlag(TeleporterFlags.Running); }
            set
            {   // if you're staff and can still see the sparkle, don't worry - we can see hidden things!
                if (GetSpecialFlag(TeleporterFlags.Sparkle))
                    if (this is EventTeleporter etb)
                        m_Sparkle.Visible = value;
                    else
                        m_Sparkle.Visible = true;

                SetSpecialFlag(TeleporterFlags.Running, value); InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest
        {
            get { return m_PointDest; }
            set { m_PointDest = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest2
        {
            get { return m_PointDest2; }
            set { m_PointDest2 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest3
        {
            get { return m_PointDest3; }
            set { m_PointDest3 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest4
        {
            get { return m_PointDest4; }
            set { m_PointDest4 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest5
        {
            get { return m_PointDest5; }
            set { m_PointDest5 = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D RectStartXY
        {
            get { return m_RectStart; }
            set { m_RectStart = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D RectEndXY
        {
            get { return m_RectEnd; }
            set { m_RectEnd = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map MapDest
        {
            get { return m_MapDest; }
            set { m_MapDest = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AllowHolidayPets
        {
            get { return GetSpecialFlag(TeleporterFlags.AllowHolidayPets); }
            set { SetSpecialFlag(TeleporterFlags.AllowHolidayPets, value); }
        }
        #endregion props
        #region Standard Construction
        public override int LabelNumber { get { return 1026095; } } // teleporter

        [Constructable]
        public Teleporter()
            : this(new Point3D(0, 0, 0), null, false)
        {
        }

        [Constructable]
        public Teleporter(Point3D pointDest, Map mapDest)
            : this(pointDest, mapDest, false)
        {
        }

        [Constructable]
        public Teleporter(int ItemID)
            : this(Point3D.Zero, null, false, ItemID)
        {
        }

        [Constructable]
        public Teleporter(Point3D pointDest, Map mapDest, bool creatures, int ItemID = 0x1BC3)
            : base(ItemID)
        {
            Movable = false;
            Visible = false;

            SetSpecialFlag(TeleporterFlags.Running, true);
            m_PointDest = pointDest;
            m_MapDest = mapDest;
            SetSpecialFlag(TeleporterFlags.Pets, creatures);
            SetSpecialFlag(TeleporterFlags.SparkleEffect, false);
            m_DelayMessage = "";
            SetSpecialFlag(TeleporterFlags.Criminals, true);
            m_PetMessage = null;
            m_TicketPasscode = null;
            m_AccessLevel = Server.AccessLevel.Player;
            TravelObject = true;
        }
        // we override this so staff cannot change it.
        //  i.e., not a CommandProperty
        public override bool Visible
        {
            get { return base.Visible; }
            set { base.Visible = value; }
        }
        public Teleporter(Serial serial)
            : base(serial)
        {
        }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (GetSpecialFlag(TeleporterFlags.Running))
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive

            if (m_MapDest != null)
                list.Add(1060658, "Map\t{0}", m_MapDest);

            if (m_PointDest != Point3D.Zero)
                list.Add(1060659, "Coords\t{0}", m_PointDest);

            list.Add(1060660, "Creatures\t{0}", GetSpecialFlag(TeleporterFlags.Pets) ? "Yes" : "No");
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (GetSpecialFlag(TeleporterFlags.Running))
            {
                Rectangle2D rect = new Rectangle2D(m_RectStart.X, m_RectStart.Y, m_RectEnd.X, m_RectEnd.Y);
                if (m_MapDest != null && m_PointDest != Point3D.Zero)
                    LabelTo(from, "{0} [{1}]", m_PointDest, m_MapDest);
                else if (m_MapDest != null)
                    LabelTo(from, "[{0}]", m_MapDest);
                else if (m_PointDest != Point3D.Zero)
                    LabelTo(from, m_PointDest.ToString());
                else if (m_PointDest2 != Point3D.Zero)
                    LabelTo(from, m_PointDest2.ToString());
                else if (m_PointDest3 != Point3D.Zero)
                    LabelTo(from, m_PointDest3.ToString());
                else if (m_PointDest4 != Point3D.Zero)
                    LabelTo(from, m_PointDest4.ToString());
                else if (m_PointDest5 != Point3D.Zero)
                    LabelTo(from, m_PointDest5.ToString());
                else if (m_RectStart != Point2D.Zero && m_RectEnd != Point2D.Zero)
                    LabelTo(from, rect.ToString());

                // teleporters are *very* sensitive to z-order. Suggest raising the teleporter
                if (from.Z != Z && GetDistanceToSqrt(from) < 2)
                    from.SendSystemMessage(string.Format("Teleporter maybe too low, try raising its Z to {0}", from.Z));

                LabelTo(from, "(active)");
            }
            else
            {
                LabelTo(from, "(inactive)");
            }
        }

        protected void SendSparkles_Callback(object state)
        {
            Mobile from = (Mobile)state;

            if (from.Location != this.Location)
                return;

            Effects.SendLocationParticles(EffectItem.Create(
                this.Location, this.Map, TimeSpan.FromSeconds(1.0)
                ), 0x376A, 9, 32, 5020);

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(SendSparkles_Callback), from);
        }
        public virtual void StartTeleport(Mobile m)
        {
            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
            {
                Timer.DelayCall(m_Delay, DoTeleport_Callback, m);
                if (m_DelayMessage != "")
                    m.SendMessage(m_DelayMessage);
            }
        }
        private void DoTeleport_Callback(Mobile from)
        {
            if (!CanTeleport(from) || !ValidateUse(from, message: true))
                return;

            DoTeleport(from);
        }
        public virtual void DoTeleport(Mobile m)
        {
            if (this.Delay != TimeSpan.Zero && m.Location != this.Location)
            {
                //If we're delayed and we're not on the teleporter, ignore the teleport
                return;
            }

            #region Destination rects
            Map map = m_MapDest;
            ArrayList temp = new ArrayList();

            if (map == null || map == Map.Internal)
                map = m.Map;

            Point3D p;

            if (m_PointDest != Point3D.Zero)
                temp.Add(m_PointDest);
            if (m_PointDest2 != Point3D.Zero)
                temp.Add(m_PointDest2);
            if (m_PointDest3 != Point3D.Zero)
                temp.Add(m_PointDest3);
            if (m_PointDest4 != Point3D.Zero)
                temp.Add(m_PointDest4);
            if (m_PointDest5 != Point3D.Zero)
                temp.Add(m_PointDest5);

            if (temp.Count == 0)
                p = m.Location;
            else
                p = (Point3D)temp[Utility.Random(temp.Count)];

            if (m_RectStart != Point2D.Zero && m_RectEnd != Point2D.Zero)
            {
                for (int i = 0; i < 20; ++i)
                {
                    int x = Utility.RandomMinMax(m_RectStart.X, m_RectEnd.X);
                    int y = Utility.RandomMinMax(m_RectStart.Y, m_RectEnd.Y);
                    if (map.CanSpawnLandMobile(x, y, 0))
                    {
                        p = new Point3D(x, y, 0);
                        continue;
                    }
                    else
                    {
                        int z = map.GetAverageZ(x, y);

                        if (map.CanSpawnLandMobile(x, y, z))
                        {
                            p = new Point3D(x, y, z);
                            continue;
                        }
                    }
                }
            }
            #endregion Destination

            #region Kiting Mitigation
            KM_OnDoTeleport(m);
            #endregion Kiting Mitigation

            #region EverythingToBank
            // move everything to the bank?
            if (EverythingToBank && m is PlayerMobile pm)
            {
                byte bags = 0;
                if (!CheckBank(pm, ref bags))
                {
                    m.SendMessage("Your bank is full.");
                    m.SendMessage("Free up some room and try again.");
                    return;
                }
                if (!BankEverything(pm))
                {
                    m.SendMessage("Your bank is full.");
                    m.SendMessage("Some of your items may have been dropped to the ground.");
                    return;
                }
            }
            #endregion EverythingToBank

            #region Pets
            if (StablePets)
            {
                if (!Utility.CanStablePets(m))
                    m.SendMessage("No room in the stables and your {0} behind.", (Utility.CountPets(m) > 0) ? "companions remain" : "companion remains");
                else
                    Utility.StablePets(m);
            }
            else if (Pets)
            {
                BaseCreature.TeleportResult result = BaseCreature.TeleportPets(m, p, map, AllowPetTravel);

                if (result == BaseCreature.TeleportResult.AnyRejected)
                    m.SendMessage("Your companion is unable to accompany you and remains behind.");
            }
            else if (BaseCreature.CountPetsInRange(m) != 0)
            {
                if (NoTeleportPetMessage != null)
                    m.SendMessage(NoTeleportPetMessage);
                else
                    m.SendMessage("Your companion is unable to accompany you and remains behind.");
            }
            #endregion Pets

            if (GetSpecialFlag(TeleporterFlags.SourceEffect))
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            // 6/21/2026, Adam: disabled in Move() - causes jerky teleporter usage.
            //  Maybe keep if we can limit scope to only the affected teleporters, like the one in Prison Spirit Spawn
            // Teleport with a brief post-move input lock.
            // This prevents client movement packets already in flight from being
            // applied at the old Z/geometry, which can cause landing at an incorrect Z
            // or becoming stuck (a known RunUO timing quirk).
            m.TeleportMoveLockUntil = DateTime.UtcNow + TimeSpan.FromMilliseconds(CoreAI.PostTeleportMoveLockMs);
            m.MoveToWorld(p, map);

            #region Kiting Mitigation
            KM_OnAfterTeleport(m);
            #endregion Kiting Mitigation

            // now, if we are teleporting onto another teleporter that uses UseDirectional, we issue an OnMoveOver
            //  this is so that if the user then turns into the prescribed direction, they will be teleported
            {
                Teleporter? other = (Teleporter?)Utility.FindOneItemAt(p, map, typeof(Teleporter));
                if (other is not null && other.Directional == true)
                    other.OnMoveOver(m);
            }
            if (GetSpecialFlag(TeleporterFlags.DestEffect))
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                m.Send(new Server.Network.PlaySound(m_SoundID, m.Location));
        }
        private bool AllowPetTravel(BaseCreature pet)
        {
            if (!AllowHolidayPets && pet.IsWinterHolidayPet)
                return false;

            return true;
        }
        #endregion Standard Construction
        #region ITriggerable
        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            if (!Running)
                return false;

            if (from != null && CanTeleport(from))
                return true;

            return false;
        }
        public virtual void OnTrigger(Mobile from)
        {
            StartTeleport(from);
        }
        #endregion ITriggerable
        public virtual bool IsGate => false;
        private Direction GetTrueDirection(Direction d)
        {
            return d & Direction.Mask;
            if (d < Direction.Running)
                return d;

            return (Direction)((byte)d - (byte)Direction.Running);

        }
        private Direction EnterDirection()
        {
            switch (Direction)
            {
                default:
                case Direction.North: return Direction.South;
                case Direction.South: return Direction.North;
                case Direction.East: return Direction.West;
                case Direction.West: return Direction.East;
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
            }
        }

        private void FakeOnMoveOver(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is Mobile m)
            {
                if (m.Location == this.Location)
                {   // if they now are facing the right Direction...
                    if (GetTrueDirection(m.Direction) == Direction)
                        //if (GetTrueDirection(m.Direction) == EnterDirection()) 
                        OnMoveOver(m);
                    else
                        // as long as they stand here
                        Timer.DelayCall(TimeSpan.FromMilliseconds(150), new TimerStateCallback(FakeOnMoveOver), new object[] { m });
                }
            }
        }

        public bool CanTeleport(Mobile m)
        {
            if (GetSpecialFlag(TeleporterFlags.Running))
            {
                if (Directional == true && GetTrueDirection(m.Direction) != Direction)
                {   // if we moved over the teleporter, but we weren't going the prescribed direction
                    //  Create a 'fake' OnMoveOver timer to check for a position change to the correct direction
                    //  if the user is still standing on the teleporter and they then change to the prescribed direction
                    //  Issue an OnMoveOver to complete the teleport.
                    // Why? This handles the case where the player moved onto the teleporter facing the wrong direction,
                    //  But, they then turn to the prescribed direction. (We assume an attempted step here.)
                    // Note, we can't check (m.Location == this.Location) here since OnMoveOver is called before the players position
                    //  is actually updated. We will check it in FakeOnMoveOver()
                    Timer.DelayCall(TimeSpan.FromMilliseconds(150), new TimerStateCallback(FakeOnMoveOver), new object[] { m });

                    return false;
                }

                if (m.Player)
                {
                    PlayerMobile? pm = m as PlayerMobile;
                    if (pm == null)
                        return false;

                    // NOTE: OSI relied on short criminal timers for travel denial.
                    // Mortalis uses explicit travel restriction logic; Criminal is no longer sufficient.
                    if (!GetSpecialFlag(TeleporterFlags.Criminals) && !pm.CanCriminalTravel == true)
                    {
                        if (IsGate)
                            m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                        else
                            m.SendMessage("You are a criminal and may not use this.");
                        return false;
                    }

                    if (m.CheckState(Mobile.ExpirationFlagID.EvilCrim) && !GetSpecialFlag(TeleporterFlags.Criminals))
                    {
                        if (IsGate)
                            m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                        else
                            m.SendMessage("You are a criminal and may not use this.");

                        return false;
                    }

                    // clones can never leave the TestCenter map unless it's via a Test Center Teleporter
                    if (pm.Clone && this is not TestCenterTeleporter)
                    {
                        if (MapDest != null && MapDest != Map.TestCenter)
                        {
                            m.SendMessage("You cannot travel there.");
                            return false;
                        }
                    }

                    if (m.AccessLevel < m_AccessLevel)
                    {
                        m.SendMessage("You shall not pass!");
                        return false;
                    }

                    #region World Zone
                    if (WorldZone.BlockTeleport(m, m_PointDest, (m_MapDest == null || m_MapDest == Map.Internal) ? m.Map : m_MapDest))
                        return false;
                    #endregion

                    if (CheckSpecialRestrictions(m) == false)
                    {
                        // they already got a message
                        return false;
                    }

                    if (Factions.Sigil.ExistsOn(m))
                    {
                        //m.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
                        m.SendMessage("You can't use this while carrying the sigil.");
                        return false;
                    }

                    if (Engines.Alignment.TheFlag.ExistsOn(m))
                    {
                        m.SendMessage("You can't use this while carrying the flag.");
                        return false;
                    }

                    // this check is only valid for gates.
                    if (m.Spell != null && IsGate)
                    {
                        //m.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
                        m.SendMessage("You are too busy to use this at the moment.");
                        return false;
                    }

                    // not while mounted
                    //  If staff, we already got a message
                    if (!Pets && !StablePets && CheckMount(m) && m.AccessLevel == AccessLevel.Player)
                    {
                        m.SendMessage("You can't use this while mounted on your pet.");
                        return false;
                    }
                }
                else if (!CreatureTeleportOk(m))
                    return false;
            }
            else
                return false;
            return true;
        }
        private bool CheckMount(Mobile m)
        {
            BaseCreature mount;
            return (mount = m.Mount as BaseCreature) != null && mount.Controlled && mount.ControlMaster == m;
        }
        public static bool CheckBank(Mobile m, ref byte bags)
        {   // will the players bank hold all their crap?
            if (m.BankBox != null && m.Backpack != null)
            {
                int available = 125 - m.BankBox.TotalItems;
                int equipment = Utility.GetEquippedItems(m).Count;
                equipment += m.Dragging == null ? 0 : 1; // account for items on the cursor
                int belongings = m.Backpack.GetDeepItems().Count;

                bags = 0;
                int total = equipment + belongings;
                if (total <= available)
                {   // the bank will hold all there stuff. Now see if we can add 1-2 bags to hold their stuff (one for equipment and one for belongings)

                    if (total + 1 <= available)
                        bags = 1;

                    if (total + 2 <= available)
                        bags = 2;

                    return true;
                }

            }

            return false;
        }
        public static void ArchiveEverything(Mobile m)
        {
            // Archive all possessions into the bank.
            // Items are collected into two bags (belongings + equipment) and
            // dropped into the bank box. Any capacity issues are intentionally
            // handled by the bank overflow / recycler system.

            if (m == null || m.Deleted)
                return;

            // Drop what they are currently holding
            Utility.DropHolding(m);

            BankBox bank = m.BankBox;
            if (bank == null)
                return; // nothing we can do

            // Collect backpack contents
            Backpack belongings = new Backpack();
            foreach (Item item in Utility.GetBackpackItems(m))
            {
                m.Backpack.RemoveItem(item);
                belongings.AddItem(item);
            }

            // Collect equipped items
            Backpack equipment = new Backpack();
            foreach (Item item in Utility.GetEquippedItems(m))
            {
                m.RemoveItem(item);
                equipment.AddItem(item);
            }

            // Archive into bank (overflow/recycler will handle excess)
            bank.DropItem(belongings);
            bank.DropItem(equipment);
        }

        public static bool BankEverything(Mobile m)
        {   // will the players bank hold all their crap?
            if (m.BankBox != null && m.Backpack != null)
            {
                byte bags = 0;
                if (CheckBank(m, ref bags))
                {
                    bool success_belongings = BankThese(m, Utility.GetBackpackItems(m), bags > 0);    // if we are allowed one bag, use it for backpack items
                    bool use_bag = bags > 1 || (bags > 0 && Utility.GetBackpackItems(m).Count == 0);    // we didn't need the first bag, so we will use it for equiped items
                    bool success_equipment = BankThese(m, Utility.GetEquippedItems(m), use_bag);     // if we are allowed two bags, use the second for equiped items

                    if (!success_belongings || !success_equipment)
                        return false;

                    return true;
                }
            }

            return false;
        }
        private static bool BankThese(Mobile m, List<Item> items, bool use_bag)
        {
            List<Item> refused = new();
            BankThese(m, items, use_bag, RefusedItems.DropAtFeet, refused);
            return refused.Count == 0;
        }

        /// <summary>What becomes of an item the bank box refuses.</summary>
        public enum RefusedItems
        {
            DropAtFeet,     // moved to the world where they stand (as BankThese always has)
            KeepWithOwner,  // left with them: a refused item stays where it was, a refused bag is emptied back into the backpack
        }

        /// <summary>
        /// BankThese, reporting what it did: returns how many of items were banked; the refused ones are added to refused.
        /// In a bag the items are banked or refused together.
        /// </summary>
        public static int BankThese(Mobile m, List<Item> items, bool use_bag, RefusedItems onRefusal, List<Item> refused)
        {
            if (items.Count == 0)
                return 0;

            if (use_bag)
            {
                Backpack backpack = new();
                foreach (Item item in items)
                    backpack.AddItem(item);

                if (m.BankBox.TryDropItem(m, backpack, sendFullMessage: false))
                    return items.Count;

                refused.AddRange(items);
                if (onRefusal == RefusedItems.DropAtFeet)
                    backpack.MoveToWorld(m.Location, m.Map);
                else
                {
                    foreach (Item item in items)
                        m.Backpack.DropItem(item);
                    backpack.Delete();
                }
                return 0;
            }
            else
            {
                int banked = 0;
                foreach (Item item in items)
                    if (m.BankBox.TryDropItem(m, item, sendFullMessage: false))
                        banked++;
                    else
                    {
                        refused.Add(item);
                        if (onRefusal == RefusedItems.DropAtFeet)
                            item.MoveToWorld(m.Location, m.Map);
                    }

                return banked;
            }
        }
        private bool CreatureTeleportOk(Mobile m)
        {
            BaseCreature? bc = m as BaseCreature;
            if (bc is not null)
            {   // antiKiting is a one time use. It lets us chase after the cheesy player that is ducking in and out of a teleporter
                bool antiKiting = bc.GetCreatureBool(CreatureBoolTable.AntiKiting);
                if (antiKiting)
                    bc.SetCreatureBool(CreatureBoolTable.AntiKiting, false);
                return GetSpecialFlag(TeleporterFlags.Creatures) || antiKiting;
            }
            return false;
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
        public Map GetMap(Mobile m)
        {
            //  in AI 5 and RunUO 2.6, null maps are allowed for teleporters, but not gates.
            //  This is to allow teleporters to work relative to the currrent location. basically 'a teleport'
            Map map = m_MapDest;
            if (!IsGate)
                if (map == null || map == Map.Internal)
                    map = m.Map;

            return map;
        }
        public virtual bool TryUseTeleport(Mobile m)
        {
            Map map = GetMap(m);

            if (m is BaseCreature bc && bc.OnMagicTravel() == BaseCreature.TeleportResult.AnyRejected)
            {   // message issued in OnMagicTravel()
                return false;  // we're not traveling like this
            }
            if (map != null && map != Map.Internal)
            {
                bool jail;
                // DestinationOverride lets us go places usually restricted by CheckTravel()
                if (DestinationOverride || SpellHelper.CheckTravel(map, PointDest, IsGate ? TravelCheckType.GateTo : TravelCheckType.TeleportTo, m, out jail))
                {
                    if (GetSpecialFlag(TeleporterFlags.DropDragging))
                        m.DropDragging();

                    StartTeleport(m);
                    if (GetSpecialFlag(TeleporterFlags.SparkleEffect))
                        Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(SendSparkles_Callback), m);

                    return true;
                }
                else
                {
                    if (jail == true)
                    {
                        Point3D jailCell = new Point3D(5295, 1174, 0);
                        m.MoveToWorld(jailCell, m.Map);
                        return true;
                    }
                    else if (IsGate)
                        m.SendMessage("This Moongate does not seem to go anywhere.");
                }
            }
            else if (IsGate)
                m.SendMessage("This Moongate does not seem to go anywhere.");

            return true;
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (CanTeleport(m) && ValidateUse(m, message: true))
                Timer.DelayCall(TimeSpan.FromSeconds(.01), new TimerStateCallback(OnMoveOverTick), new object[] { m });

            return true;
        }
        private void OnMoveOverTick(object state)
        {
            object[] aState = (object[])state;
            if (aState[0] != null && aState[0] is Mobile m)
            {
                //m.Direction = EnterDirection();
                TryUseTeleport(m);
            }
        }
        #region  Special Restrictions
        private TeleporterFlags m_SpecialAccess;
        public bool GetSpecialFlag(TeleporterFlags flag)
        {
            return ((m_SpecialAccess & flag) != 0);
        }
        public void SetSpecialFlag(TeleporterFlags flag, bool value)
        {
            if (value)
            {
                if (CheckIllegalFlagCombination(flag, out string reason))
                {
                    this.SendMessageNearby(reason);
                    return;
                }
            }

            if (value)
                m_SpecialAccess |= flag;
            else
                m_SpecialAccess &= ~flag;
        }
        private bool CheckIllegalFlagCombination(TeleporterFlags flag, out string reason)
        {
            reason = null;

            TeleporterFlags temp = m_SpecialAccess | flag;

            // GhostsOnly vs MortalsOnly
            if ((temp & TeleporterFlags.GhostsOnly) != 0 &&
                (temp & TeleporterFlags.MortalsOnly) != 0)
            {
                reason = "Cannot combine GhostsOnly with MortalsOnly.";
                return true;
            }

            // GhostsOnly vs NoGhosts
            if ((temp & TeleporterFlags.GhostsOnly) != 0 &&
                (temp & TeleporterFlags.NoGhosts) != 0)
            {
                reason = "Cannot combine GhostsOnly with NoGhosts.";
                return true;
            }

            // MortalsOnly vs NoGhosts (optional depending on semantics)
            if ((temp & TeleporterFlags.MortalsOnly) != 0 &&
                (temp & TeleporterFlags.NoGhosts) != 0)
            {
                reason = "MortalsOnly already excludes ghosts; NoGhosts is redundant.";
                return true;
            }

            return false;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool GhostsOnly
        {
            get { return GetSpecialFlag(TeleporterFlags.GhostsOnly); }
            set { SetSpecialFlag(TeleporterFlags.GhostsOnly, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool NoGhosts
        {
            get { return GetSpecialFlag(TeleporterFlags.NoGhosts); }
            set { SetSpecialFlag(TeleporterFlags.NoGhosts, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EverythingToBank
        {
            get { return GetSpecialFlag(TeleporterFlags.EverythingToBank); }
            set { SetSpecialFlag(TeleporterFlags.EverythingToBank, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool StablePets
        {
            get { return GetSpecialFlag(TeleporterFlags.StablePets); }
            set { SetSpecialFlag(TeleporterFlags.StablePets, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Directional
        {   // When UseDirectional is enabled, players can only be teleported if they enter the teleporter
            //  from the same direction the teleporter is facing.
            //  For instance, if the teleporter is facing north (Direction == North,) the player will only be 
            //  teleported if they too are moving north.
            get { return GetSpecialFlag(TeleporterFlags.UseDirectional); }
            set { SetSpecialFlag(TeleporterFlags.UseDirectional, value); UpdateItemID(); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool TicketPasscode
        {
            get { return GetSpecialFlag(TeleporterFlags.TicketPasscode); }
            set
            {
                SetSpecialFlag(TeleporterFlags.TicketPasscode, value);
                if (value == false)
                    m_TicketPasscode = null;
                UpdateItemID();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool MortalsOnly
        {
            get { return GetSpecialFlag(TeleporterFlags.MortalsOnly); }
            set { SetSpecialFlag(TeleporterFlags.MortalsOnly, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool KinOnly
        {
            get { return GetSpecialFlag(TeleporterFlags.KinOnly); }
            set { SetSpecialFlag(TeleporterFlags.KinOnly, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool EmptyBackpack
        {
            get { return GetSpecialFlag(TeleporterFlags.EmptyBackpack); }
            set { SetSpecialFlag(TeleporterFlags.EmptyBackpack, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestinationOverride
        {
            /* DestinationOverride
		     *	DestinationOverride lets us go places usually restricted by CheckTravel()
		     *	useful for staff created gates.
		     */
            get { return GetSpecialFlag(TeleporterFlags.DestinationOverride); }
            set { SetSpecialFlag(TeleporterFlags.DestinationOverride, value); }
        }
        public static bool RestrictedItem(Mobile m, Item item)
        {   // items that are allowed to pass (also called for AITeleporters)
            if (item == m.Backpack || item == m.BankBox ||
                item == m.Hair || item == m.Beard ||
                item is DeathShroud || item.ItemID == 8270  // 8270 == deathshroud
                )
                return false;
            else
                return true;
        }

        public bool CheckSpecialRestrictions(Mobile m)
        {
            bool result = CheckSpecialRestrictionsInternal(m);
            //Always let staff pass.
            if (result == false && m.AccessLevel > AccessLevel.Player)
            {
                m.SendMessage("This item is not usable by all, but your godly powers allow you to transcend the laws of Britannia.");
                return true;
            }

            return result;
        }
        public bool CheckSpecialRestrictionsInternal(Mobile m)
        {
            //Don't bother checking it if there's no special flags.
            if (m_SpecialAccess == TeleporterFlags.None) return true;
            PlayerMobile? pm = m as PlayerMobile;

            // can we move everything to the bank?
            if (EverythingToBank && pm != null)
            {
                byte bags = 0;
                if (!CheckBank(pm, ref bags))
                {
                    m.SendMessage("Your bank is full.");
                    m.SendMessage("Free up some room and try again.");
                    return false;
                }
            }

            // stable pets
            if (StablePets)
            {
                if (!Utility.CanStablePets(m))
                {
                    m.SendMessage("No room in the stables for your pets.");
                    return false;
                }
            }

            // Pets
            if (!Pets && !StablePets && pm != null)
            {
                if (CheckMount(pm))
                {
                    m.SendMessage("You can't use this while mounted.");
                    return false;
                }
            }

            //Ghosts Only
            if (GhostsOnly)
            {
                if (m is PlayerMobile)
                {
                    if (m.Alive)
                    {
                        m.SendMessage("You are alive, you cannot pass.");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            //Living Only
            if (NoGhosts)
            {
                if (m is PlayerMobile)
                {
                    if (!m.Alive)
                    {
                        m.SendMessage("You are dead, you cannot pass.");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            //Mortals Only
            if (MortalsOnly)
            {
                if (m is PlayerMobile)
                {
                    if (((PlayerMobile)m).Mortal == false)
                    {
                        m.SendMessage("You are not mortal, you cannot pass.");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            //Empty Backpack
            if (EmptyBackpack)
            {
                if (m is PlayerMobile)
                {
                    if (m.Backpack != null && m.Backpack.Items.Count > 0)
                    {
                        m.SendMessage("You have items in your backpack, you cannot pass.");
                        return false;
                    }
                    if (m.Dragging != null)
                    {
                        m.SendMessage("You are holding something, you cannot pass.");
                        return false;
                    }
                    if (m.Items != null && m.Items.Count > 0)
                    {
                        foreach (Item it in m.Items)
                        {
                            if (RestrictedItem(m, it) == true)
                            {
                                m.SendMessage("You have items on you, you cannot pass.");
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            //Kin only
            if (KinOnly)
            {
                if (!(m is PlayerMobile)) return false;
                if (((PlayerMobile)m).IOBRealAlignment == IOBAlignment.None)
                {
                    m.SendMessage("You are not Kin aligned, you cannot pass.");
                    return false;
                }
            }

            if (TicketPasscode)
            {
                // find all of their tickets
                List<Ticket> list = new List<Ticket>();
                if (m != null && m.Backpack != null)
                    foreach (object o in m.Backpack.GetDeepItems())
                        if (o is Ticket ticket)
                            list.Add(ticket);

                // see if they have any tickets to this venue
                bool found = false;
                foreach (Ticket ticket in list)
                    if (!string.IsNullOrEmpty(ticket.Passcode))
                        if (TicketPasscodeText.Equals(ticket.Passcode, StringComparison.OrdinalIgnoreCase))
                            if (ticket.Expired == false)
                                found = true;

                // did not have a ticket to this venue
                if (found == false)
                {
                    m.SendMessage("You must have a valid ticket to this venue, you cannot pass.");
                    return false;
                }
            }

            return true;
        }
        #endregion Special Restrictions
        private void ConfigureFlags()
        {
            if (!ValidSparkle())
            {
                SetSpecialFlag(TeleporterFlags.Sparkle, false);
                SetSpecialFlag(TeleporterFlags.SparkleHue, false);
            }
        }
        private void UpdateItemID()
        {
            if (ItemID != 0x1BC3 && ItemID != 0x1BC4 && ItemID != 0x1BC5)
                return;

            if (Directional)
            {
                if (Direction == Direction.East || Direction == Direction.West)
                    ItemID = 0x1BC5;
                else
                    ItemID = 0x1BC4;
            }
            else
            {
                ItemID = 0x1BC3;
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)10);                   // version
            ConfigureFlags();
            writer.Write((int)m_SpecialAccess);     // must always follow version

            // version 10
            if (GetSpecialFlag(TeleporterFlags.TicketPasscode))
                writer.Write(m_TicketPasscode);

            // version 9
            {
                if (GetSpecialFlag(TeleporterFlags.Sparkle))
                    writer.Write(m_Sparkle);

                if (GetSpecialFlag(TeleporterFlags.SparkleHue))
                    writer.Write(m_SparkleHue);
            }

            // version 8
            //  removed in version 9
            //writer.Write((int)m_SpecialAccess);

            // version 7
            writer.Write((int)m_AccessLevel);

            // version 6 (I guess)
            //writer.Write((bool)m_TransportPets); removed in version 8
            writer.Write(m_PetMessage);
            writer.Write(m_PointDest2);
            writer.Write(m_PointDest3);
            writer.Write(m_PointDest4);
            writer.Write(m_PointDest5);
            writer.Write(m_RectStart);
            writer.Write(m_RectEnd);
            //writer.Write((bool)m_Criminal);       removed in version 8
            writer.Write(m_DelayMessage);
            //writer.Write((bool)m_SparkleEffect);  removed in version 8
            //writer.Write((bool)m_SourceEffect);   removed in version 8
            //writer.Write((bool)m_DestEffect);     removed in version 8
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            //writer.Write(m_Creatures);            removed in version 8

            //writer.Write(m_Active);               removed in version 8
            writer.Write(m_PointDest);
            writer.Write(m_MapDest);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            if (version > 8)
                m_SpecialAccess = (TeleporterFlags)reader.ReadInt();

            switch (version)
            {
                case 10:
                    {
                        if (GetSpecialFlag(TeleporterFlags.TicketPasscode))
                            m_TicketPasscode = reader.ReadString();
                        goto case 9;
                    }
                case 9:
                    {
                        if (GetSpecialFlag(TeleporterFlags.Sparkle))
                            m_Sparkle = reader.ReadItem();
                        if (GetSpecialFlag(TeleporterFlags.SparkleHue))
                            m_SparkleHue = reader.ReadInt();
                        goto case 8;
                    }
                case 8:
                    {
                        if (version < 9)
                            m_SpecialAccess = (TeleporterFlags)reader.ReadInt();
                        goto case 7;
                    }
                case 7:
                    {
                        m_AccessLevel = (AccessLevel)reader.ReadInt();
                        goto case 6;
                    }
                case 6:
                    {
                        if (version < 8)
                            SetSpecialFlag(TeleporterFlags.Pets, reader.ReadBool());
                        m_PetMessage = reader.ReadString();
                        goto case 5;
                    }

                case 5:
                    {
                        m_PointDest2 = reader.ReadPoint3D();
                        m_PointDest3 = reader.ReadPoint3D();
                        m_PointDest4 = reader.ReadPoint3D();
                        m_PointDest5 = reader.ReadPoint3D();
                        m_RectStart = reader.ReadPoint2D();
                        m_RectEnd = reader.ReadPoint2D();

                        goto case 4;
                    }

                case 4:
                    {
                        if (version < 8)
                            SetSpecialFlag(TeleporterFlags.Criminals, reader.ReadBool());

                        goto case 3;
                    }

                case 3:
                    {
                        m_DelayMessage = reader.ReadString();
                        if (version < 8)
                            SetSpecialFlag(TeleporterFlags.SparkleEffect, reader.ReadBool());

                        goto case 2;
                    }

                case 2:
                    {
                        if (version < 8)
                            SetSpecialFlag(TeleporterFlags.SourceEffect, reader.ReadBool());
                        if (version < 8)
                            SetSpecialFlag(TeleporterFlags.DestEffect, reader.ReadBool());

                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 8)
                            SetSpecialFlag(TeleporterFlags.Creatures, reader.ReadBool());

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 8)
                            SetSpecialFlag(TeleporterFlags.Running, reader.ReadBool());
                        m_PointDest = reader.ReadPoint3D();
                        m_MapDest = reader.ReadMap();

                        break;
                    }
            }

            if (version < 7)
            {
                m_AccessLevel = AccessLevel.Player;
            }

            if (version < 6)
            {
                SetSpecialFlag(TeleporterFlags.Pets, true);
                m_PetMessage = null;

            }

            UpdateItemID();
        }
    }
    public class EventTeleporter : Teleporter
    {
        public override Item Dupe(int amount)
        {
            EventTeleporter new_item = new EventTeleporter();
            Utility.CopyProperties(this, new_item);
            return base.Dupe(new_item, amount);
        }
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
        // don't make a command property, we don't want GM's starting/stopping in this way
        public override bool Running
        {
            get { return base.Running; }
            set
            {
                base.Running = value;
                InvalidateProperties();
            }
        }
        [Constructable]
        public EventTeleporter()
            : base()
        {
            m_event = new Event(this, null, EventStarted, EventEnded);
            base.Running = false;
            base.Sparkle = true;
        }
        public override void OnSingleClick(Mobile from)
        {
            if (!base.Running)
            {
                if (!EventRunning)
                {
                    LabelTo(from, "event not running");
                    if (Countdown > TimeSpan.Zero)

                        LabelTo(from, string.Format("{0} activating in {1} seconds",
                            this.GetType().Name, string.Format("{0:N2}", this.Countdown.TotalSeconds)));
                }
                else
                    LabelTo(from, "event running");

                if (!DestinationOverride)
                {
                    bool jail;
                    bool result = SpellHelper.CheckTravel(GetMap(from), PointDest, IsGate ? TravelCheckType.GateTo : TravelCheckType.TeleportTo, from, out jail);
                    if (!result)
                        LabelTo(from, "a destination override is required");
                }

                LabelTo(from, "(inactive)");
            }
            else
                base.OnSingleClick(from);
        }
        public EventTeleporter(Serial serial)
        : base(serial)
        {

        }
        public virtual void EventStarted(object o)
        {
            base.Running = true;
            if (Core.Debug && false)
                Utility.Monitor.WriteLine(string.Format("{0} got 'Event started' event.", this), ConsoleColor.Yellow);
        }
        public virtual void EventEnded(object o)
        {
            base.Running = false;
            if (Core.Debug && false)
                Utility.Monitor.WriteLine(string.Format("{0} got 'Event ended' event.", this), ConsoleColor.Yellow);
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
    public class SkillTeleporter : Teleporter
    {
        public override Item Dupe(int amount)
        {
            SkillTeleporter new_item = new SkillTeleporter();
            Utility.CopyProperties(this, new_item);
            return base.Dupe(new_item, amount);
        }
        private SkillName m_Skill;
        private double m_Required;
        private string m_MessageString;
        private int m_MessageNumber;

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill
        {
            get { return m_Skill; }
            set { m_Skill = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Required
        {
            get { return m_Required; }
            set { m_Required = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string MessageString
        {
            get { return m_MessageString; }
            set { m_MessageString = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageNumber
        {
            get { return m_MessageNumber; }
            set { m_MessageNumber = value; InvalidateProperties(); }
        }

        private void EndMessageLock(object state)
        {
            ((Mobile)state).EndAction(this);
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (Running)
            {
                if (!Pets && !m.Player)
                    return true;

                Skill sk = m.Skills[m_Skill];

                if (sk == null || sk.Base < m_Required)
                {
                    if (m.BeginAction(this))
                    {
                        if (m_MessageString != null)
                            m.Send(new UnicodeMessage(Serial, ItemID, MessageType.Regular, 0x3B2, 3, "ENU", null, m_MessageString));
                        else if (m_MessageNumber != 0)
                            m.Send(new MessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, m_MessageNumber, null, ""));

                        Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerStateCallback(EndMessageLock), m);
                    }

                    return false;
                }

                StartTeleport(m);
                return false;
            }

            return true;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            int skillIndex = (int)m_Skill;
            string skillName;

            if (skillIndex >= 0 && skillIndex < SkillInfo.Table.Length)
                skillName = SkillInfo.Table[skillIndex].Name;
            else
                skillName = "(Invalid)";

            list.Add(1060661, "{0}\t{1:F1}", skillName, m_Required);

            if (m_MessageString != null)
                list.Add(1060662, "Message\t{0}", m_MessageString);
            else if (m_MessageNumber != 0)
                list.Add(1060662, "Message\t#{0}", m_MessageNumber);
        }

        [Constructable]
        public SkillTeleporter()
        {
        }

        public SkillTeleporter(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Skill);
            writer.Write((double)m_Required);
            writer.Write((string)m_MessageString);
            writer.Write((int)m_MessageNumber);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Skill = (SkillName)reader.ReadInt();
                        m_Required = reader.ReadDouble();
                        m_MessageString = reader.ReadString();
                        m_MessageNumber = reader.ReadInt();

                        break;
                    }
            }
        }
    }
    public class KeywordTeleporter : Teleporter
    {
        public override Item Dupe(int amount)
        {
            KeywordTeleporter new_item = new KeywordTeleporter();
            Utility.CopyProperties(this, new_item);
            return base.Dupe(new_item, amount);
        }
        private string? m_Substring;
        private int m_Keyword;
        private int m_Range;
        public override void OnTrigger(Mobile from)
        {
            StartTeleport(from);
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string? Substring
        {
            get { return m_Substring; }
            set { m_Substring = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Keyword
        {
            get { return m_Keyword; }
            set { m_Keyword = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; InvalidateProperties(); }
        }
        //public override bool HandlesOnMovement { get { return true; } }

        //public override void OnMovement(Mobile m, Point3D oldLocation)
        //{   // teleporters are *very* sensitive to z-order. If the teleporter is placed at z=3, and you are standing at z=6,
        //    //  the teleporter won't fire. (in fact items don't get the OnMoveOver in this case either.)
        //    // this helper bumps an OnMoveOver in such cases
        //    int myZ = Z;
        //    Point2D myXY = new Point2D(X, Y);
        //    int theirZ = m.Z;
        //    Point2D theirXY = new Point2D(m.X, m.Y);
        //    if (myXY != theirXY)
        //        return;

        //    if (Math.Abs(myZ - theirZ) <= 3)
        //        OnMoveOver(m);
        //}
        public override bool HandlesOnSpeech { get { return true; } }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && Running)
            {
                Mobile m = e.Mobile;

                if (!Pets && !m.Player)
                    return;

                if (!m.InRange(GetWorldLocation(), m_Range))
                    return;

                bool isMatch = false;

                if (m_Keyword >= 0 && e.HasKeyword(m_Keyword))
                    isMatch = true;
                else if (m_Substring != null && e.Speech.ToLower().IndexOf(m_Substring.ToLower()) >= 0)
                    isMatch = true;

                if (!isMatch)
                    return;

                e.Handled = true;
                StartTeleport(m);
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            return true;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060661, "Range\t{0}", m_Range);

            if (m_Keyword >= 0)
                list.Add(1060662, "Keyword\t{0}", m_Keyword);

            if (m_Substring != null)
                list.Add(1060663, "Substring\t{0}", m_Substring);
        }

        [Constructable]
        public KeywordTeleporter()
        {
            m_Keyword = -1;
            m_Substring = null;
        }

        public KeywordTeleporter(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Substring);
            writer.Write(m_Keyword);
            writer.Write(m_Range);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Substring = reader.ReadString();
                        m_Keyword = reader.ReadInt();
                        m_Range = reader.ReadInt();

                        break;
                    }
            }
        }
    }
    public class EventKeywordTeleporter : EventTeleporter
    {
        public override Item Dupe(int amount)
        {
            EventKeywordTeleporter new_item = new EventKeywordTeleporter();
            Utility.CopyProperties(this, new_item);
            return base.Dupe(new_item, amount);
        }
        private Event m_event;
        #region Event Properties
        #endregion Event Properties
        // don't make a command property, we don't want GM's starting/stopping in this way
        public override bool Running
        {
            get { return base.Running; }
            set
            {
                base.Running = value;
                InvalidateProperties();
            }
        }
        [Constructable]
        public EventKeywordTeleporter()
            : base()
        {
            m_event = new Event(this, null, EventStarted, EventEnded);
            base.Running = false;
            base.Sparkle = true;
        }
        public EventKeywordTeleporter(Serial serial)
            : base(serial)
        {

        }
        public override void EventStarted(object o)
        {
            base.Running = true;
            if (Core.Debug && false)
                Utility.Monitor.WriteLine(string.Format("{0} got 'Event started' event.", this), ConsoleColor.Yellow);
        }
        public override void EventEnded(object o)
        {
            base.Running = false;
            if (Core.Debug && false)
                Utility.Monitor.WriteLine(string.Format("{0} got 'Event ended' event.", this), ConsoleColor.Yellow);
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
    /// <summary>
    /// TestCenterTeleporter - Advanced clone-based Test Center system.
    /// 
    /// This teleporter supports three modes:
    ///   - None  : Normal teleporter behavior (default)
    ///   - To    : Real player steps on → creates disposable clone, transfers NetState, parks original
    ///   - From  : Clone steps on → swaps NetState back to original player, then deletes the clone
    /// 
    /// Key Design:
    ///   - Clones are completely disposable (dummy account + owned properties are deleted on delete)
    ///   - Original player is safely stored on internal map while clone is active
    ///   - Full NetState + packet handoff ensures smooth client transition in both directions
    ///   - Strong safety: clones cannot re-enter, non-clones cannot exit from inside TC
    ///   - Cron job catches and jails any clones that escape into the world
    /// 
    /// Logout/Reconnect Behavior:
    ///   If a clone logs out, the original player is restored on next login and the clone is cleaned up.
    /// 
    /// All clone-related penalties (jail, inventory wipe, etc.) are handled via ApplyTestCenterPenalties().
    /// </summary>
    public class TestCenterTeleporter : Teleporter
    {
        public override Item Dupe(int amount)
        {
            TestCenterTeleporter new_item = new TestCenterTeleporter();
            Utility.CopyProperties(this, new_item);
            return base.Dupe(new_item, amount);
        }
        public enum TestCenterMode
        {
            None,   // Normal teleporter behavior
            To,     // Player enters Test Center (create clone)
            From    // Clone exits Test Center (return to original)
        }

        private TestCenterMode m_Mode = TestCenterMode.None;

        [CommandProperty(AccessLevel.GameMaster)]
        public TestCenterMode Mode
        {
            get { return m_Mode; }
            set { m_Mode = value; InvalidateProperties(); }
        }

        [Constructable]
        public TestCenterTeleporter()
        {
        }

        public TestCenterTeleporter(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
                                  // version 1
            writer.Write((int)m_Mode);

            // version 0
            // nothing to do
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    { // added TestCenterMode
                        m_Mode = (TestCenterMode)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    { // initial version
                        break;
                    }
            }
        }

        public override void DoTeleport(Mobile m)
        {
            if (m == null || !m.Player || !(m is PlayerMobile player))
            {
                base.DoTeleport(m);
                return;
            }

            // Staff always bypass cloning and get normal teleport behavior
            if (player.AccessLevel > AccessLevel.Player)
            {
                m.SendMessage("Your godly powers spare you from the transformation mortals experience here.");
                base.DoTeleport(m);
                return;
            }

            switch (m_Mode)
            {
                case TestCenterMode.None:
                    base.DoTeleport(m);
                    return;

                case TestCenterMode.To:
                    if (!player.Clone)
                    {
                        // a misplaced entry portal is refused, never used (TestCenterPolicy.ValidatePortal)
                        string? badEntry = TestCenterPolicy.ValidatePortal(this, original: null);
                        if (badEntry != null)
                        {
                            TestCenterPolicy.ReportPortal(this, player, badEntry);
                            return;
                        }

                        // Real player entering → create clone and send into Test Center
                        PlayerMobile clone = HandleEnterTestCenter(player);
                        CommandHandlers.BroadcastMessage(AccessLevel.Administrator, 0x482, $"[TC] {player.Name} entered Test Center (clone: {clone.Serial})");
                    }
                    else
                    {
                        // A clone is trying to re-enter the Test Center (not allowed)
                        TestCenterPolicy.Enforce(player, "Clone caught outside Test Center (trying to enter).");
                    }
                    return;

                case TestCenterMode.From:
                    if (player.Clone)
                    {
                        // a misplaced exit portal, or one that would put the original back in Test Center, is refused
                        string? badExit = TestCenterPolicy.ValidatePortal(this, FindOriginal(player));
                        if (badExit != null)
                        {
                            TestCenterPolicy.ReportPortal(this, player, badExit);
                            return;
                        }

                        // Valid clone exiting → return to original character
                        PlayerMobile? original = HandleExitTestCenter(player);
                        CommandHandlers.BroadcastMessage(AccessLevel.Administrator, 0x482, $"[TC] {original?.Name} returned from Test Center (clone removed: {player.Serial})");
                    }
                    else
                    {
                        // Non-clone player trying to use the exit teleporter (abuse)
                        TestCenterPolicy.Enforce(player, "Player caught inside Test Center.");
                    }
                    return;
            }
        }

        private PlayerMobile HandleEnterTestCenter(PlayerMobile player)
        {
            // 1. Create new clone
            PlayerMobile clone = BuildClone(player);
            clone.OwnerGUID = player.PlayerGUID; // clone points back to player

            PerformNetStateSwap(player, clone, TestCenterMode.To);

            // Load the clones bankbox with testcenter goods
            CharacterCreation.FillBankbox(clone);

            // ======================
            // Now let the base class teleport the NEW mobile
            // ======================
            base.DoTeleport(clone);

            // when the player logs back in, they will be here - near the teleporter they entered
            player.LogoutLocation = player.Location = GetLogoutLocation(start: this.Location, this.Map, distance: 3);
            player.LogoutMap = player.Map = this.Map;

            // Park the original mobile on internal map
            player.MoveToIntStorage(PreserveLocation: true);

            return clone;
        }
        private static Point3D GetLogoutLocation(Point3D start, Map map, int distance)
        {
            if (map == null || map == Map.Internal || distance <= 0)
                return start;

            HashSet<Point2D> blocked = new HashSet<Point2D>();

            IPooledEnumerable eable = map.GetItemsInRange(start, distance);

            foreach (Item item in eable)
            {
                if (item == null || item.Deleted)
                    continue;

                if (item is Teleporter)
                    blocked.Add(new Point2D(item.X, item.Y));
            }

            eable.Free();

            for (int r = 1; r <= distance; r++)
            {
                for (int x = start.X - r; x <= start.X + r; x++)
                {
                    for (int y = start.Y - r; y <= start.Y + r; y++)
                    {
                        // Block entire vertical column
                        if (blocked.Contains(new Point2D(x, y)))
                            continue;

                        int newZ = start.Z;

                        if (Utility.CanSpawnMobile(map, x, y, ref newZ))
                            return new Point3D(x, y, newZ);
                    }
                }
            }

            return start;
        }
        private PlayerMobile BuildClone(PlayerMobile source)
        {
            // 1. Create new clone
            // link everything up so that when we delete the account, we delete 'owned' properties
            string username = "Clone:" + KeyGenerator.Alloc().ToString();
            string password = KeyGenerator.Alloc().ToString();
            Account? acct = Accounts.AddAccount(username, password);
            acct.GUID = Utility.MakeGuid(acct.Username, acct.Username);
            PlayerMobile clone = new PlayerMobile(acct.GUID);
            clone.Account = acct;

            // associate this clone with the character array in it's account
            for (int i = 0; i < 5; ++i)
                if (acct[i] == null || acct[i].Deleted)
                {
                    acct[i] = clone;
                    break;
                }

            // copy the clothes, jewelry, everything
            Utility.CopyLayers(clone, source, CopyLayerFlags.Default);
            Utility.CopySkills(clone, source, include_stats: true);

            clone.Backpack?.Delete();
            clone.BankBox?.Delete();
            if (clone.Backpack == null)
            {
                Backpack backpack = new Backpack();
                backpack.Movable = false;
                clone.Backpack = backpack;
                backpack.TrapOnDelete = true;
            }
            if (clone.BankBox == null)
            {
                BankBox bankBox = new BankBox(clone);
                bankBox.Movable = false;
                clone.BankBox = bankBox;
                bankBox.TrapOnDelete = true;
            }

            // now everything else
            clone.Name = source.Name;
            clone.Hue = source.Hue;
            clone.Body = (source.Female) ? 401 : 400; // get the correct body
            clone.Female = source.Female; // get the correct sex
            clone.Title = source.Title; // get the title
            clone.Player = source.Player;
            clone.Mortal = source.Mortal;
            clone.AccessLevelInternal = source.AccessLevel;

            clone.Clone = true; // I am a clone

            return clone;
        }

        // the original player, by the OwnerGUID that was set during clone creation
        private static PlayerMobile? FindOriginal(PlayerMobile clone)
        {
            foreach (PlayerMobile pm in PlayerMobile.Instances)
                if (pm != null && !pm.Deleted && pm.PlayerGUID == clone.OwnerGUID)
                    return pm;
            return null;
        }

        private PlayerMobile? HandleExitTestCenter(PlayerMobile clone)
        {
            // Find the original player using the OwnerGUID that was set during clone creation
            PlayerMobile? original = FindOriginal(clone);

            if (original == null)
            {
                System.Diagnostics.Debug.Assert(false, $"Could not locate original player for clone {clone}");
                // Fallback: just delete the clone so it doesn't leak
                clone.MoveToIntStorage();
                clone.Delete();
                return original;
            }

            // Since PerformNetStateSwap performs login-sequence packets, we need to supress the 'welcome message'
            original.SetPlayerBool(PlayerBoolTable.SuppressNextWelcomeMessage, true);

            // Perform the NetState swap back to the original character
            PerformNetStateSwap(clone, original, TestCenterMode.From);

            // Teleport the original player back to the real world
            base.DoTeleport(original);

            // Delete the clone. This will delete its dummy account and clean up any owned properties.
            clone.MoveToIntStorage();
            clone.Delete();

            return original;
        }

        /// <summary>
        /// Performs the NetState swap and all required client packet synchronization.
        /// This is the core identity handoff used when moving between real player and clone.
        /// </summary>
        /// <param name="srcMobile">The mobile currently controlling the client (being detached)</param>
        /// <param name="dstMobile">The mobile that will take control of the client (being attached)</param>
        /// <param name="direction">Whether we are entering or exiting the Test Center</param>
        private void PerformNetStateSwap(PlayerMobile srcMobile, PlayerMobile dstMobile, TestCenterMode direction)
        {
            // 1. Move destination mobile to its final world location BEFORE sending packets
            // This reduces client-side flicker and desync caused by late movement packets.
            if (direction == TestCenterMode.From)
            {
                // Exiting Test Center: restore original player to their saved logout location
                // Note: the original player is still on internal map, so we have to move them back to the world first before teleporting
                //  the subsiquent teleport will then move them to the correct location near the felucca teleporter entrance
                dstMobile.MoveToWorld(dstMobile.LogoutLocation, dstMobile.LogoutMap ?? Map.Felucca);
            }
            else
            {
                // Entering Test Center: place the new clone at the teleporter's destination
                dstMobile.MoveToWorld(this.PointDest, this.MapDest ?? Map.Trammel);
            }

            // 2. Transfer NetState (critical: rebind the live client session)
            NetState ns = srcMobile.NetState;
            if (ns != null)
            {
                srcMobile.NetState = null;           // detach from source mobile
                dstMobile.NetState = ns;             // attach to destination mobile
                ns.Mobile = dstMobile;               // re-anchor the session
            }

            // 3. Retire the old mobile from the client's view
            if (srcMobile != null && ns != null)
                ns.Send(new RemoveMobile(srcMobile)); // breaks old identity on client

            // 4. Re-establish identity and synchronize full state using the canonical login pipeline
            // This is more reliable than sending individual packets manually
            PacketHandlers.DoLogin(ns, dstMobile);

            // 5. Force container and equipment deltas (ensures backpack, bank, and equipped items appear correctly)
            dstMobile.Backpack?.Delta(ItemDelta.Update);
            dstMobile.Backpack?.ProcessDelta();
            dstMobile.BankBox?.Delta(ItemDelta.Update);
            dstMobile.BankBox?.ProcessDelta();

            // 6. Run custom login logic (Seasonal account handling, etc.)
            OnLogin(dstMobile);
        }
        public override bool TryUseTeleport(Mobile m)
        {
            Map map = GetMap(m);

            if (map != null && map != Map.Internal)
            {
                if (GetSpecialFlag(TeleporterFlags.DropDragging))
                    m.DropDragging();

                StartTeleport(m);
                if (GetSpecialFlag(TeleporterFlags.SparkleEffect))
                    Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(SendSparkles_Callback), m);

                return true;
            }
            else if (IsGate)
                m.SendMessage("This Moongate does not seem to go anywhere.");

            return true;
        }
        private void OnLogin(Mobile m)
        {
            #region Seasons
            // Ensure this account has a valid seasonal leaderboard identity.
            // If the seasonal account already exists, this will populate DisplayName
            // once the player has chosen a real name.
            if (Core.RuleSets.SeasonsRules() && m.Clone)
                SeasonManager.EnsureSeasonalAccount(m.AcctGUID, m.Name + " " + "(clone)");
            #endregion Seasons
        }
    }

}