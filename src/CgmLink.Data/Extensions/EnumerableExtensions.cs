using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CgmLink.Data.Extensions;

public static class EnumerableExtensions
{
    public static IOrderedEnumerable<T> OrderByProperty<T>(this IEnumerable<T> source, string propertyPath, bool descending = false)
    {
        var keySelector = BuildKeySelector<T>(propertyPath);
        return descending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
    }

    public static IOrderedEnumerable<T> ThenByProperty<T>(this IOrderedEnumerable<T> source, string propertyPath, bool descending = false)
    {
        var keySelector = BuildKeySelector<T>(propertyPath);
        return descending ? source.ThenByDescending(keySelector) : source.ThenBy(keySelector);
    }

    private static Func<T, object> BuildKeySelector<T>(string propertyPath)
    {
        var parameter = Expression.Parameter(typeof(T), "item");
        Expression body = parameter;

        foreach (var propertyName in propertyPath.Split('.'))
        {
            body = Expression.Property(body, propertyName);
        }

        var converted = Expression.Convert(body, typeof(object));
        return Expression.Lambda<Func<T, object>>(converted, parameter).Compile();
    }
}
