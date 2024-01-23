using KanPublisher;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaretrackerSvc.Services.KanPubSub.KanPublisher
{
    public class SqlDatabseUtility
    {
        private readonly string _connectionString;
        private readonly string _instanceCode;
        public SqlDatabseUtility(string connectionString, string instanceCode)
        {
            _connectionString = connectionString;
            _instanceCode = instanceCode;
        }

        public async Task<List<RTBPublish>> GetPublishConfiguration(string appCode)
        {
            List<RTBPublish> publishes = null;
            string query = @"SELECT configuration_json FROM message_queue_global_configurations WITH(NOLOCK) 
                                WHERE instance_code = '" + _instanceCode + "' and application_code = '" + appCode + "' and action_type = 'publish'";
            List<JObject> publishConfig = null;

            using (var sqlConn = new SqlConnection(_connectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(query, sqlConn);
                sqlCommand.CommandType = CommandType.Text;
                sqlConn.Open();
                SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();
                while (sqlDataReader.Read())
                {
                    publishes = JsonConvert.DeserializeObject<List<RTBPublish>>(Convert.ToString(sqlDataReader["configuration_json"]));
                }
                sqlConn.Close();
            }
            //if (publishConfig == null || publishConfig.Count == 0)
            //    return publishes;



            return publishes;
        }

        public async Task<string> GetInstanceConnetionString(string databaseType)
        {
            string connectionString = string.Empty;
            string query = @"SELECT ConnectionString From InstanceConnectionStrings WITH(NOLOCK)" +
                        " WHERE InstanceCode = '" + _instanceCode + "'" + " AND DatabaseType = '" + databaseType + "'";

            using(var sqlConn = new SqlConnection(_connectionString))
            {
                var sqlCommand = new SqlCommand(query, sqlConn);
                sqlCommand.CommandType = CommandType.Text;
                sqlConn.Open();
                SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();
                while (sqlDataReader.Read())
                {
                    connectionString = sqlDataReader["ConnectionString"] as string;
                }
                sqlConn.Close();
            }
            return connectionString;
        }
    }
}
