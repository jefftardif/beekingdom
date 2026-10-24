#if UNITY_ANDROID
using System;
using System.IO;
using System.Xml;
using BeeKingdom.Monetization;
using UnityEditor.Android;

namespace BeeKingdom.Editor.Monetization
{
    /// <summary>
    /// Adds the Google Mobile Ads application identifier to the generated
    /// Android manifest, without replacing Unity's own main activity.
    /// </summary>
    public sealed class BeeKingdomAdMobAndroidManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        private const string GoogleAppIdMetadataName = "com.google.android.gms.ads.APPLICATION_ID";

        public int callbackOrder => 999;

        public void OnPostGenerateGradleAndroidProject(string gradleProjectPath)
        {
            string manifest = Path.Combine(gradleProjectPath, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest))
                throw new FileNotFoundException("Unity AndroidManifest.xml was not generated.", manifest);

            var document = new XmlDocument { PreserveWhitespace = true };
            document.Load(manifest);
            XmlElement application = document.DocumentElement?.SelectSingleNode("application") as XmlElement;
            if (application == null)
                throw new InvalidOperationException("AndroidManifest.xml is missing the application element.");

            foreach (XmlNode node in application.ChildNodes)
            {
                if (node is XmlElement element &&
                    string.Equals(element.LocalName, "meta-data", StringComparison.Ordinal) &&
                    string.Equals(element.GetAttribute("name", AndroidNamespace), GoogleAppIdMetadataName,
                        StringComparison.Ordinal))
                {
                    if (!string.Equals(element.GetAttribute("value", AndroidNamespace),
                            BeeKingdomLevelPlayRewardedBridge.AdMobAndroidAppId, StringComparison.Ordinal))
                    {
                        element.SetAttribute("value", AndroidNamespace,
                            BeeKingdomLevelPlayRewardedBridge.AdMobAndroidAppId);
                        document.Save(manifest);
                    }
                    return;
                }
            }

            XmlElement metadata = document.CreateElement("meta-data");
            metadata.SetAttribute("name", AndroidNamespace, GoogleAppIdMetadataName);
            metadata.SetAttribute("value", AndroidNamespace,
                BeeKingdomLevelPlayRewardedBridge.AdMobAndroidAppId);
            application.AppendChild(metadata);
            document.Save(manifest);
        }
    }
}
#endif
