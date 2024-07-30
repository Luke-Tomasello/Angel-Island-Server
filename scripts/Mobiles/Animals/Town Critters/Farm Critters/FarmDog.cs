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

/* Scripts\Mobiles\Animals\Town Critters\Farm Critters\FarmDog.cs
 *	ChangeLog :
 *	    10/1/2023, Adam
 *	    First time check in
 */

using Server.Commands;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a farm dog corpse")]
    public class FarmDog : BaseCreature
    {
        [Constructable]
        public FarmDog()
            : base(AIType.AI_Melee, FightMode.Aggressor | FightMode.Weakest, 10, 1, 0.25, 0.5)
        {
            Name = NameList.RandomName("GuardDog");
            Body = 23;
            BaseSoundID = 0x85; // dog sounds

            SetStr(96, 120);
            SetDex(81, 105);
            SetInt(36, 60);

            SetHits(58, 72);
            SetMana(0);

            SetDamage(11, 17);

            SetSkill(SkillName.MagicResist, 57.6, 75.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 0;
            Karma = 300;

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
                            EmoteCommand.OnCommand(new CommandEventArgs(this, "e", "sniff", new string[] { "sniff" }));
                        }
                    }
                }
                eable.Free();
            }
            base.OnThink();
        }
        public override int Meat { get { return 1; } }
        public override int Hides { get { return 7; } }
        public override HideType HideType { get { return HideType.Spined; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public override PackInstinct PackInstinct { get { return PackInstinct.Canine; } }

        public FarmDog(Serial serial)
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