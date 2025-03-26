using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.ML;
using Newtonsoft.Json;

namespace Reconciliation
{
    public partial class Form1 : Form
    {
        static readonly string _reconciliationHistoryDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "Reconciliation_History.csv");
        static readonly string _reconciliationDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "Reconciliation.csv");
        List<ReconciliationData> processedReconciliations = new List<ReconciliationData>();
        readonly string instanceUrl = "https://dev307521.service-now.com"; // Base URL of the ServiceNow instance
        readonly string tokenEndpoint; // OAuth token endpoint
        readonly string clientId = "16d0da5aa6d14564b6a9e0a04dd047b6"; // Replace with your Client ID
        readonly string clientSecret = "D:1e.;#n&R"; // Replace with your Client Secret
        readonly string username = "admin"; // Replace with your ServiceNow username
        readonly string password = "hewQ3nJ=A4P!"; // Replace with your ServiceNow password

        public Form1()
        {
            InitializeComponent();
            this.Text = "Reconciliation Anomaly Detection";
            tokenEndpoint = $"{instanceUrl}/oauth_token.do";
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                MLContext mlContext = new MLContext();

                // Historical Reconciliation Data
                IDataView historicalReconciliationData = mlContext.Data.LoadFromTextFile<ReconciliationData>(
                    _reconciliationHistoryDataPath, hasHeader: true, separatorChar: ',');

                // Reconciliation Data
                IDataView reconciliationDataView = mlContext.Data.LoadFromTextFile<ReconciliationData>(
                    _reconciliationDataPath, hasHeader: true, separatorChar: ',');

                Dictionary<string, List<ReconciliationData>> historicalReconciliationDataDict =
                    ProcessReconciliationHistoricalData.PrepareDataForAnomalyCheck(mlContext, historicalReconciliationData);


                IEnumerable<ReconciliationData> reconciliations = ProcessReconciliationData.PrepareDataForAnomalyCheck(mlContext, reconciliationDataView);

                if (reconciliations != null && reconciliations.Count() > 0)
                {
                    foreach (ReconciliationData reconciliation in reconciliations)
                    {
                        string key = string.Format("{0}_{1}_{2}_{3}_{4}", reconciliation.Company.ToString(), reconciliation.Account.ToString(),
                            reconciliation.Au.ToString(), reconciliation.Currency.ToUpper(), reconciliation.PrimaryAccount.ToString());

                        List<ReconciliationData> reconciliationData = new List<ReconciliationData>();
                        bool isExistingItem = historicalReconciliationDataDict.TryGetValue(key, out reconciliationData);
                        if (isExistingItem)
                        {
                            reconciliationData.Add(reconciliation);

                            bool isCheckForAnomaly = ProcessReconciliationData.CheckForAnomaly(mlContext, reconciliationData, reconciliationData.Count());
                            if (isCheckForAnomaly)
                            {
                                reconciliation.Anomaly = true;
                                processedReconciliations.Add(reconciliation);
                            }
                        }

                    }
                }
                // Bind Data
                dataGridView1.DataSource = processedReconciliations;
                if (processedReconciliations.Count > 0)
                {
                    button2.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (processedReconciliations.Count > 0)
            {
                //process and send notification
                //Notification message is as below

                //Anamoly detected for the account number 1234,company 1,Currency USD,Primary Account 1234. Please check fro details.
                string accessToken = await GetAccessToken(tokenEndpoint, clientId, clientSecret, username, password);

                if (!string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("Access Token Retrieved Successfully:");
                    Console.WriteLine(accessToken);

                    // Step 3: Create an Incident
                    string description = FormatReconciliationData(processedReconciliations);
                    string incidentId = await CreateIncident(instanceUrl, accessToken, description);

                    MessageBox.Show($"Incident #{incidentId} is created ");
                }
                else
                {
                    MessageBox.Show("Failed to retrieve the access token.");
                }
            }
        }

        static string FormatReconciliationData(List<ReconciliationData> reconciliations)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var reconciliation in reconciliations)
            {
                sb.AppendLine($"Anomaly detected for the account number {reconciliation.Account}, company {reconciliation.Company}, Currency {reconciliation.Currency}, Primary Account {reconciliation.PrimaryAccount}. Please check for details.");
            }
            return sb.ToString();
        }
        static async Task<string> CreateIncident(string instanceUrl, string accessToken,string description)
        {
            string endpoint = $"{instanceUrl}/api/now/table/incident";

            // Create an incident object
            var incident = new
            {
                short_description = "Anamolies detected in the reconciliations sheet",
                description = description,
                urgency = "2", // 1 = High, 2 = Medium, 3 = Low
                impact = "2"
            };

            using (HttpClient client = new HttpClient())
            {
                // Add the Bearer token to the authorization header
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Serialize the incident object to JSON
                string jsonData = JsonConvert.SerializeObject(incident);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Make the POST request to create the incident
                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                string result = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Incident created successfully!");
                    Console.WriteLine(result);

                    // Parse the response to get the incident sys_id
                    var jsonResponse = JsonConvert.DeserializeObject<dynamic>(result);
                    return jsonResponse?.result?.number;
                }
                else
                {
                    Console.WriteLine($"Failed to create incident. Status Code: {response.StatusCode}");
                    Console.WriteLine($"Error Response: {result}");
                    return null;
                }
            }
        }
        static async Task<string> GetAccessToken(string tokenEndpoint, string clientId, string clientSecret, string username, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                // Prepare the request with key-value pairs for the form-urlencoded body
                var collection = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password)
                };

                // Prepare the HttpRequestMessage
                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(collection)
                };

                // Send the HTTP request
                HttpResponseMessage response = await client.SendAsync(request);

                // Ensure the response indicates success
                if (response.IsSuccessStatusCode)
                {
                    // Read and return the access token
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return jsonResponse?.access_token;
                }
                else
                {
                    // Log error details
                    Console.WriteLine($"Failed to get access token. Status Code: {response.StatusCode}");
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Response: {errorContent}");
                    return null;
                }
            }
        }


    }
}

