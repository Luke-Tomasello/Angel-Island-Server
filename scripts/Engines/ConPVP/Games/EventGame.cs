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

/* Scripts\Engines\ConPVP\Arena.cs
 * Changelog:
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 */

using Server.Items;

namespace Server.Engines.ConPVP
{
    public abstract class EventController : Item
    {
        public abstract EventGame Construct(DuelContext dc);

        public abstract string Title { get; }

        public abstract string GetTeamName(int teamID);

        public virtual bool HasTeamNames { get { return true; } }

        public EventController()
            : base(0x1B7A)
        {
            Visible = false;
            Movable = false;
        }

        public EventController(Serial serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }
    }

    public enum EliminationReason
    {
        Defeated,
        LeftDuelingArena,
        Yielded
    }

    public abstract class EventGame
    {
        protected DuelContext m_Context;

        public DuelContext Context { get { return m_Context; } }

        public virtual bool FreeConsume { get { return true; } }

        public virtual bool LootableCorpses { get { return false; } }

        public EventGame(DuelContext context)
        {
            m_Context = context;
        }

        public virtual bool OnDeath(Mobile mob, Container corpse)
        {
            return true;
        }

        public virtual void OnEliminated(Mobile mob, EliminationReason reason)
        {
        }

        public virtual void OnBounced(Mobile mob)
        {
        }

        public virtual bool CantDoAnything(Mobile mob)
        {
            return false;
        }

        public virtual void OnStart()
        {
        }

        public virtual void OnStop()
        {
        }
    }
}