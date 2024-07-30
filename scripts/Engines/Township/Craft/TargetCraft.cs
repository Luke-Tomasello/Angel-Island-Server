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

/* Engines/Township/Craft/TownshipCraft.cs
 * CHANGELOG:
 *  8/15/23, Yoar
 *	    Initial version.
 */

using Server.Engines.Craft;
using Server.Items;
using Server.Targeting;
using System;

namespace Server.Township
{
    public abstract class TargetCraft : CustomCraft
    {
        public virtual int Range { get { return -1; } }
        public virtual bool AllowGround { get { return true; } }

        private object m_Targeted;

        public TargetCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
        }

        public override void EndCraftAction()
        {
            OnBeginTarget();

            From.Target = new InternalTarget(this);
        }

        public override Item CompleteCraft(out TextDefinition message)
        {
            if (m_Targeted != null && ValidateTarget(m_Targeted, out message))
                OnCraft(m_Targeted, out message);

            message = 0;
            return null;
        }

        private class InternalTarget : Target
        {
            private TargetCraft m_Craft;

            public InternalTarget(TargetCraft craft)
                : base(craft.Range, craft.AllowGround, TargetFlags.None)
            {
                m_Craft = craft;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                TextDefinition message;

                if (m_Craft.ValidateTarget(targeted, out message))
                {
                    m_Craft.m_Targeted = targeted;
                    m_Craft.CraftItem.CompleteCraft(m_Craft.Quality, false, m_Craft.From, m_Craft.CraftSystem, m_Craft.TypeRes, m_Craft.Tool, m_Craft);
                }
                else
                {
                    m_Craft.Failure(message);
                }
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (cancelType == TargetCancelType.Canceled)
                    m_Craft.Failure(null);
            }
        }

        protected virtual void OnBeginTarget()
        {
            From.SendMessage("Where do you wish to build this?");
        }

        protected abstract bool ValidateTarget(object targeted, out TextDefinition message);

        protected abstract void OnCraft(object targeted, out TextDefinition message);

        protected void Failure(TextDefinition message)
        {
            if (Tool != null && !Tool.Deleted && Tool.UsesRemaining > 0)
                From.SendGump(new CraftGump(From, CraftSystem, Tool, message));
            else
                TextDefinition.SendMessageTo(From, message);
        }
    }
}