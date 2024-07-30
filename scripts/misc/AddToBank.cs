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

/* Scripts/Misc/AddToBank.cs
 * ChangeLog:
 *	2/7/08, Adam
 *		Add option to copy properties. Default: True
 *  12/20/07, Adam
 *      Add "Performing routine maintenance" message to explain the lag
 *  07/24/06, Rhiannon
 *		Added functionality for new access levels.
 *  12/13/05, Kit
 *		Fixed bug with only checking account character slot 0.
 *	12/18/05, Adam
 *		first time checkin, add header
 */

using Server.Accounting;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Commands
{
    /// <summary>
    /// David M. O'Hara
    /// 08-11-04
    /// Version 2.1
    /// Gives item (targeted or given type) into bank box. Distribution can be 1 per account, 1 per character, or
    /// based on AccessLevel (good for staff items).
    /// </summary>

    public class AddToBank
    {
        public static void Initialize()
        {
            // alter AccessLevel to be AccessLevel.Admin if you only want admins to use.
            Server.CommandSystem.Register("AddToBank", AccessLevel.Administrator, new CommandEventHandler(AddToBank_OnCommand));
        }

        private static void AddToBank_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendGump(new AddToBankGump());
        }

        private static void PlaceItemIn(Container parent, int x, int y, Item item)
        {
            parent.AddItem(item);
            item.Location = new Point3D(x, y, 0);
        }

        #region " Targeting/Dupe System "

        public class DupeTarget : Target
        {
            private bool m_InBag;
            private int m_Amount;
            private int m_GiveRule;
            private int m_Access;
            private bool m_CopyProperties;

            public DupeTarget(bool inbag, int amount, bool copyProperties, int give, int access)
                : base(15, false, TargetFlags.None)
            {
                m_InBag = inbag;
                m_Amount = amount;
                m_GiveRule = give;
                m_Access = access;
                m_CopyProperties = copyProperties;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (!(targ is Item))
                {
                    from.SendMessage("You can only dupe items.");
                    return;
                }

                Server.World.Broadcast(0x35, true, "Performing routine maintenance, please wait.");
                Console.WriteLine("AddToBank: working...");
                DateTime startTime = DateTime.UtcNow;

                from.SendMessage("Placing {0} into bank boxes...", ((Item)targ).Name == null ? "an item" : ((Item)targ).Name.ToString());
                CommandLogging.WriteLine(from, "{0} {1} adding {2} to bank boxes )", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targ));

                GiveItem(from, (Item)targ, m_Amount, m_CopyProperties, m_GiveRule, m_Access);

                DateTime endTime = DateTime.UtcNow;
                Console.WriteLine("done in {0:F1} seconds.", (endTime - startTime).TotalSeconds);
                Server.World.Broadcast(0x35, true, "Routine maintenance complete. The entire process took {0:F1} seconds.", (endTime - startTime).TotalSeconds);
            }
        }

        public static void GiveItem(Mobile from, Item item, int amount, bool CopyProperties, int give, int access)
        {
            bool done = true;
            if (give == (int)AddToBankGump.Switches.GiveToAccount)
            {
                done = AddToBank.GiveItemToAccounts(item, amount, CopyProperties);
            }
            else if (give == (int)AddToBankGump.Switches.GiveToCharacter)
            {
                done = AddToBank.GiveItemToCharacters(item, amount, CopyProperties);
            }
            else if (give == (int)AddToBankGump.Switches.GiveToAccessLevel)
            {
                done = AddToBank.GiveItemToAccessLevel(item, amount, CopyProperties, access);
            }

            if (!done)
            {
                from.SendMessage("Unable to give out to 1 or more players.");
            }
            else
            {
                from.SendMessage("Completed.");
            }

        }

        private static bool GiveItemToAccounts(Item item, int amount, bool copyProperties)
        {
            bool success = true;
            ArrayList accts = new ArrayList(Accounts.Table.Values);
            foreach (Account acct in accts)
            {

                if (acct[GetFirstCharacter(acct)] != null)
                {
                    if (!CopyItem(item, amount, copyProperties, acct[GetFirstCharacter(acct)].BankBox))
                    {
                        Console.WriteLine("Could not give item to {0}", acct[GetFirstCharacter(acct)].Name);
                        success = false;
                    }
                }
            }
            return success;
        }

        private static int GetFirstCharacter(Account acct)
        {
            for (int i = 0; i < acct.Length; ++i)
            {
                if (acct[i] != null)
                {
                    return i;
                }
            }

            Console.WriteLine("Error no valid characters found on {0}", acct.Username);
            return 0;
        }

        private static bool GiveItemToCharacters(Item item, int amount, bool copyProperties)
        {
            bool success = true;
            //ArrayList mobs = new ArrayList(World.Mobiles.Values);
            List<Mobile> mobs = new List<Mobile>(World.Mobiles.Values);
            foreach (Mobile m in mobs)
            {
                if (m is PlayerMobile)
                {
                    if (!CopyItem(item, amount, copyProperties, m.BankBox))
                    {
                        Console.WriteLine("Could not give item to {0}", m.Name);
                        success = false;
                    }
                }
            }
            return success;
        }

        private static bool GiveItemToAccessLevel(Item item, int amount, bool copyProperties, int access)
        {
            bool success = true;
            //ArrayList mobs = new ArrayList(World.Mobiles.Values);
            List<Mobile> mobs = new List<Mobile>(World.Mobiles.Values);
            foreach (Mobile m in mobs)
            {
                if (m is PlayerMobile)
                {
                    bool give = false;
                    if ((access & (int)AddToBankGump.Switches.Owner) != 0 && m.AccessLevel == AccessLevel.Owner)
                    {
                        give = true;
                    }
                    else if ((access & (int)AddToBankGump.Switches.Administrator) != 0 && m.AccessLevel == AccessLevel.Administrator)
                    {
                        give = true;
                    }
                    else if ((access & (int)AddToBankGump.Switches.GameMaster) != 0 && m.AccessLevel == AccessLevel.GameMaster)
                    {
                        give = true;
                    }
                    else if ((access & (int)AddToBankGump.Switches.Seer) != 0 && m.AccessLevel == AccessLevel.Seer)
                    {
                        give = true;
                    }
                    else if ((access & (int)AddToBankGump.Switches.Counselor) != 0 && m.AccessLevel == AccessLevel.Counselor)
                    {
                        give = true;
                    }
                    else if ((access & (int)AddToBankGump.Switches.FightBroker) != 0 && m.AccessLevel == AccessLevel.FightBroker)
                    {
                        give = true;
                    }
                    else if ((access & (int)AddToBankGump.Switches.Reporter) != 0 && m.AccessLevel == AccessLevel.Reporter)
                    {
                        give = true;
                    }

                    if (give)
                    {
                        if (!CopyItem(item, amount, copyProperties, m.BankBox))
                        {
                            Console.WriteLine("Could not give item to {0}", m.Name);
                            success = false;
                        }
                    }
                }
            }
            return success;
        }

        private static bool CopyItem(Item item, int count, bool copyProperties, Container container)
        {
            bool m_Success = false;
            Type t = item.GetType();

            ConstructorInfo[] info = t.GetConstructors();

            foreach (ConstructorInfo c in info)
            {
                ParameterInfo[] paramInfo = c.GetParameters();

                if (paramInfo.Length == 0)
                {
                    object[] objParams = new object[0];

                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            object o = c.Invoke(objParams);

                            if (o != null && o is Item)
                            {
                                Item new_item = (Item)o;
                                if (copyProperties == true)
                                    CopyProperties(new_item, item);
                                new_item.Parent = null;

                                // recurse if container
                                if (item is Container && new_item.Items.Count == 0)
                                {
                                    for (int x = 0; x < item.Items.Count; x++)
                                    {
                                        m_Success = CopyItem((Item)item.Items[x], 1, copyProperties, (Container)new_item);
                                    }
                                }

                                if (container != null)
                                    PlaceItemIn(container, 20 + (i * 10), 10, new_item);
                            }
                        }
                        m_Success = true;
                    }
                    catch
                    {
                        m_Success = false;
                    }
                }

            } // end foreach
            return m_Success;

        } // end function

        private static void CopyProperties(Item dest, Item src)
        {
            PropertyInfo[] props = src.GetType().GetProperties();
            PropertyInfo[] swaps = new PropertyInfo[2];

            for (int i = 0; i < props.Length; i++)
            {
                try
                {
                    if (props[i].CanRead && props[i].CanWrite)
                    {
                        //Console.WriteLine( "Setting {0} = {1}", props[i].Name, props[i].GetValue( src, null ) );
                        if (src is Container && (props[i].Name == "TotalWeight" || props[i].Name == "TotalItems"))
                        {
                            // don't set these props
                        }
                        else
                        {
                            // 8/5/21, Adam: Unfortunatly, Amount and TotalGold have side effects, so they have to be set in reverse order.
                            //  That could be a fix for later, but it's too complex to try to handle before launch. For now I just swap
                            //  the order of the assignment, and that solves the problem.
                            if (props[i].Name == "Amount")
                            {
                                swaps[0] = props[i];
                                continue;
                            }
                            if (props[i].Name == "TotalGold")
                            {
                                swaps[1] = props[i];
                                continue;
                            }

                            props[i].SetValue(dest, props[i].GetValue(src, null), null);
                        }
                    }
                }
                catch
                {
                    //Console.WriteLine( "Denied" );
                }
            }

            try
            {
                if (swaps[0] != null && swaps[1] != null)
                {
                    swaps[0].SetValue(dest, swaps[0].GetValue(src, null), null);
                    swaps[1].SetValue(dest, swaps[1].GetValue(src, null), null);
                }
            }
            catch (Exception ex)
            {
                Server.Diagnostics.LogHelper.LogException(ex);
            }
        }
        #endregion

    } // end class

    #region " Gump "

    public class AddToBankGump : Gump
    {
        private int m_Amount;

        public void RenderGump()
        {
            m_Amount = 1;
            RenderGump(100, 0, string.Empty);
        }

        public void RenderGump(int rule, int access, string type)
        {
            AddPage(0);
            AddBackground(0, 0, 400, 320, 9260);
            AddLabel(125, 20, 52, @"Distribute Items to Shard");
            AddLabel(25, 40, 52, @"Rules:");
            AddLabel(260, 60, 2100, @"Amount:");
            AddLabel(315, 60, 2100, m_Amount.ToString());
            AddButton(330, 62, 9700, 9701, (int)Buttons.IncAmount, GumpButtonType.Reply, 1);
            AddButton(345, 62, 9704, 9705, (int)Buttons.DecAmount, GumpButtonType.Reply, -1);
            AddRadio(35, 60, 209, 208, rule == (int)Switches.GiveToAccount, (int)Switches.GiveToAccount);
            AddLabel(65, 60, 2100, @"Per Account");
            AddRadio(35, 80, 209, 208, rule == (int)Switches.GiveToCharacter, (int)Switches.GiveToCharacter);
            AddLabel(65, 80, 2100, @"Per Character (Mobile)");
            AddRadio(35, 100, 209, 208, rule == (int)Switches.GiveToAccessLevel, (int)Switches.GiveToAccessLevel);
            AddLabel(65, 100, 2100, @"Per AccessLevel");
            AddCheck(80, 125, 210, 211, (access & (int)Switches.Owner) != 0, (int)Switches.Owner);
            AddLabel(105, 125, 2100, @"Owner");

            AddCheck(215, 125, 210, 211, (access & (int)Switches.Administrator) != 0, (int)Switches.Administrator);
            AddLabel(240, 125, 2100, @"Administrator");

            AddCheck(80, 150, 210, 211, (access & (int)Switches.Seer) != 0, (int)Switches.Seer);
            AddLabel(105, 150, 2100, @"Seer");
            AddCheck(215, 150, 210, 211, (access & (int)Switches.GameMaster) != 0, (int)Switches.GameMaster);
            AddLabel(240, 150, 2100, @"GameMaster");
            AddCheck(80, 175, 210, 211, (access & (int)Switches.Counselor) != 0, (int)Switches.Counselor);
            AddLabel(105, 175, 2100, @"Counselor");
            AddCheck(215, 175, 210, 211, (access & (int)Switches.Counselor) != 0, (int)Switches.FightBroker);
            AddLabel(240, 175, 2100, @"FightBroker");
            AddCheck(80, 200, 210, 211, (access & (int)Switches.Counselor) != 0, (int)Switches.Reporter);
            AddLabel(105, 200, 2100, @"Reporter");

            AddCheck(215, 100, 210, 211, true, (int)Switches.CopyProperties);
            AddLabel(240, 100, 2100, @"Copy Properties");

            AddLabel(80, 235, 52, @"Give By Type");
            AddLabel(280, 235, 52, @"Give By Target");
            AddImageTiled(40, 260, 160, 20, 9274);
            AddTextEntry(45, 260, 150, 20, 2100, 100, type);
            AddButton(200, 260, 4014, 4016, (int)Buttons.GiveByType, GumpButtonType.Reply, 0);
            AddButton(310, 260, 4005, 4007, (int)Buttons.GiveByTarget, GumpButtonType.Reply, 1);
        }

        public AddToBankGump()
            : base(50, 50)
        {
            RenderGump();
        }

        public AddToBankGump(int GiveRule, int Access, string TypeName, int Amount)
            : base(50, 50)
        {
            m_Amount = Amount;
            RenderGump(GiveRule, Access, TypeName);
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            string TypeName = string.Empty;
            int GiveRule = 0;
            int Access = 0;
            int CopyProperties = 0;

            foreach (int sw in info.Switches)
            {
                switch (sw)
                {
                    case (int)Switches.GiveToCharacter:
                        {
                            GiveRule = (int)Switches.GiveToCharacter;
                            break;
                        }
                    case (int)Switches.GiveToAccount:
                        {
                            GiveRule = (int)Switches.GiveToAccount;
                            break;
                        }
                    case (int)Switches.GiveToAccessLevel:
                        {
                            GiveRule = (int)Switches.GiveToAccessLevel;
                            break;
                        }
                    case (int)Switches.CopyProperties:
                        {
                            CopyProperties = (int)Switches.CopyProperties;
                            break;
                        }

                    case (int)Switches.Owner:
                    case (int)Switches.Administrator:
                    case (int)Switches.Seer:
                    case (int)Switches.GameMaster:
                    case (int)Switches.Counselor:
                    case (int)Switches.FightBroker:
                    case (int)Switches.Reporter:
                        {
                            Access += sw;
                            break;
                        }
                }
            }
            if (GiveRule == 0)
            {
                from.SendMessage("You must select the audience rule to receive the item.");
                from.SendGump(new AddToBankGump(GiveRule, Access, TypeName, m_Amount));
                return;
            }
            else if (GiveRule == (int)Switches.GiveToAccessLevel && Access == 0)
            {
                from.SendMessage("You must select the AccessLevel to receive the item.");
                from.SendGump(new AddToBankGump(GiveRule, Access, TypeName, m_Amount));
                return;
            }

            switch (info.ButtonID)
            {
                case (int)Buttons.GiveByTarget:
                    {
                        from.Target = new AddToBank.DupeTarget(false, m_Amount, CopyProperties != 0, GiveRule, Access);
                        from.SendMessage("What do you wish to give out?");
                        break;
                    }
                case (int)Buttons.GiveByType:
                    {
                        if (info.TextEntries.Length > 0)
                        {
                            TypeName = info.TextEntries[0].Text;
                        }

                        if (TypeName == string.Empty)
                        {
                            from.SendMessage("You must specify a type");
                            from.SendGump(new AddToBankGump(GiveRule, Access, TypeName, m_Amount));
                        }
                        else
                        {
                            Type type = ScriptCompiler.FindTypeByName(TypeName, true);
                            if (type == null)
                            {
                                from.SendMessage("{0} is not a valid type", type);
                                from.SendGump(new AddToBankGump(GiveRule, Access, string.Empty, m_Amount));
                                return;
                            }
                            else
                            {
                                object obj = Activator.CreateInstance(type);
                                if (obj is Item)
                                    AddToBank.GiveItem(from, (Item)obj, m_Amount, CopyProperties != 0, GiveRule, Access);
                                else
                                {
                                    from.SendMessage("You may only duplicate items.");
                                }
                            }
                        }
                        break;
                    }
                case (int)Buttons.IncAmount:
                    {
                        from.SendGump(new AddToBankGump(GiveRule, Access, TypeName, ++m_Amount));
                        break;
                    }
                case (int)Buttons.DecAmount:
                    {
                        if (m_Amount > 1)
                            m_Amount -= 1;
                        else
                            from.SendMessage("You cannot give less than 1 item.");
                        from.SendGump(new AddToBankGump(GiveRule, Access, TypeName, m_Amount));
                        break;
                    }
            }

        }

        public enum Buttons
        {
            Cancel,
            GiveByTarget,
            GiveByType,
            IncAmount,
            DecAmount
        }

        public enum Switches
        {
            Owner = 1,
            Administrator = 2,
            Seer = 3,
            GameMaster = 4,
            Counselor = 5,
            FightBroker = 6,
            Reporter = 7,
            GiveToAccount = 100,
            GiveToCharacter = 200,
            GiveToAccessLevel = 300,
            CopyProperties = 400,
        }

    } // end class AddToBankGump

    #endregion

} // end namespace