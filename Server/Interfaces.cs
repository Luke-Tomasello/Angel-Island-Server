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

/* Server\Interfaces.cs
 * CHANGELOG:
 *  11/14/22, Adam (IHasUsesRemaining)
 *      Similar to IUsesRemaining in the harvest system, IHasUsesRemaining consumes tool uses.
 *      The major difference between the two is that IHasUsesRemaining is dynamic in that it can be controlled
 *      at runtime based on for instance the shard. For example, Siege consumes fishing pole uses, Renaissance does not.
 */

using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    interface IHasUsesRemaining
    {
        int UsesRemaining { get; set; }                     // obvi
        bool WearsOut { get; }                              // for example Siege=true, Renaissance=false
        int ToolBrokeMessage { get; }                       // obvi
        void ConsumeUse(Mobile from);                       // who's using the tool
        void OnActionComplete(Mobile from, Item tool);      // Int override called by whatever system is using the tool
    }

    interface IPersistence
    {
        Item GetInstance();
    }
}
namespace Server.Mobiles

{
    public interface IMount
    {
        Mobile Rider { get; set; }
    }

    public interface IMountItem
    {
        IMount Mount { get; }
    }
}

namespace Server
{
    public interface IVendor
    {
        bool OnBuyItems(Mobile from, List<BuyItemResponse> list);
        bool OnSellItems(Mobile from, List<SellItemResponse> list);

        DateTime LastRestock { get; set; }
        TimeSpan RestockDelay { get; }
        void Restock();
    }

    public interface IPoint2D
    {
        int X { get; }
        int Y { get; }
    }

    public interface IPoint3D : IPoint2D
    {
        int Z { get; }
    }

    public interface ICarvable
    {
        void Carve(Mobile from, Item item);
    }

    public interface IWeapon
    {
        int MaxRange { get; }
        TimeSpan OnSwing(Mobile attacker, Mobile defender);
        void GetStatusDamage(Mobile from, out int min, out int max);
    }

    public interface IHued
    {
        int HuedItemID { get; }
    }

    public interface ISpell
    {
        bool IsCasting { get; }
        void OnCasterHurt();
        void OnCasterKilled();
        void OnConnectionChanged();
        bool OnCasterMoving(Direction d);
        bool OnCasterEquiping(Item item);
        bool OnCasterUsingObject(object o);
        bool OnCastInTown(Region r);
    }

    public interface IParty
    {
        void OnStamChanged(Mobile m);
        void OnManaChanged(Mobile m);
        void OnStatsQuery(Mobile beholder, Mobile beheld);
    }

    public interface ISpawner
    {
        bool UnlinkOnTaming { get; }
        Point3D HomeLocation { get; }
        int HomeRange { get; }

        void Remove(ISpawnable spawn);
    }

    public interface ISpawnable : IEntity
    {
        void OnBeforeSpawn(Point3D location, Map map);
        void MoveToWorld(Point3D location, Map map);
        void OnAfterSpawn();

        ISpawner Spawner { get; set; }
    }
}