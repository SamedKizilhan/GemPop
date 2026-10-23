using GemPop.Items;
using UnityEngine;

namespace GemPop.Gameplay
{
    /// <summary>
    /// One asset that points at every item prefab. The factory needs a single reference instead of six.
    /// Created by GemPop > Setup > Build Item Prefabs.
    /// </summary>
    [CreateAssetMenu(menuName = "GemPop/Item Prefab Set")]
    public class ItemPrefabSet : ScriptableObject
    {
        [SerializeField] private Cube cube;
        [SerializeField] private Rocket rocket;
        [SerializeField] private Tnt tnt;
        [SerializeField] private Vase vase;
        [SerializeField] private Stone stone;
        [SerializeField] private ChaliceBox chaliceBox;

        [Header("Particles")]
        [SerializeField] private Sprite[] cubeParticles = new Sprite[4];   // indexed by CubeColor
        [SerializeField] private Sprite[] vaseParticles;
        [SerializeField] private Sprite[] stoneParticles;
        [SerializeField] private Sprite[] tntParticles;
        [SerializeField] private Sprite[] chaliceBoxParticles;
        [SerializeField] private Sprite[] chaliceDoorParticles;
        [SerializeField] private Sprite smokeParticle;
        [SerializeField] private Sprite sparkParticle;

        [Header("Rocket explosion parts")]
        [SerializeField] private Sprite rocketPartLeft;
        [SerializeField] private Sprite rocketPartRight;
        [SerializeField] private Sprite rocketPartTop;
        [SerializeField] private Sprite rocketPartBottom;

        public Cube Cube => cube;
        public Rocket Rocket => rocket;
        public Tnt Tnt => tnt;
        public Vase Vase => vase;
        public Stone Stone => stone;
        public ChaliceBox ChaliceBox => chaliceBox;

        public Sprite[] CubeParticle(CubeColor color) => new[] { cubeParticles[(int)color] };
        public Sprite[] VaseParticles => vaseParticles;
        public Sprite[] StoneParticles => stoneParticles;
        public Sprite[] TntParticles => tntParticles;
        public Sprite[] ChaliceBoxParticles => chaliceBoxParticles;
        public Sprite[] ChaliceDoorParticles => chaliceDoorParticles;
        public Sprite SmokeParticle => smokeParticle;
        public Sprite SparkParticle => sparkParticle;
        public Sprite TntRing => tntParticles != null && tntParticles.Length > 0 ? tntParticles[0] : null;
        public Sprite TntFlash => tntParticles != null && tntParticles.Length > 1 ? tntParticles[1] : null;

        /// <summary>Sprite of the rocket half that flies in 'direction'.</summary>
        public Sprite RocketPart(Vector2Int direction)
        {
            if (direction == Vector2Int.left) return rocketPartLeft;
            if (direction == Vector2Int.right) return rocketPartRight;
            if (direction == Vector2Int.up) return rocketPartTop;
            return rocketPartBottom;
        }

#if UNITY_EDITOR
        public void EditorAssign(Cube c, Rocket r, Tnt t, Vase v, Stone s, ChaliceBox cb)
        {
            cube = c; rocket = r; tnt = t; vase = v; stone = s; chaliceBox = cb;
        }

        public void EditorAssignRocketParts(Sprite left, Sprite right, Sprite top, Sprite bottom)
        {
            rocketPartLeft = left; rocketPartRight = right; rocketPartTop = top; rocketPartBottom = bottom;
        }

        public void EditorAssignParticles(Sprite[] cubes, Sprite[] vase, Sprite[] stone, Sprite[] tnt,
                                          Sprite[] chaliceBox, Sprite[] chaliceDoor, Sprite smoke, Sprite spark)
        {
            cubeParticles = cubes; vaseParticles = vase; stoneParticles = stone; tntParticles = tnt;
            chaliceBoxParticles = chaliceBox; chaliceDoorParticles = chaliceDoor; smokeParticle = smoke; sparkParticle = spark;
        }
#endif
    }
}
