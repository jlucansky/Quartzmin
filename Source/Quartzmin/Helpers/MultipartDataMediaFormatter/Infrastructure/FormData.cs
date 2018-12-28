#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MultipartDataMediaFormatter.Infrastructure
{
    public class FormData
    {
        private List<ValueFile> _Files;
        private List<ValueString> _Fields;

        public List<ValueFile> Files
        {
            get
            {
                if(_Files == null)
                    _Files = new List<ValueFile>();
                return _Files;
            }
            set
            {
                _Files = value;
            }
        }

        public List<ValueString> Fields
        {
            get
            {
                if(_Fields == null)
                    _Fields = new List<ValueString>();
                return _Fields;
            }
            set
            {
                _Fields = value;
            }
        }

        public List<string> GetAllKeys()
        {
            return Fields.Select(m => m.Name).Concat(Files.Select(m => m.Name)).ToList();
        }

        public void Add(string name, string value)
        {
            Fields.Add(new ValueString() { Name = name, Value = value});
        }

        public void Add(string name, HttpFile value)
        {
            Files.Add(new ValueFile() { Name = name, Value = value });
        }

        public bool TryGetValue(string name, CultureInfo culture, out string value)
        {
            var field = Fields.FirstOrDefault(m => culture.CompareInfo.Compare(m.Name, name, CompareOptions.IgnoreCase) == 0);
            if (field != null)
            {
                value = field.Value;
                return true;
            }
            value = null;
            return false;
        }

        public bool TryGetValue(string name, CultureInfo culture, out HttpFile value)
        {
            var field = Files.FirstOrDefault(m => culture.CompareInfo.Compare(m.Name, name, CompareOptions.IgnoreCase) == 0);
            if (field != null)
            {
                value = field.Value;
                return true;
            }
            value = null;
            return false;
        }

        public List<string> GetValues(string name, CultureInfo culture)
        {
            return Fields
                .Where(m => culture.CompareInfo.Compare(m.Name, name, CompareOptions.IgnoreCase) == 0)
                .Select(m => m.Value)
                .ToList();
        }

        public List<HttpFile> GetFiles(string name, CultureInfo culture)
        {
            return Files
                .Where(m => culture.CompareInfo.Compare(m.Name, name, CompareOptions.IgnoreCase) == 0)
                .Select(m => m.Value)
                .ToList();
        }

        public bool Contains(string name, CultureInfo culture)
        {
            string val;
            HttpFile file;

            return TryGetValue(name, culture, out val) || TryGetValue(name, culture, out file);
        }

        public class ValueString
        {
            public string Name { get; set; }
            public string Value { get; set; }

            public override string ToString()
            {
                return Name + " = " + Value;
            }
        }

        public class ValueFile
        {
            public string Name { get; set; }
            public HttpFile Value { get; set; }
        }
    }
}
#endif