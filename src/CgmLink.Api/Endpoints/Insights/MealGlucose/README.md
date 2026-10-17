# Meal glucose insight

`GET /api/v1/insights/meal-glucose?from=2026-10-01T00:00:00Z&to=2026-10-14T00:00:00Z&intervalMinutes=5`

Requires authentication. Both dates are optional; `to` defaults to now and `from` to seven days before `to`. Equal or reversed dates return a validation problem.

`intervalMinutes` is an optional whole number. Its default and minimum come from `Insights:MealGlucose` configuration (30 and 5 minutes in the supplied settings). It must be no greater than the actual duration between the resolved `from` and `to` dates, in minutes. Validation uses elapsed time across offsets without rounding up. An invalid interval returns a validation problem. The default interval must also fit within the period.

The period selects non-deleted food treatments belonging to the current user, including meals, personal ingredients, and external nutrition ingredients. Only treatments with a linked bolus injection and a usable pre-food baseline contribute. Series are grouped by insulin ID, even when different insulins share a name. Basal injections and injection-only treatments are excluded.

Each series starts at 0 minutes after food and advances by `intervalMinutes`, including points up to the configured `WindowMinutes`. With the supplied six-hour window, the default gives 13 points; a five-minute interval gives 73. An interval that does not divide the window stops at the last multiple within it. An interval greater than the window returns only the point at zero. The observation window remains separate from the period selecting treatments. Insulin duration does not stretch the time axis. Glucose values use mg/dL, matching the other glucose endpoints; the client handles conversion for display. Time fields explicitly use minutes, so neither glucose nor time unit metadata is sent.

All timing settings are configured in `appsettings.json` under `Insights:MealGlucose`, and can be overridden by environment variables such as `Insights__MealGlucose__WindowMinutes`:

```json
"Insights": {
  "MealGlucose": {
    "WindowMinutes": 360,
    "DefaultIntervalMinutes": 30,
    "MinimumIntervalMinutes": 5,
    "BaselineMinutes": 15,
    "ToleranceMinutes": 10
  }
}
```

Window and interval values must be positive; baseline and tolerance can be zero. The default interval must be at least the minimum. Invalid settings fail validation at startup.

- Baseline: the latest reading at or before food, within the configured `BaselineMinutes` (15 in the supplied settings).
- Later points: the closest reading within the configured `ToleranceMinutes` (10 in the supplied settings); ties choose the earlier reading. Readings are not interpolated or carried forward across gaps.
- Each treatment contributes at most one reading to each point, so denser recording does not give a treatment more weight.
- Points show absolute glucose levels. Baseline matching and reading tolerance are internal calculation rules, not response fields.
- Missing readings do not contribute to an average or percentile. Missing points contain null averages and percentiles. No eligible treatments return an empty array.
- The 25th and 75th percentiles use linear interpolation at `(count - 1) * fraction`. Bands show variation between treatments, not confidence intervals.

The response is an array of insulin series. Each series contains only `insulinId`, `insulinName`, and `points`. Each point contains only `minutesAfterFood`, `averageGlucoseLevel`, `p25GlucoseLevel`, and `p75GlucoseLevel`.

```json
[
  {
    "insulinId": "9afcb324-425d-4cf9-a312-16bc5d123abc",
    "insulinName": "Insulin A",
    "points": [
      {
        "minutesAfterFood": 60,
        "averageGlucoseLevel": 144,
        "p25GlucoseLevel": 126,
        "p75GlucoseLevel": 162
      }
    ]
  }
]
```

The example is shortened; actual series include all points at the requested interval, including points with null values.

Baseline and follow-up readings may fall outside the treatment selection period. Recent treatments may have incomplete follow-up. Subsequent food, injections, and other influences are not adjusted for or excluded; these are observed patterns, not isolated insulin or food effects.

`InsightsService` keeps each insight's queries explicit. Meal glucose loads only timestamps and glucose levels for the current user's bounded observation period once, then uses binary search for matching. This avoids a query per treatment or point. Memory use scales with readings in that period; moving aggregation into SQL should follow measurement against representative data.
