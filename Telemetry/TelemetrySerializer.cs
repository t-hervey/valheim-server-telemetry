using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ValheimTelemetry.Telemetry
{
    public static class TelemetrySerializer
    {
        public static string Serialize(TelemetryEvent telemetryEvent)
        {
            StringBuilder builder = new StringBuilder(384);
            builder.Append('{');
            IList<KeyValuePair<string, object>> properties = telemetryEvent.Properties;
            for (int i = 0; i < properties.Count; i++)
            {
                if (i != 0)
                {
                    builder.Append(',');
                }
                AppendString(builder, properties[i].Key);
                builder.Append(':');
                AppendValue(builder, properties[i].Value);
            }
            builder.Append('}');
            return builder.ToString();
        }

        private static void AppendValue(StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            if (value is string text)
            {
                AppendString(builder, text);
            }
            else if (value is bool boolean)
            {
                builder.Append(boolean ? "true" : "false");
            }
            else if (value is float single)
            {
                builder.Append(float.IsNaN(single) || float.IsInfinity(single) ? "null" : single.ToString("R", CultureInfo.InvariantCulture));
            }
            else if (value is double number)
            {
                builder.Append(double.IsNaN(number) || double.IsInfinity(number) ? "null" : number.ToString("R", CultureInfo.InvariantCulture));
            }
            else if (value is decimal decimalNumber)
            {
                builder.Append(decimalNumber.ToString(CultureInfo.InvariantCulture));
            }
            else if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong)
            {
                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
            else
            {
                AppendString(builder, Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        private static void AppendString(StringBuilder builder, string value)
        {
            builder.Append('"');
            if (value != null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    switch (c)
                    {
                        case '"': builder.Append("\\\""); break;
                        case '\\': builder.Append("\\\\"); break;
                        case '\b': builder.Append("\\b"); break;
                        case '\f': builder.Append("\\f"); break;
                        case '\n': builder.Append("\\n"); break;
                        case '\r': builder.Append("\\r"); break;
                        case '\t': builder.Append("\\t"); break;
                        default:
                            if (c < 0x20)
                            {
                                builder.Append("\\u");
                                builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                builder.Append(c);
                            }
                            break;
                    }
                }
            }
            builder.Append('"');
        }
    }
}

