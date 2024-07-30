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

/* Scripts/Mobiles/Familiars/HordeMinion.cs
 * ChangeLog
 *	7/16/10, adam
 *		o Make summonable
 *			o add DispelDifficulty
 *		o add OnBeforeDispel so we can drop our loot
 *		o increase control slots to 3. 
 *			this prevents tamers from having a packy AND uber pets
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Network;
using System;
using System.Collections;

namespace Server.Mobiles
{
    [CorpseName("a horde minion corpse")]
    public class HordeMinionFamiliar : BaseFamiliar
    {
        public override double DispelDifficulty { get { return 125.0; } }
        public override double DispelFocus { get { return 45.0; } }
        protected override Mobile Focus { get { return null; } }

        public HordeMinionFamiliar()
        {
            FightMode = FightMode.None;
            ControlOrder = OrderType.Follow;

            Name = "a horde minion";
            Body = 776;
            BaseSoundID = 0x39D;

            SetStr(100);
            SetDex(110);
            SetInt(100);

            SetHits(70);
            SetStam(110);
            SetMana(0);

            SetDamage(5, 10);

            SetSkill(SkillName.Wrestling, 70.1, 75.0);
            SetSkill(SkillName.Tactics, 50.0);

            ControlSlots = 3;

            Container pack = Backpack;

            if (pack != null)
                pack.Delete();

            pack = new StrongBackpack();
            pack.Movable = false;

            AddItem(pack);
        }

        private DateTime m_NextPickup;

        public override void OnThink()
        {
            base.OnThink();

            if (DateTime.UtcNow < m_NextPickup)
                return;

            m_NextPickup = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(5, 10));

            Container pack = this.Backpack;

            if (pack == null)
                return;

            ArrayList list = new ArrayList();

            IPooledEnumerable eable = this.GetItemsInRange(2);
            foreach (Item item in eable)
            {
                if (item.Movable && item.Stackable)
                    list.Add(item);
            }
            eable.Free();

            int pickedUp = 0;

            for (int i = 0; i < list.Count; ++i)
            {
                Item item = (Item)list[i];

                if (!pack.CheckHold(this, item, false, true))
                    return;

                bool rejected;
                LRReason reject;

                NextActionTime = Core.TickCount + Mobile.ActionDelay;

                Lift(item, item.Amount, out rejected, out reject);

                if (rejected)
                    continue;

                Drop(this, Point3D.Zero);

                if (++pickedUp == 3)
                    break;
            }
        }

        public override void OnBeforeDispel(Mobile Caster)
        {
            base.OnBeforeDispel(Caster);

            if (this.Backpack.Items.Count > 1)
                PackAnimal.CombineBackpacks(this);

            DropPackContents();
        }

        public override bool OnBeforeDeath()
        {
            if (!base.OnBeforeDeath())
                return false;

            OnBeforeDispel(null);

            return true;
        }

        #region Pack Animal Methods
        public override bool IsSnoop(Mobile from)
        {
            if (PackAnimal.CheckAccess(this, from))
                return false;

            return base.IsSnoop(from);
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (CheckFeed(from, item))
                return true;

            if (PackAnimal.CheckAccess(this, from))
            {
                AddToBackpack(item);
                return true;
            }

            return base.OnDragDrop(from, item);
        }

        public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
        {
            return PackAnimal.CheckAccess(this, from);
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return PackAnimal.CheckAccess(this, from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            PackAnimal.TryPackOpen(this, from);
        }

        public override void GetContextMenuEntries(Mobile from, System.Collections.ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            PackAnimal.GetContextMenuEntries(this, from, list);
        }
        #endregion

        public HordeMinionFamiliar(Serial serial)
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