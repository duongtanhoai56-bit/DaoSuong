using System;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public sealed class CampaignHUD : MonoBehaviour
    {
        private CampaignManager campaign;
        private ArchipelagoManager archipelago;
        private BaseBuildingManager buildingManager;
        private CombatController combat;
        private Text statusText;
        private Text bossStatusText;
        private Text overlayText;
        private GameObject overlayPanel;
        private GameObject buildPanel;
        private GameObject islandPanel;
        private Canvas canvas;
        private Transform safeAreaRoot;
        private readonly Dictionary<string, Button> islandButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        private float nextRefresh;

        private void Start()
        {
            canvas = GetComponent<Canvas>();
            safeAreaRoot = SafeAreaFitter.GetOrCreate(transform);
            if (safeAreaRoot == null) safeAreaRoot = transform;
            campaign = FindFirstObjectByType<CampaignManager>();
            archipelago = FindFirstObjectByType<ArchipelagoManager>();
            buildingManager = FindFirstObjectByType<BaseBuildingManager>();
            combat = FindFirstObjectByType<CombatController>();
            BuildInterface();
            if (campaign != null)
            {
                campaign.ProgressChanged += OnProgressChanged;
                campaign.Won += ShowVictory;
                campaign.Lost += ShowDefeat;
            }
            RefreshStatus();
            RefreshIslandButtons();
            RefreshBossStatus();
            if (campaign != null && campaign.HasWon) ShowVictory();
            else if (campaign != null && campaign.HasLost) ShowDefeat();
            else Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            GameplayInputGate.Release(this);
            if (campaign == null) return;
            campaign.ProgressChanged -= OnProgressChanged;
            campaign.Won -= ShowVictory;
            campaign.Lost -= ShowDefeat;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.5f;
            RefreshStatus();
            RefreshIslandButtons();
            RefreshBossStatus();
        }

        private void BuildInterface()
        {
            statusText = CreateText("CampaignStatus", safeAreaRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -28f), new Vector2(440f, 90f), 27, TextAnchor.UpperLeft, Color.white);
            bossStatusText = CreateText("BossStatus", safeAreaRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -28f), new Vector2(680f, 55f), 25, TextAnchor.UpperCenter, new Color(1f, 0.78f, 0.35f));
            CreateButton("AttackButton", safeAreaRoot, "TẤN CÔNG", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-175f, 155f), new Vector2(250f, 100f), new Color(0.65f, 0.15f, 0.12f, 0.94f),
                () => { if (combat != null) combat.TryAttack(); });

            CreateButton("BuildToggle", safeAreaRoot, "XÂY DỰNG", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-175f, 275f), new Vector2(250f, 86f), new Color(0.22f, 0.42f, 0.28f, 0.94f),
                ToggleBuildPanel);
            CreateButton("IslandToggle", safeAreaRoot, "CÁC ĐẢO", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-175f, 380f), new Vector2(250f, 86f), new Color(0.16f, 0.32f, 0.52f, 0.94f),
                ToggleIslandPanel);

            buildPanel = CreatePanel("BuildPanel", safeAreaRoot, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-455f, 280f), new Vector2(300f, 600f));
            islandPanel = CreatePanel("IslandPanel", safeAreaRoot, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-455f, 280f), new Vector2(340f, 600f));
            PopulateBuildPanel();
            PopulateIslandPanel();
            buildPanel.SetActive(false);
            islandPanel.SetActive(false);

            overlayPanel = CreateCenteredPanel("CampaignEndPanel", safeAreaRoot, new Vector2(900f, 330f));
            overlayText = CreateText("CampaignEndTitle", overlayPanel.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, 48, TextAnchor.MiddleCenter, Color.white);
            overlayText.rectTransform.offsetMin = new Vector2(28f, 105f);
            overlayText.rectTransform.offsetMax = new Vector2(-28f, -28f);
            CreateButton("CampaignRestart", overlayPanel.transform, "CHƠI LẠI",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f),
                new Vector2(300f, 68f), new Color(0.16f, 0.48f, 0.3f, 0.98f), RestartCampaign, 24);
            overlayPanel.SetActive(false);
        }

        private void PopulateBuildPanel()
        {
            if (buildingManager == null || buildingManager.Catalog == null || buildingManager.Catalog.pieces == null) return;
            CreateText("BuildTitle", buildPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -8f), new Vector2(0f, 50f), 25, TextAnchor.MiddleCenter, Color.white);
            Text title = buildPanel.transform.Find("BuildTitle").GetComponent<Text>();
            title.text = "CHỌN CÔNG TRÌNH";
            int count = Mathf.Min(8, buildingManager.Catalog.pieces.Length);
            for (int i = 0; i < count; i++)
            {
                BuildingDefinition definition = buildingManager.Catalog.pieces[i];
                if (definition == null) continue;
                BuildingDefinition captured = definition;
                string costText = BuildCostText(captured);
                CreateButton("Build_" + captured.id, buildPanel.transform,
                    captured.displayName + "\n" + costText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -82f - i * 64f), new Vector2(270f, 58f),
                    new Color(0.22f, 0.31f, 0.25f, 0.96f), () =>
                    {
                        if (buildingManager != null && buildingManager.TryBuild(captured))
                        {
                            if (buildPanel != null) buildPanel.SetActive(false);
                            GameplayInputGate.Release(this);
                        }
                    }, 17);
            }
        }

        private void PopulateIslandPanel()
        {
            if (archipelago == null || archipelago.Catalog == null || archipelago.Catalog.islands == null) return;
            CreateText("IslandTitle", islandPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -8f), new Vector2(0f, 50f), 25, TextAnchor.MiddleCenter, Color.white).text = "CHỌN HÒN ĐẢO";
            islandButtons.Clear();
            int count = Mathf.Min(8, archipelago.Catalog.islands.Length);
            for (int i = 0; i < count; i++)
            {
                IslandDestination destination = archipelago.Catalog.islands[i];
                if (destination == null || string.IsNullOrWhiteSpace(destination.id)) continue;
                IslandDestination captured = destination;
                Button button = CreateButton("Island_" + captured.id, islandPanel.transform, captured.displayName,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f - i * 68f),
                    new Vector2(300f, 62f), new Color(0.22f, 0.22f, 0.24f, 0.9f),
                    () =>
                    {
                        if (archipelago != null && archipelago.TryTravel(captured.id))
                        {
                            if (islandPanel != null) islandPanel.SetActive(false);
                            GameplayInputGate.Release(this);
                        }
                    }, 16);
                islandButtons[captured.id] = button;
            }
            RefreshIslandButtons();
        }

        private void RefreshStatus()
        {
            if (statusText == null) return;
            int days = campaign != null ? campaign.SurvivedDays : 0;
            int required = campaign != null ? campaign.RequiredDays : 99;
            int bossCount = campaign != null ? campaign.DefeatedBossIds.Count : 0;
            int totalBosses = 0;
            if (archipelago != null && archipelago.Catalog != null && archipelago.Catalog.islands != null)
                for (int i = 0; i < archipelago.Catalog.islands.Length; i++)
                    if (archipelago.Catalog.islands[i] != null && archipelago.Catalog.islands[i].boss != null) totalBosses++;
            string islandName = "";
            if (archipelago != null && archipelago.Catalog != null)
            {
                IslandDestination current = archipelago.Catalog.Find(archipelago.ActiveIslandId);
                if (current != null) islandName = "\nĐảo: " + current.displayName;
            }
            statusText.text = "NGÀY SINH TỒN: " + days + "/" + required + islandName +
                "\nBOSS ĐÃ HẠ: " + bossCount + "/" + totalBosses;
        }

        private void OnProgressChanged(int days, int required) => RefreshStatus();
        private void ShowVictory() => ShowEnd("CHIẾN THẮNG!\nBẠN ĐÃ SỐNG SÓT 99 NGÀY", new Color(0.15f, 0.85f, 0.5f));
        private void ShowDefeat() => ShowEnd("THẤT BẠI\nBẠN ĐÃ GỤC NGÃ", new Color(1f, 0.35f, 0.28f));

        private void ShowEnd(string message, Color color)
        {
            if (overlayText == null || overlayPanel == null) return;
            if (buildPanel != null) buildPanel.SetActive(false);
            if (islandPanel != null) islandPanel.SetActive(false);
            GameplayInputGate.Release(this);
            overlayText.text = message;
            overlayText.color = color;
            overlayPanel.SetActive(true);
            overlayPanel.transform.SetAsLastSibling();
            Time.timeScale = 0f;
        }

        private void ToggleBuildPanel()
        {
            if (buildPanel == null) return;
            if (campaign != null && campaign.IsFinished) return;
            bool opening = !buildPanel.activeSelf;
            if (opening && GameplayInputGate.IsLockedByOther(this))
            {
                HudMessages.Toast("Hãy đóng cửa sổ khác trước khi xây dựng.");
                return;
            }
            if (islandPanel != null) islandPanel.SetActive(false);
            buildPanel.SetActive(opening);
            if (opening) GameplayInputGate.Acquire(this);
            else GameplayInputGate.Release(this);
        }

        private void ToggleIslandPanel()
        {
            if (islandPanel == null) return;
            if (campaign != null && campaign.IsFinished) return;
            bool opening = !islandPanel.activeSelf;
            if (opening && GameplayInputGate.IsLockedByOther(this))
            {
                HudMessages.Toast("Hãy đóng cửa sổ khác trước khi di chuyển đảo.");
                return;
            }
            if (buildPanel != null) buildPanel.SetActive(false);
            islandPanel.SetActive(opening);
            if (opening) GameplayInputGate.Acquire(this);
            else GameplayInputGate.Release(this);
        }

        private void RestartCampaign()
        {
            GameplayInputGate.Release(this);
            Time.timeScale = 1f;
            if (campaign != null)
            {
                campaign.ResetRun();
                if (campaign.Clock != null) campaign.Clock.SetTime01(0.28f);
            }
            SurvivalController survival = FindFirstObjectByType<SurvivalController>();
            if (survival != null) survival.RestoreForDebug();
            if (buildingManager == null) buildingManager = FindFirstObjectByType<BaseBuildingManager>();
            if (buildingManager != null) buildingManager.ResetForNewRun();
            if (archipelago != null) archipelago.ResetForNewRun();
            if (overlayPanel != null) overlayPanel.SetActive(false);
            RefreshStatus();
            RefreshIslandButtons();
            RefreshBossStatus();
        }

        private void RefreshIslandButtons()
        {
            if (archipelago == null || archipelago.Catalog == null) return;
            foreach (KeyValuePair<string, Button> pair in islandButtons)
            {
                Button button = pair.Value;
                if (button == null) continue;
                IslandDestination destination = archipelago.Catalog.Find(pair.Key);
                if (destination == null) continue;
                bool unlocked = archipelago.IsUnlocked(destination);
                bool current = archipelago.ActiveIslandId == destination.id;
                button.interactable = unlocked && !current && Time.timeScale > 0f;
                Image image = button.GetComponent<Image>();
                if (image != null) image.color = current ? new Color(0.17f, 0.48f, 0.3f, 0.98f)
                    : unlocked ? new Color(0.15f, 0.3f, 0.48f, 0.96f) : new Color(0.22f, 0.22f, 0.24f, 0.9f);
                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    string gate = string.IsNullOrWhiteSpace(destination.requiredBossId)
                        ? "Mở ngày " + destination.unlockDay : "Hạ boss: " + destination.requiredBossId;
                    label.text = (current ? "ĐANG Ở: " : unlocked ? "ĐẾN " : "KHÓA ") + destination.displayName + "\n" + gate;
                }
            }
        }

        private void RefreshBossStatus()
        {
            if (bossStatusText == null) return;
            CreatureAgent[] agents = FindObjectsByType<CreatureAgent>(FindObjectsSortMode.None);
            IslandGenerator playerIsland = null;
            if (archipelago != null && archipelago.Player != null)
                playerIsland = archipelago.GetIslandAt(archipelago.Player.position);
            CreatureAgent nearestBoss = null;
            float nearestDistance = float.PositiveInfinity;
            Vector3 playerPosition = archipelago != null && archipelago.Player != null ? archipelago.Player.position : transform.position;
            for (int i = 0; i < agents.Length; i++)
            {
                CreatureAgent candidate = agents[i];
                if (candidate == null || !candidate.IsBoss || candidate.IsDead) continue;
                if (archipelago != null && playerIsland != null && archipelago.GetIslandAt(candidate.transform.position) != playerIsland) continue;
                float distance = (candidate.transform.position - playerPosition).sqrMagnitude;
                if (distance < nearestDistance) { nearestDistance = distance; nearestBoss = candidate; }
            }
            if (nearestBoss == null)
                bossStatusText.text = "BOSS: Chưa xuất hiện trong khu vực";
            else
                bossStatusText.text = "BOSS: " + nearestBoss.DisplayName + "  |  HP " +
                    Mathf.CeilToInt(nearestBoss.Health) + "/" + Mathf.CeilToInt(nearestBoss.MaxHealth) +
                    " (" + Mathf.RoundToInt(nearestBoss.Health01 * 100f) + "%)";
        }

        private static GameObject CreateCenteredPanel(string name, Transform parent, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
            panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.94f);
            return panel;
        }

        private static string BuildCostText(BuildingDefinition definition)
        {
            if (definition.costs == null || definition.costs.Length == 0) return "Miễn phí";
            string result = "";
            for (int i = 0; i < definition.costs.Length; i++)
            {
                BuildCost cost = definition.costs[i];
                if (cost == null || cost.item == null) continue;
                if (result.Length > 0) result += "  ";
                result += cost.amount + " " + cost.item.displayName;
            }
            return result;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = anchoredPosition; rect.sizeDelta = size;
            panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 0.94f);
            return panel;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPosition; rect.sizeDelta = size;
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize; text.alignment = alignment; text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
            Color background, Action callback, int fontSize = 20)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition; rect.sizeDelta = size;
            Image image = go.GetComponent<Image>(); image.color = background;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (callback != null) button.onClick.AddListener(() => callback());
            Text text = CreateText("Label", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                fontSize, TextAnchor.MiddleCenter, Color.white);
            text.rectTransform.offsetMin = new Vector2(6f, 4f);
            text.rectTransform.offsetMax = new Vector2(-6f, -4f);
            text.text = label;
            return button;
        }
    }
}
