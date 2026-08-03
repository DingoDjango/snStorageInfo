using HarmonyLib;
using Nautilus.Utility;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace StorageInfo
{
    public static class StorageDetailUI
    {
        // Max grid size before scaling down. Increase to allow larger containers at full size.
        private const float MaxPreviewWidth = 320f;
        private const float MaxPreviewHeight = 400f;

        // Background border padding: space between texture edge and grid.
        // Left is thicker (PDABackground_Mod asymmetric frame).
        private const float PadLeft   = 15f;
        private const float PadRight  =  5f;
        private const float PadTop    = 15f;
        private const float PadBottom =  5f;

        // Panel background texture file.
        private const string BackgroundTextureFile = "PDABackground_Mod.png";

        // Must match vanilla uGUI_ItemsContainer cell size.
        private const int CellSize = 71;

        // Fixed panel placement - 75% from left, vertical center, no offset.
        private const float PanelAnchorX = 0.75f;
        private const float PanelAnchorY = 0.5f;

        // Panel styling - semi-transparent dark like game panels.
        private const float PanelOpacity = 0.85f;

        private static GameObject root;
        private static RectTransform panelRect;
        private static RectTransform contentRect;
        private static uGUI_ItemsContainerView containerView;
        private static ItemsContainer boundContainer;
        private static bool built;
        private static Sprite panelSprite;
        private static Texture2D panelTexture;
        private static RawImage gridImage;
        // Owned only when the procedural fallback grid tile is used (vanilla grid texture is shared, never destroyed).
        private static Texture2D gridTexture;
        // Extra dark overlay above the background, below the grid. Optional via mod option.
        private static Image backgroundOverlay;

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
            }

            // Reapply every Show so mod option changes take effect on next hover.
            ApplyPanelAppearance();

            LayoutPanel(container);
            root.transform.SetAsLastSibling();
            root.SetActive(true);

            // CanvasGroup.blocksRaycasts=false already blocks all raycasts on this
            // subtree; icon raycasts are disabled once per bind in Show().
            containerView.DoUpdate();
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

            // Vanilla per-frame bar update (batteries, food decay, etc.).
            // No per-frame DisableIconRaycasts: the CanvasGroup already blocks raycasts.
            containerView.DoUpdate();
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
                UnityEngine.Object.Destroy(root);
                root = null;
            }

            if (panelSprite != null)
            {
                UnityEngine.Object.Destroy(panelSprite);
                panelSprite = null;
            }

            if (panelTexture != null)
            {
                UnityEngine.Object.Destroy(panelTexture);
                panelTexture = null;
            }

            if (gridTexture != null)
            {
                UnityEngine.Object.Destroy(gridTexture);
                gridTexture = null;
            }

            gridImage = null;
            backgroundOverlay = null;
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
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.raycastTarget = false;

            // Extra dark overlay: sits between background and grid, darkens grid area when option enabled.
            GameObject overlayObj = new GameObject("BackgroundOverlay");
            overlayObj.layer = uiLayer;
            overlayObj.transform.SetParent(panelRect, false);
            backgroundOverlay = overlayObj.AddComponent<Image>();
            backgroundOverlay.raycastTarget = false;
            backgroundOverlay.color = new Color(0f, 0f, 0f, 0.35f);
            RectTransform overlayRect = backgroundOverlay.rectTransform;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            backgroundOverlay.enabled = false;

            GameObject contentObj = new GameObject("Content");
            contentObj.layer = uiLayer;
            contentObj.transform.SetParent(panelRect, false);
            contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

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

            gridImage = gridObj.AddComponent<RawImage>();
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

        // Copies the vanilla container grid texture/material/color unchanged.
        // Falls back to a procedural 1-cell tile when no vanilla container is present.
        private static void ApplyGridAppearance()
        {
            Texture sourceTexture = null;
            Material sourceMaterial = null;
            Color sourceColor = Color.white;

            // Exclude our own preview view - it would copy its own texture back.
            uGUI_ItemsContainerView[] views = uGUI.main.GetComponentsInChildren<uGUI_ItemsContainerView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == containerView || views[i].grid == gridImage)
                {
                    continue;
                }
                if (views[i].grid != null)
                {
                    sourceTexture = views[i].grid.texture;
                    sourceMaterial = views[i].grid.material;
                    sourceColor = views[i].grid.color;
                    break;
                }
            }

            if (sourceTexture == null)
            {
                uGUI_ItemsContainer container = uGUI.main.GetComponentInChildren<uGUI_ItemsContainer>(true);
                if (container != null && container.grid != null)
                {
                    sourceTexture = container.grid.texture;
                    sourceMaterial = container.grid.material;
                    sourceColor = container.grid.color;
                }
            }

            if (sourceTexture is Texture2D sourceTex2D)
            {
                // Vanilla texture is a shared asset - copy reference, never destroy.
                SetGridTexture(sourceTex2D, false);
            }
            else if (sourceTexture != null)
            {
                gridImage.texture = sourceTexture;
            }
            else
            {
                // Procedural fallback: 1-cell tile with border on cell edges.
                if (gridTexture == null)
                {
                    gridTexture = CreateGridTile(CellSize);
                }
                SetGridTexture(gridTexture, true);
                sourceMaterial = null;
                sourceColor = Color.white;
            }

            gridImage.material = sourceMaterial;
            gridImage.color = sourceColor;
        }

        private static void SetGridTexture(Texture2D texture, bool owned)
        {
            if (gridImage == null)
            {
                return;
            }

            // Release previous owned fallback tile before swapping references.
            if (gridTexture != null && gridTexture != texture)
            {
                UnityEngine.Object.Destroy(gridTexture);
                gridTexture = null;
            }

            gridImage.texture = texture;

            if (owned)
            {
                gridTexture = texture;
            }
        }

        // 1-cell tile: 1px border on right+bottom edges, tiled once per cell via uvRect.
        // Shared edges draw exactly once (no doubled lines), aligned to cell boundaries.
        private static Texture2D CreateGridTile(int size)
        {
            Texture2D tile = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tile.wrapMode = TextureWrapMode.Repeat;
            tile.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color line = new Color(0f, 0f, 0f, 0.15f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x == size - 1 || y == size - 1;
                    tile.SetPixel(x, y, edge ? line : clear);
                }
            }

            tile.Apply();
            return tile;
        }

        private static Sprite LoadPanelSprite()
        {
            if (panelSprite != null)
            {
                return panelSprite;
            }

            panelTexture = LoadPanelTexture();

            if (panelTexture == null)
            {
                panelTexture = CreateProceduralPanelTexture();
            }

            // Deferred to Nautilus (equivalent to Sprite.Create with 100f pixelsPerUnit).
            panelSprite = ImageUtils.LoadSpriteFromTexture(panelTexture);

            return panelSprite;
        }

        private static Texture2D LoadPanelTexture()
        {
            try
            {
                string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                string texturePath = Path.Combine(pluginDir, "Images", BackgroundTextureFile);
                return ImageUtils.LoadTextureFromFile(texturePath, TextureFormat.RGBA32);
            }
            catch (Exception e)
            {
                ModPlugin.LogMessage("Failed to load " + BackgroundTextureFile + ": " + e.Message);
                return null;
            }
        }

        // Procedural fallback: dark translucent grid texture.
        private static Texture2D CreateProceduralPanelTexture()
        {
            const int gridSize = 32;
            Texture2D texture = new Texture2D(gridSize, gridSize, TextureFormat.RGBA32, false);

            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    float gridAlpha = 0.25f;

                    if (x % 4 == 0 || y % 4 == 0)
                    {
                        gridAlpha *= 1.5f;
                    }

                    texture.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Clamp(gridAlpha, 0f, 1f)));
                }
            }

            texture.Apply();
            return texture;
        }

        private static void ApplyPanelAppearance()
        {
            Image panelImage = panelRect.GetComponent<Image>();
            if (panelImage == null)
            {
                return;
            }

            // Panel background always rendered.
            Sprite bgSprite = LoadPanelSprite();

            if (bgSprite != null)
            {
                panelImage.enabled = true;
                panelImage.sprite = bgSprite;
                panelImage.type = Image.Type.Simple;
                panelImage.color = new Color(1f, 1f, 1f, PanelOpacity);
            }

            // Extra dark overlay - only when option enabled.
            if (backgroundOverlay != null)
            {
                backgroundOverlay.enabled = ModPlugin.options.Background == PreviewBackground.Enabled;
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

            contentRect.anchoredPosition = new Vector2(PadLeft, -PadTop);

            float panelWidth  = gridWidth  * scale + PadLeft + PadRight;
            float panelHeight = gridHeight * scale + PadTop  + PadBottom;
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
