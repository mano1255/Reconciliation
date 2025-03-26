using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;

namespace Reconciliation
{
    static class ProcessReconciliationHistoricalData
    {
        public static void Process(MLContext mlContext, int docSize, IDataView reconciliationData)
        {
            // DetectSpike(mlContext, docSize, reconciliationData);
        }

        // <SnippetCreateEmptyDataView>
        static IDataView CreateEmptyDataView(MLContext mlContext)
        {
            // Create empty DataView. We just need the schema to call Fit() for the time series transforms.
            List<ReconciliationData> enumerableData = new List<ReconciliationData>();
            return mlContext.Data.LoadFromEnumerable(enumerableData);
        }
        // </SnippetCreateEmptyDataView>

        // Prepare Data for Anomaly Check
        public static Dictionary<string, List<ReconciliationData>> PrepareDataForAnomalyCheck(MLContext mlContext, IDataView historicalReconciliationData)
        {
            // Data can be referred with a combination of Company, Account, AU, Currency
            Dictionary<string, List<ReconciliationData>> dictDataForAnomaly = new Dictionary<string, List<ReconciliationData>>();
            IEnumerable<ReconciliationData> reconciliationEnumerable = mlContext.Data.CreateEnumerable<ReconciliationData>(historicalReconciliationData, reuseRowObject: true);

            foreach (var item in reconciliationEnumerable)
            {
                List<ReconciliationData> reconciliations;
                string key = string.Format("{0}_{1}_{2}_{3}_{4}", item.Company.ToString(), item.Account.ToString(), item.Au.ToString(), item.Currency.ToString(), item.PrimaryAccount.ToUpper());
                if (!dictDataForAnomaly.TryGetValue(key, out reconciliations)) {
                    reconciliations = new List<ReconciliationData>();
                    dictDataForAnomaly.Add(key, reconciliations);
                }
                reconciliations.Add(item);
            }

            return dictDataForAnomaly;
        }
    }
}
