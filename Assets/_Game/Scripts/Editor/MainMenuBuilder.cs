using GemPop.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GemPop.EditorTools
{
    /// <summary>
    /// Dresses up MainScene: a big "GemPop" title with a warm vertex gradient, a tagline and a row of
    /// floating item icons. Re-running replaces the generated objects; the LevelButton is left alone.
    /// Menu: GemPop > Setup > Build Main Menu   (open MainScene first)
    /// </summary>
    public static class MainMenuBuilder
    {
        [MenuItem("GemPop/Setup/Build Main Menu")]
        public static void Build()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null || Object.FindFirstObjectByType<LevelButton>() == null)
            {
                Debug.LogError("[MainMenuBuilder] Open MainScene (needs a Canvas and the LevelButton).");
                return;
            }
            Transform root = canvas.transform;
            foreach (string name in new[] { "Title", "Tagline", "Decor" })
            {
                Transform old = root.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // Title: thick white outline + yellow->orange gradient, like a casual-game logo.
            TMP_Text title = LevelUiBuilder.Label(root, "Title", "GemPop", 190, Color.white, new Vector2(0.5f, 0.76f), new Vector2(1000f, 260f));
            title.fontStyle = FontStyles.Bold;
            title.fontSharedMaterial = LevelUiBuilder.OutlineMaterial();
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(
                new Color(1f, 0.95f, 0.55f), new Color(1f, 0.95f, 0.55f),
                new Color(1f, 0.55f, 0.15f), new Color(1f, 0.55f, 0.15f));
            title.characterSpacing = -4f;

            TMP_Text tagline = LevelUiBuilder.Label(root, "Tagline", "tap  ·  match  ·  blast", 48, new Color(1f, 1f, 1f, 0.8f), new Vector2(0.5f, 0.685f), new Vector2(800f, 80f));
            tagline.characterSpacing = 6f;

            // Floating icons scattered between the tagline and the button (bobbed at runtime by MenuDecor).
            var decor = new GameObject("Decor", typeof(RectTransform)).GetComponent<RectTransform>();
            decor.SetParent(root, false);
            LevelUiBuilder.Anchor(decor, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f));
            decor.sizeDelta = new Vector2(1000f, 700f);
            // sprite, x, y (canvas px from the decor centre), size, rotation
            (string sprite, float x, float y, float size, float rot)[] items =
            {
                ("Cubes/DefaultState/red.png",            -330f,  210f, 150f,  -12f),
                ("SpecialItems/Rocket/vertical_rocket.png", 300f,  230f, 120f,   18f),
                ("Cubes/DefaultState/blue.png",             60f,  120f, 110f,    8f),
                ("Obstacles/Chest/Coin.png",              -140f,   20f,  96f,    0f),
                ("SpecialItems/Bomb/Bomb.png",             360f,  -40f, 130f,  -10f),
                ("Cubes/DefaultState/green.png",          -380f, -110f, 120f,   14f),
                ("Cubes/DefaultState/yellow.png",          150f, -200f, 160f,   -6f),
                ("Obstacles/Chest/Coin.png",               -90f, -260f,  80f,    0f),
            };
            foreach (var it in items)
            {
                RectTransform icon = LevelUiBuilder.MakeImage(decor, "Icon", LevelUiBuilder.LoadSprite(it.sprite), out Image image);
                image.preserveAspect = true;
                LevelUiBuilder.Centered(icon, new Vector2(it.size, it.size), new Vector2(it.x, it.y));
                icon.localRotation = Quaternion.Euler(0f, 0f, it.rot);
            }
            decor.gameObject.AddComponent<MenuDecor>();

            // Keep the button on top of the decor in draw order.
            Object.FindFirstObjectByType<LevelButton>().transform.SetAsLastSibling();

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("[GemPop] Main menu built. Save the scene (Cmd+S).");
        }
    }
}
