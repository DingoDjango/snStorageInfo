using HarmonyLib;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore;

namespace StorageInfo
{
    public static class StorageDetailUI
    {
        // Max grid size before scaling down. Increase to allow larger containers at full size.
        private const float MaxPreviewWidth = 320f;
        private const float MaxPreviewHeight = 400f;

        // Space between panel border and grid content.
        private const float PanelPadding = 10f;

        // Must match vanilla uGUI_ItemsContainer cell size.
        private const int CellSize = 71;

        private static GameObject root;
        private static RectTransform panelRect;
        private static RectTransform contentRect;
        private static RectTransform borderRect;
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
            ApplyOffset();
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
                UnityEngine.Object.Destroy(root);
                root = null;
            }

            if (panelSprite != null)
            {
                UnityEngine.Object.Destroy(panelSprite);
                panelSprite = null;
            }

            if (gridImage != null)
            {
                UnityEngine.Object.Destroy(gridImage);
                gridImage = null;
            }

            CleanupBorder();
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

            // Apply anchor based on mod options
            float anchorX = ModPlugin.options.PanelAnchor == AnchorPreset.Custom
                ? ModPlugin.options.CustomAnchorX / 100f
                : GetAnchorForPreset(ModPlugin.options.PanelAnchor);
            float anchorY = ModPlugin.options.PanelAnchor == AnchorPreset.Custom
                ? ModPlugin.options.CustomAnchorY / 100f
                : GetAnchorForPreset(ModPlugin.options.PanelAnchor);

            panelRect.anchorMin = new Vector2(anchorX, anchorY);
            panelRect.anchorMax = new Vector2(anchorX, anchorY);
            panelRect.pivot = new Vector2(0.5f, 0.5f);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.raycastTarget = false;

            // Add border for game UI integration (only if mod option enabled)
            if (ModPlugin.options.UseGamePanelStyle)
            {
                GameObject borderObj = new GameObject("Border");
                borderObj.layer = uiLayer;
                borderObj.transform.SetParent(panelRect, false);
                
                RectTransform borderRect = borderObj.AddComponent<RectTransform>();
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.offsetMin = Vector2.zero;
                borderRect.offsetMax = Vector2.zero;
                borderRect.sizeDelta = panelRect.sizeDelta;
                
                // Add outline effect for border (cyan tint matching game UI)
                Outline outline = borderObj.AddComponent<Outline>();
                outline.effectDistance = new Vector2(1.5f, 1.5f);
                outline.effectColor = new Color(0.05f, 0.3f, 0.6f, 0.8f);
            }

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
                
                // Apply grid alpha from mod options
                float gridAlpha = ModPlugin.options.GridAlpha / 100f;
                Color newColor = view.grid.color;
                newColor.a = Mathf.Clamp(newColor.a * gridAlpha, 0f, 1f);
                gridImage.color = newColor;
                
                return;
            }

            uGUI_ItemsContainer container = uGUI.main.GetComponentInChildren<uGUI_ItemsContainer>(true);
            if (container != null && container.grid != null)
            {
                gridImage.texture = container.grid.texture;
                gridImage.material = container.grid.material;
                gridImage.color = container.grid.color;
                
                // Apply grid alpha from mod options
                float gridAlpha = ModPlugin.options.GridAlpha / 100f;
                Color newColor = container.grid.color;
                newColor.a = Mathf.Clamp(newColor.a * gridAlpha, 0f, 1f);
                gridImage.color = newColor;
                
                return;
            }
        }

        // Load texture from file path (PNG only for now)
        private static Texture2D LoadTextureFromFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                return null;

            // For BepInEx plugins, we need to use Resources folder or AssetBundle
            // Since we can't access Unity Resources directly, fall back to procedural grid
            return null;
        }

        // Generate proper panel background for better UI integration.
        private static Sprite LoadPanelSprite()
        {
            if (panelSprite != null)
                return panelSprite;

            // Try to load from mod assets first - PDABackground.png
            // Load from plugin DLL path since BepInEx doesn't have access to Unity Resources folder
            string dllPath = AppDomain.CurrentDomain.BaseDirectory;
            string texturePath = Path.Combine(dllPath, "Images", "PDABackground.png");
            Texture2D texture = null;
            
            try
            {
                texture = LoadTextureFromFile(texturePath);
            }
            catch
            {
                // File not found or load failed, fall back to procedural grid
            }
            
            if (texture == null)
            {
                int gridSize = 32;
                texture = new Texture2D(gridSize, gridSize, TextureFormat.RGBA32, false);

                for (int y = 0; y < gridSize; y++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        float gridAlpha = 0.25f; // Slightly more opaque than original

                        if (x % 4 == 0 || y % 4 == 0)
                            gridAlpha *= 1.5f;

                        texture.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Clamp(gridAlpha, 0f, 1f)));
                    }
                }
                texture.Apply();
            }

            // Create sprite with proper settings
            panelSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            
            return panelSprite;
        }

        // Helper method to get anchor values based on preset
        private static float GetAnchorForPreset(AnchorPreset preset)
        {
            switch (preset)
            {
                case AnchorPreset.TopLeft: return 0f;
                case AnchorPreset.TopCenter: return 0.5f;
                case AnchorPreset.TopRight: return 1f;
                case AnchorPreset.CenterLeft: return 0f;
                case AnchorPreset.Center: return 0.5f;
                case AnchorPreset.CenterRight: return 1f;
                case AnchorPreset.BottomLeft: return 0f;
                case AnchorPreset.BottomCenter: return 0.5f;
                case AnchorPreset.BottomRight: return 1f;
                default: return 0.75f; // TopRight as default fallback
            }
        }

        // Apply panel appearance with mod options support
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
                
                // Apply opacity from mod options
                float opacity = ModPlugin.options.UseGamePanelStyle ? ModPlugin.options.PanelOpacity : 1f;
                panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, opacity);
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

        private static void ApplyOffset()
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            if (rootRect == null || rootRect.parent == null)
                return;

            // Apply offset from mod options (percentage of panel size)
            float offsetX = (ModPlugin.options.OffsetX / 100f) * panelRect.sizeDelta.x;
            float offsetY = (ModPlugin.options.OffsetY / 100f) * panelRect.sizeDelta.y;
            panelRect.anchoredPosition = new Vector2(offsetX, offsetY);
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

            // Clean up border if it exists
            if (borderRect != null)
            {
                if (borderRect.gameObject != null)
                {
                    UnityEngine.Object.Destroy(borderRect.gameObject);
                }
                borderRect = null;
            }

            boundContainer = null;
        }

        private static void CleanupBorder()
        {
            if (borderRect != null)
            {
                if (borderRect.gameObject != null)
                {
                    UnityEngine.Object.Destroy(borderRect.gameObject);
                }
                borderRect = null;
            }
        }
    }
}