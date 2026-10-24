using GemPop.Core;
using GemPop.Gameplay;
using GemPop.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GemPop.EditorTools
{
    /// <summary>
    /// Builds the LevelScene UI (top bar with goals and moves, fail popup, win celebration) under the scene's
    /// Canvas and wires it to the gameplay scripts. Panels are 9-sliced Kenney UI sprites, so every size here
    /// is a plain canvas-pixel value on the 1080x1920 reference canvas. Re-running replaces the generated objects.
    /// Menu: GemPop > Setup > Build Level UI   (open LevelScene first)
    /// </summary>
    public static class LevelUiBuilder
    {
        private const string Art = "Assets/_Game/Art/";
        private const float BarHeightPx = 330f;   // top bar height on the 1080x1920 canvas
        private const float BarMarginPx = 24f;    // gap between the bar and the screen edges
        private static readonly Vector4 PanelBorder = new Vector4(28, 28, 28, 28);   // 9-slice border of the Kenney "Double" panels
        private static readonly Color DarkText = new Color(0.23f, 0.16f, 0.07f);

        [MenuItem("GemPop/Setup/Build Level UI")]
        public static void Build()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var bootstrap = Object.FindFirstObjectByType<LevelSceneBootstrap>();
            var layout = Object.FindFirstObjectByType<BoardLayout>();
            if (canvas == null || bootstrap == null || layout == null)
            {
                Debug.LogError("[LevelUiBuilder] Open LevelScene (needs a Canvas, a LevelSceneBootstrap and a BoardLayout).");
                return;
            }

            // generated objects are rebuilt from scratch
            foreach (string name in new[] { "TopBar", "FailPopup", "WinCelebration", "DebugLabel" })
            {
                Transform old = canvas.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // Screen Space - Camera (instead of Overlay) so world-space particles with a higher sorting order
            // (celebration stars) can be drawn on top of the UI. Everything on the board stays below 2000.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 2000;

            foreach (string sliced in new[] { "UI/Gameplay/Top/panel.png", "UI/Gameplay/Top/slot.png", "UI/Gameplay/Popup/popup_base.png",
                                              "UI/Gameplay/Popup/popup_ribbon.png", "UI/Menu/button_full.png", "UI/Menu/button_frame_full.png" })
                ItemPrefabBuilder.ConfigureSlicedSprite(Art + sliced, PanelBorder);

            TopBar topBar = BuildTopBar(canvas.transform);
            FailPopup fail = BuildFailPopup(canvas.transform);
            WinCelebration win = BuildWinCelebration(canvas.transform);

            BuildSceneBackground();

            bootstrap.EditorWire(topBar, fail, win);
            // Board area: ~83% of the width, centered vertically between the bottom of the top bar and the
            // bottom of the screen. 96 canvas px = 1 world unit (orthographic size 10 on a 1920 px canvas).
            float barBottomY = 10f - (BarMarginPx + BarHeightPx) / 96f;
            layout.EditorSetArea(9.3f, 11f, new Vector2(0f, (barBottomY - 10f) * 0.5f));

            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(layout);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("[GemPop] Level UI built. Save the scene (Cmd+S).");
        }

        // ------------------------------------------------------------------ top bar

        private static TopBar BuildTopBar(Transform canvas)
        {
            // Blue 9-sliced panel across the top; inside it two grey inset boxes (goal items / moves) with
            // their captions above. Layout in canvas pixels, left box and right box mirrored.
            const float BoxSize = 200f;
            const float BoxCenterX = 270f;   // distance of each box center from the bar center
            const float BoxY = -24f;          // box center relative to the bar center (captions take the top)
            const float CaptionY = 118f;      // caption center relative to the bar center

            RectTransform bar = MakeImage(canvas, "TopBar", LoadSprite("UI/Gameplay/Top/panel.png"), out Image barImage);
            barImage.type = Image.Type.Sliced;
            Anchor(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            bar.offsetMin = new Vector2(BarMarginPx, 0f);
            bar.offsetMax = new Vector2(-BarMarginPx, 0f);
            bar.sizeDelta = new Vector2(bar.sizeDelta.x, BarHeightPx);
            bar.anchoredPosition = new Vector2(0f, -BarMarginPx);

            TMP_Text goalTab = Label(bar, "GoalTabLabel", "Goal", 44, Color.white, new Vector2(0.5f, 0.5f), new Vector2(BoxSize, 70f));
            TMP_Text moveTab = Label(bar, "MoveTabLabel", "Moves", 44, Color.white, new Vector2(0.5f, 0.5f), new Vector2(BoxSize, 70f));
            goalTab.fontStyle = moveTab.fontStyle = FontStyles.Bold;
            goalTab.fontSharedMaterial = moveTab.fontSharedMaterial = OutlineMaterial();
            goalTab.rectTransform.anchoredPosition = new Vector2(-BoxCenterX, CaptionY);
            moveTab.rectTransform.anchoredPosition = new Vector2(BoxCenterX, CaptionY);

            RectTransform goalPanel = MakeImage(bar, "GoalPanel", LoadSprite("UI/Gameplay/Top/slot.png"), out Image goalImage);
            goalImage.type = Image.Type.Sliced;
            Centered(goalPanel, new Vector2(BoxSize, BoxSize), new Vector2(-BoxCenterX, BoxY));

            var slots = new GoalSlot[3];
            for (int i = 0; i < slots.Length; i++) slots[i] = BuildGoalSlot(goalPanel, i);

            RectTransform movePanel = MakeImage(bar, "MovePanel", LoadSprite("UI/Gameplay/Top/slot.png"), out Image moveImage);
            moveImage.type = Image.Type.Sliced;
            Centered(movePanel, new Vector2(BoxSize, BoxSize), new Vector2(BoxCenterX, BoxY));

            TMP_Text moves = Label(movePanel, "MovesText", "0", 96, DarkText, new Vector2(0.5f, 0.5f), new Vector2(BoxSize, BoxSize));
            moves.fontStyle = FontStyles.Bold;

            var topBar = bar.gameObject.AddComponent<TopBar>();
            topBar.EditorWire(moves, slots,
                LoadSprite("Obstacles/Crate/crate_01.png"),
                LoadSprite("Obstacles/Rock/rock.png"),
                LoadSprite("Obstacles/Chest/Coin.png"),
                new[] { goalTab, moveTab });
            return topBar;
        }

        private static GoalSlot BuildGoalSlot(Transform parent, int index)
        {
            RectTransform slot = MakeImage(parent, $"GoalSlot{index}", null, out Image icon);
            icon.preserveAspect = true;
            Centered(slot, new Vector2(76f, 76f), Vector2.zero);

            TMP_Text count = Label(slot, "Count", "0", 36, Color.white, new Vector2(1f, 0f), new Vector2(76f, 40f));
            count.fontStyle = FontStyles.Bold;
            count.fontSharedMaterial = OutlineMaterial();      // white + dark outline stays legible over any icon
            count.alignment = TextAlignmentOptions.BottomRight;
            count.rectTransform.pivot = new Vector2(1f, 0f);
            count.rectTransform.anchoredPosition = new Vector2(10f, -8f);

            RectTransform check = MakeImage(slot, "Check", LoadSprite("UI/Gameplay/Top/goal_check.png"), out Image checkImage);
            checkImage.preserveAspect = true;
            Anchor(check, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            check.sizeDelta = new Vector2(40f, 37f);
            check.anchoredPosition = new Vector2(10f, -8f);
            check.gameObject.SetActive(false);

            var goalSlot = slot.gameObject.AddComponent<GoalSlot>();
            goalSlot.EditorWire(icon, count, check.gameObject);
            slot.gameObject.SetActive(false);
            return goalSlot;
        }

        // ------------------------------------------------------------------ fail popup

        private static FailPopup BuildFailPopup(Transform canvas)
        {
            RectTransform overlay = Overlay(canvas, "FailPopup", 0.6f);

            RectTransform panel = MakeImage(overlay, "Panel", LoadSprite("UI/Gameplay/Popup/popup_base.png"), out Image panelImage);
            panelImage.type = Image.Type.Sliced;
            Centered(panel, new Vector2(760f, 760f), new Vector2(0f, 0f));

            RectTransform ribbon = MakeImage(panel, "Ribbon", LoadSprite("UI/Gameplay/Popup/popup_ribbon.png"), out Image ribbonImage);
            ribbonImage.type = Image.Type.Sliced;
            Anchor(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f));
            ribbon.sizeDelta = new Vector2(600f, 130f);
            ribbon.anchoredPosition = new Vector2(0f, 20f);
            TMP_Text title = Label(ribbon, "Title", "Level Failed", 56, Color.white, new Vector2(0.5f, 0.5f), new Vector2(560f, 100f));
            title.fontStyle = FontStyles.Bold;

            RectTransform closeRect = MakeImage(panel, "CloseButton", LoadSprite("UI/Gameplay/Popup/close_button.png"), out Image closeImage);
            Anchor(closeRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
            closeRect.sizeDelta = new Vector2(110f, 110f);
            closeRect.anchoredPosition = new Vector2(20f, 20f);
            closeImage.raycastTarget = true;            // MakeImage defaults to false; a button must catch clicks
            Button close = closeRect.gameObject.AddComponent<Button>();
            close.targetGraphic = closeImage;

            Button tryAgain = TextButton(panel, "TryAgainButton", "Try Again", new Vector2(0f, -240f));

            TMP_Text message = Label(panel, "Message", "You ran out of moves", 44, DarkText, new Vector2(0.5f, 0.5f), new Vector2(560f, 160f));
            message.fontStyle = FontStyles.Bold;

            var popup = overlay.gameObject.AddComponent<FailPopup>();
            popup.EditorWire(panel, close, tryAgain);
            overlay.gameObject.SetActive(false);
            return popup;
        }

        // ------------------------------------------------------------------ win celebration

        private static WinCelebration BuildWinCelebration(Transform canvas)
        {
            RectTransform overlay = Overlay(canvas, "WinCelebration", 0.9f);
            Button catcher = overlay.gameObject.AddComponent<Button>();
            catcher.transition = Selectable.Transition.None;

            RectTransform star = MakeImage(overlay, "Star", LoadSprite("UI/Gameplay/Celebration/star.png"), out Image starImage);
            starImage.preserveAspect = true;
            Centered(star, new Vector2(520f, 490f), new Vector2(0f, -40f));

            TMP_Text title = Label(overlay, "Title", "Perfect!", 120, Color.white, new Vector2(0.5f, 0.72f), new Vector2(900f, 180f));
            title.fontStyle = FontStyles.Bold;

            Label(overlay, "Hint", "Tap To Continue", 48, Color.white, new Vector2(0.5f, 0.18f), new Vector2(700f, 80f));

            var win = overlay.gameObject.AddComponent<WinCelebration>();
            win.EditorWire(catcher, star, title.rectTransform,
                LoadSprite("UI/Gameplay/Celebration/star.png"),
                LoadSprite("UI/Gameplay/Celebration/Particles/additive_particle_star.png"));
            overlay.gameObject.SetActive(false);
            return win;
        }

        // ------------------------------------------------------------------ scene background

        /// <summary>
        /// The menu's area image behind the board. It is a world-space sprite (not a Canvas image) because the
        /// Canvas is Screen Space Overlay and would cover the board; drawn behind everything at order -100.
        /// </summary>
        private static void BuildSceneBackground()
        {
            var old = GameObject.Find("SceneBackground");
            if (old != null) Object.DestroyImmediate(old);

            var go = new GameObject("SceneBackground");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Menu/background.png");
            sr.sortingOrder = -100;

            Camera cam = Camera.main;
            float viewHeight = cam != null ? cam.orthographicSize * 2f : 20f;
            float viewWidth = viewHeight * 9f / 16f;
            Vector2 spriteSize = sr.sprite.bounds.size;
            float scale = Mathf.Max(viewHeight / spriteSize.y, viewWidth / spriteSize.x);   // cover the view
            go.transform.localScale = Vector3.one * scale;
            go.transform.position = Vector3.zero;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>TMP's bundled outline preset; the SDF shader only draws outlines when its OUTLINE_ON keyword is set.</summary>
        internal static Material OutlineMaterial()
        {
            const string path = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) Debug.LogWarning("[LevelUiBuilder] Outline material preset not found (import TMP Essentials): " + path);
            return material;
        }

        internal static Sprite LoadSprite(string relativePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + relativePath);
            if (sprite == null) Debug.LogError("[LevelUiBuilder] Sprite not found: " + Art + relativePath);
            return sprite;
        }

        internal static RectTransform MakeImage(Transform parent, string name, Sprite sprite, out Image image)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        internal static TMP_Text Label(Transform parent, string name, string text, float size, Color color, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Anchor(rect, anchor, anchor, new Vector2(0.5f, 0.5f));
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button TextButton(Transform parent, string name, string text, Vector2 anchoredPosition)
        {
            RectTransform rect = MakeImage(parent, name, LoadSprite("UI/Menu/button_frame_full.png"), out Image frame);
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.raycastTarget = true;
            Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rect.sizeDelta = new Vector2(440f, 150f);
            rect.anchoredPosition = anchoredPosition;

            RectTransform fill = MakeImage(rect, "Fill", LoadSprite("UI/Menu/button_full.png"), out Image fillImage);
            fillImage.type = UnityEngine.UI.Image.Type.Sliced;
            Anchor(fill, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;

            TMP_Text label = Label(rect, "Label", text, 56, Color.white, new Vector2(0.5f, 0.5f), new Vector2(400f, 120f));
            label.fontStyle = FontStyles.Bold;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            return button;
        }

        private static RectTransform Overlay(Transform canvas, string name, float alpha)
        {
            RectTransform overlay = MakeImage(canvas, name, null, out Image image);
            image.color = new Color(0f, 0f, 0f, alpha);
            image.raycastTarget = true;             // blocks taps on the board underneath
            Anchor(overlay, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            return overlay;
        }

        internal static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
        }

        internal static void Centered(RectTransform rect, Vector2 size, Vector2 offset)
        {
            Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }
    }
}
