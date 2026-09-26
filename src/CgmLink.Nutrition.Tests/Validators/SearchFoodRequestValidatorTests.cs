using CgmLink.Nutrition.Endpoints.SearchFood;
using FluentValidation.TestHelper;

namespace CgmLink.Nutrition.Tests.Validators;

[TestFixture]
public class SearchFoodRequestValidatorTests
{
    private SearchFoodRequest.Validator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new SearchFoodRequest.Validator();
    }

    [Test]
    public void Should_Have_Error_When_Query_Is_Empty()
    {
        var request = new SearchFoodRequest { Query = "", Page = 0, PageSize = 20 };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(value => value.Query);
    }

    [Test]
    public void Should_Have_Error_When_Page_Is_Negative()
    {
        var request = new SearchFoodRequest { Query = "apple", Page = -1, PageSize = 20 };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(value => value.Page);
    }

    [TestCase(0)]
    [TestCase(51)]
    public void Should_Have_Error_When_PageSize_Is_Out_Of_Range(int pageSize)
    {
        var request = new SearchFoodRequest { Query = "apple", Page = 0, PageSize = pageSize };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(value => value.PageSize);
    }

    [Test]
    public void Should_Not_Have_Error_When_Request_Is_Valid()
    {
        var request = new SearchFoodRequest { Query = "apple", Page = 0, PageSize = 20 };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
