using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OGS.HOCON;

namespace TWiME
{
    /// <summary>
    /// 读写HOCON格式的配置文件类
    /// </summary>
    public class Settings
    {
        private DictionaryReader _reader;
        private bool _readOnly;
        private string _path;

        public bool ReadOnly => _readOnly;

        public Settings(string path, bool isReadOnly = false)
        {
            _readOnly = isReadOnly;
            _path = path;
            _reader = new DictionaryReader(item =>
            {
                var content = File.ReadAllText(item);

                content = new Regex("\t+").Replace(content, " ");
                content = new Regex("\r\n{").Replace(content, "{");
                content = new Regex("( )*{( )*").Replace(content, "{");
                content = new Regex("( )*=( )*").Replace(content, "=");
                content = new Regex("( )*:( )*").Replace(content, ":");

                return content;
            });

            if (File.Exists(path))
            {
                _reader.Read(Path.Combine(path));
                _reader.Analysis();
            }
        }

        public T ReadSettingOrDefault<T>(T defaultVal, params string[] path)
        {
            string rawPath = path[0];
            if (path.Length > 1)
            {
                rawPath = string.Empty;
                foreach (var item in path)
                {
                    rawPath += item + ".";
                }

                rawPath = rawPath.Remove(rawPath.Length - 1);
            }

            if (_reader.Source.ContainsKey(rawPath))
            {
                try
                {
                    return (T)_reader.Source[rawPath];
                }
                catch (Exception)
                {
                    return defaultVal;
                }
            }
            else
            {
                this._reader.Source.Add(rawPath, defaultVal);
            }
            return defaultVal;
        }

        public bool Contains(string key)
        {
            return this._reader.Source.ContainsKey(key);
        }

        public void OverwriteWith(Settings userSettingsOverride)
        {
            foreach (var key in userSettingsOverride._reader.Source.Keys)
            {
                if (this._reader.Source.ContainsKey(key))
                {
                    this._reader.Source[key] = userSettingsOverride._reader.Source[key];
                }
                else
                {
                    this._reader.Source.Add(key, userSettingsOverride._reader.Source[key]);
                }
            }
        }

        public void Save()
        {
            using (var stream = new FileStream(_path, FileMode.Create))
            {
                new DictionaryWriter().WriteStream(stream, _reader.Source);
            }
        }

        public string ReadSetting(params string[] path)
        {
            string rawPath = path[0];
            if (path.Length > 1)
            {
                rawPath = string.Empty;
                foreach (var item in path)
                {
                    rawPath += item + ".";
                }

                rawPath = rawPath.Remove(rawPath.Length - 1);
            }
            if (_reader.Source.ContainsKey(rawPath))
            {
                try
                {
                    return _reader.Source[rawPath].ToString();
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }

            return string.Empty;
        }

        public List<string> KeysUnderSection(string path)
        {
            var node = this._reader.Source[path] as DictionaryReaderNode;

            if (node != null)
            {
                var list = node.Value.Keys.ToList();
                list.Reverse();
                return list;
            }

            return null;
        }

        public void AddSetting(object val, params string[] path)
        {
            string rawPath = path[0];
            if (path.Length > 1)
            {
                rawPath = string.Empty;
                foreach (var item in path)
                {
                    rawPath += item + ".";
                }

                rawPath = rawPath.Remove(rawPath.Length - 1);
            }

            if (_reader.Source.ContainsKey(rawPath))
            {
                _reader.Source[rawPath] = val;
            }
            else
            {
                _reader.Source.Add(rawPath, val);
            }
        }
    }
}
