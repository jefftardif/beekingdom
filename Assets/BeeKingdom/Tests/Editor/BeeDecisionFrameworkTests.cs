using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BeeDecisionFrameworkTests
    {
        [Test]
        public void EvaluateDecisionSelectsHighestScore()
        {
            BeeDecisionManager manager = new BeeDecisionManager();

            DecisionScore score = manager.EvaluateDecision(new DecisionContext("bee-1", BeeCaste.Builder), new[]
            {
                new DecisionCandidate(BeeIntent.Idle, 0.1d),
                new DecisionCandidate(BeeIntent.Build, 0.9d)
            });

            Assert.That(score.Intent, Is.EqualTo(BeeIntent.Build));
        }

        [Test]
        public void EmergencyDecisionOverridesNormalScore()
        {
            BeeDecisionManager manager = new BeeDecisionManager();

            DecisionScore score = manager.EvaluateDecision(new DecisionContext("bee-1", BeeCaste.Worker), new[]
            {
                new DecisionCandidate(BeeIntent.Gather, 1d),
                new DecisionCandidate(BeeIntent.Escape, 0.1d, true)
            });

            Assert.That(score.Intent, Is.EqualTo(BeeIntent.Escape));
        }

        [Test]
        public void InvalidIntentionsAreFiltered()
        {
            BeeDecisionManager manager = new BeeDecisionManager();

            Assert.That(manager.GenerateIntentions(new[] { new DecisionCandidate(BeeIntent.Build, 1d, false, false) }).Count, Is.EqualTo(0));
        }

        [Test]
        public void InterruptRequiresCurrentDecision()
        {
            BeeDecisionManager manager = new BeeDecisionManager();
            manager.EvaluateDecision(new DecisionContext("bee-1", BeeCaste.Worker), new[] { new DecisionCandidate(BeeIntent.Rest, 1d) });

            Assert.That(manager.InterruptDecision("bee-1", "danger"), Is.True);
        }
    }
}
