using System;

namespace CBJ.Tests.EditMode.Project
{
    /// <summary>
    /// The subset of an .asmdef JSON file that the dependency rules inspect.
    /// </summary>
    [Serializable]
    public class AsmdefFile
    {
        public string name;
        public string[] references = Array.Empty<string>();
        public string[] includePlatforms = Array.Empty<string>();
    }
}
