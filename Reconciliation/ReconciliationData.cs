using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;
namespace Reconciliation
{
    public class ReconciliationData
    {
        [LoadColumn(0)]
        public DateTime AsOfDate { get; set; }

        [LoadColumn(1)]
        public int Company { get; set; }

        [LoadColumn(2)]
        public int Account { get; set; }

        [LoadColumn(3)]
        public int Au { get; set; }

        [LoadColumn(4)]
        public string Currency { get; set; }

        [LoadColumn(5)]
        public string PrimaryAccount { get; set; }

        [LoadColumn(6)]
        public string SecondaryAccount { get; set; }

        [LoadColumn(7)]
        public float GLBalance { get; set; }

        [LoadColumn(8)]
        public float InHubBalance { get; set; }

        [LoadColumn(9)]
        public float BalanceDifference { get; set; }

        [LoadColumn(10)]
        public string MatchStatus { get; set; }

        [LoadColumn(11)]
        public string Comments { get; set; }

        [LoadColumn(12)]
        public Boolean Anomaly { get; set; }       

    }

    public class ReconciliationPrediction
    {
        [VectorType(3)]
        public double[] Prediction { get; set; }
    }
}
