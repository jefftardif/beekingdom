using BeeKingdom.Authentication.Security;

namespace BeeKingdom.Tests;

public sealed class BearerTokenSyntaxTests
{
    [TestCase("A")]
    [TestCase("abc-._~+/09")]
    [TestCase("YWJjZA==")]
    public void ValidB64TokenSyntaxIsAccepted(string value)
    {
        Assert.That(BearerTokenSyntax.IsValid(value), Is.True);
    }

    [TestCase("")]
    [TestCase(" abc")]
    [TestCase("abc ")]
    [TestCase("abc*")]
    [TestCase("ab=c")]
    [TestCase("=")]
    public void InvalidAlphabetPaddingOrWhitespaceIsRejected(string value)
    {
        Assert.That(BearerTokenSyntax.IsValid(value), Is.False);
    }

    [Test]
    public void ExactMaximumIsAcceptedAndMaximumPlusOneIsRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BearerTokenSyntax.IsValid(new string('A', BearerTokenSyntax.MaximumLength)), Is.True);
            Assert.That(BearerTokenSyntax.IsValid(new string('A', BearerTokenSyntax.MaximumLength + 1)), Is.False);
        });
    }
}
