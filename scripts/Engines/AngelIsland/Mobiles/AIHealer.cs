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

/* Scripts/Mobiles/Healers/AIHealer.cs
 * Change log
 * 1/2/23, Adam
 *  General cleanup
 * 4/1/04, changes by mith
 * Added Frozen and Direction properties so healer won't wander and always faces south.
 * 3/28/04
 *  Removed code to generate AIStinger, this has been moved to the AITeleporter.
 *  Removed ability to buy/sell items and teach skills.
 *  Set "AlwaysMurderer" flag to false so that NPC shows as blue.
 *  Created 3/28/04 by mith, copied from EvilHealer.cs
 */

using System;

namespace Server.Mobiles
{
    public class AIHealer : BaseHealer
    {
        public override bool CanTeach { get { return false; } }
        public override bool IsActiveVendor { get { return false; } }
        public override bool AlwaysMurderer { get { return false; } }

        [Constructable]
        public AIHealer()
        {
            Title = "the healer";

            AI = AIType.AI_HumanMage;
            ActiveSpeed = 0.2;
            PassiveSpeed = 0.8;
            RangePerception = BaseCreature.DefaultRangePerception;
            FightMode = FightMode.Aggressor;

            Karma = -10000;

            SetSkill(SkillName.Camping, 80.0, 100.0);
            SetSkill(SkillName.Forensics, 80.0, 100.0);
            SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);
        }
        public override Characteristics MyCharacteristics { get { return base.MyCharacteristics & ~Characteristics.DamageSlows; } }
        private Memory m_aggressionMemory = new Memory();       // memory used to remember if we a saw a player in the area
        const int AggressionMemoryTime = 120;                    // how long (seconds) we remember this player
        public override void OnHarmfulAction(Mobile aggressor, bool isCriminal, object source = null)
        {   // call for reinforcements!
            base.OnHarmfulAction(aggressor, isCriminal, source);
            if (m_aggressionMemory.Recall(aggressor) == false)
            {   // we haven't seen this player yet
                m_aggressionMemory.Remember(aggressor, TimeSpan.FromSeconds(AggressionMemoryTime).TotalSeconds);   // remember him for this long
                IPooledEnumerable eable = this.GetMobilesInRange(this.RangePerception);
                foreach (Mobile m in eable)
                {   // call the warden for help
                    if (m is AIWarden)
                    {   // cause a ruckus
                        Yell("{0}, help!", m.Name);
                        aggressor.DoHarmful(m);
                        break;
                    }
                }
                eable.Free();
            }
        }
        public override void InitSBInfo()
        {
        }

        public override bool CheckResurrect(Mobile m)
        {
            return true;
        }

        public AIHealer(Serial serial)
            : base(serial)
        {
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