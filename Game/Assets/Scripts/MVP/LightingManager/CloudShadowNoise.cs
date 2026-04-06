/// <summary>
/// Tileable cellular (Worley) noise used by both the runtime
/// CloudShadowController and the editor CloudNoiseGenerator.
///
/// Algorithm overview:
///   - Divide [0,1]² into an N×N grid of cells.
///   - Place one random feature point per cell (deterministic via integer hash).
///   - For each pixel, find the distance to the nearest feature point across
///     the 3×3 cell neighbourhood (with toroidal/wrap-around handling so the
///     texture tiles seamlessly).
///   - Output  (1 - F1)  so blob centres are bright (cloud) and cell edges
///     are dark (sky).
///   - Layer multiple octaves for richer detail.
/// </summary>
public static class CloudShadowNoise
{
    /// <summary>
    /// Returns a tileable cellular noise value in [0, 1] for pixel (px, py)
    /// inside a texture of the given <paramref name="size"/>.
    /// </summary>
    /// <param name="px">Pixel x coordinate (0 … size-1)</param>
    /// <param name="py">Pixel y coordinate (0 … size-1)</param>
    /// <param name="size">Texture resolution (must be > 0)</param>
    /// <param name="gridSize">Number of feature-point cells per axis; more = smaller blobs</param>
    /// <param name="octaves">Layers of noise stacked together; more = more detail</param>
    public static float TileableCellular(int px, int py, int size, int gridSize, int octaves)
    {
        float nx = (float)px / size;  // 0..1
        float ny = (float)py / size;  // 0..1

        float total    = 0f;
        float amplitude = 1f;
        float maxVal   = 0f;
        int   cells    = gridSize;

        for (int o = 0; o < octaves; o++)
        {
            total  += SingleOctave(nx, ny, cells) * amplitude;
            maxVal += amplitude;
            amplitude *= 0.5f;
            cells     *= 2;       // double frequency each octave
        }

        return total / maxVal;
    }

    // ── Single-octave tileable cellular noise ────────────────────────────

    private static float SingleOctave(float nx, float ny, int g)
    {
        // Pixel position in cell-space
        float cx = nx * g;
        float cy = ny * g;

        int icx = FloorInt(cx);
        int icy = FloorInt(cy);

        float minDist2 = float.MaxValue;

        // Check 3×3 neighbourhood (needed to catch near feature points in adjacent cells)
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            // Wrapped cell indices
            int ncx = Mod(icx + dx, g);
            int ncy = Mod(icy + dy, g);

            // Feature point position in the neighbouring cell's slot
            // (fpx, fpy in cell-space; use unwrapped icx+dx so distance is correct)
            float fpx = (icx + dx) + Hash(ncx, ncy, 0);
            float fpy = (icy + dy) + Hash(ncx, ncy, 1);

            float ddx = cx - fpx;
            float ddy = cy - fpy;
            float d2  = ddx * ddx + ddy * ddy;

            if (d2 < minDist2)
                minDist2 = d2;
        }

        // F1 distance normalised to ~[0,1]; max intra-cell distance ≈ 1.5
        float f1 = UnityEngine.Mathf.Sqrt(minDist2) / 1.5f;
        f1 = UnityEngine.Mathf.Clamp01(f1);

        // Invert: centre of blob (near feature point) → 1 (bright cloud),
        //         cell edge (far from any point) → 0 (clear sky)
        return 1f - f1;
    }

    // ── Hash / integer helpers ───────────────────────────────────────────

    /// <summary>Maps integer grid cell (x,y) + seed to a float in [0, 1).</summary>
    private static float Hash(int x, int y, int seed)
    {
        int h = unchecked(x * 374761393 + y * 668265263 + seed * (int)2246822519u);
        h = unchecked((h ^ (h >> 13)) * 1274126177);
        h = h ^ (h >> 16);
        // Mask to positive then normalise
        return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
    }

    private static int FloorInt(float v)  => v >= 0 ? (int)v : (int)v - 1;
    private static int Mod(int v, int m)  => ((v % m) + m) % m;
}
