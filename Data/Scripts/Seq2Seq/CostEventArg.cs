using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StationFramework.Seq2Seq
{
    public class CostEventArg : EventArgs
    {
        public double Cost { get; set; }

        public int Iteration { get; set; }
    }
}
