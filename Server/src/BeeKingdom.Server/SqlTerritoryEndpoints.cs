using BeeKingdom.Authentication;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Server;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.TerritorySqlCandidate;

// Explicit candidate registration only; no production Program/DI/config changed.
public static class SqlTerritoryEndpoints
{
    public static void Map(WebApplication app,SqlTerritoryStore store,string world,string surface,string unit,
        Func<HttpContext,AuthenticationManager,TokenValidationResult> authenticate)
    {
        IResult Run(HttpContext ctx,AuthenticationManager manager,Func<Guid,IResult> action)
        {
            var auth=authenticate(ctx,manager);
            if(!auth.IsValid||auth.PlayerId.Value==Guid.Empty)return Results.Unauthorized();
            try{return action(auth.PlayerId.Value);}
            catch(InvalidOperationException e) when(e.Message is "membership_required" or "world_assignment_required") {return Results.Json(new{code=e.Message},statusCode:403);}
            catch(InvalidOperationException) {return Results.Json(new{code="territory.authority_unavailable"},statusCode:503);}
            catch(InvalidDataException) {return Results.Json(new{code="territory.storage_invalid"},statusCode:503);}
            catch(SqlException) {return Results.Json(new{code="territory.storage_unavailable"},statusCode:503);}
        }
        app.MapPost("/alliance/v1/territory/foundation",(HttpContext ctx,AuthenticationManager manager,TerritoryFoundationRequest request)=>Run(ctx,manager,p=>{
            var result=store.Found(p,request.RequestId,new(request.X,request.Y));
            return Results.Json(result,statusCode:result.Created?201:result.Replayed?200:409);
        }));
        app.MapPost("/alliance/v1/territory/construction/work",(HttpContext ctx,AuthenticationManager manager,TerritoryConstructionWorkRequest request)=>Run(ctx,manager,p=>{
            var result=store.Credit(p,request.BuildingId,request.Source,request.ReceiptId);
            return Results.Json(result,statusCode:result.Applied||result.Replayed?200:409);
        }));
        app.MapGet("/alliance/v1/territory/mine",(HttpContext ctx,AuthenticationManager manager,string? worldId,string? surfaceId,long? minimumRevision,Guid? minimumAllianceId)=>{
            ctx.Response.Headers.CacheControl="no-store";
            return Run(ctx,manager,p=>{
                if(worldId!=world||surfaceId!=surface)return Results.Json(new{code="territory.world_scope_denied"},statusCode:403);
                if(minimumRevision<0)return Results.BadRequest();
                var read=store.Read(p,unit);
                var view=read.View;
                if(minimumAllianceId==read.AllianceId&&minimumRevision>0&&(!view.Revision.HasValue||view.Revision<minimumRevision))
                    view=new(false,"snapshot_older_than_requested",view.Revision,null);
                return Results.Ok(new TerritoryReadResponse(TerritoryEndpoints.Version,p,read.AllianceId,world,surface,read.MembershipRevision,DateTimeOffset.UtcNow,view));
            });
        });
    }
}
