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
        [SerializeField] private Bomb bomb;
        [SerializeField] private Crate crate;
        [SerializeField] private Rock rock;
        [SerializeField] private Chest chest;

        [Header("Particles")]
        [SerializeField] private Sprite[] cubeParticles = new Sprite[4];   // indexed by CubeColor
        [SerializeField] private Sprite[] crateParticles;
        [SerializeField] private Sprite[] rockParticles;
        [SerializeField] private Sprite[] bombParticles;
        [SerializeField] private Sprite[] chestParticles;
        [SerializeField] private Sprite[] coinDoorParticles;
        [SerializeField] private Sprite smokeParticle;
        [SerializeField] private Sprite sparkParticle;

        [Header("Rocket explosion parts")]
        [SerializeField] private Sprite rocketPartLeft;
        [SerializeField] private Sprite rocketPartRight;
        [SerializeField] private Sprite rocketPartTop;
        [SerializeField] private Sprite rocketPartBottom;

        public Cube Cube => cube;
        public Rocket Rocket => rocket;
        public Bomb Bomb => bomb;
        public Crate Crate => crate;
        public Rock Rock => rock;
        public Chest Chest => chest;

        public Sprite[] CubeParticle(CubeColor color) => new[] { cubeParticles[(int)color] };
        public Sprite[] CrateParticles => crateParticles;
        public Sprite[] RockParticles => rockParticles;
        public Sprite[] BombParticles => bombParticles;
        public Sprite[] ChestParticles => chestParticles;
        public Sprite[] CoinDoorParticles => coinDoorParticles;
        public Sprite SmokeParticle => smokeParticle;
        public Sprite SparkParticle => sparkParticle;
        public Sprite BombRing => bombParticles != null && bombParticles.Length > 0 ? bombParticles[0] : null;
        public Sprite BombFlash => bombParticles != null && bombParticles.Length > 1 ? bombParticles[1] : null;

        /// <summary>Sprite of the rocket half that flies in 'direction'.</summary>
        public Sprite RocketPart(Vector2Int direction)
        {
            if (direction == Vector2Int.left) return rocketPartLeft;
            if (direction == Vector2Int.right) return rocketPartRight;
            if (direction == Vector2Int.up) return rocketPartTop;
            return rocketPartBottom;
        }

#if UNITY_EDITOR
        public void EditorAssign(Cube c, Rocket r, Bomb t, Crate v, Rock s, Chest cb)
        {
            cube = c; rocket = r; bomb = t; crate = v; rock = s; chest = cb;
        }

        public void EditorAssignRocketParts(Sprite left, Sprite right, Sprite top, Sprite bottom)
        {
            rocketPartLeft = left; rocketPartRight = right; rocketPartTop = top; rocketPartBottom = bottom;
        }

        public void EditorAssignParticles(Sprite[] cubes, Sprite[] crate, Sprite[] rock, Sprite[] bomb,
                                          Sprite[] chest, Sprite[] coinDoor, Sprite smoke, Sprite spark)
        {
            cubeParticles = cubes; crateParticles = crate; rockParticles = rock; bombParticles = bomb;
            chestParticles = chest; coinDoorParticles = coinDoor; smokeParticle = smoke; sparkParticle = spark;
        }
#endif
    }
}
