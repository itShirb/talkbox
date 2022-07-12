using MySql.Data.MySqlClient;
using System.Collections;
using System.Data.Common;
using System.Linq.Expressions;

namespace talkbox
{
    public static class Database
    {
        public static MySqlConnection Connection;
        public static class Guilds
        {
            public static async Task<string> GetPrefix(ulong id)
            {
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT guild_prefix FROM guild_data WHERE guild_id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        return reader.GetString(0);
                    }
                }
            }
            public static async Task SetPrefix(ulong id, string prefix)
            {
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = "REPLACE INTO guild_data (guild_id, guild_prefix) VALUES(@id, @prefix)";
                    cmd.Parameters.AddWithValue("@prefix", prefix);
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            public static async Task<ulong?> GetRole(ulong id)
            {
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT guild_id FROM guild_data WHERE guild_id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        if (reader.IsDBNull(0)) return null;
                        else return (ulong?)reader.GetValue(0);
                    }
                }
            }
            public static async Task SetRole(ulong id, ulong? role)
            {
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = "REPLACE INTO guild_data (guild_id, guild_role) VALUES(@id, @role)";
                    cmd.Parameters.AddWithValue("@role", role);
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

        }
        public static class Users
        {
            public record Voice(string name, string languageCode, string ssmlGender);
            public static async Task<Voice> GetVoice(ulong id)
            {
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT voice_name, voice_lang, voice_gender FROM user_data WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        return new Voice(reader.GetString(0), reader.GetString(1), reader.GetString(2));
                    }
                }
            }

            public static async Task SetVoice(ulong id, Voice voice)
            {
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE IGNORE FROM user_data WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO user_data (user_id, voice_name, voice_lang, voice_gender) VALUES(@id, @name, @lang, @gender)";
                    cmd.Parameters.AddWithValue("@name", voice.name);
                    cmd.Parameters.AddWithValue("@lang", voice.languageCode);
                    cmd.Parameters.AddWithValue("@gender", voice.ssmlGender);
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}