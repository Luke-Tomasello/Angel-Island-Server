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

/* Scripts/Engines/ResourcePool/ResourcePool.cs
 * ChangeLog
 * 11/16/21, Yoar
 *      BBS overhaul.
 *  04/27/05 Taran Kain
 *		Initial Version.
 */

using Server.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Drawing.Common;
using System.IO;

namespace Server.Engines.ResourcePool
{
    class Graph
    {
        private string m_Title;
        private string m_xAxis, m_yAxis;
        private double m_xMin, m_xMax, m_yMin, m_yMax;
        private int m_xTicks, m_yTicks;
        private double[] m_Data;

        public Graph()
        {
        }

        public Graph(string title, string xAxis, string yAxis, double[] data)
        {
            m_Title = title;
            m_xAxis = xAxis;
            m_yAxis = yAxis;

            m_xMin = 0;
            m_xMax = data.Length;

            m_yMax = 0;
            for (int i = 0; i < data.Length; i++)
                if (data[i] > m_yMax)
                    m_yMax = data[i];
            m_yMin = 0;
            m_yMax *= 1.2;

            m_xTicks = (data.Length > 20) ? 20 : data.Length;
            m_yTicks = 10;
            m_Data = data;
        }

        public int Create(string filename)
        {
#if false
            Bitmap buf = new Bitmap(830, 275);
            Graphics gfx = Graphics.FromImage(buf);

            gfx.Clear(Color.White);
            Pen p = new Pen(Color.Black, 2);
            Brush b = new SolidBrush(Color.Black);
            Font f = new Font("Verdana", 12, FontStyle.Bold);
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            gfx.DrawString(m_Title, f, b, 415, 5, sf);

            gfx.DrawLine(p, 51, 40, 51, 245);
            gfx.DrawLine(p, 47, 241, (m_Data.Length > 760) ? 812 : 811, 241);

            f = new Font("Verdana", 10);
            gfx.DrawString(m_xAxis, f, b, 442, 256, sf);
            gfx.RotateTransform(-90);
            gfx.DrawString(m_yAxis, f, b, -140, -1, sf);
            gfx.RotateTransform(90);

            // x ticks
            p = new Pen(Color.Black, 1);
            f = new Font("Verdana", 8);
            for (int i = 0; i <= m_xTicks; i++)
            {
                int x = (int)(51 + (double)((m_Data.Length > 760) ? 761 : 760) / m_xTicks * i);
                gfx.DrawLine(p, x, 240, x, 244);
                gfx.DrawString(string.Format("{0}", (int)((m_xMax - m_xMin) / m_xTicks * i) + m_xMin),
                    f, b, x, 244, sf);
            }

            sf.Alignment = StringAlignment.Far;
            for (int i = 0; i <= m_yTicks; i++)
            {
                int y = (int)(40 + (double)200 / m_yTicks * i);
                gfx.DrawLine(p, 47, y, 51, y);
                gfx.DrawString(string.Format("{0}", (m_yMax - m_yMin) / m_yTicks * (m_yTicks - i) + m_yMin),
                    f, b, 47, y - f.Height / 2, sf);
            }

            b = new SolidBrush(Color.FromArgb(0x99, 0xCC, 0xFF));
            for (int i = 0; i < m_Data.Length; i++)
            {
                gfx.FillRectangle(b, (float)(52 + (double)760 / m_Data.Length * i), (float)(40 + (200 - (double)200 * (m_Data[i] / m_yMax))),
                    (m_Data.Length > 760) ? 1 : (float)760 / m_Data.Length, (float)(200 * (m_Data[i] / m_yMax)));
            }

            buf.Save(filename, ImageFormat.Gif);
#endif
            return 1;
        }
    }

    class ConsignmentComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            ResourceConsignment rc1 = x as ResourceConsignment, rc2 = y as ResourceConsignment;

            if (rc1 == null && rc2 != null)
                return -1;
            if (rc1 == null && rc2 == null)
                return 0;
            if (rc1 != null && rc2 == null)
                return 1;

            return rc1.Seller.Name.CompareTo(rc2.Seller.Name);
        }
    }

    class TransactionComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            ResourceTransaction rt1 = x as ResourceTransaction, rt2 = y as ResourceTransaction;

            if (rt1 == null && rt2 != null)
                return -1;
            if (rt1 == null && rt2 == null)
                return 0;
            if (rt1 != null && rt2 == null)
                return 1;

            return rt1.Date.CompareTo(rt2.Date);
        }
    }

    /// <summary>
    /// Summary description for ResourceTracker.
    /// </summary>
    public static class ResourceTracker
    {
        private const string OUTPUT_DIRECTORY = "web"; //directory to place file
        private const string OUTPUT_FILE = "resource.html"; //file for output
        private static string RESULTS_CSS = Path.Combine(Core.DataDirectory, "results.css");

        public static void Initialize()
        {
            Server.CommandSystem.Register("ResourceTrack", AccessLevel.Administrator, new CommandEventHandler(ResourceTrack_OnCommand));
        }

        public static void ResourceTrack_OnCommand(CommandEventArgs e)
        {
            DateTime begin = DateTime.UtcNow;

            if (!Directory.Exists(OUTPUT_DIRECTORY))
                Directory.CreateDirectory(OUTPUT_DIRECTORY);

            File.Copy(RESULTS_CSS, OUTPUT_DIRECTORY + "/results.css", true);

            StreamWriter sw = new StreamWriter(OUTPUT_DIRECTORY + "/" + OUTPUT_FILE);

            try
            {
                Console.Write("ResourceTracker running...");

                sw.WriteLine("<html>");
                sw.WriteLine("<head>");
                sw.WriteLine("<title>ResourceTracker Results</title>");
                sw.WriteLine("<link rel='stylesheet' type='text/css' href='results.css'/>");
                sw.WriteLine("</head>");
                sw.WriteLine("<body>");
                sw.WriteLine("<h1>Immediate ResourcePool State</h1>");
                sw.WriteLine("<h2>Resources</h2>");
                sw.WriteLine("<table width='90%'>");
                sw.WriteLine("<tr class='theader'><td>Item Type</td><td>Resource Name</td><td>Count</td><td>Wholesale Price</td><td>Resale Price</td><td>Total Investment</td><td>Total Value</td></tr>");

                List<ResourceData> rdList = new List<ResourceData>(ResourcePool.Resources.Values);
                rdList.Sort();

                for (int i = 0, m = 0; i < rdList.Count; i++)
                {
                    ResourceData rd = rdList[i];
                    sw.WriteLine("<tr class='tr{6}'><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td></tr>",
                        rd.Type.Name, rd.Name, rd.TotalAmount, rd.WholesalePrice, rd.ResalePrice, rd.TotalInvested, rd.TotalValue, m++ % 2 + 1);
                }

                sw.WriteLine("</table>");

                sw.WriteLine("<h2>Equivalent Resources</h2>");
                sw.WriteLine("<table width='90%'>");
                sw.WriteLine("<tr class='theader'><td>Item Type</td><td>Resource Name</td><td>Amount Factor</td><td>Price Factor</td><td>Wholesale Price</td></tr>");

                for (int i = 0, m = 0; i < rdList.Count; i++)
                {
                    ResourceData rd = rdList[i];
                    foreach (EquivalentResource er in rd.EquivalentResources)
                    {
                        sw.WriteLine("<tr class='tr{4}'><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
                            er.Type.Name, rd.Name, er.AmountFactor, er.PriceFactor, rd.WholesalePrice, m++ % 2 + 1);
                    }
                }

                sw.WriteLine("</table>");

                sw.WriteLine("<h2>ResourceData Detail</h2>");

                for (int i = 0; i < rdList.Count; i++)
                {
                    ResourceData rd = rdList[i];
                    sw.WriteLine("<a name='{0}'/>", rd.Name);
                    sw.WriteLine("<h3>{0} Detail</h3>", rd.Name);
                    sw.WriteLine("<table width='90%'>");
                    sw.WriteLine("<tr class='theader'><td>Player Name</td><td>Amount Invested</td><td>Price Sold</td><td>Total Investment</td></tr>");

                    ArrayList rclist = new ArrayList(ResourcePool.GetConsignmentList(rd.Type));
                    rclist.Sort(new ConsignmentComparer());
                    ArrayList histogram = new ArrayList();
                    for (int a = 0, m = 0; a < rclist.Count; a++)
                    {
                        ResourceConsignment rc = rclist[a] as ResourceConsignment;
                        while (histogram.Count <= rc.Amount)
                            histogram.Add((double)0);
                        histogram[rc.Amount] = (double)(histogram[rc.Amount]) + (double)1;

                        sw.WriteLine("<tr class='tr{4}'><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>",
                            rc.Seller.Name, rc.Amount, rc.Price, rc.Amount * rc.Price, m++ % 2 + 1);
                    }
                    if (histogram.Count > 0)
                    {
                        Graph g = new Graph("Consignment Amount Distribution", "Amount of Consignment", "# of Consignments", (double[])histogram.ToArray(typeof(double)));
                        g.Create(OUTPUT_DIRECTORY + "/" + rd.Name + " dist.gif");
                        sw.WriteLine("<img src='{0} dist.gif'/>", rd.Name);
                    }

                    sw.WriteLine("</table>");
                }

                sw.WriteLine("</body>");
                sw.WriteLine("</html>");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine("Error!");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return;
            }
            finally
            {
                sw.Flush();
                sw.Close();
            }

            TimeSpan ts = DateTime.UtcNow - begin;
            Console.WriteLine("finished in {0} seconds.", (double)ts.Milliseconds / (double)1000);
        }
    }
}