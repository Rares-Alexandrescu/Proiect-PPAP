using Microsoft.Data.SqlClient;

namespace PPAP_Proiect.Data
{
	public class DBConectare
	{
		private readonly IConfiguration _config;

		public DBConectare(IConfiguration config)
		{
			_config = config;
		}

		public SqlConnection GetConnection()
		{
			var connStr = _config.GetConnectionString("DefaultConnection");
			var conn = new SqlConnection(connStr);
			conn.Open();
			return conn;
		}
	}
}
