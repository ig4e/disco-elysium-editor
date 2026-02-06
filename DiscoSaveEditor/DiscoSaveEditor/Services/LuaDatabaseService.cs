using System.Text;

namespace DiscoSaveEditor.Services;

/// <summary>
/// Parses and writes the binary TLV format used by .ntwtf.lua files.
/// This is the PixelCrushers Dialogue System runtime variable database.
/// ~12,000 variables stored in binary format.
///
/// Format (TLV scheme matching the Go reference parser):
///   S = string  (7-bit encoded length + UTF-8 bytes)
///   N = number  (little-endian float64)
///   B = boolean (single byte, 0 or 1)
///   T = table   (4-byte padding + 4-byte LE int32 count, then count key/value pairs)
///
/// Every value (including table keys) is prefixed with its type byte.
/// The root of the file is a single typed table value.
/// </summary>
public class LuaDatabaseService
{
    /// <summary>
    /// Parse a .ntwtf.lua binary file into a nested dictionary.
    /// Matches Go reference: parser.New(data).ReadAll()
    /// </summary>
    public Dictionary<string, object> Parse(string filePath)
    {
        var data = File.ReadAllBytes(filePath);
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream, Encoding.UTF8);

        // ReadAll: read top-level typed values, merge tables, skip stray null bytes
        var all = new Dictionary<string, object>();
        while (stream.Position < stream.Length)
        {
            long pos = stream.Position;
            try
            {
                var val = ReadValue(reader);
                if (val is Dictionary<string, object> table)
                {
                    foreach (var (k, v) in table)
                        all[k] = v;
                }
                // Non-table top-level values are silently discarded (matches Go)
            }
            catch
            {
                // Try skipping a null byte (padding between entries)
                if (stream.Position < stream.Length)
                {
                    stream.Position = pos + 1;
                    byte b = reader.ReadByte();
                    if (b == 0)
                    {
                        stream.Position = pos + 1; // skip the null, retry
                        continue;
                    }
                    stream.Position = pos + 1; // skip one byte, retry
                    continue;
                }
                if (all.Count > 0)
                    break; // got data, trailing junk
                throw;
            }
        }

        return all;
    }

    /// <summary>
    /// Write the variable database back to a .ntwtf.lua binary file.
    /// Writes as a single root table value (type byte 'T' + table body).
    /// </summary>
    public void Write(string filePath, Dictionary<string, object> data)
    {
        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        // Write the root as a typed table value
        WriteValue(writer, data);
    }

    /// <summary>
    /// Read a single typed value from the stream.
    /// Returns string, double, bool, or Dictionary&lt;string, object&gt;.
    /// </summary>
    private object ReadValue(BinaryReader reader)
    {
        byte type = reader.ReadByte();
        return type switch
        {
            0x53 => ReadString(reader),          // S = String
            0x4E => reader.ReadDouble(),         // N = Number (float64 LE)
            0x42 => reader.ReadByte() != 0,      // B = Boolean
            0x54 => ReadTable(reader),           // T = Table (recursive)
            _ => throw new InvalidDataException(
                $"Unknown type byte 0x{type:X2} ('{(char)type}') at offset {reader.BaseStream.Position - 1}")
        };
    }

    /// <summary>
    /// Read a table body (after the 'T' type byte has been consumed).
    /// Keys are full typed values (usually strings), converted to string keys.
    /// </summary>
    private Dictionary<string, object> ReadTable(BinaryReader reader)
    {
        reader.ReadBytes(4); // 4-byte padding
        int count = reader.ReadInt32(); // LE entry count
        var dict = new Dictionary<string, object>(count);

        for (int i = 0; i < count; i++)
        {
            // Keys are typed values (matches Go: key, err := p.ReadValue())
            var keyObj = ReadValue(reader);
            var key = Convert.ToString(keyObj, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            var value = ReadValue(reader);
            dict[key] = value;
        }

        return dict;
    }

    private string ReadString(BinaryReader reader)
    {
        int length = Read7BitEncodedInt(reader);
        var bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Read a 7-bit encoded integer (same as .NET BinaryWriter.Write7BitEncodedInt).
    /// </summary>
    private int Read7BitEncodedInt(BinaryReader reader)
    {
        int result = 0;
        int shift = 0;
        byte b;
        do
        {
            b = reader.ReadByte();
            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
        return result;
    }

    /// <summary>
    /// Write a typed value (type byte + payload). Table keys are also written as typed values.
    /// </summary>
    private void WriteValue(BinaryWriter writer, object value)
    {
        switch (value)
        {
            case string s:
                writer.Write((byte)0x53); // 'S'
                WriteString(writer, s);
                break;
            case double d:
                writer.Write((byte)0x4E); // 'N'
                writer.Write(d);
                break;
            case bool b:
                writer.Write((byte)0x42); // 'B'
                writer.Write(b ? (byte)1 : (byte)0);
                break;
            case Dictionary<string, object> table:
                writer.Write((byte)0x54); // 'T'
                WriteTable(writer, table);
                break;
            default:
                throw new InvalidDataException($"Unsupported value type: {value.GetType()}");
        }
    }

    /// <summary>
    /// Write a table body (after the 'T' type byte).
    /// Each key is written as a full typed value ('S' + string).
    /// </summary>
    private void WriteTable(BinaryWriter writer, Dictionary<string, object> data)
    {
        writer.Write(new byte[4]); // 4-byte padding
        writer.Write(data.Count);  // LE int32 count

        foreach (var (key, value) in data)
        {
            // Keys are typed values (write as string with 'S' prefix)
            writer.Write((byte)0x53); // 'S'
            WriteString(writer, key);
            WriteValue(writer, value);
        }
    }

    private void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Write7BitEncodedInt(writer, bytes.Length);
        writer.Write(bytes);
    }

    /// <summary>
    /// Write a 7-bit encoded integer (matches .NET BinaryReader.Read7BitEncodedInt).
    /// </summary>
    private void Write7BitEncodedInt(BinaryWriter writer, int value)
    {
        uint v = (uint)value;
        while (v >= 0x80)
        {
            writer.Write((byte)(v | 0x80));
            v >>= 7;
        }
        writer.Write((byte)v);
    }

    /// <summary>
    /// Flatten nested variable tables into dot-separated keys for display.
    /// e.g. {"reputation": {"communist": 3.0}} → {"reputation.communist": 3.0}
    /// </summary>
    public static Dictionary<string, object> Flatten(Dictionary<string, object> data, string prefix = "")
    {
        var result = new Dictionary<string, object>();
        foreach (var (key, value) in data)
        {
            var fullKey = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
            if (value is Dictionary<string, object> nested)
            {
                foreach (var (nestedKey, nestedValue) in Flatten(nested, fullKey))
                    result[nestedKey] = nestedValue;
            }
            else
            {
                result[fullKey] = value;
            }
        }
        return result;
    }

    /// <summary>
    /// Set a value in the nested dictionary using a dot-separated key.
    /// e.g. SetValue(dict, "reputation.communist", 5.0)
    /// </summary>
    public static void SetValue(Dictionary<string, object> data, string dottedKey, object value)
    {
        var parts = dottedKey.Split('.');
        var current = data;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!current.TryGetValue(parts[i], out var next) || next is not Dictionary<string, object> nextDict)
            {
                nextDict = new Dictionary<string, object>();
                current[parts[i]] = nextDict;
            }
            current = nextDict;
        }

        current[parts[^1]] = value;
    }
}
