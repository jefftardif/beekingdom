using System;
using System.Threading;
using System.Collections.Generic;
using BeeKingdom.Networking;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public static partial class MobileAccountSessionRuntimeBootstrap
    {
        private static AllianceTerritoryClient territoryClient;
        private static AllianceHeadquartersClient headquartersClient;
        private static AllianceHeadquartersResponseGate headquartersGate;
        private static long headquartersGeneration;
        private static Guid headquartersSessionPlayer;
        private static bool headquartersEnabled;
        private static RemoteHeadquartersResponse headquartersResponse;
        private static float headquartersExpiresAt;
        private static AllianceTerritoryRuntimeSettings territorySettings;
        private static CancellationTokenSource territoryCancellation;
        private static RemoteTerritoryResponse territoryResponse;
        private static string territoryWorld, territorySurface, territoryUnit;
        private static bool TerritoryConfigurationIsCurrent => territorySettings!=null && territorySettings.Enabled
            && territorySettings.WorldId==territoryWorld && territorySettings.SurfaceId==territorySurface
            && territorySettings.CoordinateUnit==territoryUnit && territorySettings.HeadquartersDetailsEnabled==headquartersEnabled;
        private static int territoryGeneration;
        private static bool territoryBusy;
        private static float territoryNextRead;
        private static float territoryExpiresAt;
        private static readonly Dictionary<Guid,long> territoryRevisionFloors=new Dictionary<Guid,long>();
        private static readonly Dictionary<Guid,long> territoryMembershipFloors=new Dictionary<Guid,long>();
        private static Guid lastTerritoryAlliance;
        private static Guid? territoryObservedAlliance;

        private static void ConfigureTerritoryReadForSession(MobileAccountSessionGate gate,
            IGameAccountSessionSource sessions,IAuthenticatedGameRestTransport transport,bool authenticatedAuthority)
        {
            CloseTerritoryReadForSession();
            var settings=Resources.Load<AllianceTerritoryRuntimeSettings>("AllianceTerritoryRuntime");
            if(!authenticatedAuthority || settings==null || !settings.Enabled
                || string.IsNullOrWhiteSpace(settings.WorldId) || string.IsNullOrWhiteSpace(settings.SurfaceId)
                || string.IsNullOrWhiteSpace(settings.CoordinateUnit)) return;
            territorySettings=settings;
            territoryWorld=settings.WorldId;territorySurface=settings.SurfaceId;territoryUnit=settings.CoordinateUnit;
            territoryCancellation=new CancellationTokenSource();
            territoryClient=new AllianceTerritoryClient(gate,sessions,transport);
            headquartersEnabled=settings.HeadquartersDetailsEnabled;
            if(headquartersEnabled)
            {
                headquartersClient=new AllianceHeadquartersClient(gate,sessions,transport);
                headquartersSessionPlayer=headquartersClient.CurrentPlayerId;
                headquartersGate=new AllianceHeadquartersResponseGate();
            }
        }
        private static void CloseTerritoryReadForSession()
        {
            territoryGeneration++;
            territoryCancellation?.Cancel();territoryCancellation?.Dispose();territoryCancellation=null;
            territoryClient=null;territorySettings=null;territoryResponse=null;territoryBusy=false;
            headquartersSessionPlayer=Guid.Empty;headquartersClient=null;headquartersGate=null;headquartersResponse=null;headquartersExpiresAt=0;headquartersEnabled=false;
            territoryNextRead=0;territoryExpiresAt=0;
            territoryWorld=null;territorySurface=null;territoryUnit=null;
            territoryObservedAlliance=null;
            territoryRevisionFloors.Clear();territoryMembershipFloors.Clear();lastTerritoryAlliance=Guid.Empty;
        }
        // The opt-in settings explicitly select the realm represented by this map
        // session. They never grant access: the server checks its real world membership.
        public static string TerritoryConfiguredMapWorld => TerritoryConfigurationIsCurrent ? territoryWorld : null;
        public static void ObserveTerritoryAlliance(Guid allianceId)
        {
            if(territoryObservedAlliance==allianceId)return;
            territoryObservedAlliance=allianceId;territoryGeneration++;
            territoryCancellation?.Cancel();territoryCancellation?.Dispose();
            territoryCancellation=territoryClient!=null ? new CancellationTokenSource() : null;
            territoryResponse=null;territoryBusy=false;territoryExpiresAt=0;territoryNextRead=0;
            lastTerritoryAlliance=Guid.Empty;
            headquartersResponse=null;headquartersExpiresAt=0;
            if(headquartersGate!=null) headquartersGeneration=headquartersGate.Reset(headquartersClient.CurrentPlayerId,allianceId,territoryWorld,territorySurface,territoryUnit);
        }
        public static RemoteHeadquartersResponse HeadquartersReadForAlliance(Guid allianceId)
        {
            if(headquartersClient!=null && (headquartersSessionPlayer==Guid.Empty || headquartersClient.CurrentPlayerId!=headquartersSessionPlayer))
            {CloseTerritoryReadForSession();return null;}
            ObserveTerritoryAlliance(allianceId);
            if(allianceId==Guid.Empty)return null;
            TerritoryOutlineForMap(territoryWorld,territorySurface,territoryUnit);
            if(Time.unscaledTime>headquartersExpiresAt || (headquartersResponse!=null
                && (headquartersResponse.ServerTimeUtc<DateTimeOffset.UtcNow.AddSeconds(-30)
                    || headquartersResponse.ServerTimeUtc>DateTimeOffset.UtcNow.AddSeconds(5)))) headquartersResponse=null;
            return headquartersResponse?.Details?.Revision?.AllianceId==allianceId ? headquartersResponse : null;
        }
        public static RemoteTerritoryResponse TerritoryReadForAlliance(Guid allianceId)
        {
            ObserveTerritoryAlliance(allianceId);
            if(allianceId==Guid.Empty)return null;
            TerritoryOutlineForMap(territoryWorld,territorySurface,territoryUnit);
            return Time.unscaledTime<=territoryExpiresAt && territoryResponse?.AllianceId==allianceId ? territoryResponse : null;
        }
        public static RemoteTerritoryOutline TerritoryOutlineForMap(string world,string surface,string coordinateUnit)
        {
            if(!TerritoryConfigurationIsCurrent)
            {
                if(territoryClient!=null)CloseTerritoryReadForSession();
                return null;
            }
            if(territoryObservedAlliance==Guid.Empty)return null;
            if(territoryClient==null || territoryWorld!=world || territorySurface!=surface || territoryUnit!=coordinateUnit) return null;
            if(!territoryBusy && Time.unscaledTime>=territoryNextRead) RefreshTerritoryRead();
            if(Time.unscaledTime>territoryExpiresAt) return null;
            return territoryResponse?.View?.Outline;
        }
        private static async void RefreshTerritoryRead()
        {
            territoryBusy=true;territoryNextRead=Time.unscaledTime+15f;
            int generation=territoryGeneration;
            var client=territoryClient;var world=territoryWorld;var surface=territorySurface;var unit=territoryUnit;var ct=territoryCancellation.Token;
            try
            {
                long floor;territoryRevisionFloors.TryGetValue(lastTerritoryAlliance,out floor);
                RemoteTerritoryResponse result;
                using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(10));
                    result=await client.ReadAsync(world,surface,unit,lastTerritoryAlliance,floor,timeout.Token);
                }
                if(generation!=territoryGeneration || ct.IsCancellationRequested) return;
                if(!TerritoryConfigurationIsCurrent){CloseTerritoryReadForSession();return;}
                if(territoryObservedAlliance.HasValue && result.AllianceId!=territoryObservedAlliance.Value)
                {territoryResponse=null;territoryExpiresAt=0;return;}
                long knownRevision,knownMembership;
                territoryRevisionFloors.TryGetValue(result.AllianceId,out knownRevision);
                territoryMembershipFloors.TryGetValue(result.AllianceId,out knownMembership);
                if((result.View.Revision.HasValue && result.View.Revision.Value<knownRevision)
                    || result.MembershipRevision<knownMembership)
                {territoryResponse=null;territoryExpiresAt=0;return;}
                if(result.View.Revision.HasValue) territoryRevisionFloors[result.AllianceId]=result.View.Revision.Value;
                territoryMembershipFloors[result.AllianceId]=result.MembershipRevision;
                lastTerritoryAlliance=result.AllianceId;
                territoryResponse=result;territoryExpiresAt=Time.unscaledTime+20f;
            }
            catch(Exception)
            {
                // Read failure clears the overlay. Never persist or synthesize an outline.
                if(generation==territoryGeneration){territoryResponse=null;territoryExpiresAt=0;}
            }
            finally
            {
                if(generation==territoryGeneration && TerritoryConfigurationIsCurrent && !ct.IsCancellationRequested
                    && headquartersClient!=null && territoryObservedAlliance.HasValue && territoryObservedAlliance.Value!=Guid.Empty)
                {
                    var hqClient=headquartersClient;var hqGate=headquartersGate;long hqGeneration=headquartersGeneration;
                    long request=hqGate.BeginRequest();
                    try
                    {
                        RemoteHeadquartersResponse details;
                        using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct))
                        {
                            timeout.CancelAfter(TimeSpan.FromSeconds(10));
                            details=await hqClient.ReadAsync(timeout.Token);
                        }
                        if(generation==territoryGeneration && TerritoryConfigurationIsCurrent && !ct.IsCancellationRequested)
                        {
                            if(hqGate.Accept(hqGeneration,request,details,DateTimeOffset.UtcNow))
                            { headquartersResponse=details;headquartersExpiresAt=Time.unscaledTime+20f; }
                            else {headquartersResponse=null;headquartersExpiresAt=0;}
                        }
                        else if(generation==territoryGeneration)CloseTerritoryReadForSession();
                    }
                    catch(Exception)
                    {
                        if(generation==territoryGeneration){headquartersResponse=null;headquartersExpiresAt=0;}
                    }
                }
                if(generation==territoryGeneration)territoryBusy=false;
            }
        }
    }
}
