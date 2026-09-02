using System.Reflection;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    // M043K-CL: regression coverage for two client-side bugs found live against production tonight.
    // UnityAuthenticatedGameRestTransport was built exclusively for the Hive "/game/v1/" domain and
    // hardcoded that assumption in two separate places - both silently discarded every real Alliance
    // signal (a valid request, a valid server rejection) and replaced it with a generic
    // "invalid_response"/"game.rejected" the UI could never distinguish from an actual bug. Neither
    // ValidateRequest's path prefix nor ParseSafeErrorCode's code prefix are public, so these tests
    // reach them via reflection - the same technique used to prove the bug live in Play Mode.
    public sealed class UnityAuthenticatedGameRestTransportTests
    {
        [Test]
        public void AllowedPathPrefixes_IncludesGameAndAllianceRoutes()
        {
            string[] prefixes = GetPrivateStaticField<string[]>("AllowedPathPrefixes");
            Assert.That(prefixes, Does.Contain("/game/v1/"));
            Assert.That(prefixes, Does.Contain("/alliance/v1/"));
        }

        [TestCase("/game/v1/hives/00000000-0000-0000-0000-000000000001/research", true)]
        [TestCase("/alliance/v1/membership/mine", true)]
        [TestCase("/alliance/v1/alliances", true)]
        [TestCase("/chat/v1/conversations", false)]
        [TestCase("https://evil.example.com/alliance/v1/x", false)]
        public void StartsWithAllowedPrefix_MatchesOnlyRegisteredDomains(string path, bool expected)
        {
            bool result = (bool)InvokePrivateStatic("StartsWithAllowedPrefix", path);
            Assert.That(result, Is.EqualTo(expected), path);
        }

        [TestCase("game.session_required", true)]
        [TestCase("alliance.not_found", true)]
        [TestCase("alliance.invalid_request", true)]
        [TestCase("alliance.already_in_alliance", true)]
        [TestCase("chat.disabled", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsSafeGameCode_AcceptsGameAndAllianceCodes(string code, bool expected)
        {
            bool result = (bool)InvokePrivateStatic("IsSafeGameCode", code);
            Assert.That(result, Is.EqualTo(expected), code ?? "<null>");
        }

        private static object InvokePrivateStatic(string methodName, params object[] args)
        {
            MethodInfo method = typeof(UnityAuthenticatedGameRestTransport).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Method '{methodName}' not found - has it been renamed?");
            return method.Invoke(null, args);
        }

        private static T GetPrivateStaticField<T>(string fieldName)
        {
            FieldInfo field = typeof(UnityAuthenticatedGameRestTransport).GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' not found - has it been renamed?");
            return (T)field.GetValue(null);
        }
    }
}
