using UnityEngine;
using UnityEngine.InputSystem;

namespace GemPop.Gameplay
{
    /// <summary>
    /// Turns a tap/click into a cell coordinate and hands it to the board.
    /// Uses the Input System's Pointer, which covers both mouse (editor) and touch (device).
    /// </summary>
    public class BoardInput : MonoBehaviour
    {
        [SerializeField] private BoardController board;

        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            Vector2 screen = pointer.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));

            if (board.Layout.TryWorldToCell(world, out Vector2Int cell))
                board.OnCellTapped(cell);
        }
    }
}
