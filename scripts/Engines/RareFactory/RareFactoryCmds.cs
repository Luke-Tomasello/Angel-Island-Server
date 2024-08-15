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

/* Scripts/Commands/RareFactoryCmd.cs
 * ChangeLog:
 *  6/12/07, Adam
 *      The system uses static indexes to address the gump pages. This system crashes when the indexes
 *      no longer reflect the actual number of elements in the arrays. While I address that issue, I will also 
 *      wrap these commands in try/catches.
 *  6/1/07, Adam
 *      - Add [AddRare command that supports 'area' capture to generate bulk rares.
 *          The [AddRare command assumes: no expiration date, and that the rare name is the item.Name or Type.Name
 *      - Move [AcquireRare command to this file
 *	12/Mar/2007, weaver
 *		Initial creation.
 * 
 */


using Server.Diagnostics;
using Server.Engines;
using Server.Gumps;
using System;
using System.Text.RegularExpressions;
namespace Server.Commands
{

    public class RareFactoryCmds
    {
        //public static void Initialize()
        //{
        //    Server.CommandSystem.Register("RareFactory", AccessLevel.Administrator, new CommandEventHandler(RareFactory_OnCommand));
        //    Server.CommandSystem.Register("AcquireRare", AccessLevel.Administrator, new CommandEventHandler(AcquireRare_OnCommand));
        //    Server.CommandSystem.Register("PatchDODInst", AccessLevel.Administrator, new CommandEventHandler(PatchDODInst_OnCommand));
        //    Server.CommandSystem.Register("DumpRares", AccessLevel.Administrator, new CommandEventHandler(DumpRares_OnCommand));
        //    Server.Commands.TargetCommands.Register(new AddRareCommand());
        //}

        [Usage("DumpRares")]
        [Description("Dumps all rare templates")]
        public static void DumpRares_OnCommand(CommandEventArgs e)
        {
            LogHelper Logger = new LogHelper("RaresDump.log", false);
            try
            {
                foreach (DODGroup dg in RareFactory.DODGroup)
                {
                    if (dg is DODGroup)
                    {
                        foreach (DODInstance di in dg.DODInst)
                        {
                            if (di is DODInstance)
                            {
                                if (di.RareTemplate == null)
                                    Logger.Log(LogType.Text, string.Format("DODInstance {0} has a null RareTemplate.", di.Name));
                                else
                                    Logger.Log(LogType.Item, di.RareTemplate, string.Format("DODInstance {0}: ({1}).", di.Name, di.RareTemplate.GetType().ToString()));

                            }
                        }
                    }
                }

                Logger.Log(LogType.Text, "------------- RareFactory.DODInst -------------");

                foreach (DODInstance di in RareFactory.DODInst)
                {
                    if (di is DODInstance)
                    {
                        if (di.RareTemplate == null)
                            Logger.Log(LogType.Text, string.Format("DODInstance {0} has a null RareTemplate.", di.Name));
                        else
                            Logger.Log(LogType.Item, di.RareTemplate, string.Format("DODInstance {0}: ({1}).", di.Name, di.RareTemplate.GetType().ToString()));
                    }
                }

                e.Mobile.SendMessage("Done.");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                e.Mobile.SendMessage(ex.Message);
            }
            finally
            {
                Logger.Finish();
            }
        }

        [Usage("PatchDODInst SerialNumber")]
        [Description("Forces and null RareTemplates to SerialNumber")]
        public static void PatchDODInst_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Length != 1)
                {
                    e.Mobile.SendMessage("Incorrect number of arguments. Format : [PatchDODInst SerialNumber");
                    return;
                }

                int groupList = 0;
                int instList = 0;
                foreach (DODGroup dg in RareFactory.DODGroup)
                {
                    if (dg is DODGroup)
                    {
                        foreach (DODInstance di in dg.DODInst)
                        {
                            if (di is DODInstance)
                            {
                                if (di.RareTemplate == null)
                                {
                                    di.RareTemplate = World.FindItem((Serial)int.Parse(e.GetString(0)));
                                    groupList++;
                                }
                            }
                        }
                    }
                }

                foreach (DODInstance di in RareFactory.DODInst)
                {
                    if (di is DODInstance)
                    {
                        if (di.RareTemplate == null)
                        {
                            di.RareTemplate = World.FindItem((Serial)int.Parse(e.GetString(0)));
                            instList++;
                        }
                    }
                }

                e.Mobile.SendMessage("Done. {0} replacements in Grouplist, {1} replacements in Instlist", groupList, instList);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                e.Mobile.SendMessage(ex.Message);
            }
        }

        [Usage("RareFactory")]
        [Description("Configures the Rare Factory engine")]
        public static void RareFactory_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (!RareFactory.InUse)
                {
                    e.Mobile.SendGump(new RFGroupGump(0));
                    e.Mobile.SendGump(new RFControlGump());
                    e.Mobile.SendGump(new RFViewGump());
                }
                else
                {
                    e.Mobile.SendMessage("Rare Factory is currently being configured by another administrator! Please wait. ");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                e.Mobile.SendMessage(ex.Message);
            }
        }

        [Usage("acquirerare (<rare level>) <rare group name>")]
        [Description("Calls on RareFactory to produce a random rare from the specified group of rares")]
        public static void AcquireRare_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Length != 1 && e.Length != 2)
                {
                    e.Mobile.SendMessage("Incorrect number of arguments. Format : [acquirerare (<rare level>) <rare group name>");
                    return;
                }

                Regex ValidRarityPatt = new Regex("^[0-9]$");

                if (!ValidRarityPatt.IsMatch(e.GetString(0)))
                {
                    // Invalid 
                    e.Mobile.SendMessage("You may only enter an number to represent a rarity level. Format : [acquirerare (<rare level>) <rare group name>");
                    return;
                }

                int iRarity = e.GetInt32(0);

                // Check rarity level is ok
                if (iRarity < 0 || iRarity > 10)
                {
                    // Invalid
                    e.Mobile.SendMessage("Item rarity is scaled from 0 (most common) to 10 (most rare).");
                    return;
                }

                e.Mobile.Backpack.AddItem(
                    (e.Length == 1
                        ?
                        RareFactory.AcquireRare((short)iRarity)                     // only have rarity
                        :
                        RareFactory.AcquireRare((short)iRarity, e.GetString(1)) // rarity + group
                    ));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                e.Mobile.SendMessage(ex.Message);
            }
        }

        public class AddRareCommand : BaseCommand
        {
            public AddRareCommand()
            {
                AccessLevel = AccessLevel.Administrator;
                Supports = CommandSupport.AllItems;
                Commands = new string[] { "AddRare" };
                ObjectTypes = ObjectTypes.Items;

                Usage = "AddRare sGroup iRarity iStartIndex iLastIndex";
                Description = "Calls the Rare Factory engine to create a rare";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                try
                {
                    Item item = obj as Item;

                    if (!RareFactory.InUse)
                    {
                        if (e.Arguments.Length == 4)
                        {
                            int iRarity = 0;
                            if (int.TryParse(e.Arguments[1], out iRarity) == true && iRarity >= 0 && iRarity <= 10)
                            {
                                DODGroup match;
                                if ((match = FindGroup(e.Arguments[0], iRarity)) != null)
                                {
                                    int iStartIndex = 0;
                                    if (int.TryParse(e.Arguments[2], out iStartIndex) == true && iStartIndex > 0 && iStartIndex <= 255)
                                    {
                                        int iLastIndex = 0;
                                        if (int.TryParse(e.Arguments[3], out iLastIndex) == true && iLastIndex > 0 && iLastIndex <= 255)
                                        {
                                            if (item != null)
                                            {
                                                LogHelper Logger = null;
                                                try
                                                {
                                                    DODInstance di = RareFactory.AddRare(match, item);    // rarity is defined by the group
                                                    di.LastIndex = (short)iLastIndex;
                                                    di.StartIndex = (short)iStartIndex;
                                                    di.StartDate = DateTime.MinValue;   // valid now
                                                    di.EndDate = DateTime.MaxValue;     // valid forever

                                                    // default the name to the name of the item
                                                    if (item.Name != null && item.Name != "")
                                                        di.Name = item.Name;
                                                    else
                                                        di.Name = item.GetType().Name;

                                                    AddResponse("Sucessfully defined new rare '" + di.Name + "'!");
                                                }
                                                catch (Exception ex)
                                                {
                                                    LogHelper.LogException(ex);
                                                    e.Mobile.SendMessage(ex.Message);
                                                }
                                                finally
                                                {
                                                    if (Logger != null)
                                                        Logger.Finish();
                                                }
                                            }
                                            else
                                            {
                                                LogFailure("Only an item may be converted into a rare.");
                                            }
                                        }
                                        else
                                        {
                                            LogFailure("The LastIndex must be a numeric value between 1 and 255 inclusive.");
                                        }
                                    }
                                    else
                                    {
                                        LogFailure("The StartIndex must be a numeric value between 1 and 255 inclusive.");
                                    }
                                }
                                else
                                {
                                    LogFailure(string.Format("Could not find the group \"{0}\" with a rarity of {1}", e.Arguments[0], iRarity));
                                }
                            }
                            else
                            {
                                LogFailure("The rarity must be a numeric value between 0 and 10 inclusive.");
                            }
                        }
                        else
                        {
                            LogFailure("AddRare sGroup iRarity iStartIndex iLastIndex");
                        }
                    }
                    else
                    {
                        LogFailure("Rare Factory is currently being configured by another administrator! Please wait. ");
                    }
                }
                catch (Exception exe)
                {
                    LogHelper.LogException(exe);
                    e.Mobile.SendMessage(exe.Message);
                }
            }

            DODGroup FindGroup(string group, int iRarity)
            {
                // first find the group
                DODGroup match = null;
                try
                {
                    for (int ix = 0; ix < RareFactory.DODGroup.Count; ix++)
                    {
                        DODGroup dg = RareFactory.DODGroup[ix] as DODGroup;
                        if (dg.Name.ToLower() == group.ToLower() && dg.Rarity == (short)iRarity)
                        {
                            match = dg;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    Console.WriteLine(ex.Message);
                }

                return match;
            }
        }
    }
}