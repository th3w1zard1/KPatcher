namespace KPatcher.Core.Logger
{

    /// <summary>
    /// Represents the type of log entry
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Low-level developer trace. Not mirrored to installlog.txt; use instead of Note for technical detail.
        /// </summary>
        Diagnostic = -1,

        Verbose = 0,
        Note = 1,
        Warning = 2,
        Error = 3
    }
}

