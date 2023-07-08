using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Collections;

namespace PandaFileImporter
{
    public class PandaHeaderDictionary : IHeaderDictionary
    {
        private readonly Dictionary<string, StringValues> _headers;

        public PandaHeaderDictionary()
        {
            _headers = new Dictionary<string, StringValues>();
        }

        public StringValues this[string key]
        {
            get
            {
                _headers.TryGetValue(key, out var values);
                return values;
            }
            set
            {
                _headers[key] = value;
            }
        }

        public long? ContentLength { get; set; }

        public ICollection<string> Keys => _headers.Keys;

        public ICollection<StringValues> Values => _headers.Values;

        public int Count => _headers.Count;

        public bool IsReadOnly => false;

        public void Add(string key, StringValues value)
        {
            _headers.Add(key, value);
        }

        public void Add(KeyValuePair<string, StringValues> item)
        {
            _headers.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _headers.Clear();
        }

        public bool Contains(KeyValuePair<string, StringValues> item)
        {
            return _headers.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _headers.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            foreach (var item in _headers)
            {
                array[arrayIndex++] = item;
            }
        }

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        {
            return _headers.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _headers.Remove(key);
        }

        public bool Remove(KeyValuePair<string, StringValues> item)
        {
            return _headers.Remove(item.Key);
        }

        public bool TryGetValue(string key, out StringValues value)
        {
            return _headers.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
