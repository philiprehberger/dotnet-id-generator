namespace Philiprehberger.IdGenerator;

/// <summary>
/// Generates and validates Stripe-style prefixed IDs (e.g., "usr_01HXYZ...").
/// </summary>
public static class PrefixedId
{
    /// <summary>
    /// Creates a prefixed ID by combining the given prefix with a ULID.
    /// </summary>
    /// <param name="prefix">The prefix to prepend (e.g., "usr", "org", "txn").</param>
    /// <returns>A string in the format "{prefix}_{ulid}".</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix"/> is null or empty.</exception>
    public static string Create(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix must not be null or empty.", nameof(prefix));

        var ulid = new Ulid();
        return $"{prefix}_{ulid}";
    }

    /// <summary>
    /// Validates that an ID starts with the expected prefix and contains a valid ULID.
    /// </summary>
    /// <param name="id">The full prefixed ID string to validate.</param>
    /// <param name="expectedPrefix">The prefix the ID is expected to have.</param>
    /// <returns><c>true</c> if the ID has the expected prefix and a valid ULID; otherwise <c>false</c>.</returns>
    public static bool Validate(string id, string expectedPrefix)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(expectedPrefix))
            return false;

        var prefixWithSeparator = expectedPrefix + "_";
        if (!id.StartsWith(prefixWithSeparator, StringComparison.Ordinal))
            return false;

        var ulidPart = id[prefixWithSeparator.Length..];
        return Ulid.TryParse(ulidPart, out _);
    }

    /// <summary>
    /// Attempts to split a prefixed ID into its prefix and ULID components.
    /// The separator is the last underscore (<c>_</c>), matching the "{prefix}_{ulid}" format produced
    /// by <see cref="Create(string)"/>; ULIDs never contain underscores, so prefixes that contain
    /// underscores (e.g., "acct_live") are supported.
    /// </summary>
    /// <param name="id">The full prefixed ID string to parse.</param>
    /// <param name="prefix">The prefix, if successful; otherwise <see cref="string.Empty"/>.</param>
    /// <param name="ulid">The parsed ULID, if successful; otherwise <c>default</c>.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="id"/> has a non-blank prefix, an underscore separator, and a valid ULID;
    /// otherwise <c>false</c>.
    /// </returns>
    public static bool TryParse(string? id, out string prefix, out Ulid ulid)
    {
        prefix = string.Empty;
        ulid = default;

        if (string.IsNullOrEmpty(id))
            return false;

        var separator = id.LastIndexOf('_');
        if (separator <= 0)
            return false;

        var prefixPart = id.AsSpan(0, separator);
        if (prefixPart.IsWhiteSpace())
            return false;

        if (!Ulid.TryParse(id.AsSpan(separator + 1), out var parsed))
            return false;

        prefix = prefixPart.ToString();
        ulid = parsed;
        return true;
    }
}
