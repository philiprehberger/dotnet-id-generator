using System.Security.Cryptography;

namespace Philiprehberger.IdGenerator;

/// <summary>
/// A ULID (Universally Unique Lexicographically Sortable Identifier).
/// 128-bit value: 48-bit timestamp + 80-bit random, encoded as 26 Crockford Base32 characters.
/// </summary>
public readonly struct Ulid : IComparable<Ulid>, IEquatable<Ulid>
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
    /// Attempts to parse a 26-character Crockford Base32 string into a <see cref="Ulid"/>.
    /// </summary>
    /// <param name="input">The ULID string to parse.</param>
    /// <param name="result">The parsed ULID, if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParse(string? input, out Ulid result)
    {
        result = default;

        if (input is null || input.Length != EncodedLength)
            return false;

        var upper = input.ToUpperInvariant();
        var bytes = new byte[16];

        var bits = new int[EncodedLength];
        for (int i = 0; i < EncodedLength; i++)
        {
            var idx = CrockfordBase32.IndexOf(upper[i]);
            if (idx < 0) return false;
            bits[i] = idx;
        }

        // 26 chars × 5 bits = 130 bits, of which the top 2 are zero padding
        // added during encoding. Discard those 2 leading bits before extracting bytes.
        int bitBuffer = 0;
        int bitsInBuffer = 0;
        int byteIndex = 0;

        for (int i = 0; i < EncodedLength; i++)
        {
            bitBuffer = (bitBuffer << 5) | bits[i];
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

        result = new Ulid(bytes);
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
    /// Returns the 26-character Crockford Base32 string representation of this ULID.
    /// </summary>
    public override string ToString()
    {
        // Encode 16 bytes (128 bits) into 26 Crockford Base32 characters (130 bits, 2 leading zero bits)
        var chars = new char[EncodedLength];
        int bitBuffer = 0;
        int bitsInBuffer = 0;
        int charIndex = 0;

        // Prepend 2 zero bits to make 130 bits total
        bitBuffer = 0;
        bitsInBuffer = 2;

        for (int i = 0; i < 16 && charIndex < EncodedLength; i++)
        {
            bitBuffer = (bitBuffer << 8) | _bytes[i];
            bitsInBuffer += 8;

            while (bitsInBuffer >= 5 && charIndex < EncodedLength)
            {
                bitsInBuffer -= 5;
                chars[charIndex++] = CrockfordBase32[(bitBuffer >> bitsInBuffer) & 0x1F];
                bitBuffer &= (1 << bitsInBuffer) - 1;
            }
        }

        return new string(chars);
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
