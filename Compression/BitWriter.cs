using System.IO;

namespace Compression
{
    public class BitWriter
    {
        private readonly BinaryWriter writer;
        private byte currentByte = 0;
        private int bitPosition = 0;

        public BitWriter(BinaryWriter writer)
        {
            this.writer = writer;
        }

        public void WriteBit(bool value)
        {
            if (value)
                currentByte |= (byte)(1 << (7 - bitPosition));

            bitPosition++;
            if (bitPosition == 8)
            {
                writer.Write(currentByte);
                currentByte = 0;
                bitPosition = 0;
            }
        }

        public void Flush()
        {
            if (bitPosition > 0)
            {
                writer.Write(currentByte);
                currentByte = 0;
                bitPosition = 0;
            }
        }
    }
}
