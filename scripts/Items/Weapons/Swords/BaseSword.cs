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

/* Scripts/Items/Weapons/Swords/BaseSword.cs
 * ChangeLog :
 * 11/14/22, Adam (IHasUsesRemaining)
 *  Now implements IHasUsesRemaining to consume uses on Siege, but not on other shards.
 *      Like IUsesRemaining, IHasUsesRemaining consumes uses, but is dynamic where 
 *      IUsesRemaining is not.
 *	10/16/05, Pix
 *		Streamlined applied poison code.
 *	09/13/05, erlein
 *		Reverted poisoning rules, applied same system as archery for determining
 *		poison level achieved.
 *	09/12/05, erlein
 *		Changed OnHit() code to utilise new poisoning rules.
 */


using Server.Targets;

namespace Server.Items
{
    public abstract class BaseSword : BaseMeleeWeapon, IHasUsesRemaining
    {
        public override SkillName DefSkill { get { return SkillName.Swords; } }
        public override WeaponType DefType { get { return WeaponType.Slashing; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Slash1H; } }

        public BaseSword(int itemID)
            : base(itemID)
        {
            m_usesRemaining = 200;
        }
        #region UsesRemaining
        public bool WearsOut { get { return Core.RuleSets.SiegeStyleRules(); } }
        public int ToolBrokeMessage => -1; // no cliloc
        int m_usesRemaining;
        // staff don't need to see this
        [CommandProperty(AccessLevel.Owner)]
        public int UsesRemaining { get { return m_usesRemaining; } set { m_usesRemaining = value; } }
        public override void OnActionComplete(Mobile from, Item tool)
        {
            if (this == tool && Utility.Inventory(from).Contains(this))
                // blades only wear out in this way on Siege
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
                from.SendMessage("You broke your blade.");
            }
        }
        #endregion UsesRemaining

        public BaseSword(Serial serial)
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
            from.SendLocalizedMessage(1010018); // What do you want to use this item on?

            from.Target = new BladedItemTarget(this);
        }

        public override void OnHit(Mobile attacker, Mobile defender)
        {
            base.OnHit(attacker, defender);

            if (!Core.RuleSets.AOSRules() && Poison != null && PoisonCharges > 0)
            {
                --PoisonCharges;

                if (CheckHitPoison(attacker))
                {
                    defender.ApplyPoison(attacker, MutatePoison(attacker, Poison));

                    OnHitPoison(attacker);
                }
            }
        }
    }
}