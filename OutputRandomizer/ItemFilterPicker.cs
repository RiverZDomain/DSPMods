using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OutputRandomizer
{
    internal static class ItemFilterPicker
    {
        private const int Columns = 12;
        private const float CellSize = 46f;

        private static GameObject _window;
        private static RectTransform _content;
        private static Text _bodyTextTemplate;
        private static Text _headingTemplate;
        private static HashSet<int> _selectedIds;
        private static Action<int> _onToggle;
        private static int _activeTab;

        internal static HashSet<int> GetCandidateItemIds()
        {
            return new HashSet<int>(GetCandidateItems().Select(item => item.ID));
        }

        internal static void Open(RectTransform parent, Text bodyTextTemplate, Text headingTemplate, HashSet<int> selectedIds, Action<int> onToggle)
        {
            Plugin.ModLogger?.LogInfo("Opening ItemFilterPicker window.");
            if (parent == null || bodyTextTemplate == null || headingTemplate == null || selectedIds == null || onToggle == null)
                return;

            Close();
            _bodyTextTemplate = bodyTextTemplate;
            _headingTemplate = headingTemplate;
            _selectedIds = selectedIds;
            _onToggle = onToggle;
            _activeTab = 0;
            BuildWindow(parent);
            RebuildGrid();
        }

        internal static void Close()
        {
            if (_window != null)
                UnityEngine.Object.Destroy(_window);

            _window = null;
            _content = null;
            _selectedIds = null;
            _onToggle = null;
        }

        private static void BuildWindow(RectTransform parent)
        {
            _window = new GameObject("OutputRandomizer_ItemFilter", typeof(RectTransform), typeof(Image));
            _window.transform.SetParent(parent, false);
            _window.transform.SetAsLastSibling();
            RectTransform windowRect = _window.GetComponent<RectTransform>();
            Stretch(windowRect);
            Image backdrop = _window.GetComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.55f);
            backdrop.raycastTarget = true;

            // Border (slightly larger, behind dialog)
            var borderObj = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(_window.transform, false);
            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.pivot = new Vector2(0.5f, 0.5f);
            borderRect.sizeDelta = new Vector2(652f, 502f);
            borderObj.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.28f, 0.8f);

            // Dialog
            GameObject dialog = new GameObject("Dialog", typeof(RectTransform), typeof(Image));
            dialog.transform.SetParent(_window.transform, false);
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(650f, 500f);
            dialog.GetComponent<Image>().color = new Color(0.04f, 0.055f, 0.075f, 0.96f);
            CreateText(dialogRect, _headingTemplate, "Random Miner Output Filter",
                Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, 30f), TextAnchor.MiddleCenter, 18);
            CreateButton(dialogRect, "Close",
                new Vector2(-8f, 8f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(60f, 26f), Close);
            // Tabs
            Transform tabs = new GameObject("Tabs", typeof(RectTransform)).transform;
            tabs.SetParent(dialogRect, false);
            RectTransform tabsRect = (RectTransform)tabs;
            tabsRect.anchorMin = new Vector2(0f, 1f);
            tabsRect.anchorMax = new Vector2(1f, 1f);
            tabsRect.pivot = new Vector2(0.5f, 1f);
            tabsRect.anchoredPosition = new Vector2(0f, -48f);
            tabsRect.sizeDelta = new Vector2(-24f, 34f);
            var tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 6f;
            tabLayout.childControlHeight = true;
            tabLayout.childControlWidth = true;
            tabLayout.childForceExpandHeight = false;
            tabLayout.childForceExpandWidth = true;
            string[] tabNames = { "All", "Items", "Buildings", "Other" };
            for (int tabIndex = 0; tabIndex < tabNames.Length; tabIndex++)
            {
                int capturedIndex = tabIndex;
                CreateButton(tabsRect, tabNames[tabIndex], Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), () => SelectTab(capturedIndex));
            }
            UpdateTabColors();

            // Viewport
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(dialogRect, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(12f, 40f);
            viewport.offsetMax = new Vector2(-12f, -82f); 
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0.02f, 0.03f, 0.04f, 0.5f);
            viewportImage.raycastTarget = true;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            // Content
            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            _content = contentObject.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0.5f, 1f);
            _content.anchorMax = new Vector2(0.5f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(0f, 0f);

            GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CellSize, CellSize);
            grid.spacing = new Vector2(4f, 4f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.padding = new RectOffset(15, 15, 6, 6);
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewportObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = CellSize * 2f;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SelectTab(int tabIndex)
        {
            _activeTab = tabIndex;
            UpdateTabColors();
            RebuildGrid();
        }

        private static void UpdateTabColors()
        {
            if (_window == null) return;
            Transform tabs = _window.transform.Find("Dialog/Tabs");
            if (tabs == null) return;
            for (int i = 0; i < tabs.childCount; i++)
            {
                Image tabImg = tabs.GetChild(i).GetComponent<Image>();
                if (tabImg != null)
                    tabImg.color = (i == _activeTab)
                        ? new Color(0.22f, 0.28f, 0.38f, 1f)
                        : new Color(0.1f, 0.13f, 0.18f, 0.8f);
            }
        }

        private static void RebuildGrid()
        {
            if (_content == null || _selectedIds == null)
                return;

            for (int childIndex = _content.childCount - 1; childIndex >= 0; childIndex--)
                UnityEngine.Object.Destroy(_content.GetChild(childIndex).gameObject);

            var prototypes = GetCandidateItems()
                .Where(IsInActiveTab)
                .OrderBy(item => item.GridIndex)
                .ThenBy(item => item.ID);

            foreach (ItemProto item in prototypes)
                CreateItemCell(item);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        private static IEnumerable<ItemProto> GetCandidateItems()
        {
            var items = LDB.items?.dataArray;
            if (items == null || items.Length == 0)
                return Array.Empty<ItemProto>();

            return items.Where(item => item != null && item.ID > 0);
        }

        private static bool IsInActiveTab(ItemProto item)
        {
            if (_activeTab == 0)
                return true;

            int gridType = item.GridIndex >= 0 ? item.GridIndex / 1000 : 0;
            if (_activeTab == 1)
                return gridType == 1;
            if (_activeTab == 2)
                return gridType == 2;
            return gridType != 1 && gridType != 2;
        }

        private static void CreateItemCell(ItemProto item)
        {
            var cellObject = new GameObject("Item_" + item.ID, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ItemCellHover));
            cellObject.transform.SetParent(_content, false);
            RectTransform cellRect = cellObject.GetComponent<RectTransform>();
            cellRect.sizeDelta = new Vector2(CellSize, CellSize);

            // Cell background
            Image cellBg = cellObject.GetComponent<Image>();
            cellBg.color = new Color(0.08f, 0.1f, 0.14f, 0.7f);
            cellBg.raycastTarget = true;

            // Icon as child
            var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(cellObject.transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);
            Image icon = iconObj.GetComponent<Image>();
            icon.sprite = item.iconSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Button button = cellObject.GetComponent<Button>();
            button.targetGraphic = cellBg;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.normalColor = Color.white;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.onClick.AddListener(() => ToggleItem(item.ID));

            var hover = cellObject.GetComponent<ItemCellHover>();
            hover.SetItem(item);
            RefreshCell(cellObject, item.ID);
        }

        private static void ToggleItem(int itemId)
        {
            if (_selectedIds == null || _onToggle == null)
                return;

            if (_selectedIds.Contains(itemId) && _selectedIds.Count <= 1)
                return;

            _onToggle(itemId);
            Transform content = _content;
            if (content == null)
                return;

            for (int index = 0; index < content.childCount; index++)
            {
                ItemCellHover hover = content.GetChild(index).GetComponent<ItemCellHover>();
                hover?.RefreshVisual();
            }
        }

        private static void RefreshCell(GameObject cell, int itemId)
        {
            if (cell == null || _selectedIds == null)
                return;

            bool included = _selectedIds.Contains(itemId);

            Image cellBg = cell.GetComponent<Image>();
            if (cellBg != null)
                cellBg.color = included
                    ? new Color(0.12f, 0.16f, 0.22f, 0.9f)
                    : new Color(0.04f, 0.05f, 0.07f, 0.6f);

            Transform icon = cell.transform.Find("Icon");
            if (icon != null)
            {
                Image iconImg = icon.GetComponent<Image>();
                if (iconImg != null)
                    iconImg.color = included ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }

        private static void CreateButton(RectTransform parent, string labelText, Vector2 position, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Action onClick)
        {
            var buttonObject = new GameObject("Button_" + labelText, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.1f, 0.13f, 0.18f, 0.9f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.normalColor = Color.white;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.onClick.AddListener(() => onClick());

            Text text = CreateText(buttonObject.transform as RectTransform, _bodyTextTemplate, labelText, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero, TextAnchor.MiddleCenter, 13);
            if (text != null)
            {
                text.raycastTarget = false;
                text.color = new Color(0.85f, 0.88f, 0.92f);
            }
        }

        private static Text CreateText(RectTransform parent, Text template, string value, Vector2 position, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, TextAnchor alignment, int fontSize)
        {
            if (template == null || template.gameObject == null)
                return null;

            GameObject labelObject = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            labelObject.name = "Label_" + value.Replace(' ', '_');
            Text label = labelObject.GetComponent<Text>();
            RectTransform rect = labelObject.transform as RectTransform;
            if (label == null || rect == null)
            {
                UnityEngine.Object.Destroy(labelObject);
                return null;
            }

            label.text = value;
            label.alignment = alignment;
            label.fontSize = fontSize;
            label.raycastTarget = false;
            label.color = new Color(0.85f, 0.88f, 0.92f);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return label;
        }

        private sealed class ItemCellHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private ItemProto _item;
            private UIButtonTip _tip;

            internal void SetItem(ItemProto item)
            {
                _item = item;
            }

            internal void RefreshVisual()
            {
                if (_item != null)
                    RefreshCell(gameObject, _item.ID);
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (_item == null || _tip != null)
                    return;

                _tip = UIButtonTip.Create(true, string.Empty, _item.name, 5, Vector2.zero, 240, transform, string.Empty, string.Empty);

                if (_tip != null)
                {
                    _tip.transform.SetParent(_window.transform, false);
                    _tip.transform.SetAsLastSibling();

                    // Position above the dialog (dialog is 500px tall, centered)
                    RectTransform tipRect = _tip.transform as RectTransform;
                    if (tipRect != null)
                    {
                        tipRect.anchorMin = new Vector2(0.5f, 0.5f);
                        tipRect.anchorMax = new Vector2(0.5f, 0.5f);
                        tipRect.pivot = new Vector2(0.5f, 0f); // bottom-center of tooltip
                        tipRect.anchoredPosition = new Vector2(0f, 260f); // 250 (half dialog) + 10px gap
                    }
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                ClearTooltip();
            }

            private void OnDisable()
            {
                ClearTooltip();
            }

            private void ClearTooltip()
            {
                if (_tip == null)
                    return;

                UnityEngine.Object.Destroy(_tip.gameObject);
                _tip = null;
            }
        }
    }
}