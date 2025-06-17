using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compression
{
    public class BitReader
    {
        private readonly BinaryReader reader;
        private byte currentByte;
        private int bitPosition;
        private bool endOfStream;

        public BitReader(BinaryReader reader)
        {
            this.reader = reader;
        }

        public bool? ReadBit()
        {
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
