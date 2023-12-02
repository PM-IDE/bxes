namespace Bxes.Utils;

//https://github.com/rzubek/mini-leb128/blob/master/LEB128.cs
public static class Leb128
{
  private const long SignExtendMask = -1L;
  private const int Int64Bitsize = sizeof(long) * 8;

  public static void WriteLeb128Signed(this BinaryWriter writer, long value) => WriteLeb128Signed(writer, value, out _);

  public static void WriteLeb128Signed(this BinaryWriter writer, long value, out int bytes)
  {
    bytes = 0;
    var more = true;

    while (more)
    {
      var chunk = (byte)(value & 0x7fL);
      value >>= 7;

      var signBitSet = (chunk & 0x40) != 0;
      more = !((value == 0 && !signBitSet) || (value == -1 && signBitSet));
      if (more)
      {
        chunk |= 0x80;
      }

      writer.Write(chunk);
      bytes += 1;
    }
  }

  public static void WriteLeb128Unsigned(this BinaryWriter writer, ulong value) => WriteLeb128Unsigned(writer, value, out _);

  public static void WriteLeb128Unsigned(this BinaryWriter writer, ulong value, out int bytes)
  {
    bytes = 0;
    var more = true;

    while (more)
    {
      var chunk = (byte)(value & 0x7fUL); // extract a 7-bit chunk
      value >>= 7;

      more = value != 0;
      if (more)
      {
        chunk |= 0x80;
      } // set msb marker that more bytes are coming

      writer.Write(chunk);
      bytes += 1;
    }
  }

  public static long ReadLeb128Signed(this BinaryReader reader) => ReadLeb128Signed(reader, out _);

  public static long ReadLeb128Signed(this BinaryReader reader, out int bytes)
  {
    bytes = 0;

    long value = 0;
    var shift = 0;
    bool more = true, signBitSet = false;

    while (more)
    {
      var next = reader.ReadByte();

      bytes += 1;

      more = (next & 0x80) != 0; // extract msb
      signBitSet = (next & 0x40) != 0; // sign bit is the msb of a 7-bit byte, so 0x40

      var chunk = next & 0x7fL; // extract lower 7 bits
      value |= chunk << shift;
      shift += 7;
    }

    // extend the sign of shorter negative numbers
    if (shift < Int64Bitsize && signBitSet)
    {
      value |= SignExtendMask << shift;
    }

    return value;
  }

  public static ulong ReadLeb128Unsigned(this BinaryReader reader) => ReadLeb128Unsigned(reader, out _);

  public static ulong ReadLeb128Unsigned(this BinaryReader reader, out int bytes)
  {
    bytes = 0;

    ulong value = 0;
    var shift = 0;
    var more = true;

    while (more)
    {
      var next = reader.ReadByte();

      bytes += 1;

      more = (next & 0x80) != 0;
      var chunk = next & 0x7fUL;
      value |= chunk << shift;
      shift += 7;
    }

    return value;
  }
}