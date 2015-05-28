// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
// Copyright (c) Damian Hickey. All rights reserved. See License.txt in the project root for license information.

// https://github.com/damianh/LibOwin
// Modifying this file may result in difficulties when upgrading the package.
// All types are internal. Add a LIBOWIN_PUBLIC compilation symbol to make them public.

namespace Hangfire.LibOwin.Infrastructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    internal static partial class Constants
    {
        internal const string Https = "HTTPS";

        internal const string HttpDateFormat = "r";

        internal static partial class Headers
        {
            internal const string ContentType = "Content-Type";
            internal const string CacheControl = "Cache-Control";
            internal const string MediaType = "Media-Type";
            internal const string Accept = "Accept";
            internal const string Host = "Host";
            internal const string ETag = "ETag";
            internal const string Location = "Location";
            internal const string ContentLength = "Content-Length";
            internal const string SetCookie = "Set-Cookie";
            internal const string Expires = "Expires";
        }
    }

    internal struct HeaderSegment : IEquatable<HeaderSegment>
    {
        private readonly StringSegment _formatting;
        private readonly StringSegment _data;

        // <summary>
        // Initializes a new instance of the <see cref="T:System.Object"/> class.
        // </summary>
        public HeaderSegment(StringSegment formatting, StringSegment data)
        {
            _formatting = formatting;
            _data = data;
        }

        public StringSegment Formatting
        {
            get { return _formatting; }
        }

        public StringSegment Data
        {
            get { return _data; }
        }

        #region Equality members

        public bool Equals(HeaderSegment other)
        {
            return _formatting.Equals(other._formatting) && _data.Equals(other._data);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is HeaderSegment && Equals((HeaderSegment)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_formatting.GetHashCode() * 397) ^ _data.GetHashCode();
            }
        }

        public static bool operator ==(HeaderSegment left, HeaderSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HeaderSegment left, HeaderSegment right)
        {
            return !left.Equals(right);
        }

        #endregion
    }

    internal struct HeaderSegmentCollection : IEnumerable<HeaderSegment>, IEquatable<HeaderSegmentCollection>
    {
        private readonly string[] _headers;

        public HeaderSegmentCollection(string[] headers)
        {
            _headers = headers;
        }

        #region Equality members

        public bool Equals(HeaderSegmentCollection other)
        {
            return Equals(_headers, other._headers);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is HeaderSegmentCollection && Equals((HeaderSegmentCollection)obj);
        }

        public override int GetHashCode()
        {
            return (_headers != null ? _headers.GetHashCode() : 0);
        }

        public static bool operator ==(HeaderSegmentCollection left, HeaderSegmentCollection right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HeaderSegmentCollection left, HeaderSegmentCollection right)
        {
            return !left.Equals(right);
        }

        #endregion

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_headers);
        }

        IEnumerator<HeaderSegment> IEnumerable<HeaderSegment>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal struct Enumerator : IEnumerator<HeaderSegment>
        {
            private readonly string[] _headers;
            private int _index;

            private string _header;
            private int _headerLength;
            private int _offset;

            private int _leadingStart;
            private int _leadingEnd;
            private int _valueStart;
            private int _valueEnd;
            private int _trailingStart;

            private Mode _mode;

            private static readonly string[] NoHeaders = new string[0];

            public Enumerator(string[] headers)
            {
                _headers = headers ?? NoHeaders;
                _header = string.Empty;
                _headerLength = -1;
                _index = -1;
                _offset = -1;
                _leadingStart = -1;
                _leadingEnd = -1;
                _valueStart = -1;
                _valueEnd = -1;
                _trailingStart = -1;
                _mode = Mode.Leading;
            }

            private enum Mode
            {
                Leading,
                Value,
                ValueQuoted,
                Trailing,
                Produce,
            }

            private enum Attr
            {
                Value,
                Quote,
                Delimiter,
                Whitespace
            }

            public HeaderSegment Current
            {
                get
                {
                    return new HeaderSegment(
                        new StringSegment(_header, _leadingStart, _leadingEnd - _leadingStart),
                        new StringSegment(_header, _valueStart, _valueEnd - _valueStart));
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                while (true)
                {
                    if (_mode == Mode.Produce)
                    {
                        _leadingStart = _trailingStart;
                        _leadingEnd = -1;
                        _valueStart = -1;
                        _valueEnd = -1;
                        _trailingStart = -1;

                        if (_offset == _headerLength &&
                            _leadingStart != -1 &&
                            _leadingStart != _offset)
                        {
                            // Also produce trailing whitespace
                            _leadingEnd = _offset;
                            return true;
                        }
                        _mode = Mode.Leading;
                    }

                    // if end of a string
                    if (_offset == _headerLength)
                    {
                        ++_index;
                        _offset = -1;
                        _leadingStart = 0;
                        _leadingEnd = -1;
                        _valueStart = -1;
                        _valueEnd = -1;
                        _trailingStart = -1;

                        // if that was the last string
                        if (_index == _headers.Length)
                        {
                            // no more move nexts
                            return false;
                        }

                        // grab the next string
                        _header = _headers[_index] ?? string.Empty;
                        _headerLength = _header.Length;
                    }
                    while (true)
                    {
                        ++_offset;
                        char ch = _offset == _headerLength ? (char)0 : _header[_offset];
                        // todo - array of attrs
                        Attr attr = char.IsWhiteSpace(ch) ? Attr.Whitespace : ch == '\"' ? Attr.Quote : (ch == ',' || ch == (char)0) ? Attr.Delimiter : Attr.Value;

                        switch (_mode)
                        {
                            case Mode.Leading:
                                switch (attr)
                                {
                                    case Attr.Delimiter:
                                        _leadingEnd = _offset;
                                        _mode = Mode.Produce;
                                        break;
                                    case Attr.Quote:
                                        _leadingEnd = _offset;
                                        _valueStart = _offset;
                                        _mode = Mode.ValueQuoted;
                                        break;
                                    case Attr.Value:
                                        _leadingEnd = _offset;
                                        _valueStart = _offset;
                                        _mode = Mode.Value;
                                        break;
                                    case Attr.Whitespace:
                                        // more
                                        break;
                                }
                                break;
                            case Mode.Value:
                                switch (attr)
                                {
                                    case Attr.Quote:
                                        _mode = Mode.ValueQuoted;
                                        break;
                                    case Attr.Delimiter:
                                        _valueEnd = _offset;
                                        _trailingStart = _offset;
                                        _mode = Mode.Produce;
                                        break;
                                    case Attr.Value:
                                        // more
                                        break;
                                    case Attr.Whitespace:
                                        _valueEnd = _offset;
                                        _trailingStart = _offset;
                                        _mode = Mode.Trailing;
                                        break;
                                }
                                break;
                            case Mode.ValueQuoted:
                                switch (attr)
                                {
                                    case Attr.Quote:
                                        _mode = Mode.Value;
                                        break;
                                    case Attr.Delimiter:
                                        if (ch == (char)0)
                                        {
                                            _valueEnd = _offset;
                                            _trailingStart = _offset;
                                            _mode = Mode.Produce;
                                        }
                                        break;
                                    case Attr.Value:
                                    case Attr.Whitespace:
                                        // more
                                        break;
                                }
                                break;
                            case Mode.Trailing:
                                switch (attr)
                                {
                                    case Attr.Delimiter:
                                        _mode = Mode.Produce;
                                        break;
                                    case Attr.Quote:
                                        // back into value
                                        _trailingStart = -1;
                                        _valueEnd = -1;
                                        _mode = Mode.ValueQuoted;
                                        break;
                                    case Attr.Value:
                                        // back into value
                                        _trailingStart = -1;
                                        _valueEnd = -1;
                                        _mode = Mode.Value;
                                        break;
                                    case Attr.Whitespace:
                                        // more
                                        break;
                                }
                                break;
                        }
                        if (_mode == Mode.Produce)
                        {
                            return true;
                        }
                    }
                }
            }

            public void Reset()
            {
                _index = 0;
                _offset = 0;
                _leadingStart = 0;
                _leadingEnd = 0;
                _valueStart = 0;
                _valueEnd = 0;
            }
        }
    }

    internal struct StringSegment : IEquatable<StringSegment>
    {
        private readonly string _buffer;
        private readonly int _offset;
        private readonly int _count;

        // <summary>
        // Initializes a new instance of the <see cref="T:System.Object"/> class.
        // </summary>
        public StringSegment(string buffer, int offset, int count)
        {
            _buffer = buffer;
            _offset = offset;
            _count = count;
        }

        public string Buffer
        {
            get { return _buffer; }
        }

        public int Offset
        {
            get { return _offset; }
        }

        public int Count
        {
            get { return _count; }
        }

        public string Value
        {
            get { return _offset == -1 ? null : _buffer.Substring(_offset, _count); }
        }

        public bool HasValue
        {
            get { return _offset != -1 && _count != 0 && _buffer != null; }
        }

        #region Equality members

        public bool Equals(StringSegment other)
        {
            return string.Equals(_buffer, other._buffer) && _offset == other._offset && _count == other._count;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is StringSegment && Equals((StringSegment)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (_buffer != null ? _buffer.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _offset;
                hashCode = (hashCode * 397) ^ _count;
                return hashCode;
            }
        }

        public static bool operator ==(StringSegment left, StringSegment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringSegment left, StringSegment right)
        {
            return !left.Equals(right);
        }

        #endregion

        public bool StartsWith(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            int textLength = text.Length;
            if (!HasValue || _count < textLength)
            {
                return false;
            }

            return string.Compare(_buffer, _offset, text, 0, textLength, comparisonType) == 0;
        }

        public bool EndsWith(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            int textLength = text.Length;
            if (!HasValue || _count < textLength)
            {
                return false;
            }

            return string.Compare(_buffer, _offset + _count - textLength, text, 0, textLength, comparisonType) == 0;
        }

        public bool Equals(string text, StringComparison comparisonType)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            int textLength = text.Length;
            if (!HasValue || _count != textLength)
            {
                return false;
            }

            return string.Compare(_buffer, _offset, text, 0, textLength, comparisonType) == 0;
        }

        public string Substring(int offset, int length)
        {
            return _buffer.Substring(_offset + offset, length);
        }

        public StringSegment Subsegment(int offset, int length)
        {
            return new StringSegment(_buffer, _offset + offset, length);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }

    internal static partial class OwinHelpers
    {
        private static readonly Action<string, string, object> AddCookieCallback = (name, value, state) =>
        {
            var dictionary = (IDictionary<string, string>)state;
            if (!dictionary.ContainsKey(name))
            {
                dictionary.Add(name, value);
            }
        };

        private static readonly char[] SemicolonAndComma = new[] { ';', ',' };

        internal static IDictionary<string, string> GetCookies(IOwinRequest request)
        {
            var cookies = request.Get<IDictionary<string, string>>("Microsoft.Owin.Cookies#dictionary");
            if (cookies == null)
            {
                cookies = new Dictionary<string, string>(StringComparer.Ordinal);
                request.Set("Microsoft.Owin.Cookies#dictionary", cookies);
            }

            string text = GetHeader(request.Headers, "Cookie");
            if (request.Get<string>("Microsoft.Owin.Cookies#text") != text)
            {
                cookies.Clear();
                ParseDelimited(text, SemicolonAndComma, AddCookieCallback, cookies);
                request.Set("Microsoft.Owin.Cookies#text", text);
            }
            return cookies;
        }

        internal static void ParseDelimited(string text, char[] delimiters, Action<string, string, object> callback, object state)
        {
            int textLength = text.Length;
            int equalIndex = text.IndexOf('=');
            if (equalIndex == -1)
            {
                equalIndex = textLength;
            }
            int scanIndex = 0;
            while (scanIndex < textLength)
            {
                int delimiterIndex = text.IndexOfAny(delimiters, scanIndex);
                if (delimiterIndex == -1)
                {
                    delimiterIndex = textLength;
                }
                if (equalIndex < delimiterIndex)
                {
                    while (scanIndex != equalIndex && char.IsWhiteSpace(text[scanIndex]))
                    {
                        ++scanIndex;
                    }
                    string name = text.Substring(scanIndex, equalIndex - scanIndex);
                    string value = text.Substring(equalIndex + 1, delimiterIndex - equalIndex - 1);
                    callback(
                        Uri.UnescapeDataString(name.Replace('+', ' ')),
                        Uri.UnescapeDataString(value.Replace('+', ' ')),
                        state);
                    equalIndex = text.IndexOf('=', delimiterIndex);
                    if (equalIndex == -1)
                    {
                        equalIndex = textLength;
                    }
                }
                scanIndex = delimiterIndex + 1;
            }
        }
    }

    internal static partial class OwinHelpers
    {
        public static string GetHeader(IDictionary<string, string[]> headers, string key)
        {
            string[] values = GetHeaderUnmodified(headers, key);
            return values == null ? null : string.Join(",", values);
        }

        public static IEnumerable<string> GetHeaderSplit(IDictionary<string, string[]> headers, string key)
        {
            string[] values = GetHeaderUnmodified(headers, key);
            return values == null ? null : GetHeaderSplitImplementation(values);
        }

        private static IEnumerable<string> GetHeaderSplitImplementation(string[] values)
        {
            foreach (var segment in new HeaderSegmentCollection(values))
            {
                if (segment.Data.HasValue)
                {
                    yield return DeQuote(segment.Data.Value);
                }
            }
        }

        public static string[] GetHeaderUnmodified(IDictionary<string, string[]> headers, string key)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }
            string[] values;
            return headers.TryGetValue(key, out values) ? values : null;
        }

        public static void SetHeader(IDictionary<string, string[]> headers, string key, string value)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                headers.Remove(key);
            }
            else
            {
                headers[key] = new[] { value };
            }
        }

        public static void SetHeaderJoined(IDictionary<string, string[]> headers, string key, params string[] values)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (values == null || values.Length == 0)
            {
                headers.Remove(key);
            }
            else
            {
                headers[key] = new[] { string.Join(",", values.Select(value => QuoteIfNeeded(value))) };
            }
        }

        // Quote items that contain comas and are not already quoted.
        private static string QuoteIfNeeded(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Ignore
            }
            else if (value.Contains(','))
            {
                if (value[0] != '"' || value[value.Length - 1] != '"')
                {
                    value = '"' + value + '"';
                }
            }

            return value;
        }

        private static string DeQuote(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Ignore
            }
            else if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                value = value.Substring(1, value.Length - 2);
            }

            return value;
        }

        public static void SetHeaderUnmodified(IDictionary<string, string[]> headers, string key, params string[] values)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (values == null || values.Length == 0)
            {
                headers.Remove(key);
            }
            else
            {
                headers[key] = values;
            }
        }

        public static void SetHeaderUnmodified(IDictionary<string, string[]> headers, string key, IEnumerable<string> values)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }
            headers[key] = values.ToArray();
        }

        public static void AppendHeader(IDictionary<string, string[]> headers, string key, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
            {
                return;
            }

            string existing = GetHeader(headers, key);
            if (existing == null)
            {
                SetHeader(headers, key, values);
            }
            else
            {
                headers[key] = new[] { existing + "," + values };
            }
        }

        public static void AppendHeaderJoined(IDictionary<string, string[]> headers, string key, params string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return;
            }

            string existing = GetHeader(headers, key);
            if (existing == null)
            {
                SetHeaderJoined(headers, key, values);
            }
            else
            {
                headers[key] = new[] { existing + "," + string.Join(",", values.Select(value => QuoteIfNeeded(value))) };
            }
        }

        public static void AppendHeaderUnmodified(IDictionary<string, string[]> headers, string key, params string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return;
            }

            string[] existing = GetHeaderUnmodified(headers, key);
            if (existing == null)
            {
                SetHeaderUnmodified(headers, key, values);
            }
            else
            {
                SetHeaderUnmodified(headers, key, existing.Concat(values));
            }
        }
    }

    internal static partial class OwinHelpers
    {
        private static readonly Action<string, string, object> AppendItemCallback = (name, value, state) =>
        {
            var dictionary = (IDictionary<string, List<String>>)state;

            List<string> existing;
            if (!dictionary.TryGetValue(name, out existing))
            {
                dictionary.Add(name, new List<string>(1) { value });
            }
            else
            {
                existing.Add(value);
            }
        };

        private static readonly char[] AmpersandAndSemicolon = new[] { '&', ';' };

        internal static IDictionary<string, string[]> GetQuery(IOwinRequest request)
        {
            var query = request.Get<IDictionary<string, string[]>>("Microsoft.Owin.Query#dictionary");
            if (query == null)
            {
                query = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                request.Set("Microsoft.Owin.Query#dictionary", query);
            }

            string text = request.QueryString.Value;
            if (request.Get<string>("Microsoft.Owin.Query#text") != text)
            {
                query.Clear();
                var accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                ParseDelimited(text, AmpersandAndSemicolon, AppendItemCallback, accumulator);
                foreach (var kv in accumulator)
                {
                    query.Add(kv.Key, kv.Value.ToArray());
                }
                request.Set("Microsoft.Owin.Query#text", text);
            }
            return query;
        }

        internal static IFormCollection GetForm(string text)
        {
            IDictionary<string, string[]> form = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            var accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            ParseDelimited(text, new[] { '&' }, AppendItemCallback, accumulator);
            foreach (var kv in accumulator)
            {
                form.Add(kv.Key, kv.Value.ToArray());
            }
            return new FormCollection(form);
        }

        internal static string GetJoinedValue(IDictionary<string, string[]> store, string key)
        {
            string[] values = GetUnmodifiedValues(store, key);
            return values == null ? null : string.Join(",", values);
        }

        internal static string[] GetUnmodifiedValues(IDictionary<string, string[]> store, string key)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            string[] values;
            return store.TryGetValue(key, out values) ? values : null;
        }
    }

    internal static partial class OwinHelpers
    {
        internal static string GetHost(IOwinRequest request)
        {
            IHeaderDictionary headers = request.Headers;

            string host = GetHeader(headers, "Host");
            if (!string.IsNullOrWhiteSpace(host))
            {
                return host;
            }

            string localIpAddress = request.LocalIpAddress ?? "localhost";
            var localPort = request.Get<string>(OwinConstants.CommonKeys.LocalPort);
            return string.IsNullOrWhiteSpace(localPort) ? localIpAddress : (localIpAddress + ":" + localPort);
        }
    }
}

namespace Hangfire.LibOwin
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Hangfire.LibOwin.Infrastructure;

    /// <summary>
    /// Options used to create a new cookie.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    class CookieOptions
    {
        /// <summary>
        /// Creates a default cookie with a path of '/'.
        /// </summary>
        public CookieOptions()
        {
            Path = "/";
        }

        /// <summary>
        /// Gets or sets the domain to associate the cookie with.
        /// </summary>
        /// <returns>The domain to associate the cookie with.</returns>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the cookie path.
        /// </summary>
        /// <returns>The cookie path.</returns>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the expiration date and time for the cookie.
        /// </summary>
        /// <returns>The expiration date and time for the cookie.</returns>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether to transmit the cookie using Secure Sockets Layer (SSL)�that is, over HTTPS only.
        /// </summary>
        /// <returns>true to transmit the cookie only over an SSL connection (HTTPS); otherwise, false.</returns>
        public bool Secure { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether a cookie is accessible by client-side script.
        /// </summary>
        /// <returns>true if a cookie is accessible by client-side script; otherwise, false.</returns>
        public bool HttpOnly { get; set; }
    }

    /// <summary>
    /// Contains the parsed form values.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    class FormCollection : ReadableStringCollection, IFormCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Owin.FormCollection" /> class.
        /// </summary>
        /// <param name="store">The store for the form.</param>
        public FormCollection(IDictionary<string, string[]> store)
            : base(store)
        {}
    }

    /// <summary>
    /// Represents a wrapper for owin.RequestHeaders and owin.ResponseHeaders.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    class HeaderDictionary : IHeaderDictionary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Owin.HeaderDictionary" /> class.
        /// </summary>
        /// <param name="store">The underlying data store.</param>
        public HeaderDictionary(IDictionary<string, string[]> store)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            Store = store;
        }

        private IDictionary<string, string[]> Store { get; set; }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection" /> that contains the keys in the <see cref="T:Microsoft.Owin.HeaderDictionary" />;.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.ICollection" /> that contains the keys in the <see cref="T:Microsoft.Owin.HeaderDictionary" />.</returns>
        public ICollection<string> Keys
        {
            get { return Store.Keys; }
        }

        /// <summary>
        ///
        /// </summary>
        public ICollection<string[]> Values
        {
            get { return Store.Values; }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:Microsoft.Owin.HeaderDictionary" />;.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:Microsoft.Owin.HeaderDictionary" />.</returns>
        public int Count
        {
            get { return Store.Count; }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="T:Microsoft.Owin.HeaderDictionary" /> is in read-only mode.
        /// </summary>
        /// <returns>true if the <see cref="T:Microsoft.Owin.HeaderDictionary" /> is in read-only mode; otherwise, false.</returns>
        public bool IsReadOnly
        {
            get { return Store.IsReadOnly; }
        }

        /// <summary>
        /// Get or sets the associated value from the collection as a single string.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>the associated value from the collection as a single string or null if the key is not present.</returns>
        public string this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        /// <summary>
        /// Throws KeyNotFoundException if the key is not present.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns></returns>
        string[] IDictionary<string, string[]>.this[string key]
        {
            get { return Store[key]; }
            set { Store[key] = value; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            return Store.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get the associated value from the collection as a single string.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>the associated value from the collection as a single string or null if the key is not present.</returns>
        public string Get(string key)
        {
            return OwinHelpers.GetHeader(Store, key);
        }

        /// <summary>
        /// Get the associated values from the collection without modification.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>the associated value from the collection without modification, or null if the key is not present.</returns>
        public IList<string> GetValues(string key)
        {
            return OwinHelpers.GetHeaderUnmodified(Store, key);
        }

        /// <summary>
        /// Get the associated values from the collection separated into individual values.
        /// Quoted values will not be split, and the quotes will be removed.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>the associated values from the collection separated into individual values, or null if the key is not present.</returns>
        public IList<string> GetCommaSeparatedValues(string key)
        {
            IEnumerable<string> values = OwinHelpers.GetHeaderSplit(Store, key);
            return values == null ? null : values.ToList();
        }

        /// <summary>
        /// Add a new value. Appends to the header if already present
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The header value.</param>
        public void Append(string key, string value)
        {
            OwinHelpers.AppendHeader(Store, key, value);
        }

        /// <summary>
        /// Add new values. Each item remains a separate array entry.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="values">The header values.</param>
        public void AppendValues(string key, params string[] values)
        {
            OwinHelpers.AppendHeaderUnmodified(Store, key, values);
        }

        /// <summary>
        /// Quotes any values containing comas, and then coma joins all of the values with any existing values.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="values">The header values.</param>
        public void AppendCommaSeparatedValues(string key, params string[] values)
        {
            OwinHelpers.AppendHeaderJoined(Store, key, values);
        }

        /// <summary>
        /// Sets a specific header value.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The header value.</param>
        public void Set(string key, string value)
        {
            OwinHelpers.SetHeader(Store, key, value);
        }

        /// <summary>
        /// Sets the specified header values without modification.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="values">The header values.</param>
        public void SetValues(string key, params string[] values)
        {
            OwinHelpers.SetHeaderUnmodified(Store, key, values);
        }

        /// <summary>
        /// Quotes any values containing comas, and then coma joins all of the values.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="values">The header values.</param>
        public void SetCommaSeparatedValues(string key, params string[] values)
        {
            OwinHelpers.SetHeaderJoined(Store, key, values);
        }

        /// <summary>
        /// Adds the given header and values to the collection.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The header values.</param>
        public void Add(string key, string[] value)
        {
            Store.Add(key, value);
        }

        /// <summary>
        /// Determines whether the <see cref="T:Microsoft.Owin.HeaderDictionary" /> contains a specific key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>true if the <see cref="T:Microsoft.Owin.HeaderDictionary" /> contains a specific key; otherwise, false.</returns>
        public bool ContainsKey(string key)
        {
            return Store.ContainsKey(key);
        }

        /// <summary>
        /// Removes the given header from the collection.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>true if the specified object was removed from the collection; otherwise, false.</returns>
        public bool Remove(string key)
        {
            return Store.Remove(key);
        }

        /// <summary>
        /// Retrieves a value from the dictionary.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the <see cref="T:Microsoft.Owin.HeaderDictionary" /> contains the key; otherwise, false.</returns>
        public bool TryGetValue(string key, out string[] value)
        {
            return Store.TryGetValue(key, out value);
        }

        /// <summary>
        /// Adds a new list of items to the collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(KeyValuePair<string, string[]> item)
        {
            Store.Add(item);
        }

        /// <summary>
        /// Clears the entire list of objects.
        /// </summary>
        public void Clear()
        {
            Store.Clear();
        }

        /// <summary>
        /// Returns a value indicating whether the specified object occurs within this collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>true if the specified object occurs within this collection; otherwise, false.</returns>
        public bool Contains(KeyValuePair<string, string[]> item)
        {
            return Store.Contains(item);
        }

        /// <summary>
        /// Copies the <see cref="T:Microsoft.Owin.HeaderDictionary" /> elements to a one-dimensional Array instance at the specified index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the specified objects copied from the <see cref="T:Microsoft.Owin.HeaderDictionary" />.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex)
        {
            Store.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the given item from the the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>true if the specified object was removed from the collection; otherwise, false.</returns>
        public bool Remove(KeyValuePair<string, string[]> item)
        {
            return Store.Remove(item);
        }
    }

    /// <summary>
    /// Represents the host portion of a Uri can be used to construct Uri's properly formatted and encoded for use in
    /// HTTP headers.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    struct HostString : IEquatable<HostString>
    {
        private readonly string _value;

        /// <summary>
        /// Creates a new HostString without modification. The value should be Unicode rather than punycode, and may have a port.
        /// IPv4 and IPv6 addresses are also allowed, and also may have ports.
        /// </summary>
        /// <param name="value"></param>
        public HostString(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Returns the original value from the constructor.
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Returns the value as normalized by ToUriComponent().
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToUriComponent();
        }

        /// <summary>
        /// Returns the value properly formatted and encoded for use in a URI in a HTTP header.
        /// Any Unicode is converted to punycode. IPv6 addresses will have brackets added if they are missing.
        /// </summary>
        /// <returns></returns>
        public string ToUriComponent()
        {
            int index;
            if (string.IsNullOrEmpty(_value))
            {
                return string.Empty;
            }
            else if (_value.IndexOf('[') >= 0)
            {
                // IPv6 in brackets [::1], maybe with port
                return _value;
            }
            else if ((index = _value.IndexOf(':')) >= 0
                && index < _value.Length - 1
                && _value.IndexOf(':', index + 1) >= 0)
            {
                // IPv6 without brackets ::1 is the only type of host with 2 or more colons
                return "[" + _value + "]";
            }
            else if (index >= 0)
            {
                // Has a port
                string port = _value.Substring(index);
                var mapping = new IdnMapping();
                return mapping.GetAscii(_value, 0, index) + port;
            }
            else
            {
                var mapping = new IdnMapping();
                return mapping.GetAscii(_value);
            }
        }

        /// <summary>
        /// Creates a new HostString from the given uri component.
        /// Any punycode will be converted to Unicode.
        /// </summary>
        /// <param name="uriComponent"></param>
        /// <returns></returns>
        public static HostString FromUriComponent(string uriComponent)
        {
            if (!string.IsNullOrEmpty(uriComponent))
            {
                int index;
                if (uriComponent.IndexOf('[') >= 0)
                {
                    // IPv6 in brackets [::1], maybe with port
                }
                else if ((index = uriComponent.IndexOf(':')) >= 0
                    && index < uriComponent.Length - 1
                    && uriComponent.IndexOf(':', index + 1) >= 0)
                {
                    // IPv6 without brackets ::1 is the only type of host with 2 or more colons
                }
                else if (uriComponent.IndexOf("xn--", StringComparison.Ordinal) >= 0)
                {
                    // Contains punycode
                    if (index >= 0)
                    {
                        // Has a port
                        string port = uriComponent.Substring(index);
                        IdnMapping mapping = new IdnMapping();
                        uriComponent = mapping.GetUnicode(uriComponent, 0, index) + port;
                    }
                    else
                    {
                        IdnMapping mapping = new IdnMapping();
                        uriComponent = mapping.GetUnicode(uriComponent);
                    }
                }
            }
            return new HostString(uriComponent);
        }

        /// <summary>
        /// Creates a new HostString from the host and port of the give Uri instance.
        /// Punycode will be converted to Unicode.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static HostString FromUriComponent(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            return new HostString(uri.GetComponents(
                UriComponents.NormalizedHost | // Always convert punycode to Unicode.
                UriComponents.HostAndPort, UriFormat.Unescaped));
        }

        /// <summary>
        /// Compares the equality of the Value property, ignoring case.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(HostString other)
        {
            return string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares against the given object only if it is a HostString.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is HostString && Equals((HostString)obj);
        }

        /// <summary>
        /// Gets a hash code for the value.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (_value != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(_value) : 0);
        }

        /// <summary>
        /// Compares the two instances for equality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(HostString left, HostString right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the two instances for inequality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(HostString left, HostString right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Contains the parsed form values.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    interface IFormCollection : IReadableStringCollection
    {}

    /// <summary>
    /// Represents a wrapper for owin.RequestHeaders and owin.ResponseHeaders.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    interface IHeaderDictionary : IReadableStringCollection, IDictionary<string, string[]>
    {
        /// <summary>
        /// Get or sets the associated value from the collection as a single string.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>the associated value from the collection as a single string or null if the key is not present.</returns>
        new string this[string key] { get; set; }

        /// <summary>
        /// Get the associated values from the collection separated into individual values.
        /// Quoted values will not be split, and the quotes will be removed.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>the associated values from the collection separated into individual values, or null if the key is not present.</returns>
        IList<string> GetCommaSeparatedValues(string key);

        /// <summary>
        /// Add a new value. Appends to the header if already present
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The header value.</param>
        void Append(string key, string value);

        /// <summary>
        /// Add new values. Each item remains a separate array entry.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="values">The header values.</param>
        void AppendValues(string key, params string[] values);

        /// <summary>
        /// Quotes any values containing comas, and then coma joins all of the values with any existing values.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="values">The header values.</param>
        void AppendCommaSeparatedValues(string key, params string[] values);

        /// <summary>
        /// Sets a specific header value.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The header value.</param>
        void Set(string key, string value);

        /// <summary>
        /// Sets the specified header values without modification.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="values">The header values.</param>
        void SetValues(string key, params string[] values);

        /// <summary>
        /// Quotes any values containing comas, and then coma joins all of the values.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="values">The header values.</param>
        void SetCommaSeparatedValues(string key, params string[] values);
    }

    /// <summary>
    /// This wraps OWIN environment dictionary and provides strongly typed accessors.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    interface IOwinContext
    {
        /// <summary>
        /// Gets a wrapper exposing request specific properties.
        /// </summary>
        /// <returns>A wrapper exposing request specific properties.</returns>
        IOwinRequest Request { get; }

        /// <summary>
        /// Gets a wrapper exposing response specific properties.
        /// </summary>
        /// <returns>A wrapper exposing response specific properties.</returns>
        IOwinResponse Response { get; }

        /// <summary>
        /// Gets the OWIN environment.
        /// </summary>
        /// <returns>The OWIN environment.</returns>
        IDictionary<string, object> Environment { get; }

        /// <summary>
        /// Gets or sets the host.TraceOutput environment value.
        /// </summary>
        /// <returns>The host.TraceOutput TextWriter.</returns>
        TextWriter TraceOutput { get; set; }

        /// <summary>
        /// Gets a value from the OWIN environment, or returns default(T) if not present.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value with the specified key or the default(T) if not present.</returns>
        T Get<T>(string key);

        /// <summary>
        /// Sets the given key and value in the OWIN environment.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>This instance.</returns>
        IOwinContext Set<T>(string key, T value);
    }

    /// <summary>
    /// This wraps OWIN environment dictionary and provides strongly typed accessors.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    interface IOwinRequest
    {
        /// <summary>
        /// Gets the OWIN environment.
        /// </summary>
        /// <returns>The OWIN environment.</returns>
        IDictionary<string, object> Environment { get; }

        /// <summary>
        /// Gets the request context.
        /// </summary>
        /// <returns>The request context.</returns>
        IOwinContext Context { get; }

        /// <summary>
        /// Gets or set the HTTP method.
        /// </summary>
        /// <returns>The HTTP method.</returns>
        string Method { get; set; }

        /// <summary>
        /// Gets or set the HTTP request scheme from owin.RequestScheme.
        /// </summary>
        /// <returns>The HTTP request scheme from owin.RequestScheme.</returns>
        string Scheme { get; set; }

        /// <summary>
        /// Returns true if the owin.RequestScheme is https.
        /// </summary>
        /// <returns>true if this request is using https; otherwise, false.</returns>
        bool IsSecure { get; }

        /// <summary>
        /// Gets or set the Host header. May include the port.
        /// </summary>
        /// <return>The Host header.</return>
        HostString Host { get; set; }

        /// <summary>
        /// Gets or set the owin.RequestPathBase.
        /// </summary>
        /// <returns>The owin.RequestPathBase.</returns>
        PathString PathBase { get; set; }

        /// <summary>
        /// Gets or set the request path from owin.RequestPath.
        /// </summary>
        /// <returns>The request path from owin.RequestPath.</returns>
        PathString Path { get; set; }

        /// <summary>
        /// Gets or set the query string from owin.RequestQueryString.
        /// </summary>
        /// <returns>The query string from owin.RequestQueryString.</returns>
        QueryString QueryString { get; set; }

        /// <summary>
        /// Gets the query value collection parsed from owin.RequestQueryString.
        /// </summary>
        /// <returns>The query value collection parsed from owin.RequestQueryString.</returns>
        IReadableStringCollection Query { get; }

        /// <summary>
        /// Gets the uniform resource identifier (URI) associated with the request.
        /// </summary>
        /// <returns>The uniform resource identifier (URI) associated with the request.</returns>
        Uri Uri { get; }

        /// <summary>
        /// Gets or set the owin.RequestProtocol.
        /// </summary>
        /// <returns>The owin.RequestProtocol.</returns>
        string Protocol { get; set; }

        /// <summary>
        /// Gets the request headers.
        /// </summary>
        /// <returns>The request headers.</returns>
        IHeaderDictionary Headers { get; }

        /// <summary>
        /// Gets the collection of Cookies for this request.
        /// </summary>
        /// <returns>The collection of Cookies for this request.</returns>
        RequestCookieCollection Cookies { get; }

        /// <summary>
        /// Gets or sets the Content-Type header.
        /// </summary>
        /// <returns>The Content-Type header.</returns>
        string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the Cache-Control header.
        /// </summary>
        /// <returns>The Cache-Control header.</returns>
        string CacheControl { get; set; }

        /// <summary>
        /// Gets or sets the Media-Type header.
        /// </summary>
        /// <returns>The Media-Type header.</returns>
        string MediaType { get; set; }

        /// <summary>
        /// Gets or set the Accept header.
        /// </summary>
        /// <returns>The Accept header.</returns>
        string Accept { get; set; }

        /// <summary>
        /// Gets or set the owin.RequestBody Stream.
        /// </summary>
        /// <returns>The owin.RequestBody Stream.</returns>
        Stream Body { get; set; }

        /// <summary>
        /// Gets or sets the cancellation token for the request.
        /// </summary>
        /// <returns>The cancellation token for the request.</returns>
        CancellationToken CallCancelled { get; set; }

        /// <summary>
        /// Gets or set the server.LocalIpAddress.
        /// </summary>
        /// <returns>The server.LocalIpAddress.</returns>
        string LocalIpAddress { get; set; }

        /// <summary>
        /// Gets or set the server.LocalPort.
        /// </summary>
        /// <returns>The server.LocalPort.</returns>
        int? LocalPort { get; set; }

        /// <summary>
        /// Gets or set the server.RemoteIpAddress.
        /// </summary>
        /// <returns>The server.RemoteIpAddress.</returns>
        string RemoteIpAddress { get; set; }

        /// <summary>
        /// Gets or set the server.RemotePort.
        /// </summary>
        /// <returns>The server.RemotePort.</returns>
        int? RemotePort { get; set; }

        /// <summary>
        /// Gets or set the owin.RequestUser (or gets server.User for non-standard implementations).
        /// </summary>
        /// <returns>The server.User.</returns>
        ClaimsPrincipal User { get; set; }

        /// <summary>
        /// Asynchronously reads and parses the request body as a form.
        /// </summary>
        /// <returns>The parsed form data.</returns>
        Task<IFormCollection> ReadFormAsync();

        /// <summary>
        /// Gets a value from the OWIN environment, or returns default(T) if not present.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value with the specified key or the default(T) if not present.</returns>
        T Get<T>(string key);

        /// <summary>
        /// Sets the given key and value in the OWIN environment.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>This instance.</returns>
        IOwinRequest Set<T>(string key, T value);
    }

    /// <summary>
    /// This wraps OWIN environment dictionary and provides strongly typed accessors.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    interface IOwinResponse
    {
        /// <summary>
        /// Gets the OWIN environment.
        /// </summary>
        /// <returns>The OWIN environment.</returns>
        IDictionary<string, object> Environment { get; }

        /// <summary>
        /// Gets the request context.
        /// </summary>
        /// <returns>The request context.</returns>
        IOwinContext Context { get; }

        /// <summary>
        /// Gets or sets the optional owin.ResponseStatusCode.
        /// </summary>
        /// <returns>The optional owin.ResponseStatusCode, or 200 if not set.</returns>
        int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the the optional owin.ResponseReasonPhrase.
        /// </summary>
        /// <returns>The the optional owin.ResponseReasonPhrase.</returns>
        string ReasonPhrase { get; set; }

        /// <summary>
        /// Gets or sets the owin.ResponseProtocol.
        /// </summary>
        /// <returns>The owin.ResponseProtocol.</returns>
        string Protocol { get; set; }

        /// <summary>
        /// Gets the response header collection.
        /// </summary>
        /// <returns>The response header collection.</returns>
        IHeaderDictionary Headers { get; }

        /// <summary>
        /// Gets a collection used to manipulate the Set-Cookie header.
        /// </summary>
        /// <returns>A collection used to manipulate the Set-Cookie header.</returns>
        ResponseCookieCollection Cookies { get; }

        /// <summary>
        /// Gets or sets the Content-Length header.
        /// </summary>
        /// <returns>The Content-Length header.</returns>
        long? ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the Content-Type header.
        /// </summary>
        /// <returns>The Content-Type header.</returns>
        string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the Expires header.
        /// </summary>
        /// <returns>The Expires header.</returns>
        DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Gets or sets the E-Tag header.
        /// </summary>
        /// <returns>The E-Tag header.</returns>
        string ETag { get; set; }

        /// <summary>
        /// Gets or sets the owin.ResponseBody Stream.
        /// </summary>
        /// <returns>The owin.ResponseBody Stream.</returns>
        Stream Body { get; set; }

        /// <summary>
        /// Registers for an event that fires when the response headers are sent.
        /// </summary>
        /// <param name="callback">The callback method.</param>
        /// <param name="state">The callback state.</param>
        void OnSendingHeaders(Action<object> callback, object state);

        /// <summary>
        /// Sets a 302 response status code and the Location header.
        /// </summary>
        /// <param name="location">The location where to redirect the client.</param>
        void Redirect(string location);

        /// <summary>
        /// Writes the given text to the response body stream using UTF-8.
        /// </summary>
        /// <param name="text">The response data.</param>
        void Write(string text);

        /// <summary>
        /// Writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        void Write(byte[] data);

        /// <summary>
        /// Writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="offset">The zero-based byte offset in the <paramref name="data" /> parameter at which to begin copying bytes.</param>
        /// <param name="count">The number of bytes to write.</param>
        void Write(byte[] data, int offset, int count);

        /// <summary>
        /// Asynchronously writes the given text to the response body stream using UTF-8.
        /// </summary>
        /// <param name="text">The response data.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        Task WriteAsync(string text);

        /// <summary>
        /// Asynchronously writes the given text to the response body stream using UTF-8.
        /// </summary>
        /// <param name="text">The response data.</param>
        /// <param name="token">A token used to indicate cancellation.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        Task WriteAsync(string text, CancellationToken token);

        /// <summary>
        /// Asynchronously writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        Task WriteAsync(byte[] data);

        /// <summary>
        /// Asynchronously writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="token">A token used to indicate cancellation.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        Task WriteAsync(byte[] data, CancellationToken token);

        /// <summary>
        /// Asynchronously writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="offset">The zero-based byte offset in the <paramref name="data" /> parameter at which to begin copying bytes.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="token">A token used to indicate cancellation.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        Task WriteAsync(byte[] data, int offset, int count, CancellationToken token);

        /// <summary>
        /// Gets a value from the OWIN environment, or returns default(T) if not present.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value with the specified key or the default(T) if not present.</returns>
        T Get<T>(string key);

        /// <summary>
        /// Sets the given key and value in the OWIN environment.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>This instance.</returns>
        IOwinResponse Set<T>(string key, T value);
    }

    /// <summary>
    /// Accessors for headers, query, forms, etc.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    interface IReadableStringCollection : IEnumerable<KeyValuePair<string, string[]>>
    {
        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string this[string key] { get; }

        // Joined

        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string Get(string key);

        // Joined

        /// <summary>
        /// Get the associated values from the collection in their original format.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IList<string> GetValues(string key);

        // Raw
    }

    internal static class OwinConstants
    {
        #region OWIN v1.0.0 - 3.2.1. Request Data

        // http://owin.org/spec/owin-1.0.0.html

        public const string RequestScheme = "owin.RequestScheme";
        public const string RequestMethod = "owin.RequestMethod";
        public const string RequestPathBase = "owin.RequestPathBase";
        public const string RequestPath = "owin.RequestPath";
        public const string RequestQueryString = "owin.RequestQueryString";
        public const string RequestProtocol = "owin.RequestProtocol";
        public const string RequestHeaders = "owin.RequestHeaders";
        public const string RequestBody = "owin.RequestBody";
        public const string RequestUser = "owin.RequestUser"; //owin 1.0.1

        #endregion

        #region OWIN v1.0.0 - 3.2.2. Response Data

        // http://owin.org/spec/owin-1.0.0.html

        public const string ResponseStatusCode = "owin.ResponseStatusCode";
        public const string ResponseReasonPhrase = "owin.ResponseReasonPhrase";
        public const string ResponseProtocol = "owin.ResponseProtocol";
        public const string ResponseHeaders = "owin.ResponseHeaders";
        public const string ResponseBody = "owin.ResponseBody";

        #endregion

        #region OWIN v1.0.0 - 3.2.3. Other Data

        // http://owin.org/spec/owin-1.0.0.html

        public const string CallCancelled = "owin.CallCancelled";

        public const string OwinVersion = "owin.Version";

        #endregion

        #region OWIN Keys for IAppBuilder.Properties

        internal static class Builder
        {
            public const string AddSignatureConversion = "builder.AddSignatureConversion";
            public const string DefaultApp = "builder.DefaultApp";
        }

        #endregion

        #region OWIN Key Guidelines and Common Keys - 6. Common keys

        // http://owin.org/spec/CommonKeys.html

        internal static class CommonKeys
        {
            public const string ClientCertificate = "ssl.ClientCertificate";
            public const string RemoteIpAddress = "server.RemoteIpAddress";
            public const string RemotePort = "server.RemotePort";
            public const string LocalIpAddress = "server.LocalIpAddress";
            public const string LocalPort = "server.LocalPort";
            public const string IsLocal = "server.IsLocal";
            public const string TraceOutput = "host.TraceOutput";
            public const string Addresses = "host.Addresses";
            public const string AppName = "host.AppName";
            public const string Capabilities = "server.Capabilities";
            public const string OnSendingHeaders = "server.OnSendingHeaders";
            public const string OnAppDisposing = "host.OnAppDisposing";
            public const string Scheme = "scheme";
            public const string Host = "host";
            public const string Port = "port";
            public const string Path = "path";
        }

        #endregion

        #region SendFiles v0.3.0

        // http://owin.org/extensions/owin-SendFile-Extension-v0.3.0.htm

        internal static class SendFiles
        {
            // 3.1. Startup

            public const string Version = "sendfile.Version";
            public const string Support = "sendfile.Support";
            public const string Concurrency = "sendfile.Concurrency";

            // 3.2. Per Request

            public const string SendAsync = "sendfile.SendAsync";
        }

        #endregion

        #region Opaque v0.3.0

        // http://owin.org/extensions/owin-OpaqueStream-Extension-v0.3.0.htm

        internal static class OpaqueConstants
        {
            // 3.1. Startup
            public const string Version = "opaque.Version";

            // 3.2. Per Request
            public const string Upgrade = "opaque.Upgrade";

            // 5. Consumption
            public const string Stream = "opaque.Stream";
            // public const string Version = "opaque.Version"; // redundant, declared above
            public const string CallCancelled = "opaque.CallCancelled";
        }

        #endregion

        #region WebSocket v0.4.0

        // http://owin.org/extensions/owin-OpaqueStream-Extension-v0.3.0.htm

        internal static class WebSocket
        {
            // 3.1. Startup
            public const string Version = "websocket.Version";

            // 3.2. Per Request
            public const string Accept = "websocket.Accept";

            // 4. Accept
            public const string SubProtocol = "websocket.SubProtocol";

            // 5. Consumption
            public const string SendAsync = "websocket.SendAsync";
            public const string ReceiveAsync = "websocket.ReceiveAsync";
            public const string CloseAsync = "websocket.CloseAsync";
            // public const string Version = "websocket.Version"; // redundant, declared above
            public const string CallCancelled = "websocket.CallCancelled";
            public const string ClientCloseStatus = "websocket.ClientCloseStatus";
            public const string ClientCloseDescription = "websocket.ClientCloseDescription";
        }

        #endregion

        #region Security v0.1.0

        // http://owin.org/extensions/owin-Security-Extension-v0.1.0.htm

        internal static class Security
        {
            // 3.2. Per Request

            public const string User = "server.User";

            public const string Authenticate = "security.Authenticate";

            // 3.3. Response

            public const string SignIn = "security.SignIn";

            public const string SignOut = "security.SignOut";

            public const string SignOutProperties = "security.SignOutProperties";

            public const string Challenge = "security.Challenge";
        }

        #endregion
    }

    /// <summary>
    /// This wraps OWIN environment dictionary and provides strongly typed accessors.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    class OwinContext : IOwinContext
    {
        /// <summary>
        /// Create a new context with only request and response header collections.
        /// </summary>
        public OwinContext()
        {
            IDictionary<string, object> environment = new Dictionary<string, object>(StringComparer.Ordinal);
            environment[OwinConstants.RequestHeaders] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            environment[OwinConstants.ResponseHeaders] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            Environment = environment;
            Request = new OwinRequest(environment);
            Response = new OwinResponse(environment);
        }

        /// <summary>
        /// Create a new wrapper.
        /// </summary>
        /// <param name="environment">OWIN environment dictionary which stores state information about the request, response and relevant server state.</param>
        public OwinContext(IDictionary<string, object> environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException("environment");
            }

            Environment = environment;
            Request = new OwinRequest(environment);
            Response = new OwinResponse(environment);
        }

        /// <summary>
        /// Gets a wrapper exposing request specific properties.
        /// </summary>
        /// <returns>A wrapper exposing request specific properties.</returns>
        public virtual IOwinRequest Request { get; private set; }

        /// <summary>
        /// Gets a wrapper exposing response specific properties.
        /// </summary>
        /// <returns>A wrapper exposing response specific properties.</returns>
        public virtual IOwinResponse Response { get; private set; }

        /// <summary>
        /// Gets the OWIN environment.
        /// </summary>
        /// <returns>The OWIN environment.</returns>
        public virtual IDictionary<string, object> Environment { get; private set; }

        /// <summary>
        /// Gets or sets the host.TraceOutput environment value.
        /// </summary>
        /// <returns>The host.TraceOutput TextWriter.</returns>
        public virtual TextWriter TraceOutput
        {
            get { return Get<TextWriter>(OwinConstants.CommonKeys.TraceOutput); }
            set { Set<TextWriter>(OwinConstants.CommonKeys.TraceOutput, value); }
        }

        /// <summary>
        /// Gets a value from the OWIN environment, or returns default(T) if not present.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value with the specified key or the default(T) if not present.</returns>
        public virtual T Get<T>(string key)
        {
            object value;
            return Environment.TryGetValue(key, out value) ? (T)value : default(T);
        }

        /// <summary>
        /// Sets the given key and value in the OWIN environment.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>This instance.</returns>
        public virtual IOwinContext Set<T>(string key, T value)
        {
            Environment[key] = value;
            return this;
        }
    }

    /// <summary>
    /// This wraps OWIN environment dictionary and provides strongly typed accessors.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    class OwinRequest : IOwinRequest
    {
        /// <summary>
        /// Create a new context with only request and response header collections.
        /// </summary>
        public OwinRequest()
        {
            IDictionary<string, object> environment = new Dictionary<string, object>(StringComparer.Ordinal);
            environment[OwinConstants.RequestHeaders] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            environment[OwinConstants.ResponseHeaders] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            Environment = environment;
        }

        /// <summary>
        /// Create a new environment wrapper exposing request properties.
        /// </summary>
        /// <param name="environment">OWIN environment dictionary which stores state information about the request, response and relevant server state.</param>
        public OwinRequest(IDictionary<string, object> environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException("environment");
            }

            Environment = environment;
        }

        /// <summary>
        /// Gets the OWIN environment.
        /// </summary>
        /// <returns>The OWIN environment.</returns>
        public virtual IDictionary<string, object> Environment { get; private set; }

        /// <summary>
        /// Gets the request context.
        /// </summary>
        /// <returns>The request context.</returns>
        public virtual IOwinContext Context
        {
            get { return new OwinContext(Environment); }
        }

        /// <summary>
        /// Gets or set the HTTP method.
        /// </summary>
        /// <returns>The HTTP method.</returns>
        public virtual string Method
        {
            get { return Get<string>(OwinConstants.RequestMethod); }
            set { Set(OwinConstants.RequestMethod, value); }
        }

        /// <summary>
        /// Gets or set the HTTP request scheme from owin.RequestScheme.
        /// </summary>
        /// <returns>The HTTP request scheme from owin.RequestScheme.</returns>
        public virtual string Scheme
        {
            get { return Get<string>(OwinConstants.RequestScheme); }
            set { Set(OwinConstants.RequestScheme, value); }
        }

        /// <summary>
        /// Returns true if the owin.RequestScheme is https.
        /// </summary>
        /// <returns>true if this request is using https; otherwise, false.</returns>
        public virtual bool IsSecure
        {
            get { return string.Equals(Scheme, Constants.Https, StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>
        /// Gets or set the Host header. May include the port.
        /// </summary>
        /// <return>The Host header.</return>
        public virtual HostString Host
        {
            get { return new HostString(OwinHelpers.GetHost(this)); }
            set { OwinHelpers.SetHeader(RawHeaders, Constants.Headers.Host, value.Value); }
        }

        /// <summary>
        /// Gets or set the owin.RequestPathBase.
        /// </summary>
        /// <returns>The owin.RequestPathBase.</returns>
        public virtual PathString PathBase
        {
            get { return new PathString(Get<string>(OwinConstants.RequestPathBase)); }
            set { Set(OwinConstants.RequestPathBase, value.Value); }
        }

        /// <summary>
        /// Gets or set the request path from owin.RequestPath.
        /// </summary>
        /// <returns>The request path from owin.RequestPath.</returns>
        public virtual PathString Path
        {
            get { return new PathString(Get<string>(OwinConstants.RequestPath)); }
            set { Set(OwinConstants.RequestPath, value.Value); }
        }

        /// <summary>
        /// Gets or set the query string from owin.RequestQueryString.
        /// </summary>
        /// <returns>The query string from owin.RequestQueryString.</returns>
        public virtual QueryString QueryString
        {
            get { return new QueryString(Get<string>(OwinConstants.RequestQueryString)); }
            set { Set(OwinConstants.RequestQueryString, value.Value); }
        }

        /// <summary>
        /// Gets the query value collection parsed from owin.RequestQueryString.
        /// </summary>
        /// <returns>The query value collection parsed from owin.RequestQueryString.</returns>
        public virtual IReadableStringCollection Query
        {
            get { return new ReadableStringCollection(OwinHelpers.GetQuery(this)); }
        }

        /// <summary>
        /// Gets the uniform resource identifier (URI) associated with the request.
        /// </summary>
        /// <returns>The uniform resource identifier (URI) associated with the request.</returns>
        public virtual Uri Uri
        {
            get { return new Uri(Scheme + "://" + Host + PathBase + Path + QueryString); }
        }

        /// <summary>
        /// Gets or set the owin.RequestProtocol.
        /// </summary>
        /// <returns>The owin.RequestProtocol.</returns>
        public virtual string Protocol
        {
            get { return Get<string>(OwinConstants.RequestProtocol); }
            set { Set(OwinConstants.RequestProtocol, value); }
        }

        /// <summary>
        /// Gets the request headers.
        /// </summary>
        /// <returns>The request headers.</returns>
        public virtual IHeaderDictionary Headers
        {
            get { return new HeaderDictionary(RawHeaders); }
        }

        private IDictionary<string, string[]> RawHeaders
        {
            get { return Get<IDictionary<string, string[]>>(OwinConstants.RequestHeaders); }
        }

        /// <summary>
        /// Gets the collection of Cookies for this request.
        /// </summary>
        /// <returns>The collection of Cookies for this request.</returns>
        public RequestCookieCollection Cookies
        {
            get { return new RequestCookieCollection(OwinHelpers.GetCookies(this)); }
        }

        /// <summary>
        /// Gets or sets the Content-Type header.
        /// </summary>
        /// <returns>The Content-Type header.</returns>
        public virtual string ContentType
        {
            get { return OwinHelpers.GetHeader(RawHeaders, Constants.Headers.ContentType); }
            set { OwinHelpers.SetHeader(RawHeaders, Constants.Headers.ContentType, value); }
        }

        /// <summary>
        /// Gets or sets the Cache-Control header.
        /// </summary>
        /// <returns>The Cache-Control header.</returns>
        public virtual string CacheControl
        {
            get { return OwinHelpers.GetHeader(RawHeaders, Constants.Headers.CacheControl); }
            set { OwinHelpers.SetHeader(RawHeaders, Constants.Headers.CacheControl, value); }
        }

        /// <summary>
        /// Gets or sets the Media-Type header.
        /// </summary>
        /// <returns>The Media-Type header.</returns>
        public virtual string MediaType
        {
            get { return OwinHelpers.GetHeader(RawHeaders, Constants.Headers.MediaType); }
            set { OwinHelpers.SetHeader(RawHeaders, Constants.Headers.MediaType, value); }
        }

        /// <summary>
        /// Gets or set the Accept header.
        /// </summary>
        /// <returns>The Accept header.</returns>
        public virtual string Accept
        {
            get { return OwinHelpers.GetHeader(RawHeaders, Constants.Headers.Accept); }
            set { OwinHelpers.SetHeader(RawHeaders, Constants.Headers.Accept, value); }
        }

        /// <summary>
        /// Gets or set the owin.RequestBody Stream.
        /// </summary>
        /// <returns>The owin.RequestBody Stream.</returns>
        public virtual Stream Body
        {
            get { return Get<Stream>(OwinConstants.RequestBody); }
            set { Set(OwinConstants.RequestBody, value); }
        }

        /// <summary>
        /// Gets or sets the cancellation token for the request.
        /// </summary>
        /// <returns>The cancellation token for the request.</returns>
        public virtual CancellationToken CallCancelled
        {
            get { return Get<CancellationToken>(OwinConstants.CallCancelled); }
            set { Set(OwinConstants.CallCancelled, value); }
        }

        /// <summary>
        /// Gets or set the server.LocalIpAddress.
        /// </summary>
        /// <returns>The server.LocalIpAddress.</returns>
        public virtual string LocalIpAddress
        {
            get { return Get<string>(OwinConstants.CommonKeys.LocalIpAddress); }
            set { Set(OwinConstants.CommonKeys.LocalIpAddress, value); }
        }

        /// <summary>
        /// Gets or set the server.LocalPort.
        /// </summary>
        /// <returns>The server.LocalPort.</returns>
        public virtual int? LocalPort
        {
            get
            {
                int value;
                if (int.TryParse(LocalPortString, out value))
                {
                    return value;
                }
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    LocalPortString = value.Value.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    Environment.Remove(OwinConstants.CommonKeys.LocalPort);
                }
            }
        }

        private string LocalPortString
        {
            get { return Get<string>(OwinConstants.CommonKeys.LocalPort); }
            set { Set(OwinConstants.CommonKeys.LocalPort, value); }
        }

        /// <summary>
        /// Gets or set the server.RemoteIpAddress.
        /// </summary>
        /// <returns>The server.RemoteIpAddress.</returns>
        public virtual string RemoteIpAddress
        {
            get { return Get<string>(OwinConstants.CommonKeys.RemoteIpAddress); }
            set { Set(OwinConstants.CommonKeys.RemoteIpAddress, value); }
        }

        /// <summary>
        /// Gets or set the server.RemotePort.
        /// </summary>
        /// <returns>The server.RemotePort.</returns>
        public virtual int? RemotePort
        {
            get
            {
                int value;
                if (int.TryParse(RemotePortString, out value))
                {
                    return value;
                }
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    RemotePortString = value.Value.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    Environment.Remove(OwinConstants.CommonKeys.RemotePort);
                }
            }
        }

        private string RemotePortString
        {
            get { return Get<string>(OwinConstants.CommonKeys.RemotePort); }
            set { Set(OwinConstants.CommonKeys.RemotePort, value); }
        }

        /// <summary>
        /// Gets or set the owin.RequestUser (or gets server.User for non-standard implementations).
        /// </summary>
        /// <returns>The server.User.</returns>
        public virtual ClaimsPrincipal User
        {
            get
            {
                var claimsPrincipal = Get<ClaimsPrincipal>(OwinConstants.RequestUser);
                return claimsPrincipal ?? Get<IPrincipal>(OwinConstants.Security.User) as ClaimsPrincipal;
            }
            set { Set(OwinConstants.RequestUser, value); }
        }

        /// <summary>
        /// Asynchronously reads and parses the request body as a form.
        /// </summary>
        /// <returns>The parsed form data.</returns>
        public async Task<IFormCollection> ReadFormAsync()
        {
            var form = Get<IFormCollection>("Microsoft.Owin.Form#collection");
            if (form == null)
            {
                string text;
                // Don't close, it prevents re-winding.
                using (var reader = new StreamReader(Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4 * 1024, leaveOpen: true))
                {
                    text = await reader.ReadToEndAsync();
                }
                form = OwinHelpers.GetForm(text);
                Set("Microsoft.Owin.Form#collection", form);
            }

            return form;
        }

        /// <summary>
        /// Gets a value from the OWIN environment, or returns default(T) if not present.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value with the specified key or the default(T) if not present.</returns>
        public virtual T Get<T>(string key)
        {
            object value;
            return Environment.TryGetValue(key, out value) ? (T)value : default(T);
        }

        /// <summary>
        /// Sets the given key and value in the OWIN environment.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>This instance.</returns>
        public virtual IOwinRequest Set<T>(string key, T value)
        {
            Environment[key] = value;
            return this;
        }
    }

    /// <summary>
    /// This wraps OWIN environment dictionary and provides strongly typed accessors.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    class OwinResponse : IOwinResponse
    {
        /// <summary>
        /// Create a new context with only request and response header collections.
        /// </summary>
        public OwinResponse()
        {
            IDictionary<string, object> environment = new Dictionary<string, object>(StringComparer.Ordinal);
            environment[OwinConstants.RequestHeaders] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            environment[OwinConstants.ResponseHeaders] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            Environment = environment;
        }

        /// <summary>
        /// Creates a new environment wrapper exposing response properties.
        /// </summary>
        /// <param name="environment">OWIN environment dictionary which stores state information about the request, response and relevant server state.</param>
        public OwinResponse(IDictionary<string, object> environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException("environment");
            }

            Environment = environment;
        }

        /// <summary>
        /// Gets the OWIN environment.
        /// </summary>
        /// <returns>The OWIN environment.</returns>
        public virtual IDictionary<string, object> Environment { get; private set; }

        /// <summary>
        /// Gets the request context.
        /// </summary>
        /// <returns>The request context.</returns>
        public virtual IOwinContext Context
        {
            get { return new OwinContext(Environment); }
        }

        /// <summary>
        /// Gets or sets the optional owin.ResponseStatusCode.
        /// </summary>
        /// <returns>The optional owin.ResponseStatusCode, or 200 if not set.</returns>
        public virtual int StatusCode
        {
            get { return Get<int>(OwinConstants.ResponseStatusCode, 200); }
            set { Set(OwinConstants.ResponseStatusCode, value); }
        }

        /// <summary>
        /// Gets or sets the the optional owin.ResponseReasonPhrase.
        /// </summary>
        /// <returns>The the optional owin.ResponseReasonPhrase.</returns>
        public virtual string ReasonPhrase
        {
            get { return Get<string>(OwinConstants.ResponseReasonPhrase); }
            set { Set(OwinConstants.ResponseReasonPhrase, value); }
        }

        /// <summary>
        /// Gets or sets the owin.ResponseProtocol.
        /// </summary>
        /// <returns>The owin.ResponseProtocol.</returns>
        public virtual string Protocol
        {
            get { return Get<string>(OwinConstants.ResponseProtocol); }
            set { Set(OwinConstants.ResponseProtocol, value); }
        }

        /// <summary>
        /// Gets the response header collection.
        /// </summary>
        /// <returns>The response header collection.</returns>
        public virtual IHeaderDictionary Headers
        {
            get { return new HeaderDictionary(RawHeaders); }
        }

        private IDictionary<string, string[]> RawHeaders
        {
            get { return Get<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders); }
        }

        /// <summary>
        /// Gets a collection used to manipulate the Set-Cookie header.
        /// </summary>
        /// <returns>A collection used to manipulate the Set-Cookie header.</returns>
        public virtual ResponseCookieCollection Cookies
        {
            get { return new ResponseCookieCollection(Headers); }
        }

        /// <summary>
        /// Gets or sets the Content-Length header.
        /// </summary>
        /// <returns>The Content-Length header.</returns>
        public virtual long? ContentLength
        {
            get
            {
                long value;
                if (long.TryParse(OwinHelpers.GetHeader(RawHeaders, Constants.Headers.ContentLength), out value))
                {
                    return value;
                }
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    OwinHelpers.SetHeader(RawHeaders, Constants.Headers.ContentLength,
                        value.Value.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    RawHeaders.Remove(Constants.Headers.ContentLength);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Content-Type header.
        /// </summary>
        /// <returns>The Content-Type header.</returns>
        public virtual string ContentType
        {
            get { return OwinHelpers.GetHeader(RawHeaders, Constants.Headers.ContentType); }
            set { OwinHelpers.SetHeader(RawHeaders, Constants.Headers.ContentType, value); }
        }

        /// <summary>
        /// Gets or sets the Expires header.
        /// </summary>
        /// <returns>The Expires header.</returns>
        public virtual DateTimeOffset? Expires
        {
            get
            {
                DateTimeOffset value;
                if (DateTimeOffset.TryParse(OwinHelpers.GetHeader(RawHeaders, Constants.Headers.Expires),
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value))
                {
                    return value;
                }
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    OwinHelpers.SetHeader(RawHeaders, Constants.Headers.Expires,
                        value.Value.ToString(Constants.HttpDateFormat, CultureInfo.InvariantCulture));
                }
                else
                {
                    RawHeaders.Remove(Constants.Headers.Expires);
                }
            }
        }

        /// <summary>
        /// Gets or sets the E-Tag header.
        /// </summary>
        /// <returns>The E-Tag header.</returns>
        public virtual string ETag
        {
            get { return OwinHelpers.GetHeader(RawHeaders, Constants.Headers.ETag); }
            set { OwinHelpers.SetHeader(RawHeaders, Constants.Headers.ETag, value); }
        }

        /// <summary>
        /// Gets or sets the owin.ResponseBody Stream.
        /// </summary>
        /// <returns>The owin.ResponseBody Stream.</returns>
        public virtual Stream Body
        {
            get { return Get<Stream>(OwinConstants.ResponseBody); }
            set { Set(OwinConstants.ResponseBody, value); }
        }

        /// <summary>
        /// Registers for an event that fires when the response headers are sent.
        /// </summary>
        /// <param name="callback">The callback method.</param>
        /// <param name="state">The callback state.</param>
        public virtual void OnSendingHeaders(Action<object> callback, object state)
        {
            var onSendingHeaders = Get<Action<Action<object>, object>>(OwinConstants.CommonKeys.OnSendingHeaders);
            if (onSendingHeaders == null)
            {
                throw new NotSupportedException(Resources.Exception_MissingOnSendingHeaders);
            }
            onSendingHeaders(callback, state);
        }

        /// <summary>
        /// Sets a 302 response status code and the Location header.
        /// </summary>
        /// <param name="location">The location where to redirect the client.</param>
        public virtual void Redirect(string location)
        {
            StatusCode = 302;
            OwinHelpers.SetHeader(RawHeaders, Constants.Headers.Location, location);
        }

        /// <summary>
        /// Writes the given text to the response body stream using UTF-8.
        /// </summary>
        /// <param name="text">The response data.</param>
        public virtual void Write(string text)
        {
            Write(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// Writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        public virtual void Write(byte[] data)
        {
            Write(data, 0, data == null ? 0 : data.Length);
        }

        /// <summary>
        /// Writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="offset">The zero-based byte offset in the <paramref name="data" /> parameter at which to begin copying bytes.</param>
        /// <param name="count">The number of bytes to write.</param>
        public virtual void Write(byte[] data, int offset, int count)
        {
            Body.Write(data, offset, count);
        }

        /// <summary>
        /// Asynchronously writes the given text to the response body stream using UTF-8.
        /// </summary>
        /// <param name="text">The response data.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        public virtual Task WriteAsync(string text)
        {
            return WriteAsync(text, CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously writes the given text to the response body stream using UTF-8.
        /// </summary>
        /// <param name="text">The response data.</param>
        /// <param name="token">A token used to indicate cancellation.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        public virtual Task WriteAsync(string text, CancellationToken token)
        {
            return WriteAsync(Encoding.UTF8.GetBytes(text), token);
        }

        /// <summary>
        /// Asynchronously writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        public virtual Task WriteAsync(byte[] data)
        {
            return WriteAsync(data, CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="token">A token used to indicate cancellation.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        public virtual Task WriteAsync(byte[] data, CancellationToken token)
        {
            return WriteAsync(data, 0, data == null ? 0 : data.Length, token);
        }

        /// <summary>
        /// Asynchronously writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="offset">The zero-based byte offset in the <paramref name="data" /> parameter at which to begin copying bytes.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="token">A token used to indicate cancellation.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        public virtual Task WriteAsync(byte[] data, int offset, int count, CancellationToken token)
        {
            return Body.WriteAsync(data, offset, count, token);
        }

        /// <summary>
        /// Gets a value from the OWIN environment, or returns default(T) if not present.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value with the specified key or the default(T) if not present.</returns>
        public virtual T Get<T>(string key)
        {
            return Get(key, default(T));
        }

        private T Get<T>(string key, T fallback)
        {
            object value;
            return Environment.TryGetValue(key, out value) ? (T)value : fallback;
        }

        /// <summary>
        /// Sets the given key and value in the OWIN environment.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>This instance.</returns>
        public virtual IOwinResponse Set<T>(string key, T value)
        {
            Environment[key] = value;
            return this;
        }
    }

    /// <summary>
    /// Provides correct escaping for Path and PathBase values when needed to reconstruct a request or redirect URI string
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    struct PathString : IEquatable<PathString>
    {
        private static readonly Func<string, string> EscapeDataString = Uri.EscapeDataString;

        /// <summary>
        /// Represents the empty path. This field is read-only.
        /// </summary>
        public static readonly PathString Empty = new PathString(String.Empty);

        private readonly string _value;

        /// <summary>
        /// Initialize the path string with a given value. This value must be in un-escaped format. Use
        /// PathString.FromUriComponent(value) if you have a path value which is in an escaped format.
        /// </summary>
        /// <param name="value">The unescaped path to be assigned to the Value property.</param>
        public PathString(string value)
        {
            if (!String.IsNullOrEmpty(value) && value[0] != '/')
            {
                throw new ArgumentException(Resources.Exception_PathMustStartWithSlash, "value");
            }
            _value = value;
        }

        /// <summary>
        /// The unescaped path value
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True if the path is not empty
        /// </summary>
        public bool HasValue
        {
            get { return !String.IsNullOrEmpty(_value); }
        }

        /// <summary>
        /// Provides the path string escaped in a way which is correct for combining into the URI representation.
        /// </summary>
        /// <returns>The escaped path value</returns>
        public override string ToString()
        {
            return ToUriComponent();
        }

        /// <summary>
        /// Provides the path string escaped in a way which is correct for combining into the URI representation.
        /// </summary>
        /// <returns>The escaped path value</returns>
        public string ToUriComponent()
        {
            if (HasValue)
            {
                if (RequiresEscaping(_value))
                {
                    // TODO: Measure the cost of this escaping and consider optimizing.
                    return String.Join("/", _value.Split('/').Select(EscapeDataString));
                }
                return _value;
            }
            return String.Empty;
        }

        // Very conservative, these characters do not need to be escaped in a path.
        private static bool RequiresEscaping(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                // Check conservatively for safe characters. See http://www.ietf.org/rfc/rfc3986.txt
                bool safeChar =
                    (('a' <= c && c <= 'z')
                    || ('A' <= c && c <= 'Z')
                    || ('0' <= c && c <= '9')
                    || c == '/' || c == '-' || c == '_');
                if (!safeChar)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns an PathString given the path as it is escaped in the URI format. The string MUST NOT contain any
        /// value that is not a path.
        /// </summary>
        /// <param name="uriComponent">The escaped path as it appears in the URI format.</param>
        /// <returns>The resulting PathString</returns>
        public static PathString FromUriComponent(string uriComponent)
        {
            // REVIEW: what is the exactly correct thing to do?
            return new PathString(Uri.UnescapeDataString(uriComponent));
        }

        /// <summary>
        /// Returns an PathString given the path as from a Uri object. Relative Uri objects are not supported.
        /// </summary>
        /// <param name="uri">The Uri object</param>
        /// <returns>The resulting PathString</returns>
        public static PathString FromUriComponent(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            // REVIEW: what is the exactly correct thing to do?
            return new PathString("/" + uri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
        }

        /// <summary>
        /// Checks if this instance starts with or exactly matches the other instance. Only full segments are matched.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool StartsWithSegments(PathString other)
        {
            string value1 = Value ?? String.Empty;
            string value2 = other.Value ?? String.Empty;
            if (value1.StartsWith(value2, StringComparison.OrdinalIgnoreCase))
            {
                return value1.Length == value2.Length || value1[value2.Length] == '/';
            }
            return false;
        }

        /// <summary>
        /// Checks if this instance starts with or exactly matches the other instance. Only full segments are matched.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="remaining">Any remaining segments from this instance not included in the other instance.</param>
        /// <returns></returns>
        public bool StartsWithSegments(PathString other, out PathString remaining)
        {
            string value1 = Value ?? String.Empty;
            string value2 = other.Value ?? String.Empty;
            if (value1.StartsWith(value2, StringComparison.OrdinalIgnoreCase))
            {
                if (value1.Length == value2.Length || value1[value2.Length] == '/')
                {
                    remaining = new PathString(value1.Substring(value2.Length));
                    return true;
                }
            }
            remaining = Empty;
            return false;
        }

        /// <summary>
        /// Adds two PathString instances into a combined PathString value.
        /// </summary>
        /// <returns>The combined PathString value</returns>
        public PathString Add(PathString other)
        {
            return new PathString(Value + other.Value);
        }

        /// <summary>
        /// Combines a PathString and QueryString into the joined URI formatted string value.
        /// </summary>
        /// <returns>The joined URI formatted string value</returns>
        public string Add(QueryString other)
        {
            return ToUriComponent() + other.ToUriComponent();
        }

        /// <summary>
        /// Compares this PathString value to another value. The default comparison is StringComparison.OrdinalIgnoreCase.
        /// </summary>
        /// <param name="other">The second PathString for comparison.</param>
        /// <returns>True if both PathString values are equal</returns>
        public bool Equals(PathString other)
        {
            return string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Compares this PathString value to another value using a specific StringComparison type
        /// </summary>
        /// <param name="other">The second PathString for comparison</param>
        /// <param name="comparisonType">The StringComparison type to use</param>
        /// <returns>True if both PathString values are equal</returns>
        public bool Equals(PathString other, StringComparison comparisonType)
        {
            return string.Equals(_value, other._value, comparisonType);
        }

        /// <summary>
        /// Compares this PathString value to another value. The default comparison is StringComparison.OrdinalIgnoreCase.
        /// </summary>
        /// <param name="obj">The second PathString for comparison.</param>
        /// <returns>True if both PathString values are equal</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is PathString && Equals((PathString)obj, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the hash code for the PathString value. The hash code is provided by the OrdinalIgnoreCase implementation.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return (_value != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(_value) : 0);
        }

        /// <summary>
        /// Operator call through to Equals
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>True if both PathString values are equal</returns>
        public static bool operator ==(PathString left, PathString right)
        {
            return left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Operator call through to Equals
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>True if both PathString values are not equal</returns>
        public static bool operator !=(PathString left, PathString right)
        {
            return !left.Equals(right, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Operator call through to Add
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The PathString combination of both values</returns>
        public static PathString operator +(PathString left, PathString right)
        {
            return left.Add(right);
        }

        /// <summary>
        /// Operator call through to Add
        /// </summary>
        /// <param name="left">The left parameter</param>
        /// <param name="right">The right parameter</param>
        /// <returns>The PathString combination of both values</returns>
        public static string operator +(PathString left, QueryString right)
        {
            return left.Add(right);
        }
    }

    /// <summary>
    /// Provides correct handling for QueryString value when needed to reconstruct a request or redirect URI string
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    struct QueryString : IEquatable<QueryString>
    {
        /// <summary>
        /// Represents the empty query string. This field is read-only.
        /// </summary>
        public static readonly QueryString Empty = new QueryString(String.Empty);

        private readonly string _value;

        /// <summary>
        /// Initalize the query string with a given value. This value must be in escaped and delimited format without
        /// a leading '?' character.
        /// </summary>
        /// <param name="value">The query string to be assigned to the Value property.</param>
        public QueryString(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Initialize a query string with a single given parameter name and value. The value is
        /// </summary>
        /// <param name="name">The unencoded parameter name</param>
        /// <param name="value">The unencoded parameter value</param>
        public QueryString(string name, string value)
        {
            _value = Uri.EscapeDataString(name) + '=' + Uri.EscapeDataString(value);
        }

        /// <summary>
        /// The unescaped query string without the leading '?' character
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True if the query string is not empty
        /// </summary>
        public bool HasValue
        {
            get { return !String.IsNullOrWhiteSpace(_value); }
        }

        /// <summary>
        /// Provides the query string escaped in a way which is correct for combining into the URI representation.
        /// A leading '?' character will be prepended unless the Value is null or empty. Characters which are potentally
        /// dangerous are escaped.
        /// </summary>
        /// <returns>The query string value</returns>
        public override string ToString()
        {
            return ToUriComponent();
        }

        /// <summary>
        /// Provides the query string escaped in a way which is correct for combining into the URI representation.
        /// A leading '?' character will be prepended unless the Value is null or empty. Characters which are potentially
        /// dangerous are escaped.
        /// </summary>
        /// <returns>The query string value</returns>
        public string ToUriComponent()
        {
            // Escape things properly so System.Uri doesn't mis-interpret the data.
            return HasValue ? "?" + _value.Replace("#", "%23") : String.Empty;
        }

        /// <summary>
        /// Returns an QueryString given the query as it is escaped in the URI format. The string MUST NOT contain any
        /// value that is not a query.
        /// </summary>
        /// <param name="uriComponent">The escaped query as it appears in the URI format.</param>
        /// <returns>The resulting QueryString</returns>
        public static QueryString FromUriComponent(string uriComponent)
        {
            if (String.IsNullOrEmpty(uriComponent))
            {
                return new QueryString(string.Empty);
            }
            if (uriComponent[0] != '?')
            {
                throw new ArgumentException(Resources.Exception_QueryStringMustStartWithDelimiter, "uriComponent");
            }
            return new QueryString(uriComponent.Substring(1));
        }

        /// <summary>
        /// Returns an QueryString given the query as from a Uri object. Relative Uri objects are not supported.
        /// </summary>
        /// <param name="uri">The Uri object</param>
        /// <returns>The resulting QueryString</returns>
        public static QueryString FromUriComponent(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            return new QueryString(uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped));
        }

        /// <summary>
        /// Indicates whether the current instance is equal to the other instance.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(QueryString other)
        {
            return string.Equals(_value, other._value);
        }

        /// <summary>
        /// Indicates whether the current instance is equal to the other instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is QueryString && Equals((QueryString)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (_value != null ? _value.GetHashCode() : 0);
        }

        /// <summary>
        /// Compares the two instances for equality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(QueryString left, QueryString right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the two instances for inequality.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(QueryString left, QueryString right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Accessors for query, forms, etc.
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    class ReadableStringCollection : IReadableStringCollection
    {
        /// <summary>
        /// Create a new wrapper
        /// </summary>
        /// <param name="store"></param>
        public ReadableStringCollection(IDictionary<string, string[]> store)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            Store = store;
        }

        private IDictionary<string, string[]> Store { get; set; }

        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get { return Get(key); }
        }

        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            return OwinHelpers.GetJoinedValue(Store, key);
        }

        /// <summary>
        /// Get the associated values from the collection in their original format.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IList<string> GetValues(string key)
        {
            string[] values;
            Store.TryGetValue(key, out values);
            return values;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            return Store.GetEnumerator();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// A wrapper for the request Cookie header
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    class RequestCookieCollection : IEnumerable<KeyValuePair<string, string>>
    {
        /// <summary>
        /// Create a new wrapper
        /// </summary>
        /// <param name="store"></param>
        public RequestCookieCollection(IDictionary<string, string> store)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            Store = store;
        }

        private IDictionary<string, string> Store { get; set; }

        /// <summary>
        /// Returns null rather than throwing KeyNotFoundException
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                string value;
                Store.TryGetValue(key, out value);
                return value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return Store.GetEnumerator();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal static class Resources
    {
        internal const string Exception_MissingOnSendingHeaders = "The OWIN key 'server.OnSsoendingHeaders' is not available for this request.";
        internal const string Exception_PathMustStartWithSlash = "The path must start with a '/' followed by one or more characters.";
        internal const string Exception_QueryStringMustStartWithDelimiter = "The query string must start with a '?' unless null or empty.";
    }

    /// <summary>
    /// A wrapper for the response Set-Cookie header
    /// </summary>
#if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    class ResponseCookieCollection
    {
        /// <summary>
        /// Create a new wrapper
        /// </summary>
        /// <param name="headers"></param>
        public ResponseCookieCollection(IHeaderDictionary headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            Headers = headers;
        }

        private IHeaderDictionary Headers { get; set; }

        /// <summary>
        /// Add a new cookie and value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Append(string key, string value)
        {
            Headers.AppendValues(Constants.Headers.SetCookie, Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "; path=/");
        }

        /// <summary>
        /// Add a new cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public void Append(string key, string value, CookieOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            bool domainHasValue = !string.IsNullOrEmpty(options.Domain);
            bool pathHasValue = !string.IsNullOrEmpty(options.Path);
            bool expiresHasValue = options.Expires.HasValue;

            string setCookieValue = string.Concat(
                Uri.EscapeDataString(key),
                "=",
                Uri.EscapeDataString(value ?? string.Empty),
                !domainHasValue ? null : "; domain=",
                !domainHasValue ? null : options.Domain,
                !pathHasValue ? null : "; path=",
                !pathHasValue ? null : options.Path,
                !expiresHasValue ? null : "; expires=",
                !expiresHasValue ? null : options.Expires.Value.ToString("ddd, dd-MMM-yyyy HH:mm:ss ", CultureInfo.InvariantCulture) + "GMT",
                !options.Secure ? null : "; secure",
                !options.HttpOnly ? null : "; HttpOnly");
            Headers.AppendValues("Set-Cookie", setCookieValue);
        }

        /// <summary>
        /// Sets an expired cookie
        /// </summary>
        /// <param name="key"></param>
        public void Delete(string key)
        {
            Func<string, bool> predicate = value => value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase);

            var deleteCookies = new[] { Uri.EscapeDataString(key) + "=; expires=Thu, 01-Jan-1970 00:00:00 GMT" };
            IList<string> existingValues = Headers.GetValues(Constants.Headers.SetCookie);
            if (existingValues == null || existingValues.Count == 0)
            {
                Headers.SetValues(Constants.Headers.SetCookie, deleteCookies);
            }
            else
            {
                Headers.SetValues(Constants.Headers.SetCookie, existingValues.Where(value => !predicate(value)).Concat(deleteCookies).ToArray());
            }
        }

        /// <summary>
        /// Sets an expired cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="options"></param>
        public void Delete(string key, CookieOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            bool domainHasValue = !string.IsNullOrEmpty(options.Domain);
            bool pathHasValue = !string.IsNullOrEmpty(options.Path);

            Func<string, bool> rejectPredicate;
            if (domainHasValue)
            {
                rejectPredicate = value =>
                    value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase) &&
                        value.IndexOf("domain=" + options.Domain, StringComparison.OrdinalIgnoreCase) != -1;
            }
            else if (pathHasValue)
            {
                rejectPredicate = value =>
                    value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase) &&
                        value.IndexOf("path=" + options.Path, StringComparison.OrdinalIgnoreCase) != -1;
            }
            else
            {
                rejectPredicate = value => value.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase);
            }

            IList<string> existingValues = Headers.GetValues(Constants.Headers.SetCookie);
            if (existingValues != null)
            {
                Headers.SetValues(Constants.Headers.SetCookie, existingValues.Where(value => !rejectPredicate(value)).ToArray());
            }

            Append(key, string.Empty, new CookieOptions
            {
                Path = options.Path,
                Domain = options.Domain,
                Expires = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
        }
    }


    #if LIBOWIN_PUBLIC
    public
#else
    internal
#endif
    static class IOwinResponseExtension 
    {
        /// <summary>
        /// Registers for an event that fires when the response headers are sent.
        /// </summary>
        /// <param name="response">The owin response</param>
        /// <param name="callback">The callback method.</param>
        /// <param name="state">The callback state.</param>
        public static void OnSendingHeaders<T>(this IOwinResponse response, Action<T> callback, T state)
        {
            if (response == null) {
                throw new ArgumentNullException("response");
            }
            Action<object> innerCallback = innerState => callback((T)innerState);
            response.OnSendingHeaders(innerCallback, state);
        }
        
    }
}
