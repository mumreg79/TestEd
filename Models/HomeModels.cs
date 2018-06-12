using System;
using System.Collections.Generic;

namespace TestEdison.Models
{

    public class MediumVar
    {
        public int MediumData { get; set; }
        public int ValidData { get; set; }
    }

    public class Request
    {
        public int UserID { get; set; }
        public DateTime DateTime { get; set; }
        public int ReqValue { get; set; }
        public List<MediumVar> MediumsResult { get; set; }
    }
}