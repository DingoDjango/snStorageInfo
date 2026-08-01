using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace StorageInfo
{
    public static class StorageDetailUI
    {
        // Max grid size before scaling down. Increase to allow larger containers at full size.
        private const float MaxPreviewWidth = 320f;
        private const float MaxPreviewHeight = 400f;

        // Space between panel border and grid content.
        private const float PanelPadding = 10f;

        // Horizontal anchor for panel center. 0.5 = screen center, 1.0 = right edge.
        // 0.75 = halfway between reticle and right edge.
        private const float PanelAnchorX = 0.75f;

        // Vertical anchor for panel center. 0.5 = vertical center of screen.
        private const float PanelAnchorY = 0.5f;

        // Fine-tune position after anchor placement (pixels, canvas space).
        private static readonly Vector2 PanelOffset = Vector2.zero;

        // Must match vanilla uGUI_ItemsContainer cell size.
        private const int CellSize = 71;

        private static GameObject root;
        private static RectTransform panelRect;
        private static RectTransform contentRect;
        private static uGUI_ItemsContainerView containerView;
        private static ItemsContainer boundContainer;
        private static bool built;
        private static Sprite panelSprite;
        private static RawImage gridImage;

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

                ApplyGridAppearance();
                ApplyPanelAppearance();
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

            if (panelSprite != null)
            {
                Object.Destroy(panelSprite);
                panelSprite = null;
            }

            if (gridImage != null)
            {
                Object.Destroy(gridImage);
                gridImage = null;
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
            panelRect.anchorMin = new Vector2(PanelAnchorX, PanelAnchorY);
            panelRect.anchorMax = new Vector2(PanelAnchorX, PanelAnchorY);
            panelRect.pivot = new Vector2(0.5f, 0.5f);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.raycastTarget = false;

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
            // Set initial uvRect so grid tiles correctly once texture is assigned
            gridImage.uvRect = new Rect(0f, 0f, 1f, 1f);

            containerView = viewObj.AddComponent<uGUI_ItemsContainerView>();
            containerView.rectTransform = viewRect;
            containerView.grid = gridImage;

            root.SetActive(false);
            built = true;

            return true;
        }

        private static void ApplyGridAppearance()
        {
            uGUI_ItemsContainerView view = uGUI.main.GetComponentInChildren<uGUI_ItemsContainerView>(true);
            if (view != null && view.grid != null)
            {
                gridImage.texture = view.grid.texture;
                gridImage.material = view.grid.material;
                gridImage.color = view.grid.color;
                
                return;
            }

            uGUI_ItemsContainer container = uGUI.main.GetComponentInChildren<uGUI_ItemsContainer>(true);
            if (container != null && container.grid != null)
            {
                gridImage.texture = container.grid.texture;
                gridImage.material = container.grid.material;
                gridImage.color = container.grid.color;
                
                return;
            }
        }

        // Generate transparent grid overlay for panel background.
        private static Sprite LoadPanelSprite()
        {
            if (panelSprite != null)
                return panelSprite;

            int gridSize = 32;
            Texture2D texture = new Texture2D(gridSize, gridSize, TextureFormat.RGBA32, false);

            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    float gridAlpha = 0.15f; // Dark translucent overlay

                    if (x % 4 == 0 || y % 4 == 0)
                        gridAlpha *= 2f;

                    texture.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Clamp(gridAlpha, 0f, 1f)));
                }
            }

            texture.Apply();
            panelSprite = Sprite.Create(texture, new Rect(0f, 0f, gridSize, gridSize), new Vector2(0.5f, 0.5f));
            
            return panelSprite;
        }

        // Apply transparent grid overlay (no solid color border).
        private static void ApplyPanelAppearance()
        {
            Image panelImage = panelRect.GetComponent<Image>();
            if (panelImage == null)
            {
                return;
            }

            Sprite bgSprite = LoadPanelSprite();
            if (bgSprite != null)
            {
                panelImage.sprite = bgSprite;
                panelImage.type = Image.Type.Simple;
            }
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