using System.IO;
using GemPop.Gameplay;
using GemPop.Items;
using UnityEditor;
using UnityEngine;

namespace GemPop.EditorTools
{
    /// <summary>
    /// One-click project setup: creates the item prefabs from the case-study sprites and an ItemPrefabSet
    /// asset that references them. Re-running overwrites the prefabs (they are generated, not hand-edited).
    /// Menu: GemPop > Setup > Build Item Prefabs
    /// </summary>
    public static class ItemPrefabBuilder
    {
        private const string Art = "Assets/_Game/Art/";
        private const string Out = "Assets/_Game/Prefabs/";

        [MenuItem("GemPop/Setup/Build Item Prefabs")]
        public static void Build()
        {
            if (!Directory.Exists(Out)) Directory.CreateDirectory(Out);

            Cube cube = BuildCube();
            Rocket rocket = BuildRocket();
            Tnt tnt = BuildTnt();
            Vase vase = BuildVase();
            Stone stone = BuildStone();
            ChaliceBox box = BuildChaliceBox();

            string setPath = Out + "ItemPrefabSet.asset";
            var set = AssetDatabase.LoadAssetAtPath<ItemPrefabSet>(setPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<ItemPrefabSet>();
                AssetDatabase.CreateAsset(set, setPath);
            }
            set.EditorAssign(cube, rocket, tnt, vase, stone, box);
            set.EditorAssignParticles(
                new[]
                {
                    LoadSprite("Cubes/Particles/particle_red.png"),
                    LoadSprite("Cubes/Particles/particle_green.png"),
                    LoadSprite("Cubes/Particles/particle_blue.png"),
                    LoadSprite("Cubes/Particles/particle_yellow.png"),
                },
                LoadMany("Obstacles/Vase/Particles/particle_vase_0{0}.png", 3),
                LoadMany("Obstacles/Stone/Particles/particle_stone_0{0}.png", 3),
                LoadMany("SpecialItems/TNT/Particles/particle_tnt_0{0}.png", 2),
                LoadMany("Obstacles/ChaliceBox/Particles/BaseParticles/chalice_box_particle_0{0}.png", 5),
                LoadMany("Obstacles/ChaliceBox/Particles/DoorParticles/chalice_box_door_particle_0{0}.png", 4),
                LoadSprite("SpecialItems/Rocket/Particles/particle_smoke.png"),
                LoadSprite("SpecialItems/Rocket/Particles/particle_star.png"));
            set.EditorAssignRocketParts(
                LoadSprite("SpecialItems/Rocket/horizontal_rocket_part_left.png"),
                LoadSprite("SpecialItems/Rocket/horizontal_rocket_part_right.png"),
                LoadSprite("SpecialItems/Rocket/vertical_rocket_part_top.png"),
                LoadSprite("SpecialItems/Rocket/vertical_rocket_part_bottom.png"));
            EditorUtility.SetDirty(set);

            ConfigureSlicedSprite(Art + "UI/Gameplay/grid_background.png", new Vector4(24, 24, 24, 24));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GemPop] Item prefabs and ItemPrefabSet built in " + Out);
        }

        // ---------- individual prefabs ----------

        private static Cube BuildCube()
        {
            GameObject go = NewSpriteObject("Cube", LoadSprite("Cubes/DefaultState/blue.png"), out SpriteRenderer sr);
            Cube cube = go.AddComponent<Cube>();
            cube.EditorSetRenderer(sr);
            cube.EditorSetCellFill(0.94f);     // Kenney tiles have no transparent padding: leave a small gap
            cube.EditorSetSkins(new[]
            {
                Skin(CubeColor.Red,    "red"),
                Skin(CubeColor.Green,  "green"),
                Skin(CubeColor.Blue,   "blue"),
                Skin(CubeColor.Yellow, "yellow"),
            });
            return SavePrefab<Cube>(go);
        }

        private static Cube.Skin Skin(CubeColor color, string n) => new Cube.Skin
        {
            color = color,
            normal = LoadSprite($"Cubes/DefaultState/{n}.png"),
            rocketHint = LoadSprite($"Cubes/RocketState/{n}_rocket.png"),
            tntHint = LoadSprite($"Cubes/TntState/{n}_tnt.png"),
        };

        private static Rocket BuildRocket()
        {
            GameObject go = NewSpriteObject("Rocket", LoadSprite("SpecialItems/Rocket/horizontal_rocket.png"), out SpriteRenderer sr);
            Rocket rocket = go.AddComponent<Rocket>();
            rocket.EditorSetRenderer(sr);
            rocket.EditorSetSprites(LoadSprite("SpecialItems/Rocket/horizontal_rocket.png"),
                                    LoadSprite("SpecialItems/Rocket/vertical_rocket.png"));
            rocket.EditorSetCellFill(0.92f);
            rocket.EditorSetVisualOffset(Vector2.zero);
            return SavePrefab<Rocket>(go);
        }

        private static Tnt BuildTnt()
        {
            GameObject go = NewSpriteObject("Tnt", LoadSprite("SpecialItems/TNT/TNT.png"), out SpriteRenderer sr);
            Tnt tnt = go.AddComponent<Tnt>();
            tnt.EditorSetRenderer(sr);
            tnt.EditorSetCellFill(0.9f);
            tnt.EditorSetVisualOffset(Vector2.zero);
            return SavePrefab<Tnt>(go);
        }

        private static Vase BuildVase()
        {
            GameObject go = NewSpriteObject("Vase", LoadSprite("Obstacles/Vase/vase_01.png"), out SpriteRenderer sr);
            Vase vase = go.AddComponent<Vase>();
            vase.EditorSetRenderer(sr);
            vase.EditorSetCellFill(0.92f);
            vase.EditorSetSprites(LoadSprite("Obstacles/Vase/vase_01.png"), LoadSprite("Obstacles/Vase/vase_02.png"));
            return SavePrefab<Vase>(go);
        }

        private static Stone BuildStone()
        {
            GameObject go = NewSpriteObject("Stone", LoadSprite("Obstacles/Stone/stone.png"), out SpriteRenderer sr);
            Stone stone = go.AddComponent<Stone>();
            stone.EditorSetRenderer(sr);
            stone.EditorSetCellFill(0.92f);
            return SavePrefab<Stone>(go);
        }

        private static ChaliceBox BuildChaliceBox()
        {
            var root = new GameObject("ChaliceBox");
            SpriteRenderer bg = Child(root, "Background", LoadSprite("Obstacles/ChaliceBox/ChaliceBoxBg.png"));

            // 10 coins inside the box, two rows of five, laid out relative to the background sprite's
            // size so any art works: coin width = 26% of the box, row centers at +-30% of the half height.
            Sprite chaliceSprite = LoadSprite("Obstacles/ChaliceBox/Chalice.png");
            var shelves = new GameObject("Chalices");
            shelves.transform.SetParent(root.transform, false);
            var chalices = new SpriteRenderer[ChaliceBox.ChalicesPerBox];
            Vector2 boxHalf = bg.sprite.bounds.extents;
            float coinScale = boxHalf.x * 2f * 0.26f / chaliceSprite.bounds.size.x;
            float[] colX = { -0.62f, -0.31f, 0f, 0.31f, 0.62f };
            float[] rowY = { 0.30f, -0.30f };
            for (int i = 0; i < chalices.Length; i++)
            {
                SpriteRenderer sr = Child(shelves, $"Chalice{i}", chaliceSprite);
                sr.transform.localPosition = new Vector3(colX[i % 5] * boxHalf.x, rowY[i / 5] * boxHalf.y, 0f);
                sr.transform.localScale = Vector3.one * coinScale;
                chalices[i] = sr;
            }

            SpriteRenderer doors = Child(root, "Doors", LoadSprite("Obstacles/ChaliceBox/ChaliceBoxDoors.png"));

            ChaliceBox box = root.AddComponent<ChaliceBox>();
            box.EditorSetRenderers(bg, doors, chalices);
            box.EditorSetMaxHealth(ChaliceBox.ChalicesPerBox);
            // Slightly smaller than 2x2 and nudged down, so the box never touches the cube above it
            // (cubes overhang their cell by 0.06) nor a box stacked on top of it.
            box.EditorSetCellFill(0.96f);
            box.EditorSetVerticalStretch(1f);
            box.EditorSetVisualOffset(Vector2.zero);
            return SavePrefab<ChaliceBox>(root);
        }

        // ---------- helpers ----------

        private static Sprite[] LoadMany(string pattern, int count)
        {
            var result = new Sprite[count];
            for (int i = 0; i < count; i++) result[i] = LoadSprite(string.Format(pattern, i + 1));
            return result;
        }

        private static Sprite LoadSprite(string relativePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + relativePath);
            if (sprite == null) Debug.LogError("[ItemPrefabBuilder] Sprite not found: " + Art + relativePath);
            return sprite;
        }

        private static GameObject NewSpriteObject(string name, Sprite sprite, out SpriteRenderer sr)
        {
            var go = new GameObject(name);
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            return go;
        }

        private static SpriteRenderer Child(GameObject parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            return sr;
        }

        private static T SavePrefab<T>(GameObject go) where T : Component
        {
            string path = Out + go.name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<T>();
        }

        /// <summary>
        /// 9-slicing needs three import settings: Sprite Mode = Single (in Multiple mode the border belongs to
        /// each sub-sprite, not to the importer), Mesh Type = Full Rect (Tight meshes cannot be sliced) and
        /// the border itself (which pixels stay fixed).
        /// </summary>
        internal static void ConfigureSlicedSprite(string assetPath, Vector4 border)
        {
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer)) return;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool changed = importer.spriteBorder != border
                        || settings.spriteMeshType != SpriteMeshType.FullRect
                        || importer.spriteImportMode != SpriteImportMode.Single;
            if (!changed) return;

            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
            Debug.Log("[ItemPrefabBuilder] Re-imported as Single/FullRect/9-sliced: " + assetPath);
        }
    }
}
