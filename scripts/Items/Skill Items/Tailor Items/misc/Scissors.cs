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

/* Scripts\Items\Skill Items\Tailor Items\misc\Scissors.cs
 * ChangeLog
 *	6/13/2023 Adam
 *	    Add following check: "Items you wish to cut must be in your backpack"
 */

using Server.Targeting;

namespace Server.Items
{
    public interface IScissorable
    {
        bool Scissor(Mobile from, Scissors scissors);
    }

    [FlipableAttribute(0xf9f, 0xf9e)]
    public class Scissors : Item, IHasUsesRemaining
    {
        [Constructable]
        public Scissors()
            : base(0xF9F)
        {
            Weight = 1.0;
            m_usesRemaining = 200;
        }
        #region UsesRemaining
        public bool WearsOut { get { return Core.RuleSets.SiegeStyleRules(); } }
        public int ToolBrokeMessage => 1044038; // You have worn out your tool!
        int m_usesRemaining;
        // staff don't need to see this
        [CommandProperty(AccessLevel.Owner)]
        public int UsesRemaining { get { return m_usesRemaining; } set { m_usesRemaining = value; } }
        public override void OnActionComplete(Mobile from, Item tool)
        {
            if (this == tool && Utility.Inventory(from).Contains(this))
                // scissors only wear out on Siege
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
                from.SendMessage("You broke your scissors.");
            }
        }
        #endregion UsesRemaining
        public Scissors(Serial serial)
            : base(serial)
        {
        }

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
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendLocalizedMessage(502434); // What should I use these scissors on?

            from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private Scissors m_Item;

            public InternalTarget(Scissors item)
                : base(1, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted) return;

                /*if ( targeted is Item && !((Item)targeted).IsStandardLoot() )
				{
					from.SendLocalizedMessage( 502440 ); // Scissors can not be used on that to produce anything.
				}
				else */
                if (targeted is Item item && !item.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(502437); // Items you wish to cut must be in your backpack
                }
                else if (targeted is IScissorable)
                {
                    IScissorable obj = (IScissorable)targeted;

                    if (obj.Scissor(from, m_Item))
                        from.PlaySound(0x248);
                }
                else
                {
                    from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
                }
            }
        }
    }
}