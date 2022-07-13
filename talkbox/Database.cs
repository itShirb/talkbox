using MySql.Data.MySqlClient;
using Newtonsoft.Json;
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
            public class Voice
            {
                [JsonProperty(PropertyName = "name")]
                public string Name { get; set; }

                [JsonProperty(PropertyName = "languageCode")]
                public string Language { get; set; }
                
                [JsonProperty(PropertyName = "ssmlGender")]
                public string Gender { get; set; }

                [JsonIgnore]
                public float Rate { get; set; }

                [JsonIgnore]
                public float Pitch { get; set; }

                public Voice(string name, string languageCode, string ssmlGender, float rate = 1.0f, float pitch = 0.0f)
                {
                    Name = name;
                    Language = languageCode;
                    Gender = ssmlGender;
                    Rate = rate;
                    Pitch = pitch;
                }
            }
            public static async Task<Voice> GetVoice(ulong id)
            {
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT voice_name, voice_lang, voice_gender, voice_rate, voice_pitch FROM user_data WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        await reader.ReadAsync();
                        return new Voice(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetFloat(3), reader.GetFloat(4));
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
                    cmd.CommandText = "INSERT INTO user_data (user_id, voice_name, voice_lang, voice_gender, voice_rate, voice_pitch) VALUES(@id, @name, @lang, @gender, @rate, @pitch)";
                    cmd.Parameters.AddWithValue("@name", voice.Name);
                    cmd.Parameters.AddWithValue("@lang", voice.Language);
                    cmd.Parameters.AddWithValue("@gender", voice.Gender);
                    cmd.Parameters.AddWithValue("@rate", voice.Rate);
                    cmd.Parameters.AddWithValue("@pitch", voice.Pitch);
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}