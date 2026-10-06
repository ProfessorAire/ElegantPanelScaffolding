using System;
using System.Collections.Generic;

namespace EPS.Services
{
    /// <summary>
    /// Simple static transfer bag for passing generated preview files to the PreviewPage.
    /// </summary>
    public static class PreviewState
    {
        public static IReadOnlyList<GeneratedFile> Files { get; set; } = Array.Empty<GeneratedFile>();
    }
}
