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

/* Scripts\Mobiles\Guards\BritannianRanger.cs
 * Changelog
 * created 7/22/21, adam
 *  Rangers hide, so make updates to both BaseAI and WarriorGuard to prevent actions that would otherwise reveal us. Like meditation.
 *  We also suppress the guard chatter when a thief walks buy.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    public class BritannianRanger : PatrolGuard
    {
        DateTime m_NextHideTime = DateTime.UtcNow;
        public override bool MovementChatter { get { return false; } }
        [Constructable]
        public BritannianRanger()
            : this(null)
        {
        }

        public BritannianRanger(Mobile target)
            : base(target)
        {   // adjust fight mode to go after criminals
            base.FightMode = FightMode.Aggressor | FightMode.Criminal;
        }

        public BritannianRanger(Serial serial)
            : base(serial)
        {
        }

        public override void InitSkills()
        {
            SetSkill(SkillName.Anatomy, 115, 120.0);
            SetSkill(SkillName.Tactics, 115, 120.0);
            SetSkill(SkillName.Swords, 115, 120.0);
            SetSkill(SkillName.MagicResist, 115, 120.0);
            SetSkill(SkillName.DetectHidden, 88, 92);

            if (Core.RuleSets.AngelIslandRules())
            {
                SetSkill(SkillName.EvalInt, 115, 120.0);
                SetSkill(SkillName.Magery, 115, 120.0);
            }
        }
        public override void InitOutfit()
        {
            Title = "the ranger";
            SpeechHue = Utility.RandomSpeechHue();

            Hue = 0x83EA;

            Item hair;
            int hairHue = Utility.RandomHairHue();
            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
                hair = new LongHair(hairHue);
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
                hair = new PonyTail(hairHue);
            }

            Backpack = (Backpack)FindItemOnLayer(Layer.Backpack);
            if (Backpack != null)
                Backpack.Hue = 0x5E4;

            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            double chance = 0.98;
            AddItem(new RangerArms(), Utility.RandomDouble() < chance ? LootType.Newbied : LootType.Regular);
            AddItem(new RangerChest(), Utility.RandomDouble() < chance ? LootType.Newbied : LootType.Regular);
            AddItem(new RangerGloves(), Utility.RandomDouble() < chance ? LootType.Newbied : LootType.Regular);
            AddItem(new RangerGorget(), Utility.RandomDouble() < chance ? LootType.Newbied : LootType.Regular);
            AddItem(new RangerLegs(), Utility.RandomDouble() < chance ? LootType.Newbied : LootType.Regular);
            AddItem(new Boots(0x5E4), LootType.Newbied); // never drop, you need the IOB version

            Broadsword weapon = new Broadsword();

            weapon.Movable = false;
            weapon.Quality = WeaponQuality.Exceptional;

            if (Core.RuleSets.AngelIslandRules())
            {
                weapon.Slayer = SlayerName.Silver;
                weapon.Identified = false;
                weapon.HideAttributes = true;
                weapon.Name = "Property of Britain Armory";
            }
            else
                weapon.Crafter = this;

            AddItem(weapon);
        }

        // does this guard auto 'poof' when no longer needed?
        public override bool PoofingGuard { get { return false; } }
        public override bool UseGroundsKeeper { get { return false; } }
        public override void OnThink()
        {
            //if (Combatant != null && Combatant != Focus && Combatant.Criminal)
            //Focus = Combatant;

            base.OnThink();
        }
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            //if (m.Player)
            //(m.Region as Regions.GuardedRegion).CheckGuardCandidate(m);

            base.OnMovement(m, oldLocation);
        }
        #region Memory
        // how long we remember players in minutes
        private double m_memory = 30;                       // default: 30 minutes
        // how far until we talk to a player
        private int m_distance = 8;                         // default: 6 tiles
        private Memory m_PlayerMemory = new Memory();       // memory used to remember if we a saw a player in the area
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Memory
        {
            get { return TimeSpan.FromMinutes(m_memory); }
            set { m_memory = value.TotalMinutes; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int Distance
        {
            get { return m_distance; }
            set { m_distance = value; }
        }
        #endregion Memory
        #region See
        private enum Judgement
        {
            innocent,
            murderer,
            criminal
        }
        private Judgement EvaluatePlayer(Mobile m)
        {
            // we care most if this player is criminal (more than murderer)
            if (m.Criminal)
                return Judgement.criminal;
            else if (m.LongTermMurders > 0 || m.ShortTermMurders > 0)
                return Judgement.murderer;
            else return Judgement.innocent;
        }
        public override void OnSee(Mobile m)
        {
            // not a player
            if (m is PlayerMobile == false)
                return;

            // sanity
            if (m.Deleted || m.Hidden || !m.Alive || m.AccessLevel > this.AccessLevel || !this.CanSee(m))
                return;

            // too far away
            if (this.GetDistanceToSqrt(m) > m_distance)
                return;

            Judgement judgement = EvaluatePlayer(m);

            if (m_PlayerMemory.Recall(m) == false)
            {   // we havn't seen this player yet
                if (Hidden == false)
                {   // remember him for this long (*60 convert to minutes)
                    // don't bother remebering him if we can't talk to him
                    m_PlayerMemory.Remember(m, TimeSpan.FromSeconds(m_memory * 60).TotalSeconds);
                    this.Direction = this.GetDirectionTo(m); // face the player
                    switch (judgement)
                    {
                        case Judgement.innocent:
                            switch (Utility.Random(4))
                            {
                                case 0:
                                    Say(string.Format("Good day {0}.", m.Female ? "my lady" : "kind sir"));
                                    this.Animate(160, 5, 1, true, false, 0);    // bow?
                                    break;
                                case 1:
                                    Say(string.Format("The Britannian Rangers are at your service {0}.", m.Name));
                                    this.Animate(33, 5, 1, true, false, 0);     // Salute?
                                    break;
                                case 2:
                                    Say(string.Format("Here to hunt in the cemetery? Good luck to thee."));
                                    break;
                                case 3:
                                    Say(string.Format("We've got your back {0}.", m.Name));
                                    break;
                            }
                            break;
                        case Judgement.criminal:
                            switch (Utility.Random(4))
                            {
                                case 0:
                                    Say(string.Format("A real {0} doesn't harass new players trying to get their first kill.", m.Female ? "woman" : "man"));
                                    break;
                                case 1:
                                    Say(string.Format("The Britannian Rangers are protecting the area from folks like you {0}", m.Name));
                                    break;
                                case 2:
                                    Say(string.Format("Wanna try me instead {0}", m.Name));
                                    break;
                                case 3:
                                    Say(string.Format("Dont"));
                                    Say(string.Format("harm"));
                                    Say(string.Format("the"));
                                    Say(string.Format("newbies!"));
                                    break;
                            }
                            break;
                        case Judgement.murderer:
                            switch (Utility.Random(4))
                            {
                                case 0:
                                    Say(string.Format("{1}I've got my eye on you {0}.", m.Name, Utility.RandomBool() ? "Oh. " : ""));
                                    break;
                                case 1:
                                    Say(string.Format("Don't bring us any trouble {0}, or I'll give you a taste of my steel.", m.Name));
                                    break;
                                case 2:
                                    Say(string.Format("The Britannian Rangers will put up with no funny business from you {0}.", m.Name));
                                    break;
                                case 3:
                                    Say(string.Format("Hey everybody, look it's murder! Maybe there's a bounty on {0} head.", m.Female ? "her" : "his"));
                                    Say(string.Format("Ahaha haha!"));
                                    break;
                            }
                            break;
                    }
                }
            }
        }
        #endregion See
        public override void IAmHome()
        {   // we like to hide when we get home (RangeHome 0)
            base.IAmHome();

            if (this.Hidden == false && DateTime.UtcNow > m_NextHideTime)
            {
                new Server.Spells.Sixth.InvisibilitySpell(this, null).Cast();
                m_NextHideTime = DateTime.UtcNow + new TimeSpan(0, 0, 10);
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

            if (base.Version > 0)
            {
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {   // no work
                            break;
                        }
                }
            }
        }
    }
}