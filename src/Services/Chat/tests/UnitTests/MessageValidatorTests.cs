using Chat.Features.Messages.Send;

namespace Chat.UnitTests;

public class MessageValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespace_ReturnsError(string content)
    {
        var (ok, error) = MessageValidator.Validate(content);

        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_MessageExceedsMaxLength_ReturnsError()
    {
        var content = new string('a', MessageValidator.MaxMessageLength + 1);

        var (ok, error) = MessageValidator.Validate(content);

        Assert.False(ok);
        Assert.NotNull(error);
    }

}
