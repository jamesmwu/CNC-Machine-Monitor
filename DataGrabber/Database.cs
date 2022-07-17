using System;


using MySqlConnector;

namespace DataGrabberApp
{
	public static class Database
	{
		public static MySqlConnection DBConnection;

		public static MySqlCommand CreateCommand(string dbName)
		{
			MySqlCommand cmd = new MySqlCommand();

			try
			{
				string connString = System.Configuration.ConfigurationManager.ConnectionStrings["cnc-machine-db"].ConnectionString;

				DBConnection = new MySqlConnection(connString);
				DBConnection.Open();
				cmd = DBConnection.CreateCommand();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			return cmd;
		}

		public static int Count(string db, string table)
		{
			MySqlCommand cmd = Database.CreateCommand(db);

			cmd.CommandText = "SELECT COUNT(*) FROM " + table;

			return Convert.ToInt32(cmd.ExecuteScalar());
		}

		public static MySqlDataReader SelectAll(string db, string table)
		{
			MySqlCommand cmd = Database.CreateCommand(db);

			cmd.CommandText = "SELECT * FROM " + table;

			MySqlDataReader reader = cmd.ExecuteReader();

			return reader;
		}

		public static MySqlDataReader SelectWhere(string db, string table, string query, string value)
		{
			MySqlCommand cmd = Database.CreateCommand(db);

			cmd.CommandText = "SELECT * FROM " + table + " WHERE " + query + " = '" + value + "'";

			MySqlDataReader reader = cmd.ExecuteReader();

			return reader;
		}

		public static string SelectWhere(string db, string table, string column, string query, string value)
		{
			MySqlCommand cmd = Database.CreateCommand(db);

			cmd.CommandText = "SELECT " + column + " FROM " + table + " WHERE " + query + " = '" + value + "'";

			return cmd.ExecuteScalar().ToString();
		}

		public static void RemoveWhere(string db, string table, string column, string value)
		{
			MySqlCommand cmd = Database.CreateCommand(db);

			cmd.CommandText = "DELETE FROM " + table + " WHERE " + column + " = '" + value + "'";

			cmd.ExecuteNonQuery();

			Database.DBConnection.Close();
		}

		public static void UpdateWhere(string db, string table, string updateField, string updateValue, string query, string value)
		{
			MySqlCommand cmd = Database.CreateCommand(db);

			cmd.CommandText = "UPDATE " + table + " SET " + updateField + " = '" + updateValue + "' WHERE " + query + " = '" + value + "'";

			cmd.ExecuteNonQuery();

			Database.DBConnection.Close();
		}

		public static void Close()
		{
			DBConnection.Close();
		}
	}
}
