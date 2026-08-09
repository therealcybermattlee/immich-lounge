using ImmichLoungeCompanion.Models;
using ImmichLoungeCompanion.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImmichLoungeCompanion.Tests.Services;

[TestClass]
public class ProfileValidatorTests
{
    private readonly ProfileValidator _validator = new();

    private static Profile ValidProfile() => new() { Name = "Test" };

    [TestMethod]
    public void Validate_NoDateFilter_IsValid()
    {
        Assert.IsNull(_validator.Validate(ValidProfile()));
    }

    [TestMethod]
    public void Validate_RangeDateFilter_IsValid()
    {
        var profile = ValidProfile();
        profile.DateFilter = new() { Type = "range", From = "2024-01-01", To = "2024-12-31" };
        Assert.IsNull(_validator.Validate(profile));
    }

    [TestMethod]
    public void Validate_OpenEndedRange_IsValid()
    {
        var profile = ValidProfile();
        profile.DateFilter = new() { Type = "range", From = "2024-01-01" };
        Assert.IsNull(_validator.Validate(profile));
    }

    [TestMethod]
    public void Validate_RangeWithFromAfterTo_IsRejected()
    {
        var profile = ValidProfile();
        profile.DateFilter = new() { Type = "range", From = "2025-01-01", To = "2024-01-01" };
        Assert.IsNotNull(_validator.Validate(profile));
    }

    [TestMethod]
    public void Validate_EmptyRange_IsRejected()
    {
        var profile = ValidProfile();
        profile.DateFilter = new() { Type = "range" };
        Assert.IsNotNull(_validator.Validate(profile));
    }

    [TestMethod]
    public void Validate_RollingDateFilter_IsValid()
    {
        var profile = ValidProfile();
        profile.DateFilter = new() { Type = "rolling", Amount = 12, Unit = "months" };
        Assert.IsNull(_validator.Validate(profile));
    }

    [TestMethod]
    public void Validate_RollingWithoutAmount_IsRejected()
    {
        var profile = ValidProfile();
        profile.DateFilter = new() { Type = "rolling", Unit = "months" };
        Assert.IsNotNull(_validator.Validate(profile));
    }

    [TestMethod]
    public void Validate_RollingWithBadUnit_IsRejected()
    {
        var profile = ValidProfile();
        profile.DateFilter = new() { Type = "rolling", Amount = 3, Unit = "decades" };
        Assert.IsNotNull(_validator.Validate(profile));
    }

    [TestMethod]
    public void Validate_SearchContentSource_NormalizesToValidFilter()
    {
        var profile = ValidProfile();
        profile.ContentSources = [new() { Type = "search", Id = "beach sunset", Label = "beach sunset" }];
        profile.NormalizeAssetFilter();
        Assert.IsNull(_validator.Validate(profile));
        Assert.AreEqual(AssetFilterConditionType.Search, profile.AssetFilter!.Children[0].Type);
    }
}
