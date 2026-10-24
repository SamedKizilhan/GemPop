using UnityEngine;

namespace GemPop.UI
{
    /// <summary>Gently bobs and tilts its children (the menu's gem / bomb / rocket icons) so the title screen feels alive.</summary>
    public class MenuDecor : MonoBehaviour
    {
        [SerializeField] private float amplitude = 14f;     // canvas pixels
        [SerializeField] private float tilt = 5f;           // degrees
        [SerializeField] private float speed = 1.4f;

        private RectTransform[] _items;
        private Vector2[] _rest;
        private float[] _restAngle;

        private void Awake()
        {
            _items = new RectTransform[transform.childCount];
            _rest = new Vector2[_items.Length];
            _restAngle = new float[_items.Length];
            for (int i = 0; i < _items.Length; i++)
            {
                _items[i] = (RectTransform)transform.GetChild(i);
                _rest[i] = _items[i].anchoredPosition;
                _restAngle[i] = _items[i].localEulerAngles.z;
            }
        }

        private void Update()
        {
            float t = Time.time * speed;
            for (int i = 0; i < _items.Length; i++)
            {
                float phase = i * 1.1f;
                _items[i].anchoredPosition = _rest[i] + new Vector2(0f, Mathf.Sin(t + phase) * amplitude);
                _items[i].localRotation = Quaternion.Euler(0f, 0f, _restAngle[i] + Mathf.Sin(t * 0.8f + phase) * tilt);
            }
        }
    }
}
