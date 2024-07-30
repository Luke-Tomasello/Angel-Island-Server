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

/* Scripts\Commands\Dupe.cs
 * CHANGELOG
 * 8/5/21, Adam: Unfortunatly, Amount and TotalGold have side effects, so they have to be set in reverse order.
 *      That could be a fix for later, but it's too complex to try to handle before launch. For now I just swap
 *      the order of the assignment, and that solves the problem.
 *	5/29/10/ Adam
 *		Make CopyProperties public
 *	5/7/10, Adam
 *		New [Dupe implementation that allows the area modifier, i.e., [area dupe
 *		Scripts/Commands/Abstracted/Commands/Commands.cs
 */

using Server.Items;
using Server.Targeting;
using System;
using System.Reflection;

namespace Server.Commands
{
    public class Dupe
    {
        public static void Initialize()
        {   // adam: see comment section
            Server.CommandSystem.Register("DupeStatic", AccessLevel.GameMaster, new CommandEventHandler(DupeStatic_OnCommand));
            Server.CommandSystem.Register("DupeInBag", AccessLevel.GameMaster, new CommandEventHandler(DupeInBag_OnCommand));
        }

        [Usage("DupeStatic [amount]")]
        [Description("Dupes a targeted static or item.")]
        private static void DupeStatic_OnCommand(CommandEventArgs e)
        {
            int amount = 1;
            if (e.Length >= 1)
                amount = e.GetInt32(0);
            e.Mobile.Target = new DupeStaticTarget(amount > 0 ? amount : 1);
            e.Mobile.SendMessage("What do you wish to dupe?");
        }

        [Usage("DupeInBag <count>")]
        [Description("Dupes an item at it's current location (count) number of times.")]
        private static void DupeInBag_OnCommand(CommandEventArgs e)
        {
            int amount = 1;
            if (e.Length >= 1)
                amount = e.GetInt32(0);

            e.Mobile.Target = new DupeTarget(true, amount > 0 ? amount : 1);
            e.Mobile.SendMessage("What do you wish to dupe?");
        }
        private class DupeStaticTarget : Target
        {
            private int m_Amount;

            public DupeStaticTarget(int amount)
                : base(15, false, TargetFlags.None)
            {
                m_Amount = amount;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                bool done = false;
                if (!(targ is StaticTarget))
                {
                    from.SendMessage("You can only dupe statics.");
                    return;
                }

                CommandLogging.WriteLine(from, "{0} {1} duping {2} (amount={3})", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targ), m_Amount);

                Container pack = from.Backpack;

                try
                {
                    from.SendMessage("Duping {0}...", m_Amount);
                    for (int i = 0; i < m_Amount; i++)
                    {
                        Static copy = new Static(((StaticTarget)targ).ItemID);

                        if (pack != null)
                            pack.DropItem(copy);
                        else
                            copy.MoveToWorld(from.Location, from.Map);

                    }
                    from.SendMessage("Done");
                    done = true;
                }
                catch
                {
                    from.SendMessage("Error!");
                    return;
                }

                if (!done)
                {
                    from.SendMessage("Unable to dupe.  Item must have a 0 parameter constructor.");
                }
            }
        }
        private class DupeTarget : Target
        {
            private bool m_InBag;
            private int m_Amount;

            public DupeTarget(bool inbag, int amount)
                : base(15, false, TargetFlags.None)
            {
                m_InBag = inbag;
                m_Amount = amount;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                bool done = false;
                if (!(targ is Item))
                {
                    from.SendMessage("You can only dupe statics and items.");
                    return;
                }

                CommandLogging.WriteLine(from, "{0} {1} duping {2} (inBag={3}; amount={4})", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targ), m_InBag, m_Amount);

                Item copy = (Item)targ;
                Container pack;

                if (m_InBag)
                {
                    if (copy.Parent is Container)
                        pack = (Container)copy.Parent;
                    else if (copy.Parent is Mobile)
                        pack = ((Mobile)copy.Parent).Backpack;
                    else
                        pack = null;
                }
                else
                    pack = from.Backpack;

                Type t = copy.GetType();

                ConstructorInfo[] info = t.GetConstructors();

                foreach (ConstructorInfo c in info)
                {
                    //if ( !c.IsDefined( typeof( ConstructableAttribute ), false ) ) continue;

                    ParameterInfo[] paramInfo = c.GetParameters();

                    if (paramInfo.Length == 0)
                    {
                        object[] objParams = new object[0];

                        try
                        {
                            from.SendMessage("Duping {0}...", m_Amount);
                            for (int i = 0; i < m_Amount; i++)
                            {
                                object o = c.Invoke(objParams);

                                if (o != null && o is Item)
                                {
                                    Item new_item = (Item)o;
                                    Utility.CopyProperties(new_item, copy);//copy.Dupe( item, copy.Amount );
                                    if (new_item is Container cont)
                                        cont.UpdateAllTotals();
                                    else
                                        new_item.UpdateTotals();
                                    new_item.Parent = null;

                                    if (pack != null)
                                        pack.DropItem(new_item);
                                    else
                                        new_item.MoveToWorld(from.Location, from.Map);
                                }
                            }
                            from.SendMessage("Done");
                            done = true;
                        }
                        catch
                        {
                            from.SendMessage("Error!");
                            return;
                        }
                    }
                }

                if (!done)
                {
                    from.SendMessage("Unable to dupe.  Item must have a 0 parameter constructor.");
                }
            }
        }
        #region OBSOLETE, Moved to Utilities
#if false
        public static void CopyProperties(Item dest, Item src)
        {
            PropertyInfo[] props = src.GetType().GetProperties();
            PropertyInfo[] swaps = new PropertyInfo[2];

            for (int i = 0; i < props.Length; i++)
            {
                try
                {
                    if (props[i].CanRead && props[i].CanWrite)
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


                        //Console.WriteLine( "Setting {0} = {1}", props[i].Name, props[i].GetValue( src, null ) );
                        props[i].SetValue(dest, props[i].GetValue(src, null), null);

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
#endif
        #endregion OBSOLETE, Moved to Utilities
    }
}