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

/* Scripts/Misc/WebStatus.cs
 * ChangeLog
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Guilds;
using Server.Network;
using System;
using System.IO;
using System.Text;

namespace Server.Misc
{
    public class StatusPage : Timer
    {
        public static void Initialize()
        {
            new StatusPage().Start();
        }

        public StatusPage()
            : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(60.0))
        {
            Priority = TimerPriority.FiveSeconds;
        }

        private static string Encode(string input)
        {
            StringBuilder sb = new StringBuilder(input);

            sb.Replace("&", "&amp;");
            sb.Replace("<", "&lt;");
            sb.Replace(">", "&gt;");
            sb.Replace("\"", "&quot;");
            sb.Replace("'", "&apos;");

            return sb.ToString();
        }

        protected override void OnTick()
        {
            if (!Directory.Exists("web"))
                Directory.CreateDirectory("web");

            using (StreamWriter op = new StreamWriter("web/status.html"))
            {
                op.WriteLine("<html>");
                op.WriteLine("   <head>");
                op.WriteLine("      <title>RunUO Server Status</title>");
                op.WriteLine("   </head>");
                op.WriteLine("   <body bgcolor=\"white\">");
                op.WriteLine("      <h1>RunUO Server Status</h1>");
                op.WriteLine("      Online clients:<br>");
                op.WriteLine("      <table width=\"100%\">");
                op.WriteLine("         <tr>");
                op.WriteLine("            <td bgcolor=\"black\"><font color=\"white\">Name</font></td><td bgcolor=\"black\"><font color=\"white\">Location</font></td><td bgcolor=\"black\"><font coloLongTermMurdershite\">Kills</font></td><td bgcolor=\"black\"><font color=\"white\">Karma / Fame</font></td>");
                op.WriteLine("         </tr>");

                foreach (NetState state in NetState.Instances)
                {
                    Mobile m = state.Mobile;

                    if (m != null)
                    {
                        Guild g = m.Guild as Guild;

                        op.Write("         <tr><td>");

                        if (g != null)
                        {
                            op.Write(Encode(m.Name));
                            op.Write(" [");

                            string title = m.GuildTitle;

                            if (title != null)
                                title = title.Trim();
                            else
                                title = "";

                            if (title.Length > 0)
                            {
                                op.Write(Encode(title));
                                op.Write(", ");
                            }

                            op.Write(Encode(g.Abbreviation));

                            op.Write(']');
                        }
                        else
                        {
                            op.Write(Encode(m.Name));
                        }

                        op.Write("</td><td>");
                        op.Write(m.X);
                        op.Write(", ");
                        op.Write(m.Y);
                        op.Write(", ");
                        op.Write(m.Z);
                        op.Write(" (");
                        op.Write(m.Map);
                        op.Write(")</td><td>");
                        op.Write(m.LongTermMurders);
                        op.Write("</td><td>");
                        op.Write(m.Karma);
                        op.Write(" / ");
                        op.Write(m.Fame);
                        op.WriteLine("</td></tr>");
                    }
                }

                op.WriteLine("         <tr>");
                op.WriteLine("      </table>");
                op.WriteLine("   </body>");
                op.WriteLine("</html>");
            }
        }
    }
}