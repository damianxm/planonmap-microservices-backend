using MapItems.Shared.Domain.Common;

namespace MapItems.UnitTests;

public class MarkerContentRulesTests
{
    // --- ValidateName ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateName_EmptyOrWhitespace_ReturnsError(string name)
    {
        var (ok, error) = MarkerContentRules.ValidateName(name);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateName_ExceedsMaxLength_ReturnsError()
    {
        var name = new string('a', MarkerContentRules.MaxNameLength + 1);

        var (ok, error) = MarkerContentRules.ValidateName(name);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    // --- ValidateDescription ---

    [Fact]
    public void ValidateDescription_NullDescription_ReturnsOk()
    {
        var (ok, error) = MarkerContentRules.ValidateDescription(null);

        Assert.True(ok);
        Assert.Null(error);
    }

    [Fact]
    public void ValidateDescription_ExceedsMaxLength_ReturnsError()
    {
        var description = new string('a', MarkerContentRules.MaxDescriptionLength + 1);

        var (ok, error) = MarkerContentRules.ValidateDescription(description);

        Assert.False(ok);
        Assert.NotNull(error);
    }
}
