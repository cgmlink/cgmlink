using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using CgmLink.Data.Enums;
using CgmLink.Data.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CgmLink.Api.Tests.Extensions;

[TestFixture]
public class QueryableSortExtensionsTests
{
    private static readonly IReadOnlyDictionary<string, Expression<Func<Ingredient, object>>> SortFields =
        new Dictionary<string, Expression<Func<Ingredient, object>>>
        {
            ["Created"] = i => i.Created,
            ["Updated"] = i => i.Updated ?? i.Created,
            ["Name"] = i => i.Name,
            ["TotalCalories"] = i => i.Servings.Sum(s => s.Calories),
        };

    [Test]
    public void ApplySort_Should_Sort_By_Created_Descending_When_No_Sort_Requested()
    {
        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), Name = "Old", Created = DateTimeOffset.UtcNow.AddDays(-2) },
            new Ingredient { Id = Guid.NewGuid(), Name = "New", Created = DateTimeOffset.UtcNow },
        };

        var request = new PagedRequest { Page = 0, PageSize = 10 };

        var result = ingredients.AsQueryable().ApplySort(SortFields, request.SortBy, request.SortDirection).Select(i => i.Name).ToList();

        Assert.That(result, Is.EqualTo(new[] { "New", "Old" }));
    }

    [Test]
    public void ApplySort_Should_Sort_By_Requested_Field_And_Direction()
    {
        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), Name = "Zebra", Created = DateTimeOffset.UtcNow },
            new Ingredient { Id = Guid.NewGuid(), Name = "Apple", Created = DateTimeOffset.UtcNow },
        };

        var request = new PagedRequest
        {
            Page = 0,
            PageSize = 10,
            SortBy = "Name",
            SortDirection = SortDirection.Asc,
        };

        var result = ingredients.AsQueryable().ApplySort(SortFields, request.SortBy, request.SortDirection).Select(i => i.Name).ToList();

        Assert.That(result, Is.EqualTo(new[] { "Apple", "Zebra" }));
    }

    [Test]
    public void ApplySort_Should_Sort_By_Computed_Nutrition_Value()
    {
        var ingredients = new List<Ingredient>
        {
            new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Low",
                Created = DateTimeOffset.UtcNow,
                Servings = { new IngredientServing { Id = Guid.NewGuid(), Calories = 100, Carbs = 0, Protein = 0, Fat = 0, Created = DateTimeOffset.UtcNow } },
            },
            new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "High",
                Created = DateTimeOffset.UtcNow,
                Servings =
                {
                    new IngredientServing { Id = Guid.NewGuid(), Calories = 300, Carbs = 0, Protein = 0, Fat = 0, Created = DateTimeOffset.UtcNow },
                    new IngredientServing { Id = Guid.NewGuid(), Calories = 200, Carbs = 0, Protein = 0, Fat = 0, Created = DateTimeOffset.UtcNow },
                },
            },
        };

        var request = new PagedRequest
        {
            Page = 0,
            PageSize = 10,
            SortBy = "TotalCalories",
            SortDirection = SortDirection.Asc,
        };

        var result = ingredients.AsQueryable().ApplySort(SortFields, request.SortBy, request.SortDirection).Select(i => i.Name).ToList();

        Assert.That(result, Is.EqualTo(new[] { "Low", "High" }));
    }

    [Test]
    public void ApplySort_Should_Fall_Back_To_Created_When_Requested_Field_Is_Unknown()
    {
        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), Name = "Old", Created = DateTimeOffset.UtcNow.AddDays(-2) },
            new Ingredient { Id = Guid.NewGuid(), Name = "New", Created = DateTimeOffset.UtcNow },
        };

        var request = new PagedRequest { Page = 0, PageSize = 10, SortBy = "Bogus" };

        var result = ingredients.AsQueryable().ApplySort(SortFields, request.SortBy, request.SortDirection).Select(i => i.Name).ToList();

        Assert.That(result, Is.EqualTo(new[] { "New", "Old" }));
    }

    [Test]
    public void ApplySort_Should_Throw_When_Requested_Field_Is_Unknown_And_No_Created_Field_Exists()
    {
        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), Name = "Eggs", Created = DateTimeOffset.UtcNow },
        };

        var sortFields = new Dictionary<string, Expression<Func<Ingredient, object>>>
        {
            ["Name"] = i => i.Name,
        };

        var request = new PagedRequest { Page = 0, PageSize = 10, SortBy = "Bogus" };

        Assert.That(() => ingredients.AsQueryable().ApplySort(sortFields, request.SortBy, request.SortDirection), Throws.ArgumentException);
    }
}
