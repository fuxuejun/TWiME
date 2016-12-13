using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace OGS.HOCON
{
    public class TreeNode
    {
        public TreeNode()
        {
            Nodes = new List<TreeNode>();
        }

        public TreeNode(string name)
            : this()
        {
            this.Name = name;
        }

        public TreeNode(string name, object val)
            : this(name)
        {
            this.Value = val;
        }

        public int Depth { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
        public bool ViturlNode { get; set; }
        public TreeNode RootNode { get; set; }
        public List<TreeNode> Nodes { get; set; }

        private TreeNode FindNodeByName(string name, List<TreeNode> nodes)
        {
            foreach (var treeNode in nodes)
            {
                if (treeNode.Name == name)
                {
                    return treeNode;
                }

                var node = FindNodeByName(name, treeNode.Nodes);
                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }

        public TreeNode FindNode(string name)
        {
            return FindNodeByName(name, Nodes);
        }

        public bool Contains(string key)
        {
            return FindNodeByName(key, Nodes) != null;
        }
    }

    public class Writer<TNode>
        where TNode : class, new()
    {
        public void WriteStream(Stream stream, IEnumerable<KeyValuePair<string, object>> data, string headline = null)
        {
            var writter = new StreamWriter(stream);
            writter.Write(WriteString(data, headline));
            writter.Flush();
        }

        private TreeNode BuildTree(IEnumerable<KeyValuePair<string, object>> data)
        {
            var tree = new TreeNode();
            tree.ViturlNode = true;
            tree.Depth = 0;
            foreach (var entry in data)
            {
                var keyArray = entry.Key.Split('.');
                var prevKey = string.Empty;
                TreeNode parentNode = null;
                foreach (var key in keyArray)
                {
                    if (!string.IsNullOrEmpty(prevKey))
                    {
                        parentNode = parentNode?.FindNode(prevKey);
                    }

                    if (parentNode == null)
                    {
                        parentNode = tree;
                    }
                    if (entry.Value is DictionaryReaderNode)
                    {
                        if (!parentNode.Contains(key))
                        {
                            var node = new TreeNode(key);
                            node.Depth = parentNode.Depth + 1;
                            parentNode.Nodes.Add(node);
                        }
                    }
                    else
                    {
                        if (!parentNode.Contains(key))
                        {
                            var node = new TreeNode(key, entry.Value);
                            node.Depth = parentNode.Depth + 1;
                            parentNode.Nodes.Add(node);
                        }
                    }

                    prevKey = key;
                }
            }

            return tree;
        }

        public string WriteString(IEnumerable<KeyValuePair<string, object>> data, string headline = null)
        {
            var tree = BuildTree(data);

            var buffer = new StringBuilder();
            buffer.AppendLine("#" + headline);
            WriteNode(tree.Nodes, buffer);

            return buffer.ToString();
        }



        private void WriteNode(List<TreeNode> nodes, StringBuilder buffer)
        {
            foreach (var treeNode in nodes)
            {
                if (treeNode.ViturlNode) continue;

                if (treeNode.Nodes.Count > 0)
                {
                    buffer.AppendLine(new string('\t', (treeNode.Depth - 1)) + treeNode.Name + " {");
                    WriteNode(treeNode.Nodes, buffer);
                    buffer.AppendLine(new string('\t', (treeNode.Depth - 1)) + "}");
                }
                else
                {
                    if (treeNode.Value.GetType().IsGenericType)
                    {
                        var array = treeNode.Value as List<object>;
                        if (array == null) continue;
                        buffer.AppendLine(new string('\t', (treeNode.Depth - 1)) + treeNode.Name + " [");
                        for (var index = 0; index < array.Count; index++)
                        {
                            var item = array[index];
                            if (index == array.Count - 1)
                            {
                                buffer.AppendLine(new string('\t', (treeNode.Depth)) + WriteValue(item));
                            }
                            else
                            {
                                buffer.AppendLine(new string('\t', (treeNode.Depth)) + WriteValue(item) + ",");
                            }
                        }
                        buffer.AppendLine(new string('\t', (treeNode.Depth - 1)) + "]");
                    }
                    else
                    {
                        buffer.AppendLine(new string('\t', (treeNode.Depth - 1)) + treeNode.Name + " " + WriteValue(treeNode.Value));
                    }
                }
            }
        }

        private string WriteValue(object val)
        {
            string item;
            if (val is string)
            {
                item = "\"" + val + "\"";
            }
            else if (val is bool)
            {
                item = ((bool)val) ? "true" : "false";
            }
            else
            {
                item = val.ToString();
            }

            return item;
        }
    }
}
