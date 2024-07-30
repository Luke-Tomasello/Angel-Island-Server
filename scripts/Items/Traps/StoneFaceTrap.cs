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

/* /sandbox/ai/Scripts/Items/Traps/StoneFaceTrap.cs
 *	ChangeLog :
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 */

using System;

namespace Server.Items
{
    public enum StoneFaceTrapType
    {
        NorthWestWall,
        NorthWall,
        WestWall
    }

    public class StoneFaceTrap : BaseTrap
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public StoneFaceTrapType Type
        {
            get
            {
                switch (ItemID)
                {
                    case 0x10F5:
                    case 0x10F6:
                    case 0x10F7: return StoneFaceTrapType.NorthWestWall;
                    case 0x10FC:
                    case 0x10FD:
                    case 0x10FE: return StoneFaceTrapType.NorthWall;
                    case 0x110F:
                    case 0x1110:
                    case 0x1111: return StoneFaceTrapType.WestWall;
                }

                return StoneFaceTrapType.NorthWestWall;
            }
            set
            {
                bool breathing = this.Breathing;

                ItemID = (breathing ? GetFireID(value) : GetBaseID(value));
            }
        }

        public bool Breathing
        {
            get { return (ItemID == GetFireID(this.Type)); }
            set
            {
                if (value)
                    ItemID = GetFireID(this.Type);
                else
                    ItemID = GetBaseID(this.Type);
            }
        }

        public static int GetBaseID(StoneFaceTrapType type)
        {
            switch (type)
            {
                case StoneFaceTrapType.NorthWestWall: return 0x10F5;
                case StoneFaceTrapType.NorthWall: return 0x10FC;
                case StoneFaceTrapType.WestWall: return 0x110F;
            }

            return 0;
        }

        public static int GetFireID(StoneFaceTrapType type)
        {
            switch (type)
            {
                case StoneFaceTrapType.NorthWestWall: return 0x10F7;
                case StoneFaceTrapType.NorthWall: return 0x10FE;
                case StoneFaceTrapType.WestWall: return 0x1111;
            }

            return 0;
        }

        [Constructable]
        public StoneFaceTrap()
            : base(0x10FC)
        {
            Light = LightType.Circle225;
        }

        public override bool PassivelyTriggered { get { return true; } }
        public override TimeSpan PassiveTriggerDelay { get { return TimeSpan.Zero; } }
        public override int PassiveTriggerRange { get { return 2; } }
        public override TimeSpan ResetDelay { get { return TimeSpan.Zero; } }

        public override void OnTrigger(Mobile from)
        {
            if (!from.Alive)
                return;

            Effects.PlaySound(Location, Map, 0x359);

            Breathing = true;

            Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerCallback(FinishBreath));
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerCallback(TriggerDamage));

            base.OnTrigger(from);
        }

        public virtual void FinishBreath()
        {
            Breathing = false;
        }

        public virtual void TriggerDamage()
        {
            IPooledEnumerable eable = GetMobilesInRange(1);
            foreach (Mobile mob in eable)
            {
                if (mob.Alive && !mob.IsDeadBondedPet)
                    Spells.SpellHelper.Damage(TimeSpan.FromTicks(1), mob, mob, Utility.Dice(3, 15, 0));
            }
            eable.Free();
        }

        public StoneFaceTrap(Serial serial)
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

            Breathing = false;
        }
    }

    public class StoneFaceTrapNoDamage : StoneFaceTrap
    {
        [Constructable]
        public StoneFaceTrapNoDamage()
        {
        }

        public StoneFaceTrapNoDamage(Serial serial)
            : base(serial)
        {
        }

        public override void TriggerDamage()
        {
            // nothing..
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