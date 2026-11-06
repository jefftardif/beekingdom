using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeeKingdom.Playground.Editor
{
    /// <summary>
    /// Test the official Unity scene and the actual camera Update/ZoomAround
    /// path. No production account, purchases or server resource mutations.
    /// </summary>
    public sealed class WorldMapZoomAnchorPlayModeTests
    {
        private const BindingFlags PrivateInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private static FieldInfo Field(string name)
        {
            var result=typeof(WorldMapMmoFullscreenFoundationBootstrap)
                .GetField(name,PrivateInstance);
            Assert.That(result,Is.Not.Null,"Missing WorldMap camera field: "+name);
            return result;
        }

        [TearDown]
        public void ResetUnityErrorMode()
        {
            LogAssert.ignoreFailingMessages=false;
        }

        [UnityTest]
        public IEnumerator ActualWorldMapZoomAroundKeepsWorldPointUnderPointer()
        {
            LogAssert.ignoreFailingMessages=true;
            yield return new EnterPlayMode();
            var loading=SceneManager.LoadSceneAsync(
                "WorldMapMmoFullscreenFoundation",LoadSceneMode.Single);
            Assert.That(loading,Is.Not.Null);
            yield return loading;
            for(int i=0;i<4;i++)yield return null;

            var map=UnityEngine.Object.FindAnyObjectByType<
                WorldMapMmoFullscreenFoundationBootstrap>();
            Assert.That(map,Is.Not.Null);

            FieldInfo currentCenter=Field("currentWorldCenter");
            FieldInfo targetCenter=Field("targetWorldCenter");
            FieldInfo currentZoom=Field("currentZoom");
            FieldInfo targetZoom=Field("targetZoom");
            var zoomAround=typeof(WorldMapMmoFullscreenFoundationBootstrap)
                .GetMethod("ZoomAround",PrivateInstance);
            Assert.That(zoomAround,Is.Not.Null);

            var viewport=new Vector2(Screen.width,Screen.height);
            var center=new Vector2(16000f,16000f);
            var cursor=new Vector2(Screen.width*.62f,Screen.height*.38f);
            const float zoom=.7f;
            currentCenter.SetValue(map,center);
            targetCenter.SetValue(map,center);
            currentZoom.SetValue(map,zoom);
            targetZoom.SetValue(map,zoom);
            var chosenWorld=WorldMapZoomAnchor.WorldPoint(
                cursor,center,zoom,viewport);

            zoomAround.Invoke(map,new object[] { cursor,1.9f });
            float requestedZoom=(float)targetZoom.GetValue(map);
            Assert.That(requestedZoom,Is.EqualTo(1.9f).Within(.001f));
            Vector2 target=(Vector2)targetCenter.GetValue(map);
            Assert.That(Vector2.Distance(WorldMapZoomAnchor.ScreenPoint(
                chosenWorld,target,requestedZoom,viewport),cursor),
                Is.LessThan(.15f),"Target should preserve the cursor world point");

            for(int frame=0;frame<28;frame++)
            {
                yield return null;
                var actualCenter=(Vector2)currentCenter.GetValue(map);
                float actualZoom=(float)currentZoom.GetValue(map);
                Vector2 projected=WorldMapZoomAnchor.ScreenPoint(
                    chosenWorld,actualCenter,actualZoom,viewport);
                Assert.That(Vector2.Distance(projected,cursor),Is.LessThan(.5f),
                    "Actual WorldMap Update drift at frame "+frame);
            }

            Assert.That((float)currentZoom.GetValue(map),
                Is.GreaterThan(zoom+.2f),"Actual Unity Update must animate zoom");
            yield return new ExitPlayMode();
        }
    }
}