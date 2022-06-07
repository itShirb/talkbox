using System;
using System.Threading.Tasks;
using Discord.Commands;
using MySql.Data.MySqlClient;

namespace talkbox.Unused
{
	public class DatabaseTestModule// : ModuleBase<SocketCommandContext>
    {
    	[Command("dbtest")]
    	[Summary("database test")]
    	[Alias("db")]
    	public Task DbAsync(
    		[Summary("[function]")] string func = null, 
    		[Summary("[table]")] string tbl = null,
    		[Summary("(column)")] string col = null,
    		[Summary("(value)")] string value = null)
    	{
    		var tblExist = false;
    		if (func is null || tbl is null)
    		{
    			// await ReplyAsync(CommandHandler.ReturnCommandUsage("dbtest").Result);
    			return Task.CompletedTask;
    		}
    			
    		if (func != "select" && func != "insert" && func != "update" && func != "delete")
    		{
    			// await ReplyAsync("Invalid function. Valid functions are:\n`select`, `insert`, `update`, `delete`");
    			return Task.CompletedTask;
    		}
    
    		// var chkTbl = new MySqlCommand("select count(*) from @table;");
    		var chkTbl = new MySqlCommand("SET @sql:=CONCAT('SELECT * FROM ', @table_name);" +
    		                              "PREPARE dynamic_statement FROM @sql;" +
    		                              "EXECUTE dynamic_statement;" +
    		                              "DEALLOCATE PREPARE dynamic_statement");
    		chkTbl.Connection = Program.SqlCon;
    		chkTbl.Parameters.AddWithValue("@table_name", tbl);
    		try
    		{
    			var tblExists = (int)chkTbl.ExecuteScalar();
    			Console.WriteLine(tblExists);
    			if (tblExists > 0) tblExist = true;
    			else
    			{
    				// await ReplyAsync("Table does not exist.");
    				return Task.CompletedTask;
    			}
    		}
    		catch (Exception err)
    		{
    			// await ReplyAsync(err.ToString());
    		}
    
    		if (func == "select" && tblExist)
    		{
    			// await ReplyAsync("Found table");
    		}
    
    		return Task.CompletedTask;
    
    		// using (var cmd = new MySqlCommand($"{func}")) ;
    	}
    }	
}