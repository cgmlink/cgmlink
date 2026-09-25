# CgmLink.Nutrition

Nutrition subsystem integrating food databases (currently [fatsecret](https://platform.fatsecret.com/))
into CGM Link.

## Purpose

- Search nutrition sources for foods and retrieve food details.
- Attach nutrition source products to ingredients (`Ingredient.ProductId`, `IngredientServing.ExternalId`, `Ingredient.Barcode`).
- Aggregate nutrition onto meals and treatments.

## Storage rules

| What | Stored |
|------|--------|
| `Ingredient.Id`, `Ingredient.ProductId`, `Ingredient.Barcode`, `IngredientServing.ExternalId` | Permanently |
| Aggregated nutrition on `Meal` / `Treatment` | Permanently |
| Cached product records (product id, barcode, name, nutrition values) | Temporarily, max 24h |

The provider-specific product data is cached in a **separate** database (see `Nutrition:CacheConnectionString`)
so it never touches the user data database. The cache is provider-agnostic: it stores only the values the
API needs (`NutritionProduct`), not raw provider JSON, so another nutrition source can replace or join
fatsecret later. Reads treat expired cache rows as misses and refetch from the source.

The cache itself is behind the `INutritionCache` abstraction, selected by `Nutrition:CacheProvider`
(`ef` today; `distributed` and `memory` backends can be added as separate implementations). The `ef`
backend has no TTL, so a background hosted service (`SqlCacheCleanupService`) deletes expired
rows on a schedule.

## Nutrition sources

### fatsecret

The fatsecret API is called with OAuth 2.0 client credentials. The client uses a cached bearer token and
retrieves food data through the `foods.search` and `food.get` methods.

## Configuration

Options live in `appsettings.json` / user secrets. Nutrition services are only registered when
`Nutrition:CacheConnectionString` is set; the fatsecret options are registered only when its credentials
are provided:

```json
{
  "Nutrition": {
    "CacheProvider": "ef",
    "CacheConnectionString": "",
    "CacheExpiry": "24:00:00",
    "CacheCleanupInterval": "01:00:00"
  },
  "FatSecret": {
    "ClientId": "",
    "ClientSecret": "",
    "TokenUrl": "https://oauth.fatsecret.com/connect/token",
    "Scope": "basic",
    "Region": "US",
    "Language": "en",
    "ApiBaseUrl": "https://platform.fatsecret.com/rest/"
  }
}
```

## Endpoints

Planned (all authenticated and versioned under `/api/v1/nutrition`):

| Method | Route | nutrition source |
|--------|-------|------------------|
| GET | `/api/v1/nutrition/search?q=&page=&pageSize=` | `foods.search` |
| GET | `/api/v1/nutrition/foods/{foodId}` | `food.get` |
| GET | `/api/v1/nutrition/foods/barcode/{barcode}` | barcode lookup (no match → 404) |
| POST | `/api/v1/nutrition/ingredients` `{ foodId, servingIds? }` | — |

## Projects

- `CgmLink.Nutrition` — nutrition endpoints and DI wiring.
- `CgmLink.Nutrition.Caching` — cache abstraction (`INutritionCache`, `INutritionDbInitializer`, `NutritionProduct`, options). No storage backend dependencies.
- `CgmLink.Nutrition.Caching.Ef` — EF backed cache (`NutritionCacheDbContext`, `SqlServerNutritionCache`, cleanup service, initializer).
- `CgmLink.Nutrition.Caching.Ef.Migrators.MSSQL` — migrations for the cache schema.
- `CgmLink.Nutrition.FatSecretClient` — fatsecret options and client registration.
- `CgmLink.Nutrition.Caching.Distributed` — planned distributed (e.g. redis) cache backend.

## Migrations

Nutrition cache schema lives in `CgmLink.Nutrition.Caching.Ef.Migrators.MSSQL`. Add a migration with:

```powershell
.\scripts\add-migration.ps1 -dbContext NutritionCacheDbContext -name <name>
```

## Tests

`CgmLink.Nutrition.Tests` (NUnit). Run with:

```powershell
dotnet test src/CgmLink.Nutrition.Tests
```
