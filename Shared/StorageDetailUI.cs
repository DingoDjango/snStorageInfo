using UnityEngine;
using UnityEngine.UI;

namespace StorageInfo
{
    public static class StorageDetailUI
    {
        private const float MaxPreviewWidth = 320f;
        private const float MaxPreviewHeight = 400f;
        private const float PanelPadding = 10f;
        private const float ScreenMargin = 36f;
        private const int CellSize = 71;

        private static GameObject root;
        private static RectTransform panelRect;
        private static RectTransform contentRect;
        private static uGUI_ItemsContainerView containerView;
        private static ItemsContainer boundContainer;
        private static bool built;

        public static void Show(ItemsContainer container)
        {
            if (container == null || !CanShowOverlay(container))
            {
                Hide();
                return;
            }

            if (!EnsureBuilt())
            {
                return;
            }

            if (boundContainer != container)
            {
                UnbindContainer();
                PrepareContainerLayout(container);
                ((IItemsContainer)container).UpdateContainer();
                containerView.Init(container);
                DisableIconRaycasts();
                boundContainer = container;
            }

            LayoutPanel(container);
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            containerView.DoUpdate();
            DisableIconRaycasts();
        }

        public static void Tick(ItemsContainer container)
        {
            if (root == null || !root.activeSelf || container == null || boundContainer != container)
            {
                return;
            }

            if (!CanShowOverlay(container))
            {
                Hide();
                return;
            }

            containerView.DoUpdate();
            DisableIconRaycasts();
        }

        public static void Hide()
        {
            UnbindContainer();

            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public static void Cleanup()
        {
            Hide();

            if (root != null)
            {
                Object.Destroy(root);
                root = null;
            }

            panelRect = null;
            contentRect = null;
            containerView = null;
            built = false;
        }

        private static bool CanShowOverlay(ItemsContainer container)
        {
            if (WaitScreen.IsWaiting)
            {
                return false;
            }

            if (Player.main == null)
            {
                return false;
            }

            PDA pda = Player.main.GetPDA();

            if (pda != null && pda.isInUse)
            {
                return false;
            }

            if (container != null && Inventory.main != null && Inventory.main.IsUsingStorage(container))
            {
                return false;
            }

            return true;
        }

        private static bool EnsureBuilt()
        {
            if (built && root != null)
            {
                return true;
            }

            if (!uGUI.isInitialized || uGUI.main == null || uGUI.main.screenCanvas == null)
            {
                return false;
            }

            Canvas canvas = uGUI.main.screenCanvas;
            int uiLayer = canvas.gameObject.layer;

            root = new GameObject("StorageInfoPreview");
            root.layer = uiLayer;
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject panelObj = new GameObject("Panel");
            panelObj.layer = uiLayer;
            panelObj.transform.SetParent(root.transform, false);
            panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.raycastTarget = false;
            panelImage.color = new Color(0.04f, 0.07f, 0.11f, 0.92f);

            GameObject contentObj = new GameObject("Content");
            contentObj.layer = uiLayer;
            contentObj.transform.SetParent(panelRect, false);
            contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = new Vector2(PanelPadding, -PanelPadding);

            GameObject viewObj = new GameObject("ItemsView");
            viewObj.layer = uiLayer;
            viewObj.transform.SetParent(contentRect, false);

            RectTransform viewRect = viewObj.AddComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(0f, 1f);
            viewRect.anchorMax = new Vector2(0f, 1f);
            viewRect.pivot = new Vector2(0f, 1f);
            viewRect.anchoredPosition = Vector2.zero;

            GameObject gridObj = new GameObject("Grid");
            gridObj.layer = uiLayer;
            gridObj.transform.SetParent(viewRect, false);

            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            RawImage gridImage = gridObj.AddComponent<RawImage>();
            gridImage.raycastTarget = false;
            ApplyVanillaGridAppearance(gridImage);

            containerView = viewObj.AddComponent<uGUI_ItemsContainerView>();
            containerView.rectTransform = viewRect;
            containerView.grid = gridImage;

            root.SetActive(false);
            built = true;
            return true;
        }

        private static void ApplyVanillaGridAppearance(RawImage gridImage)
        {
            uGUI_InventoryTab inventoryTab = uGUI.main.GetComponentInChildren<uGUI_InventoryTab>(true);

            if (inventoryTab == null || inventoryTab.storage == null || inventoryTab.storage.grid == null)
            {
                return;
            }

            RawImage sourceGrid = inventoryTab.storage.grid;
            gridImage.texture = sourceGrid.texture;
            gridImage.material = sourceGrid.material;
            gridImage.color = sourceGrid.color;
            gridImage.uvRect = sourceGrid.uvRect;
        }

        private static void PrepareContainerLayout(ItemsContainer container)
        {
            int width = container.sizeX;
            int height = container.sizeY;
            float gridWidth = width * CellSize;
            float gridHeight = height * CellSize;

            contentRect.sizeDelta = new Vector2(gridWidth, gridHeight);
            containerView.rectTransform.sizeDelta = new Vector2(gridWidth, gridHeight);

            if (containerView.grid != null)
            {
                containerView.grid.uvRect = new Rect(0f, 0f, width, height);
            }
        }

        private static void LayoutPanel(ItemsContainer container)
        {
            int width = container.sizeX;
            int height = container.sizeY;
            float gridWidth = width * CellSize;
            float gridHeight = height * CellSize;
            float scale = Mathf.Min(1f, MaxPreviewWidth / gridWidth, MaxPreviewHeight / gridHeight);

            contentRect.localScale = new Vector3(scale, scale, 1f);

            float panelWidth = gridWidth * scale + PanelPadding * 2f;
            float panelHeight = gridHeight * scale + PanelPadding * 2f;
            panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            panelRect.anchoredPosition = new Vector2(-ScreenMargin, 0f);
        }

        private static void DisableIconRaycasts()
        {
            if (containerView == null)
            {
                return;
            }

            uGUI_ItemIcon[] icons = containerView.GetComponentsInChildren<uGUI_ItemIcon>(true);

            for (int i = 0; i < icons.Length; i++)
            {
                icons[i].raycastTarget = false;
            }
        }

        private static void UnbindContainer()
        {
            if (containerView != null && boundContainer != null)
            {
                containerView.Uninit();
            }

            boundContainer = null;
        }
    }
}
