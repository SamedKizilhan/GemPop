using GemPop.Gameplay;
using GemPop.Items;
using UnityEngine;

namespace GemPop.Systems.Fx
{
    /// <summary>
    /// Decides WHICH particle effect an event gets (the item type picks the sprites); ParticleFx decides HOW
    /// it looks. Kept out of BoardController so gameplay code never mentions particles.
    /// </summary>
    public class BoardFx
    {
        private readonly ItemPrefabSet _assets;
        private readonly BoardLayout _layout;

        public BoardFx(ItemPrefabSet assets, BoardLayout layout)
        {
            _assets = assets;
            _layout = layout;
        }

        private float Size => _layout.CellSize * 0.45f;
        private static readonly Color SparkColor = new Color(1f, 0.85f, 0.3f);

        public void ItemDestroyed(GridItem item)
        {
            Vector3 at = item.transform.position;
            switch (item)
            {
                case Cube cube:       ParticleFx.Burst(at, _assets.CubeParticle(cube.Color), Size, 8); break;
                case Crate:            ParticleFx.Burst(at, _assets.CrateParticles, Size, 9); break;
                case Rock:           ParticleFx.Burst(at, _assets.RockParticles, Size, 9); break;
                case Chest:      ParticleFx.Burst(at, _assets.ChestParticles, Size * 1.3f, 15, 6f); break;
                case Bomb:             break;   // its explosion (AreaExplosion) is the effect
                case Rocket:          ParticleFx.Sparkles(at, _assets.SparkParticle, Size * 0.6f, 18, SparkColor); break;
            }
        }

        /// <summary>Crate cracked / coin box door opened: a smaller puff on the item.</summary>
        public void ItemDamaged(GridItem item)
        {
            Vector3 at = item.transform.position;
            switch (item)
            {
                case Crate:       ParticleFx.Burst(at, _assets.CrateParticles, Size * 0.7f, 4, 3f); break;
                case Chest: ParticleFx.Burst(at, _assets.CoinDoorParticles, Size, 10, 5f); break;
            }
        }

        /// <summary>Bomb (radius 2) and Bomb-Bomb (radius 3): a bright yellow-orange burst as wide as the damaged area.</summary>
        public void AreaExplosion(Vector3 at, int radius)
        {
            float worldRadius = (radius + 0.5f) * _layout.CellSize;
            ParticleFx.Blast(at, _assets.BombFlash, worldRadius);
        }

        public GameObject RocketTrail(Transform part) => ParticleFx.Trail(part, _assets.SmokeParticle, _assets.SparkParticle, Size);
    }
}
