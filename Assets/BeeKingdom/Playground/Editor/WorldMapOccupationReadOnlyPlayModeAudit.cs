using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BeeKingdom.EditorOccupationAudit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeeKingdom.Playground.Editor
{
    /// <summary>
    /// Read-only diagnosis on the actual loaded World Map. A passing audit means
    /// that evidence was collected, NOT that the map has zero collisions.
    /// Footprint estimates mirror the legacy reservation envelopes, not pixels.
    /// </summary>
    public sealed class WorldMapOccupationReadOnlyPlayModeAudit
    {
        private const BindingFlags PrivateInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private static Vector2 SizeFor(string kind)
        {
            switch(kind)
            {
                case "hive": return new Vector2(146f,132f);
                case "resource": return new Vector2(164f,105f);
                case "bestiary": return new Vector2(135f,120f);
                default: return new Vector2(106f,80f);
            }
        }
        private static Vector2 OffsetFor(string kind)
        {
            switch(kind)
            {
                case "hive": return new Vector2(0,31f);
                case "resource": return new Vector2(0,12f);
                case "bestiary": return new Vector2(0,28f);
                default: return new Vector2(0,10f);
            }
        }
        private static Rect Envelope(OccupationNode node)
        {
            var box=WorldMapLocalWorldOccupancy.Box(
                new WorldMapLocalWorldOccupancy.Claim {
                    Id=node.kind+":"+node.id,
                    WorldPoint=new Vector2(node.x,node.y),
                    Size=SizeFor(node.kind), Offset=OffsetFor(node.kind)
                });
            return box;
        }
        private static bool Conflict(Rect a,Rect b)
        {
            const float gap=14f;
            return new Rect(a.xMin-gap*.5f,a.yMin-gap*.5f,
                a.width+gap,a.height+gap).Overlaps(b);
        }
        private static object ReadField(object source,string name)
        {
            return source.GetType().GetField(name,PrivateInstance)?.GetValue(source);
        }

        [TearDown]
        public void RestoreLogs(){ LogAssert.ignoreFailingMessages=false; }

        [UnityTest]
        public IEnumerator AuditLoadedNodesWithoutMovingAnything()
        {
            LogAssert.ignoreFailingMessages=true;
            yield return new EnterPlayMode();
            var loading=SceneManager.LoadSceneAsync(
                "WorldMapMmoFullscreenFoundation",LoadSceneMode.Single);
            Assert.That(loading,Is.Not.Null);
            yield return loading;
            for(int i=0;i<6;i++)yield return null;
            var map=UnityEngine.Object.FindAnyObjectByType<
                WorldMapMmoFullscreenFoundationBootstrap>();
            Assert.That(map,Is.Not.Null);

            OccupationRead read=MapOccupationReadOnlySnapshot.Read(map,null);
            var visible=new List<OccupationNode>();
            foreach(var node in read.nodes)
                if(node.source.StartsWith("visible:")) visible.Add(node);
            Assert.That(visible.Count,Is.GreaterThan(0),
                "Audit requires already-loaded world nodes");

            int overlapCount=0,waterCount=0;
            var examples=new List<string>();
            for(int i=0;i<visible.Count;i++)
            {
                if(visible[i].kind=="resource" && visible[i].subtype=="Water")
                    waterCount++;
                for(int j=i+1;j<visible.Count;j++)
                {
                    if(visible[i].kind==visible[j].kind &&
                       visible[i].id==visible[j].id) continue;
                    if(!Conflict(Envelope(visible[i]),Envelope(visible[j])))
                        continue;
                    overlapCount++;
                    if(examples.Count<12)
                        examples.Add(visible[i].kind+":"+visible[i].id+" / "+
                                     visible[j].kind+":"+visible[j].id);
                }
            }
            bool maskEnabled=(bool)ReadField(map,
                "useWave5Method12288PreviewRuntimePackageForPlayMode");
            bool maskLoaded=(bool)ReadField(map,"runtimePlacementMaskLoaded");
            int claimedConflicts=map.LocalWorldPositionConflictsForProof;
            Debug.Log("BK_WORLD_OCCUPATION_READONLY_AUDIT"+
                " visibleNodes="+visible.Count+
                " approximateEnvelopeOverlaps="+overlapCount+
                " localReservationUnresolved="+claimedConflicts+
                " waterNodes="+waterCount+
                " auditedMaskEnabled="+maskEnabled+
                " auditedMaskLoaded="+maskLoaded+
                " unknownHydrology=true");
            foreach(var example in examples)
                Debug.Log("BK_WORLD_OCCUPATION_OVERLAP "+example);
            foreach(var node in visible)
                if(node.id=="hive_player_test" || node.id=="spider_next_to_hive" ||
                    node.id=="beast_t3_core_demo")
                    Debug.Log("BK_WORLD_OCCUPATION_CORE "+
                        node.kind+":"+node.id+" "+node.x+","+node.y);
            yield return new ExitPlayMode();
        }
    }
}