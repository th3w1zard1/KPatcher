namespace KPatcher.Core.Formats.NCS
{
    /// <summary>
    /// Base class for NCS optimizers with common functionality.
    /// </summary>
    public abstract class NCSOptimizer
    {
        public int InstructionsCleared { get; protected set; }

        protected NCSOptimizer()
        {
            InstructionsCleared = 0;
        }

        public abstract void Optimize(NCS ncs);

        public virtual void Reset()
        {
            InstructionsCleared = 0;
        }
    }
}

