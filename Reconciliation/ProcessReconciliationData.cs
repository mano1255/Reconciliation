using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;

namespace Reconciliation
{
    static class ProcessReconciliationData
    {
        // <SnippetCreateEmptyDataView>
        static IDataView CreateEmptyDataView(MLContext mlContext)
        {
            // Create empty DataView. We just need the schema to call Fit() for the time series transforms
            IEnumerable<ReconciliationData> enumerableData = new List<ReconciliationData>();
            return mlContext.Data.LoadFromEnumerable(enumerableData);
        }
        // </SnippetCreateEmptyDataView>

        public static IEnumerable<ReconciliationData> PrepareDataForAnomalyCheck(MLContext mlContext, IDataView reconciliationData)
        {
            // data can be referred with a combination of Company, Account, AU, Currency
            IEnumerable<ReconciliationData> reconciliationEnumerable =
                mlContext.Data.CreateEnumerable<ReconciliationData>(reconciliationData, reuseRowObject: true);

            return reconciliationEnumerable;
        }

        public static bool CheckForAnomaly(MLContext mlContext, List<ReconciliationData> reconciliations, int docSize)
        {
            bool isAnomaly = false;

            // Check for anomaly
            var dataView = mlContext.Data.LoadFromEnumerable(reconciliations);

            var iidSpikeEstimator = mlContext.Transforms.DetectSpikeBySsa
                (outputColumnName:nameof(ReconciliationPrediction.Prediction),
                inputColumnName: nameof(ReconciliationData.BalanceDifference),         
                confidence: 95,
                pvalueHistoryLength:docSize/2,
                trainingWindowSize: docSize,
                seasonalityWindowSize:2
            );

            var iidSpikeTransform = iidSpikeEstimator.Fit(CreateEmptyDataView(mlContext));
            IDataView transformedData = iidSpikeTransform.Transform(dataView);

            var predictions = mlContext.Data.CreateEnumerable<ReconciliationPrediction>(transformedData, reuseRowObject: false);

            foreach (var p in predictions)
            {
                if (p.Prediction[0] == 1)
                {
                    isAnomaly = true;
                    break;
                }
            }

            return isAnomaly;
        }

    }
}
