using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CgmLink.Data.Enums;

namespace CgmLink.Data.Extensions;

public static class QueryableExtensions
{
    public static IOrderedQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> sortFields,
        string? sortBy = null,
        SortDirection direction = SortDirection.Desc)
    {
        var field = string.IsNullOrWhiteSpace(sortBy) ? "Created" : sortBy;
        var keySelector = sortFields.GetValueOrDefault(field) ?? sortFields.GetValueOrDefault("Created");
        if (keySelector is null)
        {
            throw new ArgumentException($"Unknown sort field '{field}'.", nameof(sortBy));
        }

        return direction == SortDirection.Asc ? source.OrderBy(keySelector) : source.OrderByDescending(keySelector);
    }
}
