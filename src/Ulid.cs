using System.Security.Cryptography;

namespace Philiprehberger.IdGenerator;

/// <summary>
/// A ULID (Universally Unique Lexicographically Sortable Identifier).
/// 128-bit value: 48-bit timestamp + 80-bit random, encoded as 26 Crockford Base32 characters.
/// </summary>
public readonly struct Ulid : IComparable<Ulid>, IEquatable<Ulid>, ISpanParsable<Ulid>, ISpanFormattable
{
    private const string CrockfordBase32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int EncodedLength = 26;
    private const int TimestampBytes = 6;
    private const int RandomBytes = 10;

    private static readonly long UnixEpochMs = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

    private static readonly object MonotonicLock = new();
    private static long _lastMonotonicTimestamp;
    private static byte[]? _lastMonotonicRandom;

    private readonly byte[] _bytes;

    /// <summary>
    /// Creates a new ULID with the current timestamp and cryptographically random data.
    /// </summary>
    public Ulid()
    {
        _bytes = new byte[16];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        WriteTimestamp(_bytes, timestamp);
        RandomNumberGenerator.Fill(_bytes.AsSpan(TimestampBytes));
    }

    private Ulid(byte[] bytes)
    {
        _bytes = bytes;
    }

    /// <summary>
    /// Creates a new ULID guaranteed to sort strictly after any previous monotonic ULID
    /// generated in the same process, even when called multiple times within the same millisecond.
    /// </summary>
    /// <returns>A new monotonic <see cref="Ulid"/>.</returns>
    public static Ulid NewMonotonic()
    {
        var bytes = new byte[16];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        lock (MonotonicLock)
        {
            if (timestamp == _lastMonotonicTimestamp && _lastMonotonicRandom is not null)
            {
                Array.Copy(_lastMonotonicRandom, 0, bytes, TimestampBytes, RandomBytes);
                IncrementRandom(bytes);
            }
            else
            {
                RandomNumberGenerator.Fill(bytes.AsSpan(TimestampBytes));
            }

            WriteTimestamp(bytes, timestamp);
            _lastMonotonicTimestamp = timestamp;
            _lastMonotonicRandom = new byte[RandomBytes];
            Array.Copy(bytes, TimestampBytes, _lastMonotonicRandom, 0, RandomBytes);
        }

        return new Ulid(bytes);
    }

    /// <summary>
    /// Creates a <see cref="Ulid"/> from its 16-byte binary representation.
    /// </summary>
    /// <param name="bytes">A 16-byte span containing the ULID payload (6-byte timestamp + 10 random).</param>
    /// <returns>A <see cref="Ulid"/> wrapping a copy of the input.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="bytes"/> is not exactly 16 bytes long.</exception>
    public static Ulid FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 16)
            throw new ArgumentException("ULID byte representation must be exactly 16 bytes.", nameof(bytes));

        var copy = new byte[16];
        bytes.CopyTo(copy);
        return new Ulid(copy);
    }

    /// <summary>
    /// The smallest possible ULID — all 16 bytes zero. Sorts before every other ULID.
    /// </summary>
    public static Ulid MinValue => new(new byte[16]);

    /// <summary>
    /// The largest possible ULID — all 16 bytes <c>0xFF</c>. Sorts after every other ULID.
    /// </summary>
    public static Ulid MaxValue
    {
        get
        {
            var bytes = new byte[16];
            Array.Fill(bytes, (byte)0xFF);
            return new Ulid(bytes);
        }
    }

    /// <summary>
    /// Creates a ULID with the given timestamp and a fixed random component of either all-zero
    /// (lower bound) or all-<c>0xFF</c> (upper bound). Useful for building inclusive range bounds
    /// over ULID-keyed data: <c>id BETWEEN FromTimestamp(start) AND FromTimestamp(end, maxRandom: true)</c>.
    /// </summary>
    /// <param name="timestamp">The timestamp to encode in the ULID (millisecond precision).</param>
    /// <param name="maxRandom">
    /// When <c>false</c> (default), the random component is all zero (the smallest ULID for the timestamp).
    /// When <c>true</c>, it is all <c>0xFF</c> (the largest ULID for the timestamp).
    /// </param>
    /// <returns>A boundary <see cref="Ulid"/> for the given timestamp.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timestamp"/> is before the Unix epoch.</exception>
    public static Ulid FromTimestamp(DateTimeOffset timestamp, bool maxRandom = false)
    {
        var ms = timestamp.ToUnixTimeMilliseconds();
        if (ms < 0)
            throw new ArgumentOutOfRangeException(nameof(timestamp), "Timestamp must be at or after the Unix epoch (1970-01-01).");

        var bytes = new byte[16];
        WriteTimestamp(bytes, ms);

        if (maxRandom)
        {
            for (var i = TimestampBytes; i < 16; i++)
                bytes[i] = 0xFF;
        }

        return new Ulid(bytes);
    }

    /// <summary>
    /// Returns a copy of the 16-byte binary representation of this ULID.
    /// </summary>
    /// <returns>A 16-byte array containing the timestamp (first 6 bytes) and random (last 10 bytes) components.</returns>
    public byte[] ToByteArray()
    {
        var copy = new byte[16];
        if (_bytes is not null)
            Array.Copy(_bytes, copy, Math.Min(_bytes.Length, 16));
        return copy;
    }

    /// <summary>
    /// Gets the timestamp component of the ULID as a <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset Timestamp
    {
        get
        {
            long ms = 0;
            for (int i = 0; i < TimestampBytes; i++)
            {
                ms = (ms << 8) | _bytes[i];
            }
            return DateTimeOffset.FromUnixTimeMilliseconds(ms);
        }
    }

    /// <summary>
    /// Parses a 26-character Crockford Base32 string into a <see cref="Ulid"/>.
    /// </summary>
    /// <param name="input">The ULID string to parse.</param>
    /// <returns>The parsed ULID.</returns>
    /// <exception cref="FormatException">Thrown when the input is not a valid ULID string.</exception>
    public static Ulid Parse(string input)
    {
        if (!TryParse(input, out var ulid))
            throw new FormatException($"Invalid ULID string: '{input}'");
        return ulid;
    }

    /// <summary>
    /// Parses a 26-character Crockford Base32 string into a <see cref="Ulid"/>.
    /// </summary>
    /// <param name="s">The ULID string to parse.</param>
    /// <param name="provider">Ignored; ULID parsing is culture-invariant.</param>
    /// <returns>The parsed ULID.</returns>
    /// <exception cref="FormatException">Thrown when the input is not a valid ULID string.</exception>
    public static Ulid Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Parses a 26-character Crockford Base32 span into a <see cref="Ulid"/>.
    /// Decoding is case-insensitive.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <returns>The parsed ULID.</returns>
    /// <exception cref="FormatException">Thrown when the input is not a valid ULID string.</exception>
    public static Ulid Parse(ReadOnlySpan<char> s)
    {
        if (!TryParse(s, out var ulid))
            throw new FormatException($"Invalid ULID string: '{s.ToString()}'");
        return ulid;
    }

    /// <summary>
    /// Parses a 26-character Crockford Base32 span into a <see cref="Ulid"/>.
    /// Decoding is case-insensitive.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="provider">Ignored; ULID parsing is culture-invariant.</param>
    /// <returns>The parsed ULID.</returns>
    /// <exception cref="FormatException">Thrown when the input is not a valid ULID string.</exception>
    public static Ulid Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

    /// <summary>
    /// Attempts to parse a 26-character Crockford Base32 string into a <see cref="Ulid"/>.
    /// </summary>
    /// <param name="input">The ULID string to parse.</param>
    /// <param name="result">The parsed ULID, if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(string? input, out Ulid result)
    {
        if (input is null)
        {
            result = default;
            return false;
        }

        return TryParse(input.AsSpan(), out result);
    }

    /// <summary>
    /// Attempts to parse a 26-character Crockford Base32 string into a <see cref="Ulid"/>.
    /// </summary>
    /// <param name="s">The ULID string to parse.</param>
    /// <param name="provider">Ignored; ULID parsing is culture-invariant.</param>
    /// <param name="result">The parsed ULID, if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Ulid result) => TryParse(s, out result);

    /// <summary>
    /// Attempts to parse a 26-character Crockford Base32 span into a <see cref="Ulid"/>
    /// without allocating intermediate buffers. Decoding is case-insensitive.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="provider">Ignored; ULID parsing is culture-invariant.</param>
    /// <param name="result">The parsed ULID, if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Ulid result) => TryParse(s, out result);

    /// <summary>
    /// Attempts to parse a 26-character Crockford Base32 span into a <see cref="Ulid"/>
    /// without allocating intermediate buffers. Decoding is case-insensitive.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="result">The parsed ULID, if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out Ulid result)
    {
        result = default;

        if (s.Length != EncodedLength)
            return false;

        Span<byte> bytes = stackalloc byte[16];

        // 26 chars × 5 bits = 130 bits, of which the top 2 are zero padding
        // added during encoding. Discard those 2 leading bits before extracting bytes.
        int bitBuffer = 0;
        int bitsInBuffer = 0;
        int byteIndex = 0;

        for (int i = 0; i < EncodedLength; i++)
        {
            var idx = CrockfordBase32.IndexOf(char.ToUpperInvariant(s[i]));
            if (idx < 0) return false;

            bitBuffer = (bitBuffer << 5) | idx;
            bitsInBuffer += 5;

            if (i == 0)
            {
                bitsInBuffer -= 2;
                bitBuffer &= (1 << bitsInBuffer) - 1;
            }

            while (bitsInBuffer >= 8 && byteIndex < 16)
            {
                bitsInBuffer -= 8;
                bytes[byteIndex++] = (byte)(bitBuffer >> bitsInBuffer);
                bitBuffer &= (1 << bitsInBuffer) - 1;
            }
        }

        result = new Ulid(bytes.ToArray());
        return true;
    }

    /// <summary>
    /// Converts this ULID to a <see cref="Guid"/>.
    /// </summary>
    /// <returns>A GUID representation of this ULID.</returns>
    public Guid ToGuid()
    {
        return new Guid(_bytes);
    }

    /// <summary>
    /// Creates a <see cref="Ulid"/> from a <see cref="Guid"/>. This is the inverse of
    /// <see cref="ToGuid"/> — <c>Ulid.FromGuid(u.ToGuid())</c> returns a ULID equal to <c>u</c>.
    /// </summary>
    /// <param name="guid">The GUID whose 16 bytes back the ULID.</param>
    /// <returns>A <see cref="Ulid"/> wrapping the GUID's bytes.</returns>
    public static Ulid FromGuid(Guid guid)
    {
        return new Ulid(guid.ToByteArray());
    }

    /// <summary>
    /// Returns the 26-character Crockford Base32 string representation of this ULID.
    /// </summary>
    public override string ToString()
    {
        return string.Create(EncodedLength, this, static (chars, ulid) => ulid.Encode(chars));
    }

    /// <summary>
    /// Returns the 26-character Crockford Base32 string representation of this ULID.
    /// </summary>
    /// <param name="format">The format to use. Only <c>null</c>, empty, or <c>"G"</c> (canonical uppercase) is supported.</param>
    /// <param name="formatProvider">Ignored; ULID formatting is culture-invariant.</param>
    /// <returns>The canonical ULID string.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format"/> is not supported.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        ValidateFormat(format);
        return ToString();
    }

    /// <summary>
    /// Writes the 26-character Crockford Base32 representation of this ULID into
    /// <paramref name="destination"/> without allocating.
    /// </summary>
    /// <param name="destination">The span to write into. Must hold at least 26 characters.</param>
    /// <param name="charsWritten">The number of characters written (26 on success, 0 on failure).</param>
    /// <param name="format">The format to use. Only empty or <c>"G"</c> (canonical uppercase) is supported.</param>
    /// <param name="provider">Ignored; ULID formatting is culture-invariant.</param>
    /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too small.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format"/> is not supported.</exception>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    {
        ValidateFormat(format);

        if (destination.Length < EncodedLength)
        {
            charsWritten = 0;
            return false;
        }

        Encode(destination);
        charsWritten = EncodedLength;
        return true;
    }

    private static void ValidateFormat(ReadOnlySpan<char> format)
    {
        if (format.Length == 0 || (format.Length == 1 && format[0] == 'G'))
            return;

        throw new FormatException($"Unsupported ULID format: '{format.ToString()}'. Use null, empty, or \"G\".");
    }

    private void Encode(Span<char> chars)
    {
        // Encode 16 bytes (128 bits) into 26 Crockford Base32 characters (130 bits, 2 leading zero bits).
        // A default-initialized struct has no backing array and encodes as all zeros.
        var bytes = _bytes;
        if (bytes is null)
        {
            chars[..EncodedLength].Fill('0');
            return;
        }

        // Prepend 2 zero bits to make 130 bits total
        int bitBuffer = 0;
        int bitsInBuffer = 2;
        int charIndex = 0;

        for (int i = 0; i < 16 && charIndex < EncodedLength; i++)
        {
            bitBuffer = (bitBuffer << 8) | bytes[i];
            bitsInBuffer += 8;

            while (bitsInBuffer >= 5 && charIndex < EncodedLength)
            {
                bitsInBuffer -= 5;
                chars[charIndex++] = CrockfordBase32[(bitBuffer >> bitsInBuffer) & 0x1F];
                bitBuffer &= (1 << bitsInBuffer) - 1;
            }
        }
    }

    /// <inheritdoc />
    public int CompareTo(Ulid other)
    {
        var a = _bytes ?? Array.Empty<byte>();
        var b = other._bytes ?? Array.Empty<byte>();
        for (int i = 0; i < 16; i++)
        {
            var av = i < a.Length ? a[i] : (byte)0;
            var bv = i < b.Length ? b[i] : (byte)0;
            var cmp = av.CompareTo(bv);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    /// <inheritdoc />
    public bool Equals(Ulid other) => CompareTo(other) == 0;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Ulid other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        var bytes = _bytes ?? Array.Empty<byte>();
        foreach (var b in bytes)
            hash.Add(b);
        return hash.ToHashCode();
    }

    /// <summary>Determines whether two ULIDs are equal.</summary>
    public static bool operator ==(Ulid left, Ulid right) => left.Equals(right);

    /// <summary>Determines whether two ULIDs are not equal.</summary>
    public static bool operator !=(Ulid left, Ulid right) => !left.Equals(right);

    /// <summary>Determines whether the left ULID is less than the right ULID.</summary>
    public static bool operator <(Ulid left, Ulid right) => left.CompareTo(right) < 0;

    /// <summary>Determines whether the left ULID is greater than the right ULID.</summary>
    public static bool operator >(Ulid left, Ulid right) => left.CompareTo(right) > 0;

    /// <summary>Determines whether the left ULID is less than or equal to the right ULID.</summary>
    public static bool operator <=(Ulid left, Ulid right) => left.CompareTo(right) <= 0;

    /// <summary>Determines whether the left ULID is greater than or equal to the right ULID.</summary>
    public static bool operator >=(Ulid left, Ulid right) => left.CompareTo(right) >= 0;

    private static void WriteTimestamp(byte[] bytes, long timestampMs)
    {
        bytes[0] = (byte)(timestampMs >> 40);
        bytes[1] = (byte)(timestampMs >> 32);
        bytes[2] = (byte)(timestampMs >> 24);
        bytes[3] = (byte)(timestampMs >> 16);
        bytes[4] = (byte)(timestampMs >> 8);
        bytes[5] = (byte)timestampMs;
    }

    private static void IncrementRandom(byte[] bytes)
    {
        for (var i = 15; i >= TimestampBytes; i--)
        {
            if (++bytes[i] != 0)
                return;
        }

        throw new OverflowException("Monotonic ULID random component overflowed; cannot maintain ordering within the current millisecond.");
    }
}
