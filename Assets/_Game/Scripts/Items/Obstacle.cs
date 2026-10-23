using UnityEngine;

namespace GemPop.Items
{
    /// <summary>
    /// Level goals. Each obstacle has health; HOW it takes damage differs per type
    /// and will be expressed by overriding TakeDamage (Day 5).
    /// </summary>
    public abstract class Obstacle : GridItem
    {
        [SerializeField] protected int maxHealth = 1;

        public int Health { get; protected set; }
        public bool IsCleared => Health <= 0;
        public override bool IsObstacle => true;

        protected virtual void Awake()
        {
            Health = maxHealth;
        }

#if UNITY_EDITOR
        public void EditorSetMaxHealth(int value) => maxHealth = value;
#endif
    }
}
