namespace CBJ.Gameplay
{
    /// <summary>
    /// The supplied modular block pieces. Each covers one quadrant of a cell, except the inner corner, which covers
    /// the three quadrants around a concave corner.
    /// </summary>
    public enum BlockPartKind
    {
        Center,
        Edge,
        OuterCorner,
        InnerCorner
    }
}
