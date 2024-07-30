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

/* Scripts/Commands/Abstracted/Implementors/RegionCommandImplementor.cs
 * CHANGELOG
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Diagnostics;
using System;
using System.Collections;

namespace Server.Commands
{
    public class RegionCommandImplementor : BaseCommandImplementor
    {
        public RegionCommandImplementor()
        {
            Accessors = new string[] { "Region" };
            SupportRequirement = CommandSupport.Region;
            SupportsConditionals = true;
            AccessLevel = AccessLevel.Administrator;
            Usage = "Region <command> [condition]";
            Description = "Invokes the command on all appropriate mobiles in your current region. Optional condition arguments can further restrict the set of objects.";
        }

        public override void Compile(Mobile from, BaseCommand command, ref string[] args, ref object obj)
        {
            try
            {
                ObjectConditional cond = ObjectConditional.Parse(from, ref args);

                bool items, mobiles;

                if (!CheckObjectTypes(command, cond, out items, out mobiles))
                    return;

                Region reg = from.Region;

                ArrayList list = new ArrayList();

                if (mobiles && !items)
                {
                    foreach (Mobile mob in reg.Mobiles.Values)
                    {
                        if (cond.CheckCondition(mob))
                            list.Add(mob);
                    }
                }
                else
                {
                    foreach (Rectangle3D rect in reg.Coords)
                    {
                        Rectangle2D bounds = new Rectangle2D(rect.Start, rect.End);

                        IPooledEnumerable eable;

                        if (items && mobiles)
                            eable = reg.Map.GetObjectsInBounds(bounds);
                        else if (items)
                            eable = reg.Map.GetItemsInBounds(bounds);
                        else
                            eable = Map.NullEnumerable<IEntity>.Instance;

                        foreach (IEntity ent in eable)
                        {
                            if (list.Contains(ent))
                                continue;

                            if (mobiles && ent is Mobile && !BaseCommand.IsAccessible(from, ent))
                                continue;

                            if (cond.CheckCondition(ent))
                                list.Add(ent);
                        }

                        eable.Free();
                    }
                }

                obj = list;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                from.SendMessage(ex.Message);
            }
        }
    }
}