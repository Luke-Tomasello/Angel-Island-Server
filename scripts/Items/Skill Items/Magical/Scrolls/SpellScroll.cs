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

/* Items/SkillItems/Magical/Scrolls/SpellScroll.cs
 * CHANGELOG:
 *	1/6/23, Yoar
 *	    SpellScroll now implements IFactionItem
 *	    Faction imbued spell scrolls can now be used to "charge" gnarled staffs (OSI accurate)
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Factions;
using Server.Spells;
using Server.Targeting;
using System.Collections;

namespace Server.Items
{
    public class SpellScroll : Item, IFactionItem
    {
        #region Factions
        private FactionItem m_FactionState;

        [CommandProperty(AccessLevel.GameMaster)]
        public FactionItem FactionItemState
        {
            get { return m_FactionState; }
            set
            {
                m_FactionState = value;

                LootType = (m_FactionState == null ? LootType.Regular : LootType.Blessed);
            }
        }
        #endregion

        private int m_SpellID;

        public int SpellID
        {
            get
            {
                return m_SpellID;
            }
        }

        public SpellScroll(Serial serial)
            : base(serial)
        {
        }

        [Constructable]
        public SpellScroll(int spellID, int itemID)
            : this(spellID, itemID, 1)
        {
        }

        [Constructable]
        public SpellScroll(int spellID, int itemID, int amount)
            : base(itemID)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;

            m_SpellID = spellID;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_SpellID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_SpellID = reader.ReadInt();

                        break;
                    }
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive && this.Movable)
                list.Add(new ContextMenus.AddToSpellbookEntry());
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new SpellScroll(m_SpellID, ItemID, amount), amount);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!Multis.DesignContext.Check(from))
                return; // They are customizing

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            #region Factions
            if (m_FactionState != null)
            {
                PlayerState player = PlayerState.Find(from);

                if (player == null)
                {
                    from.SendLocalizedMessage(1010376); // You may not use this unless you are a faction member!
                }
                else if (player.Faction != m_FactionState.Faction)
                {
                    from.SendLocalizedMessage(1010377); // You may not use a scroll crafted by the other factions!
                }
                else
                {
                    from.SendLocalizedMessage(1010378); // Select a gnarled faction staff to charge
                    from.Target = new InternalTarget(this);
                }

                return;
            }
            #endregion

            Spell spell = SpellRegistry.NewSpell(m_SpellID, from, this);

            if (spell != null)
                spell.Cast();
            else
                from.SendLocalizedMessage(502345); // This spell has been temporarily disabled.
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            #region Factions
            if (m_FactionState != null)
                list.Add(1041350); // faction item
            #endregion
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            #region Factions
            if (m_FactionState != null)
                LabelTo(from, 1041350); // faction item
            #endregion
        }

        public override bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            #region Factions
            if (m_FactionState != null || (dropped is SpellScroll && ((SpellScroll)dropped).m_FactionState != null))
                return false;
            #endregion

            return base.StackWith(from, dropped, playSound);
        }

        private class InternalTarget : Target
        {
            private SpellScroll m_Scroll;

            public InternalTarget(SpellScroll scroll)
                : base(2, false, TargetFlags.None)
            {
                m_Scroll = scroll;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Scroll.Deleted || m_Scroll.FactionItemState == null || !m_Scroll.IsChildOf(from.Backpack))
                    return;

                int spellID = m_Scroll.SpellID;

                if (spellID < 0 || spellID >= 64)
                    return;

                MagicItemEffect effect = (MagicItemEffect)m_Scroll.SpellID;

                PlayerState player = PlayerState.Find(from);
                GnarledStaff staff = targeted as GnarledStaff;

                if (staff == null)
                {
                    from.SendLocalizedMessage(1010387); // You cant use a faction scroll on that!
                }
                else if (staff.FactionItemState == null)
                {
                    from.SendLocalizedMessage(1010386); // This staff is not faction made and thus may not be charged
                }
                else if (player.Faction != staff.FactionItemState.Faction)
                {
                    from.SendLocalizedMessage(1010385); // You may not charge enemy faction staves!
                }
                else if (staff.MagicEffect != MagicItemEffect.None)
                {
                    from.SendLocalizedMessage(1010379); // This staff has already been charged - you may not recharge it!
                }
                else if (from.Skills[SkillName.Inscribe].Base < 90.0)
                {
                    // just a guess...
                    from.PlaySound(0x5C); // fizzle
                }
                else
                {
                    int circle = (m_Scroll.SpellID / 8) + 1;

                    from.SendLocalizedMessage(1010380); // The staff is now charged

                    staff.MagicEffect = effect;
                    staff.MagicCharges = 40 / circle;

                    m_Scroll.Consume();
                }
            }
        }
    }
}