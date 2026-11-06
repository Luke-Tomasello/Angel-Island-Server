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

/* Items/SkillItems/Magical/Misc/PotionKeg.cs
 * CHANGELOG:
 *	5/29/10, adam
 *		Place filled bottles back into the same pack the empty came from
 *		Use  the version of ConsumeTotal() that takes an on OnItemConsumed delegate so that we can record the bottles parent container
 *	9/4/04, mith
 *		OnDragDrop(): Copied Else block from Spellbook, to prevent people dropping things on book to have it bounce back to original location.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using System;

namespace Server.Items
{
    public class PotionKeg : Item
    {
        private PotionEffect m_Type;
        private int m_Held;
        private Container? m_returnPack = null;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Held
        {
            get
            {
                return m_Held;
            }
            set
            {
                if (m_Held != value)
                {
                    this.Weight += (value - m_Held) * 0.8;

                    m_Held = value;
                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PotionEffect Type
        {
            get
            {
                return m_Type;
            }
            set
            {
                m_Type = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public PotionKeg()
            : base(0x1940)
        {
            this.Weight = 1.0;
        }

        public PotionKeg(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Type);
            writer.Write((int)m_Held);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Type = (PotionEffect)reader.ReadInt();
                        m_Held = reader.ReadInt();

                        break;
                    }
            }
        }

        public override int LabelNumber
        {

            get
            {
                if (m_Held <= 0)
                    return 1041641;

                // Publish 15
                // Potion kegs will now display the correct contents when single-clicked (ex. the keg will say �a keg of greater poison� versus �a keg of green potions�).
                // more info on id'ing http://forums.uosecondage.com/viewtopic.php?f=4&t=1440
                if (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisStyleRules() && PublishInfo.Publish < 15)
                    switch (m_Type)
                    {
                        case PotionEffect.Nightsight: return 1041611;       // "A keg of black liquid.";
                        case PotionEffect.CureLesser: return 1041612;       // "A keg of orange liquid.";
                        case PotionEffect.Cure: return 1041612;             // "A keg of orange liquid.";
                        case PotionEffect.CureGreater: return 1041612;      // "A keg of orange liquid.";
                        case PotionEffect.Agility: return 1041613;          // "A keg of blue liquid."
                        case PotionEffect.AgilityGreater: return 1041613;   // "A keg of blue liquid."
                        case PotionEffect.Strength: return 1041614;         // "A keg of white liquid."
                        case PotionEffect.StrengthGreater: return 1041614;  // "A keg of white liquid."
                        case PotionEffect.PoisonLesser: return 1041615;     // "A keg of green liquid."
                        case PotionEffect.Poison: return 1041615;           // "A keg of green liquid."
                        case PotionEffect.PoisonGreater: return 1041615;    // "A keg of green liquid."
                        case PotionEffect.PoisonDeadly: return 1041615;     // "A keg of green liquid."
                        case PotionEffect.Refresh: return 1041616;          // "A keg of red liquid."
                        case PotionEffect.RefreshTotal: return 1041616;     // "A keg of red liquid."
                        case PotionEffect.HealLesser: return 1041617;       // "A keg of yellow liquid."
                        case PotionEffect.Heal: return 1041617;             // "A keg of yellow liquid."
                        case PotionEffect.HealGreater: return 1041617;      // "A keg of yellow liquid."
                        case PotionEffect.ExplosionLesser: return 1041618;  // "A keg of purple liquid."
                        case PotionEffect.Explosion: return 1041618;        // "A keg of purple liquid."
                        case PotionEffect.ExplosionGreater: return 1041618; // "A keg of purple liquid."
                    }

                return (1041620 + (int)m_Type);
            }

        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            int number;

            if (m_Held <= 0)
                number = 502246; // The keg is empty.
            else if (m_Held < 5)
                number = 502248; // The keg is nearly empty.
            else if (m_Held < 20)
                number = 502249; // The keg is not very full.
            else if (m_Held < 30)
                number = 502250; // The keg is about one quarter full.
            else if (m_Held < 40)
                number = 502251; // The keg is about one third full.
            else if (m_Held < 47)
                number = 502252; // The keg is almost half full.
            else if (m_Held < 54)
                number = 502254; // The keg is approximately half full.
            else if (m_Held < 70)
                number = 502253; // The keg is more than half full.
            else if (m_Held < 80)
                number = 502255; // The keg is about three quarters full.
            else if (m_Held < 96)
                number = 502256; // The keg is very full.
            else if (m_Held < 100)
                number = 502257; // The liquid is almost to the top of the keg.
            else
                number = 502258; // The keg is completely full.

            list.Add(number);
        }

        public override void OnSingleClick(Mobile from)
        {
            int number;

            if (m_Held <= 0)
                number = 502246; // The keg is empty.
            else if (m_Held < 5)
                number = 502248; // The keg is nearly empty.
            else if (m_Held < 20)
                number = 502249; // The keg is not very full.
            else if (m_Held < 30)
                number = 502250; // The keg is about one quarter full.
            else if (m_Held < 40)
                number = 502251; // The keg is about one third full.
            else if (m_Held < 47)
                number = 502252; // The keg is almost half full.
            else if (m_Held < 54)
                number = 502254; // The keg is approximately half full.
            else if (m_Held < 70)
                number = 502253; // The keg is more than half full.
            else if (m_Held < 80)
                number = 502255; // The keg is about three quarters full.
            else if (m_Held < 96)
                number = 502256; // The keg is very full.
            else if (m_Held < 100)
                number = 502257; // The liquid is almost to the top of the keg.
            else
                number = 502258; // The keg is completely full.

            if (m_Held <= 0)
                base.OnSingleClick(from);           // "a keg"
            else
                this.LabelTo(from, LabelNumber);    // "A keg of purple liquid."

            this.LabelTo(from, number);             // "The keg is completely full."
        }

        public void OnItemConsumed(Item item, int amount)
        {
            // remember the pack from which the bottle came
            if (item != null && item.Parent != null && item.Parent is Container && m_returnPack == null)
                m_returnPack = item.Parent as Container;
        }

        public override void OnDoubleClick(Mobile from)
        {
            m_returnPack = null;

            if (from.InRange(GetWorldLocation(), 2))
            {
                if (m_Held > 0)
                {
                    Container pack = from.Backpack;

                    // Adam: be cool and remember the pack from which the bottle came, then place the filled bottle back in that pack
                    if (pack != null && pack.ConsumeTotal(typeof(Bottle), 1, true, OnItemConsumed))
                    {
                        from.SendLocalizedMessage(502242); // You pour some of the keg's contents into an empty bottle...

                        // default return location
                        if (m_returnPack == null)
                            m_returnPack = from.Backpack;

                        BasePotion pot = FillBottle();

                        if (m_returnPack != null && m_returnPack.TryDropItem(from, pot, false))
                        {
                            from.SendLocalizedMessage(502243); // ...and place it into your backpack.
                            from.PlaySound(0x240);

                            if (--Held == 0)
                                from.SendLocalizedMessage(502245); // The keg is now empty.
                        }
                        else
                        {
                            from.SendLocalizedMessage(502244); // ...but there is no room for the bottle in your backpack.
                            pot.Delete();
                        }
                    }
                    else
                    {
                        // TODO: Target a bottle
                    }
                }
                else
                {
                    from.SendLocalizedMessage(502246); // The keg is empty.
                }
            }
            else
            {
                from.LocalOverheadMessage(Network.MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }

        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            try
            {
                if (from == null || from.Deleted || item == null || item.Deleted)
                    return false;

                if (item is BasePotion pot)
                {
                    // Sanity: stacked potions must have a meaningful Amount.
                    int offer = pot.Amount;
                    if (offer <= 0)
                        return false;

                    // No mixing different potion types in the same keg.
                    if (m_Held > 0 && pot.PotionEffect != m_Type)
                    {
                        from.SendLocalizedMessage(502236); // You decide that it would be a bad idea to mix different types of potions.
                        return false;
                    }

                    // Keg capacity.
                    int space = 100 - m_Held;
                    if (space <= 0)
                    {
                        from.SendLocalizedMessage(502233); // The keg will not hold any more!
                        return false;
                    }

                    int transfer = Math.Min(offer, space);
                    if (transfer <= 0)
                    {
                        from.SendLocalizedMessage(502233); // The keg will not hold any more!
                        return false;
                    }

                    // If the keg is empty, lock in the potion type on first fill.
                    if (m_Held == 0)
                        m_Type = pot.PotionEffect;

                    int poured = 0;

                    // Loop is fine here (max 100). Keep it simple and obvious.
                    for (int i = 0; i < transfer; i++)
                    {
                        // Preserve original semantics: do not pour unless we can return an empty bottle.
                        if (!GiveBottle(from))
                        {
                            if (poured == 0)
                            {
                                from.SendLocalizedMessage(502238); // You don't have room for the empty bottle in your backpack.
                                return false;
                            }

                            // Partial success: stop pouring and report what happened.
                            from.SendLocalizedMessage(502238); // You don't have room for the empty bottle in your backpack.
                            break;
                        }

                        // Pour exactly one unit.
                        Held = m_Held + 1;

                        // Consume exactly one potion from the stack (Item.Consume(1)).
                        // This handles stack decrement + deletion at zero.
                        pot.Consume(1);

                        poured++;
                        if (pot.Deleted) // Stack fully consumed.
                            break;
                    }

                    if (poured > 0)
                    {
                        from.PlaySound(0x240);

                        // Better UX and better debugging signal than the old fixed localized text.
                        from.SendMessage("You pour {0} {1} into the keg.", poured, poured == 1 ? "potion" : "potions");

                        // If we could not pour the entire stack (e.g., keg filled), the remainder must be returned to the player.
                        // When OnDragDrop returns true, the engine may treat the dropped item as "accepted" by this keg, so
                        // any remainder must be explicitly re-homed.
                        if (pot != null && !pot.Deleted && pot.Amount > 0)
                        {
                            Container pack = from.Backpack;

                            // If the potion is not already in the player's pack, try to return it there.
                            // (If it is already in the pack, do nothing.)
                            if (pack == null)
                            {
                                // No backpack: drop remainder at the player's feet as a last resort.
                                pot.MoveToWorld(from.Location, from.Map);
                            }
                            else if (pot.Parent != pack)
                            {
                                // Best-effort: put it back in the backpack.
                                // If this fails for any reason, drop at feet rather than orphaning.
                                if (!pack.TryDropItem(from, pot, false))
                                    pot.MoveToWorld(from.Location, from.Map);
                            }
                        }

                        // Optional: if partial due to keg full (space-limited), you can tell them explicitly.
                        if (poured < offer && m_Held >= 100)
                            from.SendLocalizedMessage(502233); // The keg will not hold any more!

                        return true;
                    }

                    return false;
                }

                // Non-potions: preserve legacy behavior (bounce into backpack).
                from.SendLocalizedMessage(502232); // The keg is not designed to hold that type of object.

                from.AddToBackpack(item);
                from.PlaySound(item.GetDropSound());
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return false;
            }
        }

        public bool GiveBottle(Mobile m)
        {
            Container pack = m.Backpack;

            Bottle bottle = new Bottle();

            if (pack == null || !pack.TryDropItem(m, bottle, false))
            {
                bottle.Delete();
                return false;
            }

            return true;
        }

        public BasePotion FillBottle()
        {
            switch (m_Type)
            {
                default:
                case PotionEffect.Nightsight: return new NightSightPotion();

                case PotionEffect.CureLesser: return new LesserCurePotion();
                case PotionEffect.Cure: return new CurePotion();
                case PotionEffect.CureGreater: return new GreaterCurePotion();

                case PotionEffect.Agility: return new AgilityPotion();
                case PotionEffect.AgilityGreater: return new GreaterAgilityPotion();

                case PotionEffect.Strength: return new StrengthPotion();
                case PotionEffect.StrengthGreater: return new GreaterStrengthPotion();

                case PotionEffect.PoisonLesser: return new LesserPoisonPotion();
                case PotionEffect.Poison: return new PoisonPotion();
                case PotionEffect.PoisonGreater: return new GreaterPoisonPotion();
                case PotionEffect.PoisonDeadly: return new DeadlyPoisonPotion();

                case PotionEffect.Refresh: return new RefreshPotion();
                case PotionEffect.RefreshTotal: return new TotalRefreshPotion();

                case PotionEffect.HealLesser: return new LesserHealPotion();
                case PotionEffect.Heal: return new HealPotion();
                case PotionEffect.HealGreater: return new GreaterHealPotion();

                case PotionEffect.ExplosionLesser: return new LesserExplosionPotion();
                case PotionEffect.Explosion: return new ExplosionPotion();
                case PotionEffect.ExplosionGreater: return new GreaterExplosionPotion();
            }
        }

        public static void Initialize()
        {
            TileData.ItemTable[0x1940].Height = 4;
        }
    }
}