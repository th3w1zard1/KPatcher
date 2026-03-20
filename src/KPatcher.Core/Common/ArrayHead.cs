namespace KPatcher.Core.Common
{
    /// <summary>
    /// Represents an offset/length pair for array data in binary streams.
    /// </summary>
    public struct ArrayHead
    {
        public int Offset { get; set; }
        public int Length { get; set; }

        public ArrayHead(int arrayOffset = 0, int arrayLength = 0)
        {
            Offset = arrayOffset;
            Length = arrayLength;
        }
    }
}

