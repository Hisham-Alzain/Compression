using System;
using System.IO;

namespace Compression
{
    public class BitReader
    {
        private readonly BinaryReader reader;
        private byte currentByte;
        private int bitPosition;
        private bool endOfStream = false;

        public BitReader(BinaryReader reader)
        {
            this.reader = reader;
        }

        public bool? ReadBit()
        {
            if (endOfStream) return null;
            if (bitPosition == 0)
            {
                try
                {
                    currentByte = reader.ReadByte();
                }
                catch (EndOfStreamException)
                {
                    endOfStream = true;
                    return null;
                }
            }

            bool bit = (currentByte & (1 << (7 - bitPosition))) != 0;
            bitPosition = (bitPosition + 1) % 8;
            return bit;
        }
    }
}
