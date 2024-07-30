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

/* Scripts/Items/Body Parts/Remains.cs
 * CHANGELOG:
 *  11/21/10, Adam 
 *      Add RibCage and BonePile (needed for IceSerpent loot)
 *	7/12/08, weaver
 *		Added Dupe() override to ensure human meat is properly duplicated.
 *	4/17/07 - Pix.
 *		Fixed eating of jerky to take into account whether it's locked down,
 *		how close you are, and how full you are.
 *		Removed context menus from jerky.
 */

using Server.Targeting;
using System;
using System.Collections;

namespace Server.Items
{

    [FlipableAttribute(0x1B09, 0x1B10)]
    public class BonePile : Item, IScissorable
    {
        [Constructable]
        public BonePile()
            : base(0x1B09 + Utility.Random(8))
        {
            Stackable = false;
            Weight = 10.0;
        }

        public BonePile(Serial serial)
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

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
                return false;

            base.ScissorHelper(scissors, from, new Bone(), Utility.RandomMinMax(10, 15));

            return true;
        }
    }

    [FlipableAttribute(0x1B17, 0x1B18)]
    public class RibCage : Item, IScissorable
    {
        [Constructable]
        public RibCage()
            : base(0x1B17 + Utility.Random(2))
        {
            Stackable = false;
            Weight = 5.0;
        }

        public RibCage(Serial serial)
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

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
                return false;

            base.ScissorHelper(scissors, from, new Bone(), Utility.RandomMinMax(3, 5));

            return true;
        }
    }

    public class Brain : Item
    {
        public override TimeSpan DecayTime
        {
            get
            {
                return TimeSpan.FromMinutes(15.0);
            }
        }

        [Constructable]
        public Brain()
            : base(0x1CF0)
        {
            Name = "a brain";
        }

        public Brain(string name)
            : this()
        {
            if (name != null && name.Length > 0)
            {
                this.Name = "the brain of " + name;
            }
        }

        public Brain(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class Skull : Item
    {
        public override TimeSpan DecayTime
        {
            get
            {
                return TimeSpan.FromMinutes(15.0);
            }
        }

        [Constructable]
        public Skull()
            : base(0x1AE0)
        {
            Name = "a skull";

            int rNum = Utility.Random(0, 4);
            switch (rNum)
            {
                case 0:
                    ItemID = 0x1AE0;
                    break;
                case 1:
                    ItemID = 0x1AE1;
                    break;
                case 2:
                    ItemID = 0x1AE2;
                    break;
                case 3:
                    ItemID = 0x1AE3;
                    break;
                case 4:
                    ItemID = 0x1AE4;
                    break;
            }
        }

        public Skull(string name)
            : this()
        {
            if (name != null && name.Length > 0)
            {
                this.Name = "the skull of " + name;
            }
        }

        public Skull(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class Jerky : Food
    {
        public override TimeSpan DecayTime
        {
            get
            {
                return TimeSpan.FromMinutes(15.0);
            }
        }

        [Constructable]
        public Jerky()
            : base(0x976)
        {
            Name = "human meat";
        }

        public Jerky(IOBAlignment kin)
            : this()
        {
            if (kin == IOBAlignment.Orcish)
            {
                Name = "orc meat";
            }
        }

        public Jerky(Serial serial)
            : base(serial)
        {
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
        }

        // wea: ensure human meat is duped properly
        public override Item Dupe(int amount)
        {
            return base.Dupe(new Jerky(), amount);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (this.ItemID == 0x978) //in jerky form, eat it
            {
                if (!Movable)
                    return;

                if (from.InRange(this.GetWorldLocation(), 1))
                {
                    int fillfactor = Utility.RandomMinMax(-5, 10);

                    // Fill the Mobile with FillFactor
                    if (Food.FillHunger(from, fillfactor))
                    {
                        if (fillfactor < 0)
                        {
                            from.SendMessage("That jerky didn't taste too good!");
                            from.Stam -= Utility.Random(15, 10);
                        }

                        // Play a random "eat" sound
                        from.PlaySound(Utility.Random(0x3A, 3));

                        if (from.Body.IsHuman && !from.Mounted)
                            from.Animate(34, 5, 1, true, false, 0);

                        if (this.Poison != null)
                            from.ApplyPoison(this.Poisoner, this.Poison);

                        Consume();
                    }
                }
            }
            else
            {
                if (Movable) //if it's not movable, then we're already cooking it
                {
                    from.SendMessage("Target the fire to make jerky.");
                    from.Target = new CookTarget(this);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class BodyPart : Item
    {
        public override TimeSpan DecayTime
        {
            get
            {
                return TimeSpan.FromMinutes(15.0);
            }
        }

        public enum Part
        {
            HEART, LIVER, ENTRAILS
        }

        public BodyPart()
            : base(0x1CED)
        {
        }

        public BodyPart(Part part)
            : this()
        {
            SetBodyPart(part);
        }

        public BodyPart(Serial serial)
            : base(serial)
        {
        }

        private void SetBodyPart(Part part)
        {
            switch (part)
            {
                case Part.HEART:
                    ItemID = 0x1CED;
                    Name = "a heart";
                    break;
                case Part.LIVER:
                    ItemID = 0x1CEE;
                    Name = "a liver";
                    break;
                case Part.ENTRAILS:
                    ItemID = 0x1CEF;
                    Name = "entrails";
                    break;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }


    #region CookTarget
    public class CookTarget : Target
    {
        private Item m_CookItem = null;
        private double m_Difficulty = 0.0;
        private Campfire m_Fire = null;
        private Mobile m_From = null;

        public CookTarget(Item toCook)
            : base(2, false, TargetFlags.None)
        {
            m_CookItem = toCook;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (from == null || targeted == null) return;

            m_From = from;

            if (targeted is Campfire)
            {
                m_Fire = targeted as Campfire;

                double cookskill = from.Skills[SkillName.Cooking].Value;

                m_Difficulty = cookskill;

                if (m_CookItem != null)
                {
                    if (m_CookItem is Jerky)
                    {
                        m_Difficulty -= 31.4;
                    }

                    if (m_Difficulty < 0)
                    {
                        from.SendMessage("You throw it in the fire having no idea how to cook it.");
                        m_CookItem.Delete();
                    }
                    else
                    {
                        m_CookItem.Movable = false;
                        from.SendMessage("You begin cooking.");
                        Timer.DelayCall(TimeSpan.FromSeconds((double)Utility.RandomMinMax(12, 34)), new TimerCallback(CookEnd));
                    }
                }
            }
        }

        private void CookEnd()
        {
            if (m_CookItem == null || m_CookItem.Deleted) return;
            if (m_Fire == null) return;
            if (m_From == null || m_From.Deleted) return;

            if (m_From.GetDistanceToSqrt(m_Fire) > 3)
            {
                m_From.SendMessage("You leave your fire unattended and burn what you're cooking to a crisp.");
                m_CookItem.Delete();
            }
            else
            {
                if (Utility.RandomDouble() < m_Difficulty)
                {
                    m_CookItem.Movable = true;
                    m_From.SendMessage("You cook it nicely.");
                    if (m_CookItem is Jerky)
                    {
                        m_CookItem.ItemID = 0x978;
                        if (m_CookItem.Name.Contains("orc"))
                        {
                            m_CookItem.Name = "orc jerky";
                        }
                        else
                        {
                            m_CookItem.Name = "human jerky";
                        }
                    }
                }
                else
                {
                    m_From.SendMessage("You burn it to a crisp.");
                    m_CookItem.Delete();
                }
            }
        }
    }
    #endregion
}