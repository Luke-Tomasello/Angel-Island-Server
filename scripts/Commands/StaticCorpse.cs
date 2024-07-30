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

/* Scripts\Commands\StaticCorpse.cs
 * CHANGELOG
 * 3/22/2024, Adam: 
 *  Created
 */

using Server.Items;
using System;

namespace Server.Commands
{
    public class StaticCorpse
    {
        public static void Initialize()
        {
            TargetCommands.Register(new StaticCorpseCommand());
        }
        private class StaticCorpseCommand : BaseCommand
        {
            public StaticCorpseCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "StaticCorpse" };
                ObjectTypes = ObjectTypes.All;
                Usage = "StaticCorpse <mobile type> [Direction]";
                Description = "build a static corpse.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                IPoint3D center = obj as IPoint3D;
                if (center != null)
                {
                    try
                    {
                        Direction direction = Direction.North;
                        Type typeFound = ScriptCompiler.FindTypeByName(e.GetString(0));
                        if (typeFound != null)
                        {
                            try
                            {
                                direction = (Direction)Enum.Parse(typeof(Direction), e.GetString(1), ignoreCase: true);
                            }
                            catch { }

                            if (obj is Item item)
                            {
                                center = item.GetWorldLocation();
                            }

                            object o = Activator.CreateInstance(typeFound);
                            if (o is Mobile m)
                            {
                                Body body = m.Body;
                                Static corpse = new Static(0x2006);

                                corpse.Stackable = true;    // To suppress console warnings, stackable must be true
                                corpse.Amount = body;       // protocol defines that for itemid 0x2006, amount=body
                                corpse.Stackable = false;
                                corpse.Movable = false;
                                corpse.Name = Corpse.GetCorpseName(m);
                                corpse.MoveToWorld(new Point3D(center), e.Mobile.Map);
                                m.Delete();

                                if (!e.Mobile.CanSee(corpse))
                                {
                                    LogFailure(string.Format("You cannot see {0}", e.GetString(0)));
                                    corpse.Delete();
                                }
                            }
                            else
                                LogFailure(string.Format("{0} is not a mobile", e.GetString(0)));
                        }
                        else
                            LogFailure(string.Format("{0} not found", e.GetString(0)));
                    }
                    catch
                    {

                    }
                }
                else
                    LogFailure("Cannot create a corpse there");
            }
        }
    }
}