using System;
namespace durableFA_validation.Helper
{
    public class TxDataTracker
    {
        public Customertechnicalheader customerTechnicalHeader { get; set; }
    }

    public class Customertechnicalheader
    {
        public string correlationId { get; set; }
    }
}

