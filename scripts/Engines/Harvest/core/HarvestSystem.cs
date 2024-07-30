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

/* Engines/Harvest/Core/HarvestSystem.cs
 * CHANGELOG:
 *  11/24/21, Yoar
 *      Moved resource map stuff to Mining.
 *  11/10/21, Yoar
 *      Added resource maps.
 * 7/26/21, Pix
 *      Added ClearBankMemory to clear all the "banks" of veins in memory for this HarvestSystem.
 *	9/1/10, Adam
 *		Add OnHarvestFailed() for the simple case when there are resources, yet you came up empty.
 *		For fishing, this is when the tillerman will give you a bit of a pep-talk
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Engines.Harvest
{
    public abstract class HarvestSystem
    {
        private ArrayList m_Definitions;

        public ArrayList Definitions { get { return m_Definitions; } }

        public HarvestSystem()
        {
            m_Definitions = new ArrayList();
        }

        public virtual bool ClearBankMemory(Mobile admin)
        {
            try
            {
                int defCount = 0;
                int keyCount = 0;
                int bankCount = 0;
                int pointCount = 0;
                foreach (Engines.Harvest.HarvestDefinition def in Engines.Harvest.Mining.System.Definitions)
                {
                    defCount++;
                    foreach (var key in def.Banks.Keys)
                    {
                        keyCount++;
                        Hashtable banks = (Hashtable)def.Banks[key];
                        if (banks != null)
                        {
                            bankCount++;
                            pointCount += banks.Count;
                            banks.Clear();
                        }
                    }
                }
                admin.SendMessage("Cleared banks: {0}/{1}/{2}/{3}", defCount, keyCount, bankCount, pointCount);
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("ERROR in ClearBankMemory: " + ex.Message + "\n" + ex.StackTrace);
                admin.SendMessage("ERROR clearing banks: {0}", ex.Message);
            }
            return false;
        }

        public virtual bool CheckTool(Mobile from, Item tool)
        {
            bool wornOut = (tool == null || tool.Deleted || (tool is IUsesRemaining && ((IUsesRemaining)tool).UsesRemaining <= 0));

            if (wornOut)
                from.SendLocalizedMessage(1044038); // You have worn out your tool!

            return !wornOut;
        }

        public virtual bool CheckHarvest(Mobile from, Item tool)
        {
            return CheckTool(from, tool);
        }

        public virtual bool CheckHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            return CheckTool(from, tool);
        }

        public virtual bool CheckRange(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, bool timed)
        {
            bool inRange = (from.Map == map && from.InRange(loc, def.MaxRange));

            if (!inRange)
                def.SendMessageTo(from, timed ? def.TimedOutOfRangeMessage : def.OutOfRangeMessage);

            return inRange;
        }

        public virtual bool CheckResources(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, bool timed)
        {
            HarvestBank bank = def.GetBank(map, loc.X, loc.Y);
            bool available = (bank != null && bank.Current >= def.ConsumedPerHarvest);

            if (!available)
                def.SendMessageTo(from, timed ? def.DoubleHarvestMessage : def.NoResourcesMessage);

            return available;
        }

        public virtual void OnBadHarvestTarget(Mobile from, Item tool, object toHarvest)
        {
        }

        public virtual object GetLock(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            /* Here we prevent multiple harvesting.
			 * 
			 * Some options:
			 *  - 'return tool;' : This will allow the player to harvest more than once concurrently, but only if they use multiple tools. This seems to be as OSI.
			 *  - 'return GetType();' : This will disallow multiple harvesting of the same type. That is, we couldn't mine more than once concurrently, but we could be both mining and lumberjacking.
			 *  - 'return typeof( HarvestSystem );' : This will completely restrict concurrent harvesting.
			 */

            return tool;
        }

        public virtual void OnConcurrentHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
        }

        public virtual void OnHarvestStarted(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
        }

        public virtual bool BeginHarvesting(Mobile from, Item tool)
        {
            if (!CheckHarvest(from, tool))
                return false;

            from.Target = new HarvestTarget(tool, this);
            return true;
        }

        public virtual void FinishHarvesting(Mobile from, Item tool, HarvestDefinition def, object toHarvest, object locked)
        {
            from.EndAction(locked);

            if (!CheckHarvest(from, tool))
                return;

            int tileID;
            Map map;
            Point3D loc;

            if (!GetHarvestDetails(from, tool, toHarvest, out tileID, out map, out loc))
            {
                OnBadHarvestTarget(from, tool, toHarvest);
                return;
            }
            else if (!def.Validate(tileID))
            {
                OnBadHarvestTarget(from, tool, toHarvest);
                return;
            }

            if (!CheckRange(from, tool, def, map, loc, true))
                return;
            else if (!CheckResources(from, tool, def, map, loc, true))
                return;
            else if (!CheckHarvest(from, tool, def, toHarvest))
                return;

            if (SpecialHarvest(from, tool, def, map, loc))
                return;

            HarvestBank bank = def.GetBank(map, loc.X, loc.Y);

            if (bank == null)
                return;

            HarvestVein vein = bank.Vein;

            if (vein != null)
                vein = MutateVein(from, tool, def, bank, toHarvest, vein);

            if (vein == null)
                return;

            HarvestResource primary = vein.PrimaryResource;
            HarvestResource fallback = vein.FallbackResource;
            HarvestResource resource = MutateResource(from, tool, def, map, loc, vein, primary, fallback);

            double skillBase = from.Skills[def.Skill].Base;
            double skillValue = from.Skills[def.Skill].Value;

            Type type = null;

            if (skillBase >= resource.ReqSkill && from.CheckSkill(def.Skill, resource.MinSkill, resource.MaxSkill, contextObj: new object[2]))
            {
                type = GetResourceType(from, tool, def, map, loc, resource);

                if (type != null)
                    type = MutateType(type, from, tool, def, map, loc, resource);

                if (type != null)
                {
                    Item item = Construct(type, from, tool, def, map, loc, resource);

                    if (item == null)
                    {
                        type = null;
                    }
                    else
                    {
                        if (item.Stackable)
                        {
                            if (map == Map.Felucca && bank.Current >= def.ConsumedPerFeluccaHarvest)
                                item.Amount = def.ConsumedPerFeluccaHarvest;
                            else
                                item.Amount = def.ConsumedPerHarvest;
                        }

                        bank.Consume(item.Amount);

                        if (Give(from, item, def.PlaceAtFeetIfFull))
                        {
                            SendSuccessTo(from, item, resource);
                        }
                        else
                        {
                            SendPackFullTo(from, item, def, resource);
                            item.Delete();
                        }

                        if (tool is IUsesRemaining)
                        {
                            IUsesRemaining toolWithUses = (IUsesRemaining)tool;

                            toolWithUses.ShowUsesRemaining = true;

                            if (toolWithUses.UsesRemaining > 0)
                                --toolWithUses.UsesRemaining;

                            if (toolWithUses.UsesRemaining < 1)
                            {
                                tool.Delete();
                                def.SendMessageTo(from, def.ToolBrokeMessage);
                            }
                        }

                        if (tool != null)
                            tool.OnActionComplete(from, tool);
                    }
                }
            }

            if (type == null)
            {
                def.SendMessageTo(from, def.FailMessage);
                OnHarvestFailed(type, from, tool, def, map, loc, resource);
            }

            OnHarvestFinished(from, tool, def, vein, bank, resource, toHarvest);
        }

        public virtual void OnHarvestFinished(Mobile from, Item tool, HarvestDefinition def, HarvestVein vein, HarvestBank bank, HarvestResource resource, object harvested)
        {
        }

        public virtual void OnHarvestFailed(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
        }

        public virtual bool SpecialHarvest(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc)
        {
            return false;
        }

        public virtual Item Construct(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
            try { return Activator.CreateInstance(type) as Item; }
            catch { return null; }
        }

        public virtual HarvestVein MutateVein(Mobile from, Item tool, HarvestDefinition def, HarvestBank bank, object toHarvest, HarvestVein vein)
        {
            return vein;
        }

        public virtual void SendSuccessTo(Mobile from, Item item, HarvestResource resource)
        {
            resource.SendSuccessTo(from);
        }

        public virtual void SendPackFullTo(Mobile from, Item item, HarvestDefinition def, HarvestResource resource)
        {
            def.SendMessageTo(from, def.PackFullMessage);
        }

        public virtual bool Give(Mobile m, Item item, bool placeAtFeet)
        {
            if (m.PlaceInBackpack(item))
                return true;

            if (!placeAtFeet)
                return false;

            Map map = m.Map;

            if (map == null)
                return false;

            ArrayList atFeet = new ArrayList();

            IPooledEnumerable eable = m.GetItemsInRange(0);
            foreach (Item obj in eable)
                atFeet.Add(obj);
            eable.Free();

            for (int i = 0; i < atFeet.Count; ++i)
            {
                Item check = (Item)atFeet[i];

                if (check.StackWith(m, item, false))
                    return true;
            }

            item.MoveToWorld(m.Location, map);
            return true;
        }

        public virtual Type MutateType(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
            return from.Region.GetResource(type);
        }

        public virtual Type GetResourceType(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
            if (resource.Types.Length > 0)
                return resource.Types[Utility.Random(resource.Types.Length)];

            return null;
        }

        public virtual HarvestResource MutateResource(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestVein vein, HarvestResource primary, HarvestResource fallback)
        {
            if (vein.ChanceToFallback > Utility.RandomDouble())
                return fallback;

            double skillValue = from.Skills[def.Skill].Value;

            if (fallback != null && (skillValue < primary.ReqSkill || skillValue < primary.MinSkill))
                return fallback;

            return primary;
        }

        public virtual bool OnHarvesting(Mobile from, Item tool, HarvestDefinition def, object toHarvest, object locked, bool last)
        {
            if (!CheckHarvest(from, tool))
            {
                from.EndAction(locked);
                return false;
            }

            int tileID;
            Map map;
            Point3D loc;

            if (!GetHarvestDetails(from, tool, toHarvest, out tileID, out map, out loc))
            {
                from.EndAction(locked);
                OnBadHarvestTarget(from, tool, toHarvest);
                return false;
            }
            else if (!def.Validate(tileID))
            {
                from.EndAction(locked);
                OnBadHarvestTarget(from, tool, toHarvest);
                return false;
            }
            else if (!CheckRange(from, tool, def, map, loc, true))
            {
                from.EndAction(locked);
                return false;
            }
            else if (!CheckResources(from, tool, def, map, loc, true))
            {
                from.EndAction(locked);
                return false;
            }
            else if (!CheckHarvest(from, tool, def, toHarvest))
            {
                from.EndAction(locked);
                return false;
            }

            DoHarvestingEffect(from, tool, def, map, loc);

            new HarvestSoundTimer(from, tool, this, def, toHarvest, locked, last).Start();

            return !last;
        }

        public virtual void DoHarvestingSound(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            if (def.EffectSounds.Length > 0)
                from.PlaySound(Utility.RandomList(def.EffectSounds));
        }

        public virtual void DoHarvestingEffect(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc)
        {
            from.Direction = from.GetDirectionTo(loc);

            if (!from.Mounted)
                from.Animate(Utility.RandomList(def.EffectActions), 5, 1, true, false, 0);
        }

        public virtual HarvestDefinition GetDefinition(int tileID)
        {
            HarvestDefinition def = null;

            for (int i = 0; def == null && i < m_Definitions.Count; ++i)
            {
                HarvestDefinition check = (HarvestDefinition)m_Definitions[i];

                if (check.Validate(tileID))
                    def = check;
            }

            return def;
        }

        public virtual void StartHarvesting(Mobile from, Item tool, object toHarvest)
        {
            if (!CheckHarvest(from, tool))
                return;

            int tileID;
            Map map;
            Point3D loc;

            if (!GetHarvestDetails(from, tool, toHarvest, out tileID, out map, out loc))
            {
                OnBadHarvestTarget(from, tool, toHarvest);
                return;
            }

            HarvestDefinition def = GetDefinition(tileID);

            if (def == null)
            {
                OnBadHarvestTarget(from, tool, toHarvest);
                return;
            }

            if (!CheckRange(from, tool, def, map, loc, false))
                return;
            else if (!CheckResources(from, tool, def, map, loc, false))
                return;
            else if (!CheckHarvest(from, tool, def, toHarvest))
                return;

            object toLock = GetLock(from, tool, def, toHarvest);

            if (!from.BeginAction(toLock))
            {
                OnConcurrentHarvest(from, tool, def, toHarvest);
                return;
            }

            new HarvestTimer(from, tool, this, def, toHarvest, toLock).Start();
            OnHarvestStarted(from, tool, def, toHarvest);
        }

        public virtual bool GetHarvestDetails(Mobile from, Item tool, object toHarvest, out int tileID, out Map map, out Point3D loc)
        {
            if (toHarvest is Static && !((Static)toHarvest).Movable)
            {
                Static obj = (Static)toHarvest;

                tileID = (obj.ItemID & 0x3FFF) | 0x4000;
                map = obj.Map;
                loc = obj.GetWorldLocation();
            }
            else if (toHarvest is StaticTarget)
            {
                StaticTarget obj = (StaticTarget)toHarvest;

                tileID = (obj.ItemID & 0x3FFF) | 0x4000;
                map = from.Map;
                loc = obj.Location;
            }
            else if (toHarvest is LandTarget)
            {
                LandTarget obj = (LandTarget)toHarvest;

                tileID = obj.TileID & 0x3FFF;
                map = from.Map;
                loc = obj.Location;
            }
            else
            {
                tileID = 0;
                map = null;
                loc = Point3D.Zero;
                return false;
            }

            return (map != null && map != Map.Internal);
        }
    }
}

namespace Server
{
    public interface IChopable
    {
        void OnChop(Mobile from);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class FurnitureAttribute : Attribute
    {
        public static bool Check(Item item)
        {
            return (item != null && item.GetType().IsDefined(typeof(FurnitureAttribute), false));
        }

        public FurnitureAttribute()
        {
        }
    }
}