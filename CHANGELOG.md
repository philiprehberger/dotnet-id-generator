# Changelog

## 0.2.0 (2026-06-14)

- Add `Ulid.NewMonotonic()` and `Id.NewMonotonicUlid()` for strictly-increasing ULIDs within the same millisecond
- Add `Ulid.FromBytes(ReadOnlySpan<byte>)` and `Ulid.ToByteArray()` for binary round-trip
- Add `Id.NewGuidV7()` returning a UUID v7 `Guid` per RFC 9562
- Fix `Ulid.TryParse` round-trip — decoder previously dropped two bits per byte, causing `Ulid.Parse(ulid.ToString())` to return incorrect values
- Add xUnit test project under `tests/Philiprehberger.IdGenerator.Tests/` with full coverage of every public class
- Add `dotnet test` step to the CI workflow
- Add card image to README

## 0.1.8 (2026-03-31)

- Standardize README to 3-badge format with emoji Support section
- Update CI actions to v5 for Node.js 24 compatibility
- Add GitHub issue templates, dependabot config, and PR template

## 0.1.7 (2026-03-26)

- Add Sponsor badge to README
- Fix License section format
- Add trailing period to description

## 0.1.6 (2026-03-23)

- Sync .csproj description with README

## 0.1.5 (2026-03-22)

- Fix changelog formatting

## 0.1.4 (2026-03-17)

- Rename Install section to Installation in README per package guide

## 0.1.3 (2026-03-16)

- Add Development section to README
- Add GenerateDocumentationFile, RepositoryType, PackageReadmeFile to .csproj

## 0.1.1 (2026-03-16)

- Fix: add NuGet publishing secret

## 0.1.0 (2026-03-15)

- Initial release
- ULID generation with Crockford Base32 encoding
- NanoID generation with configurable alphabet and size
- Stripe-style prefixed ID generation and validation
- Short ID generation for compact identifiers
- System.Text.Json converter for ULID serialization
