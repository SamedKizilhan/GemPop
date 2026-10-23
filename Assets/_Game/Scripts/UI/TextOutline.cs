using TMPro;
using UnityEngine;

namespace GemPop.UI
{
    /// <summary>
    /// White digits with a dark outline (reference look). Applied at runtime so no material instance
    /// needs to be serialized into the scene; TMP creates a per-text material copy on first use.
    /// </summary>
    public static class TextOutline
    {
        public static readonly Color32 Dark = new Color32(25, 15, 5, 255);

        public static void Apply(TMP_Text text, float width = 0.22f) => Apply(text, width, Dark);

        public static void Apply(TMP_Text text, float width, Color32 color)
        {
            if (text == null) return;
            text.fontMaterial.EnableKeyword("OUTLINE_ON");   // instance material; no asset is modified
            text.outlineWidth = width;
            text.outlineColor = color;
        }

        /// <summary>A white outline on white text thickens the strokes: "extra bold" without another font.</summary>
        public static void ApplyBold(TMP_Text text, float width = 0.12f) => Apply(text, width, Color.white);
    }
}
