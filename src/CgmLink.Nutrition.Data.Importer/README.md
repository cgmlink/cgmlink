# CgmLink.Nutrition.Data.Importer

Imports Open Food Facts JSONL product data into the CGM Link nutrition database.

## What It Does

- `Rebuild` mode inserts products from the JSONL export into the nutrition database.
- `Backfill` mode updates existing products by matching Open Food Facts `code` to CGM Link `Products.Code`.
- The importer computes `ImageUrl` and `ImageThumbUrl` from the Open Food Facts `code` and `images` data using the documented OFF image-folder convention.

## Prerequisites

- A valid SQL Server connection string in `ConnectionStrings:DefaultConnection`.
- An Open Food Facts JSONL export file available locally.
- User secrets or configuration containing:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True"
  },
  "OpenFoodFacts": {
    "Path": "/absolute/path/to/open-food-facts.jsonl"
  }
}
```

You can provide the JSONL path either through configuration or with the `--path` command-line argument.

## Usage

Run from the repository root:

```bash
dotnet run --project src/CgmLink.Nutrition.Data.Importer -- --path "/absolute/path/to/open-food-facts.jsonl"
```

### Rebuild Mode

`Rebuild` is the default mode.

```bash
dotnet run --project src/CgmLink.Nutrition.Data.Importer -- \
  --mode rebuild \
  --path "/absolute/path/to/open-food-facts.jsonl"
```

Behavior:

- Applies pending nutrition database migrations before importing.
- Reads the JSONL file line by line.
- Inserts products in batches.
- Skips rows where `nutriments` is missing.
- Computes front image URLs from the product barcode and selected front-image revision metadata in the JSONL export.

Important:

- `Rebuild` inserts rows; it does not delete existing data first.
- If you want a true rebuild from an empty table, clear the target data before running it.

### Backfill Mode

Use this to add or refresh image fields on existing products without reimporting all nutrition data.

```bash
dotnet run --project src/CgmLink.Nutrition.Data.Importer -- \
  --mode backfill \
  --path "/absolute/path/to/open-food-facts.jsonl"
```

Behavior:

- Matches records by `Products.Code`.
- Updates all rows that share the same code.
- Skips JSONL rows with a blank `code`.
- By default, only writes non-empty computed image values from Open Food Facts.
- Existing image values are left unchanged when the source image fields are blank.

To allow blank source image values to clear existing database values:

```bash
dotnet run --project src/CgmLink.Nutrition.Data.Importer -- \
  --mode backfill \
  --path "/absolute/path/to/open-food-facts.jsonl" \
  --overwrite-missing-images
```

## Command-Line Options

- `--path <file>`: Absolute or relative path to the Open Food Facts JSONL export.
- `--mode <rebuild|backfill>`: Import mode. Defaults to `rebuild`.
- `--batch-size <number>`: Number of records processed per batch. Defaults to `10000`.
- `--overwrite-missing-images`: In `backfill` mode, clears existing image fields when the source JSONL has blank image values.

## Image URL Computation

- Base URL: `https://images.openfoodfacts.org/images/products`
- Product folder rule:
  - If the barcode has fewer than 13 digits, it is left-padded with zeroes to 13 digits.
  - The normalized barcode is split as `3/3/3/rest`.
  - Example: `123` becomes `000/000/000/0123`.
  - Example: `3435660768163` becomes `343/566/076/8163`.
- Selected front-image rule:
  - The importer prefers `front_<lc>` when the JSONL record contains a matching product language code.
  - If that key is missing, it falls back to the first available `front_*` image entry.
  - Full image URL format: `<base>/<folder>/front_<lang>.<rev>.full.jpg`
  - Thumbnail URL format: `<base>/<folder>/front_<lang>.<rev>.100.jpg`

Search responses expose only the computed thumbnail URL. Full product responses expose both computed image fields.
