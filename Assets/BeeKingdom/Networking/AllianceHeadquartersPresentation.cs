using System;

namespace BeeKingdom.Networking
{
    // All seven rows derive from one accepted, unexpired transactional details response.
    public static class AllianceHeadquartersPresentation
    {
        public static string[] Rows(RemoteHeadquartersResponse response, Func<string,string,string,string> text)
        {
            string no=text("unavailable","Information indisponible","Information unavailable");
            string[] rows={no,no,no,no,no,no,no};
            var d=response?.Details;
            if(d==null || !d.Available)return rows;
            switch(d.Code)
            {
                case "headquarters_absent": rows[0]=text("hq_absent","Aucun QG confirmé","No headquarters confirmed");break;
                case "construction_cancelled":rows[0]=text("cancelled","Construction annulée","Construction cancelled");break;
                case "construction_in_progress":rows[0]=text("in_progress","Construction en cours","Construction in progress");break;
                case "territory_released":rows[0]=text("released","Territoire libéré","Territory released");break;
                case "headquarters_operational":rows[0]=text("operational","QG opérationnel","Headquarters operational");break;
                case "capacity_exceeded":rows[0]=text("capacity_exceeded","Capacité dépassée","Capacity exceeded");break;
                default:return rows;
            }
            if(d.HeadquartersLevel.HasValue)rows[1]=text("hq_level","Niveau du QG : ","Headquarters level: ")+d.HeadquartersLevel.Value;
            if(d.HeadquartersOperational.HasValue)rows[2]=d.HeadquartersOperational.Value
                ?text("operational","QG opérationnel","Headquarters operational")
                :text("not_operational","QG non opérationnel","Headquarters not operational");
            if(d.MaximumArea.HasValue && d.OccupiedArea.HasValue)rows[3]=text("area","Surface occupée / capacité : ","Occupied area / capacity: ")+d.OccupiedArea.Value+" / "+d.MaximumArea.Value+" "+d.AreaUnit;
            if(d.RemainingArea.HasValue)rows[4]=text("remaining","Surface disponible : ","Remaining area: ")+d.RemainingArea.Value+" "+d.AreaUnit;
            if(d.MaximumFloralInstallations.HasValue)rows[5]=text("floral_capacity","Capacité florale : ","Floral capacity: ")+d.MaximumFloralInstallations.Value;
            if(d.Construction!=null)rows[6]=text("work","Travail confirmé : ","Confirmed work: ")+d.Construction.CompletedWork+" / "+d.Construction.RequiredWork;
            return rows;
        }
    }
}
