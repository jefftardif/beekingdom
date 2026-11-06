using System;
using System.Linq;
using System.Threading.Tasks;
using BeeKingdom.Gameplay.Communication;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public static partial class HiveViewProductUiPresenter
    {
        private static bool worldChatSettingsOpen;
        private static bool worldChatOpening;
        private static bool worldChatPointerCaptured;
        private static int worldChatInputSuppressedThroughFrame=-1;
        private static string worldChatPreferenceOwner;
        private static LivingHiveChatFeedChannels worldChatFilter;
        private static bool IsHiveChatHost => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.StartsWith("Environment2D5D",StringComparison.Ordinal);
        private static Rect CurrentSharedChatBar
        {
            get
            {
                if(!IsHiveChatHost)return WorldMapHudLayout.CurrentBar;
                return BeeKingdom.LivingHiveMenu.LivingHiveMenuSpec.RailRectForSafeArea(Screen.width,Screen.height,Screen.safeArea);
            }
        }
        private static Rect CurrentSharedChatFeed => WorldMapHudLayout.ChatFeed(CurrentSharedChatBar,WorldMapHudLayout.SafeGuiRect(Screen.width,Screen.height,Screen.safeArea));
        private static Rect CurrentSharedChatSettings => WorldMapHudLayout.WithinSafeArea(new Rect(CurrentSharedChatBar.x,CurrentSharedChatBar.y-240f,CurrentSharedChatBar.width,232f));
        internal static bool WorldChatFullScreenOpen => chatScreenOpen;
        internal static bool WorldChatInputBlocked
        {
            get
            {
                bool held=Input.GetMouseButton(0);
                bool started=Input.GetMouseButtonDown(0);
                Vector2 point=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
                bool overFeed=WorldChatFeedVisibleForMap && started && CurrentSharedChatFeed.Contains(point);
                for(int i=0;i<Input.touchCount;i++)
                {
                    Touch touch=Input.GetTouch(i);
                    if(touch.phase==TouchPhase.Ended || touch.phase==TouchPhase.Canceled)continue;
                    held=true;
                    if(touch.phase==TouchPhase.Began)
                    { started=true;overFeed |= WorldChatFeedVisibleForMap && CurrentSharedChatFeed.Contains(new Vector2(touch.position.x,Screen.height-touch.position.y)); }
                }
                bool modal=worldChatSettingsOpen || chatScreenOpen || SettingsOwnCommunicationInput;
                if((modal && held) || (started && overFeed))worldChatPointerCaptured=true;
                if(worldChatPointerCaptured && !held)
                {
                    worldChatPointerCaptured=false;
                    worldChatInputSuppressedThroughFrame=Time.frameCount+1;
                }
                return WorldMapMmoFullscreenFoundationBootstrap.SharedNavigationPanelOpen || HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking() || modal || worldChatPointerCaptured || Time.frameCount<=worldChatInputSuppressedThroughFrame;
            }
        }
        // The Plus popup is owned by the uGUI menu, not activeMainMenuId.
        // Preserve its raycaster: treating it as an external modal would disable
        // the very canvas that must receive the Settings row click.
        private static bool HiveMoreMenuOccludesChatFeed
        {
            get
            {
                if (!IsHiveChatHost) return false;
                var menu = BeeKingdom.LivingHiveMenu.LivingHiveMenuRuntime.CanvasComponent;
                return menu != null &&
                    (menu.State.IsMenuOpen(BeeKingdom.LivingHiveMenu.LivingHiveMenuSpec.MoreId) ||
                     menu.PanelShown(BeeKingdom.LivingHiveMenu.LivingHiveMenuSpec.MoreId));
            }
        }
        internal static bool WorldChatFeedVisibleForMap => WorldChatFilter()!=LivingHiveChatFeedChannels.None && !chatScreenOpen && !HiveMoreMenuOccludesChatFeed;

        private static LivingHiveChatFeedChannels WorldChatFilter()
        {
            string owner=MobileAccountSessionRuntimeBootstrap.GameplayPlayerId.ToString("N");
            if(worldChatPreferenceOwner!=owner)
            {
                worldChatPreferenceOwner=owner;
                worldChatFilter=(LivingHiveChatFeedChannels)(PlayerPrefs.GetInt("beekingdom.worldchat.channels."+owner,1)&15);
                worldChatSettingsOpen=false;
            }
            LivingHiveChatRuntime.SetFeedChannels(worldChatFilter);
            return worldChatFilter;
        }
        private static void SetWorldChatFilter(LivingHiveChatFeedChannels value)
        {
            WorldChatFilter();worldChatFilter=value;
            PlayerPrefs.SetInt("beekingdom.worldchat.channels."+worldChatPreferenceOwner,(int)value);
            PlayerPrefs.Save();
            LivingHiveChatRuntime.SetFeedChannels(worldChatFilter);
        }
        private static string WorldChatChannelLabel(LivingHiveChatFeedChannels value)
        {
            switch(value)
            {
                case LivingHiveChatFeedChannels.World:return "Monde";
                case LivingHiveChatFeedChannels.Alliance:return "Alliance";
                case LivingHiveChatFeedChannels.Private:return "Privé";
                case LivingHiveChatFeedChannels.Group:return "Groupes";
                default:return string.Empty;
            }
        }
        private static string WorldChatSingleLine(string value) => (value??string.Empty).Replace('\n',' ').Replace('\r',' ');

        internal static bool IsWorldChatPointBlocked(Vector2 point) => WorldChatInputBlocked ||
            (WorldChatFeedVisibleForMap && CurrentSharedChatFeed.Contains(point));

        private static void DrawWorldChatFeed()
        {
            if (SettingsOwnCommunicationInput) return;
            WorldChatFilter();
            LivingHiveChatSnapshot snapshot=ChatServerSnapshot();
            if(chatScreenOpen && !LivingHiveChatFeed.IsLive(snapshot))CloseChatScreen();
            if(chatScreenOpen)
            {
                DrawChatScreen(IsPortraitLayout());
                return;
            }
            if(WorldChatFeedVisibleForMap && !worldChatSettingsOpen && CurrentSharedChatFeed.height>=8f)
            {
                Rect panel=CurrentSharedChatFeed;
                WorldMapHudLayout.DrawChatFeedSurface(panel);
                var rows=LivingHiveChatFeed.Select(snapshot,worldChatFilter);
                float rowHeight=Mathf.Max(0f,(panel.height-8f)/3f);
                var style=new GUIStyle(smallStyle) { fontSize=Mathf.Min(12,Mathf.Max(8,Mathf.FloorToInt(rowHeight*.45f))),richText=false,wordWrap=false,clipping=TextClipping.Clip,alignment=TextAnchor.MiddleLeft };
                style.normal.textColor=new Color(1f,.95f,.82f,1f);
                for(int i=0;i<3;i++)
                {
                    Rect row=new Rect(panel.x+10f,panel.y+4f+i*rowHeight,panel.width-20f,rowHeight);
                    if(i>=rows.Count)
                    {
                        if(i==0)GUI.Label(row,LivingHiveChatFeed.IsLive(snapshot)?"Aucun message dans les canaux choisis":"Chat non connecté",style);
                        continue;
                    }
                    LivingHiveChatMessage message=rows[i];
                    var conversation=LivingHiveChatFeed.Resolve(snapshot,message.ConversationId);
                    string prefix="["+WorldChatChannelLabel(LivingHiveChatFeed.Channel(conversation.ChannelType))+"] ";
                    string text=prefix+WorldChatSingleLine(message.SenderDisplayName)+" : "+WorldChatSingleLine(message.VisibleBody??message.OriginalBody);
                    GUI.Label(row,new GUIContent(text,text),style);
                    if(GUI.Button(row,GUIContent.none,GUIStyle.none))_ = OpenWorldChatConversationAsync(message.ConversationId);
                }
            }
            if(worldChatSettingsOpen)DrawWorldChatSettings(snapshot);
        }

        private static void DismissWorldChatSettings()
        {
            HiveMapOverlayInputGateBootstrap.CaptureClosingGesture();
            worldChatSettingsOpen=false;
            worldChatPointerCaptured=true;
            worldChatInputSuppressedThroughFrame=Time.frameCount+1;
            ReleaseGuiInputCapture();
            if(Event.current!=null && Event.current.type!=EventType.Layout && Event.current.type!=EventType.Repaint)Event.current.Use();
        }
        private static void DisableWorldChatFeed()
        {
            SetWorldChatFilter(LivingHiveChatFeedChannels.None);
            DismissWorldChatSettings();
        }

        private static void DrawWorldChatSettings(LivingHiveChatSnapshot snapshot)
        {
            Rect panel=CurrentSharedChatSettings;
            WorldMapHudLayout.DrawChatFeedSurface(panel,1f);
            GUI.Label(new Rect(panel.x+14f,panel.y+10f,panel.width-70f,25f),"CHAT PENDANT LE JEU",new GUIStyle(badgeStyle) { fontSize=13 });
            if(GUI.Button(new Rect(panel.xMax-48f,panel.y+4f,40f,40f),"×") ||
                (Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Escape))
            {
                DismissWorldChatSettings();return;
            }
            var available=LivingHiveChatFeed.Available(snapshot);
            var choices=new[]{LivingHiveChatFeedChannels.World,LivingHiveChatFeedChannels.Alliance,LivingHiveChatFeedChannels.Private,LivingHiveChatFeedChannels.Group};
            for(int i=0;i<choices.Length;i++)
            {
                var choice=choices[i];bool selected=(worldChatFilter&choice)!=0;
                bool allowed=(available&choice)!=0;
                Rect row=new Rect(panel.x+14f+(i%2)*(panel.width-28f)*.5f,panel.y+46f+(i/2)*42f,(panel.width-32f)*.5f,40f);
                bool previous=GUI.enabled;GUI.enabled=previous && (allowed||selected);
                bool next=GUI.Toggle(row,selected,WorldChatChannelLabel(choice));GUI.enabled=previous;
                if(next!=selected && (!next||allowed))SetWorldChatFilter(next?worldChatFilter|choice:worldChatFilter&~choice);
            }
            if(GUI.Button(new Rect(panel.x+14f,panel.y+136f,(panel.width-34f)*.5f,42f),"Aucun canal"))
            {
                DisableWorldChatFeed();
                return;
            }
            bool old=GUI.enabled;GUI.enabled=old && LivingHiveChatFeed.IsLive(snapshot) && snapshot.Conversations.Count>0 && !worldChatOpening;
            if(GUI.Button(new Rect(panel.x+20f+(panel.width-34f)*.5f,panel.y+136f,(panel.width-34f)*.5f,42f),"Ouvrir le chat"))
            {
                var conversation=snapshot.Conversations.FirstOrDefault(c=>c!=null && (LivingHiveChatFeed.Channel(c.ChannelType)&worldChatFilter)!=0)??snapshot.Conversations.FirstOrDefault();
                if(conversation!=null)_ = OpenWorldChatConversationAsync(conversation.ConversationId);
            }
            GUI.enabled=old;
            GUI.Label(new Rect(panel.x+14f,panel.y+186f,panel.width-28f,32f),LivingHiveChatFeed.IsLive(snapshot)?"Seuls vos canaux autorisés sont proposés.":"Connectez-vous au chat pour choisir vos canaux.",new GUIStyle(smallStyle) { fontSize=11,wordWrap=true });
        }

        private static async Task OpenWorldChatConversationAsync(string conversationId)
        {
            if(worldChatOpening || LivingHiveChatFeed.Resolve(ChatServerSnapshot(),conversationId)==null)return;
            Guid owner=MobileAccountSessionRuntimeBootstrap.GameplayPlayerId;
            worldChatOpening=true;
            try
            {
                await LivingHiveChatRuntime.SelectAsync(conversationId);
                var snapshot=ChatServerSnapshot();
                var target=LivingHiveChatFeed.Resolve(snapshot,conversationId);
                if(owner!=MobileAccountSessionRuntimeBootstrap.GameplayPlayerId || target==null || snapshot.SelectedConversationId!=conversationId)return;
                ChatRoyalSyncFromServer();
                // This entry point never brings legacy demo conversations into the premium screen.
                chatConversations.RemoveAll(c=>!snapshot.Conversations.Any(real=>real.ConversationId==c.Id));
                worldChatSettingsOpen=false;
                OpenChatScreenTargeted(ChatChannelIdFor(target.ChannelType),conversationId,true);
            }
            catch(Exception exception) { Debug.LogWarning("[WorldChat] Open failed: "+exception.GetType().Name); }
            finally { worldChatOpening=false; }
        }
    }
}
