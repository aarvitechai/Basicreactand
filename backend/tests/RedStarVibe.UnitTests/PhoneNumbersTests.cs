using RedStarVibe.Application.Common;

namespace RedStarVibe.UnitTests;

public sealed class PhoneNumbersTests
{
    [Theory]
    [InlineData("+919876543210", "+919876543210")]
    [InlineData("+91 98765 43210", "+919876543210")]
    [InlineData("+1 (555) 123-4567", "+15551234567")]
    [InlineData("0044 20 7946 0958", "+442079460958")]
    public void Normalize_ValidInput_ReturnsE164(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumbers.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("9876543210")]
    [InlineData("+0123456789")]
    [InlineData("+12")]
    [InlineData("+1234567890123456")]
    [InlineData("+91abc5432100")]
    public void Normalize_InvalidInput_ReturnsNull(string? input)
    {
        Assert.Null(PhoneNumbers.Normalize(input));
    }
}
