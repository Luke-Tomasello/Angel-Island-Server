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
 * ChangeLog :
 * 11/14/22, Adam (IHasUsesRemaining)
 *  Now implements IHasUsesRemaining to consume uses on Siege, but not on other shards.
 *      Like IUsesRemaining, IHasUsesRemaining consumes uses, but is dynamic where 
 *      IUsesRemaining is not.
 *  11/23/21, Adam (HerdingImmune)
 *      Add support for the new HerdingImmune property
 *  04/16/05, erlein
 *    - Fixed problem with new crooks where they were disallowed.
 *    - Changed so crook stores date/time of last use rather than initializing
 *      a timer.
 *	03/31/05, erlein
 *		- Altered taming formula so 20% chance at GM herd with max loyalty pet and
 *			more consisted scaling of difficulty.
 *		- Allowed wild beast herding.
 *		- Added 2 second delay between herding attempts (note - specific to crook)
 *		- Reformatted for readability (was all double spaced)
 *	05/18/04, PanchoVilla
 *		modified script to make it more difficult to herd a tamed animal
 *
 */

using Server.Mobiles;
using Server.Targeting;
using System;


namespace Server.Items
{
    [FlipableAttribute(0xE81, 0xE82)]

    public class ShepherdsCrook : BaseStaff, IHasUsesRemaining
    {
        #region Weapon Attributes
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.CrushingBlow; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Disarm; } }


        //		public override int AosStrengthReq{ get{ return 20; } }
        //		public override int AosMinDamage{ get{ return 13; } }
        //		public override int AosMaxDamage{ get{ return 15; } }
        //		public override int AosSpeed{ get{ return 40; } }
        //
        public override int OldMinDamage { get { return 3; } }
        public override int OldMaxDamage { get { return 12; } }

        public override int OldStrengthReq { get { return 10; } }
        public override int OldSpeed { get { return 30; } }

        public override int OldDieRolls { get { return 3; } }
        public override int OldDieMax { get { return 4; } }
        public override int OldAddConstant { get { return 0; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 50; } }
        #endregion Weapon Attributes
        private DateTime m_LastUsed;
        public DateTime LastUsed
        {
            get
            {
                return m_LastUsed;
            }
            set
            {
                m_LastUsed = value;
            }

        }

        [Constructable]
        public ShepherdsCrook()
            : base(0xE81)
        {
            Weight = 4.0;
            m_usesRemaining = 200;
        }

        #region UsesRemaining
        public bool WearsOut { get { return Core.RuleSets.SiegeStyleRules(); } }
        public int ToolBrokeMessage => 502470; // You broke your shepherd's crook.
        int m_usesRemaining;
        // staff don't need to see this
        [CommandProperty(AccessLevel.Owner)]
        public int UsesRemaining { get { return m_usesRemaining; } set { m_usesRemaining = value; } }
        public override void OnActionComplete(Mobile from, Item tool)
        {
            if (this == tool && Utility.Inventory(from).Contains(this))
                // fishing poles only wear out on Siege
                if (WearsOut)
                    ConsumeUse(from);
        }
        public void ConsumeUse(Mobile from)
        {
            // diminish uses

            if (UsesRemaining > 0)
                UsesRemaining--;

            if (UsesRemaining < 1)
            {
                Delete();
                from.SendLocalizedMessage(ToolBrokeMessage);
            }
        }
        #endregion UsesRemaining
        public ShepherdsCrook(Serial serial)
            : base(serial)
        {
            LastUsed = DateTime.UtcNow;
        }


        // old name removed, see base class

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            // version 1
            writer.Write(m_usesRemaining);
        }


        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_usesRemaining = reader.ReadInt();
                        break;
                    }
                case 0:
                    {
                        if (Weight == 2.0)
                            Weight = 4.0;
                        break;
                    }
            }

            LastUsed = DateTime.UtcNow;
        }


        public override void OnDoubleClick(Mobile from)
        {
            DateTime now = DateTime.UtcNow;

            if (now > LastUsed + TimeSpan.FromSeconds(2.0))
            {
                from.SendLocalizedMessage(502464); // Target the animal you wish to herd.
                from.Target = new HerdingTarget(this);
                LastUsed = now;
            }
            else
                // Not allowed to use yet
                from.SendMessage("You must wait before using this crook again.");

        }

        private class HerdingTarget : Target
        {
            private Item m_tool;
            public HerdingTarget(Item tool)
                : base(10, false, TargetFlags.None)
            {
                m_tool = tool;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is BaseCreature)
                {
                    BaseCreature bc = (BaseCreature)targ;

                    if (bc.Body.IsAnimal && !bc.HerdingImmune)
                    {
                        from.SendLocalizedMessage(502475); // Click where you wish the animal to go.
                        from.Target = new InternalTarget(m_tool, bc);
                    }
                    else
                    {
                        from.SendLocalizedMessage(502468); // That is not a herdable animal.
                    }

                }
                else
                {
                    from.SendLocalizedMessage(502472); // You don't seem to be able to persuade that to move.
                }
            }


            private class InternalTarget : Target
            {
                private Item m_tool;
                private BaseCreature m_Creature;

                public InternalTarget(Item tool, BaseCreature c)
                    : base(10, true, TargetFlags.None)
                {
                    m_tool = tool;
                    m_Creature = c;
                }

                protected override void OnTarget(Mobile from, object targ)
                {
                    if (targ is IPoint2D)
                    {
                        int LoyaltyLev = (int)m_Creature.LoyaltyValue;

                        if (from.CheckTargetSkill(SkillName.Herding, m_Creature, 0, 100, new object[2] { m_Creature, null }/*contextObj*/))
                        {
                            // erl: ammended formula so scales proportionately and without
                            // bias + allow non controled beast herding (assumes 88% chance
                            // of success if wild)

                            if (
                                    (
                                        (
                                            (
                                                (from.Skills[SkillName.Herding].Value / 100) - (m_Creature.Controlled ? (LoyaltyLev / 11) : 0)
                                            ) * 0.66
                                                + 0.2
                                                + Utility.RandomDouble()
                                        ) > 1
                                    )
                                 || (from == m_Creature.ControlMaster)
                                )
                            {

                                // Able
                                m_Creature.Herder = from;
                                m_Creature.HerdTime = DateTime.UtcNow;
                                m_Creature.TargetLocation = new Point2D((IPoint2D)targ);

                                from.SendLocalizedMessage(502479); // The animal walks where it was instructed to.
                            }
                            else
                                // Unable
                                from.SendLocalizedMessage(502472); // You don't seem to be able to persuade that to move.
                        }
                        else
                            from.SendLocalizedMessage(502472); // You don't seem to be able to persuade that to move.

                        // consume uses
                        if (m_tool != null)
                            m_tool.OnActionComplete(from, m_tool);
                    }
                }
            }
        }
    }
}