using System.Linq;
using System.Linq.Expressions;

namespace CgmLink.Data.Extensions;

public static class QueryableExtensions
{
    public static IOrderedQueryable<T> OrderByProperty<T>(this IQueryable<T> source, string propertyPath, bool descending = false)
    {
        return OrderByUsing(source, propertyPath, descending ? "OrderByDescending" : "OrderBy");
    }

    public static IOrderedQueryable<T> ThenByProperty<T>(this IOrderedQueryable<T> source, string propertyPath, bool descending = false)
    {
        return OrderByUsing(source, propertyPath, descending ? "ThenByDescending" : "ThenBy");
    }

    private static IOrderedQueryable<T> OrderByUsing<T>(this IQueryable<T> source, string propertyPath, string method)
    {
        var parameter = Expression.Parameter(typeof(T), "item");
        var member = propertyPath.Split('.')
            .Aggregate((Expression)parameter, Expression.PropertyOrField);
        var keySelector = Expression.Lambda(member, parameter);
        var methodCall = Expression.Call(
            typeof(Queryable),
            method,
            [parameter.Type, member.Type],
            source.Expression,
            Expression.Quote(keySelector));

        return (IOrderedQueryable<T>)source.Provider.CreateQuery(methodCall);
    }
}
