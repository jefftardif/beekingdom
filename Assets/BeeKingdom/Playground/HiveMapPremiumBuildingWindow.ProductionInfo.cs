using System;
using System.Globalization;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// HD, server-read-only production facts shared by Honey Reserve,
    /// Warehouse and Transformation. The original building click may still
    /// trigger official/manual collection; this "i" view NEVER mutates stock.
    /// </summary>
    public sealed partial class HiveMapPremiumBuildingWindow
    {
        private bool productionInfoMode;
        private IHiveOfflineProductionPanelController productionInfoController;
        private float nextProductionLabelsAt;
        private float nextProductionRefreshAt;
        private TextMeshProUGUI productionInfoStatus;
        private TextMeshProUGUI productionInfoStock;
        private TextMeshProUGUI productionInfoRate;
        private TextMeshProUGUI productionInfoBalance;
        private TextMeshProUGUI productionInfoCapacity;
        private TextMeshProUGUI productionInfoResource;
        private TextMeshProUGUI productionInfoHint;
        private Button productionInfoRefreshButton;
        public bool ProductionInfoModeForProof => windowOpen && productionInfoMode;

        public void ShowProductionInfo(BuildingDefinition building,
            string frenchTitle, string englishTitle,
            IHiveOfflineProductionPanelController controller,
            Action onClose, Action onUpgrade)
        {
            if (building == null) return;
            bool allowed = building.BuildingType == BuildingTypes.HoneyReserve
                || building.BuildingType == BuildingTypes.Warehouse
                || building.BuildingType == BuildingTypes.Transformation;
            if (!allowed) throw new ArgumentException(
                "Only actual producer buildings are supported.", nameof(building));
            productionInfoMode = true;
            productionInfoController = controller;
            nextProductionLabelsAt = 0;
            nextProductionRefreshAt = Time.unscaledTime + 15f;
            Show(building,frenchTitle,englishTitle,
                string.Empty,string.Empty,onClose,onUpgrade);
            RefreshProductionInfoLabels();
        }

        private static string ProductionText(string key,string fallback) =>
            BeeLocalization.Text("production.info.premium." + key,fallback);

        private static void SetProductionText(TextMeshProUGUI label,string value)
        {
            if (label != null && !string.Equals(label.text,value,StringComparison.Ordinal))
                label.text=value;
        }

        private void TickProductionInfo()
        {
            float now=Time.unscaledTime;
            if (now>=nextProductionLabelsAt)
            {
                nextProductionLabelsAt=now+.5f;
                RefreshProductionInfoLabels();
            }
            if(now>=nextProductionRefreshAt)
            {
                nextProductionRefreshAt=now+15f;
                if(productionInfoController!=null &&
                   productionInfoController.IsConfigured &&
                   !productionInfoController.IsBusy)
                    productionInfoController.Refresh();
            }
        }

        private void RenderProductionInfoCards(Transform parent,float width,float left,
            out float cursor,float viewportHeight)
        {
            cursor=14f;
            RectTransform overview=CardAt(parent,
                new Rect(left,cursor,width,216f),
                ProductionText("card.status","ETAT DE PRODUCTION"),
                selected != null ? selected.IconId : "honey", true);
            productionInfoStatus=Label(overview,
                new Rect(18f,100f,width-36f,46f),"--",20f,Gold,true);
            productionInfoResource=Label(overview,
                new Rect(18f,152f,width-36f,48f),"--",15f,Paper);
            cursor+=236f;

            RectTransform stock=CardAt(parent,
                new Rect(left,cursor,width,270f),
                ProductionText("card.resources","RESSOURCES ET CAPACITE"),"resources",true);
            productionInfoStock=Label(stock,
                new Rect(18f,97f,width-36f,32f),"--",19f,Gold,true);
            productionInfoRate=Label(stock,
                new Rect(18f,133f,width-36f,31f),"--",16f,Paper);
            productionInfoBalance=Label(stock,
                new Rect(18f,172f,width-36f,31f),"--",16f,Paper);
            productionInfoCapacity=Label(stock,
                new Rect(18f,213f,width-36f,41f),"--",15f,Muted);
            cursor+=290f;

            RectTransform action=CardAt(parent,
                new Rect(left,cursor,width,232f),
                ProductionText("card.management","SUIVI DE PRODUCTION"),"book",true);
            productionInfoHint=Label(action,
                new Rect(18f,94f,width-36f,61f),
                ProductionText("note.no_server",
                    "Les valeurs officielles proviennent du serveur."),
                14f,Paper);
            productionInfoRefreshButton=ButtonAt(action,
                new Rect(18f,173f,Mathf.Max(116f,width-36f),44f),
                ProductionText("action.refresh","Actualiser"),false,()=>
                {
                    if(productionInfoController!=null &&
                       productionInfoController.IsConfigured &&
                       !productionInfoController.IsBusy)
                        productionInfoController.Refresh();
                });
            cursor+=252f;
            if(parent is RectTransform content)
                content.sizeDelta=new Vector2(0f,
                    Mathf.Max(cursor+24f,viewportHeight+1f));
            RefreshProductionInfoLabels();
        }

        private void RefreshProductionInfoLabels()
        {
            if(!windowOpen || !productionInfoMode || suppressed ||
               productionInfoStatus==null || selected==null) return;
            var controller=productionInfoController;
            HiveOfflineProductionScreenModel model=controller?.Model;
            bool configured=controller!=null && controller.IsConfigured &&
                model!=null && model.State!=HiveOfflineProductionScreenState.NotConfigured;
            bool busy=controller?.IsBusy ?? false;
            HiveOfflineProductionLineModel line=configured
                ? model.FindLine(selected.LegacyKey):null;
            string status;
            if(!configured)
                status=ProductionText("status.no_server","Session officielle requise");
            else if(model.State==HiveOfflineProductionScreenState.OfflineReadOnly)
                status=ProductionText("status.offline","Consultation hors ligne");
            else if(model.State==HiveOfflineProductionScreenState.Error)
                status=ProductionText("status.error","Données non confirmées");
            else if(line==null)
                status=ProductionText("status.loading","Synchronisation en cours");
            else if(model.State==HiveOfflineProductionScreenState.Collecting &&
                    model.CollectingBuildingKey==selected.LegacyKey)
                status=ProductionText("status.collecting","Collecte en cours");
            else
                status=ProductionText("status.ready","Production synchronisée");
            SetProductionText(productionInfoStatus,status);
            SetProductionText(productionInfoResource,
                ProductionText("resource","Ressource") + " : " +
                (line!=null
                    ? BeeLocalization.Text("resource."+line.ResourceKey,line.ResourceKey)
                    : "--"));
            SetProductionText(productionInfoStock,
                ProductionText("pending","En attente") + " : " +
                (line!=null ? line.PendingAmount.ToString("N0",
                    CultureInfo.CurrentCulture):"--"));
            SetProductionText(productionInfoRate,
                ProductionText("hourly","Taux horaire") + " : " +
                (line!=null ? line.HourlyRate.ToString("N1",
                    CultureInfo.CurrentCulture):"--") + " / h");
            SetProductionText(productionInfoBalance,
                ProductionText("reserve","Réserve") + " : " +
                (line!=null
                    ? line.BalanceAmount.ToString("N0",CultureInfo.CurrentCulture)
                      + " / " + line.BalanceCapacity.ToString("N0",CultureInfo.CurrentCulture)
                    : "-- / --"));
            SetProductionText(productionInfoCapacity,
                ProductionText("capacity","Capacité de production") + " : " +
                (line!=null
                    ? line.ProductionCapacity.ToString("N0",CultureInfo.CurrentCulture)
                    : "--"));
            string hint=!configured
                ? ProductionText("note.no_server","Connectez-vous pour lire les valeurs officielles.")
                : model.IsReadOnly
                    ? ProductionText("note.offline","Mode consultation : aucune opération possible.")
                    : line!=null && line.IsResourceCapacityFull
                        ? ProductionText("note.full","Réserve pleine : récolte indisponible.")
                        : ProductionText("note.collect_on_building",
                            "Touchez le bâtiment pour récolter. Ce panneau est en lecture seule.");
            SetProductionText(productionInfoHint,hint);
            if(productionInfoRefreshButton!=null)
                productionInfoRefreshButton.interactable=configured && !busy &&
                    model.State!=HiveOfflineProductionScreenState.Collecting &&
                    model.State!=HiveOfflineProductionScreenState.OfflineReadOnly;
        }
    }
}
