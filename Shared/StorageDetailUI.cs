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
        // PDABackground_Mod frame is nearly symmetric (L~20px, R~17px, T~17px, B~19px
        // in a 1195x970 texture) and renders only ~5px thick at typical panel sizes
        // (Image.Type.Simple stretches borders with panel size). PadRight/PadBottom must
        // clear the rendered frame, else the grid spills over the border.
        private const float PadLeft   = 15f;
        private const float PadRight  = 15f;
        private const float PadTop    = 15f;
        private const float PadBottom = 15f;

        // Panel background texture file.
        private const string BackgroundTextureFile = "PDABackground_Mod.png";

        // Vanilla cell/grid texture (grey fill + left/top border lines, Repeat wrap).
        private const string CellTextureFile = "PDACellBackground.png";

        // Vanilla grid corner L-shape sprites (36x36 texture, 4x 18x18 slices).
        private const string CornerTextureFile = "InventoryGridCorners.png";
        private const float CornerSize = 36f;
        // Corners sit OUTSIDE the grid rect in vanilla - push them outward this far.
        private const float CornerPadding = 10f;

        // Must match vanilla uGUI_ItemsContainer cell size.
        private const int CellSize = 71;

        // Fixed panel placement - 75% from left, vertical center, no offset.
        private const float PanelAnchorX = 0.75f;
        private const float PanelAnchorY = 0.5f;

        // Panel styling - semi-transparent dark like game panels.
        private const float PanelOpacity = 0.85f;

        // Sprite rects in InventoryGridCorners.png, Unity bottom-left origin. From the
        // exported .asset files: BL=(0,0), BR=(18,0), TL=(0,18), TR=(18,18), each 18x18.
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
        private static uGUI_ItemsContainerView containerView;
        private static ItemsContainer boundContainer;
        private static bool built;
        private static Sprite panelSprite;
        private static Texture2D panelTexture;
        // True when panelTexture was created by the mod (file/procedural). False when it
        // is the shared vanilla cell texture - never destroyed.
        private static bool panelTextureOwned;
        private static RawImage gridImage;
        // Owned only when the procedural fallback grid tile is used (vanilla grid texture is shared, never destroyed).
        private static Texture2D gridTexture;
        // Corner L-shape images (TL/TR/BL/BR) childed to the grid rect, like vanilla.
        private static Image[] cornerImages;
        // Owned only when built from the mod-folder fallback file (vanilla sprites are
        // shared assets, never destroyed).
        private static Sprite[] cornerSprites;
        private static bool cornerSpritesOwned;
        // Extra dark overlay above the background, below the grid. Optional via mod option.
        private static Image backgroundOverlay;
        // Cached vanilla container so repeat look-ups are O(1). Unity-null safe:
        // re-searches when the object is destroyed (scene change) or the grid loses
        // its texture.
        private static uGUI_ItemsContainer cachedVanillaContainer;
        // One-shot log of which search branch resolved the vanilla container.
        private static bool loggedFindBranch;

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
            overlayObj.transform.SetParent(panelObj.transform, false);
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

            // Corner L-shapes (TL/TR/BL/BR), children of the grid like vanilla.
            cornerImages = new Image[4];
            for (int c = 0; c < cornerImages.Length; c++)
            {
                GameObject cornerObj = new GameObject("Corner" + c);
                cornerObj.layer = uiLayer;
                cornerObj.transform.SetParent(gridObj.transform, false);

                RectTransform cornerRect = cornerObj.AddComponent<RectTransform>();
                // Pivot/anchor to the matching corner of the grid rect. Offset outward
                // by CornerPadding (vanilla places corner L-shapes outside the cell area).
                float px = (c % 2 == 1) ? 1f : 0f;   // TR/BR = right
                float py = (c >= 2) ? 0f : 1f;       // BL/BR = bottom
                float signX = (c % 2 == 1) ? 1f : -1f;
                float signY = (c >= 2) ? -1f : 1f;
                cornerRect.anchorMin = new Vector2(px, py);
                cornerRect.anchorMax = new Vector2(px, py);
                cornerRect.pivot = new Vector2(px, py);
                cornerRect.anchoredPosition = new Vector2(signX * CornerPadding, signY * CornerPadding);
                cornerRect.sizeDelta = new Vector2(CornerSize, CornerSize);

                cornerImages[c] = cornerObj.AddComponent<Image>();
                cornerImages[c].raycastTarget = false;
            }

            containerView = viewObj.AddComponent<uGUI_ItemsContainerView>();
            containerView.rectTransform = viewRect;
            containerView.grid = gridImage;

            root.SetActive(false);
            built = true;

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
                ModPlugin.LogMessage($"Preview grid: vanilla shared texture \"{sourceTex2D.name}\" ({sourceTex2D.width}x{sourceTex2D.height})");
            }
            else if (sourceTexture != null)
            {
                gridImage.texture = sourceTexture;
                ModPlugin.LogMessage($"Preview grid: vanilla shared texture (non-Texture2D) \"{sourceTexture.name}\"");
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
                    ModPlugin.LogMessage($"Preview grid: no vanilla source, mod folder \"{CellTextureFile}\" ({cellTexture.width}x{cellTexture.height})");
                }
                else
                {
                    // Last resort: procedural 1-cell tile with border on cell edges.
                    if (gridTexture == null)
                    {
                        gridTexture = CreateGridTile(CellSize);
                    }
                    SetGridTexture(gridTexture, true);
                    sourceMaterial = null;
                    sourceColor = Color.white;
                    ModPlugin.LogMessage("Preview grid: no vanilla source and no mod folder file, procedural tile");
                }
            }

            gridImage.material = sourceMaterial;
            gridImage.color = sourceColor;

            ApplyCornerAppearance(vanilla);
        }

        private static uGUI_ItemsContainer FindVanillaContainer()
        {
            if (cachedVanillaContainer != null && cachedVanillaContainer.grid != null && cachedVanillaContainer.grid.texture != null)
            {
                return cachedVanillaContainer;
            }
            cachedVanillaContainer = null;

            // Fast path: uGUI_PDA.main is the component on the uGUI_PDAScreen(Clone)
            // root (lazily instantiated once by PDA.ui and kept in the scene). The
            // vanilla inventory containers (Content/InventoryTab/*) are children of it,
            // so one GetComponentsInChildren resolves the source without scanning every
            // canvas or the whole scene.
            if (uGUI_PDA.main != null)
            {
                uGUI_ItemsContainer c = FindLiveContainer(uGUI_PDA.main.transform);
                if (c != null)
                {
                    LogFindBranch("pda-root: " + BuildTransformPath(c.transform));
                    cachedVanillaContainer = c;
                    return c;
                }
            }

            // Fallback: uGUI.main direct search (covers layouts where the PDA screen is
            // parented under the main UI canvas).
            if (uGUI.main != null)
            {
                uGUI_ItemsContainer c = FindLiveContainer(uGUI.main.transform);
                if (c != null)
                {
                    LogFindBranch("uGUI-main: " + BuildTransformPath(c.transform));
                    cachedVanillaContainer = c;
                    return c;
                }
            }

            // Broader fallback: search all canvases for a container with a live grid.
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                uGUI_ItemsContainer c = FindLiveContainer(canvases[i].transform);
                if (c != null)
                {
                    LogFindBranch("canvas-branch: " + BuildTransformPath(c.transform));
                    cachedVanillaContainer = c;
                    return c;
                }
            }

            // Nuclear: scene-wide scan (Resources.FindObjectsOfTypeAll includes inactive;
            // FindObjectsOfType(bool) was not added until Unity 2020.2).
            uGUI_ItemsContainer[] all = Resources.FindObjectsOfTypeAll<uGUI_ItemsContainer>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].grid != null && all[i].grid.texture != null)
                {
                    LogFindBranch("resources-branch: " + BuildTransformPath(all[i].transform));
                    cachedVanillaContainer = all[i];
                    return all[i];
                }
            }
            LogFindBranch("null (no vanilla container found)");
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
            for (int i = 0; i < containers.Length; i++)
            {
                if (containers[i].grid != null && containers[i].grid.texture != null)
                {
                    return containers[i];
                }
            }
            return null;
        }

        private static void LogFindBranch(string message)
        {
            if (loggedFindBranch)
            {
                return;
            }
            loggedFindBranch = true;
            ModPlugin.LogMessage("Preview find branch: " + message);
        }

        private static string BuildTransformPath(Transform node)
        {
            string path = node.name;
            Transform parent = node.parent;
            int hops = 0;
            while (parent != null && hops < 8)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
                hops++;
            }
            return path;
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

        // 1-cell tile mimicking the vanilla PDACellBackground convention: border on the
        // left (x==0) and top (y==0) edges only, tiled once per cell via uvRect. When
        // tiled, each open right/bottom edge is closed by the next cell's left/top line.
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
                    bool edge = x == 0 || y == 0;
                    tile.SetPixel(x, y, edge ? line : clear);
                }
            }

            tile.Apply();
            return tile;
        }

        // Corner L-shapes (TL/TR/BL/BR) on the grid rect, like vanilla. Copies the
        // vanilla sprites when the storage container is present (shared assets), else
        // builds owned sprites from the mod-folder InventoryGridCorners export.
        private static void ApplyCornerAppearance(uGUI_ItemsContainer vanilla)
        {
            if (cornerImages == null)
            {
                return;
            }

            // Release previously owned fallback sprites before swapping references.
            if (cornerSpritesOwned && cornerSprites != null)
            {
                for (int c = 0; c < cornerSprites.Length; c++)
                {
                    if (cornerSprites[c] != null)
                    {
                        UnityEngine.Object.Destroy(cornerSprites[c]);
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
                Transform gridT = vanilla.grid.transform;
                for (int c = 0; c < cornerNames.Length; c++)
                {
                    Transform cornerT = gridT.Find(cornerNames[c]);
                    Image cornerImg = cornerT != null ? cornerT.GetComponent<Image>() : null;
                    vanillaSprites[c] = cornerImg != null ? cornerImg.sprite : null;
                }
                for (int c = 0; c < cornerImages.Length; c++)
                {
                    if (vanillaSprites[c] == null)
                    {
                        vanillaSprites = null;
                        break;
                    }
                }
            }

            if (vanillaSprites != null)
            {
                for (int c = 0; c < cornerImages.Length; c++)
                {
                    cornerImages[c].sprite = vanillaSprites[c];
                    cornerImages[c].enabled = true;
                }
                ModPlugin.LogMessage($"Preview corners: vanilla shared sprites (tex \"{vanillaSprites[0].texture.name}\")");
                return;
            }

            // Fallback: mod-folder InventoryGridCorners export, sliced into 4 sprites.
            Texture2D cornerTexture = LoadImageTexture(CornerTextureFile);
            if (cornerTexture == null)
            {
                for (int c = 0; c < cornerImages.Length; c++)
                {
                    cornerImages[c].sprite = null;
                    cornerImages[c].enabled = false;
                }
                ModPlugin.LogMessage("Preview corners: no vanilla sprites and no mod folder file, corners disabled");
                return;
            }

            cornerSprites = new Sprite[4];
            for (int c = 0; c < cornerImages.Length; c++)
            {
                cornerSprites[c] = Sprite.Create(cornerTexture, CornerRects[c], new Vector2(0.5f, 0.5f), 100f);
                cornerImages[c].sprite = cornerSprites[c];
                cornerImages[c].enabled = true;
            }
            cornerSpritesOwned = true;
            ModPlugin.LogMessage($"Preview corners: no vanilla sprites, mod folder \"{CornerTextureFile}\" sliced into 4 sprites");
        }

        // Loads an image from the mod plugin's Images folder (RGBA32). Last-resort
        // source when the shared vanilla assets are not present.
        private static Texture2D LoadImageTexture(string fileName)
        {
            try
            {
                string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                string texturePath = Path.Combine(pluginDir, "Images", fileName);
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
                ModPlugin.LogMessage($"Preview panel background: vanilla shared texture \"{vanillaBg.name}\" ({vanillaBg.width}x{vanillaBg.height})");
                return panelSprite;
            }
            ModPlugin.LogMessage("Preview panel background: no vanilla background, falling back to mod folder");

            // Fallback: mod-folder cell texture (PDABackground_Mod temporarily disabled),
            // then procedural.
            panelTexture = LoadImageTexture(CellTextureFile);
            panelTextureOwned = panelTexture != null;

            if (panelTexture == null)
            {
                panelTexture = CreateProceduralPanelTexture();
            }
            ModPlugin.LogMessage($"Preview panel background: fallback texture \"{panelTexture.name}\" ({panelTexture.width}x{panelTexture.height})");

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
                backgroundOverlay.enabled = ModPlugin.options.Background;
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
                for (int c = 0; c < cornerSprites.Length; c++)
                {
                    if (cornerSprites[c] != null)
                    {
                        UnityEngine.Object.Destroy(cornerSprites[c]);
                    }
                }
            }
            cornerSprites = null;
            cornerSpritesOwned = false;
            cornerImages = null;

            gridImage = null;
            backgroundOverlay = null;
            panelRect = null;
            contentRect = null;
            containerView = null;
            cachedVanillaContainer = null;
            built = false;
        }
    }
}
