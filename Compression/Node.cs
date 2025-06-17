using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compression
{
    public class Node : IComparable<Node>
    {
        public byte Symbol { get; set; }
        public int Frequency { get; set; }
        public string Code { get; set; }

        // only for huffman
        public Node Left { get; set; }
        public Node Right { get; set; }

        public int CompareTo(Node other)
        {
            return Frequency - other.Frequency;
        }

        public bool IsLeaf()
        {
            return Left == null && Right == null;
        }
    }
}
