using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Memento.Pet;

namespace Memento.Windows
{
    public static class PetRenderer
    {
        private class Particle
        {
            public Vector2 Pos, Vel;
            public float Life, MaxLife;
            public uint Color;
            public string Glyph = "";
            public Particle(Vector2 pos, Vector2 vel, float life, uint color, string glyph)
            { Pos = pos; Vel = vel; Life = life; MaxLife = life; Color = color; Glyph = glyph; }
        }

        private static readonly List<Particle> Particles = new();
        private static readonly Random Rng = new();
        private static double LastSpawn = 0;

        private static uint C(float r, float g, float b, float a = 1f)
            => ImGui.ColorConvertFloat4ToU32(new Vector4(r, g, b, a));

        private static readonly uint FoxBody = C(0.91f, 0.53f, 0.35f);
        private static readonly uint FoxBelly = C(0.96f, 0.79f, 0.63f);
        private static readonly uint FoxEarInner = C(1.00f, 0.56f, 0.56f);
        private static readonly uint FoxDark = C(0.53f, 0.18f, 0.07f);
        private static readonly uint EyeColor = C(0.16f, 0.10f, 0.30f);
        private static readonly uint EyeShine = C(1.00f, 1.00f, 1.00f);
        private static readonly uint TailTip = C(0.96f, 0.79f, 0.63f);
        private static readonly uint NoseColor = C(0.75f, 0.31f, 0.17f);
        private static readonly uint EggShell = C(0.97f, 0.94f, 0.88f);
        private static readonly uint EggSpot = C(0.80f, 0.70f, 0.55f);
        private static readonly uint EggCrack = C(0.50f, 0.38f, 0.28f);
        private static readonly uint BlushColor = C(1.00f, 0.55f, 0.55f, 0.55f);

        /// <summary>
        /// Draw the fox pet.
        /// mood: 0 = happy (eyes high + blush), 1 = neutral, 2 = sad (eyes low + brow mark)
        /// </summary>
        public static void Draw(PetData pet, Vector2 origin, float scale = 3f, int mood = 1)
        {
            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            float now = (float)ImGui.GetTime();
            float bounce = MathF.Sin(now * 2.2f) * 3f * scale;
            float sway = MathF.Sin(now * 1.5f) * 2f * scale;
            var drawPos = origin + new Vector2(sway, bounce);

            switch (pet.Stage)
            {
                case PetStage.Egg: DrawEgg(dl, drawPos, scale, now); break;
                case PetStage.Baby: DrawBabyFox(dl, drawPos, scale, now, mood); break;
                case PetStage.Teen: DrawTeenFox(dl, drawPos, scale, now, mood); break;
                default: DrawAdultFox(dl, drawPos, scale, now, pet.IsAlive, mood); break;
            }

            UpdateParticles(dl, now);
            MaybeSpawnParticle(pet, origin, now, mood);
        }

        private static void DrawEgg(ImDrawListPtr dl, Vector2 o, float s, float t)
        {
            float wobble = MathF.Sin(t * 3f) * 1.5f * s;
            o.X += wobble;
            FillRect(dl, o, 2, 0, 8, 1, s, EggShell);
            FillRect(dl, o, 1, 1, 10, 6, s, EggShell);
            FillRect(dl, o, 2, 7, 8, 2, s, EggShell);
            FillRect(dl, o, 3, 2, 2, 2, s, EggSpot);
            FillRect(dl, o, 7, 5, 2, 2, s, EggSpot);
            if (t % 10f > 7f)
            {
                FillRect(dl, o, 5, 1, 1, 3, s, EggCrack);
                FillRect(dl, o, 6, 2, 1, 2, s, EggCrack);
            }
        }

        private static void DrawBabyFox(ImDrawListPtr dl, Vector2 o, float s, float t, int mood)
        {
            FillRect(dl, o, 3, 8, 8, 6, s, FoxBody);
            FillRect(dl, o, 4, 9, 6, 4, s, FoxBelly);
            FillRect(dl, o, 2, 2, 10, 8, s, FoxBody);
            FillRect(dl, o, 3, 6, 8, 3, s, FoxBelly);
            FillRect(dl, o, 2, 0, 3, 4, s, FoxBody);
            FillRect(dl, o, 3, 1, 2, 3, s, FoxEarInner);
            FillRect(dl, o, 9, 0, 3, 4, s, FoxBody);
            FillRect(dl, o, 10, 1, 2, 3, s, FoxEarInner);
            // Nose dot — tiny, won't read as frown
            FillRect(dl, o, 6, 7, 2, 1, s, NoseColor);
            FillRect(dl, o, 3, 13, 2, 2, s, FoxDark);
            FillRect(dl, o, 9, 13, 2, 2, s, FoxDark);

            // Eyes by mood
            if (mood == 0) // happy — eyes high + blush
            {
                FillRect(dl, o, 3, 4, 3, 3, s, EyeColor);
                FillRect(dl, o, 8, 4, 3, 3, s, EyeColor);
                FillRect(dl, o, 4, 4, 1, 1, s, EyeShine);
                FillRect(dl, o, 9, 4, 1, 1, s, EyeShine);
                FillRect(dl, o, 2, 7, 2, 1, s, BlushColor);
                FillRect(dl, o, 10, 7, 2, 1, s, BlushColor);
            }
            else if (mood == 1) // neutral
            {
                FillRect(dl, o, 3, 5, 3, 3, s, EyeColor);
                FillRect(dl, o, 8, 5, 3, 3, s, EyeColor);
                FillRect(dl, o, 4, 5, 1, 1, s, EyeShine);
                FillRect(dl, o, 9, 5, 1, 1, s, EyeShine);
            }
            else // sad — eyes low + inner brow mark
            {
                FillRect(dl, o, 3, 6, 3, 3, s, EyeColor);
                FillRect(dl, o, 8, 6, 3, 3, s, EyeColor);
                FillRect(dl, o, 2, 6, 1, 1, s, EyeColor); // brow droop
                FillRect(dl, o, 10, 6, 1, 1, s, EyeColor);
            }
        }

        private static void DrawTeenFox(ImDrawListPtr dl, Vector2 o, float s, float t, int mood)
        {
            uint star = C(1.0f, 0.85f, 0.1f);
            FillRect(dl, o, 4, 11, 10, 8, s, FoxBody);
            FillRect(dl, o, 5, 13, 8, 5, s, FoxBelly);
            FillRect(dl, o, 13, 13, 5, 3, s, FoxBody);
            FillRect(dl, o, 15, 11, 4, 4, s, FoxBody);
            FillRect(dl, o, 16, 10, 3, 3, s, TailTip);
            FillRect(dl, o, 3, 3, 12, 10, s, FoxBody);
            FillRect(dl, o, 4, 9, 10, 4, s, FoxBelly);
            FillRect(dl, o, 3, 0, 4, 6, s, FoxBody);
            FillRect(dl, o, 4, 1, 2, 4, s, FoxEarInner);
            FillRect(dl, o, 11, 0, 4, 6, s, FoxBody);
            FillRect(dl, o, 12, 1, 2, 4, s, FoxEarInner);
            FillRect(dl, o, 10, 0, 2, 1, s, star);
            FillRect(dl, o, 10, 1, 1, 1, s, star);
            FillRect(dl, o, 8, 9, 2, 1, s, NoseColor);
            FillRect(dl, o, 7, 10, 1, 1, s, NoseColor);
            FillRect(dl, o, 10, 10, 1, 1, s, NoseColor);
            FillRect(dl, o, 5, 18, 3, 3, s, FoxDark);
            FillRect(dl, o, 10, 18, 3, 3, s, FoxDark);

            if (mood == 0)
            {
                FillRect(dl, o, 4, 5, 3, 2, s, EyeColor);
                FillRect(dl, o, 11, 5, 3, 2, s, EyeColor);
                FillRect(dl, o, 5, 5, 1, 1, s, EyeShine);
                FillRect(dl, o, 12, 5, 1, 1, s, EyeShine);
                FillRect(dl, o, 3, 7, 2, 1, s, BlushColor);
                FillRect(dl, o, 13, 7, 2, 1, s, BlushColor);
            }
            else if (mood == 1)
            {
                FillRect(dl, o, 4, 5, 3, 2, s, EyeColor);
                FillRect(dl, o, 11, 5, 3, 2, s, EyeColor);
                FillRect(dl, o, 5, 5, 1, 1, s, EyeShine);
                FillRect(dl, o, 12, 5, 1, 1, s, EyeShine);
            }
            else
            {
                FillRect(dl, o, 4, 6, 3, 2, s, EyeColor);
                FillRect(dl, o, 11, 6, 3, 2, s, EyeColor);
                FillRect(dl, o, 3, 6, 1, 1, s, EyeColor);
                FillRect(dl, o, 14, 6, 1, 1, s, EyeColor);
            }
        }

        private static void DrawAdultFox(ImDrawListPtr dl, Vector2 o, float s, float t, bool alive, int mood)
        {
            uint body = alive ? FoxBody : C(0.55f, 0.55f, 0.60f);
            uint belly = alive ? FoxBelly : C(0.70f, 0.70f, 0.72f);
            uint earIn = alive ? FoxEarInner : C(0.65f, 0.60f, 0.65f);
            uint eyeC = alive ? EyeColor : C(0.30f, 0.28f, 0.35f);

            FillRect(dl, o, 16, 14, 8, 4, s, body);
            FillRect(dl, o, 19, 11, 6, 5, s, body);
            FillRect(dl, o, 20, 9, 5, 4, s, TailTip);
            FillRect(dl, o, 5, 14, 13, 10, s, body);
            FillRect(dl, o, 7, 16, 9, 7, s, belly);
            FillRect(dl, o, 4, 4, 15, 13, s, body);
            FillRect(dl, o, 6, 12, 11, 5, s, belly);
            FillRect(dl, o, 4, 0, 5, 8, s, body);
            FillRect(dl, o, 5, 1, 3, 6, s, earIn);
            FillRect(dl, o, 14, 0, 5, 8, s, body);
            FillRect(dl, o, 15, 1, 3, 6, s, earIn);
            FillRect(dl, o, 10, 13, 3, 1, s, NoseColor);
            FillRect(dl, o, 9, 14, 1, 1, s, NoseColor);
            FillRect(dl, o, 13, 14, 1, 1, s, NoseColor);
            FillRect(dl, o, 6, 23, 4, 5, s, FoxDark);
            FillRect(dl, o, 13, 23, 4, 5, s, FoxDark);

            bool blinking = ((int)(t * 1f) % 4 == 0) && (t % 1f > 0.85f);

            if (!alive)
            {
                FillRect(dl, o, 6, 7, 1, 1, s, eyeC); FillRect(dl, o, 9, 7, 1, 1, s, eyeC);
                FillRect(dl, o, 7, 8, 1, 1, s, eyeC); FillRect(dl, o, 8, 8, 1, 1, s, eyeC);
                FillRect(dl, o, 13, 7, 1, 1, s, eyeC); FillRect(dl, o, 16, 7, 1, 1, s, eyeC);
                FillRect(dl, o, 14, 8, 1, 1, s, eyeC); FillRect(dl, o, 15, 8, 1, 1, s, eyeC);
            }
            else if (blinking)
            {
                FillRect(dl, o, 6, 9, 4, 1, s, eyeC);
                FillRect(dl, o, 13, 9, 4, 1, s, eyeC);
            }
            else if (mood == 0) // happy — eyes high + blush
            {
                FillRect(dl, o, 6, 6, 4, 4, s, eyeC);
                FillRect(dl, o, 13, 6, 4, 4, s, eyeC);
                FillRect(dl, o, 7, 6, 1, 1, s, EyeShine);
                FillRect(dl, o, 14, 6, 1, 1, s, EyeShine);
                FillRect(dl, o, 4, 11, 3, 1, s, BlushColor);
                FillRect(dl, o, 16, 11, 3, 1, s, BlushColor);
            }
            else if (mood == 1) // neutral
            {
                FillRect(dl, o, 6, 7, 4, 4, s, eyeC);
                FillRect(dl, o, 13, 7, 4, 4, s, eyeC);
                FillRect(dl, o, 7, 7, 1, 1, s, EyeShine);
                FillRect(dl, o, 14, 7, 1, 1, s, EyeShine);
            }
            else // sad — eyes lower + brow mark
            {
                FillRect(dl, o, 6, 9, 4, 3, s, eyeC);
                FillRect(dl, o, 13, 9, 4, 3, s, eyeC);
                FillRect(dl, o, 5, 8, 2, 1, s, eyeC);
                FillRect(dl, o, 16, 8, 2, 1, s, eyeC);
            }
        }

        private static void FillRect(ImDrawListPtr dl, Vector2 o,
            int px, int py, int pw, int ph, float scale, uint color)
        {
            var tl = o + new Vector2(px * scale, py * scale);
            dl.AddRectFilled(tl, tl + new Vector2(pw * scale, ph * scale), color);
        }

        private static readonly string[] ParticleGlyphs = { "♥", "✦", "✿", "⋆", "*" };
        private static readonly uint[] ParticleColors =
        {
            C(1.00f,0.30f,0.55f,0.9f), C(0.77f,0.55f,1.00f,0.9f),
            C(0.18f,0.83f,0.75f,0.9f), C(1.00f,0.85f,0.20f,0.9f),
        };

        private static void MaybeSpawnParticle(PetData pet, Vector2 center, float t, int mood)
        {
            if (!pet.IsAlive || mood == 2) return; // no particles when sad
            float interval = pet.Stage switch
            {
                PetStage.Egg => 4f,
                PetStage.Baby => 2.5f,
                PetStage.Teen => 1.8f,
                _ => 1.2f,
            };
            if (t - LastSpawn < interval) return;
            LastSpawn = t;
            float avg = (pet.Love + pet.Joy) / 2f;
            if (avg < 0.2f) return;

            Particles.Add(new Particle(
                new Vector2(center.X + (float)(Rng.NextDouble() * 60 - 30),
                            center.Y + (float)(Rng.NextDouble() * 20 - 10)),
                new Vector2((float)(Rng.NextDouble() * 20 - 10),
                            -(float)(Rng.NextDouble() * 25 + 15)),
                2.5f, ParticleColors[Rng.Next(ParticleColors.Length)],
                ParticleGlyphs[Rng.Next(ParticleGlyphs.Length)]));
        }

        private static void UpdateParticles(ImDrawListPtr dl, float t)
        {
            float dt = ImGui.GetIO().DeltaTime;
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                var p = Particles[i]; p.Life -= dt;
                if (p.Life <= 0f) { Particles.RemoveAt(i); continue; }
                p.Pos += p.Vel * dt;
                byte a = (byte)(Math.Min(1f, p.Life / p.MaxLife * 2f) * 255);
                dl.AddText(p.Pos, (p.Color & 0x00FFFFFF) | ((uint)a << 24), p.Glyph);
            }
        }
    }
}
