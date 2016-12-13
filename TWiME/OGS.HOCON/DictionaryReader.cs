using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace OGS.HOCON
{
    public class DictionaryReaderNode : DynamicObject
    {
        public DictionaryReaderNode()
        {
            Value = new Dictionary<string, object>();
        }

        public object this[string key]
        {
            get
            {
                if (Value.ContainsKey(key))
                {
                    return Value[key];
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public IDictionary<string, object> Value { get; internal set; }

        public override bool TrySetMember
             (SetMemberBinder binder, object value)
        {
            if (!Value.ContainsKey(binder.Name))
                Value.Add(binder.Name, value);
            else
                Value[binder.Name] = value;

            return true;
        }

        public override bool TryGetMember
               (GetMemberBinder binder, out object result)
        {
            if (Value.ContainsKey(binder.Name))
            {
                result = Value[binder.Name];
                return true;
            }
            else
            {
                return base.TryGetMember(binder, out result);
            }
        }

        public override bool TryInvokeMember
           (InvokeMemberBinder binder, object[] args, out object result)
        {
            if (Value.ContainsKey(binder.Name)
                      && Value[binder.Name] is Delegate)
            {
                result = (Value[binder.Name] as Delegate).DynamicInvoke(args);
                return true;
            }
            else
            {
                return base.TryInvokeMember(binder, args, out result);
            }
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return Value.Keys;
        }
    }

    public class DictionaryReader : Reader<DictionaryReaderNode>
    {
        public IDictionary<string, object> Source { get; private set; }

        public DictionaryReader(ResolveSourceHandler resolveSource)
        {
            Source = new Dictionary<string, object>();

            ResolveSource += resolveSource;

            CreateOrUpdateValue += (path, value) => Source[path] = value;
            CreateOrUpdateNode += (path, node) => Source[path] = node;
            RemoveNode += path => Source.Remove(path);
            GetNodeOrValue += path => Source.ContainsKey(path) ? Source[path] : null;
            GetNodesOrValues += path => Source.Where(item => path == item.Key || item.Key.StartsWith(path + ".")).ToArray();
        }
        
        public void Analysis()
        {
            foreach (var sourceKey in Source.Keys.Reverse())
            {
                object node;
                if (Source.TryGetValue(sourceKey, out node) && node is DictionaryReaderNode)
                {
                    var readNode = (DictionaryReaderNode)node;
                    foreach (var key in Source.Keys.Reverse())
                    {
                        var temp = (sourceKey + ".");
                        if (key.Length > temp.Length && key.IndexOf('.', temp.Length) < 0
                            && key.StartsWith(temp))
                        {
                            var newKey = key.Replace(temp, "");
                            if (readNode.Value.ContainsKey(newKey))
                            {
                                readNode.Value[newKey] = Source[key];
                            }
                            else
                            {
                                readNode.Value.Add(newKey, Source[key]);
                            }
                        }
                    }
                }
            }
        }
    }
}