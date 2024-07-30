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

/* Changelog
 *  2/5/2024, Adam
 *      Spawner property TemplateGet removed (unreliable)
 *      New commands:
 *      [GetTemplateMob <target spawner>
 *      [GetTemplateItem <target spawner>
 *      Both functions bring the template to your location
 * 10/13/23, Yoar
 *  Reworked SpawnerDefrag command
 *  Added SpawnerClear, SpawnerActivate, SpawnerDeactivate commands
 * 8/23/22, Adam (SpawnerTarget)
 *  Add missing "else" to "else if" construct
 * 1/07/05, Darva
 *  First checkin.
 */

using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    public static class SpawnerCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("Spawner", AccessLevel.GameMaster, new CommandEventHandler(Spawner_OnCommand));
            TargetCommands.Register(new SpawnerDefragCommand());
            TargetCommands.Register(new SpawnerClearCommand());
            TargetCommands.Register(new SpawnerActivateCommand());
            TargetCommands.Register(new SpawnerDeactivateCommand());

            TargetCommands.Register(new SpawnerGetTemplateMobCommand());
            TargetCommands.Register(new SpawnerGetTemplateItemCommand());
        }

        [Usage("Spawner")]
        [Description("Moves you to the spawner of the targeted creature, if any.")]
        private static void Spawner_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new SpawnerTarget();
        }

        private class SpawnerTarget : Target
        {
            public SpawnerTarget()
                : base(-1, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is BaseCreature bc)
                {
                    if (bc.Spawner != null)
                    {
                        from.MoveToWorld(bc.Spawner.Location, bc.Spawner.Map);
                    }
                    else if (bc.ChampEngine != null)
                    {
                        from.MoveToWorld(bc.ChampEngine.Location, bc.ChampEngine.Map);
                    }
                    else if (bc.SpawnerTempMob)
                    {
                        if (from is PlayerMobile pm)
                        {
                            pm.JumpIndex = 0;
                            pm.JumpList = new System.Collections.ArrayList();
                            foreach (Item item in World.Items.Values)
                                if (item is Spawner spawner && spawner.TemplateMobile != null && spawner.TemplateMobile.Serial == bc.Serial)
                                    pm.JumpList.Add(item);
                            from.SendMessage("Your jump list has been loaded with {0} entries", pm.JumpList.Count);
                        }
                    }
                    else
                    {
                        from.SendMessage("That mobile is homeless");
                    }
                }
                else if (o is Item)
                {
                    if (((Item)o).Spawner != null)
                    {
                        from.MoveToWorld((o as Item).Spawner.Location, (o as Item).Spawner.Map);
                    }
                    else
                    {
                        from.SendMessage("That item is not from a spawner");
                    }
                }
                else
                {
                    from.SendMessage("Why would that have a spawner?");
                }
            }
        }

        private class SpawnerGetTemplateMobCommand : BaseCommand
        {
            public SpawnerGetTemplateMobCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllItems;
                Commands = new string[] { "GetTemplateMob" };
                ObjectTypes = ObjectTypes.Items;
                Usage = "GetTemplateMob";
                Description = "Get the template mobile for the targeted spawner.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is Spawner spawner)
                {
                    if (spawner.TemplateEnabled)
                    {
                        if (spawner.TemplateMobile != null)
                        {
                            if (spawner.TemplateMobile.Deleted == false)
                            {
                                spawner.TemplateMobile.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
                            }
                            else
                                LogFailure("That spawner has a template mobile that has been deleted.");
                        }
                        else
                            LogFailure("That spawner has no template mobile.");
                    }
                    else
                        LogFailure("That spawner has no template mobile.");
                }
                else
                    LogFailure("That is not a spawner.");
            }
        }
        private class SpawnerGetTemplateItemCommand : BaseCommand
        {
            public SpawnerGetTemplateItemCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllItems;
                Commands = new string[] { "GetTemplateItem" };
                ObjectTypes = ObjectTypes.Items;
                Usage = "GetTemplateItem";
                Description = "Get the template item for the targeted spawner.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is Spawner spawner)
                {
                    if (spawner.TemplateEnabled)
                    {
                        if (spawner.TemplateItem != null)
                        {
                            if (spawner.TemplateItem.Deleted == false)
                            {
                                spawner.TemplateItem.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
                            }
                            else
                                LogFailure("That spawner has a template item that has been deleted.");
                        }
                        else
                            LogFailure("That spawner has no template item.");
                    }
                    else
                        LogFailure("That spawner has no template item.");
                }
                else
                    LogFailure("That is not a spawner.");
            }
        }
        private class SpawnerDefragCommand : BaseCommand
        {
            public SpawnerDefragCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllItems;
                Commands = new string[] { "SpawnerDefrag" };
                ObjectTypes = ObjectTypes.Items;
                Usage = "SpawnerDefrag";
                Description = "Defrags the targeted spawner.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is Spawner)
                    ((Spawner)obj).Defrag();
                else
                    LogFailure("That is not a spawner.");
            }
        }

        private class SpawnerClearCommand : BaseCommand
        {
            public SpawnerClearCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllItems;
                Commands = new string[] { "SpawnerClear" };
                ObjectTypes = ObjectTypes.Items;
                Usage = "SpawnerClear";
                Description = "Clears all spawned objects of the targeted spawner.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is Spawner)
                    ((Spawner)obj).RemoveObjects();
                else
                    LogFailure("That is not a spawner.");
            }
        }

        private class SpawnerActivateCommand : BaseCommand
        {
            public SpawnerActivateCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllItems;
                Commands = new string[] { "SpawnerActivate" };
                ObjectTypes = ObjectTypes.Items;
                Usage = "SpawnerActivate <respawn>";
                Description = "Activates the targeted spawner.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is Spawner)
                {
                    Spawner sp = (Spawner)obj;

                    if (Spawner.ShouldShardEnable(sp.Shard))
                    {
                        sp.Running = true;

                        if (e.GetBoolean(0))
                            sp.Respawn();
                    }
                }
                else
                {
                    LogFailure("That is not a spawner.");
                }
            }
        }

        private class SpawnerDeactivateCommand : BaseCommand
        {
            public SpawnerDeactivateCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllItems;
                Commands = new string[] { "SpawnerDeactivate" };
                ObjectTypes = ObjectTypes.Items;
                Usage = "SpawnerDeactivate <clear>";
                Description = "Deactivates the targeted spawner.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is Spawner)
                {
                    Spawner sp = (Spawner)obj;

                    if (e.GetBoolean(0))
                        sp.RemoveObjects();

                    sp.Running = false;
                }
                else
                {
                    LogFailure("That is not a spawner.");
                }
            }
        }
    }
}