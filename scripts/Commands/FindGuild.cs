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

/* Scripts/Commands/FindGuild.cs
 * Changelog
 *	06/14/06, Adam
 *		Add the account name to the display
 *	05/17/06, Kit
 *		Initial creation.
 */
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Commands
{
    public class FindGuild
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindGuild", AccessLevel.GameMaster, new CommandEventHandler(FindGuild_OnCommand));
            Server.CommandSystem.Register("FindTownStone", AccessLevel.GameMaster, new CommandEventHandler(FindStone_OnCommand));
        }

        [Usage("FindGuild <abbrevation>")]
        [Description("Finds a guild by abbreviation/name.")]
        public static void FindGuild_OnCommand(CommandEventArgs e)
        {
            Guild temp = null;
            PlayerMobile ptemp = null;

            if (e.Length == 1)
            {
                string text = e.GetString(0).ToLower();

                foreach (Item n in World.Items.Values)
                {
                    if (n is Guildstone && n != null)
                    {
                        if (((Guildstone)n).Guild != null)
                            temp = ((Guildstone)n).Guild;

                        if (temp.Abbreviation.ToLower() == text || temp.Name.ToLower() == text)
                        {
                            if (n.Parent != null && n.Parent is PlayerMobile)
                            {
                                ptemp = (PlayerMobile)n.Parent;
                                e.Mobile.SendMessage("Guild Stone Found on Mobile {2}:{0}, {1}", ptemp.Name, ptemp.Location, ptemp.Account);
                            }
                            else
                            {
                                e.Mobile.SendMessage("Guild Stone {1} Found at: {0} ({2})", n.Location, n.Serial, n.Map);
                                if (e.Mobile is PlayerMobile pm)
                                {
                                    pm.JumpIndex = 0;
                                    pm.JumpList = new System.Collections.ArrayList();
                                    pm.JumpList.Add(n);
                                }
                            }
                            return;
                        }
                    }
                }
                e.Mobile.SendMessage("Guild Stone not found in world");
            }
            else
            {
                Region region = TownshipRegion.Find(e.Mobile.Location, e.Mobile.Map);
                if (region is TownshipRegion tsr)
                    if (tsr.TStone != null && tsr.TStone.Guild != null && tsr.TStone.Guild.Guildstone != null)
                        FindGuild_OnCommand(new CommandEventArgs(e.Mobile, "FindGuild", tsr.TStone.Guild.Abbreviation, new string[] { tsr.TStone.Guild.Abbreviation }));
                    else
                        e.Mobile.SendMessage("Format: FindGuild <abbreviation>");
            }
        }

        [Usage("FindTownStone <abbrevation>")]
        [Description("Finds a township stone by guild abbreviation/name.")]
        public static void FindStone_OnCommand(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;
            pm.JumpIndex = 0;
            pm.JumpList = new System.Collections.ArrayList();

            if (e.Length == 1)
            {
                string text = e.GetString(0).ToLower();

                foreach (Item n in World.Items.Values)
                {
                    if (n is TownshipStone stone && stone.Guild != null)
                    {
                        string name = string.Empty;
                        if (!string.IsNullOrEmpty(stone.Guild.Abbreviation))
                            name = stone.Guild.Abbreviation.ToLower();
                        else if (!string.IsNullOrEmpty(stone.Guild.Name))
                            name = stone.Guild.Name.ToLower();

                        if (name.Contains(text))
                        {
                            pm.SendMessage("Guild Stone {1} Found at: {0} ({2})", n.Location, n.Serial, n.Map);
                            pm.JumpList.Add(n);
                        }
                    }
                }

                if (pm.JumpList.Count == 0)
                    pm.SendMessage("Town Stone not found in world");
            }
            else
            {
                pm.SendMessage("Format: FindTownStone <guild abbreviation>");
            }
        }
    }
}