using System.Collections;
using System.Collections.Generic;
using GemPop.Audio;
using GemPop.Gameplay;
using GemPop.Items;
using GemPop.Systems.Fx;
using UnityEngine;

namespace GemPop.Systems
{
    public enum ExplosionKind { Rocket, Tnt, RocketRocket, TntTnt, TntRocket }

    public readonly struct ExplosionRequest
    {
        public readonly ExplosionKind Kind;
        public readonly Vector2Int Center;
        public readonly RocketDirection Direction;   // only for Kind == Rocket

        public ExplosionRequest(ExplosionKind kind, Vector2Int center, RocketDirection direction = default)
        {
            Kind = kind; Center = center; Direction = direction;
        }

        /// <summary>The individual explosion of one special item at its own cell.</summary>
        public static ExplosionRequest For(SpecialItem item) => item switch
        {
            Rocket rocket => new ExplosionRequest(ExplosionKind.Rocket, rocket.Position, rocket.Direction),
            _             => new ExplosionRequest(ExplosionKind.Tnt, item.Position),
        };

        /// <summary>Spec rules for a tapped group of adjacent special items.</summary>
        public static ExplosionKind ComboKind(List<SpecialItem> group)
        {
            int tnts = 0;
            foreach (SpecialItem s in group) if (s is Tnt) tnts++;
            if (tnts >= 2) return ExplosionKind.TntTnt;
            if (tnts == 1) return ExplosionKind.TntRocket;
            return ExplosionKind.RocketRocket;
        }
    }

    /// <summary>
    /// Plays explosions and applies their damage through the board. Explosions are processed in WAVES:
    /// everything queued now plays in parallel; specials hit during a wave are queued and form the next
    /// wave (chain reactions). Shapes:
    ///   Rocket       two halves fly in opposite directions, damaging cells one by one
    ///   Tnt          5x5 area            TntTnt        7x7 area
    ///   RocketRocket a '+' of two rockets at the center
    ///   TntRocket    3 horizontal + 3 vertical rockets through the center (3x3 '+')
    /// </summary>
    public class ExplosionResolver
    {
        private readonly BoardController _board;
        private readonly ItemPrefabSet _prefabs;
        private readonly Queue<ExplosionRequest> _queue = new();

        private const float RocketCellsPerSecond = 24f;
        private const float AreaRingDelay = 0.03f;
        private const int PartSortingOrder = 1000;

        public ExplosionResolver(BoardController board, ItemPrefabSet prefabs)
        {
            _board = board;
            _prefabs = prefabs;
        }

        public void Enqueue(ExplosionRequest request) => _queue.Enqueue(request);

        /// <summary>Runs until no explosion is left, including the ones triggered along the way.</summary>
        public IEnumerator ResolveAll()
        {
            while (_queue.Count > 0)
            {
                var wave = new CoroutineBatch(_board);
                while (_queue.Count > 0) wave.Add(Play(_queue.Dequeue()));
                yield return wave.WaitAll();
            }
        }

        private IEnumerator Play(ExplosionRequest request)
        {
            int eventId = _board.NextEventId();
            Vector2Int c = request.Center;

            switch (request.Kind)
            {
                case ExplosionKind.Rocket:
                    yield return Line(c, request.Direction == RocketDirection.Horizontal, DamageSource.Rocket, eventId);
                    break;

                case ExplosionKind.Tnt:
                    yield return Area(c, 2, DamageSource.Tnt, eventId);
                    break;

                case ExplosionKind.TntTnt:
                    yield return Area(c, 3, DamageSource.Combo, eventId);
                    break;

                case ExplosionKind.RocketRocket:
                {
                    var batch = new CoroutineBatch(_board);
                    batch.Add(Line(c, true, DamageSource.Combo, eventId));
                    batch.Add(Line(c, false, DamageSource.Combo, eventId));
                    yield return batch.WaitAll();
                    break;
                }

                case ExplosionKind.TntRocket:
                {
                    var batch = new CoroutineBatch(_board);
                    for (int offset = -1; offset <= 1; offset++)
                    {
                        batch.Add(Line(c + new Vector2Int(0, offset), true, DamageSource.Combo, eventId));
                        batch.Add(Line(c + new Vector2Int(offset, 0), false, DamageSource.Combo, eventId));
                    }
                    yield return batch.WaitAll();
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- shapes

        /// <summary>A full row or column through 'start': the start cell now, then two halves outward.</summary>
        private IEnumerator Line(Vector2Int start, bool horizontal, DamageSource source, int eventId)
        {
            Board board = _board.Board;
            if (!board.InBounds(start)) yield break;

            AudioManager.Play(Sfx.RocketWhoosh);
            _board.ApplyDamageAt(start, new DamageInfo(source, eventId));

            Vector2Int dir = horizontal ? Vector2Int.right : Vector2Int.up;
            var batch = new CoroutineBatch(_board);
            batch.Add(RocketPart(start, dir, source, eventId));
            batch.Add(RocketPart(start, -dir, source, eventId));
            yield return batch.WaitAll();
        }

        /// <summary>One rocket half: a sprite that flies from 'start' to past the board edge, hitting each cell it crosses.</summary>
        private IEnumerator RocketPart(Vector2Int start, Vector2Int dir, DamageSource source, int eventId)
        {
            Board board = _board.Board;
            BoardLayout layout = _board.Layout;

            int steps = 0;
            while (board.InBounds(start + dir * (steps + 1))) steps++;

            var go = new GameObject("RocketPart");
            go.transform.SetParent(_board.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _prefabs.RocketPart(dir);
            sr.sortingOrder = PartSortingOrder;
            go.transform.localScale = Vector3.one * (layout.CellSize / 1.4f);   // part sprites are 140 px

            Vector3 origin = layout.CellToWorld(start);
            go.transform.position = origin;
            GameObject trail = _board.Fx.RocketTrail(go.transform);
            Vector3 step = new Vector3(dir.x, dir.y, 0f) * layout.CellSize;

            float traveled = 0f;     // in cells
            int nextCell = 1;
            float end = steps + 1.5f;  // fly a little past the edge before disappearing

            while (traveled < end)
            {
                traveled += RocketCellsPerSecond * Time.deltaTime;
                go.transform.position = origin + step * traveled;

                while (nextCell <= steps && traveled >= nextCell)
                {
                    _board.ApplyDamageAt(start + dir * nextCell, new DamageInfo(source, eventId));
                    nextCell++;
                }
                yield return null;
            }
            ParticleFx.EndTrail(trail);                  // let the last smoke fade out on its own
            Object.Destroy(go);
        }

        /// <summary>Square area of the given radius (2 -> 5x5, 3 -> 7x7), damaged ring by ring from the center.</summary>
        private IEnumerator Area(Vector2Int center, int radius, DamageSource source, int eventId)
        {
            Board board = _board.Board;
            _board.Fx.AreaExplosion(_board.Layout.CellToWorld(center), radius);
            AudioManager.Play(Sfx.TntBoom, radius == 2 ? 1f : 0.8f);
            for (int ring = 0; ring <= radius; ring++)
            {
                for (int dx = -ring; dx <= ring; dx++)
                    for (int dy = -ring; dy <= ring; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring) continue;   // only this ring
                        Vector2Int cell = center + new Vector2Int(dx, dy);
                        if (board.InBounds(cell)) _board.ApplyDamageAt(cell, new DamageInfo(source, eventId));
                    }
                if (ring < radius) yield return new WaitForSeconds(AreaRingDelay);
            }
        }
    }
}
