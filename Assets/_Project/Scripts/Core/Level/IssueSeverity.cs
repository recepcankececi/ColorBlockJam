namespace CBJ.Core
{
    /// <summary>
    /// How serious a <see cref="LevelIssue"/> is.
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>The level cannot be played as saved.</summary>
        Error,

        /// <summary>The level plays, but probably cannot be won.</summary>
        Warning
    }
}
