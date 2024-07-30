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

/* Scripts\Engines\Catacomb\Items\OrbOfTransference.cs
 * ChangeLog
 *  3/3/2024, Adam
 *      Doll-up the Transfer Effect
 */
using Server.Items;
using Server.Network;
using Server.Targeting;
using System;

namespace Server.Engines.Catacomb
{
    public class OrbOfTransference : Item
    {
        public override string DefaultName { get { return "orb of charge transfer"; } }

        private int m_Charges;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get { return m_Charges; }
            set { m_Charges = value; }
        }

        [Constructable]
        public OrbOfTransference()
            : this(50)
        {
        }

        [Constructable]
        public OrbOfTransference(int charges)
            : base(0xE2E)
        {
            Weight = 1.0;
            Light = LightType.Circle150;

            m_Charges = charges;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, 1060741, m_Charges.ToString()); // charges: ~1_val~
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }
            else if (m_Charges <= 0)
            {
                from.SendMessage("The orb is out of charges.");
                return;
            }

            from.SendMessage("Target the wand you wish to discharge.");
            from.Target = new Discharge(this);
        }

        private class Discharge : Target
        {
            private OrbOfTransference m_Orb;

            public Discharge(OrbOfTransference orb)
                : base(2, false, TargetFlags.None)
            {
                m_Orb = orb;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.Alive)
                    return;

                if (m_Orb.Deleted || !m_Orb.IsAccessibleTo(from) || !from.InRange(m_Orb.GetWorldLocation(), 2) || !from.InLOS(m_Orb) || m_Orb.Charges <= 0)
                    return;

                BaseWand wand = targeted as BaseWand;

                if (wand == null)
                {
                    from.SendMessage("That is not a wand.");
                    return;
                }
                else if (wand.MagicEffect == MagicItemEffect.None)
                {
                    from.SendMessage("That wand is not magically imbued.");
                    return;
                }
                else if (wand.MagicCharges <= 0)
                {
                    from.SendMessage("That wand is out of charges.");
                    return;
                }

                from.SendMessage("Target the wand you wish to recharge.");
                from.Target = new RechargeTarget(m_Orb, wand);
            }
        }

        private class RechargeTarget : Target
        {
            private OrbOfTransference m_Orb;
            private BaseWand m_First;

            public RechargeTarget(OrbOfTransference orb, BaseWand first)
                : base(2, false, TargetFlags.None)
            {
                m_Orb = orb;
                m_First = first;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.Alive)
                    return;

                if (m_Orb.Deleted || !m_Orb.IsAccessibleTo(from) || !from.InRange(m_Orb.GetWorldLocation(), 2) || !from.InLOS(m_Orb) || m_Orb.Charges <= 0)
                    return;

                if (m_First.Deleted || !m_First.IsAccessibleTo(from) || !from.InRange(m_First.GetWorldLocation(), 2) || !from.InLOS(m_First) || m_First.MagicCharges <= 0)
                    return;

                BaseWand second = targeted as BaseWand;

                if (second == null)
                {
                    from.SendMessage("That is not a wand.");
                    return;
                }
                else if (second.MagicEffect == MagicItemEffect.None)
                {
                    from.SendMessage("That wand is not magically charged.");
                    return;
                }
                else if (second.MagicCharges <= 0)
                {
                    from.SendMessage("That wand is out of charges.");
                    return;
                }
                else if (m_First.MagicEffect != second.MagicEffect)
                {
                    from.SendMessage("The wands are not imbued with the same effect.");
                    return;
                }

                int transferable = GetRechargeLimit(second.MagicEffect) - second.MagicCharges;

                if (transferable <= 0)
                {
                    from.SendMessage("The wand you wish to recharge is already at full charges.");
                    return;
                }

                int transfer = m_First.MagicCharges;

                if (transfer > transferable)
                    transfer = transferable;

                m_Orb.Charges--;

                m_First.MagicCharges -= transfer;
                second.MagicCharges += transfer;

                DoTransferEffect(from, m_First, second);
            }

            private void DoTransferEffect(Mobile from, Item src_item, Item dest_item)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(TransferEffect_Stage1), new object[] { from, src_item, dest_item });
            }

            private void TransferEffect_Stage1(object state)
            {
                object[] aState = (object[])state;
                Mobile from = aState[0] as Mobile;
                Item src_item = aState[1] as Item;
                Item dest_item = aState[2] as Item;

                if (src_item.RootParent == null && dest_item.RootParent == null)
                {
                    Effects.SendMovingParticles(from: src_item, to: dest_item, itemID: 0x36FA, speed: 1, duration: 0, fixedDirection: false,
                        explodes: false, hue: 1108, renderMode: 0, effect: 9533, explodeEffect: 1, explodeSound: 0, layer: (EffectLayer)255, unknown: 0x100);
                    Effects.SendMovingParticles(from: src_item, to: dest_item, itemID: 0x0001, speed: 1, duration: 0, fixedDirection: false,
                        explodes: true, hue: 1108, renderMode: 0, effect: 9533, explodeEffect: 9534, explodeSound: 0, layer: (EffectLayer)255, unknown: 0);
                }
                else
                {
                    // effect handled in TransferEffect_Stage2()
                }
                from.PlaySound(0x1FB);

                Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(TransferEffect_Stage2), state);
            }

            private void TransferEffect_Stage2(object state)
            {
                object[] aState = (object[])state;
                Mobile from = aState[0] as Mobile;
                Item src_item = aState[1] as Item;
                Item dest_item = aState[2] as Item;

                if (src_item.RootParent == null && dest_item.RootParent == null)
                {
                    Effects.SendMovingParticles(dest_item, src_item, 0x36F4, 1, 0, false, false, 32, 0, 9535, 1, 0, (EffectLayer)255, 0x100);
                    Effects.SendMovingParticles(dest_item, src_item, 0x0001, 1, 0, false, true, 32, 0, 9535, 9536, 0, (EffectLayer)255, 0);
                }
                else
                {   // ripped from ConsecrateWeaponSpell
                    //  no longer works, not even RunUO shows the overhead animation. I suspect newer clients don't support it? 
                    //  It used to work on Angel Island for charging a slayer weapon (Slayer Strike)
                    from.FixedParticles(0x3779, 1, 30, 9964, 3, 3, EffectLayer.Waist);
                    IEntity from_object = new Entity(Serial.Zero, new Point3D(from.X, from.Y, from.Z), from.Map);
                    IEntity to_object = new Entity(Serial.Zero, new Point3D(from.X, from.Y, from.Z + 50), from.Map);
                    Effects.SendMovingParticles(from_object, to_object, 0xFB4/*dest_item.ItemID*/, 1, 0, false, false, 33, 3, 9501, 1, 0, EffectLayer.Head, 0x100);
                }
                from.PlaySound(0x209);

                if ((src_item as BaseWand).MagicCharges <= 0)
                    from.SendMessage("You transfer all charges from the first wand to the second.");
                else
                    from.SendMessage("You transfer some charges from the first wand to the second.");
            }
        }

        private static int GetRechargeLimit(MagicItemEffect effect)
        {
            if (effect == MagicItemEffect.Identification)
                return 50;
            else
                return 20;
        }

        public OrbOfTransference(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_Charges);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Charges = reader.ReadInt();

            if (version < 1)
                Light = LightType.Circle150;
        }
    }
}