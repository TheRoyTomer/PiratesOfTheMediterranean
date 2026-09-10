namespace RenderWave.Runtime.Data
{
    /// <summary>
    /// Logical water category used by queries and contained-water profiles.
    /// River remains forward-looking metadata until dedicated river tooling exists.
    /// </summary>
    public enum WaterZoneType
    {
        Ocean = 0,
        Lake = 1,
        River = 2,
        Pool = 3
    }
}
