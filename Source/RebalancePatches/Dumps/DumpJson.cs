using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RebalancePatches
{
    /// <summary>
    /// Identity comparer, so a traversal can track objects it has already visited without
    /// tripping over types that override Equals.
    /// </summary>
    internal sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();
        bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);
        int IEqualityComparer<object>.GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    /// <summary>
    /// Minimal forward-only JSON writer shared by the dev database dumps. Culture-invariant, so
    /// numbers stay machine-readable on locales that use a decimal comma.
    /// </summary>
    internal sealed class Json
    {
        private readonly StringBuilder sb = new StringBuilder(1 << 22);
        private readonly Stack<bool> hasItems = new Stack<bool>();
        private bool pendingValue;

        public void BeginObject() { Sep(); sb.Append('{'); hasItems.Push(false); }
        public void EndObject() { hasItems.Pop(); sb.Append('}'); }
        public void BeginArray() { Sep(); sb.Append('['); hasItems.Push(false); }
        public void EndArray() { hasItems.Pop(); sb.Append(']'); }

        public void Name(string name)
        {
            Sep();
            WriteEscaped(name);
            sb.Append(':');
            pendingValue = true;
        }

        public void Value(string s)
        {
            if (s == null) { Null(); return; }
            Sep();
            WriteEscaped(s);
        }
        public void Value(bool b) { Sep(); sb.Append(b ? "true" : "false"); }
        public void Value(float f) { Number(f); }
        public void Null() { Sep(); sb.Append("null"); }

        public void Number(object value)
        {
            Sep();
            if (value is float f)
            {
                if (float.IsNaN(f) || float.IsInfinity(f)) { WriteEscaped(f.ToString(CultureInfo.InvariantCulture)); return; }
                sb.Append(f.ToString("R", CultureInfo.InvariantCulture));
                return;
            }
            if (value is double d)
            {
                if (double.IsNaN(d) || double.IsInfinity(d)) { WriteEscaped(d.ToString(CultureInfo.InvariantCulture)); return; }
                sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
                return;
            }
            sb.Append(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
        }

        private void Sep()
        {
            if (pendingValue)
            {
                pendingValue = false;
                return;
            }
            if (hasItems.Count == 0)
                return;
            if (hasItems.Peek())
            {
                sb.Append(',');
            }
            else
            {
                hasItems.Pop();
                hasItems.Push(true);
            }
        }

        private void WriteEscaped(string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        public override string ToString() => sb.ToString();
    }
}
