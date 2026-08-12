using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class EmergencyResponseFrameworkTests
    {
        [Test]
        public void DetectActivateEscalateAndResolveEmergency()
        {
            EmergencyResponseManager manager = CreateManager();
            EmergencyIncident incident = manager.DetectEmergency("fire", 0.6d);
            Assert.That(incident, Is.Not.Null);
            Assert.That(manager.ActivateEmergency(incident.IncidentId), Is.True);
            Assert.That(manager.EscalateEmergency(incident.IncidentId, 0.9d), Is.True);
            Assert.That(incident.Severity, Is.EqualTo(EmergencySeverity.Critical));
            Assert.That(manager.ResolveEmergency(incident.IncidentId), Is.True);
        }

        private static EmergencyResponseManager CreateManager()
        {
            EmergencyResponseManager manager = new EmergencyResponseManager();
            manager.RegisterEmergencyType(new EmergencyPlan("fire", EmergencyType.Fire, 0.5d));
            return manager;
        }
    }
}
