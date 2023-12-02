namespace Bxes.Utils;

//https://github.com/rzubek/mini-leb128/blob/master/LEB128.cs
public static class Leb128
{
  private const long SignExtendMask = -1L;
  private const int Int64Bitsize = sizeof(long) * 8;

  public static void WriteLeb128Signed(this Stream stream, long value) => WriteLeb128Signed(stream, value, out _);

  public static void WriteLeb128Signed(this Stream stream, long value, out int bytes)
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

      stream.WriteByte(chunk);
      bytes += 1;
    }
  }

  public static void WriteLeb128Unsigned(this Stream stream, ulong value) => WriteLeb128Unsigned(stream, value, out _);

  public static void WriteLeb128Unsigned(this Stream stream, ulong value, out int bytes)
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

      stream.WriteByte(chunk);
      bytes += 1;
    }
  }

  public static long ReadLeb128Signed(this Stream stream) => ReadLeb128Signed(stream, out _);

  public static long ReadLeb128Signed(this Stream stream, out int bytes)
  {
    bytes = 0;

    long value = 0;
    var shift = 0;
    bool more = true, signBitSet = false;

    while (more)
    {
      var next = stream.ReadByte();
      if (next < 0)
      {
        throw new InvalidOperationException("Unexpected end of stream");
      }

      var b = (byte)next;
      bytes += 1;

      more = (b & 0x80) != 0; // extract msb
      signBitSet = (b & 0x40) != 0; // sign bit is the msb of a 7-bit byte, so 0x40

      var chunk = b & 0x7fL; // extract lower 7 bits
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

  public static ulong ReadLeb128Unsigned(this Stream stream) => ReadLeb128Unsigned(stream, out _);

  public static ulong ReadLeb128Unsigned(this Stream stream, out int bytes)
  {
    bytes = 0;

    ulong value = 0;
    var shift = 0;
    var more = true;

    while (more)
    {
      var next = stream.ReadByte();
      if (next < 0)
      {
        throw new InvalidOperationException("Unexpected end of stream");
      }

      var b = (byte)next;
      bytes += 1;

      more = (b & 0x80) != 0;
      var chunk = b & 0x7fUL;
      value |= chunk << shift;
      shift += 7;
    }

    return value;
  }
}