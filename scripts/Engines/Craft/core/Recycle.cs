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

/* Engines/Crafting/Core/Recycle.cs
 * CHANGELOG:
 *	12/16/21, Yoar
 *	    Initial version.
 */

using Server.Items;
using Server.Targeting;
using System;

namespace Server.Engines.Craft
{
    public enum RecycleResult : byte
    {
        Success,
        Invalid,
        NoSkill
    }

    public abstract class Recycle
    {
        public static Recycle Metal { get { return DefMetal.Instance; } }
        public static Recycle Lumber { get { return DefLumber.Instance; } }

        protected abstract CraftResourceType ResourceType { get; }
        protected abstract SkillName HarvestSkill { get; }
        protected abstract TextEntry SuccessMessage { get; }
        protected abstract TextEntry InvalidMessage { get; }
        protected abstract TextEntry NoSkillMessage { get; }
        protected abstract TextEntry StoreBoughtMessage { get; }

        protected Recycle()
        {
        }

        public void Do(Mobile from, CraftSystem craftSystem, BaseTool tool)
        {
            TextDefinition badCraft = craftSystem.CanCraft(from, tool, null);

            if (!TextDefinition.IsNullOrEmpty(badCraft))
            {
                from.SendGump(new CraftGump(from, craftSystem, tool, badCraft));
            }
            else
            {
                from.Target = new InternalTarget(this, craftSystem, tool);
                from.SendLocalizedMessage(1044273); // Target an item to recycle.
            }
        }

        private class InternalTarget : Target
        {
            private Recycle m_Recycle;
            private CraftSystem m_CraftSystem;
            private BaseTool m_Tool;

            public InternalTarget(Recycle recycle, CraftSystem craftSystem, BaseTool tool)
                : base(2, false, TargetFlags.None)
            {
                m_Recycle = recycle;
                m_CraftSystem = craftSystem;
                m_Tool = tool;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                m_Recycle.ProcessTarget(from, m_CraftSystem, m_Tool, targeted);
            }
        }

        public void ProcessTarget(Mobile from, CraftSystem craftSystem, BaseTool tool, object targeted)
        {
            TextDefinition badCraft = craftSystem.CanCraft(from, tool, null);

            if (!TextDefinition.IsNullOrEmpty(badCraft))
            {
                from.SendGump(new CraftGump(from, craftSystem, tool, badCraft));
            }
            else
            {
                RecycleResult result = RecycleResult.Invalid;
                bool playerCrafted = false;

                if (targeted is BaseArmor)
                    result = ProcessRecycle(from, craftSystem, (BaseArmor)targeted, ((BaseArmor)targeted).Resource, playerCrafted = ((BaseArmor)targeted).PlayerCrafted);
                else if (targeted is BaseWeapon)
                    result = ProcessRecycle(from, craftSystem, (BaseWeapon)targeted, ((BaseWeapon)targeted).Resource, playerCrafted = ((BaseWeapon)targeted).PlayerCrafted);
                else if (targeted is DragonBardingDeed)
                    result = ProcessRecycle(from, craftSystem, (DragonBardingDeed)targeted, ((DragonBardingDeed)targeted).Resource, playerCrafted = true);
                else if (targeted is BaseInstrument)
                    result = ProcessRecycle(from, craftSystem, (BaseInstrument)targeted, ((BaseInstrument)targeted).Resource, playerCrafted = ((BaseInstrument)targeted).PlayerCrafted);
                else if (targeted is BaseCraftableItem)
                    result = ProcessRecycle(from, craftSystem, (BaseCraftableItem)targeted, ((BaseCraftableItem)targeted).Resource, playerCrafted = ((BaseCraftableItem)targeted).PlayerCrafted);
                else if (targeted is BaseOtherEquipable)
                    result = ProcessRecycle(from, craftSystem, (BaseOtherEquipable)targeted, ((BaseOtherEquipable)targeted).Resource, playerCrafted = ((BaseOtherEquipable)targeted).PlayerCrafted);

                TextEntry message;

                switch (result)
                {
                    default:
                    case RecycleResult.Invalid: message = InvalidMessage; break;
                    case RecycleResult.NoSkill: message = NoSkillMessage; break;
                    case RecycleResult.Success: message = playerCrafted ? SuccessMessage : StoreBoughtMessage; break;
                }

                if (message.String != null)
                    from.SendGump(new CraftGump(from, craftSystem, tool, message.String));
                else
                    from.SendGump(new CraftGump(from, craftSystem, tool, message.Number));
            }
        }

        public RecycleResult ProcessRecycle(Mobile from, CraftSystem craftSystem, Item item, CraftResource resource, bool playerCrafted)
        {
            try
            {
                if (CraftResources.GetType(resource) != ResourceType)
                    return RecycleResult.Invalid;

                CraftResourceInfo info = CraftResources.GetInfo(resource);

                if (info == null || info.ResourceTypes.Length == 0)
                    return RecycleResult.Invalid;

                CraftItem craftItem = craftSystem.CraftItems.SearchFor(item.GetType());

                if (craftItem == null || craftItem.Resources.Count == 0)
                    return RecycleResult.Invalid;

                CraftRes craftResource = craftItem.Resources.GetAt(0);

                if (craftResource.Amount < 2)
                    return RecycleResult.Invalid;

                double difficulty = GetDifficulty(resource);

                if (difficulty > from.Skills[HarvestSkill].Value && difficulty > from.Skills[craftSystem.MainSkill].Value)
                    return RecycleResult.NoSkill;

                Type resourceType = info.ResourceTypes[0];
                Item recycled = (Item)Activator.CreateInstance(resourceType);

                if (playerCrafted)
                    recycled.Amount = craftResource.Amount / 2;
                else
                    recycled.Amount = 1;

                item.Delete();
                from.AddToBackpack(recycled);

                PlayRecycleEffect(from, craftSystem);

                return RecycleResult.Success;
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            return RecycleResult.Invalid;
        }

        public virtual double GetDifficulty(CraftResource resource)
        {
            return 0.0;
        }

        public virtual void PlayRecycleEffect(Mobile from, CraftSystem craftSystem)
        {
            craftSystem.PlayCraftEffect(from, obj: null);
        }

        private class DefMetal : Recycle
        {
            private static Recycle m_Instance;

            public static Recycle Instance
            {
                get
                {
                    if (m_Instance == null)
                        m_Instance = new DefMetal();

                    return m_Instance;
                }
            }

            protected override CraftResourceType ResourceType { get { return CraftResourceType.Metal; } }
            protected override SkillName HarvestSkill { get { return SkillName.Mining; } }
            protected override TextEntry SuccessMessage { get { return 1044270; } } // You melt the item down into ingots.
            protected override TextEntry InvalidMessage { get { return 1044272; } } // You can't melt that down into ingots.
            protected override TextEntry NoSkillMessage { get { return 1044269; } } // You have no idea how to work this metal.
            protected override TextEntry StoreBoughtMessage { get { return 500418; } } // This item is of imported material, probably bought at a store, and does not yield much metal.

            private DefMetal()
                : base()
            {
            }

            public override double GetDifficulty(CraftResource resource)
            {
                switch (resource)
                {
                    case CraftResource.DullCopper: return 65.0;
                    case CraftResource.ShadowIron: return 70.0;
                    case CraftResource.Copper: return 75.0;
                    case CraftResource.Bronze: return 80.0;
                    case CraftResource.Gold: return 85.0;
                    case CraftResource.Agapite: return 90.0;
                    case CraftResource.Verite: return 95.0;
                    case CraftResource.Valorite: return 99.0;
                }

                return 0.0;
            }

            public override void PlayRecycleEffect(Mobile from, CraftSystem craftSystem)
            {
                from.PlaySound(0x2A);
                from.PlaySound(0x240);
            }
        }

        private class DefLumber : Recycle
        {
            private static Recycle m_Instance;

            public static Recycle Instance
            {
                get
                {
                    if (m_Instance == null)
                        m_Instance = new DefLumber();

                    return m_Instance;
                }
            }

            protected override CraftResourceType ResourceType { get { return CraftResourceType.Wood; } }
            protected override SkillName HarvestSkill { get { return SkillName.Lumberjacking; } }
            protected override TextEntry SuccessMessage { get { return "You recycle the item into boards."; } }
            protected override TextEntry InvalidMessage { get { return "You can't recycle that into boards."; } }
            protected override TextEntry NoSkillMessage { get { return 1072652; } } // You cannot work this strange and unusual wood.
            protected override TextEntry StoreBoughtMessage { get { return "This item is of imported material, probably bought at a store, and does not yield much wood."; } }

            private DefLumber()
                : base()
            {
            }

            public override double GetDifficulty(CraftResource resource)
            {
                switch (resource)
                {
                    case CraftResource.OakWood: return 65.0;
                    case CraftResource.AshWood: return 75.0;
                    case CraftResource.YewWood: return 80.0;
                    case CraftResource.Heartwood: return 90.0;
                    case CraftResource.Bloodwood: return 95.0;
                    case CraftResource.Frostwood: return 99.0;
                }

                return 0.0;
            }
        }
    }
}