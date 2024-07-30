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

/* Scripts\Engines\Quests\Witch Apprentice\Mobiles\Grizelda.cs
 * ChangeLog:
 *  10/13/21, Adam
 *      Added Tenjin's Saw. Tenjin's Saw is used for magic crafting for carpenters. 
 *	5/23/21, Adam
 *		Update weapon/armor drop to use new weapon or armor modifiers
 *		Level 3 weapon with a 20% chance at an upgrade.
 *  3/18/08, Adam
 *		Remove references to virtue code
 */

using Server.Items;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Engines.Quests.Hag
{
    public class Grizelda : BaseQuester
    {
        public override bool ClickTitle { get { return true; } }

        [Constructable]
        public Grizelda()
            : base("the Hag")
        {
        }

        public Grizelda(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Hue = 0x83EA;

            Female = true;
            Body = 0x191;
            Name = "Grizelda";
        }

        public override void InitOutfit()
        {
            AddItem(new Robe(0x1));
            AddItem(new Sandals());
            AddItem(new WizardsHat(0x1));
            AddItem(new GoldBracelet());

            AddItem(new LongHair(0x0));

            Item staff = new GnarledStaff();
            staff.Movable = false;
            AddItem(staff);
        }

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
            Direction = GetDirectionTo(player);

            QuestSystem qs = player.Quest;

            if (qs is WitchApprenticeQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(FindApprenticeObjective)))
                {
                    PlaySound(0x259);
                    PlaySound(0x206);
                    qs.AddConversation(new HagDuringCorpseSearchConversation());
                }
                else
                {
                    QuestObjective obj = qs.FindObjective(typeof(FindGrizeldaAboutMurderObjective));

                    if (obj != null && !obj.Completed)
                    {
                        PlaySound(0x420);
                        PlaySound(0x20);
                        obj.Complete();
                    }
                    else if (qs.IsObjectiveInProgress(typeof(KillImpsObjective))
                        || qs.IsObjectiveInProgress(typeof(FindZeefzorpulObjective)))
                    {
                        PlaySound(0x259);
                        PlaySound(0x206);
                        qs.AddConversation(new HagDuringImpSearchConversation());
                    }
                    else
                    {
                        obj = qs.FindObjective(typeof(ReturnRecipeObjective));

                        if (obj != null && !obj.Completed)
                        {
                            PlaySound(0x258);
                            PlaySound(0x41B);
                            obj.Complete();
                        }
                        else if (qs.IsObjectiveInProgress(typeof(FindIngredientObjective)))
                        {
                            PlaySound(0x259);
                            PlaySound(0x206);
                            qs.AddConversation(new HagDuringIngredientsConversation());
                        }
                        else
                        {
                            obj = qs.FindObjective(typeof(ReturnIngredientsObjective));

                            if (obj != null && !obj.Completed)
                            {
                                Container cont = GetNewContainer();

                                cont.DropItem(new BlackPearl(30));
                                cont.DropItem(new Bloodmoss(30));
                                cont.DropItem(new Garlic(30));
                                cont.DropItem(new Ginseng(30));
                                cont.DropItem(new MandrakeRoot(30));
                                cont.DropItem(new Nightshade(30));
                                cont.DropItem(new SulfurousAsh(30));
                                cont.DropItem(new SpidersSilk(30));

                                cont.DropItem(new Cauldron());
                                cont.DropItem(new MoonfireBrew());
                                cont.DropItem(new TreasureMap(Utility.RandomMinMax(1, 4), this.Map));

                                // Skill items
                                List<Item> skillItems = new List<Item>();

                                if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.MagicCraftSystem) == true)
                                {
                                    // Tenjin's Saw is used for magic carpentry
                                    if (Utility.RandomChance(player.Skills[SkillName.Carpentry].Base))
                                        skillItems.Add(new TenjinsSaw());

                                    // Tenjin's Needle is used for magic tailoring
                                    if (Utility.RandomChance(player.Skills[SkillName.Tailoring].Base))
                                        skillItems.Add(new TenjinsNeedle());

                                    // Tenjin's Hammer is used for magic blacksmithy
                                    if (Utility.RandomChance(player.Skills[SkillName.Blacksmith].Base))
                                        skillItems.Add(new TenjinsHammer());
                                }

                                if (skillItems.Count > 0)
                                {
                                    Item toDrop = skillItems[Utility.Random(skillItems.Count)];
                                    skillItems.Remove(toDrop);
                                    cont.DropItem(toDrop);
                                    foreach (Item ix in skillItems)
                                        ix.Delete();
                                }

                                if (Utility.RandomBool())
                                {
                                    BaseWeapon weapon = Loot.RandomWeapon();

                                    if (Core.RuleSets.AOSRules())
                                    {
                                        BaseRunicTool.ApplyAttributesTo(weapon, 2, 20, 30);
                                    }
                                    else
                                    {
                                        // Adam: 5/23/21
                                        // Update weapon to use new weapon or armor modifiers
                                        // Level 3 weapon with a 20% chance at an upgrade.
                                        //weapon.DamageLevel = (WeaponDamageLevel)BaseCreature.RandomMinMaxScaled(20, 30);
                                        //weapon.AccuracyLevel = (WeaponAccuracyLevel)BaseCreature.RandomMinMaxScaled(20, 30);
                                        //weapon.DurabilityLevel = (WeaponDurabilityLevel)BaseCreature.RandomMinMaxScaled(20, 30);
                                        Loot.ImbueWeaponOrArmor(noThrottle: false, weapon, Loot.ImbueLevel.Level3 /*3*/, 0.2, false);
                                    }

                                    cont.DropItem(weapon);
                                }
                                else
                                {
                                    Item item;

                                    if (Core.RuleSets.AOSRules())
                                    {
                                        item = Loot.RandomArmorOrShieldOrJewelry();

                                        if (item is BaseArmor)
                                            BaseRunicTool.ApplyAttributesTo((BaseArmor)item, 2, 20, 30);
                                        else if (item is BaseJewel)
                                            BaseRunicTool.ApplyAttributesTo((BaseJewel)item, 2, 20, 30);
                                    }
                                    else
                                    {
                                        BaseArmor armor = Loot.RandomArmorOrShield();
                                        item = armor;
                                        // Adam: 5/23/21
                                        // Update armor to use new weapon or armor modifiers
                                        // Level 3 armor with a 20% chance at an upgrade.
                                        //armor.ProtectionLevel = (ArmorProtectionLevel)BaseCreature.RandomMinMaxScaled(20, 30);
                                        //armor.Durability = (ArmorDurabilityLevel)BaseCreature.RandomMinMaxScaled(20, 30);
                                        Loot.ImbueWeaponOrArmor(noThrottle: false, armor, Loot.ImbueLevel.Level3 /*3*/, 0.2, false);
                                    }

                                    cont.DropItem(item);
                                }

                                if (player.BAC > 0)
                                    cont.DropItem(new HangoverCure());

                                if (player.PlaceInBackpack(cont))
                                {
                                    // adam: 3/18/08 - virtues are obsolete
                                    //bool gainedPath = false;
                                    //if ( VirtueHelper.Award( player, VirtueName.Sacrifice, 1, ref gainedPath ) )
                                    //player.SendLocalizedMessage( 1054160 ); // You have gained in sacrifice.

                                    PlaySound(0x253);
                                    PlaySound(0x20);
                                    obj.Complete();
                                }
                                else
                                {
                                    cont.Delete();
                                    player.SendLocalizedMessage(1046260); // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                QuestSystem newQuest = new WitchApprenticeQuest(player);
                bool inRestartPeriod = false;

                if (qs != null)
                {
                    newQuest.AddConversation(new DontOfferConversation());
                }
                else if (QuestSystem.CanOfferQuest(player, typeof(WitchApprenticeQuest), out inRestartPeriod))
                {
                    PlaySound(0x20);
                    PlaySound(0x206);
                    newQuest.SendOffer();
                }
                else if (inRestartPeriod)
                {
                    PlaySound(0x259);
                    PlaySound(0x206);
                    newQuest.AddConversation(new RecentlyFinishedConversation());
                }
            }
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
}