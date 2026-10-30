using System.Security.Cryptography;

namespace Philiprehberger.IdGenerator;

/// <summary>
/// Generates NanoID-style unique identifiers using a cryptographically secure random source.
/// </summary>
public static class NanoId
{
    /// <summary>
    /// The default URL-safe alphabet: A-Za-z0-9_-
    /// </summary>
    public const string DefaultAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";

    /// <summary>
    /// Common alphabets for use with <see cref="Generate(int, string?)"/>.
    /// </summary>
    public static class Alphabets
    {
        /// <summary>
        /// The default URL-safe alphabet: A-Za-z0-9_- (same as <see cref="NanoId.DefaultAlphabet"/>).
        /// </summary>
        public const string Default = DefaultAlphabet;

        /// <summary>
        /// Decimal digits only: 0-9.
        /// </summary>
        public const string Numbers = "0123456789";

        /// <summary>
        /// Lowercase ASCII letters only: a-z.
        /// </summary>
        public const string Lowercase = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Uppercase and lowercase ASCII letters and digits: A-Za-z0-9 (no symbols).
        /// </summary>
        public const string Alphanumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        /// <summary>
        /// Letters and digits with visually ambiguous characters removed
        /// (<c>1</c>, <c>l</c>, <c>I</c>, <c>0</c>, <c>O</c>, <c>o</c>, <c>u</c>, <c>v</c>, <c>5</c>, <c>S</c>, <c>s</c>, <c>2</c>, <c>Z</c>).
        /// Useful for IDs that people read aloud or type by hand.
        /// </summary>
        public const string NoLookAlikes = "346789ABCDEFGHJKLMNPQRTUVWXYabcdefghijkmnpqrtwxyz";
    }

    /// <summary>
    /// Generates a NanoID string.
    /// </summary>
    /// <param name="size">The length of the generated ID. Defaults to 21.</param>
    /// <param name="alphabet">
    /// The character set to use. Defaults to the URL-safe alphabet (A-Za-z0-9_-).
    /// </param>
    /// <returns>A randomly generated ID string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is less than 1.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="alphabet"/> is empty.</exception>
    public static string Generate(int size = 21, string? alphabet = null)
    {
        if (size < 1)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be at least 1.");

        var chars = alphabet ?? DefaultAlphabet;
        if (chars.Length == 0)
            throw new ArgumentException("Alphabet must not be empty.", nameof(alphabet));

        // Use bit masking to reduce bias when alphabet size is a power of two or close to it
        var mask = (1 << (int)Math.Ceiling(Math.Log2(chars.Length))) - 1;
        var step = (int)Math.Ceiling(1.6 * mask * size / chars.Length);

        var result = new char[size];
        var count = 0;
        var randomBytes = new byte[step];

        while (count < size)
        {
            RandomNumberGenerator.Fill(randomBytes);

            for (int i = 0; i < step && count < size; i++)
            {
                var idx = randomBytes[i] & mask;
                if (idx < chars.Length)
                {
                    result[count++] = chars[idx];
                }
            }
        }

        return new string(result);
    }
}
