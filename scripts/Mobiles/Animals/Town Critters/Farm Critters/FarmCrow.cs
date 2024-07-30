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

/* Scripts\Mobiles\Animals\Town Critters\Farm Critters\FarmCrow.cs
 *	ChangeLog :
 *  10/1/2023, Adam
 *  First time check in
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a FarmCrow corpse")]
    public class FarmCrow : BaseCreature
    {
        [Constructable]
        public FarmCrow()
            : base(AIType.AI_Animal, FightMode.Aggressor | FightMode.Weakest, 10, 1, 0.25, 0.5)
        {
            Name = "a crow";
            Body = 5;
            BaseSoundID = 0x7D;
            Hue = 0x901;

            SetStr(31, 47);
            SetDex(36, 60);
            SetInt(8, 20);

            SetHits(20, 27);
            SetMana(0);

            SetDamage(5, 10);

            SetSkill(SkillName.MagicResist, 15.3, 30.0);
            SetSkill(SkillName.Tactics, 18.1, 37.0);
            SetSkill(SkillName.Wrestling, 20.1, 30.0);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 22;

            Tamable = false;
            GuardIgnore = true;
        }
        public override void OnThink()
        {
            if (Combatant == null || Combatant.Dead)
            {
                IPooledEnumerable eable = Map.Felucca.GetMobilesInRange(this.Location, 20);
                foreach (Mobile m in eable)
                {
                    if (m is PlayerMobile pm)
                    {
                        Server.Memory.ObjectMemory o = FarmableCrop.Pickers.Recall(m as object);
                        if (o == null || o.Context == null || o.Context is not Server.Mobiles.Spawner || m.GetDistanceToSqrt((o.Context as Spawner)) > 20)
                            // maybe the teleported to a field where they have not picked anything yet
                            continue;

                        // okay, we remember this guy, and he's in our field!
                        if (CanSee(pm))
                        {
                            Combatant = pm;
                            pm.SendMessage("You've been caught stealing from farmer Jones!");
                            break;
                        }
                        else
                        {
                            TargetLocation = new Point2D(pm.Location.X, pm.Location.Y);
                        }
                    }
                }
                eable.Free();
            }
            base.OnThink();
        }
        public override int Meat { get { return 1; } }
        public override MeatType MeatType { get { return MeatType.Bird; } }
        public override int Feathers { get { return 36; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat | FoodType.Fish; } }

        private static readonly CarveEntry[] m_RareCarvables = new CarveEntry[]
            {
                new CarveEntry(1, typeof(Item), 0x1E83, 0x1E84), // bird
                new CarveEntry(1, typeof(Item), 0x1E85, 0x1E86), // bird
            };

        public override double RareCarvableChance { get { return 0.015; } }
        public override CarveEntry[] RareCarvables { get { return m_RareCarvables; } }

        public FarmCrow(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(25, 50);
                PackItem(new BlackPearl(4));
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020221204551/uo.stratics.com/hunters/eagle.shtml
                    // 1 Raw bird (carved), 36 Feathers (carved)

                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {
                    // no loot in RunUO 2.0
                }
            }
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