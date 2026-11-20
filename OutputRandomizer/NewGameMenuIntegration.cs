using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OutputRandomizer
{
    internal static class NewGameMenuIntegration
    {
        private const float PanelHeight = 112f;
        private const float PanelGap = 8f;
        private const float RowHeight = 30f;
        private const string HoverDescriptionText = "Includes Mining Machine, Advanced Mining Machine, Water Pump, and Oil Extractor.";

        private static UIGalaxySelect _screen;
        private static object _gameDesc;
        private static GameObject _panel;
        private static GameObject _row;
        private static UIToggle _toggle;
        private static Button _filterButton;
        private static Text _filterLabelText;
        private static Text _filterCountText;
        private static HashSet<int> _pendingItemIds = new HashSet<int>();
        private static int _candidateItemCount;

        internal static bool PendingEnabled { get; set; } = true;
        internal static HashSet<int> PendingItemIds => _pendingItemIds;

        internal static void OnOpen(UIGalaxySelect __instance)
        {
            if (__instance == null)
                return;

            _screen = __instance;
            object gameDesc = AccessTools.Field(typeof(UIGalaxySelect), "gameDesc")?.GetValue(__instance);
            if (!ReferenceEquals(_gameDesc, gameDesc))
            {
                _gameDesc = gameDesc;
                PendingEnabled = Plugin.DefaultEnabled?.Value ?? true;
                _pendingItemIds = ItemFilterPicker.GetCandidateItemIds();
                _candidateItemCount = _pendingItemIds.Count;
            }

            EnsureTable(__instance);
            if (_toggle != null)
                _toggle.isOn = PendingEnabled;
        }

        internal static void OnClose(UIGalaxySelect __instance)
        {
            if (__instance == _screen)
                ItemFilterPicker.Close();
        }

        private static void EnsureTable(UIGalaxySelect screen)
        {
            if (_panel != null || screen.propertyRect == null || screen.propertyMultiplierText == null || screen.sandboxToggle == null)
                return;

            RectTransform metadataRect = screen.propertyRect;
            RectTransform parent = metadataRect.parent as RectTransform;
            if (parent == null)
                return;

            float panelWidth = Mathf.Max(250f, metadataRect.rect.width);
            _panel = CreatePanel(parent, metadataRect, panelWidth);
            if (_panel == null)
                return;

            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            Text headingTemplate = FindMetadataHeading(screen) ?? screen.propertyMultiplierText;
            Text heading = CreateLabel(headingTemplate, panelRect, "Randomizer", new Vector2(0f, -10f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-24f, 28f), 0);
            if (heading != null)
            {
                heading.alignment = TextAnchor.MiddleCenter;
                heading.fontSize = Mathf.Max(heading.fontSize, 16);
                heading.fontStyle = FontStyle.Bold;
            }

            _row = new GameObject("OutputRandomizer_OptionRow", typeof(RectTransform), typeof(Image), typeof(HoverDescription));
            _row.transform.SetParent(panelRect, false);
            RectTransform rowRect = _row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.offsetMin = new Vector2(8f, 42f);
            rowRect.offsetMax = new Vector2(-8f, 42f + RowHeight);
            rowRect.sizeDelta = new Vector2(0f, RowHeight);
            Image rowImage = _row.GetComponent<Image>();
            rowImage.color = new Color(1f, 1f, 1f, 0f);
            rowImage.raycastTarget = true;

            CreateLabel(screen.propertyMultiplierText, rowRect, "Random Miner Outputs", new Vector2(4f, RowHeight * 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(panelWidth - 76f, RowHeight), 0);
            _toggle = CreateToggle(screen.sandboxToggle, rowRect, new Vector2(-8f, RowHeight * 0.5f));
            if (_toggle == null)
            {
                UnityEngine.Object.Destroy(_panel);
                _panel = null;
                return;
            }

            var hover = _row.GetComponent<HoverDescription>();
            _row.transform.SetAsLastSibling();
            CreateItemFilterRow(screen, panelRect, panelWidth);
            _panel.SetActive(true);
        }

        private static void CreateItemFilterRow(UIGalaxySelect screen, RectTransform parent, float panelWidth)
        {
            // Row container (no Image, no Button — just a layout holder)
            var filterRow = new GameObject("OutputRandomizer_ItemFilterRow", typeof(RectTransform));
            filterRow.transform.SetParent(parent, false);
            RectTransform rowRect = filterRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.offsetMin = new Vector2(8f, 8f);
            rowRect.offsetMax = new Vector2(-8f, 8f + RowHeight);
            rowRect.sizeDelta = new Vector2(0f, RowHeight);

            // Button (fixed width, left-aligned)
            var btnObj = new GameObject("FilterBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(rowRect, false);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0f, 0f);
            btnRect.anchorMax = new Vector2(0f, 1f);
            btnRect.pivot = new Vector2(0f, 0.5f);
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = new Vector2(100f, 0f);

            Image btnImage = btnObj.GetComponent<Image>();
            btnImage.color = new Color(0.1f, 0.13f, 0.18f, 0.9f);
            btnImage.raycastTarget = true;

            _filterButton = btnObj.GetComponent<Button>();
            _filterButton.targetGraphic = btnImage;
            _filterButton.transition = Selectable.Transition.ColorTint;
            var colors = _filterButton.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.normalColor = Color.white;
            colors.fadeDuration = 0.1f;
            _filterButton.colors = colors;
            _filterButton.onClick.AddListener(OpenItemPicker);

            // "Filter" label inside the button
            _filterLabelText = CreateLabel(screen.propertyMultiplierText, btnRect, "Filter",
                Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero, 0);
            if (_filterLabelText != null)
            {
                _filterLabelText.alignment = TextAnchor.MiddleCenter;
                _filterLabelText.color = new Color(0.85f, 0.88f, 0.92f);
            }

            // Count label (right side of the row)
            _filterCountText = CreateLabel(screen.propertyMultiplierText, rowRect, string.Empty,
                new Vector2(-8f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(80f, RowHeight), 1);
            if (_filterCountText != null)
            {
                _filterCountText.alignment = TextAnchor.MiddleRight;
                _filterCountText.color = new Color(0.85f, 0.88f, 0.92f);
            }

            RefreshFilterControl();
        }

        private static void OpenItemPicker()
        {
            if (!PendingEnabled || _screen == null)
            return;

            // Find the Canvas that this screen renders in
            var canvas = _screen.GetComponentInParent<Canvas>();
            RectTransform pickerParent = canvas?.transform as RectTransform;

            if (pickerParent == null)
            {
                Plugin.ModLogger?.LogError("[OR] No Canvas found above UIGalaxySelect!");
                return;
            }

            ItemFilterPicker.Open(pickerParent, _screen.propertyMultiplierText,
                FindMetadataHeading(_screen) ?? _screen.propertyMultiplierText,
                _pendingItemIds ?? new HashSet<int>(), OnItemFilterChanged);
        }

        private static void OnItemFilterChanged(int itemId)
        {
            if (_pendingItemIds.Contains(itemId))
                _pendingItemIds.Remove(itemId);
            else
                _pendingItemIds.Add(itemId);

            RefreshFilterControl();
        }

        private static void RefreshFilterControl()
        {
            if (_filterButton != null)
                _filterButton.interactable = PendingEnabled;

            if (_filterCountText != null)
            {
                _filterCountText.text = _pendingItemIds.Count + "/" + _candidateItemCount;
                _filterCountText.color = PendingEnabled ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.7f);
            }

            if (_filterLabelText != null)
                _filterLabelText.color = PendingEnabled ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.7f);
        }

        private static Text FindMetadataHeading(UIGalaxySelect screen)
        {
            RectTransform metadataGroup = screen.leftGroupRect;
            if (metadataGroup == null)
                return null;

            Text[] labels = metadataGroup.GetComponentsInChildren<Text>(true);
            foreach (Text label in labels)
            {
                if (label != null && !string.IsNullOrEmpty(label.text) &&
                    label.text.IndexOf("metadata", StringComparison.OrdinalIgnoreCase) >= 0)
                    return label;
            }

            return null;
        }

        private static GameObject CreatePanel(RectTransform parent, RectTransform metadataRect, float width)
        {
            var panel = new GameObject("OutputRandomizer_OptionsTable", typeof(RectTransform));
            panel.transform.SetParent(parent, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(width, PanelHeight);

            var corners = new Vector3[4];
            metadataRect.GetWorldCorners(corners);
            float bottom = float.PositiveInfinity;
            float centerX = 0f;
            for (int index = 0; index < corners.Length; index++)
            {
                Vector3 localCorner = parent.InverseTransformPoint(corners[index]);
                bottom = Mathf.Min(bottom, localCorner.y);
                centerX += localCorner.x * 0.25f;
            }
            panelRect.anchoredPosition = new Vector2(centerX, bottom - PanelGap - PanelHeight * 0.5f);
            panel.transform.SetAsLastSibling();

            return panel;
        }

        private static Text CreateLabel(Text template, RectTransform parent, string value, Vector2 position, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, int siblingIndex)
        {
            GameObject labelObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            labelObject.name = "OutputRandomizer_Label_" + value.Replace(' ', '_');
            Text label = labelObject.GetComponent<Text>();
            RectTransform rectTransform = labelObject.transform as RectTransform;
            if (label == null || rectTransform == null)
            {
                UnityEngine.Object.Destroy(labelObject);
                return null;
            }

            label.text = value;
            label.raycastTarget = false;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = anchorMin;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.SetSiblingIndex(siblingIndex);
            return label;
        }

        private static UIToggle CreateToggle(UIToggle template, RectTransform parent, Vector2 position)
        {
            GameObject toggleObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            toggleObject.name = "OutputRandomizer_RandomMinerOutputsToggle";
            UIToggle toggle = toggleObject.GetComponent<UIToggle>();
            RectTransform rectTransform = toggleObject.transform as RectTransform;
            if (toggle == null || rectTransform == null || toggle.toggle == null)
            {
                UnityEngine.Object.Destroy(toggleObject);
                return null;
            }

            rectTransform.anchorMin = new Vector2(1f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.pivot = new Vector2(1f, 0.5f);
            rectTransform.anchoredPosition = position;
            toggle.toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.toggle.onValueChanged.AddListener(OnToggleChanged);
            toggle.isOn = PendingEnabled;
            return toggle;
        }

        private static void OnToggleChanged(bool value)
        {
            PendingEnabled = value;
            RefreshFilterControl();
        }

        private sealed class HoverDescription : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private UIButtonTip _tooltip;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (_tooltip != null || UIRoot.instance == null || UIRoot.instance.buttonTopTipTransform == null)
                    return;

                _tooltip = UIButtonTip.Create(
                    true,
                    string.Empty,
                    HoverDescriptionText,
                    5,
                    Vector2.zero,
                    320,
                    transform,
                    string.Empty,
                    string.Empty);
                PositionTooltipNearPointer(eventData);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (_tooltip == null)
                    return;

                UnityEngine.Object.Destroy(_tooltip.gameObject);
                _tooltip = null;
            }

            private void PositionTooltipNearPointer(PointerEventData eventData)
            {
                if (_tooltip == null)
                    return;

                RectTransform tooltipRect = _tooltip.transform as RectTransform;
                RectTransform parentRect = tooltipRect != null ? tooltipRect.parent as RectTransform : null;
                if (tooltipRect == null || parentRect == null)
                    return;

                Canvas canvas = parentRect.GetComponentInParent<Canvas>();
                Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventCamera, out Vector2 pointerPosition))
                    return;

                tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
                tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
                tooltipRect.pivot = new Vector2(0.5f, 0.5f);

                Rect bounds = parentRect.rect;
                Vector2 halfSize = tooltipRect.rect.size * 0.5f;
                Vector2 desiredCenter = pointerPosition + new Vector2(halfSize.x + 16f, -halfSize.y - 12f);
                float minX = bounds.xMin + halfSize.x + 8f;
                float maxX = bounds.xMax - halfSize.x - 8f;
                float minY = bounds.yMin + halfSize.y + 8f;
                float maxY = bounds.yMax - halfSize.y - 8f;

                if (desiredCenter.x > maxX)
                    desiredCenter.x = pointerPosition.x - halfSize.x - 16f;

                desiredCenter.x = Mathf.Clamp(desiredCenter.x, minX, maxX);
                desiredCenter.y = Mathf.Clamp(desiredCenter.y, minY, maxY);
                tooltipRect.anchoredPosition = desiredCenter - bounds.center;
            }
        }
    }
}
