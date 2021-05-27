using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace blobparser
{
    public static class blobparser
    {
        public static string _tableName;
        public static ILogger _log;
        public static string _sqlconnectionString;
        public static bool _createdTable = false;
        [FunctionName("blobparser")]
        public static void Run([BlobTrigger("iotdata/{name}", Connection = "storageconnection")]Stream myBlob, string name, ILogger log, ExecutionContext context)
        {
            //log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
                        _log = log;

            if (name.EndsWith(".json"))
            {
                var config = new ConfigurationBuilder()
                     .SetBasePath(context.FunctionAppDirectory)
                     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables()
                     .Build();
                string myConnectionString = config["ConnectionStrings:SQLConnectionString"];
                _sqlconnectionString = myConnectionString;
                string table_name = config["table_name"];
                _tableName = table_name;


                ParseBlob(myBlob, name);


            }

            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
         private static void CreateSQLTable()
        {

            _log.LogInformation($"Creating table if does not exist: {_tableName}");
            using (SqlConnection con = new SqlConnection(_sqlconnectionString))
            {
                string sql = @$"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{_tableName}]') AND type in (N'U'))
                                BEGIN
                                CREATE TABLE [dbo].[{_tableName}](
	                                [DeviceID] [varchar](800) NULL,
	                                [TagID] [varchar](800) NULL,
	                                [ModelID] [varchar](800) NULL,
	                                [V] [varchar](800) NULL,
	                                [Q] [varchar](800) NULL,
	                                [T] [varchar](800) NULL,
	                                [MimeType] [varchar](800) NULL
                                ) ON [PRIMARY]
                                END";
                using (SqlCommand cmd = new SqlCommand(sql))
                {
                    cmd.Connection = con;
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
            _createdTable = true;

        }
        private static void ParseBlob(Stream myBlob, string name)
        {

            try
            {


                var jsonReader = new JsonTextReader(new StreamReader(myBlob))
                {
                    SupportMultipleContent = true
                };

                var jsonSerializer = new JsonSerializer();
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.StartObject)
                    {
                        var jo = JObject.Load(jsonReader);
                        var deviceID = jo["SystemProperties"]["connectionDeviceId"].ToString();
                        var encodedbody = jo["Body"].ToString();

                        var body = Base64StringDecode(encodedbody);
                        var kvp = new KeyValuePair<string, string>(deviceID, body);

                        var dt = Tabulate(kvp);
                        WritetoDB(dt);

                    }
                    else
                    {
                        continue;
                    }
                }



            }
            catch (Exception e)
            {
                _log.LogError($"Error when processing {name}.  Exception: {e.Message}.");
            }

        }
        public static DataTable Tabulate(KeyValuePair<string, string> payload)
        {
            var jsonLinq = JObject.Parse(payload.Value);

            // Find the first array using Linq
            var srcArray = jsonLinq.Descendants().Where(d => d is JArray).First();
            var trgArray = new JArray();
            foreach (JObject row in srcArray.Children<JObject>())
            {
                var cleanRow = new JObject();
                cleanRow.Add("DeviceID", payload.Key);
                foreach (JProperty column in row.Properties())
                {
                    // Only include JValue types
                    if (column.Value is JValue)
                    {
                        cleanRow.Add(column.Name, column.Value);
                    }
                    else
                    {
                       foreach (JObject vqt in column.Value)
                        {
                            var cleanVQT = new JObject();
                            foreach(JProperty col in vqt.Properties())
                            {
                                if(col.Value is JValue)
                                {
                                    cleanRow.Add(col.Name, col.Value.ToString());
                                }
                            }
                        }
                    }
                }
                trgArray.Add(cleanRow);
            }
            return JsonConvert.DeserializeObject<DataTable>(trgArray.ToString());
        }
        public static void WritetoDB(DataTable dt)
        {
            if(_createdTable == false)
            {
                CreateSQLTable();
            }
            SqlBulkCopy bulkcopy = new SqlBulkCopy(_sqlconnectionString);
            bulkcopy.DestinationTableName = _tableName;

            try
            {
                bulkcopy.WriteToServer(dt);
            }
            catch (Exception e)
            {
                _log.LogError(e.Message.ToString());
            }
            _log.LogInformation("Wrote some JSON to SQL");
        }
        public static string Base64StringDecode(string encodedString)
        {
            var bytes = Convert.FromBase64String(encodedString);

            var decodedString = Encoding.UTF8.GetString(bytes);

            return decodedString;
        }




    }
}
