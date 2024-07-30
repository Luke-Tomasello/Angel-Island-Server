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

/* Server/ClientVersion.cs
 * CHANGELOG:
 *	3/4/10, Adam
 *		rewrite CompareTo() to use our new Version function.
 *	3/3/10, Adam
 *		Really fixing the client version parsing.
 *	9/7/05: Pix
 *		Fixed client version parsing
 */

using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace Server
{
    public enum ClientType
    {
        Regular,
        UOTD,
        God,
        SA
    }

    public class ClientVersion : IComparable, IComparer
    {
        private int m_Major, m_Minor, m_Revision, m_Patch;
        private ClientType m_Type;
        private string m_SourceString;

        public int Major
        {
            get
            {
                return m_Major;
            }
        }

        public int Minor
        {
            get
            {
                return m_Minor;
            }
        }

        public int Revision
        {
            get
            {
                return m_Revision;
            }
        }

        public int Patch
        {
            get
            {
                return m_Patch;
            }
        }

        public ClientType Type
        {
            get
            {
                return m_Type;
            }
        }

        public string SourceString
        {
            get
            {
                return m_SourceString;
            }
        }

        public ClientVersion(int maj, int min, int rev, int pat)
            : this(maj, min, rev, pat, ClientType.Regular)
        {
        }

        public ClientVersion(int maj, int min, int rev, int pat, ClientType type)
        {
            m_Major = maj;
            m_Minor = min;
            m_Revision = rev;
            m_Patch = pat;
            m_Type = type;

            m_SourceString = ToString();
        }

        public static bool operator ==(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) == 0);
        }

        public static bool operator !=(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) != 0);
        }

        public static bool operator >=(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) >= 0);
        }

        public static bool operator >(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) > 0);
        }

        public static bool operator <=(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) <= 0);
        }

        public static bool operator <(ClientVersion l, ClientVersion r)
        {
            return (Compare(l, r) < 0);
        }

        public override int GetHashCode()
        {
            return m_Major ^ m_Minor ^ m_Revision ^ m_Patch ^ (int)m_Type;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            ClientVersion v = obj as ClientVersion;

            if (v == null)
                return false;

            return m_Major == v.m_Major
                && m_Minor == v.m_Minor
                && m_Revision == v.m_Revision
                && m_Patch == v.m_Patch
                && m_Type == v.m_Type;
        }

        private uint Version(int major, int minor, int revision)
        {
            uint ver = (uint)(revision | minor << 8 | major << 16);
            return ver;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(16);

            builder.Append(m_Major);
            builder.Append('.');
            builder.Append(m_Minor);
            builder.Append('.');
            builder.Append(m_Revision);

            // adam: lols
            // if (m_Major <= 5 && m_Minor <= 0 && m_Revision <= 6)	//Anything before 5.0.7
            if (Version(m_Major, m_Minor, m_Revision) < Version(5, 0, 7))   //Anything before 5.0.7
            {
                if (m_Patch > 0)
                    builder.Append((char)('a' + (m_Patch - 1)));
            }
            else
            {
                builder.Append('.');
                builder.Append(m_Patch);
            }

            if (m_Type != ClientType.Regular)
            {
                builder.Append(' ');
                builder.Append(m_Type.ToString());
            }

            return builder.ToString();
        }

        public ClientVersion(string fmt)
        {
            m_SourceString = fmt;

            try
            {
                fmt = fmt.ToLower();

                // < 5.0.7 can look like 4.0.9b, >= 5.0.7 will look like 5.0.7 or 5.0.7.3
                Regex versionregex = new Regex("(?<major>[0-9]+).(?<minor>[0-9]+).(?<revision>[0-9]+)(?<tail>.*)");
                Match m = versionregex.Match(fmt);
                if (m != null && m.Groups != null)
                {
                    m_Major = (m.Groups["major"] != null) ? Utility.ToInt32(m.Groups["major"].ToString()) : 0;
                    m_Minor = (m.Groups["minor"] != null) ? Utility.ToInt32(m.Groups["minor"].ToString()) : 0; ;
                    m_Revision = (m.Groups["revision"] != null) ? Utility.ToInt32(m.Groups["revision"].ToString()) : 0; ;
                    string tail = (m.Groups["tail"] != null) ? m.Groups["tail"].ToString() : ""; ;

                    if (m != null && m.Groups != null)
                    {
                        if (Version(m_Major, m_Minor, m_Revision) < Version(5, 0, 7))   //Anything before 5.0.7
                        {
                            if (tail.Length == 0)                               // something like 4.0.9
                                m_Patch = 0;
                            else
                                m_Patch = (tail[0] - 'a') + 1;                  // something like 4.0.9b
                        }
                        else
                        {
                            if (tail.Length == 0)                               // something like 5.0.7
                                m_Patch = 0;
                            else
                                m_Patch = Utility.ToInt32(tail.Substring(1));   // something like 6.0.1.7
                        }
                    }
                }

                // this is safe since in ClientVerification.cs we have:
                // private static bool m_AllowRegular = true, m_AllowUOTD = false, m_AllowGod = false;
                if (fmt.IndexOf("god") >= 0 || fmt.IndexOf("gq") >= 0)
                    m_Type = ClientType.God;
                else if (fmt.IndexOf("third dawn") >= 0 || fmt.IndexOf("uo:td") >= 0 || fmt.IndexOf("uotd") >= 0 || fmt.IndexOf("uo3d") >= 0 || fmt.IndexOf("uo:3d") >= 0)
                    m_Type = ClientType.UOTD;
                else
                    m_Type = ClientType.Regular;

                /* old broken logic
				 fmt = fmt.ToLower();

				int br1 = fmt.IndexOf('.');
				int br2 = fmt.IndexOf('.', br1 + 1);

				int br3 = br2 + 1;
				while (br3 < fmt.Length && Char.IsDigit(fmt, br3))
					br3++;

				m_Major = Utility.ToInt32(fmt.Substring(0, br1));
				m_Minor = Utility.ToInt32(fmt.Substring(br1 + 1, br2 - br1 - 1));
				m_Revision = Utility.ToInt32(fmt.Substring(br2 + 1, br3 - br2 - 1));

				if (br3 < fmt.Length)
				{
					//if (m_Major <= 5 && m_Minor <= 0 && m_Revision <= 6)	//Anything before 5.0.7
					if (Version(m_Major, m_Minor, m_Revision) < Version(5, 0, 7))	//Anything before 5.0.7
					{
						if (!Char.IsWhiteSpace(fmt, br3))
							m_Patch = (fmt[br3] - 'a') + 1;
					}
					else
					{
						m_Patch = Utility.ToInt32(fmt.Substring(br3 + 1, fmt.Length - br3 - 1));
					}
				}

				if (fmt.IndexOf("god") >= 0 || fmt.IndexOf("gq") >= 0)
					m_Type = ClientType.God;
				else if (fmt.IndexOf("third dawn") >= 0 || fmt.IndexOf("uo:td") >= 0 || fmt.IndexOf("uotd") >= 0 || fmt.IndexOf("uo3d") >= 0 || fmt.IndexOf("uo:3d") >= 0)
					m_Type = ClientType.UOTD;
				else
					m_Type = ClientType.Regular;
				 */
            }
            catch
            {
                m_Major = 0;
                m_Minor = 0;
                m_Revision = 0;
                m_Patch = 0;
                m_Type = ClientType.Regular;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            ClientVersion o = obj as ClientVersion;

            if (o == null)
                throw new ArgumentException();

            return Version(m_Major, m_Minor, m_Revision).CompareTo(Version(o.Major, o.Minor, o.Revision));

            /*
			if (m_Major > o.m_Major)
				return 1;
			else if (m_Major < o.m_Major)
				return -1;
			else if (m_Minor > o.m_Minor)
				return 1;
			else if (m_Minor < o.m_Minor)
				return -1;
			else if (m_Revision > o.m_Revision)
				return 1;
			else if (m_Revision < o.m_Revision)
				return -1;
			else if (m_Patch > o.m_Patch)
				return 1;
			else if (m_Patch < o.m_Patch)
				return -1;
			else
				return 0;*/
        }

        public static bool IsNull(object x)
        {
            return Object.ReferenceEquals(x, null);
        }

        public int Compare(object x, object y)
        {
            if (IsNull(x) && IsNull(y))
                return 0;
            else if (IsNull(x))
                return -1;
            else if (IsNull(y))
                return 1;

            ClientVersion a = x as ClientVersion;
            ClientVersion b = y as ClientVersion;

            if (IsNull(a) || IsNull(b))
                throw new ArgumentException();

            return a.CompareTo(b);
        }

        public static int Compare(ClientVersion a, ClientVersion b)
        {
            if (IsNull(a) && IsNull(b))
                return 0;
            else if (IsNull(a))
                return -1;
            else if (IsNull(b))
                return 1;

            return a.CompareTo(b);
        }
    }
}