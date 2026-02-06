use std::collections::HashMap;
use std::io::{self, Read, Write, Cursor};
use byteorder::{LittleEndian, ReadBytesExt, WriteBytesExt};
use crate::models::LuaValue;

/// Parses the binary TLV format used by .ntwtf.lua files.
/// Format (matching the C#/Go reference):
///   S = string  (7-bit encoded length + UTF-8 bytes)
///   N = number  (little-endian float64)
///   B = boolean (single byte, 0 or 1)
///   T = table   (4-byte padding + 4-byte LE int32 count, then count key/value pairs)
#[allow(dead_code)]
pub fn parse_lua_database(path: &str) -> io::Result<HashMap<String, LuaValue>> {
    let data = std::fs::read(path)?;
    parse_lua_data(&data)
}

pub fn parse_lua_data(data: &Vec<u8>) -> io::Result<HashMap<String, LuaValue>> {
    let mut cursor = Cursor::new(data);
    let mut all = HashMap::new();

    while (cursor.position() as usize) < data.len() {
        let pos = cursor.position();
        match read_value(&mut cursor) {
            Ok(LuaValue::Table(table)) => {
                for (k, v) in table {
                    all.insert(k, v);
                }
            }
            Ok(_) => {}
            Err(_) => {
                if (cursor.position() as usize) < data.len() {
                    cursor.set_position(pos + 1);
                    continue;
                }
                if !all.is_empty() {
                    break;
                }
                return Err(io::Error::new(io::ErrorKind::InvalidData, "Failed to parse lua database"));
            }
        }
    }

    Ok(all)
}

#[allow(dead_code)]
pub fn write_lua_database(path: &str, data: &HashMap<String, LuaValue>) -> io::Result<()> {
    let bytes = serialize_lua_database(data)?;
    std::fs::write(path, bytes)
}

pub fn serialize_lua_database(data: &HashMap<String, LuaValue>) -> io::Result<Vec<u8>> {
    let mut buffer = Vec::new();
    write_value(&mut buffer, &LuaValue::Table(data.clone()))?;
    Ok(buffer)
}

fn read_value(reader: &mut Cursor<&Vec<u8>>) -> io::Result<LuaValue> {
    let type_byte = reader.read_u8()?;
    match type_byte {
        0x53 => {
            // String
            let s = read_string(reader)?;
            Ok(LuaValue::String(s))
        }
        0x4E => {
            // Number (float64 LE)
            let n = reader.read_f64::<LittleEndian>()?;
            Ok(LuaValue::Number(n))
        }
        0x42 => {
            // Boolean
            let b = reader.read_u8()?;
            Ok(LuaValue::Boolean(b != 0))
        }
        0x54 => {
            // Table
            read_table(reader)
        }
        _ => Err(io::Error::new(
            io::ErrorKind::InvalidData,
            format!("Unknown type byte 0x{:02X} at offset {}", type_byte, reader.position() - 1),
        )),
    }
}

fn read_table(reader: &mut Cursor<&Vec<u8>>) -> io::Result<LuaValue> {
    let mut padding = [0u8; 4];
    reader.read_exact(&mut padding)?;
    let count = reader.read_i32::<LittleEndian>()?;
    let mut dict = HashMap::with_capacity(count as usize);

    for _ in 0..count {
        let key_val = read_value(reader)?;
        let key = match key_val {
            LuaValue::String(s) => s,
            LuaValue::Number(n) => n.to_string(),
            LuaValue::Boolean(b) => b.to_string(),
            _ => String::from("unknown"),
        };
        let value = read_value(reader)?;
        dict.insert(key, value);
    }

    Ok(LuaValue::Table(dict))
}

fn read_string(reader: &mut Cursor<&Vec<u8>>) -> io::Result<String> {
    let length = read_7bit_encoded_int(reader)?;
    let mut buf = vec![0u8; length as usize];
    reader.read_exact(&mut buf)?;
    String::from_utf8(buf).map_err(|e| io::Error::new(io::ErrorKind::InvalidData, e))
}

fn read_7bit_encoded_int(reader: &mut Cursor<&Vec<u8>>) -> io::Result<i32> {
    let mut result: i32 = 0;
    let mut shift = 0;
    loop {
        let b = reader.read_u8()?;
        result |= ((b & 0x7F) as i32) << shift;
        shift += 7;
        if b & 0x80 == 0 {
            break;
        }
    }
    Ok(result)
}

fn write_value<W: Write>(writer: &mut W, value: &LuaValue) -> io::Result<()> {
    match value {
        LuaValue::String(s) => {
            writer.write_u8(0x53)?;
            write_string(writer, s)?;
        }
        LuaValue::Number(n) => {
            writer.write_u8(0x4E)?;
            writer.write_f64::<LittleEndian>(*n)?;
        }
        LuaValue::Boolean(b) => {
            writer.write_u8(0x42)?;
            writer.write_u8(if *b { 1 } else { 0 })?;
        }
        LuaValue::Table(table) => {
            writer.write_u8(0x54)?;
            write_table(writer, table)?;
        }
    }
    Ok(())
}

fn write_table<W: Write>(writer: &mut W, data: &HashMap<String, LuaValue>) -> io::Result<()> {
    writer.write_all(&[0u8; 4])?; // 4-byte padding
    writer.write_i32::<LittleEndian>(data.len() as i32)?;

    for (key, value) in data {
        writer.write_u8(0x53)?;
        write_string(writer, key)?;
        write_value(writer, value)?;
    }

    Ok(())
}

fn write_string<W: Write>(writer: &mut W, value: &str) -> io::Result<()> {
    let bytes = value.as_bytes();
    write_7bit_encoded_int(writer, bytes.len() as i32)?;
    writer.write_all(bytes)?;
    Ok(())
}

fn write_7bit_encoded_int<W: Write>(writer: &mut W, value: i32) -> io::Result<()> {
    let mut v = value as u32;
    while v >= 0x80 {
        writer.write_u8((v | 0x80) as u8)?;
        v >>= 7;
    }
    writer.write_u8(v as u8)?;
    Ok(())
}

/// Flatten a nested LuaValue table into dotted-key map
pub fn flatten_lua(data: &HashMap<String, LuaValue>, prefix: &str) -> HashMap<String, LuaValue> {
    let mut result = HashMap::new();
    for (key, value) in data {
        let full_key = if prefix.is_empty() {
            key.clone()
        } else {
            format!("{}.{}", prefix, key)
        };

        match value {
            LuaValue::Table(nested) => {
                for (nk, nv) in flatten_lua(nested, &full_key) {
                    result.insert(nk, nv);
                }
            }
            _ => {
                result.insert(full_key, value.clone());
            }
        }
    }
    result
}

/// Set a dotted-key value in a nested LuaValue table
pub fn set_lua_value(data: &mut HashMap<String, LuaValue>, dotted_key: &str, value: LuaValue) {
    let parts: Vec<&str> = dotted_key.split('.').collect();
    let mut current = data;

    for i in 0..parts.len() - 1 {
        let part = parts[i].to_string();
        if !current.contains_key(&part) || !matches!(current.get(&part), Some(LuaValue::Table(_))) {
            current.insert(part.clone(), LuaValue::Table(HashMap::new()));
        }
        current = match current.get_mut(&part) {
            Some(LuaValue::Table(t)) => t,
            _ => unreachable!(),
        };
    }

    current.insert(parts.last().unwrap().to_string(), value);
}
