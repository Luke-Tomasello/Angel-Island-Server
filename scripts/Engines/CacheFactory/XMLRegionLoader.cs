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

/* scripts\Engines\CacheFactory\XMLRegionLoader.cs
 * CHANGELOG
 *  12/6/22, Adam
 *      First time checkin.
 *      Remove the region loading stuff from Region.cs (obsolete)
 *          and moved it here. It only ugly'ied up Region.cs, and we don't want folks 
 *          using that anyway.
 *          CacheFactory does use/load XML regions, so it is better placed here.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server.Engines
{
    public static partial class CacheFactory
    {
        private static bool m_SupressXmlWarnings;

        public static bool SupressXmlWarnings
        {
            get { return m_SupressXmlWarnings; }
            set { m_SupressXmlWarnings = value; }
        }

        public static Rectangle3D ParseRectangle(XmlElement rect)
        {
            int x1, y1, x2, y2;

            if (rect.HasAttribute("x") && rect.HasAttribute("y") && rect.HasAttribute("width") && rect.HasAttribute("height"))
            {
                x1 = int.Parse(rect.GetAttribute("x"));
                y1 = int.Parse(rect.GetAttribute("y"));
                x2 = x1 + int.Parse(rect.GetAttribute("width"));
                y2 = y1 + int.Parse(rect.GetAttribute("height"));
            }
            else if (rect.HasAttribute("x1") && rect.HasAttribute("y1") && rect.HasAttribute("x2") && rect.HasAttribute("y2"))
            {
                x1 = int.Parse(rect.GetAttribute("x1"));
                y1 = int.Parse(rect.GetAttribute("y1"));
                x2 = int.Parse(rect.GetAttribute("x2"));
                y2 = int.Parse(rect.GetAttribute("y2"));
            }
            else
            {
                throw new ArgumentException("Wrong attributes specified.");
            }

            int zmin = Region.DefaultMinZ;
            int zmax = Region.DefaultMaxZ;

            if (rect.HasAttribute("zmin"))
                zmin = int.Parse(rect.GetAttribute("zmin"));

            if (rect.HasAttribute("zmax"))
                zmax = int.Parse(rect.GetAttribute("zmax"));

            return new Rectangle3D(new Point3D(x1, x2, zmin), new Point3D(x2, y2, zmax));
        }

        public static Region CreateRegionFromXML(ref string outputText, string regionName)
        {
            List<Region> regions = CreateCacheFromXML("Regions.xml", ref outputText, regionName);
            if (regions.Count == 1)
            {
                return regions[0];
            }
            return null;
        }

        public static List<Region> CreateCacheFromXML(ref string outputText)
        {
            return CreateCacheFromXML("Regions.xml", ref outputText, string.Empty);
        }

        /// <summary>
        /// Plasma: This method will create a cache of ALL the regions in the XML, not just the ones that have been loaded itno m_Regions through Initialize()
        /// This code still respects the LoadFromXML and all other previous logic.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="outputText"></param>
        /// <returns></returns>
        /// <param name="regionToFind"></param>
        public static List<Region> CreateCacheFromXML(string fileName, ref string outputText, string regionToFind)
        {
            List<Rectangle3D> m_LoadCoords = new();
            List<Region> loadedRegions = new List<Region>();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (!File.Exists(Path.Combine(Core.DataDirectory, fileName)))
            {
                sb.Append(Environment.NewLine).Append(string.Format("Error: {0} does not exist", Path.Combine(Core.DataDirectory, fileName)));
                return loadedRegions;
            }

            Console.Write("Regions: Loading...");

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(Core.DataDirectory, fileName));

            XmlElement root = doc["ServerRegions"];
            foreach (XmlElement facet in root.GetElementsByTagName("Facet"))
            {
                bool exit = false;
                string facetName = facet.GetAttribute("name");
                try { Map.Parse(facetName); }
                catch
                {
                    Utility.Monitor.WriteLine("\nIgnoring facet {0}", ConsoleColor.Yellow, facetName);
                    continue;
                }
                Map map = Map.Parse(facetName);

                if (map == null || map == Map.Internal)
                {
                    if (!m_SupressXmlWarnings)
                        sb.Append(Environment.NewLine).AppendFormat(fileName + ": Invalid facet name '{0}'", facetName);
                    continue;
                }

                foreach (XmlElement reg in facet.GetElementsByTagName("region"))
                {

                    string name = reg.GetAttribute("name");
                    if (name == null || name.Length <= 0)
                        continue;

                    Region r = new Region(string.Empty, name, map);

                    if (string.IsNullOrEmpty(regionToFind) || name == regionToFind)
                    {
                        loadedRegions.Add(r);

                        if (!r.LoadFromXml)
                        {
                            if (!m_SupressXmlWarnings)
                                sb.Append(Environment.NewLine).AppendFormat(fileName + ": Region '{0}' has an XML entry, but is set not to LoadFromXml.", name);
                            continue;
                        }

                        try
                        {
                            string s = reg.GetAttribute("priority");
                            if (!string.IsNullOrEmpty(s))
                                r.Priority = int.Parse(s);
                        }
                        catch
                        {
                            if (!m_SupressXmlWarnings)
                                sb.Append(Environment.NewLine).AppendFormat(fileName + ": Could not parse priority for region '{0}' (assuming TownPriority)", r.Name);
                            r.Priority = Region.TownPriority;
                        }

                        XmlElement el;

                        el = reg["go"];
                        if (el != null)
                        {
                            try
                            {
                                r.GoLocation = Point3D.Parse(el.GetAttribute("location"));
                            }
                            catch
                            {
                                if (!m_SupressXmlWarnings)
                                    sb.Append(Environment.NewLine).AppendFormat(fileName + ": Could not parse go location for region '{0}'", r.Name);
                            }
                        }

                        el = reg["music"];
                        if (el != null)
                        {
                            try
                            {
                                //public static bool TryParse(Type enumType, string? value, out object? result);
                                object o;
                                if (Enum.TryParse(typeof(MusicName), el.GetAttribute("name"), out o))
                                    r.Music = (MusicName)o;// (MusicName)Enum.Parse(typeof(MusicName), el.GetAttribute("name"), true);
                            }
                            catch
                            {
                                if (!m_SupressXmlWarnings)
                                    sb.Append(Environment.NewLine).AppendFormat(fileName + ": Could not parse music for region '{0}'", r.Name);
                            }
                        }

                        el = reg["zrange"];
                        if (el != null)
                        {
                            string s = el.GetAttribute("min");
                            if (s != null && s != "")
                            {
                                try
                                {
                                    r.MinZ = int.Parse(s);
                                }
                                catch
                                {
                                    if (!m_SupressXmlWarnings)
                                        sb.Append(Environment.NewLine).AppendFormat(fileName + ": Could not parse zrange:min for region '{0}'", r.Name);
                                }
                            }

                            s = el.GetAttribute("max");
                            if (s != null && s != "")
                            {
                                try
                                {
                                    r.MaxZ = int.Parse(s);
                                }
                                catch
                                {
                                    if (!m_SupressXmlWarnings)
                                        sb.Append(Environment.NewLine).AppendFormat(fileName + ": Could not parse zrange:max for region '{0}'", r.Name);
                                }
                            }
                        }

                        foreach (XmlElement rect in reg.GetElementsByTagName("rect"))
                        {
                            try
                            {
                                if (m_LoadCoords == null)
                                    m_LoadCoords = new List<Rectangle3D>();

                                m_LoadCoords.Add(ParseRectangle(rect));
                            }
                            catch
                            {
                                if (!m_SupressXmlWarnings)
                                    sb.Append(Environment.NewLine).AppendFormat(fileName + ": Error parsing rect for region '{0}'", r.Name);
                                continue;
                            }
                        }

                        foreach (XmlElement rect in reg.GetElementsByTagName("inn"))
                        {
                            try
                            {
                                r.InnBounds.Add(ParseRectangle(rect));
                            }
                            catch
                            {
                                if (!m_SupressXmlWarnings)
                                    sb.Append(Environment.NewLine).AppendFormat(fileName + ": Error parsing inn for region '{0}'", r.Name);
                                continue;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(regionToFind) && name == regionToFind)
                    {
                        exit = true;
                    }
                }

                if (exit) break;
            }

            //ArrayList copy = new ArrayList(m_Regions);

            int i;
            for (i = 0; i < loadedRegions.Count; ++i)
            {
                Region region = loadedRegions[i];

                if (region.LoadFromXml && m_LoadCoords != null)
                    region.Coords.AddRange(m_LoadCoords);
            }

            sb.Append(Environment.NewLine).Append("done");
            outputText = sb.ToString();
            return loadedRegions;
        }
        #region OLD_REGION_LOADER
#if OLD_REGION_LOADER
        public static void Load()
        {
            List<Rectangle3D> m_LoadCoords = new();
            if (!System.IO.File.Exists("Data/Regions.xml"))
			{
				Console.WriteLine("Error: Data/Regions.xml does not exist");
				return;
			}

			Console.Write("Regions: Loading...");

			XmlDocument doc = new XmlDocument();
			doc.Load("Data/Regions.xml");

			XmlElement root = doc["ServerRegions"];
			foreach (XmlElement facet in root.GetElementsByTagName("Facet"))
			{
				string facetName = facet.GetAttribute("name");
				Map map = Map.Parse(facetName);

				if (map == null || map == Map.Internal)
				{
					if (!m_SupressXmlWarnings)
						Console.WriteLine("Regions.xml: Invalid facet name '{0}'", facetName);
					continue;
				}

				foreach (XmlElement reg in facet.GetElementsByTagName("region"))
				{
					string name = reg.GetAttribute("name");
					if (name == null || name.Length <= 0)
						continue;

					Region r = GetByName(name, map);
					if (r == null)
					{
						//if ( !m_SupressXmlWarnings )
						//	Console.WriteLine( "Regions.xml: Region '{0}' not defined.", name );
						continue;
					}
					else if (!r.LoadFromXml)
					{
						if (!m_SupressXmlWarnings)
							Console.WriteLine("Regions.xml: Region '{0}' has an XML entry, but is set not to LoadFromXml.", name);
						continue;
					}

					try
					{
						r.Priority = int.Parse(reg.GetAttribute("priority"));
					}
					catch
					{
						if (!m_SupressXmlWarnings)
							Console.WriteLine("Regions.xml: Could not parse priority for region '{0}' (assuming TownPriority)", r.Name);
						r.Priority = TownPriority;
					}

					XmlElement el;

					el = reg["go"];
					if (el != null)
					{
						try
						{
							r.GoLocation = Point3D.Parse(el.GetAttribute("location"));
						}
						catch
						{
							if (!m_SupressXmlWarnings)
								Console.WriteLine("Regions.xml: Could not parse go location for region '{0}'", r.Name);
						}
					}

					el = reg["music"];
					if (el != null)
					{
						try
						{
							r.Music = (MusicName)Enum.Parse(typeof(MusicName), el.GetAttribute("name"), true);
						}
						catch
						{
							if (!m_SupressXmlWarnings)
								Console.WriteLine("Regions.xml: Could not parse music for region '{0}'", r.Name);
						}
					}

					el = reg["zrange"];
					if (el != null)
					{
						string s = el.GetAttribute("min");
						if (s != null && s != "")
						{
							try
							{
								r.MinZ = int.Parse(s);
							}
							catch
							{
								if (!m_SupressXmlWarnings)
									Console.WriteLine("Regions.xml: Could not parse zrange:min for region '{0}'", r.Name);
							}
						}

						s = el.GetAttribute("max");
						if (s != null && s != "")
						{
							try
							{
								r.MaxZ = int.Parse(s);
							}
							catch
							{
								if (!m_SupressXmlWarnings)
									Console.WriteLine("Regions.xml: Could not parse zrange:max for region '{0}'", r.Name);
							}
						}
					}

					foreach (XmlElement rect in reg.GetElementsByTagName("rect"))
					{
						try
						{
							if (r.m_LoadCoords == null)
								r.m_LoadCoords = new ArrayList(1);

							r.m_LoadCoords.Add(ParseRectangle(rect, true));
						}
						catch
						{
							if (!m_SupressXmlWarnings)
								Console.WriteLine("Regions.xml: Error parsing rect for region '{0}'", r.Name);
							continue;
						}
					}

					foreach (XmlElement rect in reg.GetElementsByTagName("inn"))
					{
						try
						{
							if (r.InnBounds == null)
								r.InnBounds = new ArrayList(1);

							r.InnBounds.Add(ParseRectangle(rect, false));
						}
						catch
						{
							if (!m_SupressXmlWarnings)
								Console.WriteLine("Regions.xml: Error parsing inn for region '{0}'", r.Name);
							continue;
						}
					}
				}
			}

			ArrayList copy = new ArrayList(m_Regions);

			int i;
			for (i = 0; i < copy.Count; ++i)
			{
				Region region = (Region)copy[i];
				if (!region.LoadFromXml && region.m_Coords == null)
				{
					region.Coords = new ArrayList();
				}
				else if (region.LoadFromXml)
				{
					if (region.m_LoadCoords == null)
						region.m_LoadCoords = new ArrayList();

					region.Coords = region.m_LoadCoords;

					//if ( !m_SupressXmlWarnings )
					//	Console.WriteLine( "Warning: Region '{0}' did not contain any coords in Regions.xml (map={0})", region, region.Map.Name );
				}
			}

			for (i = 0; i < Map.AllMaps.Count; ++i)
				((Map)Map.AllMaps[i]).Regions.Sort();

			ArrayList list = new ArrayList(World.Mobiles.Values);

			foreach (Mobile m in list)
				m.ForceRegionReEnter(true);

			Console.WriteLine("done");
		}
#endif
        #endregion OLD_REGION_LOADER
    }
}