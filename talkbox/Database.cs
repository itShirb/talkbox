using System;
using MySql.Data.MySqlClient;

namespace talkbox;

public class Database
{
	MySqlConnection connection;
	public Database(MySqlConnection connection)
    {
        this.connection = connection;
    }

    public Task<bool> setVoice(ulong id, string name)
    {
		return Task.FromResult(false);
    }

	public static object? CheckExists(int type, string col1, string table, string col2, object value)
	{
		using var dbCheckExists = new MySqlCommand($"SELECT {col1} FROM {table} WHERE {col2} = {value}"); 
		dbCheckExists.Connection = Program.sqlConnection;
		var data = dbCheckExists.ExecuteScalar();
		return type switch
		{
			0 => Convert.ToUInt64(data) > 0,
			1 => Convert.ToString(data),
			_ => null
		};
	}
}