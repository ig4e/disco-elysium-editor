// Package parser implements a reader for Disco Elysium's binary-serialized Lua
// tables. The format uses a TLV (Type-Length-Value) scheme:
//
//	S  = string  (7-bit encoded length + UTF-8 bytes)
//	N  = number  (little-endian float64)
//	B  = boolean (single byte, 0 or 1)
//	T  = table   (4-byte padding + 4-byte LE int32 count, then count key/value pairs)
package parser

import (
	"bytes"
	"encoding/binary"
	"fmt"
	"io"
	"math"
)

const (
	TypeString  byte = 'S'
	TypeNumber  byte = 'N'
	TypeBoolean byte = 'B'
	TypeTable   byte = 'T'
)

// LuaParser reads TLV-encoded Lua tables from a byte buffer.
type LuaParser struct {
	R *bytes.Reader
}

// New creates a LuaParser from raw bytes.
func New(data []byte) *LuaParser {
	return &LuaParser{R: bytes.NewReader(data)}
}

// read7BitInt decodes a 7-bit variable length integer (same encoding as
// BinaryWriter.Write7BitEncodedInt in .NET / C#).
func (p *LuaParser) read7BitInt() (int, error) {
	var result int
	var shift uint
	for {
		b, err := p.R.ReadByte()
		if err != nil {
			return 0, err
		}
		result |= int(b&0x7F) << shift
		if b&0x80 == 0 {
			break
		}
		shift += 7
	}
	return result, nil
}

// ReadValue reads one typed value from the stream. Returns one of:
// string, float64, int64, bool, or map[string]interface{}.
func (p *LuaParser) ReadValue() (interface{}, error) {
	t, err := p.R.ReadByte()
	if err != nil {
		return nil, err
	}

	switch t {
	case TypeString:
		length, err := p.read7BitInt()
		if err != nil {
			return nil, fmt.Errorf("string length: %w", err)
		}
		buf := make([]byte, length)
		if _, err := io.ReadFull(p.R, buf); err != nil {
			return nil, fmt.Errorf("string body: %w", err)
		}
		return string(buf), nil

	case TypeNumber:
		var n float64
		if err := binary.Read(p.R, binary.LittleEndian, &n); err != nil {
			return nil, fmt.Errorf("number: %w", err)
		}
		if n == math.Trunc(n) && n >= math.MinInt64 && n <= math.MaxInt64 {
			return int64(n), nil
		}
		return n, nil

	case TypeBoolean:
		b, err := p.R.ReadByte()
		if err != nil {
			return nil, fmt.Errorf("boolean: %w", err)
		}
		return b != 0, nil

	case TypeTable:
		var padding int32
		var count int32
		if err := binary.Read(p.R, binary.LittleEndian, &padding); err != nil {
			return nil, fmt.Errorf("table padding: %w", err)
		}
		if err := binary.Read(p.R, binary.LittleEndian, &count); err != nil {
			return nil, fmt.Errorf("table count: %w", err)
		}

		table := make(map[string]interface{}, int(count))
		for i := 0; i < int(count); i++ {
			key, err := p.ReadValue()
			if err != nil {
				return nil, fmt.Errorf("table key %d: %w", i, err)
			}
			val, err := p.ReadValue()
			if err != nil {
				return nil, fmt.Errorf("table val %d: %w", i, err)
			}
			table[fmt.Sprint(key)] = val
		}
		return table, nil

	default:
		off := p.R.Size() - int64(p.R.Len()) - 1
		return nil, fmt.Errorf("unknown type byte 0x%02X ('%c') at offset %d", t, t, off)
	}
}

// ReadAll reads all top-level values from the stream, merging tables into a
// single map. Stray null bytes between entries are silently skipped.
func (p *LuaParser) ReadAll() (map[string]interface{}, error) {
	all := make(map[string]interface{})
	for p.R.Len() > 0 {
		val, err := p.ReadValue()
		if err != nil {
			// Try skipping a null byte (padding between entries)
			if p.R.Len() > 0 {
				b, _ := p.R.ReadByte()
				if b == 0 {
					continue
				}
				p.R.UnreadByte()
			}
			if len(all) > 0 {
				break // we already got data, just trailing junk
			}
			return nil, err
		}
		if table, ok := val.(map[string]interface{}); ok {
			for k, v := range table {
				all[k] = v
			}
		}
	}
	return all, nil
}

// Remaining returns bytes remaining in the reader.
func (p *LuaParser) Remaining() int {
	return p.R.Len()
}
