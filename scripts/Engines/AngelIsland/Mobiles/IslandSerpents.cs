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

/* Scripts\Engines\AngelIsland\Mobiles\IslandSerpents.cs
 * ChangeLog
 *	7/10/2024, Adam
 *	    created
 */

using Server.Items;
using System.Collections.Generic;

namespace Server.Mobiles
{

    [CorpseName("a gila monster corpse")]
    [TypeAlias("Server.Mobiles.IslandSerpentModerate")]
    public class GilaMonsterModerate : BaseCreature
    {
        [Constructable]
        public GilaMonsterModerate()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a gila monster";
            Body = 0xCE;
            BaseSoundID = 0x5A;
            Hue = 0xB8F;    // assume a mottled hue

            SetStr(216, 245);
            SetDex(76, 100);
            SetInt(66, 85);

            SetHits(130, 147);

            SetDamage(7, 17);

            SetSkill(SkillName.Poisoning, 75.1, 95.0);
            SetSkill(SkillName.MagicResist, 45.1, 60.0);
            SetSkill(SkillName.Tactics, 75.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 40;

            Tamable = true;
            ControlSlots = 2;       // nightmare class
            MinTameSkill = 71.1;    // giant ice worm

            SetCreatureBool(CreatureBoolTable.IsPrisonPet, true);
        }
        public override void OnCombatantChange()
        {
            if (Combatant != null)
                PlaySound(0x26B);
        }
        public override bool EatCorpse(Corpse c)
        {
            if (c != null && this.IsTame && ControlMaster is PlayerMobile pm)
                if (c is ICarvable)
                    if (this.LoyaltyDisplay < PetLoyalty.WonderfullyHappy)
                    {
                        Katana sword = new Katana();
                        ((ICarvable)c).Carve(this, sword);
                        sword.Delete();

                        // creature
                        List<Item> list = new List<Item>();
                        if (c.Items != null && c.Items.Count > 0)
                            foreach (var item in c.Items)
                                if (item is RawRibs || item is RawBird || item is RawLambLeg || item is RawFishSteak)
                                    list.Add(item);

                        //humanoid
                        if (list.Count == 0 && c.Owner != null && c.Owner.Body.IsHuman)
                        {
                            IPooledEnumerable eable = this.Map.GetItemsInRange(this.Location, range: 3);
                            foreach (Item thing in eable)
                                if (thing is Head || thing is Torso || thing is LeftLeg || thing is LeftArm || thing is RightLeg || thing is RightArm)
                                    list.Add(thing);
                            eable.Free();
                        }

                        foreach (Item food in list)
                            if (this.LoyaltyDisplay >= PetLoyalty.WonderfullyHappy)
                                break;
                            else
                                OnDragDrop(pm, food);
                    }
            return false;
        }
        public override int Meat { get { return 1; } }
        public override Poison PoisonImmune { get { return Poison.Greater; } }
        public override Poison HitPoison { get { return Poison.Greater; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public GilaMonsterModerate(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }


    }
    [CorpseName("a gila monster corpse")]
    [TypeAlias("Server.Mobiles.IslandSerpentStrong")]
    public class GilaMonsterStrong : BaseCreature
    {
        [Constructable]
        public GilaMonsterStrong()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a gila monster";
            Body = 0xCE;
            BaseSoundID = 0x5A;
            Hue = 0xB8e;

            SetStr(161, 360);
            SetDex(151, 300);
            SetInt(21, 40);

            SetHits(97, 216);

            SetDamage(5, 21);

            SetSkill(SkillName.Poisoning, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 95.1, 100.0);
            SetSkill(SkillName.Tactics, 80.1, 95.0);
            SetSkill(SkillName.Wrestling, 85.1, 100.0);

            Fame = 7000;
            Karma = -7000;

            VirtualArmor = 40;

            Tamable = true;
            ControlSlots = 3;       // dragon class
            MinTameSkill = 84.3;    // drake class

            SetCreatureBool(CreatureBoolTable.IsPrisonPet, true);
        }
        public override void OnCombatantChange()
        {
            if (Combatant != null)
                PlaySound(0x26B);
        }
        public override bool EatCorpse(Corpse c)
        {
            if (c != null && this.IsTame && ControlMaster is PlayerMobile pm)
                if (c is ICarvable)
                    if (this.LoyaltyDisplay < PetLoyalty.WonderfullyHappy)
                    {
                        Katana sword = new Katana();
                        ((ICarvable)c).Carve(this, sword);
                        sword.Delete();

                        // creature
                        List<Item> list = new List<Item>();
                        if (c.Items != null && c.Items.Count > 0)
                            foreach (var item in c.Items)
                                if (item is RawRibs || item is RawBird || item is RawLambLeg || item is RawFishSteak)
                                    list.Add(item);

                        //humanoid
                        if (list.Count == 0 && c.Owner != null && c.Owner.Body.IsHuman)
                        {
                            IPooledEnumerable eable = this.Map.GetItemsInRange(this.Location, range: 3);
                            foreach (Item thing in eable)
                                if (thing is Head || thing is Torso || thing is LeftLeg || thing is LeftArm || thing is RightLeg || thing is RightArm)
                                    list.Add(thing);
                            eable.Free();
                        }

                        foreach (Item food in list)
                            if (this.LoyaltyDisplay >= PetLoyalty.WonderfullyHappy)
                                break;
                            else
                                OnDragDrop(pm, food);
                    }
            return false;
        }
        public override int Meat { get { return 1; } }
        public override Poison PoisonImmune { get { return Poison.Greater; } }
        public override Poison HitPoison { get { return Poison.Greater; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public GilaMonsterStrong(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

}