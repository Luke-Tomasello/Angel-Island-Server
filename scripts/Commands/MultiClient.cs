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

/* scripts/commands/MultiClient.cs
 * 	CHANGELOG:
 *      11/7/07, Adam
 *          Add null checks to IsSameHWInfo()
 *		9/13/07, Adam
 *			Make IsSameHWInfo() a static/public so that we can use it elsewhere (ClientMon)
 *		6/16/07: Pix
 *			Added protection against NetStates which might not be in a good state (e.g. in the middle of logging in)
 *		6/1/06: Pix
 *			Fixed HWInfo comparison.
 *		5/25/06: Pix
 *			Initial Version.
 */

using Server.Diagnostics;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Commands
{

    public class MultiClientCommand : BaseCommand
    {

        public static void Initialize()
        {
            TargetCommands.Register(new MultiClientCommand());
        }

        public MultiClientCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Simple;
            Commands = new string[] { "MultiClient" };
            ObjectTypes = ObjectTypes.Mobiles;

            Usage = "MultiClient <target>";
            Description = "Lists possible multiclients of the target";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            PlayerMobile pm = obj as PlayerMobile;
            Mobile from = e.Mobile;

            try
            {
                if (pm != null)
                {
                    NetState ns = pm.NetState;
                    if (ns == null)
                    {
                        from.SendMessage("That player is no longer online.");
                        return;
                    }
                    Server.Accounting.Account pmAccount = (Server.Accounting.Account)pm.Account;
                    HardwareInfo pmHWInfo = pmAccount.HardwareInfo;

                    from.SendMessage("{0}/{1}: Finding possible multi-clients (IP: {2}, CV: {3})",
                        pmAccount.Username, pm.Name, ns.Address.ToString(), ns.Version.ToString());

                    List<NetState> netStates = NetState.Instances;

                    for (int i = 0; i < netStates.Count; i++)
                    {
                        NetState compState = netStates[i];

                        //guard against NetStates which haven't completely logged in yet
                        if (compState == null ||
                            compState.Address == null ||
                            compState.Mobile == null)
                        {
                            continue;
                        }

                        if (ns.Address.Equals(compState.Address))
                        {
                            if (compState.Mobile != pm)
                            {
                                Server.Accounting.Account compAcct = (Server.Accounting.Account)compState.Mobile.Account;
                                string clientName = string.Format("{0}/{1}", compAcct.Username, compState.Mobile.Name);

                                HardwareInfo compHWInfo = compAcct.HardwareInfo;

                                from.SendMessage("{0}: Same IP Address ({1})", clientName, compState.Address.ToString());

                                //Found another client from same IP, check client version
                                if (ns.Version.CompareTo(compState.Version) == 0)
                                {
                                    from.SendMessage("{0}: Same Client Version: {1}", clientName, compState.Version.ToString());
                                }
                                else
                                {
                                    from.SendMessage("{0}: Different Client Version: {1}", clientName, compState.Version.ToString());
                                }

                                //Check HWInfo
                                if (pmHWInfo == null && compHWInfo == null)
                                {
                                    from.SendMessage("{0}+{1}: BOTH Hardware UNKNOWN", pm.Name, clientName);
                                }
                                else if (pmHWInfo == null || (pmHWInfo.CpuClockSpeed == 0 && pmHWInfo.OSMajor == 0))
                                {
                                    from.SendMessage("{0}: Hardware UNKNOWN, {1} Known", pm.Name, clientName);
                                }
                                else if (compHWInfo == null || (compHWInfo.CpuClockSpeed == 0 && compHWInfo.OSMajor == 0))
                                {
                                    from.SendMessage("{0}: Hardware UNKNOWN, {1} Known", clientName, pm.Name);
                                }
                                else if (IsSameHWInfo(pmHWInfo, compHWInfo))
                                {
                                    from.SendMessage("{0}: Same Hardware", clientName);
                                }
                                else
                                {
                                    from.SendMessage("{0}: Different Hardware", clientName);
                                }
                            }
                        }
                    }

                }
                else
                {
                    AddResponse("Please target a player.");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                from.SendMessage("ERROR: Caught exception: " + ex.Message);
            }
        }

        public static bool IsSameHWInfo(HardwareInfo hw1, HardwareInfo hw2)
        {
            bool bSame = false;

            if (hw1 == null || hw2 == null)
                return bSame;

            if (hw1.CpuClockSpeed == hw2.CpuClockSpeed
                && hw1.CpuFamily == hw2.CpuFamily
                && hw1.CpuManufacturer == hw2.CpuManufacturer
                && hw1.CpuModel == hw2.CpuModel
                && hw1.CpuQuantity == hw2.CpuQuantity
                && hw1.DXMajor == hw2.DXMajor
                && hw1.DXMinor == hw2.DXMinor
                && hw1.OSMajor == hw2.OSMajor
                && hw1.OSMinor == hw2.OSMinor
                && hw1.OSRevision == hw2.OSRevision
                && hw1.PhysicalMemory == hw2.PhysicalMemory
                && hw1.VCDescription == hw2.VCDescription
                && hw1.VCMemory == hw2.VCMemory
                )
            {
                bSame = true;
            }

            return bSame;
        }
    }


}