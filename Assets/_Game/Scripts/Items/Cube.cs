using System;
using UnityEngine;

namespace GemPop.Items
{
    public class Cube : GridItem
    {
        [Serializable]
        public class Skin
        {
            public CubeColor color;
            public Sprite normal;
            public Sprite rocketHint;
            public Sprite bombHint;
        }

        [SerializeField] private Skin[] skins;

        public CubeColor Color { get; private set; }
        public HintType Hint { get; private set; }

        public override bool CanFall => true;

        /// <summary>Any explosion destroys a cube; blasts never reach here (a blast removes its own group).</summary>
        public override DamageOutcome TakeDamage(DamageInfo info) => DamageOutcome.Destroyed;

        public void Initialize(CubeColor color)
        {
            Color = color;
            Hint = HintType.None;
            ApplySprite();
        }

        /// <summary>Called by the hint system after every board change.</summary>
        public void SetHint(HintType hint)
        {
            if (Hint == hint) return;
            Hint = hint;
            ApplySprite();
        }

        private void ApplySprite()
        {
            Skin skin = FindSkin(Color);
            if (skin == null) return;
            mainRenderer.sprite = Hint switch
            {
                HintType.Rocket => skin.rocketHint,
                HintType.Bomb    => skin.bombHint,
                _               => skin.normal,
            };
        }

        private Skin FindSkin(CubeColor color)
        {
            for (int i = 0; i < skins.Length; i++)
                if (skins[i].color == color) return skins[i];
            Debug.LogError($"[Cube] No skin for color {color}");
            return null;
        }

#if UNITY_EDITOR
        public void EditorSetSkins(Skin[] value) => skins = value;
#endif
    }
}
