/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server.Gumps
{
    public class LocationTree
    {
        private Map m_Map;
        private ParentNode m_Root;
        private Hashtable m_LastBranch;

        public LocationTree(string fileName, Map map)
        {
            m_LastBranch = new Hashtable();
            m_Map = map;

            string path = Path.Combine(Core.DataDirectory, "Locations", fileName);

            if (File.Exists(path))
            {
                XmlTextReader xml = new XmlTextReader(new StreamReader(path));

                xml.WhitespaceHandling = WhitespaceHandling.None;

                m_Root = Parse(xml);

                xml.Close();
            }
        }

        private LocationTree(ParentNode root, Map map)
        {
            m_LastBranch = new Hashtable();
            m_Map = map;
            m_Root = root;
        }

        public Hashtable LastBranch { get { return m_LastBranch; } }

        public Map Map { get { return m_Map; } }

        public ParentNode? Root { get { return m_Root; } }

        private ParentNode Parse(XmlTextReader xml)
        {
            xml.Read();
            xml.Read();
            xml.Read();

            return new ParentNode(xml, null);
        }

        public LocationTree CreateForAccess(AccessLevel accessLevel)
        {
            ParentNode? root = CloneParentNode(m_Root, null, AccessLevel.Player, accessLevel);

            return new LocationTree(root, m_Map);
        }

        private ParentNode? CloneParentNode(
            ParentNode source,
            ParentNode? newParent,
            AccessLevel inherited,
            AccessLevel target)
        {
            if (source == null)
                return null;

            AccessLevel effective =
                source.HasAccess ? Max(inherited, source.Access) : inherited;

            if (effective > target)
                return null;

            ArrayList children = new ArrayList();

            foreach (object child in source.Children)
            {
                object? cloned = CloneObject(child, null, effective, target); // temp parent null

                if (cloned != null)
                    children.Add(cloned);
            }

            if (children.Count == 0)
                return null;

            // Now create this node
            ParentNode clone = new ParentNode(
                source,
                newParent,
                effective,
                children.ToArray()
            );

            // Fix parent pointers for children
            foreach (object child in clone.Children)
            {
                if (child is ParentNode p)
                    p.SetParent(clone);
                else if (child is ChildNode c)
                    c.SetParent(clone);
            }

            return clone;
        }

        private object? CloneObject(
            object obj,
            ParentNode? newParent,
            AccessLevel inherited,
            AccessLevel target)
        {
            ParentNode? p = obj as ParentNode;

            if (p != null)
                return CloneParentNode(p, newParent, inherited, target);

            ChildNode? c = obj as ChildNode;

            if (c != null)
            {
                AccessLevel effective =
                    c.HasAccess ? Max(inherited, c.Access) : inherited;

                if (effective > target)
                    return null;

                return new ChildNode(c, newParent, effective);
            }

            return null;
        }

        private static AccessLevel Max(AccessLevel a, AccessLevel b)
        {
            return (a > b) ? a : b;
        }

        public IEnumerable<ChildNode> GetAllChildNodes()
        {
            if (m_Root == null)
                yield break;

            foreach (var node in Enumerate(m_Root))
                yield return node;
        }

        private IEnumerable<ChildNode> Enumerate(ParentNode parent)
        {
            foreach (object obj in parent.Children)
            {
                if (obj is ChildNode c)
                {
                    yield return c;
                }
                else if (obj is ParentNode p)
                {
                    foreach (var child in Enumerate(p))
                        yield return child;
                }
            }
        }
        public ChildNode? FindChildExact(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            name = name.Trim();

            foreach (var c in GetAllChildNodes())
            {
                if (Insensitive.Equals(c.Name, name))
                    return c;
            }

            return null;
        }

        public ParentNode? FindParentExact(string name)
        {
            if (m_Root == null || string.IsNullOrWhiteSpace(name))
                return null;

            return FindParentRecursive(m_Root, name.Trim());
        }

        private ParentNode? FindParentRecursive(ParentNode parent, string name)
        {
            if (Insensitive.Equals(parent.Name, name))
                return parent;

            foreach (object obj in parent.Children)
            {
                if (obj is ParentNode p)
                {
                    var found = FindParentRecursive(p, name);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        public ChildNode? GetDefaultChild(ParentNode parent)
        {
            if (parent == null)
                return null;

            foreach (object obj in parent.Children)
            {
                if (obj is ChildNode c)
                    return c;
            }

            return null;
        }

        public ChildNode? Resolve(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            name = name.Trim();

            // 1. exact child match
            var child = FindChildExact(name);
            if (child != null)
                return child;

            // 2. exact parent match -> default child
            var parent = FindParentExact(name);
            if (parent != null)
                return GetDefaultChild(parent);

            return null;
        }
    }
}