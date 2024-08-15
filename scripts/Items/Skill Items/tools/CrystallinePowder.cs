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

/* Scripts/Items/Skill Items/Tools/CrystallinePowder.cs
 * CHANGELOG:
 *  12/10/21, Yoar
 *      Initial version.
 */

using System;

namespace Server.Items
{
    public class CrystallineDullCopper : CrystallinePowder
    {
        public const int BaseCost = 6;
        public int Cost { get { return (int)((BaseCost + BaseCost * .2) * base.UsesRemaining); } }

        [Constructable]
        public CrystallineDullCopper()
             : base(CraftResource.DullCopper, 100)
        {
        }

        public CrystallineDullCopper(Serial serial)
        : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
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
    public class CrystallineShadowIron : CrystallinePowder
    {
        public const int BaseCost = 7;
        public int Cost { get { return (int)((BaseCost + BaseCost * .2) * base.UsesRemaining); } }
        [Constructable]
        public CrystallineShadowIron()
            : base(CraftResource.ShadowIron, 100)
        {
        }

        public CrystallineShadowIron(Serial serial)
        : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
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
    public class CrystallineCopper : CrystallinePowder
    {
        public const int BaseCost = 8;
        public int Cost { get { return (int)((BaseCost + BaseCost * .2) * base.UsesRemaining); } }
        [Constructable]
        public CrystallineCopper()
            : base(CraftResource.Copper, 100)
        {
        }

        public CrystallineCopper(Serial serial)
        : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
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
    public class CrystallineBronze : CrystallinePowder
    {
        public const int BaseCost = 9;
        public int Cost { get { return (int)((BaseCost + BaseCost * .2) * base.UsesRemaining); } }
        [Constructable]
        public CrystallineBronze()
            : base(CraftResource.Bronze, 100)
        {
        }

        public CrystallineBronze(Serial serial)
        : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
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
    public class CrystallineGold : CrystallinePowder
    {
        public const int BaseCost = 10;
        public int Cost { get { return (int)((BaseCost + BaseCost * .2) * base.UsesRemaining); } }
        [Constructable]
        public CrystallineGold()
            : base(CraftResource.Gold, 100)
        {
        }

        public CrystallineGold(Serial serial)
        : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
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
    public class CrystallineAgapite : CrystallinePowder
    {
        public const int BaseCost = 11;
        public int Cost { get { return (int)((BaseCost + BaseCost * .2) * base.UsesRemaining); } }
        [Constructable]
        public CrystallineAgapite()
            : base(CraftResource.Agapite, 100)
        {
        }

        public CrystallineAgapite(Serial serial)
        : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
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
    public class CrystallineVerite : CrystallinePowder
    {
        public const int BaseCost = 13;
        public int Cost { get { return (int)((BaseCost + BaseCost * .2) * base.UsesRemaining); } }
        [Constructable]
        public CrystallineVerite()
            : base(CraftResource.Verite, 100)
        {
        }

        public CrystallineVerite(Serial serial)
        : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
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
    public class CrystallineValorite : CrystallinePowder
    {
        public const int BaseCost = 15;
        public int Cost { get { return (int)((BaseCost + BaseCost * .2) * base.UsesRemaining); } }
        [Constructable]
        public CrystallineValorite()
            : base(CraftResource.Valorite, 100)
        {
        }

        public CrystallineValorite(Serial serial)
        : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
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

    public class CrystallinePowder : Item, IUsesRemaining
    {
        private int m_MaxUses;

        private int m_UsesRemaining;
        private CraftResource m_Resource;

        public int MaxUses { get { return m_MaxUses; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; UpdateGraphic(); UpdateWeight(); InvalidateProperties(); }
        }

        public bool ShowUsesRemaining
        {
            get { return true; }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set { m_Resource = value; UpdateName(); UpdateHue(); }
        }

        [Constructable]
        public CrystallinePowder()
            : this((CraftResource)Utility.RandomMinMax((int)CraftResource.DullCopper, (int)CraftResource.Valorite), 10)
        {
        }

        [Constructable]
        public CrystallinePowder(CraftResource res)
            : this(res, 10)
        {
        }

        [Constructable]
        public CrystallinePowder(CraftResource res, int uses)
            : base(0x1005)
        {
            m_Resource = res;
            m_UsesRemaining = uses;
            m_MaxUses = uses;
            UpdateGraphic();
            UpdateName();
            UpdateHue();
            UpdateWeight();
        }

        private void UpdateWeight()
        {
            if (m_UsesRemaining <= 0)
                this.Weight = 3; // empty
            else if (m_UsesRemaining > m_MaxUses / 2)
                this.Weight = 7; // full
            else
                this.Weight = 5; // half empty
        }
        private void UpdateGraphic()
        {
            if (this.ItemID >= 0x1005 && this.ItemID <= 0x1007)
            {
                if (m_UsesRemaining <= 0)
                    this.ItemID = 0x1005; // empty
                else if (m_UsesRemaining > m_MaxUses / 2)
                    this.ItemID = 0x1006; // full
                else
                    this.ItemID = 0x1007; // half empty
            }
        }

        private void UpdateName()
        {
            this.Name = string.Format("crystalline {0}", CraftResources.GetName(m_Resource).ToLower());
        }

        private void UpdateHue()
        {
            this.Hue = CraftResources.GetHue(m_Resource);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_UsesRemaining == 0)
                list.Add("The jar is empty.");
            else if (m_UsesRemaining == m_MaxUses)
                list.Add("The jar is completely full.");
            else if (m_UsesRemaining >= m_MaxUses / 2)
                list.Add("The jar is nearly full.");
            else
                list.Add("The jar is nearly empty.");
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_UsesRemaining == 0)
                this.LabelTo(from, "The jar is empty.");
            else if (m_UsesRemaining == m_MaxUses)
                this.LabelTo(from, "The jar is completely full.");
            else if (m_UsesRemaining >= m_MaxUses / 2)
                this.LabelTo(from, "The jar is nearly full.");
            else
                this.LabelTo(from, "The jar is nearly empty.");
        }

#if false
        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            else if (m_UsesRemaining <= 0)
                from.SendMessage("You have used up your crystalline powder.");
            else
                from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private CrystallinePowder m_Powder;

            public InternalTarget(CrystallinePowder powder)
                : base(2, false, TargetFlags.None)
            {
                m_Powder = powder;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!m_Powder.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                    return;
                }
                else if (m_Powder.UsesRemaining <= 0)
                {
                    from.SendMessage("You have used up your crystalline powder.");
                    return;
                }

                // TODO
            }
        }
#endif

        public CrystallinePowder(Serial serial)
        : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_MaxUses);
            writer.Write((int)m_UsesRemaining);
            writer.Write((int)m_Resource);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_MaxUses = reader.ReadInt();
            m_UsesRemaining = reader.ReadInt();
            m_Resource = (CraftResource)reader.ReadInt();
        }
    }
}