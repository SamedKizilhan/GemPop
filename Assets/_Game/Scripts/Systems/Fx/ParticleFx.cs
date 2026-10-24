using UnityEngine;

namespace GemPop.Systems.Fx
{
    /// <summary>
    /// Builds short-lived ParticleSystems from code (no prefabs, no editor setup) using the case-study
    /// particle sprites. Every effect is one burst that destroys itself when it is over.
    /// </summary>
    public static class ParticleFx
    {
        private const int DefaultSortingOrder = 1500;    // above items (<= 1000), below the UI canvas (2000)
        private static Material _alphaMaterial;
        private static Material _additiveMaterial;

        private static Material AlphaMaterial =>
            _alphaMaterial ??= new Material(Shader.Find("Sprites/Default"));

        /// <summary>Additive blending: black pixels become invisible, bright pixels glow (for the sparkle sprites).</summary>
        private static Material AdditiveMaterial
        {
            get
            {
                if (_additiveMaterial == null)
                {
                    Shader shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
                    _additiveMaterial = new Material(shader);
                }
                return _additiveMaterial;
            }
        }

        private static Sprite _glowSprite;

        /// <summary>
        /// A procedurally drawn soft disc (opaque centre, smooth falloff). The shipped particle_smoke.png is a
        /// pale, mostly transparent cloud, which can never look like fire however many are stacked.
        /// </summary>
        private static Sprite GlowSprite
        {
            get
            {
                if (_glowSprite != null) return _glowSprite;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color[n * n];
                float c = (n - 1) * 0.5f;
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;   // 0 centre .. 1 edge
                    float a = Mathf.Clamp01(1f - r);
                    a = a * a * (3f - 2f * a);                                          // smoothstep falloff
                    pixels[y * n + x] = new Color(1f, 1f, 1f, a);
                }
                tex.SetPixels(pixels);
                tex.Apply();
                _glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                return _glowSprite;
            }
        }

        /// <summary>
        /// Rocket exhaust look — tune here. All sizes are fractions of a board cell.
        /// </summary>
        public static class RocketTrailSettings
        {
            public static float CoreWidth = 0.43f;        // bright yellow ribbon thickness
            public static float HaloWidth = 0.53f;        // orange glow around it
            public static float CoreLifetime = 0.45f;     // seconds the ribbon stays on screen
            public static float HaloLifetime = 0.45f;
            public static float CoreDensity = 60f;        // particles per world unit travelled (brightness)
            public static float HaloDensity = 30f;
            public static float StarDensity = 23f;
            public static Color CoreColor = new Color(1f, 1f, 0.80f, 1f);
            public static Color HaloColor = new Color(1f, 0.55f, 0.05f, 0.9f);
        }

        /// <summary>Debris flying out of a destroyed item: a few sprites, radial velocity, gravity, shrink + fade.</summary>
        public static void Burst(Vector3 position, Sprite[] sprites, float size, int count = 8,
                                 float speed = 4.5f, float life = 0.55f, float gravity = 2.5f,
                                 int sortingOrder = DefaultSortingOrder, Color? tint = null, bool additive = false)
        {
            if (sprites == null || sprites.Length == 0) return;

            // one system per sprite so each piece keeps its own texture; count is split between them
            int perSprite = Mathf.Max(1, count / sprites.Length);
            foreach (Sprite sprite in sprites)
            {
                if (sprite == null) continue;
                ParticleSystem ps = Create("Burst", position, sprite, sortingOrder, additive);

                var main = ps.main;
                if (tint.HasValue) main.startColor = tint.Value;
                main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
                main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
                main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.gravityModifier = gravity;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.15f;

                var emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)perSprite) });

                var sizeOverLife = ps.sizeOverLifetime;
                sizeOverLife.enabled = true;
                sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

                var colorOverLife = ps.colorOverLifetime;
                colorOverLife.enabled = true;
                colorOverLife.color = FadeOut();

                var rotation = ps.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

                ps.Play();
                Object.Destroy(ps.gameObject, life + 0.2f);
            }
        }

        /// <summary>
        /// Bomb blast: several overlapping additive flashes of different sizes and offsets (so it reads as an
        /// irregular burst of light, not a neat circle), yellow-to-orange, plus sparks flying out.
        /// </summary>
        public static void Blast(Vector3 position, Sprite flash, float radius, int sortingOrder = DefaultSortingOrder)
        {
            if (flash == null) return;

            var yellow = new Color(1f, 0.95f, 0.45f);
            var orange = new Color(1f, 0.45f, 0.08f);

            // 1) Detonation flash: one big near-white disc that pops to ~3x the blast radius in a few frames
            //    and is gone in a quarter second. This is the "hit" the eye registers; everything after is the fire.
            ParticleSystem flashPs = Create("BlastFlash", position, GlowSprite, sortingOrder + 3, true);
            var flashMain = flashPs.main;
            flashMain.startLifetime = 0.22f;
            flashMain.startSpeed = 0f;
            flashMain.startSize = radius * 3.2f;
            flashMain.startColor = new Color(1f, 1f, 0.9f, 1f);
            PointEmitter(flashPs);
            var flashEmission = flashPs.emission;
            flashEmission.rateOverTime = 0f;
            flashEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });
            var flashSize = flashPs.sizeOverLifetime;
            flashSize.enabled = true;
            flashSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1.1f));
            var flashColor = flashPs.colorOverLifetime;
            flashColor.enabled = true;
            flashColor.color = FadeOut(0.3f);
            flashPs.Play();
            Object.Destroy(flashPs.gameObject, 0.6f);

            // 2) Fireball: a handful of large orange-yellow blobs that swell out of the centre.
            ParticleSystem fireball = Create("BlastFireball", position, GlowSprite, sortingOrder + 2, true);
            var fbMain = fireball.main;
            fbMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.5f);
            fbMain.startSpeed = new ParticleSystem.MinMaxCurve(radius * 0.8f, radius * 2.0f);
            fbMain.startSize = new ParticleSystem.MinMaxCurve(radius * 1.4f, radius * 2.2f);
            fbMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.4f, 0.05f));
            var fbShape = fireball.shape;
            fbShape.enabled = true;
            fbShape.shapeType = ParticleSystemShapeType.Circle;
            fbShape.radius = radius * 0.2f;
            var fbEmission = fireball.emission;
            fbEmission.rateOverTime = 0f;
            fbEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)8) });
            var fbSize = fireball.sizeOverLifetime;
            fbSize.enabled = true;
            fbSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.3f));
            var fbColor = fireball.colorOverLifetime;
            fbColor.enabled = true;
            fbColor.color = FadeOut(0.35f);
            fireball.Play();
            Object.Destroy(fireball.gameObject, 1f);

            // 3) core flashes (the lingering yellow fire)
            ParticleSystem core = Create("BlastCore", position, flash, sortingOrder + 1, true);
            var main = core.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);      // slower burn-out
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, radius * 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 1.0f, radius * 2.0f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(yellow, orange);

            var shape = core.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * 0.3f;

            var emission = core.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)6), new ParticleSystem.Burst(0.08f, (short)3), new ParticleSystem.Burst(0.2f, (short)2) });

            var sizeOverLife = core.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.25f));

            var colorOverLife = core.colorOverLifetime;
            colorOverLife.enabled = true;
            colorOverLife.color = FadeOut(0.4f);                                     // hold bright longer, then fade

            core.Play();
            Object.Destroy(core.gameObject, 1.6f);

            // sparks
            Burst(position, new[] { flash }, radius * 0.45f, 20, radius * 3.5f, 1.0f, 0.5f, sortingOrder + 1,
                  new Color(1f, 0.8f, 0.3f), true);
        }

        /// <summary>Small glittering stars flying out, no gravity (rocket sparkle).</summary>
        public static void Sparkles(Vector3 position, Sprite star, float size, int count, Color color, int sortingOrder = DefaultSortingOrder)
        {
            Burst(position, new[] { star }, size, count, 5f, 0.45f, 0f, sortingOrder, color);
        }

        /// <summary>
        /// Smoke + sparkles left behind a flying rocket half. Returns the root so the caller can detach it when
        /// the rocket is gone; the particles finish fading on their own.
        /// </summary>
        /// <summary>
        /// Rocket exhaust drawn as a bright flame streak (reference: a solid glowing yellow band with a
        /// few stars in it). Particles are emitted per distance in world space with no velocity, so they
        /// stay where the rocket passed and form a continuous ribbon that fades out behind it.
        /// </summary>
        public static GameObject Trail(Transform parent, Sprite unusedSmoke, Sprite star, float size)
        {
            float cell = size / 0.45f;             // BoardFx passes 45% of a cell; convert back to cell units
            var root = new GameObject("Trail");
            root.transform.SetParent(parent, false);

            ParticleSystem halo = Create("Halo", parent.position, GlowSprite, DefaultSortingOrder - 2, additive: true);
            halo.transform.SetParent(root.transform, true);
            var haloMain = halo.main;
            haloMain.startLifetime = RocketTrailSettings.HaloLifetime;
            haloMain.startSpeed = 0f;
            haloMain.startSize = cell * RocketTrailSettings.HaloWidth;
            haloMain.startColor = RocketTrailSettings.HaloColor;
            var haloEmission = halo.emission;
            haloEmission.rateOverTime = 0f;
            haloEmission.rateOverDistance = RocketTrailSettings.HaloDensity;
            PointEmitter(halo);
            var haloColor = halo.colorOverLifetime;
            haloColor.enabled = true;
            haloColor.color = FadeOut(0.4f);
            halo.Play();

            ParticleSystem core = Create("Core", parent.position, GlowSprite, DefaultSortingOrder - 1, additive: true);
            core.transform.SetParent(root.transform, true);
            var coreMain = core.main;
            coreMain.startLifetime = RocketTrailSettings.CoreLifetime;
            coreMain.startSpeed = 0f;
            coreMain.startSize = cell * RocketTrailSettings.CoreWidth;
            coreMain.startColor = RocketTrailSettings.CoreColor;
            var coreEmission = core.emission;
            coreEmission.rateOverTime = 0f;
            coreEmission.rateOverDistance = RocketTrailSettings.CoreDensity;
            PointEmitter(core);
            var coreColor = core.colorOverLifetime;
            coreColor.enabled = true;
            coreColor.color = FlameGradient();
            var coreSize = core.sizeOverLifetime;
            coreSize.enabled = true;
            coreSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.55f));
            core.Play();

            if (star != null)
            {
                ParticleSystem stars = Create("Stars", parent.position, star, DefaultSortingOrder, additive: true);
                stars.transform.SetParent(root.transform, true);
                var starMain = stars.main;
                starMain.startLifetime = 0.45f;
                starMain.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
                starMain.startSize = new ParticleSystem.MinMaxCurve(cell * 0.06f, cell * 0.11f);
                starMain.startColor = new Color(1f, 0.95f, 0.7f);
                starMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                var starEmission = stars.emission;
                starEmission.rateOverTime = 0f;
                starEmission.rateOverDistance = RocketTrailSettings.StarDensity;
                PointEmitter(stars, RocketTrailSettings.CoreWidth * 0.5f * cell);
                var starColor = stars.colorOverLifetime;
                starColor.enabled = true;
                starColor.color = FadeOut(0.4f);
                stars.Play();
            }
            return root;
        }

        /// <summary>
        /// Unity's default Shape module is a cone with a 1-unit radius, which scatters particles around the
        /// emitter. A trail must be born exactly on the rocket's path, so emit from a point (or a tiny disc).
        /// </summary>
        private static void PointEmitter(ParticleSystem ps, float radius = 0f)
        {
            var shape = ps.shape;
            if (radius <= 0f) { shape.enabled = false; return; }
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 1f;
        }

        /// <summary>White-yellow → yellow → orange → transparent, the colour path of a cooling flame.</summary>
        private static ParticleSystem.MinMaxGradient FlameGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 0.85f), 0f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0.35f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.1f), 0.8f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.05f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            return new ParticleSystem.MinMaxGradient(g);
        }

        public static void EndTrail(GameObject trail)
        {
            if (trail == null) return;
            trail.transform.SetParent(null, true);
            foreach (ParticleSystem ps in trail.GetComponentsInChildren<ParticleSystem>()) ps.Stop();
            Object.Destroy(trail, 1.0f);   // >= longest trail lifetime
        }

        /// <summary>Win celebration: gold stars (alpha) and glowing sparkles (additive) shooting up and raining down.</summary>
        public static void Celebrate(Vector3 position, Sprite star, Sprite sparkle, int sortingOrder)
        {
            CelebrationStream(position, star, sortingOrder, false);
            CelebrationStream(position, sparkle, sortingOrder + 1, true);
        }

        private static void CelebrationStream(Vector3 position, Sprite sprite, int sortingOrder, bool additive)
        {
                if (sprite == null) return;
                ParticleSystem ps = Create("Celebration", position, sprite, sortingOrder, additive);

                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(13f, 19f);   // from y=-9 the stars reach the top half
                main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.gravityModifier = 1.0f;
                main.maxParticles = 200;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 35f;
                shape.radius = 0.3f;
                shape.rotation = new Vector3(-90f, 0f, 0f);   // cone points up (+Y)

                var emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[]
                {
                    new ParticleSystem.Burst(0f, (short)30),
                    new ParticleSystem.Burst(0.35f, (short)25),
                    new ParticleSystem.Burst(0.8f, (short)20),
                });

                var rotation = ps.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

                var colorOverLife = ps.colorOverLifetime;
                colorOverLife.enabled = true;
                colorOverLife.color = FadeOut(0.7f);

                ps.Play();
                Object.Destroy(ps.gameObject, 3.5f);
        }

        // ---------------------------------------------------------------- internals

        private static ParticleSystem Create(string name, Vector3 position, Sprite sprite, int sortingOrder, bool additive = false)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.AddSprite(sprite);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = additive ? AdditiveMaterial : AlphaMaterial;
            renderer.sortingOrder = sortingOrder;
            return ps;
        }

        private static ParticleSystem.MinMaxGradient FadeOut(float holdUntil = 0.5f)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, holdUntil), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(gradient);
        }
    }
}
