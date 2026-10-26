namespace CBJ.Core
{
    /// <summary>
    /// How a block moves (ADR-008). The serialized default is <see cref="Normal"/>, so blocks saved before kinds
    /// existed load unchanged.
    /// </summary>
    public enum BlockKind
    {
        Normal,
        Arrow,
        Ice
    }
}
