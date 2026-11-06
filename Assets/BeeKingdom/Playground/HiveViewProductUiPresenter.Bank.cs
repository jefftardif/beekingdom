using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeeKingdom.Audio;
using BeeKingdom.Networking;
using BeeKingdom.Localization;
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


        private static string BankText(string key) => BeeLocalization.Text("bank.premium." + key);
        private static string BankLocalizedFeedback(string message) =>
            !string.IsNullOrEmpty(message) && message.StartsWith("bank.premium.", StringComparison.Ordinal)
                ? BeeLocalization.Text(message)
                : (message ?? string.Empty);

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
            new BankMilestone(1,  "milestone.1.title", "milestone.1.description", "royal-jelly"),
            new BankMilestone(3,  "milestone.3.title", "milestone.3.description", "capacity"),
            new BankMilestone(5,  "milestone.5.title", "milestone.5.description", "wax"),
            new BankMilestone(10, "milestone.10.title", "milestone.10.description", "resources"),
            new BankMilestone(12, "milestone.12.title", "milestone.12.description", "shield"),
            new BankMilestone(15, "milestone.15.title", "milestone.15.description", "production"),
            new BankMilestone(20, "milestone.20.title", "milestone.20.description", "queen-core"),
            new BankMilestone(23, "milestone.23.title", "milestone.23.description", "alliance-center"),
            new BankMilestone(25, "milestone.25.title", "milestone.25.description", "book"),
            new BankMilestone(29, "milestone.29.title", "milestone.29.description", "events"),
            new BankMilestone(30, "milestone.30.title", "milestone.30.description", "queen")
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
            bankNextReserveRefreshAt = 0f;
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
                bankRoyalReserveError = "bank.premium.error.server_required";
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
                bankRoyalReserveError = "bank.premium.error.sync_failed";
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
                bankRoyalReserveError = "bank.premium.error.sync_first";
                return;
            }

            string normalizedAmount = (bankReserveAmountText ?? string.Empty).Replace(" ", string.Empty).Replace("\u00a0", string.Empty);
            if (!long.TryParse(normalizedAmount, NumberStyles.None, CultureInfo.InvariantCulture, out long amount) || amount <= 0)
            {
                bankRoyalReserveError = "bank.premium.error.invalid_amount";
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
                    ? "bank.premium.notice.withdraw"
                     : "bank.premium.notice.deposit";
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
                bankRoyalReserveError = "bank.premium.error.transaction_failed";
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
                    return "bank.premium.error.insufficient_resources";
                case "game.bank_reserve_capacity_insufficient":
                    return "bank.premium.error.reserve_full";
                case "game.bank_reserve_insufficient":
                    return "bank.premium.error.reserved_insufficient";
                case "game.storage_capacity_insufficient":
                    return "bank.premium.error.storage_full";
                case "game.revision_conflict":
                    return "bank.premium.error.revision_conflict";
                case "game.session_required":
                case "auth.session_required":
                    return "bank.premium.error.session_required";
                case "game.invalid_request":
                    return "bank.premium.error.invalid_request";
                default:
                    return "bank.premium.error.server_unavailable";
            }
        }


        private static string BankResourceLabel(string key)
        {
            switch (key)
            {
                case "honey": return BankText("resource.honey");
                case "wax": return BankText("resource.wax");
                case "pollen": return BankText("resource.pollen");
                default: return key ?? string.Empty;
            }
        }


        private static string BankFormatAmount(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ");
        }


        private static float bankNextReserveRefreshAt;

        public static void TickBankOverlayForExternalHost()
        {
            if (!bankOverlayOpenForExternalHost
                || !string.Equals(bankSelectedView, "reserve", StringComparison.Ordinal)
                || bankRoyalReserveBusy || Time.unscaledTime < bankNextReserveRefreshAt)
                return;
            bankNextReserveRefreshAt = Time.unscaledTime + 30f;
            RefreshBankRoyalReserveAsync();
        }

        private static void OpenBankRoyalReserve()
        {
            bankSelectedView = "reserve";
            bankNextReserveRefreshAt = Time.unscaledTime + 30f;
            bankRoyalReserveError = string.Empty;
            bankRoyalReserveNotice = string.Empty;
            RefreshBankRoyalReserveAsync();
        }


        private static void DrawBankRoyalReserve(Rect area, bool compact)
        {
            DrawActivitiesSurface(area, new Color(.070f, .045f, .024f, .985f));

            if (DrawActivitiesButton(
                new Rect(area.x + 14f, area.y + 12f, compact ? 122f : 146f, 34f),
                BankText("reserve.roadmap_tab"),
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
                BankText("reserve.title"),
                new GUIStyle(titleStyle)
                {
                    fontSize = compact ? 19 : 23,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .77f, .36f, 1f) }
                });

            GUI.Label(
                new Rect(area.x + 18f, area.y + 51f, area.width - 36f, 48f),
                BankText("reserve.description"),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 13 : 15,
                    wordWrap = true,
                    normal = { textColor = new Color(.80f, .73f, .61f, 1f) }
                });

            // The Bank runtime automatically refreshes server balances.

            if (bankRoyalReserveSnapshot == null)
            {
                Rect loading = new Rect(area.x + 18f, area.y + 94f, area.width - 36f, 92f);
                DrawActivitiesSurface(loading, new Color(.095f, .063f, .033f, .96f));
                GUI.Label(
                    new Rect(loading.x + 16f, loading.y + 18f, loading.width - 32f, 24f),
                    bankRoyalReserveBusy ? BankText("reserve.loading") :
                        string.IsNullOrEmpty(bankRoyalReserveError) ? BankText("reserve.ready") : BankLocalizedFeedback(bankRoyalReserveError),
                    new GUIStyle(smallStyle)
                    {
                        fontSize = compact ? 14 : 16,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true,
                        normal = { textColor = string.IsNullOrEmpty(bankRoyalReserveError)
                            ? new Color(.82f, .75f, .62f, 1f)
                            : new Color(1f, .55f, .35f, 1f) }
                    });

                if (!bankRoyalReserveBusy &&
                    DrawActivitiesButton(
                        new Rect(loading.x + loading.width * .25f, loading.y + 52f, loading.width * .5f, 30f),
                        BankText("reserve.sync"),
                        true,
                        true))
                    RefreshBankRoyalReserveAsync();
                return;
            }

            RemoteRoyalReserveSnapshot snapshot = bankRoyalReserveSnapshot;
            Rect summary = new Rect(area.x + 18f, area.y + 107f, area.width - 36f, compact ? 92f : 82f);
            DrawActivitiesSurface(summary, new Color(.13f, .085f, .038f, .98f));

            GUI.Label(
                new Rect(summary.x + 16f, summary.y + 10f, summary.width - 32f, 22f),
                BankText("reserve.capacity"),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 15 : 17,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .95f, .82f, 1f) }
                });

            GUI.Label(
                new Rect(summary.x + 16f, summary.y + 33f, summary.width - 32f, 20f),
                BankFormatAmount(snapshot.ReservedTotal) + " / " + BankFormatAmount(snapshot.Capacity) +
                BankText("reserve.level") + snapshot.BankLevel.ToString(CultureInfo.InvariantCulture),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 14 : 16,
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
                        fontSize = compact ? 14 : 16,
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
                    BankText("reserve.liquid") + BankFormatAmount(liquid) + "\n" + BankText("reserve.reserved") + BankFormatAmount(reserved),
                    new GUIStyle(tinyLabelStyle)
                    {
                        fontSize = compact ? 12 : 14,
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
                BankText("reserve.transaction") + BankResourceLabel(bankReserveResource),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 15 : 17,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .95f, .82f, 1f) }
                });

            float amountY = transaction.y + 38f;
            float amountLabelWidth = compact ? 62f : 78f;
            GUI.Label(
                new Rect(transaction.x + 16f, amountY + 6f, amountLabelWidth, 24f),
                BankText("reserve.amount"),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 14 : 16,
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
                    fontSize = compact ? 15 : 17,
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
            if (DrawActivitiesButton(new Rect(quickX, amountY, compact ? 86f : 104f, 32f), BankText("reserve.max_deposit"), !bankRoyalReserveBusy && selectedLiquid > 0, false))
                bankReserveAmountText = Math.Min(selectedLiquid, Math.Max(0L, snapshot.Capacity - snapshot.ReservedTotal)).ToString(CultureInfo.InvariantCulture);
            quickX += compact ? 92f : 110f;
            if (DrawActivitiesButton(new Rect(quickX, amountY, compact ? 86f : 104f, 32f), BankText("reserve.max_withdraw"), !bankRoyalReserveBusy && selectedReserved > 0, false))
                bankReserveAmountText = Math.Min(selectedReserved, selectedBalance == null ? 0L : Math.Max(0L, selectedBalance.Capacity - selectedBalance.Amount)).ToString(CultureInfo.InvariantCulture);

            float actionY = amountY + 43f;
            float actionGap = 10f;
            float actionWidth = (transaction.width - 32f - actionGap) * .5f;
            bool canDeposit = !bankRoyalReserveBusy && selectedLiquid > 0;
            bool canWithdraw = !bankRoyalReserveBusy && selectedReserved > 0;

            if (DrawActivitiesButton(
                new Rect(transaction.x + 16f, actionY, actionWidth, 36f),
                bankRoyalReserveBusy ? BankText("reserve.in_progress") : BankText("reserve.deposit"),
                canDeposit,
                true))
                TransferBankRoyalReserveAsync(false);

            if (DrawActivitiesButton(
                new Rect(transaction.x + 16f + actionWidth + actionGap, actionY, actionWidth, 36f),
                bankRoyalReserveBusy ? BankText("reserve.in_progress") : BankText("reserve.withdraw"),
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
                    BankLocalizedFeedback(feedback),
                    new GUIStyle(tinyLabelStyle)
                    {
                        fontSize = compact ? 12 : 14,
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

            if (DrawActivitiesButton(new Rect(16f, 14f, 78f, 48f), BankText("header.back"), true, false))
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

            GUI.Label(new Rect(112f, 12f, Mathf.Max(100f, PremiumBuildingUpgradeRectForProof(Screen.width, compact).x - 123f), 38f), BankText("header.title"),
                new GUIStyle(titleStyle)
                {
                    fontSize = compact ? 23 : 29,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .77f, .36f, 1f) }
                });
            GUI.Label(new Rect(114f, 52f, Screen.width * .55f, 22f),
                BankText("header.subtitle"),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 14 : 16,
                    normal = { textColor = new Color(.80f, .73f, .61f, 1f) }
                });

            int level = BankDisplayedLevel(out bool levelPending);
            string levelText = levelPending ? BankText("level.pending") : BankText("level.label") + level.ToString(CultureInfo.InvariantCulture);

            DrawPremiumBuildingUpgradeHeader("hive_bank", compact);

            float top = 94f;
            float margin = compact ? 10f : 32f;
            Rect levelCard = new Rect(margin, top, Screen.width - margin * 2f, compact ? 96f : 88f);
            DrawActivitiesSurface(levelCard, new Color(.095f, .063f, .033f, .98f));
            GUI.Label(new Rect(levelCard.x + 18f, levelCard.y + 12f, levelCard.width - 210f, 24f),
                BankText("level.bank") + levelText,
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 17 : 19,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .95f, .82f, 1f) }
                });

            BankMilestone next = levelPending ? BankMilestones[0] : NextBankMilestone(level);
            string nextText = next == null
                ? BankText("level.all_unlocked")
                : BankText("level.next") + BankText(next.Title) + BankText("level.suffix") + next.Level.ToString(CultureInfo.InvariantCulture);
            GUI.Label(new Rect(levelCard.x + 18f, levelCard.y + 41f, levelCard.width - 36f, 20f), nextText,
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 14 : 16,
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

            GUI.Label(new Rect(roadmapCard.x + 18f, roadmapCard.y + 12f, roadmapCard.width - 36f, 28f), BankText("roadmap.heading"),
                new GUIStyle(titleStyle)
                {
                    fontSize = compact ? 19 : 23,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, .77f, .36f, 1f) }
                });
            GUI.Label(new Rect(roadmapCard.x + 18f, roadmapCard.y + 42f, roadmapCard.width - 36f, 33f),
                BankText("roadmap.description"),
                new GUIStyle(smallStyle)
                {
                    fontSize = compact ? 14 : 16,
                    wordWrap = true,
                    normal = { textColor = new Color(.80f, .73f, .61f, 1f) }
                });

            Rect viewport = new Rect(roadmapCard.x + 12f, roadmapCard.y + 86f, roadmapCard.width - 24f, roadmapCard.height - 98f);
            float gap = compact ? 10f : 12f;
            float rowHeight = compact ? 104f : 100f;
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
                float textWidth = Mathf.Max(80f, row.xMax - 172f - textX);
                GUI.Label(new Rect(textX, row.y + 11f, textWidth, 27f),
                    BankText(milestone.Title),
                    new GUIStyle(smallStyle)
                    {
                        fontSize = compact ? 16 : 18,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = unlockedByLevel ? new Color(1f, .95f, .82f, 1f) : new Color(.80f, .73f, .61f, 1f) }
                    });
                GUI.Label(new Rect(textX, row.y + 42f, textWidth, row.height - 47f),
                    BankText(milestone.Description),
                    new GUIStyle(smallStyle)
                    {
                        fontSize = compact ? 17 : 19,
                        wordWrap = true,
                        clipping = TextClipping.Clip,
                        normal = { textColor = new Color(.72f, .66f, .56f, 1f) }
                    });

                string status;
                if (levelPending) status = BankText("status.synchronizing");
                else if (royalReserveAvailable) status = BankText("status.available");
                else if (unlockedByLevel) status = BankText("status.reached");
                else if (isNext) status = BankText("status.next") + milestone.Level.ToString(CultureInfo.InvariantCulture);
                else status = BankText("status.locked") + milestone.Level.ToString(CultureInfo.InvariantCulture);

                GUI.Label(new Rect(row.xMax - 160f, row.y + 11f, 146f, 26f), status,
                    new GUIStyle(badgeStyle)
                    {
                        fontSize = compact ? 12 : 14,
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = unlockedByLevel ? new Color(1f, .77f, .36f, 1f) : new Color(.68f, .62f, .52f, 1f) }
                    });

                if (royalReserveAvailable)
                {
                    if (DrawActivitiesButton(
                        new Rect(row.xMax - 152f, row.y + 44f, 138f, 40f),
                        BankText("roadmap.open"),
                        !bankRoyalReserveBusy,
                        true))
                    {
                        AudioManager.Instance?.PlayUIClick();
                        OpenBankRoyalReserve();
                    }
                }
                else
                {
                    GUI.Label(new Rect(row.xMax - 160f, row.y + 44f, 146f, 46f),
                        unlockedByLevel ? BankText("roadmap.preparing") : BankText("roadmap.unlock"),
                        new GUIStyle(tinyLabelStyle)
                        {
                            fontSize = compact ? 12 : 14,
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
