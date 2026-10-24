using System;
using System.Linq;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// The three Activities-v4 cards are explicit views of official DTOs.
    /// No timers, costs, scores, ecology, member counts or harvest rewards
    /// are derived from client-local Unity flowers.
    /// </summary>
    public sealed partial class PollinationPremiumWindow
    {
        private void BuildBody(bool resetScroll)
        {
            if (content == null || scroller == null || controller == null) return;
            float oldPosition = scroller.verticalNormalizedPosition;
            for(int i=content.childCount-1;i>=0;i--)
            {
                GameObject old=content.GetChild(i).gameObject;
                old.SetActive(false);
                if (Application.isPlaying) Destroy(old);
                else DestroyImmediate(old);
            }
            liveLabels.Clear();
            liveButtons.Clear();
            PollinationScreenModel m=controller.Model;
            float cursor=14f;
            float cardWidth=Mathf.Max(160f,viewportWidth-36f);
            RectTransform menu=CardAt(cursor,128f,
                T("LA VIE DES FLEURS","THE LIFE OF FLOWERS"),"nursery");
            float buttonWidth=Mathf.Max(75f,(cardWidth-52f)/3f);
            string[] tabFr={"Champs","Essaims","Alliance"};
            string[] tabEn={"Fields","Swarms","Alliance"};
            for(int i=0;i<3;i++)
            {
                int chosen=i;
                ButtonAt(menu,new Rect(18f+i*(buttonWidth+8f),75f,
                    buttonWidth,41f),French?tabFr[i]:tabEn[i],
                    i==activeTab,()=>SelectTabForProof(chosen));
            }
            cursor+=148f;

            if(m.State==PollinationScreenState.NotConfigured ||
               (m.State==PollinationScreenState.Loading && m.Population==null) ||
               (m.State==PollinationScreenState.Unavailable && m.Population==null))
            {
                RectTransform message=CardAt(cursor,158f,
                    T("UN MONDE EN ÉVEIL","A WORLD AWAKENING"),"nursery");
                Label(message,new Rect(18f,78f,cardWidth-36f,57f),
                    T("La pollinisation se prépare. Votre ruche et vos ressources restent protégées.",
                        "Pollination is being prepared. Your hive and resources remain protected."),
                    14f,Paper);
                cursor+=178f;
            }
            else
            {
                switch(activeTab)
                {
                    case 0: cursor=BuildFieldCards(cursor,m); break;
                    case 1: cursor=BuildSwarmCards(cursor,m); break;
                    default:cursor=BuildAllianceCards(cursor,m);break;
                }
            }

            SetContentHeight(cursor);
            CaptureReferences();
            Canvas.ForceUpdateCanvases();
            scroller.verticalNormalizedPosition=resetScroll?1f:oldPosition;
            RefreshVisibleValues();
        }

        private float BuildFieldCards(float cursor,PollinationScreenModel m)
        {
            float width=Mathf.Max(160f,viewportWidth-36f);
            if (m.Sites == null || m.Sites.Count == 0)
            {
                RectTransform empty=CardAt(cursor,148f,
                    T("CHAMPS FLORAUX","FLORAL FIELDS"),"nursery");
                Label(empty,new Rect(18f,78f,width-36f,55f),
                    T("Aucun site floral n'est actuellement accessible.",
                      "No floral field is currently available."),14f,Paper);
                return cursor+168f;
            }
            if(m.FloralSitesOnly)
            {
                RectTransform preview = CardAt(cursor,118f,
                    T("APERÇU DES CHAMPS","FIELDS PREVIEW"),"nursery");
                Label(preview,new Rect(18f,73f,width-36f,43f),
                    T("Les prairies sont réelles. Les essaims et expéditions ouvriront plus tard.",
                      "These fields are real. Swarms and expeditions will open later."),
                    14f,Paper);
                cursor+=138f;
            }
            RectTransform overview=CardAt(cursor,138f,
                T("ÉCOSYSTÈME PARTAGÉ","SHARED ECOSYSTEM"),"nursery");
            Label(overview,new Rect(18f,78f,width-36f,48f),
                T("Chaque champ possède une réserve commune à tous les joueurs. Les gains sont validés par le serveur.",
                  "Every field has one shared reserve. All rewards are validated by the server."),
                14f,Muted);
            cursor+=158f;
            foreach(RemoteFloralSite site in m.Sites)
            {
                string id=site.SiteId;
                bool selected=site.SiteId==m.SelectedSiteId;
                RectTransform card=CardAt(cursor,146f,
                    site.LocalizedName(BeeLocalization.CurrentLocale),"nursery");
                LiveLabel(card,new Rect(18f,76f,width-160f,34f),
                    ()=>T("Réserve de pollen : ","Pollen reserves: ")
                        +site.PollenRemaining+" / "+site.PollenCapacity,
                    15f,Paper,true);
                LiveLabel(card,new Rect(18f,112f,width-160f,24f),
                    ()=>T("Fertilité : ","Fertility: ")
                        +(site.FertilityBp/100f).ToString("0.#")+" %",13f,Muted);
                ButtonAt(card,new Rect(width-135f,82f,117f,45f),
                    selected?T("Choisi","Selected"):T("Choisir","Select"),
                    selected,()=>
                    {
                        controller.SelectSite(id);
                        BuildBody(false);
                    },()=>!controller.IsBusy);
                cursor+=166f;
            }

            RemoteFloralSite active=m.SelectedSite;
            if(active!=null)
            {
                RectTransform metrics=CardAt(cursor,222f,
                    T("SANTÉ DU CHAMP","FIELD HEALTH"),"nursery");
                LiveLabel(metrics,new Rect(18f,76f,width-36f,26f),
                    ()=>T("Fertilité : ","Fertility: ")
                        +(active.FertilityBp/100f).ToString("0.#")+" %",
                    17f,Gold,true);
                Progress(metrics,new Rect(18f,105f,width-36f,12f),
                    active.FertilityBp,10000,new Color(.33f,.65f,.40f));
                LiveLabel(metrics,new Rect(18f,123f,width-36f,24f),
                    ()=>T("Biodiversité : ","Biodiversity: ")
                        +(active.BiodiversityBp/100f).ToString("0.#")+" %",
                    15f,Paper);
                Progress(metrics,new Rect(18f,150f,width-36f,12f),
                    active.BiodiversityBp,10000,new Color(.37f,.64f,.78f));
                LiveLabel(metrics,new Rect(18f,171f,width-36f,37f),
                    ()=>T("Contributeurs : ","Contributors: ")
                        +active.DistinctContributors+
                        T("  ·  Points : ","  ·  Points: ")+active.TotalPollinationPoints,
                    13f,Muted);
                cursor+=242f;
                RectTransform actions=CardAt(cursor,143f,
                    T("DÉPART EN EXPÉDITION","EXPEDITION PREPARATION"),"nursery");
                Label(actions,new Rect(18f,77f,width-176f,50f),
                    T("Envoyez des butineuses civiles vers le champ sélectionné.",
                      "Send civilian foragers to the selected field."),
                    14f,Paper);
                ButtonAt(actions,new Rect(width-152f,83f,134f,43f),
                    T("Préparer","Prepare"),true,()=>SelectTabForProof(1));
                cursor+=163f;
            }
            return cursor;
        }

        private string CivilCount(long value) =>
            value.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo(
                French?"fr-CA":"en-US"));

        private float BuildSwarmCards(float cursor,PollinationScreenModel m)
        {
            float width=Mathf.Max(160f,viewportWidth-36f);
            RemoteCivilPopulationSnapshot p=m.Population;
            RemoteCivilExpeditionSnapshot e=m.Expedition;
            bool readOnly=m.FloralSitesOnly;
            RectTransform roster=CardAt(cursor,readOnly?216f:183f,
                T("BUTINEUSES CIVILES","CIVILIAN FORAGERS"),"nursery");
            LiveLabel(roster,new Rect(18f,78f,width-36f,32f),
                ()=>T("Disponibles : ","Available: ")
                    +(p==null?"--":CivilCount(p.AvailableWorkers)),19f,Gold,true);
            LiveLabel(roster,new Rect(18f,111f,width-36f,26f),
                ()=>T("En mission : ","Assigned: ")
                    +(p==null?"--":CivilCount(p.AssignedWorkers)),15f,Paper);
            LiveLabel(roster,new Rect(18f,139f,width-36f,27f),
                ()=>T(readOnly?"Capacité maximale : ":"Capacité : ",
                       readOnly?"Maximum capacity: ":"Capacity: ")
                    +(p==null?"--":CivilCount(p.Capacity)),14f,Muted);
            if(readOnly)
                Label(roster,new Rect(18f,170f,width-36f,28f),
                    T("Capacité de la nurserie, pas des butineuses déjà créées.",
                      "Nursery capacity, not the number of foragers created."),
                    13f,Muted);
            cursor+=readOnly?236f:203f;

            RectTransform nursery=CardAt(cursor,151f,
                readOnly
                    ? T("FORMATION À VENIR","TRAINING COMING SOON")
                    : T("NOUVELLE COHORTE","NEW BROOD COHORT"),"nursery");
            if(readOnly)
            {
                Label(nursery,new Rect(18f,76f,width-36f,30f),
                    T("APERÇU EN LECTURE SEULE","READ-ONLY PREVIEW"),
                    15f,Gold,true);
                Label(nursery,new Rect(18f,109f,width-36f,39f),
                    T("Formation désactivée. Aucun miel ni pollen dépensé.",
                      "Training is disabled. No honey or pollen is spent."),
                    14f,Muted);
                // Do not draw a dead pay button. The server also forbids POST.
            }
            else
            {
                LiveLabel(nursery,new Rect(18f,75f,width-36f,28f),
                    ()=>p==null
                        ? T("Formation des butineuses à venir","Forager training coming soon")
                        : p.Active==null
                            ? T(p.CanStart?"Nurserie disponible":"Formation indisponible",
                                p.CanStart?"Nursery available":"Training unavailable")
                            : T("Éclosion prévue : ","Expected emergence: ")
                                +p.Active.ReadyAtUtc.ToLocalTime().ToString("g"),
                    14f,Paper);
                ButtonAt(nursery,new Rect(18f,111f,
                    Mathf.Min(260f,width-36f),38f),
                    T("Former une cohorte","Start a cohort"),true,
                    ()=>controller.StartCivilCohort(),()=>m.CanStartCohort &&
                        !controller.IsBusy);
            }
            cursor+=171f;

            RectTransform voyage=CardAt(cursor,e?.Active==null?260f:240f,
                T("EXPÉDITION DE POLLINISATION","POLLINATION EXPEDITION"),
                "nursery");
            if(e == null)
            {
                Label(voyage,new Rect(18f,76f,width-36f,61f),
                    T("Les expéditions ouvriront avec les essaims civils.",
                      "Expeditions will open with civilian swarms."),14f,Paper);
                return cursor+280f;
            }
            if(e.Active!=null)
            {
                LiveLabel(voyage,new Rect(18f,78f,width-36f,30f),
                    ()=>T("Destination : ","Destination: ")+e.Active.SiteId,
                    15f,Paper,true);
                LiveLabel(voyage,new Rect(18f,111f,width-36f,32f),
                    ()=>T("Phase : ","Phase: ")+PhaseLabel(e.Status),
                    16f,Gold,true);
                LiveLabel(voyage,new Rect(18f,146f,width-36f,28f),
                    ()=>T("Butineuses engagées : ","Assigned foragers: ")
                        +e.Active.Workers,14f,Paper);
                LiveLabel(voyage,new Rect(18f,179f,width-36f,42f),
                    ()=>T("Retour prévu : ","Expected return: ")
                        +e.Active.ReturnsAtUtc.ToLocalTime().ToString("g"),
                    14f,Muted);
                cursor+=260f;
            }
            else
            {
                RemoteFloralSite site=m.SelectedSite;
                LiveLabel(voyage,new Rect(18f,78f,width-36f,31f),
                    ()=>T("Destination : ","Destination: ")
                        +(site?.LocalizedName(BeeLocalization.CurrentLocale)??"--"),
                    15f,Paper,true);
                LiveLabel(voyage,new Rect(18f,112f,width-36f,29f),
                    ()=>T("Taille de l'essaim : ","Swarm size: ")+m.DraftWorkers,
                    16f,Gold,true);
                float buttonWidth=Mathf.Min(84f,Mathf.Max(45f,(width-60f)/4f));
                ButtonAt(voyage,new Rect(18f,150f,buttonWidth,40f),
                    "−1",false,()=>controller.AdjustWorkers(-1),
                    ()=>!controller.IsBusy);
                ButtonAt(voyage,new Rect(26f+buttonWidth,150f,buttonWidth,40f),
                    "+1",false,()=>controller.AdjustWorkers(1),
                    ()=>!controller.IsBusy);
                ButtonAt(voyage,new Rect(34f+2f*buttonWidth,150f,buttonWidth,40f),
                    "+10",false,()=>controller.AdjustWorkers(10),
                    ()=>!controller.IsBusy);
                ButtonAt(voyage,new Rect(18f,206f,
                    Mathf.Min(350f,width-36f),45f),
                    T("Envoyer l'essaim civil","Send civilian swarm"),
                    true,()=>controller.LaunchSelectedExpedition(),
                    ()=>m.CanLaunch && !controller.IsBusy);
                cursor+=280f;
            }

            if(e.LastCompletion!=null)
            {
                RectTransform report=CardAt(cursor,139f,
                    T("DERNIER RAPPORT","LAST REPORT"),"nursery");
                LiveLabel(report,new Rect(18f,76f,width-36f,30f),
                    ()=>T("Pollen réellement livré : ","Pollen delivered: ")
                        +e.LastCompletion.PollenCredited,16f,Gold,true);
                LiveLabel(report,new Rect(18f,110f,width-36f,24f),
                    ()=>T("Contribution écologique : ","Ecological contribution: ")
                        +e.LastCompletion.PollinationPoints,
                    14f,Paper);
                cursor+=159f;
            }
            return cursor;
        }

        private float BuildAllianceCards(float cursor,PollinationScreenModel m)
        {
            float width=Mathf.Max(160f,viewportWidth-36f);
            if(m.AllianceId==Guid.Empty)
            {
                RectTransform empty=CardAt(cursor,151f,
                    T("L'ESSAIM FAIT LA FORCE","STRENGTH IN THE SWARM"),"nursery");
                Label(empty,new Rect(18f,77f,width-36f,67f),
                    T("Rejoignez une alliance pour participer aux projets floraux collectifs.",
                      "Join an alliance to participate in collective floral projects."),
                    15f,Paper);
                return cursor+171f;
            }
            var projectList=m.Projects;
            if(!m.ProjectServiceAvailable)
            {
                RectTransform preview=CardAt(cursor,151f,
                    T("PROJETS FLORAUX À VENIR","FLORAL PROJECTS COMING SOON"),
                    "nursery");
                Label(preview,new Rect(18f,77f,width-36f,60f),
                    T("Les missions collectives ouvriront avec les alliances.",
                      "Cooperative missions will open with alliances."),
                    14f,Paper);
                cursor+=171f;
            }
            else if(projectList==null || projectList.Count==0)
            {
                RectTransform empty=CardAt(cursor,157f,
                    T("PROJETS FLORAUX","FLORAL PROJECTS"),"nursery");
                Label(empty,new Rect(18f,77f,width-36f,60f),
                    T("Aucun projet ouvert dans votre alliance.",
                      "Your alliance has no open project."),
                    15f,Paper);
                cursor+=177f;
            }
            else
            {
                foreach(RemoteFloralAllianceProjectSnapshot wrapper in projectList)
                {
                    RemoteFloralAllianceProject project=wrapper?.Project;
                    if(project==null) continue;
                    RemoteFloralSite site=m.Sites?.FirstOrDefault(x=>x.SiteId==project.SiteId);
                    RectTransform card=CardAt(cursor,228f,
                        site?.LocalizedName(BeeLocalization.CurrentLocale)??project.SiteId,
                        "nursery");
                    LiveLabel(card,new Rect(18f,76f,width-36f,34f),
                        ()=>T("Objectif : ","Goal: ")+project.Points+
                            " / "+project.GoalPoints,17f,Gold,true);
                    Progress(card,new Rect(18f,110f,width-36f,12f),
                        project.Points,project.GoalPoints,Gold);
                    LiveLabel(card,new Rect(18f,129f,width-36f,30f),
                        ()=>T("Membres qualifiés : ","Qualified members: ")
                            +project.QualifiedMembers+" / "+project.RequiredDistinctMembers,
                        15f,Paper);
                    Progress(card,new Rect(18f,163f,width-36f,12f),
                        project.QualifiedMembers,project.RequiredDistinctMembers,
                        new Color(.36f,.75f,.46f));
                    Label(card,new Rect(18f,185f,width-36f,27f),
                        wrapper.IsCompleted?T("Projet terminé","Project completed")
                            :T("Contribution collective en cours","Collective project in progress"),
                        14f,Muted);
                    cursor+=248f;
                }
            }
            bool readOnlyAlliance = !m.ProjectServiceAvailable ||
                m.Expedition == null;
            RectTransform action=CardAt(cursor,172f,
                T("COOPÉRATION","COOPERATION"),"nursery");
            Label(action,new Rect(18f,77f,width-36f,37f),
                T("La réussite exige plusieurs membres ayant réellement participé.",
                  "Completion requires multiple genuinely contributing members."),
                14f,Paper);
            if(readOnlyAlliance)
            {
                Label(action,new Rect(18f,120f,width-36f,43f),
                    T("Consultation seulement : création de projet à venir.",
                      "Read-only: project creation is coming soon."),
                    14f,Muted);
            }
            else
            {
                ButtonAt(action,new Rect(18f,121f,
                    Mathf.Min(290f,width-36f),42f),
                    T("Ouvrir un projet floral","Create a floral project"),
                    true,()=>controller.CreateSelectedAllianceProject(),
                    ()=>m.CanCreateProject && !controller.IsBusy);
            }
            cursor+=192f;
            return cursor;
        }

        private void Progress(Transform parent,Rect rect,long value,
            long capacity,Color fill)
        {
            Plate(parent,"Track",rect,new Color(.12f,.08f,.04f),"panel-fill");
            float progress=capacity>0
                ?Mathf.Clamp01((float)((double)value/capacity)):0f;
            if(progress>0f)
                Plate(parent,"Fill",new Rect(rect.x+1f,rect.y+1f,
                    Mathf.Max(1f,(rect.width-2f)*progress),
                    Mathf.Max(1f,rect.height-2f)),fill,"etched-line");
        }

        private static string PhaseLabel(string phase)
        {
            switch(phase)
            {
                case "outbound":return T("En route","Outbound");
                case "pollinating":return T("Pollinisation","Pollinating");
                case "returning":return T("Retour","Returning");
                case "awaiting_settlement":return T("Retour arrivé","Arrived");
                default:return T("Disponible","Available");
            }
        }
    }
}
