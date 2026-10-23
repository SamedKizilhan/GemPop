using System;
using System.Collections;
using System.Collections.Generic;
using GemPop.Audio;
using GemPop.Data;
using GemPop.Items;
using GemPop.Systems;
using GemPop.Systems.Fx;
using UnityEngine;

namespace GemPop.Gameplay
{
    /// <summary>
    /// Scene-side owner of the board. Builds the model from LevelData, places the views and runs the
    /// turn loop:  tap -> (blast | create special) -> gravity -> refill -> hints -> turn ended.
    /// While a turn is resolving, input is ignored (IsBusy), so the model is never changed mid-animation.
    /// </summary>
    public class BoardController : MonoBehaviour
    {
        [SerializeField] private BoardLayout layout;
        [SerializeField] private ItemPrefabSet prefabs;
        [SerializeField] private Transform itemsParent;

        [Header("Timing")]
        [SerializeField] private float blastDuration = 0.12f;
        [SerializeField] private float gatherDuration = 0.16f;
        [SerializeField] private float spawnStagger = 0.03f;

        public Board Board { get; private set; }
        public BoardLayout Layout => layout;
        public bool IsBusy { get; private set; }

        /// <summary>Raised when a turn has fully resolved (board is still, hints are fresh).</summary>
        public event Action TurnEnded;

        /// <summary>Raised for every item removed by damage (goal tracking listens for obstacles).</summary>
        public event Action<GridItem> ItemDestroyed;

        /// <summary>Raised when an item survives a hit but changed (vase cracked, chalices collected).</summary>
        public event Action<GridItem> ItemDamaged;

        /// <summary>Set by the level flow while a popup is open; taps are ignored.</summary>
        public bool InputLocked { get; set; }

        private ItemFactory _factory;
        private MoveTracker _moves;
        private ExplosionResolver _explosions;
        private CoroutineBatch _effects;      // pop animations of destroyed items
        private BoardFx _fx;
        private int _eventCounter;

        public BoardFx Fx => _fx;

        // ------------------------------------------------------------------ setup

        public void Initialize(LevelData level, MoveTracker moves)
        {
            _moves = moves;
            Board = new Board(level.Width, level.Height);
            layout.Configure(level.Width, level.Height);
            _factory = new ItemFactory(prefabs, itemsParent != null ? itemsParent : transform);
            _explosions = new ExplosionResolver(this, prefabs);
            _effects = new CoroutineBatch(this);
            _fx = new BoardFx(prefabs, layout);

            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    string code = level.GetCode(x, y);
                    if (!ItemCode.TryParse(code, out ItemSpec spec))
                    {
                        Debug.LogError($"[Board] Unknown code '{code}' at ({x},{y})");
                        continue;
                    }

                    // A chalice box is written as 4 codes; only its bottom-left corner creates the object.
                    if (spec.Kind == ItemKind.ChaliceBoxPart && spec.Corner != ChaliceCorner.BottomLeft)
                    {
                        if (!(Board.Get(x, y) is ChaliceBox))
                            Debug.LogError($"[Board] Chalice box corner '{code}' at ({x},{y}) has no matching bottom-left corner");
                        continue;
                    }

                    GridItem item = _factory.Create(spec);
                    if (item == null) continue;

                    Board.Place(item, new Vector2Int(x, y));
                    PlaceView(item);
                }
            }

            EnsureMoveExists();
            HintSystem.Refresh(Board);
        }

        /// <summary>
        /// Deadlock guard (not in the brief, but a board with no 2-group and no special item would be stuck):
        /// re-roll cube colors in place until a move exists. Obstacles and specials are untouched.
        /// </summary>
        private void EnsureMoveExists()
        {
            const int maxTries = 30;
            for (int attempt = 0; attempt < maxTries && !MatchFinder.HasAnyMove(Board); attempt++)
            {
                foreach (GridItem item in Board.AllItems())
                    if (item is Cube cube) cube.Initialize(ItemFactory.RandomColor());
                if (attempt == 0) Debug.Log("[Board] No valid move left - reshuffling cube colors");
            }
        }

        // ------------------------------------------------------------------ input

        public void OnCellTapped(Vector2Int cell)
        {
            if (IsBusy || InputLocked || _moves == null || !_moves.HasMoves) return;

            GridItem item = Board.Get(cell);
            switch (item)
            {
                case Cube cube:
                    List<Cube> group = MatchFinder.FindGroup(Board, cell);
                    if (group.Count < MatchFinder.MinBlastSize)              // not a valid move, no move spent
                    {
                        AudioManager.Play(Sfx.Invalid);
                        return;
                    }
                    StartCoroutine(ResolveCubeTap(group, cell));
                    break;

                case SpecialItem special:
                    StartCoroutine(ResolveSpecialTap(special, cell));
                    break;
            }
        }

        // ------------------------------------------------------------------ turn

        private IEnumerator ResolveCubeTap(List<Cube> group, Vector2Int tappedCell)
        {
            IsBusy = true;
            _moves.Spend();

            // Obstacles touching the group take "adjacent blast" damage (one event for the whole blast).
            int eventId = NextEventId();
            foreach (var hit in AdjacencyDamage.Collect(Board, group))
                ApplyDamage(hit.Key, new DamageInfo(DamageSource.Blast, eventId, hit.Value));

            // Model first: the cells are free from this moment on; the views catch up with animations.
            foreach (Cube cube in group) Board.Remove(cube);

            HintType creates = MatchFinder.HintFor(group.Count);
            AudioManager.Play(Sfx.CubePop, 1f + Mathf.Min(group.Count, 8) * 0.03f);   // bigger group, slightly higher pop
            if (creates == HintType.None)
                yield return BlastCubes(group);
            else
                yield return GatherIntoSpecial(group, tappedCell, creates);

            yield return FinishTurn();
        }

        /// <summary>
        /// Tapping a special item: alone it explodes in place; with adjacent specials they all gather at the
        /// tapped cell and explode once as a combo (individual explosions are skipped, per spec).
        /// </summary>
        private IEnumerator ResolveSpecialTap(SpecialItem tapped, Vector2Int tappedCell)
        {
            IsBusy = true;
            _moves.Spend();

            List<SpecialItem> group = MatchFinder.FindSpecialGroup(Board, tappedCell);
            foreach (SpecialItem s in group) Board.Remove(s);

            if (group.Count == 1)
            {
                _explosions.Enqueue(ExplosionRequest.For(tapped));
                Destroy(tapped.gameObject);
            }
            else
            {
                ExplosionKind kind = ExplosionRequest.ComboKind(group);
                Vector3 target = layout.CellToWorld(tappedCell);
                var batch = new CoroutineBatch(this);
                foreach (SpecialItem s in group) batch.Add(Tween.MoveTo(s.transform, target, gatherDuration));
                yield return batch.WaitAll();
                foreach (SpecialItem s in group) Destroy(s.gameObject);
                _explosions.Enqueue(new ExplosionRequest(kind, tappedCell));
            }

            yield return FinishTurn();
        }

        /// <summary>Common tail of every turn: chain explosions, wait for pops, gravity, refill, hints.</summary>
        private IEnumerator FinishTurn()
        {
            yield return _explosions.ResolveAll();
            yield return _effects.WaitAll();
            yield return CollapseAndRefill();

            EnsureMoveExists();
            HintSystem.Refresh(Board);
            IsBusy = false;
            TurnEnded?.Invoke();
        }

        /// <summary>Groups of 2-3: cubes simply pop.</summary>
        private IEnumerator BlastCubes(List<Cube> group)
        {
            var batch = new CoroutineBatch(this);
            foreach (Cube cube in group) batch.Add(PopAndDestroy(cube));
            yield return batch.WaitAll();
        }

        /// <summary>
        /// Groups of 4+: spec says cubes animate to the tapped cell and a special item appears there
        /// (4-5 -> random rocket, 6+ -> TNT).
        /// </summary>
        private IEnumerator GatherIntoSpecial(List<Cube> group, Vector2Int tappedCell, HintType creates)
        {
            Vector3 target = layout.CellToWorld(tappedCell);
            var batch = new CoroutineBatch(this);
            foreach (Cube cube in group) batch.Add(Tween.MoveTo(cube.transform, target, gatherDuration));
            yield return batch.WaitAll();
            foreach (Cube cube in group)
            {
                _fx.ItemDestroyed(cube);
                _factory.Release(cube);
            }

            GridItem special = creates == HintType.Tnt
                ? _factory.CreateTnt()
                : _factory.CreateRocket(UnityEngine.Random.value < 0.5f ? RocketDirection.Horizontal : RocketDirection.Vertical);

            Board.Place(special, tappedCell);
            PlaceView(special);
            AudioManager.Play(Sfx.SpecialCreated);
            yield return Tween.PopIn(special.transform);
        }

        private IEnumerator PopAndDestroy(GridItem item)
        {
            _fx.ItemDestroyed(item);
            yield return Tween.ScaleTo(item.transform, 0f, blastDuration);
            _factory.Release(item);
        }

        // ------------------------------------------------------------------ damage

        /// <summary>Each blast or explosion gets its own id, so items can apply once-per-event rules.</summary>
        public int NextEventId() => ++_eventCounter;

        public void ApplyDamageAt(Vector2Int cell, DamageInfo info)
        {
            GridItem item = Board.Get(cell);
            if (item != null) ApplyDamage(item, info);
        }

        /// <summary>
        /// The single place where damage reaches an item. Special items chain-explode; everything else
        /// decides for itself in TakeDamage, and destroyed items leave the model immediately.
        /// </summary>
        public void ApplyDamage(GridItem item, DamageInfo info)
        {
            if (item is SpecialItem special)
            {
                Board.Remove(special);
                _effects.Add(PopAndDestroy(special));
                _explosions.Enqueue(ExplosionRequest.For(special));
                return;
            }

            DamageOutcome outcome = item.TakeDamage(info);
            if (outcome == DamageOutcome.Ignored) return;
            if (outcome == DamageOutcome.Damaged)
            {
                _fx.ItemDamaged(item);
                BoardSfx.ItemDamaged(item);
                ItemDamaged?.Invoke(item);
                return;
            }

            Board.Remove(item);
            BoardSfx.ItemDestroyed(item);
            _effects.Add(PopAndDestroy(item));
            ItemDestroyed?.Invoke(item);
        }

        /// <summary>Applies gravity to the model, then animates every move and every new cube falling in.</summary>
        private IEnumerator CollapseAndRefill()
        {
            var batch = new CoroutineBatch(this);

            foreach (FallMove move in GravitySystem.Collapse(Board))
            {
                move.Item.SetSortingOrder(SortingOrderFor(move.To.y));
                batch.Add(Tween.Fall(move.Item.transform, layout.ItemWorldPosition(move.Item)));
            }

            foreach (SpawnRequest spawn in GravitySystem.FindSpawnCells(Board))
            {
                Cube cube = _factory.CreateCube(ItemFactory.RandomColor());
                Board.Place(cube, spawn.Cell);
                cube.FitToCell(layout.CellSize);
                cube.SetSortingOrder(SortingOrderFor(spawn.Cell.y));
                cube.transform.position = layout.SpawnPosition(spawn.Cell.x, spawn.RowsAboveTop);
                batch.Add(DelayedFall(cube, layout.ItemWorldPosition(cube), spawn.RowsAboveTop * spawnStagger));
            }

            yield return batch.WaitAll();
        }

        private IEnumerator DelayedFall(GridItem item, Vector3 target, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            yield return Tween.Fall(item.transform, target);
        }

        // ------------------------------------------------------------------ view helpers

        /// <summary>Snaps an item's GameObject to where the model says it is.</summary>
        public void PlaceView(GridItem item)
        {
            item.transform.position = layout.ItemWorldPosition(item);
            item.FitToCell(layout.CellSize);
            item.SetSortingOrder(SortingOrderFor(item.Position.y));
        }

        /// <summary>
        /// Higher rows are drawn in front (matches the reference art: a cube's bottom edge covers the top
        /// edge of the cube below it). Multiples of 10 leave room for per-item layers (e.g. chalice box doors).
        /// </summary>
        public int SortingOrderFor(int y) => (y + 1) * 10;
    }
}
