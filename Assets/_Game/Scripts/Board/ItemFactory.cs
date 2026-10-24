using System.Collections.Generic;
using GemPop.Data;
using GemPop.Items;
using UnityEngine;

namespace GemPop.Gameplay
{
    /// <summary>
    /// The only place that instantiates items. Callers say WHAT they want (a red cube, a vertical rocket);
    /// the factory knows WHICH prefab and HOW to initialise it. Object pooling can be added here later
    /// without touching any caller.
    /// </summary>
    public class ItemFactory
    {
        private readonly ItemPrefabSet _prefabs;
        private readonly Transform _parent;
        private readonly Stack<Cube> _cubePool = new();

        public ItemFactory(ItemPrefabSet prefabs, Transform parent)
        {
            _prefabs = prefabs;
            _parent = parent;
        }

        public static CubeColor RandomColor() => (CubeColor)Random.Range(0, 4);

        public Cube CreateCube(CubeColor color)
        {
            Cube cube;
            if (_cubePool.Count > 0)
            {
                cube = _cubePool.Pop();
                cube.gameObject.SetActive(true);
            }
            else
            {
                cube = Object.Instantiate(_prefabs.Cube, _parent);
            }
            cube.Initialize(color);
            cube.name = $"Cube_{color}";
            return cube;
        }

        /// <summary>
        /// Gives an item back. Cubes are pooled (dozens are destroyed and spawned every turn); everything
        /// else is rare enough to simply destroy. Callers never need to know which happened.
        /// </summary>
        public void Release(GridItem item)
        {
            if (item is Cube cube)
            {
                cube.gameObject.SetActive(false);
                _cubePool.Push(cube);
            }
            else
            {
                Object.Destroy(item.gameObject);
            }
        }

        public Rocket CreateRocket(RocketDirection direction)
        {
            Rocket rocket = Object.Instantiate(_prefabs.Rocket, _parent);
            rocket.Initialize(direction);
            rocket.name = $"Rocket_{direction}";
            return rocket;
        }

        public Bomb CreateBomb() => Object.Instantiate(_prefabs.Bomb, _parent);
        public Crate CreateCrate() => Object.Instantiate(_prefabs.Crate, _parent);
        public Rock CreateRock() => Object.Instantiate(_prefabs.Rock, _parent);
        public Chest CreateChest() => Object.Instantiate(_prefabs.Chest, _parent);

        /// <summary>Creates the item a level code asks for. Coin box parts other than BottomLeft return null.</summary>
        public GridItem Create(ItemSpec spec)
        {
            switch (spec.Kind)
            {
                case ItemKind.Cube:       return CreateCube(spec.Color);
                case ItemKind.RandomCube: return CreateCube(RandomColor());
                case ItemKind.Rocket:     return CreateRocket(spec.Direction);
                case ItemKind.Bomb:        return CreateBomb();
                case ItemKind.Crate:       return CreateCrate();
                case ItemKind.Rock:      return CreateRock();
                case ItemKind.ChestPart:
                    return spec.Corner == ChestCorner.BottomLeft ? CreateChest() : null;
                default:
                    Debug.LogError($"[ItemFactory] Unknown item kind {spec.Kind}");
                    return null;
            }
        }
    }
}
