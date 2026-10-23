using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemPop.UI
{
    /// <summary>One goal icon in the top bar: icon + remaining count, or a check mark when done.</summary>
    public class GoalSlot : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject checkMark;

        private void Awake()
        {
            // White digits with a dark outline, like the reference. Done at runtime so no material instance
            // has to be serialized into the scene.
            TextOutline.Apply(countText);
        }

        public void Setup(Sprite sprite)
        {
            icon.sprite = sprite;
            icon.preserveAspect = true;
            gameObject.SetActive(true);
        }

        public void SetCount(int remaining)
        {
            bool done = remaining <= 0;
            countText.gameObject.SetActive(!done);
            checkMark.SetActive(done);
            if (!done) countText.text = remaining.ToString();
        }

#if UNITY_EDITOR
        public void EditorWire(Image iconImage, TMP_Text count, GameObject check)
        {
            icon = iconImage; countText = count; checkMark = check;
        }
#endif
    }
}
