using System.Text;
using BeeKingdom.Chat.Diagnostics;

namespace BeeKingdom.Tests;

public sealed class ChatResponseBudgetTests
{
    [Test]
    public void ExactBudgetIsAcceptedAndNextByteIsRejected()
    {
        byte[] exact = new byte[ChatResponseBudget.DefaultBytes];
        byte[] over = new byte[ChatResponseBudget.DefaultBytes + 1];
        Assert.Multiple(() =>
        {
            Assert.That(ChatResponseBudget.IsWithinLimit(exact), Is.True);
            Assert.That(ChatResponseBudget.IsWithinLimit(over), Is.False);
        });
    }

    [Test]
    public void BoundsAndContentTypeAreExplicit()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ChatResponseBudget.IsValidConfiguration(ChatResponseBudget.MinimumBytes), Is.True);
            Assert.That(ChatResponseBudget.IsValidConfiguration(ChatResponseBudget.MaximumBytes), Is.True);
            Assert.That(ChatResponseBudget.IsValidConfiguration(ChatResponseBudget.MinimumBytes - 1), Is.False);
            Assert.That(ChatResponseBudget.IsValidConfiguration(ChatResponseBudget.MaximumBytes + 1), Is.False);
            Assert.That(ChatResponseBudget.IsJsonContentType("application/json; charset=utf-8"), Is.True);
            Assert.That(ChatResponseBudget.IsJsonContentType("text/html"), Is.False);
        });
    }
}
