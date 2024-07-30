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

/* Scripts\Engines\Township\Gumps\TSRancherGump.cs
 * CHANGELOG:
 *  1/15/23, Yoar
 *      Releasing previously owned pets from the livestock will now instantly retame them.
 *  8/23/23, Yoar
 *      Initial version.
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Township
{
    public class TSRancherGump : BaseTownshipGump
    {
        private enum Button
        {
            Close,
            AddLivestock,

            Release = 1000,
            Claim = 2000,
        }

        private object m_State;

        public TSRancherGump(Items.TownshipStone stone, Mobile from)
            : base(stone)
        {
            BaseCreature[] livestock = (new List<BaseCreature>(m_Stone.Livestock.Keys)).ToArray();

            m_State = livestock;

            AddBackground();

            AddTitle("The Rancher");

            AddLine("Livestock: {0}/{1}", livestock.Length, m_Stone.MaxLivestock);

            AddButton((int)Button.AddLivestock, "Add Livestock");

            AddList(livestock, 8, delegate (int index)
            {
                AddLine(livestock[index].Name);

                AddGridButton(4, 2, (int)Button.Release + index, "Release");
                AddGridButton(4, 3, (int)Button.Claim + index, "Claim");
            });
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Stone.HasNPC(typeof(Mobiles.TSRancher)))
                return;

            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Ally))
                return;

            int index;

            switch ((Button)GetButtonID(info.ButtonID, out index))
            {
                case Button.AddLivestock:
                    {
                        BeginAddLivestock(m_Stone, from);

                        break;
                    }
                case Button.Release:
                    {
                        if (!(m_State is BaseCreature[]))
                            return;

                        BaseCreature[] mobs = (BaseCreature[])m_State;

                        if (index >= 0 && index < mobs.Length)
                        {
                            ReleaseLivestock(m_Stone, from, mobs[index]);

                            from.CloseGump(typeof(TSRancherGump));
                            from.SendGump(new TSRancherGump(m_Stone, from));
                        }

                        break;
                    }
                case Button.Claim:
                    {
                        if (!(m_State is BaseCreature[]))
                            return;

                        BaseCreature[] mobs = (BaseCreature[])m_State;

                        if (index >= 0 && index < mobs.Length)
                        {
                            ClaimLivestock(m_Stone, from, mobs[index]);

                            from.CloseGump(typeof(TSRancherGump));
                            from.SendGump(new TSRancherGump(m_Stone, from));
                        }

                        break;
                    }
            }
        }

        public static void BeginAddLivestock(Items.TownshipStone stone, Mobile from)
        {
            from.SendMessage("What animal do you wish to keep as livestock?");
            from.Target = new AddLivestockTarget(stone);
        }

        public class AddLivestockTarget : Target
        {
            private Items.TownshipStone m_Stone;

            public AddLivestockTarget(Items.TownshipStone stone)
                : base(-1, false, TargetFlags.None)
            {
                m_Stone = stone;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!m_Stone.HasNPC(typeof(Mobiles.TSRancher)))
                    return;

                if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Ally))
                    return;

                BaseCreature bc = targeted as BaseCreature;

                if (bc == null)
                    return;

                AddLivestock(m_Stone, from, bc);

                from.CloseGump(typeof(TSRancherGump));
                from.SendGump(new TSRancherGump(m_Stone, from));
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (!m_Stone.HasNPC(typeof(Mobiles.TSRancher)))
                    return;

                if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Ally))
                    return;

                from.CloseGump(typeof(TSRancherGump));
                from.SendGump(new TSRancherGump(m_Stone, from));
            }
        }

        public static void AddLivestock(Items.TownshipStone stone, Mobile from, BaseCreature bc)
        {
            if (!bc.Body.IsAnimal)
            {
                from.SendMessage("That is not an animal!");
            }
            else if (!ContainsType(m_LivestockTypes, bc.GetType()))
            {
                from.SendMessage("You cannot keep that type of animal as livestock.");
            }
            else if (bc.GetCreatureBool(CreatureBoolTable.IsTownshipLivestock))
            {
                if (stone.Livestock.ContainsKey(bc))
                    from.SendMessage("That is already your livestock!");
                else
                    from.SendMessage("That belongs to someone else!");
            }
            else if (bc.Guild != null || bc.IsInvulnerable)
            {
                from.SendMessage("You cannot keep that animal as livestock.");
            }
            else if (!stone.Contains(bc))
            {
                from.SendMessage("The animal must be driven into your township to keep it as livestock.");
            }
            else if (bc.Controlled ? (bc.ControlMaster != from) : (bc.GetHerder() != from))
            {
                from.SendMessage("The animal does not obey.");
            }
            else
            {
                from.SendMessage("You added {0} to your livestock.", bc.Name);
                stone.MakeLivestock(bc, from);
            }
        }

        public static void ReleaseLivestock(Items.TownshipStone stone, Mobile from, BaseCreature bc)
        {
            if (!stone.Livestock.ContainsKey(bc))
            {
                from.SendMessage("That is not your livestock!");
            }
            else
            {
                stone.ReleaseLivestock(bc);

                from.SendMessage("You release {0}.", bc.Name);
            }
        }

        public static void ClaimLivestock(Items.TownshipStone stone, Mobile from, BaseCreature bc)
        {
            if (!stone.Livestock.ContainsKey(bc))
            {
                from.SendMessage("That is not your livestock!");
            }
            else if (!stone.IsLivestockOwner(bc, from) && !CouldTame(from, bc))
            {
                from.SendMessage("It doesn't obey you.");
            }
            else if (from.FollowerCount + bc.ControlSlots > from.FollowersMax)
            {
                from.SendMessage("You have too many followers.");
            }
            else
            {
                stone.ReleaseLivestock(bc);

                bc.SetControlMaster(from);

                bc.ControlTarget = from;
                bc.ControlOrder = OrderType.Follow;

                from.SendMessage("You claim {0}.", bc.Name);
            }
        }

        private static bool CouldTame(Mobile from, BaseCreature bc)
        {
            return (bc.Tamable && from.Skills[SkillName.AnimalTaming].Value >= bc.MinTameSkill);
        }

        private static readonly Type[] m_LivestockTypes = new Type[]
            {
                typeof(Chicken),
                typeof(Goat),
                typeof(Pig),
                typeof(Sheep),
                typeof(Cow),

                typeof(Horse),
                typeof(RidableLlama),
                typeof(ForestOstard),
                typeof(DesertOstard),

                typeof(PackHorse),
                typeof(PackLlama),

                typeof(Cat),
                typeof(Dog),

                typeof(Destrier),
                typeof(BaseWarHorse),
            };

        private static bool ContainsType(Type[] types, Type type)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].IsAssignableFrom(type))
                    return true;
            }

            return false;
        }
    }
}