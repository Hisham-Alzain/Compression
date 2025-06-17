using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compression
{
    public class Symbol
    {
        public byte character { get; set; }
        public int frequency { get; set; }
        public string code { get; set; }
    }
}
