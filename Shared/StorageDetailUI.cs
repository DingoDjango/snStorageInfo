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
        private const float MaxPreviewWidth = 320f;
        private const float MaxPreviewHeight = 400f;

        /* Padding between item grid and border texture. */
        private const float PadLeft   = 15f;
        private const float PadRight  = 15f;
        private const float PadTop    = 15f;
        private const float PadBottom = 15f;

        // Vanilla cell/grid texture (grey fill + left/top border lines, Repeat wrap).
        private const string CellTextureFile = "PDACellBackground.png";

        // Vanilla grid corner L-shape sprites (36x36 texture, 4x 18x18 slices).
        private const string CornerTextureFile = "InventoryGridCorners.png";
        private const float CornerSize = 18f;
        private const float CornerPadding = 10f;

        // Must match vanilla uGUI_ItemsContainer cell size
        private const int CellSize = 71;

        private const float PanelAnchorX = 0.75f;
        private const float PanelAnchorY = 0.5f;

        private const float PanelOpacity = 0.85f;

        /* Sprite rects in InventoryGridCorners.png. BL, BR, TL, TR order. */
        private static readonly Rect[] CornerRects =
        {
            new Rect(0f, 18f, 18f, 18f),   // TL
            new Rect(18f, 18f, 18f, 18f),  // TR
            new Rect(0f, 0f, 18f, 18f),    // BL
            new Rect(18f, 0f, 18f, 18f),   // BR
        };

        private static GameObject root;
        private static RectTransform panelRect;
        private static RectTransform contentRect;
        private static uGUI_ItemsContainerView itemsContainerView;
        private static ItemsContainer boundContainer;
        private static bool isBuilt;
        private static Sprite panelSprite;
        private static Texture2D panelTexture;         
        private static bool panelTextureOwned; // True = panelTexture mod-created, False = vanilla shared asset (never destroyed)
        private static RawImage gridRawImage;
        private static Texture2D gridTexture;
        private static Image[] cornerImageComponents; // Corner L-shape images (TL/TR/BL/BR) childed to the grid rect, like vanilla.
        private static Sprite[] cornerSprites; // Owned only when built from mod fallback file (vanilla sprites are shared assets).
        private static bool cornerSpritesOwned;
        private static Image backgroundOverlay;
        private static uGUI_ItemsContainer cachedVanillaContainer;
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
            if (isBuilt && root != null)
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

            // Dark overlay between background and grid, improves readability over bright scenes.
            GameObject overlayObj = new GameObject("BackgroundOverlay");
            overlayObj.layer = uiLayer;
            overlayObj.transform.SetParent(panelObj.transform, false);
            backgroundOverlay = overlayObj.AddComponent<Image>();
            backgroundOverlay.raycastTarget = false;
            backgroundOverlay.color = new Color(0f, 0f, 0f, ModPlugin.options.PreviewUIBackgroundOpacity);
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

            gridRawImage = gridObj.AddComponent<RawImage>();
            gridRawImage.raycastTarget = false;
            // Set initial uvRect so grid tiles correctly once texture is assigned
            gridRawImage.uvRect = new Rect(0f, 0f, 1f, 1f);

            // Corner L-shapes (TL/TR/BL/BR), children of the grid like vanilla.
            cornerImageComponents = new Image[4];
            for (int cornerIndex = 0; cornerIndex < cornerImageComponents.Length; cornerIndex++)
            {
                GameObject cornerObj = new GameObject("Corner" + cornerIndex);
                cornerObj.layer = uiLayer;
                cornerObj.transform.SetParent(gridObj.transform, false);

                RectTransform cornerRect = cornerObj.AddComponent<RectTransform>();
                // Pivot/anchor to matching corner, offset outward by CornerPadding.
                float pivotX = (cornerIndex % 2 == 1) ? 1f : 0f;   // TR/BR = right
                float pivotY = (cornerIndex >= 2) ? 0f : 1f;       // BL/BR = bottom
                float signX = (cornerIndex % 2 == 1) ? 1f : -1f;
                float signY = (cornerIndex >= 2) ? -1f : 1f;
                cornerRect.anchorMin = new Vector2(pivotX, pivotY);
                cornerRect.anchorMax = new Vector2(pivotX, pivotY);
                cornerRect.pivot = new Vector2(pivotX, pivotY);
                cornerRect.anchoredPosition = new Vector2(signX * CornerPadding, signY * CornerPadding);
                cornerRect.sizeDelta = new Vector2(CornerSize, CornerSize);

                cornerImageComponents[cornerIndex] = cornerObj.AddComponent<Image>();
                cornerImageComponents[cornerIndex].raycastTarget = false;
            }

            itemsContainerView = viewObj.AddComponent<uGUI_ItemsContainerView>();
            itemsContainerView.rectTransform = viewRect;
            itemsContainerView.grid = gridRawImage;

            root.SetActive(false);
            isBuilt = true;

            return true;
        }

        // Copies the vanilla container grid texture/material/color unchanged, then
        // applies the corner L-sprites. Falls back to the mod-folder PDACellBackground
        // export, then to the procedural 1-cell tile when no source is available.
        private static void ApplyGridAppearance()
        {
            uGUI_ItemsContainer vanilla = FindVanillaContainer();
            Texture sourceTexture = null;
            Material sourceMaterial = null;
            Color sourceColor = Color.white;

            if (vanilla != null && vanilla.grid != null)
            {
                sourceTexture = vanilla.grid.texture;
                sourceMaterial = vanilla.grid.material;
                sourceColor = vanilla.grid.color;
            }

            if (sourceTexture is Texture2D sourceTex2D)
            {
                // Vanilla texture is a shared asset - copy reference, never destroy.
                SetGridTexture(sourceTex2D, false);
#if DEBUG
                ModPlugin.LogMessage($"Preview grid: vanilla shared texture \"{sourceTex2D.name}\" ({sourceTex2D.width}x{sourceTex2D.height})");
#endif
            }
            else if (sourceTexture != null)
            {
                gridRawImage.texture = sourceTexture;
#if DEBUG
                ModPlugin.LogMessage($"Preview grid: vanilla shared texture (non-Texture2D) \"{sourceTexture.name}\"");
#endif
            }
            else
            {
                // Mod-folder export of the vanilla cell texture.
                Texture2D cellTexture = LoadImageTexture(CellTextureFile);
                if (cellTexture != null)
                {
                    SetGridTexture(cellTexture, true);
                    sourceMaterial = null;
                    sourceColor = Color.white;
#if DEBUG
                    ModPlugin.LogMessage($"Preview grid: no vanilla source, mod folder \"{CellTextureFile}\" ({cellTexture.width}x{cellTexture.height})");
#endif
                }
                else
                {
                    // Last resort: procedural 1-cell tile with border on cell edges.
                    if (gridTexture == null)
                    {
                        gridTexture = CreateGridTile(CellSize);
                    }
                    SetGridTexture(gridTexture, true);
#if DEBUG
                    ModPlugin.LogMessage("Preview grid: no vanilla source and no mod folder file, procedural tile");
#endif
                }
            }

            gridRawImage.material = sourceMaterial;
            gridRawImage.color = sourceColor;

            ApplyCornerAppearance(vanilla);
        }

        private static uGUI_ItemsContainer FindVanillaContainer()
        {
            if (cachedVanillaContainer != null && cachedVanillaContainer.grid != null && cachedVanillaContainer.grid.texture != null)
            {
                return cachedVanillaContainer;
            }
            cachedVanillaContainer = null;

            // Fast path: search PDA screen first (children include vanilla inventory).
            if (uGUI_PDA.main != null)
            {
                uGUI_ItemsContainer container = FindLiveContainer(uGUI_PDA.main.transform);
                if (container != null)
                {
                    cachedVanillaContainer = container;
                    return container;
                }
            }

            // Fallback: uGUI.main direct search (covers layouts where the PDA screen is
            // parented under the main UI canvas).
            if (uGUI.main != null)
            {
                uGUI_ItemsContainer container = FindLiveContainer(uGUI.main.transform);
                if (container != null)
                {
                    cachedVanillaContainer = container;
                    return container;
                }
            }

            // Broader fallback: search all canvases for a container with a live grid.
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            for (int canvasIndex = 0; canvasIndex < canvases.Length; canvasIndex++)
            {
                uGUI_ItemsContainer container = FindLiveContainer(canvases[canvasIndex].transform);
                if (container != null)
                {
                    cachedVanillaContainer = container;
                    return container;
                }
            }

            // Nuclear: scene-wide scan (includes inactive objects).
            uGUI_ItemsContainer[] allContainers = Resources.FindObjectsOfTypeAll<uGUI_ItemsContainer>();
            for (int containerIndex = 0; containerIndex < allContainers.Length; containerIndex++)
            {
                if (allContainers[containerIndex].grid != null && allContainers[containerIndex].grid.texture != null)
                {
                    cachedVanillaContainer = allContainers[containerIndex];
                    return allContainers[containerIndex];
                }
            }
            return null;
        }

        // First uGUI_ItemsContainer under root with a live grid (inactive included).
        private static uGUI_ItemsContainer FindLiveContainer(Transform root)
        {
            if (root == null)
            {
                return null;
            }
            uGUI_ItemsContainer[] containers = root.GetComponentsInChildren<uGUI_ItemsContainer>(true);
            for (int containerIndex = 0; containerIndex < containers.Length; containerIndex++)
            {
                if (containers[containerIndex].grid != null && containers[containerIndex].grid.texture != null)
                {
                    return containers[containerIndex];
                }
            }
            return null;
        }

        private static void SetGridTexture(Texture2D texture, bool owned)
        {
            if (gridRawImage == null)
            {
                return;
            }

            // Release previous owned fallback tile before swapping references.
            if (gridTexture != null && gridTexture != texture)
            {
                UnityEngine.Object.Destroy(gridTexture);
                gridTexture = null;
            }

            gridRawImage.texture = texture;

            if (owned)
            {
                gridTexture = texture;
            }
        }

        // 1-cell tile with left/top border lines, tiled via uvRect.
        private static Texture2D CreateGridTile(int size)
        {
            Texture2D tile = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tile.wrapMode = TextureWrapMode.Repeat;
            tile.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color line = new Color(0f, 0f, 0f, 0.15f);

            for (int pixelY = 0; pixelY < size; pixelY++)
            {
                for (int pixelX = 0; pixelX < size; pixelX++)
                {
                    bool edge = pixelX == 0 || pixelY == 0;
                    tile.SetPixel(pixelX, pixelY, edge ? line : clear);
                }
            }

            tile.Apply();
            return tile;
        }

        // Apply corner L-shapes from vanilla or fallback.
        private static void ApplyCornerAppearance(uGUI_ItemsContainer vanilla)
        {
            if (cornerImageComponents == null)
            {
                return;
            }

            // Release previously owned fallback sprites before swapping references.
            if (cornerSpritesOwned && cornerSprites != null)
            {
                for (int cornerIndex = 0; cornerIndex < cornerSprites.Length; cornerIndex++)
                {
                    if (cornerSprites[cornerIndex] != null)
                    {
                        UnityEngine.Object.Destroy(cornerSprites[cornerIndex]);
                    }
                }
            }
            cornerSprites = null;
            cornerSpritesOwned = false;

            // Try vanilla corner images first: Grid/TL, Grid/TR, Grid/BL, Grid/BR.
            Sprite[] vanillaSprites = null;
            if (vanilla != null && vanilla.grid != null)
            {
                vanillaSprites = new Sprite[4];
                string[] cornerNames = { "TL", "TR", "BL", "BR" };
                Transform gridTransform = vanilla.grid.transform;
                for (int cornerIndex = 0; cornerIndex < cornerNames.Length; cornerIndex++)
                {
                    Transform cornerTransform = gridTransform.Find(cornerNames[cornerIndex]);
                    Image cornerImage = cornerTransform != null ? cornerTransform.GetComponent<Image>() : null;
                    vanillaSprites[cornerIndex] = cornerImage != null ? cornerImage.sprite : null;
                }
                for (int cornerIndex = 0; cornerIndex < cornerImageComponents.Length; cornerIndex++)
                {
                    if (vanillaSprites[cornerIndex] == null)
                    {
                        vanillaSprites = null;
                        break;
                    }
                }
            }

            if (vanillaSprites != null)
            {
                for (int cornerIndex = 0; cornerIndex < cornerImageComponents.Length; cornerIndex++)
                {
                    cornerImageComponents[cornerIndex].sprite = vanillaSprites[cornerIndex];
                    cornerImageComponents[cornerIndex].enabled = true;
                }
#if DEBUG
                ModPlugin.LogMessage($"Preview corners: vanilla shared sprites (tex \"{vanillaSprites[0].texture.name}\")");
#endif
                return;
            }

            // Fallback: mod-folder InventoryGridCorners export, sliced into 4 sprites.
            Texture2D cornerTexture = LoadImageTexture(CornerTextureFile);
            if (cornerTexture == null)
            {
                for (int cornerIndex = 0; cornerIndex < cornerImageComponents.Length; cornerIndex++)
                {
                    cornerImageComponents[cornerIndex].sprite = null;
                    cornerImageComponents[cornerIndex].enabled = false;
                }
#if DEBUG
                ModPlugin.LogMessage("Preview corners: no vanilla sprites and no mod folder file, corners disabled");
#endif
                return;
            }

            cornerSprites = new Sprite[4];
            for (int cornerIndex = 0; cornerIndex < cornerImageComponents.Length; cornerIndex++)
            {
                cornerSprites[cornerIndex] = Sprite.Create(cornerTexture, CornerRects[cornerIndex], new Vector2(0.5f, 0.5f), 100f);
                cornerImageComponents[cornerIndex].sprite = cornerSprites[cornerIndex];
                cornerImageComponents[cornerIndex].enabled = true;
            }
            cornerSpritesOwned = true;
#if DEBUG
            ModPlugin.LogMessage($"Preview corners: no vanilla sprites, mod folder \"{CornerTextureFile}\" sliced into 4 sprites");
#endif
        }

        // Loads image from mod fallback folder (Images/Fallback/).
        private static Texture2D LoadImageTexture(string fileName)
        {
            try
            {
                string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                string texturePath = Path.Combine(pluginDir, "Images", "Fallback", fileName);
                return ImageUtils.LoadTextureFromFile(texturePath, TextureFormat.RGBA32);
            }
            catch (Exception e)
            {
                ModPlugin.LogMessage("Failed to load " + fileName + ": " + e.Message);
                return null;
            }
        }

        private static Sprite LoadPanelSprite()
        {
            if (panelSprite != null)
            {
                return panelSprite;
            }

            // Preferred: the vanilla storage background - the stretched PDACellBackground
            // cell texture (grey fill + left/top lines). Shared asset, never destroyed.
            Texture2D vanillaBg = GetVanillaBackgroundTexture();
            if (vanillaBg != null)
            {
                panelTexture = vanillaBg;
                panelTextureOwned = false;
                panelSprite = ImageUtils.LoadSpriteFromTexture(panelTexture);
#if DEBUG
                ModPlugin.LogMessage($"Preview panel background: vanilla shared texture \"{vanillaBg.name}\" ({vanillaBg.width}x{vanillaBg.height})");
#endif
                return panelSprite;
            }
#if DEBUG
            ModPlugin.LogMessage("Preview panel background: no vanilla background, falling back to mod folder");
#endif

            // Fallback: mod-folder cell texture (PDABackground_Mod temporarily disabled),
            // then procedural.
            panelTexture = LoadImageTexture(CellTextureFile);
            panelTextureOwned = panelTexture != null;

            if (panelTexture == null)
            {
                panelTexture = CreateProceduralPanelTexture();
            }
#if DEBUG
            ModPlugin.LogMessage($"Preview panel background: fallback texture \"{panelTexture.name}\" ({panelTexture.width}x{panelTexture.height})");
#endif

            // Deferred to Nautilus (equivalent to Sprite.Create with 100f pixelsPerUnit).
            panelSprite = ImageUtils.LoadSpriteFromTexture(panelTexture);

            return panelSprite;
        }

        // Vanilla container.background is the stretched PDACellBackground RawImage.
        private static Texture2D GetVanillaBackgroundTexture()
        {
            uGUI_ItemsContainer vanilla = FindVanillaContainer();
            if (vanilla == null || vanilla.background == null)
            {
                return null;
            }

            RawImage bgRaw = vanilla.background as RawImage;
            if (bgRaw != null)
            {
                return bgRaw.texture as Texture2D;
            }

            Image bgImage = vanilla.background as Image;
            if (bgImage != null && bgImage.sprite != null)
            {
                return bgImage.sprite.texture as Texture2D;
            }

            return null;
        }

        // Procedural fallback: dark translucent grid texture.
        private static Texture2D CreateProceduralPanelTexture()
        {
            const int gridSize = 32;
            Texture2D texture = new Texture2D(gridSize, gridSize, TextureFormat.RGBA32, false);

            for (int pixelY = 0; pixelY < gridSize; pixelY++)
            {
                for (int pixelX = 0; pixelX < gridSize; pixelX++)
                {
                    float gridAlpha = 0.25f;

                    if (pixelX % 4 == 0 || pixelY % 4 == 0)
                    {
                        gridAlpha *= 1.5f;
                    }

                    texture.SetPixel(pixelX, pixelY, new Color(0f, 0f, 0f, Mathf.Clamp(gridAlpha, 0f, 1f)));
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

            if (backgroundOverlay != null)
            {
                backgroundOverlay.color = new Color(0f, 0f, 0f, ModPlugin.options.PreviewUIBackgroundOpacity);
                backgroundOverlay.enabled = ModPlugin.options.PreviewUIBackround;
            }
        }

        private static void PrepareContainerLayout(ItemsContainer container)
        {
            int width = container.sizeX;
            int height = container.sizeY;
            float gridWidth = width * CellSize;
            float gridHeight = height * CellSize;

            contentRect.sizeDelta = new Vector2(gridWidth, gridHeight);
            itemsContainerView.rectTransform.sizeDelta = new Vector2(gridWidth, gridHeight);

            if (itemsContainerView.grid != null)
            {
                itemsContainerView.grid.uvRect = new Rect(0f, 0f, width, height);
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

            // Corners are children of the grid, so they inherit the content scale.
            // Counter-scale size/offset by 1/scale so the L-shapes always render at
            // their native pixel size (CornerSize) - crisp 1:1 like the vanilla PDA
            // storage view at any panel scale, never upscaled/blurry.
            if (cornerImageComponents != null)
            {
                float cornerSize = CornerSize / scale;
                float cornerPad = CornerPadding / scale;
                for (int cornerIndex = 0; cornerIndex < cornerImageComponents.Length; cornerIndex++)
                {
                    RectTransform cornerRect = cornerImageComponents[cornerIndex].rectTransform;
                    float signX = (cornerIndex % 2 == 1) ? 1f : -1f;
                    float signY = (cornerIndex >= 2) ? -1f : 1f;
                    cornerRect.sizeDelta = new Vector2(cornerSize, cornerSize);
                    cornerRect.anchoredPosition = new Vector2(signX * cornerPad, signY * cornerPad);
                }
            }

            contentRect.anchoredPosition = new Vector2(PadLeft, -PadTop);

            float panelWidth  = gridWidth  * scale + PadLeft + PadRight;
            float panelHeight = gridHeight * scale + PadTop  + PadBottom;
            panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        }

        private static void DisableIconRaycasts()
        {
            if (itemsContainerView == null)
            {
                return;
            }

            uGUI_ItemIcon[] icons = itemsContainerView.GetComponentsInChildren<uGUI_ItemIcon>(true);

            for (int iconIndex = 0; iconIndex < icons.Length; iconIndex++)
            {
                icons[iconIndex].raycastTarget = false;
            }
        }

        private static void UnbindContainer()
        {
            if (itemsContainerView != null && boundContainer != null)
            {
                itemsContainerView.Uninit();
            }

            boundContainer = null;
        }

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
                itemsContainerView.Init(container);
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
            itemsContainerView.DoUpdate();
        }

        public static void Tick(ItemsContainer container)
        {
            if (root == null || !root.activeSelf || container == null || boundContainer != container)
            {
                // Panel not built/bound/visible. After a scene load the first Show()
                // can fail while uGUI/Player are still initializing, and the reticle
                // dirty-flag gate in HarmonyPatches won't retry it - so re-enter the
                // full Show() path here while the overlay is allowed. Show() is a cheap
                // no-op once the panel is up and bound; gating on CanShowOverlay avoids
                // Show()/Hide() churn every frame while blocked (e.g. PDA open).
                if (container != null && CanShowOverlay(container))
                {
                    Show(container);
                }
                return;
            }

            if (!CanShowOverlay(container))
            {
                Hide();
                return;
            }

            // Vanilla per-frame bar update (batteries, food decay, etc.).
            // No per-frame DisableIconRaycasts: the CanvasGroup already blocks raycasts.
            itemsContainerView.DoUpdate();
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

            if (panelTexture != null && panelTextureOwned)
            {
                UnityEngine.Object.Destroy(panelTexture);
            }
            panelTexture = null;
            panelTextureOwned = false;

            if (gridTexture != null)
            {
                UnityEngine.Object.Destroy(gridTexture);
                gridTexture = null;
            }

            // Corner sprites are shared vanilla assets unless owned by the mod fallback.
            if (cornerSpritesOwned && cornerSprites != null)
            {
                for (int cornerIndex = 0; cornerIndex < cornerSprites.Length; cornerIndex++)
                {
                    if (cornerSprites[cornerIndex] != null)
                    {
                        UnityEngine.Object.Destroy(cornerSprites[cornerIndex]);
                    }
                }
            }
            cornerSprites = null;
            cornerSpritesOwned = false;
            cornerImageComponents = null;

            gridRawImage = null;
            backgroundOverlay = null;
            panelRect = null;
            contentRect = null;
            itemsContainerView = null;
            cachedVanillaContainer = null;
            isBuilt = false;
        }
    }
}
