using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Configuration.Models
{
    public class ResultMessage
    {
        public int Id { get; set; }

        public string Task { get; set; }

        public int Precision { get; set; }

        public BigInteger Result { get; set; }
    }
}
