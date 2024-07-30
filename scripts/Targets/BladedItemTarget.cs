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

/* ChangeLog:
 * 11/14/22, Adam (OnActionComplete)
 *  Add call to OnActionComplete in support of IHasUsesRemaining
 *      IHasUsesRemaining consumes uses on Siege, but not on other shards.
 *      Like IUsesRemaining, IHasUsesRemaining consumes uses, but is dynamic where 
 *      IUsesRemaining is not.
 *  6/16/04, Old Salty
 *  	Fixed a RunUO bug so kindling can once again be taken from trees
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
*/

using Server.Engines.Harvest;
using Server.Engines.Quests;
using Server.Engines.Quests.Hag;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Targets
{
    public class BladedItemTarget : Target
    {
        private Item m_Item;

        public BladedItemTarget(Item item)
            : base(2, false, TargetFlags.None)
        {
            m_Item = item;
        }

        protected override void OnTargetOutOfRange(Mobile from, object targeted)
        {
            if (targeted is UnholyBone && from.InRange(((UnholyBone)targeted), 12))
                ((UnholyBone)targeted).Carve(from, m_Item);
            else
                base.OnTargetOutOfRange(from, targeted);
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Item.Deleted)
                return;
            bool usedBladedWeapon = true;
            if (targeted is ICarvable)
            {
                ((ICarvable)targeted).Carve(from, m_Item);
            }
            else if (targeted is SwampDragon && ((SwampDragon)targeted).HasBarding)
            {
                SwampDragon pet = (SwampDragon)targeted;

                if (!pet.Controlled || pet.ControlMaster != from)
                    from.SendLocalizedMessage(1053022); // You cannot remove barding from a swamp dragon you do not own.
                else
                    pet.HasBarding = false;
            }
            else if (targeted is StaticTarget || (targeted is Static && !((Static)targeted).Movable))
            {
                int itemID = 0;

                if (targeted is StaticTarget)
                    itemID = ((StaticTarget)targeted).ItemID;
                else if (targeted is Item)
                    itemID = ((Item)targeted).ItemID;

                if (itemID == 0xD15 || itemID == 0xD16) // red mushroom
                {
                    PlayerMobile player = from as PlayerMobile;

                    if (player != null)
                    {
                        QuestSystem qs = player.Quest;

                        if (qs is WitchApprenticeQuest)
                        {
                            FindIngredientObjective obj = qs.FindObjective(typeof(FindIngredientObjective)) as FindIngredientObjective;

                            if (obj != null && !obj.Completed && obj.Ingredient == Ingredient.RedMushrooms)
                            {
                                player.SendLocalizedMessage(1055036); // You slice a red cap mushroom from its stem.
                                obj.Complete();
                            }
                        }
                    }
                }

                else
                {
                    HarvestSystem system = Lumberjacking.System;
                    HarvestDefinition def = Lumberjacking.System.Definition;

                    int tileID;
                    Map map;
                    Point3D loc;

                    if (!system.GetHarvestDetails(from, m_Item, targeted, out tileID, out map, out loc))
                    {
                        from.SendLocalizedMessage(500494); // You can't use a bladed item on that!
                    }
                    else if (!def.Validate(tileID))
                    {
                        from.SendLocalizedMessage(500494); // You can't use a bladed item on that!
                    }
                    else
                    {
                        HarvestBank bank = def.GetBank(map, loc.X, loc.Y);

                        if (bank == null)
                            return;

                        if (bank.Current < 5)
                        {
                            from.SendLocalizedMessage(500493); // There's not enough wood here to harvest.
                        }
                        else
                        {
                            bank.Consume(5);

                            Item item = new Kindling();

                            if (from.PlaceInBackpack(item))
                            {
                                from.SendLocalizedMessage(500491); // You put some kindling into your backpack.
                                from.SendLocalizedMessage(500492); // An axe would probably get you more wood.
                            }
                            else
                            {
                                from.SendLocalizedMessage(500490); // You can't place any kindling into your backpack!

                                item.Delete();
                            }
                        }
                    }
                }
            }
            else
                usedBladedWeapon = false;

            // consume uses
            if (usedBladedWeapon == true)
                m_Item.OnActionComplete(from, m_Item);
        }
    }
}