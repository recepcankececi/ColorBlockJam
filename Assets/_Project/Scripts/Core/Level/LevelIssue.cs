namespace CBJ.Core
{
    /// <summary>
    /// One problem <see cref="LevelValidator"/> found in a level, with a message for the designer.
    /// </summary>
    public readonly struct LevelIssue
    {
        /// <summary>Creates an issue.</summary>
        public LevelIssue(IssueSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        /// <summary>Whether the level can still be played.</summary>
        public IssueSeverity Severity { get; }

        /// <summary>What is wrong, in designer terms; blocks and doors are numbered from 1 in list order.</summary>
        public string Message { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Severity}: {Message}";
        }
    }
}
