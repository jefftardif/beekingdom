using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeeKingdom.Audio;
using BeeKingdom.Networking;
using UnityEngine;
namespace BeeKingdom.Playground {
 public static partial class HiveViewProductUiPresenter {

        private static bool bankOverlayOpenForExternalHost;
        private static int bankRequestVersion;

        private static Vector2 bankRoadmapScroll;

        private static string bankSelectedView = "roadmap";

        private static RemoteRoyalReserveSnapshot bankRoyalReserveSnapshot;

        private static bool bankRoyalReserveBusy;

        private static string bankRoyalReserveError = string.Empty;

        private static string bankRoyalReserveNotice = string.Empty;

        private static string bankReserveResource = "honey";

        private static string bankReserveAmountText = "100";


        private sealed class BankMilestone
        {
            public readonly int Level;
            public readonly string Title;
            public readonly string Description;
            public readonly string IconId;

            public BankMilestone(int level, string title, string description, string iconId)
            {
                Level = level;
                Title = title;
                Description = description;
                IconId = iconId;
            }
        }


        private static readonly BankMilestone[] BankMilestones =
        {
            new BankMilestone(1,  "Réserve Royale", "Mettez une partie de vos ressources à l'abri et préparez vos objectifs futurs.", "royal-jelly"),
            new BankMilestone(3,  "Coffres spécialisés", "Créez des fonds dédiés : militaire, construction, recherche et reconstruction.", "capacity"),
            new BankMilestone(5,  "Certificats de cire", "Immobilisez des ressources pour obtenir de meilleures conditions économiques.", "wax"),
            new BankMilestone(10, "Marché d'échange", "Convertissez vos surplus selon des taux contrôlés par l'économie du serveur.", "resources"),
            new BankMilestone(12, "Assurance des expéditions", "Couvrez une partie de la valeur transportée lors des sorties risquées.", "shield"),
            new BankMilestone(15, "Avance économique", "Lancez certains projets avant d'avoir accumulé 100 % des ressources.", "production"),
            new BankMilestone(20, "Obligations de la Ruche", "Financez progressivement les grands projets de votre colonie.", "queen-core"),
            new BankMilestone(23, "Investissements d'Alliance", "Contribuez à certains grands projets collectifs contre prestige et contribution.", "alliance-center"),
            new BankMilestone(25, "Conseiller économique", "Anticipez pénuries, dépenses et futurs paliers finançables.", "book"),
            new BankMilestone(29, "Indice de confiance", "Votre stabilité économique influence progressivement vos conditions bancaires.", "events"),
            new BankMilestone(30, "Spécialisation bancaire", "Orientez la Banque vers la sécurité, le commerce, l'expansion ou l'entraide.", "queen")
        };


        public static bool BankOverlayOpenForExternalHost => bankOverlayOpenForExternalHost;


        public static void OpenBankOverlayForExternalHost()
        {
            MobileAccountSessionRuntimeBootstrap.TryConfigureGameplayForActiveSession();
            if (OfficialBuildingUpgradeConfigured() && buildingUpgradeController != null && !buildingUpgradeController.IsBusy
                && (buildingUpgradeController.Model == null || buildingUpgradeController.Model.BuildingLevels.Count == 0))
                buildingUpgradeController.Refresh();
            ResetBankViewState();
            bankOverlayOpenForExternalHost = true;
            bankRoyalReserveSnapshot = MobileAccountSessionRuntimeBootstrap.TakeStartupBankSnapshot();
            bankRoadmapScroll = Vector2.zero;
            bankSelectedView = "roadmap";
            bankRoyalReserveError = string.Empty;
            bankRoyalReserveNotice = string.Empty;
            try { BeeKingdom.Tutorial.TutorialGameplayNotifier.NotifyWindowOpened("hive_bank"); } catch {}
        }


        public static void CloseBankOverlayForExternalHost()
        {
            bankOverlayOpenForExternalHost = false;
            ResetBankViewState();
            bankSelectedView = "roadmap";
            bankRoyalReserveError = string.Empty;
            bankRoyalReserveNotice = string.Empty;
        }


        public static void DrawBankOverlayForExternalHost(bool compact)
        {
            if (!bankOverlayOpenForExternalHost) return;
            EnsureStyles();

            int previousDepth = GUI.depth;
            GUI.depth = HiveMapUiOcclusion.ModalWindowGuiDepth;
            try
            {
                DrawBankScreen(compact);
            }
            finally
            {
                GUI.depth = previousDepth;
            }
        }


        // Every opening is read fresh; late responses from a closed view or another
        // account may not reintroduce an old player's balances or unlock actions.
        private static void ResetBankViewState()
        {
            unchecked { bankRequestVersion++; }
            bankRoyalReserveSnapshot = null;
            bankRoyalReserveBusy = false;
            bankRoyalReserveError = string.Empty;
            bankRoyalReserveNotice = string.Empty;
            bankReserveAmountText = "100";
            bankReserveResource = "honey";
        }

        private static bool BankRequestIsCurrent(int version, IRoyalReserveClient client, Guid hiveId)
        {
            return version == bankRequestVersion && bankOverlayOpenForExternalHost
                && hiveId == MobileAccountSessionRuntimeBootstrap.GameplayHiveId
                && ReferenceEquals(client, MobileAccountSessionRuntimeBootstrap.RoyalReserveClient);
        }
        private static int BankDisplayedLevel(out bool pending)
        {
            pending = false;
            if (TryResolveAuthoritativeBuildingLevel("hive_bank", out int authoritative))
                return Mathf.Max(1, authoritative);

            if (AuthoritativeBuildingLevelsExpected())
            {
                pending = true;
                return 0;
            }

            return Mathf.Max(1, DisplayedBuildingLevel("hive_bank"));
        }


        private static BankMilestone NextBankMilestone(int level)
        {
            for (int i = 0; i < BankMilestones.Length; i++)
                if (BankMilestones[i].Level > level) return BankMilestones[i];
            return null;
        }


        private static async void RefreshBankRoyalReserveAsync()
        {
            if (bankRoyalReserveBusy) return;
            IRoyalReserveClient client = MobileAccountSessionRuntimeBootstrap.RoyalReserveClient;
            Guid hiveId = MobileAccountSessionRuntimeBootstrap.GameplayHiveId;
            if (client == null || hiveId == Guid.Empty)
            {
                bankRoyalReserveError = "Connexion au serveur officiel requise.";
                return;
            }

            int requestVersion = bankRequestVersion;
            bankRoyalReserveBusy = true;
            bankRoyalReserveError = string.Empty;
            try
            {
                RemoteRoyalReserveSnapshot snapshot = await client.ReadAsync(hiveId);
                if (BankRequestIsCurrent(requestVersion, client, hiveId)) bankRoyalReserveSnapshot = snapshot;
            }
            catch (RoyalReserveClientException exception)
            {
                if (!BankRequestIsCurrent(requestVersion, client, hiveId)) return;
                bankRoyalReserveSnapshot = null;
                bankRoyalReserveError = BankRoyalReserveErrorMessage(exception.SafeCode);
            }
            catch (Exception)
            {
                if (!BankRequestIsCurrent(requestVersion, client, hiveId)) return;
                bankRoyalReserveSnapshot = null;
                bankRoyalReserveError = "Impossible de synchroniser la Réserve Royale.";
            }
            finally
            {
                if (requestVersion == bankRequestVersion) bankRoyalReserveBusy = false;
            }
        }


        private static async void TransferBankRoyalReserveAsync(bool withdraw)
        {
            if (bankRoyalReserveBusy) return;
            IRoyalReserveClient client = MobileAccountSessionRuntimeBootstrap.RoyalReserveClient;
            Guid hiveId = MobileAccountSessionRuntimeBootstrap.GameplayHiveId;
            if (client == null || hiveId == Guid.Empty || bankRoyalReserveSnapshot == null)
            {
                bankRoyalReserveError = "Synchronisez d'abord la Réserve Royale.";
                return;
            }

            string normalizedAmount = (bankReserveAmountText ?? string.Empty).Replace(" ", string.Empty).Replace("\u00a0", string.Empty);
            if (!long.TryParse(normalizedAmount, NumberStyles.None, CultureInfo.InvariantCulture, out long amount) || amount <= 0)
            {
                bankRoyalReserveError = "Entrez un montant supérieur à zéro.";
                return;
            }

            bankReserveAmountText = amount.ToString(CultureInfo.InvariantCulture);
            int requestVersion = bankRequestVersion;
            bankRoyalReserveBusy = true;
            bankRoyalReserveError = string.Empty;
            bankRoyalReserveNotice = string.Empty;
            try
            {
                string key = "bank-" + (withdraw ? "withdraw-" : "deposit-") + Guid.NewGuid().ToString("N");
                RemoteRoyalReserveSnapshot snapshot = withdraw
                    ? await client.WithdrawAsync(
                        hiveId,
                        bankReserveResource,
                        amount,
                        bankRoyalReserveSnapshot.Revision,
                        key)
                    : await client.DepositAsync(
                        hiveId,
                        bankReserveResource,
                        amount,
                        bankRoyalReserveSnapshot.Revision,
                        key);

                if (!BankRequestIsCurrent(requestVersion, client, hiveId)) return;
                bankRoyalReserveSnapshot = snapshot;
                bankRoyalReserveNotice = withdraw
                    ? "Retrait confirmé par la Banque."
                    : "Dépôt confirmé dans la Réserve Royale.";
                MobileAccountSessionRuntimeBootstrap.RefreshStocksAfterBankMutation();
            }
            catch (RoyalReserveClientException exception)
            {
                if (!BankRequestIsCurrent(requestVersion, client, hiveId)) return;
                bankRoyalReserveSnapshot = null;
                bankRoyalReserveError = BankRoyalReserveErrorMessage(exception.SafeCode);
            }
            catch (Exception)
            {
                if (!BankRequestIsCurrent(requestVersion, client, hiveId)) return;
                bankRoyalReserveSnapshot = null;
                bankRoyalReserveError = "Transaction bancaire impossible pour le moment.";
            }
            finally
            {
                if (requestVersion == bankRequestVersion) bankRoyalReserveBusy = false;
            }
        }


        private static string BankRoyalReserveErrorMessage(string code)
        {
            switch ((code ?? string.Empty).Trim())
            {
                case "game.insufficient_resources":
                    return "Ressources liquides insuffisantes pour ce dépôt.";
                case "game.bank_reserve_capacity_insufficient":
                    return "La Réserve Royale est pleine. Améliorez la Banque pour augmenter sa capacité.";
                case "game.bank_reserve_insufficient":
                    return "Le montant réservé est insuffisant pour ce retrait.";
                case "game.storage_capacity_insufficient":
                    return "Vos bâtiments de stockage n'ont pas assez de place pour recevoir ce retrait.";
                case "game.revision_conflict":
                    return "L'état de la ruche a changé. Actualisez la Réserve puis réessayez.";
                case "game.session_required":
                case "auth.session_required":
                    return "Reconnectez-vous pour utiliser la Réserve Royale.";
                case "game.invalid_request":
                    return "La transaction demandée n'est pas valide.";
                default:
                    return "Le serveur bancaire est momentanément indisponible.";
            }
        }


        private static string BankResourceLabel(string key)
        {
            switch (key)
            {
                case "honey": return "Miel";
                case "wax": return "Cire";
                case "pollen": return "Pollen";
                default: return key ?? string.Empty;
            }
        }


        private static string BankFormatAmount(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ");
        }


        private static void OpenBankRoyalReserve()
        {
            bankSelectedView = "reserve";
            bankRoyalReserveError = string.Empty;
            bankRoyalReserveNotice = string.Empty;
            RefreshBankRoyalReserveAsync();
        }


        private static void DrawBankRoyalReserve(Rect area, bool compact)
        {
            DrawActivitiesSurface(area, new Color(.070f, .045f, .024f, .985f));

            if (DrawActivitiesButton(
                new Rect(area.x + 14f, area.y + 12f, compact ? 122f : 146f, 34f),
                "Feuille de route",
                !bankRoyalReserveBusy,
                false))
            {
                bankSelectedView = "roadmap";
                bankRoyalReserveError = string.Empty;
                bankRoyalReserveNotice = string.Empty;
                return;
            }

            float reserveTitleX = area.x + (compact ? 148f : 176f);
            float reserveTitleWidth = Mathf.Max(120f, area.xMax - 150f - reserveTitleX);
            GUI.Label(
                new Rect(reserveTitleX, area.y + 10f, reserveTitleWidth, 30f),
                "RÉSERVE ROYALE",
                new GUIStyle(titleStyle)
                {
                    fontSize = compact ? 17 : 21,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .77f, .36f, 1f) }
                });

            GUI.Label(
                new Rect(area.x + 18f, area.y + 51f, area.width - 36f, 30f),
                "Mettez une partie de vos ressources à l'abri. Un dépôt retire la ressource de vos stocks liquides; un retrait la rend disponible à nouveau.",
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 9 : 11,
                    wordWrap = true,
                    normal = { textColor = new Color(.80f, .73f, .61f, 1f) }
                });

            Rect refreshRect = new Rect(area.xMax - 132f, area.y + 10f, 116f, 34f);
            if (DrawActivitiesButton(refreshRect, bankRoyalReserveBusy ? "Synchro..." : "Actualiser", !bankRoyalReserveBusy, false))
                RefreshBankRoyalReserveAsync();

            if (bankRoyalReserveSnapshot == null)
            {
                Rect loading = new Rect(area.x + 18f, area.y + 94f, area.width - 36f, 92f);
                DrawActivitiesSurface(loading, new Color(.095f, .063f, .033f, .96f));
                GUI.Label(
                    new Rect(loading.x + 16f, loading.y + 18f, loading.width - 32f, 24f),
                    bankRoyalReserveBusy ? "Synchronisation de la Réserve Royale..." :
                        string.IsNullOrEmpty(bankRoyalReserveError) ? "Réserve Royale prête à être synchronisée." : bankRoyalReserveError,
                    new GUIStyle(smallStyle)
                    {
                        fontSize = compact ? 10 : 12,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true,
                        normal = { textColor = string.IsNullOrEmpty(bankRoyalReserveError)
                            ? new Color(.82f, .75f, .62f, 1f)
                            : new Color(1f, .55f, .35f, 1f) }
                    });

                if (!bankRoyalReserveBusy &&
                    DrawActivitiesButton(
                        new Rect(loading.x + loading.width * .25f, loading.y + 52f, loading.width * .5f, 30f),
                        "Synchroniser",
                        true,
                        true))
                    RefreshBankRoyalReserveAsync();
                return;
            }

            RemoteRoyalReserveSnapshot snapshot = bankRoyalReserveSnapshot;
            Rect summary = new Rect(area.x + 18f, area.y + 88f, area.width - 36f, compact ? 92f : 82f);
            DrawActivitiesSurface(summary, new Color(.13f, .085f, .038f, .98f));

            GUI.Label(
                new Rect(summary.x + 16f, summary.y + 10f, summary.width - 32f, 22f),
                "CAPACITÉ PROTÉGÉE",
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 11 : 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .95f, .82f, 1f) }
                });

            GUI.Label(
                new Rect(summary.x + 16f, summary.y + 33f, summary.width - 32f, 20f),
                BankFormatAmount(snapshot.ReservedTotal) + " / " + BankFormatAmount(snapshot.Capacity) +
                "   ·   Banque niveau " + snapshot.BankLevel.ToString(CultureInfo.InvariantCulture),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 10 : 12,
                    normal = { textColor = new Color(.80f, .73f, .61f, 1f) }
                });

            float reserveProgress = snapshot.Capacity <= 0
                ? 0f
                : Mathf.Clamp01((float)((double)snapshot.ReservedTotal / snapshot.Capacity));
            Rect reserveTrack = new Rect(summary.x + 16f, summary.yMax - 17f, summary.width - 32f, 6f);
            GUI.color = new Color(.22f, .15f, .07f, .95f);
            GUI.DrawTexture(reserveTrack, Texture2D.whiteTexture);
            GUI.color = new Color(1f, .67f, .20f, .95f);
            GUI.DrawTexture(new Rect(reserveTrack.x, reserveTrack.y, reserveTrack.width * reserveProgress, reserveTrack.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float resourcesTop = summary.yMax + 10f;
            string[] resourceKeys = { "honey", "wax", "pollen" };
            float resourceGap = compact ? 6f : 10f;
            float resourceHeight = compact ? 72f : 78f;
            float resourceWidth = (area.width - 36f - resourceGap * 2f) / 3f;

            for (int i = 0; i < resourceKeys.Length; i++)
            {
                string key = resourceKeys[i];
                bool selected = string.Equals(bankReserveResource, key, StringComparison.Ordinal);
                Rect card = new Rect(
                    area.x + 18f + i * (resourceWidth + resourceGap),
                    resourcesTop,
                    resourceWidth,
                    resourceHeight);
                DrawActivitiesSurface(card, selected
                    ? new Color(.20f, .13f, .055f, .99f)
                    : new Color(.095f, .063f, .033f, .96f));

                DrawGameIcon(new Rect(card.x + 10f, card.y + 13f, compact ? 34f : 40f, compact ? 34f : 40f), key, Color.white);

                GUI.Label(
                    new Rect(card.x + (compact ? 50f : 58f), card.y + 9f, card.width - (compact ? 60f : 68f), 20f),
                    BankResourceLabel(key),
                    new GUIStyle(smallStyle)
                    {
                        fontSize = compact ? 10 : 12,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = selected
                            ? new Color(1f, .82f, .38f, 1f)
                            : new Color(1f, .95f, .82f, 1f) }
                    });

                long liquid = snapshot.LiquidBalances.TryGetValue(key, out RemoteHiveResourceBalance balance) && balance != null
                    ? balance.Amount
                    : 0L;
                long reserved = snapshot.Reserved.GetValueOrDefault(key);
                GUI.Label(
                    new Rect(card.x + (compact ? 50f : 58f), card.y + 31f, card.width - (compact ? 60f : 68f), 34f),
                    "Liquide : " + BankFormatAmount(liquid) + "\nRéservé : " + BankFormatAmount(reserved),
                    new GUIStyle(tinyLabelStyle)
                    {
                        fontSize = compact ? 8 : 10,
                        wordWrap = true,
                        normal = { textColor = new Color(.72f, .66f, .56f, 1f) }
                    });

                if (GUI.Button(card, GUIContent.none, GUIStyle.none))
                {
                    bankReserveResource = key;
                    bankRoyalReserveError = string.Empty;
                    bankRoyalReserveNotice = string.Empty;
                    AudioManager.Instance?.PlayUIClick();
                }
            }

            float transactionTop = resourcesTop + resourceHeight + 10f;
            Rect transaction = new Rect(
                area.x + 18f,
                transactionTop,
                area.width - 36f,
                Mathf.Max(118f, area.yMax - transactionTop - 14f));
            DrawActivitiesSurface(transaction, new Color(.085f, .055f, .028f, .98f));

            GUI.Label(
                new Rect(transaction.x + 16f, transaction.y + 10f, transaction.width - 32f, 22f),
                "TRANSACTION · " + BankResourceLabel(bankReserveResource),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 11 : 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .95f, .82f, 1f) }
                });

            float amountY = transaction.y + 38f;
            float amountLabelWidth = compact ? 62f : 78f;
            GUI.Label(
                new Rect(transaction.x + 16f, amountY + 6f, amountLabelWidth, 24f),
                "Montant",
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 9 : 11,
                    normal = { textColor = new Color(.80f, .73f, .61f, 1f) }
                });

            Rect amountRect = new Rect(
                transaction.x + 16f + amountLabelWidth,
                amountY,
                compact ? 112f : 150f,
                32f);
            string typed = GUI.TextField(
                amountRect,
                bankReserveAmountText ?? string.Empty,
                12,
                new GUIStyle(GUI.skin.textField)
                {
                    fontSize = compact ? 11 : 13,
                    alignment = TextAnchor.MiddleCenter
                });
            bankReserveAmountText = typed ?? string.Empty;

            float quickX = amountRect.xMax + 8f;
            if (DrawActivitiesButton(new Rect(quickX, amountY, 64f, 32f), "100", !bankRoyalReserveBusy, false))
                bankReserveAmountText = "100";
            quickX += 70f;
            if (DrawActivitiesButton(new Rect(quickX, amountY, 76f, 32f), "1 000", !bankRoyalReserveBusy, false))
                bankReserveAmountText = "1000";

            long selectedLiquid = snapshot.LiquidBalances.TryGetValue(bankReserveResource, out RemoteHiveResourceBalance selectedBalance) && selectedBalance != null
                ? selectedBalance.Amount
                : 0L;
            long selectedReserved = snapshot.Reserved.GetValueOrDefault(bankReserveResource);

            quickX += 82f;
            if (DrawActivitiesButton(new Rect(quickX, amountY, compact ? 86f : 104f, 32f), "MAX dépôt", !bankRoyalReserveBusy && selectedLiquid > 0, false))
                bankReserveAmountText = Math.Min(selectedLiquid, Math.Max(0L, snapshot.Capacity - snapshot.ReservedTotal)).ToString(CultureInfo.InvariantCulture);
            quickX += compact ? 92f : 110f;
            if (DrawActivitiesButton(new Rect(quickX, amountY, compact ? 86f : 104f, 32f), "MAX retrait", !bankRoyalReserveBusy && selectedReserved > 0, false))
                bankReserveAmountText = Math.Min(selectedReserved, selectedBalance == null ? 0L : Math.Max(0L, selectedBalance.Capacity - selectedBalance.Amount)).ToString(CultureInfo.InvariantCulture);

            float actionY = amountY + 43f;
            float actionGap = 10f;
            float actionWidth = (transaction.width - 32f - actionGap) * .5f;
            bool canDeposit = !bankRoyalReserveBusy && selectedLiquid > 0;
            bool canWithdraw = !bankRoyalReserveBusy && selectedReserved > 0;

            if (DrawActivitiesButton(
                new Rect(transaction.x + 16f, actionY, actionWidth, 36f),
                bankRoyalReserveBusy ? "Transaction..." : "Déposer",
                canDeposit,
                true))
                TransferBankRoyalReserveAsync(false);

            if (DrawActivitiesButton(
                new Rect(transaction.x + 16f + actionWidth + actionGap, actionY, actionWidth, 36f),
                bankRoyalReserveBusy ? "Transaction..." : "Retirer",
                canWithdraw,
                false))
                TransferBankRoyalReserveAsync(true);

            string feedback = !string.IsNullOrEmpty(bankRoyalReserveError)
                ? bankRoyalReserveError
                : bankRoyalReserveNotice;
            if (!string.IsNullOrEmpty(feedback))
            {
                GUI.Label(
                    new Rect(transaction.x + 16f, actionY + 43f, transaction.width - 32f, 34f),
                    feedback,
                    new GUIStyle(tinyLabelStyle)
                    {
                        fontSize = compact ? 8 : 10,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true,
                        normal = { textColor = !string.IsNullOrEmpty(bankRoyalReserveError)
                            ? new Color(1f, .55f, .35f, 1f)
                            : new Color(.72f, .90f, .55f, 1f) }
                    });
            }
        }


        private static void DrawBankScreen(bool compact)
        {
            Rect full = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = new Color(.035f, .022f, .012f, .995f);
            GUI.DrawTexture(full, Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (DrawActivitiesButton(new Rect(16f, 14f, 78f, 48f), "Retour", true, false))
            {
                AudioManager.Instance?.PlayUIClick();
                if (string.Equals(bankSelectedView, "reserve", StringComparison.Ordinal))
                {
                    bankSelectedView = "roadmap";
                    bankRoyalReserveError = string.Empty;
                    bankRoyalReserveNotice = string.Empty;
                }
                else
                {
                    AudioManager.Instance?.PlayMenuClose();
                    CloseBankOverlayForExternalHost();
                }
                return;
            }

            GUI.Label(new Rect(112f, 12f, Screen.width * .44f, 38f), "BANQUE DE LA RUCHE",
                new GUIStyle(titleStyle)
                {
                    fontSize = compact ? 21 : 27,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .77f, .36f, 1f) }
                });
            GUI.Label(new Rect(114f, 52f, Screen.width * .55f, 22f),
                "Le centre stratégique de la richesse de votre ruche.",
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 10 : 12,
                    normal = { textColor = new Color(.80f, .73f, .61f, 1f) }
                });

            int level = BankDisplayedLevel(out bool levelPending);
            string levelText = levelPending ? "Niveau —" : "Niveau " + level.ToString(CultureInfo.InvariantCulture);

            Rect upgrade = new Rect(Screen.width - (compact ? 156f : 184f), 14f, compact ? 140f : 168f, 48f);
            bool officialConfigured = OfficialBuildingUpgradeConfigured();
            bool upgradeEnabled = officialConfigured ? OfficialBuildingUpgradeActionEnabled("hive_bank") : true;
            string upgradeLabel = officialConfigured ? OfficialBuildingUpgradeActionLabel("hive_bank") : "Améliorer";
            if (DrawActivitiesButton(upgrade, upgradeLabel, upgradeEnabled, true) && upgradeEnabled)
            {
                AudioManager.Instance?.PlayUIClick();
                TryStartUpgradeWithPrerequisiteRedirectForExternalHost("hive_bank");
            }

            float top = 94f;
            float margin = compact ? 10f : 32f;
            Rect levelCard = new Rect(margin, top, Screen.width - margin * 2f, compact ? 96f : 88f);
            DrawActivitiesSurface(levelCard, new Color(.095f, .063f, .033f, .98f));
            GUI.Label(new Rect(levelCard.x + 18f, levelCard.y + 12f, levelCard.width - 210f, 24f),
                "BANQUE · " + levelText,
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 13 : 15,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .95f, .82f, 1f) }
                });

            BankMilestone next = levelPending ? BankMilestones[0] : NextBankMilestone(level);
            string nextText = next == null
                ? "Toutes les fonctions majeures de la première branche sont débloquées."
                : "Prochain déblocage majeur : " + next.Title + " · niveau " + next.Level.ToString(CultureInfo.InvariantCulture);
            GUI.Label(new Rect(levelCard.x + 18f, levelCard.y + 41f, levelCard.width - 36f, 20f), nextText,
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 10 : 12,
                    normal = { textColor = new Color(.80f, .73f, .61f, 1f) }
                });

            if (next != null && !levelPending)
            {
                float previousMajor = 1f;
                for (int i = 0; i < BankMilestones.Length; i++)
                {
                    if (BankMilestones[i].Level >= next.Level) break;
                    previousMajor = BankMilestones[i].Level;
                }
                float progress = Mathf.InverseLerp(previousMajor, next.Level, level);
                Rect track = new Rect(levelCard.x + 18f, levelCard.yMax - 16f, levelCard.width - 36f, 5f);
                GUI.color = new Color(.22f, .15f, .07f, .95f);
                GUI.DrawTexture(track, Texture2D.whiteTexture);
                GUI.color = new Color(1f, .67f, .20f, .95f);
                GUI.DrawTexture(new Rect(track.x, track.y, track.width * progress, track.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            float contentTop = levelCard.yMax + 14f;
            Rect roadmapCard = new Rect(margin, contentTop, Screen.width - margin * 2f, Screen.height - contentTop - 18f);
            if (string.Equals(bankSelectedView, "reserve", StringComparison.Ordinal))
            {
                DrawBankRoyalReserve(roadmapCard, compact);
                return;
            }

            DrawActivitiesSurface(roadmapCard, new Color(.070f, .045f, .024f, .985f));

            GUI.Label(new Rect(roadmapCard.x + 18f, roadmapCard.y + 12f, roadmapCard.width - 36f, 28f), "FEUILLE DE ROUTE BANCAIRE",
                new GUIStyle(titleStyle)
                {
                    fontSize = compact ? 17 : 21,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .77f, .36f, 1f) }
                });
            GUI.Label(new Rect(roadmapCard.x + 18f, roadmapCard.y + 42f, roadmapCard.width - 36f, 22f),
                "Les fonctions futures restent visibles : vous savez toujours ce que le prochain niveau peut vous apporter.",
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 9 : 11,
                    wordWrap = true,
                    normal = { textColor = new Color(.80f, .73f, .61f, 1f) }
                });

            Rect viewport = new Rect(roadmapCard.x + 12f, roadmapCard.y + 74f, roadmapCard.width - 24f, roadmapCard.height - 86f);
            float gap = compact ? 7f : 9f;
            float rowHeight = compact ? 88f : 82f;
            float contentWidth = Mathf.Max(40f, viewport.width - 18f);
            float contentHeight = BankMilestones.Length * (rowHeight + gap);
            bankRoadmapScroll.x = 0f;
            bankRoadmapScroll = GUI.BeginScrollView(viewport, bankRoadmapScroll,
                new Rect(0f, 0f, contentWidth, Mathf.Max(viewport.height, contentHeight)), false, true);

            for (int i = 0; i < BankMilestones.Length; i++)
            {
                BankMilestone milestone = BankMilestones[i];
                bool unlockedByLevel = !levelPending && level >= milestone.Level;
                bool royalReserveAvailable = unlockedByLevel && milestone.Level == 1;
                bool isNext = next != null && milestone.Level == next.Level;
                Rect row = new Rect(2f, i * (rowHeight + gap), contentWidth - 4f, rowHeight);

                Color fill = unlockedByLevel
                    ? new Color(.20f, .135f, .070f, .98f)
                    : isNext
                        ? new Color(.17f, .105f, .045f, .98f)
                        : new Color(.105f, .075f, .050f, .94f);
                DrawActivitiesSurface(row, fill);

                Rect icon = new Rect(row.x + 12f, row.y + 14f, compact ? 38f : 44f, compact ? 38f : 44f);
                DrawGameIcon(icon, milestone.IconId, unlockedByLevel ? Color.white : new Color(.62f, .58f, .52f, 1f));

                float textX = icon.xMax + 12f;
                float textWidth = Mathf.Max(80f, row.xMax - 154f - textX);
                GUI.Label(new Rect(textX, row.y + 9f, textWidth, 22f),
                    milestone.Title,
                    new GUIStyle(smallStyle)
                    {
                        fontSize = compact ? 12 : 14,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = unlockedByLevel ? new Color(1f, .95f, .82f, 1f) : new Color(.80f, .73f, .61f, 1f) }
                    });
                GUI.Label(new Rect(textX, row.y + 34f, textWidth, row.height - 40f),
                    milestone.Description,
                    new GUIStyle(smallStyle)
                    {
                        fontSize = compact ? 9 : 11,
                        wordWrap = true,
                        clipping = TextClipping.Clip,
                        normal = { textColor = new Color(.72f, .66f, .56f, 1f) }
                    });

                string status;
                if (levelPending) status = "Synchronisation";
                else if (royalReserveAvailable) status = "DISPONIBLE";
                else if (unlockedByLevel) status = "PALIER ATTEINT";
                else if (isNext) status = "PROCHAIN · NIV. " + milestone.Level.ToString(CultureInfo.InvariantCulture);
                else status = "🔒 NIV. " + milestone.Level.ToString(CultureInfo.InvariantCulture);

                GUI.Label(new Rect(row.xMax - 142f, row.y + 12f, 128f, 22f), status,
                    new GUIStyle(badgeStyle)
                    {
                        fontSize = compact ? 8 : 10,
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = unlockedByLevel ? new Color(1f, .77f, .36f, 1f) : new Color(.68f, .62f, .52f, 1f) }
                    });

                if (royalReserveAvailable)
                {
                    if (DrawActivitiesButton(
                        new Rect(row.xMax - 134f, row.y + 37f, 120f, 32f),
                        "Ouvrir",
                        !bankRoyalReserveBusy,
                        true))
                    {
                        AudioManager.Instance?.PlayUIClick();
                        OpenBankRoyalReserve();
                    }
                }
                else
                {
                    GUI.Label(new Rect(row.xMax - 142f, row.y + 38f, 128f, 32f),
                        unlockedByLevel ? "Service bancaire\nen préparation" : "Améliorez la Banque\npour débloquer",
                        new GUIStyle(tinyLabelStyle)
                        {
                            fontSize = compact ? 8 : 9,
                            alignment = TextAnchor.MiddleRight,
                            wordWrap = true,
                            normal = { textColor = new Color(.60f, .55f, .48f, 1f) }
                        });
                }
            }

            GUI.EndScrollView();
        }

 }
}
