# Philiprehberger.IdGenerator

[![CI](https://github.com/philiprehberger/dotnet-id-generator/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-id-generator/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.IdGenerator.svg)](https://www.nuget.org/packages/Philiprehberger.IdGenerator)
[![License](https://img.shields.io/github/license/philiprehberger/dotnet-id-generator)](LICENSE)

Sortable, URL-safe unique ID generators — ULID, NanoID, and prefixed IDs.

## Install

```bash
dotnet add package Philiprehberger.IdGenerator
```

## Usage

```csharp
using Philiprehberger.IdGenerator;

// Generate a ULID (sortable, 26 chars)
var ulid = Id.NewUlid();
Console.WriteLine(ulid); // "01HXYZ..."

// Generate a NanoID (21 chars, URL-safe)
var nano = Id.NewNanoId();
Console.WriteLine(nano); // "V1StGXR8_Z5jdHi6B-myT"

// Generate a NanoID with custom size and alphabet
var custom = Id.NewNanoId(size: 10, alphabet: "0123456789abcdef");

// Generate a Stripe-style prefixed ID
var prefixed = Id.NewPrefixed("usr");
Console.WriteLine(prefixed); // "usr_01HXYZ..."

// Generate a short ID (12 chars)
var shortId = Id.NewShortId();
Console.WriteLine(shortId); // "a8f3kQ_9xZrT"
```

### ULID

```csharp
using Philiprehberger.IdGenerator;

var ulid = new Ulid();

// Extract timestamp
DateTimeOffset timestamp = ulid.Timestamp;

// Parse from string
var parsed = Ulid.Parse("01HXYZ...");

// Safe parsing
if (Ulid.TryParse("01HXYZ...", out var result))
{
    Console.WriteLine(result);
}

// Convert to GUID
Guid guid = ulid.ToGuid();

// ULIDs are sortable
var a = Id.NewUlid();
var b = Id.NewUlid();
Console.WriteLine(a < b); // true (a was created first)
```

### Prefixed IDs

```csharp
using Philiprehberger.IdGenerator;

// Create prefixed IDs for different entity types
var userId = PrefixedId.Create("usr");   // "usr_01HXYZ..."
var orgId = PrefixedId.Create("org");    // "org_01HXYZ..."

// Validate a prefixed ID
bool valid = PrefixedId.Validate("usr_01HXYZ...", "usr"); // true
bool wrong = PrefixedId.Validate("usr_01HXYZ...", "org"); // false
```

### JSON Serialization

```csharp
using System.Text.Json;
using Philiprehberger.IdGenerator;

var options = new JsonSerializerOptions();
options.Converters.Add(new UlidJsonConverter());

var ulid = Id.NewUlid();
var json = JsonSerializer.Serialize(ulid, options); // "\"01HXYZ...\""
var back = JsonSerializer.Deserialize<Ulid>(json, options);
```

## API

### `Id` (static factory)

| Method | Description |
|--------|-------------|
| `NewUlid()` | Generate a new ULID |
| `NewNanoId(int size = 21, string? alphabet = null)` | Generate a NanoID |
| `NewPrefixed(string prefix)` | Generate a prefixed ID |
| `NewShortId(int length = 12)` | Generate a short random ID |

### `Ulid` (readonly struct)

| Member | Description |
|--------|-------------|
| `Ulid()` | Create a new ULID with current timestamp |
| `Timestamp` | Extract the timestamp as DateTimeOffset |
| `Parse(string)` | Parse a ULID from a 26-char string |
| `TryParse(string?, out Ulid)` | Safely parse a ULID string |
| `ToGuid()` | Convert to a Guid |
| `ToString()` | 26-char Crockford Base32 representation |
| `==`, `!=`, `<`, `>`, `<=`, `>=` | Comparison operators |

### `NanoId` (static)

| Method | Description |
|--------|-------------|
| `Generate(int size = 21, string? alphabet = null)` | Generate a random NanoID |

### `PrefixedId` (static)

| Method | Description |
|--------|-------------|
| `Create(string prefix)` | Create a prefixed ID with a ULID suffix |
| `Validate(string id, string expectedPrefix)` | Check prefix and ULID validity |

### `UlidJsonConverter`

| Description |
|-------------|
| System.Text.Json converter for ULID serialization/deserialization |

## Development

```bash
dotnet build src/Philiprehberger.IdGenerator.csproj --configuration Release
```

## License

MIT
