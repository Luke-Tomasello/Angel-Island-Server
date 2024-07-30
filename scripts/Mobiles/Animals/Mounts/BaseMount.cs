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
 * Scripts/Mobiles/Animals/Mounts/BaseMount.cs
 * ChangeLog:
 *  1/24/24, Yoar
 *      Mounts now dynamically instantiate MountItem as they need them.
 *  10/14/2023, Adam
 *      Since we no longer leave all creature AIs running all the time, we need to restart the AI once a player dismounts their mount
 *  3/11/23, Yoar
 *      Removed m_riderStamAccumulator, using PlayerMobile.StamDrain instead
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *  09/25/08, Adam
 *		TURN OFF MOUNTS 
 *		Seems there is at least one client out there that moves mounted players faster regardless of the server settings.
 *		We will need to disable this until this is better understood.
 *  09/23/08, Adam
 *		TURN ON MOUNTS
 *		Comment out the code in OnDoubleClick() that was preventing players from riding a mountable creature. 
 *		Please see ComputeMovementSpeed() in PlayerMobile.cs for changes to Mount Speed.
 *	6/20/2004 - Pixie
 *		Fixed problem with old mountables disappearing from the stables when the server restarts.
 *		Was a problem with the MountItem constructor - if m_Mount was set to null, the next
 *		deserialize would delete the MountItem, and if the MountItem in BaseMount was null, the
 *		BaseMount would delete itself.
 * 3/18/04 code changes by smerX:
 *	"OnDoubleClick( Mobile from )" directed straight to "OnDisallowedRider( Mobile m )"
 * - Mounts are no longer mountable.
*/

namespace Server.Mobiles
{
    public abstract class BaseMount : BaseCreature, IMount
    {
        private Mobile m_Rider;
        private Item m_InternalItem;
        private int m_ItemID;
        public Item InternalItem
        {
            get { return m_InternalItem; }
            set { m_InternalItem = value; }
        }
        public virtual bool AllowMaleRider { get { return true; } }
        public virtual bool AllowFemaleRider { get { return true; } }

        public BaseMount(string name, int bodyID, int itemID, AIType aiType, FightMode fightMode, int rangePerception, int rangeFight, double activeSpeed, double passiveSpeed)
            : base(aiType, fightMode, rangePerception, rangeFight, activeSpeed, passiveSpeed)
        {
            Name = name;
            Body = bodyID;

            m_ItemID = itemID;
        }

        public BaseMount(Serial serial)
            : base(serial)
        {
        }

        public override Characteristics MyCharacteristics { get { return Characteristics.Run; } }

        #region Mount Stamina Refresh

        private Memory StamRefresh = new Memory();
        private int LastStepTaken = 0;

        public override void OnControlOrder(OrderType order)
        {   // do the old-school stam refresh on "all follow"
            if (Misc.WeightOverloading.MountStamina && order == OrderType.Follow)
            {
                // as usual, we we don't know the actual formula, make something up, but make it reasonable

                if (ControlMaster is PlayerMobile == false)     // null check + sanity
                    return;

                if (StamRefresh.Recall(ControlMaster) == true)  // too soon to refresh?
                    return;

                StamRefresh.Remember(ControlMaster, .250);      // 1/4 second delay between refreshes

                PlayerMobile pm = ControlMaster as PlayerMobile;

                if (pm.StepsTaken == LastStepTaken)             // you need to move (like you walk while the creature follows)
                    return;

                LastStepTaken = pm.StepsTaken;

                if (Stam >= StamMax)
                    return;

                /*
				 * Content = 60,
				 * Happy = 70,
				 * RatherHappy = 80,
				 * VeryHappy = 90,
				 * ExtremelyHappy = 100,
				 * WonderfullyHappy = 110
				 */
                if (LoyaltyValue < PetLoyalty.Content)
                    return;

                // bump from 12 - 22 stam points
                Stam += (int)LoyaltyValue / 5;
            }
        }

        #endregion

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get
            {
                return base.Hue;
            }
            set
            {
                base.Hue = value;

                if (m_InternalItem != null)
                    m_InternalItem.Hue = value;
            }
        }

        public override bool OnBeforeDeath()
        {
            Rider = null;

            return base.OnBeforeDeath();
        }

        public override void OnAfterDelete()
        {
            if (m_InternalItem != null)
                m_InternalItem.Delete();

            m_InternalItem = null;

            base.OnAfterDelete();
        }

        public override void OnDelete()
        {
            Rider = null;

            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 2;
            writer.Write(version); // version
            switch (version)
            {
                case 2:
                case 1:
                    {
                        writer.Write((int)m_ItemID);
                        goto case 0;
                    }
                case 0:
                    {
                        writer.Write(m_Rider);
                        writer.Write(m_InternalItem);
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
                case 1:
                    {
                        if (version >= 2)
                            m_ItemID = reader.ReadInt();
                        else
                            m_ItemID = reader.ReadUShort();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Rider = reader.ReadMobile();
                        m_InternalItem = reader.ReadItem();

                        break;
                    }
            }

            if (m_ItemID == 0)
                ValidationQueue<BaseMount>.Enqueue(this);
        }

        public void Validate()
        {
            if (m_ItemID == 0)
            {
                if (m_InternalItem != null)
                    m_ItemID = m_InternalItem.ItemID;
                else
                    m_ItemID = LookupItemID(BodyValue);
            }
        }

        public static int LookupItemID(int bodyValue)
        {
            // prefer code here over data-memory. I.e., no need to store this rarely used table in memory always.
            int[,] table = new int[,]
            {
                /*  Table for converting mount itemID to mount animation
                 *  Format:
                 *  Item, Body
                 */

                //  Raptalon
                {0x3E90,276},
                //  CuShidhe
                {0x3E91,277},
                //  MondainSteed01
                {0x3E92,284},
                //  Hai_Riyo
                {0x3E94,243},
                //  Gaint_Fire_Beetle
                {0x3E95,169},
                //  giant_beetle_ethereal
                {0x3E97,195},
                //  swamp_dragon_ethereal
                {0x3E98,194},
                //  ridgeback_ridgeback_ethereal
                {0x3E9A,193},
                //  equines_unicorn_ethereal
                {0x3E9B,192},
                //  kirin_kirin_ethereal
                {0x3E9C,191},
                //  equines_unicorn_ethereal
                {0x3E9D,192},
                //  equines_horse_firesteed
                {0x3E9E,190},
                //  equines_horse_dappled_brown
                {0x3E9F,200},
                //  equines_horse_dappled_grey
                {0x3EA0,226},
                //  equines_horse_tan
                {0x3EA1,228},
                //  equines_horse_dark_brown
                {0x3EA2,204},
                //  ostards_ostard_desert
                {0x3EA3,210},
                //  ostards_ostard_forest
                {0x3EA4,218},
                //  ostards_ostard_frenzied
                {0x3EA5,219},
                //  llamas_llama
                {0x3EA6,220},
                //  equines_horse_nightmare
                {0x3EA7,116},
                //  equines_horse_silver_steed
                {0x3EA8,117},
                //  equines_horse_dark_steed
                {0x3EA9,114},
                //  equines_horse_ethereal
                {0x3EAA,115},
                //  llamas_llama_ethereal
                {0x3EAB,170},
                //  ostards_ostard_ethereal
                {0x3EAC,171},
                //  kirin_kirin
                {0x3EAD,132},
                //  equines_horse_war_minax
                {0x3EAF,120},
                //  equines_horse_war_shadowlord
                {0x3EB0,121},
                //  equines_horse_war_mage_council
                {0x3EB1,119},
                //  equines_horse_war_brittanian
                {0x3EB2,118},
                //  sea_horse_sea_horse
                {0x3EB3,144},
                //  equines_unicorn
                {0x3EB4,122},
                //  equines_horse_nightmare2
                {0x3EB5,177},
                //  equines_horse_nightmare3
                {0x3EB6,178},
                //  equines_horse_nightmare4
                {0x3EB7,179},
                //  ridgeback_savage
                {0x3EB8,188},
                //  ridgeback_ridgeback
                {0x3EBA,187},
                //  skeletal_mount
                {0x3EBB,793},
                //  giant_beetle
                {0x3EBC,791},
                //  swamp_dragon
                {0x3EBD,794},
                //  swamp_dragon_armor
                {0x3EBE,799},
                //  bruins_bear_polar
                {0x3EC5,213},
            };

            for (int i = 0; i < table.GetLength(0); i++)
            {
                if (table[i, 1] == bodyValue)
                    return table[i, 0];
            }

            return 0;
        }

        public virtual void OnDisallowedRider(Mobile m)
        {
            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())    // when no mounts use this message
                m.SendMessage("That beast will not allow you to mount it.");
            else
                m.SendMessage("You may not ride this creature.");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!Core.RuleSets.AllowMountRules()) // "turn off" mounts certain shards (Angel Island for instance)
            {
                if (from.AccessLevel == AccessLevel.Player)
                {
                    OnDisallowedRider(from);
                    return;
                }
            }

            if (IsDeadPet)
                return; // TODO: Should there be a message here?

            if (from.IsBodyMod && !from.Body.IsHuman)
            {
                if (Core.RuleSets.AOSRules()) // You cannot ride a mount in your current form.
                    PrivateOverheadMessage(Network.MessageType.Regular, 0x3B2, 1062061, from.NetState);
                else
                    from.SendLocalizedMessage(1061628); // You can't do that while polymorphed.

                return;
            }

            if (!from.CanBeginAction(typeof(BaseMount)))
            {
                from.SendLocalizedMessage(1040024); // You are still too dazed from being knocked off your mount to ride!
                return;
            }

            if (from.Mounted)
            {
                from.SendLocalizedMessage(1005583); // Please dismount first.
                return;
            }

            if (from.Female ? !AllowFemaleRider : !AllowMaleRider)
            {
                OnDisallowedRider(from);
                return;
            }

            if (!Multis.DesignContext.Check(from))
                return;

            #region Dueling
            if (from is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)from;

                if (pm.DuelContext != null && pm.DuelPlayer != null && pm.DuelContext.Registered && pm.DuelContext.StartedBeginCountdown && !pm.DuelPlayer.Eliminated)
                    return;
            }
            #endregion

            if (from.InRange(this, 1))
            {
                if ((Controlled && ControlMaster == from) || (Summoned && SummonMaster == from) || from.AccessLevel >= AccessLevel.GameMaster)
                {
                    Rider = from;
                    from.OnMount(this);
                }
                else if (!Controlled && !Summoned)
                {
                    from.SendLocalizedMessage(501263); // That mount does not look broken! You would have to tame it to ride it.
                }
                else
                {
                    from.SendLocalizedMessage(501264); // This isn't your mount; it refuses to let you ride.
                }
            }
            else
            {
                from.SendLocalizedMessage(500206); // That is too far away to ride.
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ItemID
        {
            get { return m_ItemID; }
            set
            {
                m_ItemID = value;

                if (m_InternalItem != null)
                    m_InternalItem.ItemID = value;
            }
        }

        public static void Dismount(Mobile m)
        {
            IMount mount = m.Mount;

            if (mount != null)
                mount.Rider = null;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Rider
        {
            get
            {
                return m_Rider;
            }
            set
            {
                bool mountChanging = m_Rider != value;
                Mobile rider = m_Rider ?? value;
                if (mountChanging)
                {
                    if (value == null)
                    {
                        Point3D loc = m_Rider.Location;
                        Map map = m_Rider.Map;

                        if (map == null || map == Map.Internal)
                        {
                            loc = m_Rider.LogoutLocation;
                            map = m_Rider.LogoutMap;
                        }

                        Direction = m_Rider.Direction;
                        Location = loc;
                        Map = map;

                        if (this.AIObject != null)
                            // reignite the AI Timer
                            this.AIObject.m_Timer.Start();

                        if (m_InternalItem != null)
                            m_InternalItem.Internalize();
                    }
                    else
                    {
                        if (m_Rider != null)
                            Dismount(m_Rider);

                        Dismount(value);

                        if (m_InternalItem == null || m_InternalItem.Deleted)
                        {
                            m_InternalItem = new MountItem(this, m_ItemID);
                            m_InternalItem.Hue = Hue;
                        }

                        if (m_InternalItem != null)
                            value.AddItem(m_InternalItem);

                        value.Direction = this.Direction;

                        Internalize();
                    }

                    Mobile oldRider = m_Rider;

                    m_Rider = value;

                    if (oldRider != null)
                        oldRider.OnDismount(this);
                }

                // notify the mount changes and make any necessary speed adjustments
                if (mountChanging && rider != null && rider is BaseCreature bc && bc.AIObject != null)
                    bc.AIObject.OnMountChanged();
            }
        }
    }

    public class MountItem : Item, IMountItem
    {
        private BaseMount m_Mount;

        public MountItem(BaseMount mount, int itemID)
            : base(itemID)
        {
            Layer = Layer.Mount;
            Movable = false;

            m_Mount = mount;
        }

        public MountItem(Serial serial)
            : base(serial)
        {
        }

        public override void OnAfterDelete()
        {
            m_Mount = null;

            base.OnAfterDelete();
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            if (m_Mount != null)
                m_Mount.Rider = null;

            return DeathMoveResult.RemainEquiped;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Mount);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Mount = reader.ReadMobile() as BaseMount;

                        if (m_Mount == null)
                            Delete();

                        break;
                    }
            }
        }

        public IMount Mount
        {
            get
            {
                return m_Mount;
            }
        }
    }
}