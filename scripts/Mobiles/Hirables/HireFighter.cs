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

/* Scripts\Mobiles\Hirables\HireFighter.cs
 * ChangeLog
 * 7/22/04 - Old Salty:  Fighter now computes his "price" on being born, so you can't hire them for 1 gp.
 * 6/12/04 - Old Salty:  Changed "ActiveSpeed" so that these guys aren't as fast
 */

using Server.Diagnostics;
using Server.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using static Server.Utility;

namespace Server.Mobiles
{
    public class HireFighter : BaseHire
    {
        string m_type = string.Empty;
        [Constructable]
        public HireFighter()
            : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.175, 0.5)
        {
            SpeechHue = Utility.RandomSpeechHue();
            Hue = Utility.RandomSkinHue();
            UsesHumanWeapons = true;                    // so that it consumes arrows/bolts/weapons
            if (this.Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }

            //Title = "the fighter";
            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomNeutralHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            if (Utility.RandomBool() && !this.Female)
            {
                Item beard = new Item(Utility.RandomList(0x203E, 0x203F, 0x2040, 0x2041, 0x204B, 0x204C, 0x204D));

                beard.Hue = hair.Hue;
                beard.Layer = Layer.FacialHair;
                beard.Movable = false;

                AddItem(beard);
            }

            SetStr(91, 91);
            SetDex(91, 91);
            SetInt(50, 50);

            SetDamage(7, 14);

            SetSkill(SkillName.Tactics, 50, 60);
            SetSkill(SkillName.Anatomy, 50, 60);
            SetSkill(SkillName.Parry, 50, 60);

            switch (Utility.Random(5))  //pick what type of fighter they will be
            {
                case 0: //sword fighter
                    MakeSwordsman();
                    m_type = "swordsman";
                    break;
                case 1: //mace fighter
                    MakeMacer();
                    m_type = "macer";
                    break;
                case 2: //fencer
                    MakeFencer();
                    m_type = "fencer";
                    break;
                case 3: //wrestler
                    SetSkill(SkillName.Wrestling, 40, 80);
                    m_type = "wrestler";
                    break;
                case 4: //archer
                    MakeArcher();
                    m_type = "archer";
                    break;
            }

            if (HasFreeHand() && Utility.RandomBool())
                AddShield();

            Fame = 100;
            Karma = 100;

            AddItem(new Shoes(Utility.RandomNeutralHue()));
            AddItem(new Shirt());

            // Pick some armor
            switch (Utility.Random(5))
            {
                case 0: // Leather
                    AddItem(new LeatherChest());
                    AddItem(new LeatherArms());
                    AddItem(new LeatherGloves());
                    AddItem(new LeatherGorget());
                    AddItem(new LeatherLegs());
                    AddHelm();
                    break;

                case 1: // Studded Leather
                    AddItem(new StuddedChest());
                    AddItem(new StuddedArms());
                    AddItem(new StuddedGloves());
                    AddItem(new StuddedGorget());
                    AddItem(new StuddedLegs());
                    AddHelm();
                    break;

                case 2: // Ringmail
                    AddItem(new RingmailChest());
                    AddItem(new RingmailArms());
                    AddItem(new RingmailGloves());
                    AddItem(new RingmailLegs());
                    AddHelm();
                    break;

                case 3: // Chain
                    AddItem(new ChainChest());
                    AddItem(new ChainCoif());
                    AddItem(new ChainLegs());
                    break;

                case 4: // Plate
                    AddItem(new PlateChest());
                    AddItem(new PlateArms());
                    AddItem(new PlateGloves());
                    AddItem(new PlateGorget());
                    AddItem(new PlateLegs());
                    AddHelm();
                    break;
            }

            foreach (Item item in Items)
                item.SetItemBool(Item.ItemBoolTable.DeleteOnLift, true);       // don't allow players to farm fighters for their items
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Type
        {
            get { return !string.IsNullOrEmpty(m_type) ? char.ToUpper(m_type.First()) + m_type.Substring(1).ToLower() : "Unknown"; }
        }
        public override string Title
        {
            get
            {
                return string.Format("the {0}", m_type.ToLower());
            }
            set {; }
        }
        private List<Item> FindItems(Type type)
        {
            List<Item> list = new();

            foreach (Item item in Items)
                if (type.IsAssignableFrom(item.GetType()))
                    list.Add(item);

            if (this.Backpack != null)
                foreach (Item item in this.Backpack.Items)
                    if (type.IsAssignableFrom(item.GetType()))
                        list.Add(item);

            return list;
        }
        private bool FindItem(Type type)
        {
            if (FindItemByType(type) != null)
                return true;
            if (this.Backpack != null)
                if (this.Backpack.FindItemByType(type) != null)
                    return true;
            return false;
        }
        private Type FindItemType(Type type)
        {
            Item item;
            if ((item = FindItemByType(type)) != null)
                return item.GetType();
            if (this.Backpack != null)
                if ((item = this.Backpack.FindItemByType(type)) != null)
                    return item.GetType();
            return null;
        }
        private new void RemoveItem(Item item)
        {
            base.RemoveItem(item);
            if (this.Backpack != null)
                this.Backpack.RemoveItem(item);
        }
        private void SpeakUp(string message)
        {
            if (this.AIObject.GetMobileInfo(new MobileInfo(ControlMaster)).available)
                SayTo(ControlMaster, message);
            else
                Say(message);
        }
        public override bool CanLore { get { return true; } }
        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            bool accepted = base.OnDragDrop(from, dropped);
            if (!accepted)
                accepted = EvaluateItemAdded(from, dropped);
            return accepted;
        }
        private bool AlreadyHasOne(Item dropped, Type type, bool already_added)
        {
            List<Item> list = FindItems(type);
            int count = list.Count;
            return (dropped.GetType().IsAssignableTo(type) && (count > 1 && already_added || count == 1 && !already_added));
        }
        public bool EvaluateClothingItem(Mobile from, Item dropped)
        {
            switch (dropped.Layer)
            {
                case Layer.OneHanded:
                case Layer.TwoHanded:
                    return false;
            }
            return true;
        }
        public bool EvaluateItemAdded(Mobile from, Item dropped, bool already_added = false)
        {
            /* 
             * This evaluator decides whether of not an item will be accepted.
             * It is called from both OnDragDrop and AddItem.
             * OnDragDrop - the item is not yet in our inventory
             * AddItem - item is already in out inventory
             * The test: AlreadyHasOne() - (count > 1 && already_added || count == 1 && !already_added)
             * makes this determination.
             */
            bool accepted = false;
            string notTheWep = "Not the type of weapon I'm used to.";
            string alreadyGotWeapon = "I've already got a weapon, thanks.";
            string alreadyGotShield = "I've already got a shield, thanks.";
            string ty = "Thanks.";
            string ready = "Ready.";
            string needBolts = "I'll need bolts to go along with that.";
            string needArrows = "I'll need arrows to go along with that.";
            string needWeapon = "Still need a weapon.";
            string cannotHold = "I cannot hold that.";
            string ImAWrestler = "I'm a wrestler!";

            bool duplicate = false;
            bool alreadyHasWeapon = AlreadyHasOne(dropped, typeof(BaseWeapon), already_added);
            bool alreadyHasShield = AlreadyHasOne(dropped, typeof(BaseShield), already_added);
            if (alreadyHasWeapon || alreadyHasShield)
            {
                if (alreadyHasWeapon)
                    SpeakUp(alreadyGotWeapon);
                else
                    SpeakUp(alreadyGotShield);
                accepted = false;
                duplicate = true;
            }
            if (!accepted && !duplicate)
            {
                switch (m_type)
                {
                    case "swordsman": //sword fighter
                        {
                            Type[] mine = new Type[] { typeof(BaseSword), typeof(BaseAxe), typeof(BasePoleArm) };
                            if (dropped.GetType().IsAssignableTo(typeof(BaseWeapon)))
                            {
                                //if (!dropped.GetType().IsAssignableTo(mine[0]) && !dropped.GetType().IsAssignableTo(mine[1]))
                                if ((dropped as BaseWeapon).AosSkill != SkillName.Swords)
                                    SpeakUp(notTheWep);
                                else
                                {
                                    SpeakUp(ty);
                                    PackItem(dropped);
                                    SpeakUp(ready);
                                    Equip(mine);
                                    accepted = true;
                                }
                            }
                        }
                        break;
                    case "macer": //mace fighter
                        {
                            Type[] mine = new Type[] { typeof(BaseBashing), null };
                            if (dropped.GetType().IsAssignableTo(typeof(BaseWeapon)))
                            {
                                if (!dropped.GetType().IsAssignableTo(mine[0]))
                                    SpeakUp(notTheWep);
                                else
                                {
                                    SpeakUp(ty);
                                    PackItem(dropped);
                                    SpeakUp(ready);
                                    Equip(mine);
                                    accepted = true;
                                }
                            }
                        }
                        break;
                    case "fencer": //fencer
                        {
                            Type[] mine = new Type[] { typeof(BaseSpear), typeof(BaseSword) };
                            if (dropped.GetType().IsAssignableTo(typeof(BaseWeapon)))
                            {
                                if ((dropped as BaseWeapon).AosSkill != SkillName.Fencing)
                                    SpeakUp(notTheWep);
                                else
                                {
                                    SpeakUp(ty);
                                    PackItem(dropped);
                                    SpeakUp(ready);
                                    Equip(mine);
                                    accepted = true;
                                }
                            }
                        }
                        break;
                    case "wrestler": //wrestler
                        {
                            Type[] mine = new Type[] { typeof(BaseShield), null };
                            if (dropped.GetType().IsAssignableTo(typeof(BaseWeapon)))
                            {
                                SpeakUp(cannotHold);
                                SpeakUp(ImAWrestler);
                            }
                            else if (dropped.GetType().IsAssignableTo(typeof(BaseShield)))
                            {
                                if (!dropped.GetType().IsAssignableTo(mine[0]))
                                    SpeakUp(notTheWep);
                                else
                                {
                                    SpeakUp(ty);
                                    PackItem(dropped);
                                    SpeakUp(ready);
                                    Equip(mine);
                                    accepted = true;
                                }
                            }
                        }
                        break;
                    case "archer":
                        {
                            Type[] mine = new Type[] { typeof(BaseRanged), null };
                            if (dropped.GetType() == typeof(Bolt) || dropped.GetType() == typeof(Arrow))
                            {
                                SpeakUp(ty);
                                PackItem(dropped);
                                accepted = true;
                            }
                            else if (dropped.GetType().IsAssignableTo(typeof(BaseWeapon)))
                            {
                                if (!dropped.GetType().IsAssignableTo(mine[0]))
                                    SpeakUp(notTheWep);
                                else
                                {
                                    SpeakUp(ty);
                                    PackItem(dropped);
                                    accepted = true;
                                }
                            }

                            Type type;
                            if (accepted)
                            {
                                if ((type = FindItemType(typeof(BaseWeapon))) == null)
                                {
                                    SpeakUp(needWeapon);
                                }
                                else if (type == typeof(HeavyCrossbow) || type == typeof(Crossbow))
                                {
                                    if (FindItemType(typeof(Bolt)) == null)
                                        SpeakUp(needBolts);
                                    else
                                    {
                                        SpeakUp(ready);
                                        Equip(mine);
                                    }
                                }
                                else if (type == typeof(Bow))
                                {
                                    if (FindItemType(typeof(Arrow)) == null)
                                        SpeakUp(needArrows);
                                    else
                                    {
                                        SpeakUp(ready);
                                        Equip(mine);
                                    }
                                }
                            } // archer

                            break;
                        }
                }

                if (!accepted && dropped is BaseShield)
                {
                    bool twohanded = dropped.Layer.HasFlag(Layer.TwoHanded);
                    bool onehanded = dropped.Layer.HasFlag(Layer.OneHanded);
                    bool hasTwoFreeHands = FindItemOnLayer(Layer.TwoHanded) == null || FindItemOnLayer(Layer.TwoHanded) == dropped;
                    bool hasOneFreeHand = FindItemOnLayer(Layer.OneHanded) == null || FindItemOnLayer(Layer.OneHanded) == dropped;
                    if (twohanded && hasTwoFreeHands)
                    {
                        SpeakUp(ty);
                        PackItem(dropped);
                        SpeakUp(ready);
                        Equip(new Type[] { typeof(BaseShield), null });
                        accepted = true;
                    }
                    if (onehanded && hasOneFreeHand || hasTwoFreeHands)
                    {
                        SpeakUp(ty);
                        accepted = true;
                    }
                    else
                        SpeakUp(cannotHold);
                }
            }

            if (already_added && !accepted)
                RemoveItem(dropped);

            return accepted;
        }
        private bool Equip(Type[] types)
        {
            Container pack = Backpack;
            if (pack != null)
            {
                Item equipedTwoHanded = FindItemOnLayer(Layer.TwoHanded);
                Item packGear = null;

                // see if we got one
                foreach (Type t in types)
                    if (t != null)
                        if ((packGear = pack.FindItemByType(t)) != null)
                            break;

                if (packGear != null)
                {
                    if (equipedTwoHanded != null)
                        PackItem(equipedTwoHanded);

                    if (packGear != null)
                        base.AddItem(packGear);

                    // else it's already in our hands
                    return true;
                }
            }
            // our weapon was destroyed (or error, we *should* have a backpack)
            return false;
        }
        public override void WeaponStatus(BaseWeapon weapon, object o)
        {
            if (ControlMaster != null)
            {
                string message = "Weapon getting critical here!";
                if (this.AIObject.GetMobileInfo(new MobileInfo(ControlMaster)).available)
                    SayTo(ControlMaster, message);
                else
                    Say(message);
            }
        }
        public override void ArmorStatus(BaseArmor armor, object o)
        {
            if (ControlMaster != null)
            {
                string message = "Armor getting critical here!";
                if (this.AIObject.GetMobileInfo(new MobileInfo(ControlMaster)).available)
                    SayTo(ControlMaster, message);
                else
                    Say(message);
            }
        }
        public override void ClothingStatus(BaseClothing clothing, object o)
        {
            ;
        }
        public override void Destroyed(Item item)
        {
            if (ControlMaster != null)
                if (true)
                {
                    string message = string.Format("{0} destroyed!", item.GetType().Name);
                    if (this.AIObject.GetMobileInfo(new MobileInfo(ControlMaster)).available)
                        SayTo(ControlMaster, message);
                    else
                        Say(message);
                }
        }
        Utility.LocalTimer AmmoStatusTimer = new Utility.LocalTimer(0);
        public override void AmmoStatus(Item ammo)
        {
            if (ControlMaster != null)
                if (ammo.Amount <= 20 && (AmmoStatusTimer.Triggered || ammo.Amount == 0))
                {
                    AmmoStatusTimer.Start(TimeSpan.FromSeconds(5).TotalMilliseconds);
                    string message = "Ammo getting low here!";
                    if (ammo.Amount == 0)
                        message = "Out of ammo!";
                    if (this.AIObject.GetMobileInfo(new MobileInfo(ControlMaster)).available)
                        SayTo(ControlMaster, message);
                    else
                        Say(message);
                }
        }
        public override void AddItem(Item item)
        {
            bool fromMyMaster = ControlMaster is Mobile from && from.InRange(this, 3) && from.LastHeld == item;
            if (ControlMaster is Mobile && EvaluateClothingItem(ControlMaster, item))
            {   // just regular dress-up clothing
                base.AddItem(item);
                return;
            }
            // otherwise, we need to look at the weapon or shield to decide if we can use it
            base.AddItem(item);
            if (fromMyMaster)
                Timer.DelayCall(TimeSpan.FromSeconds(0.2), new TimerStateCallback(EvaluateItemAddedTick), new object[] { item });
        }
        private void EvaluateItemAddedTick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] is Item dropped)
            {
                if (EvaluateItemAdded(ControlMaster, dropped, already_added: true) == false)
                {
                    RemoveItem(dropped);
                    dropped.MoveToWorld(this.Location, this.Map);
                }
            }
        }
        public override bool ClickTitle { get { return false; } }
        public override void ProcessPay()
        {
            base.ProcessPay();

            if (HoldGold <= 0)
                DoFighterDeath();
        }
        public override int RequiredPay()
        {
            int skillsTotal = 0;

            // TODO: Shouldn't we divide this by 4?
            skillsTotal += (int)Skills[SkillName.Macing].Base;
            skillsTotal += (int)Skills[SkillName.Swords].Base;
            skillsTotal += (int)Skills[SkillName.Fencing].Base;
            skillsTotal += (int)Skills[SkillName.Wrestling].Base;
            skillsTotal += (int)Skills[SkillName.Archery].Base;

            int pay;

            if (skillsTotal < 50)
                pay = 6;
            else if (skillsTotal < 65)
                pay = 7;
            else if (skillsTotal < 80)
                pay = 8;
            else if (skillsTotal < 90)
                pay = 9;
            else
                pay = 10;

            if (Core.RuleSets.SiegeStyleRules())
                pay *= 3;

            return pay;
        }
        public override bool KeepsItemsOnDeath { get { return false; } }    // players can get THEIR stuff back
        public override bool OnBeforeRelease(Mobile controlMaster)
        {
            DoFighterDeath();
            return base.OnBeforeRelease(controlMaster);
        }
        public override bool OnBeforeDeath()
        {

            LogHelper logger = new LogHelper("hireling death.log", overwrite: false, sline: true);
            logger.Log(LogType.Mobile, this, "died.");
            foreach (DamageEntry de in DamageEntries)
            {
                logger.Log(string.Format("was killed by {0}", de.Damager));
            }
            logger.Finish();

            foreach (Item item in this.Items)
                if (item.GetItemBool(Item.ItemBoolTable.DeleteOnLift))       // don't allow players to farm fighters for their items)
                    item.LootType = LootType.Newbied;
            return base.OnBeforeDeath();
        }
        private void DoFighterDeath()
        {
            Say(503235); // I regret nothing!postal 
            BasePotion.PlayDrinkEffect(this);
            this.ApplyPoison(this, Poison.Lethal);
            this.SetMobileBool(MobileBoolTable.Incurable, true);
        }
        public HireFighter(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version); // version 

            switch (version)
            {
                case 1:
                    {
                        writer.Write(m_type);
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_type = reader.ReadString();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 1)
                            UsesHumanWeapons = true;                    // so that it consumes arrows/bolts/weapons

                        Timer.DelayCall(TimeSpan.FromSeconds(0.2), new TimerStateCallback(ConfigureTick), new object[] { version });
                        break;
                    }
            }
            #region Speed up older versions
            if (CurrentSpeed == ActiveSpeed)
            {
                CurrentSpeed = ActiveSpeed = 0.175;
                PassiveSpeed = 0.5;
            }
            else if (CurrentSpeed == PassiveSpeed)
            {
                CurrentSpeed = PassiveSpeed = 0.5;
                ActiveSpeed = 0.175;
            }
            else
            {
                ActiveSpeed = 0.175;
                CurrentSpeed = PassiveSpeed = 0.5;
            }
            #endregion Speed up older versions
        }
        private void ConfigureTick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] is int version)
            {
                // what kind of fighter are we?
                if (version < 1)
                {
                    if (FindItemByType(typeof(BaseBashing)) != null)
                        m_type = "macer";
                    if (FindItemByType(typeof(BaseSpear)) != null)
                        m_type = "fencer";
                    if (FindItemByType(typeof(BaseSword)) != null)
                        m_type = "swordsman";
                    if (FindItemByType(typeof(BaseRanged)) != null)
                        m_type = "archer";
                }
            }
        }
        public void AddHelm()
        {
            switch (Utility.Random(6))
            {
                case 0: break;
                case 1: AddItem(new Bascinet()); break;
                case 2: AddItem(new CloseHelm()); break;
                case 3: AddItem(new NorseHelm()); break;
                case 4: AddItem(new Helmet()); break;
                case 5: AddItem(new PlateHelm()); break;
            }
        }
        public void MakeSwordsman()
        {
            SetSkill(SkillName.Swords, 40, 80);

            switch (Utility.Random(4)) // Pick a random sword
            {
                case 0: AddItem(new Longsword()); break;
                case 2: AddItem(new VikingSword()); break;
                case 3: AddItem(new BattleAxe()); break;
                case 4: AddItem(new TwoHandedAxe()); break;
            }
        }
        public void MakeMacer()
        {
            SetSkill(SkillName.Macing, 40, 80);

            switch (Utility.Random(4)) //Pick a random mace 
            {
                case 0: AddItem(new Club()); break;
                case 1: AddItem(new WarAxe()); break;
                case 2: AddItem(new WarHammer()); break;
                case 3: AddItem(new QuarterStaff()); break;
            }
        }
        public void MakeArcher()
        {
            SetSkill(SkillName.Archery, 40, 80);
            SetSkill(SkillName.Parry, 0);
            switch (Utility.Random(4)) //Pick a random bow 
            {
                case 0:
                    {
                        AddItem(new HeavyCrossbow());
                        PackItem(new Bolt(20), lootType: LootType.Newbied);
                        AI = AIType.AI_Archer;
                        break;
                    }
                case 1:
                    {
                        AddItem(new Crossbow());
                        PackItem(new Bolt(20), lootType: LootType.Newbied);
                        AI = AIType.AI_Archer;
                        break;
                    }
                case 2:
                case 3:
                    {
                        AddItem(new Bow());
                        PackItem(new Arrow(20), lootType: LootType.Newbied);
                        AI = AIType.AI_Archer;
                        break;
                    }
            }
        }
        public void MakeFencer()
        {
            SetSkill(SkillName.Fencing, 40, 80);

            switch (Utility.Random(4))  //Pick a random fencing wep
            {
                case 0: AddItem(new WarFork()); break;
                case 1: AddItem(new Kryss()); break;
                case 2: AddItem(new Spear()); break;
                case 3: AddItem(new ShortSpear()); break;
            }
        }
        public void AddShield()
        {
            switch (Utility.Random(6)) // Pick a random shield
            {
                case 0: AddItem(new BronzeShield()); break;
                case 1: AddItem(new HeaterShield()); break;
                case 2: AddItem(new MetalKiteShield()); break;
                case 3: AddItem(new MetalShield()); break;
                case 4: AddItem(new WoodenKiteShield()); break;
                case 5: AddItem(new WoodenShield()); break;
            }
        }
    }
}