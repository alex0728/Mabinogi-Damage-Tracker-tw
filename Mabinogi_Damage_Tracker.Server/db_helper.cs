using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Mabinogi_Damage_tracker.Models;
using Mabinogi_Damage_Tracker;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;

namespace Mabinogi_Damage_tracker
{
    public class db_helper
    {
        private static string db_connection = @"Data Source=trackerdb.db;";

        public static void Initalize_db()
        {
            using (SqliteConnection connection = new SqliteConnection(db_connection))
            {
                connection.Open();
                SqliteCommand sqliteCommand = connection.CreateCommand();
                //create the playerid table
                string create_playerid = @"
                CREATE TABLE IF NOT EXISTS players (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    playerid NUMERIC NOT NULL UNIQUE,
                    playername VARCHAR(48) NOT NULL )";

                //create the damage table
                string create_damage = @"
                CREATE TABLE IF NOT EXISTS damages (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    playerid NUMERIC NOT NULL,
                    damage NUMERIC NOT NULL,
                    wound NUMERIC,
                    manadamage NUMERIC,
                    enemyid NUMERIC NOT NULL,
                    skill INT NOT NULL,
                    subskill INT NOT NULL,
                    dt TEXT NOT NULL,
                    ut INTEGER NOT NULL)";

                //create the healing table
                string create_heal = @"
                CREATE TABLE IF NOT EXISTS heals (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    healer NUMERIC NOT NULL,
                    heal NUMERIC NOT NULL,
                    recipient NUMERIC NOT NULL,
                    dt TEXT NOT NULL,
                    ut INTEGER NOT NULL)";

                //create the recording table
                string create_recording = @"
                CREATE TABLE IF NOT EXISTS recordings (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT,
                    start_ut INTEGER NOT NULL,
                    end_ut INTEGER)";

                string create_adapter = @"
                    CREATE TABLE IF NOT EXISTS local_adapter(
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        adapter TEXT
                    )";


                sqliteCommand.CommandText = create_playerid;
                sqliteCommand.ExecuteNonQuery();
                sqliteCommand.CommandText = create_damage;
                sqliteCommand.ExecuteNonQuery();
                sqliteCommand.CommandText = create_heal;
                sqliteCommand.ExecuteNonQuery();
                sqliteCommand.CommandText = create_recording;
                sqliteCommand.ExecuteNonQuery();
                sqliteCommand.CommandText = create_adapter;
                sqliteCommand.ExecuteNonQuery();
            }
        }

        public static void add_player(string playername, Int64 playerid)
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand add_command = new SqliteCommand(@"
                    INSERT OR IGNORE INTO players (playerid, playername)
                        VALUES(@playerid,@playername)
                    ", connection);
                    add_command.Parameters.AddWithValue("@playerid", playerid);
                    add_command.Parameters.AddWithValue("@playername", playername);
                    add_command.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                Debug.WriteLine("could not send sql command");
            }
        }

        public static List<object> Get_All_Players()
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand(@"
                    SELECT * FROM players
                    ", connection);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows == false) { return null; }
                        var query_results = new List<object>();
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(reader.GetOrdinal("id"));
                            long playerId = reader.GetInt64(reader.GetOrdinal("playerid"));
                            string playerName = reader["playername"]?.ToString() ?? $"Player {playerId}";

                            query_results.Add(new 
                            { 
                                id = id,
                                playerId = playerId,
                                playerName = playerName
                            });
                        }

                        return query_results;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static void add_damage(Int64 playerid, double damage, double wound, int manadamage, Int64 enemyid, int skill, int subskill)
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand add_command = new SqliteCommand(@"
                    INSERT INTO damages (playerid, damage, wound, manadamage, enemyid, skill, subskill, dt, ut)
                        VALUES(@id,@dmg,@wound,@manadamage,@enemyid,@skill,@subskill,datetime(), unixepoch())
                    ", connection);
                    add_command.Parameters.AddWithValue("@id", playerid);
                    add_command.Parameters.AddWithValue("@dmg", damage);
                    add_command.Parameters.AddWithValue("@wound", wound);
                    add_command.Parameters.AddWithValue("@manadamage", manadamage);
                    add_command.Parameters.AddWithValue("@enemyid", enemyid);
                    add_command.Parameters.AddWithValue("@skill", skill);
                    add_command.Parameters.AddWithValue("@subskill", subskill);
                    add_command.ExecuteNonQueryAsync();
                }
            }
            catch 
            {
                Debug.WriteLine("couldnt send sql command");
            }
        }

        public static Damage_Simple Get_Largest_Single_Damage_Instance(int start_ut, int end_ut)
        {
            return Get_ListOf_Distinct_Largest_Single_Damage_Instance(start_ut, end_ut, 1)[0];
        }

        public static List<Damage_Simple> Get_ListOf_Distinct_Largest_Single_Damage_Instance(int start_ut, int end_ut, int count)
        {
            List<Damage_Simple> query_results = new List<Damage_Simple>();
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand(@"
                    SELECT distinct damages.playerid, MAX(damage) AS mx_damage, playername, damages.ut
                    FROM damages
                    left join players on damages.playerid = players.playerid
                    WHERE ut BETWEEN @start_ut AND @end_ut
                    GROUP by damages.playerid 
                    order by mx_damage DESC
                    limit @count
                    ", connection);

                    command.Parameters.AddWithValue("@start_ut", start_ut);
                    command.Parameters.AddWithValue("@end_ut", end_ut);
                    command.Parameters.AddWithValue("@count", count);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows == false) { return null; }
                        while (reader.Read())
                        {
                            long playerId = reader.GetInt64(reader.GetOrdinal("playerid"));
                            string playerName = reader.IsDBNull(reader.GetOrdinal("playername")) ? $"{playerId}" : reader.GetString(reader.GetOrdinal("playername"));
                            double dmg = reader.GetDouble(reader.GetOrdinal("mx_damage"));
                            Int32 ut = reader.GetInt32(reader.GetOrdinal("ut"));
                            query_results.Add(new Damage_Simple( dmg, playerId, playerName, ut));
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
            return query_results;
           
        }

        public static Int64 Get_Last_Damage_Row_Id()
        {
            Int64 query_results = 0;
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand(@"
                    SELECT MAX(id) FROM damages;
                    ", connection);
                    query_results = (Int64)command.ExecuteScalar();
                }
            }
            catch
            {
                return 0;
            }
            return query_results;
        }

        public static void add_heal (UInt64 healer, UInt64 recipient, UInt32 heal)
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand add_command = new SqliteCommand(@"
                    INSERT INTO heals (healer, heal, recipient, dt, ut)
                        VALUES(@healer,@heal,@rec,datetime(), unixepoch())
                    ", connection);
                    add_command.Parameters.AddWithValue("@healer", healer);
                    add_command.Parameters.AddWithValue("@heal", heal);
                    add_command.Parameters.AddWithValue("@rec", recipient);
                    add_command.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                Debug.WriteLine("couldnt send sql command");
            }
        }

        public static int Get_SumHeals_BetweenUT(int start_ut, int end_ut)
        {
            int query_result = 0;
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand(@"
                    SELECT SUM(heals.heal) from heals
                    WHERE heals.ut BETWEEN @start_ut AND @end_ut
                    ", connection);
                    command.Parameters.AddWithValue("@start_ut", start_ut);
                    command.Parameters.AddWithValue("@end_ut", end_ut);
                    object result = command.ExecuteScalar();
                    return result == DBNull.Value ? 0 : Convert.ToInt32(result);
                }
            }
            catch
            {
                return 0;
            }
        }

        public static void add_recording(string name, int start_ut, int end_ut)
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand add_command = new SqliteCommand(@"
                    INSERT INTO recordings (name, start_ut, end_ut)
                        VALUES(@name,@start_ut,@end_ut)
                    ", connection);
                    add_command.Parameters.AddWithValue("@name", name);
                    add_command.Parameters.AddWithValue("@start_ut", start_ut);
                    add_command.Parameters.AddWithValue("@end_ut", end_ut);
                    add_command.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                Debug.WriteLine("couldnt send sql command");
            }
        }

        public static void delete_recording(int id)
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand delete_command = new SqliteCommand(@"
                    DELETE FROM recordings
                    WHERE recordings.id = @id
                    ", connection);
                    delete_command.Parameters.AddWithValue("@id", id);
                    delete_command.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                Debug.WriteLine("Couldn't Delete Recording Id: ", id);
            }
        }

        public static void update_recording_name(int id, string name)
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand add_command = new SqliteCommand(@"
                    UPDATE recordings
                    SET name = @name
                    WHERE id = @id
                    ", connection);
                    add_command.Parameters.AddWithValue("@name", name);
                    add_command.Parameters.AddWithValue("@id", id);
                    add_command.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                Debug.WriteLine("couldnt send sql command");
            }
        }

        public static List<Models.Damage_Simple> Get_TotalDamage_ByPlayers()
        {
            List<Models.Damage_Simple> query_results = new List<Models.Damage_Simple>();
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand(@"
                    SELECT damages.playerid, SUM( damage), playername
                        FROM damages
                        left join players on damages.playerid = players.playerid
                        group by damages.playerid 
                    ", connection);
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows == false) { return null; }
                        while (reader.Read())
                        {
                            string name = "";
                            if (reader.IsDBNull(2) != true)
                            {
                                name = reader.GetString(2);
                            }
                            if (name == "")
                            {
                                name = reader.GetInt64(0).ToString();
                            }
                            query_results.Add(new Models.Damage_Simple(reader.GetDouble(1), reader.GetInt64(0), name));
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
            return query_results;
        }

        public static List<Models.Damage_Simple> Get_Damages_Between_Ut(Int32 start_ut, Int32 end_ut)
        {
            List<Models.Damage_Simple> query_results = new List<Models.Damage_Simple>();
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    using (SqliteCommand command = new SqliteCommand(@"
                        SELECT damages.id, damages.playerid, damage, playername, ut
                        FROM damages
                        left join players on damages.playerid = players.playerid
                        WHERE damages.ut BETWEEN @start_ut and @end_ut
                        ORDER BY ut ASC;
                    ", connection))
                    {

                        command.Parameters.AddWithValue("@start_ut", start_ut);
                        command.Parameters.AddWithValue("@end_ut", end_ut);

                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows == false) { return null; }

                            while (reader.Read())
                            {
                                long playerId = reader.GetInt64(reader.GetOrdinal("playerid"));
                                string playerName = reader.IsDBNull(reader.GetOrdinal("playername")) ? $"{playerId}" : reader.GetString(reader.GetOrdinal("playername"));
                                double dmg = reader.GetDouble(reader.GetOrdinal("damage"));
                                Int32 ut = reader.GetInt32(reader.GetOrdinal("ut"));

                                query_results.Add(new Damage_Simple(dmg, playerId, playerName, ut));
                            }
                        }

                        return query_results;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
                return null;
            }

        }

        public static object Get_AllDamages_GroupedByPlayers_AfterId(Int32 lastFetchedId)
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    using (SqliteCommand command = new SqliteCommand(@"
                        SELECT damages.id, damages.playerid, damage, playername, ut
                        FROM damages
                        left join players on damages.playerid = players.playerid
                        WHERE damages.id > @lastFetchedId
                        ORDER BY ut ASC;
                    ", connection))
                    {

                        command.Parameters.AddWithValue("@lastFetchedId", lastFetchedId);


                        var players = new Dictionary<long, string>();
                        var buckets = new Dictionary<long, Dictionary<long, double>>();

                        long firstUt = -1;
                        long lastUt = -1;
                        long last_id = 0;

                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows == false) { return null; }

                            while (reader.Read())
                            {
                                last_id = reader.GetInt64(reader.GetOrdinal("id"));
                                long playerId = reader.GetInt64(reader.GetOrdinal("playerid"));
                                string playerName = reader.IsDBNull(reader.GetOrdinal("playername")) ? $"{playerId}" : reader.GetString(reader.GetOrdinal("playername"));
                                double dmg = reader.GetDouble(reader.GetOrdinal("damage"));
                                long ut = reader.GetInt64(reader.GetOrdinal("ut"));

                                // mark the first timestamp
                                if (firstUt == -1) firstUt = ut;
                                // keep updating to know the last timestamp
                                lastUt = ut;

                                players[playerId] = playerName;

                                if (!buckets.ContainsKey(ut)) // have we seen this second before?
                                    // Initialize this seconds Dictionary of PlayerIDs and their Damage for this second
                                    buckets[ut] = new Dictionary<long, double>();

                                if (!buckets[ut].ContainsKey(playerId)) // have we seen this player do damage during this bucket (second)?
                                    // If the player does not exist in this timestamps dictionary add them.
                                    buckets[ut][playerId] = 0;

                                // this represents the accumulated damage that a player did at a particular second of time
                                buckets[ut][playerId] += dmg;
                            }
                        }

                        // We do this to fill in any holes where no damage was done.
                        var filledBuckets = new List<long>();
                        for (long ut = firstUt; ut <= lastUt; ut++)
                        {
                            filledBuckets.Add(ut);
                            if (!buckets.ContainsKey(ut))
                                buckets[ut] = new Dictionary<long, double>(); // empty bucket
                        }

                        var finalSeries = new List<object>();

                        // iterate through all the players building their json
                        foreach (var p in players)
                        {
                            long playerId = p.Key;
                            string playerName = p.Value;

                            // this data structure holds the timeline of damages in buckets of seconds for each player. 
                            var dataArray = new List<double>();

                            // For each second in the buckets array
                            foreach (var b in filledBuckets)
                            {
                                // If a player did damage during this second add it to the data array otherwise add 0 for this second
                                double v = buckets[b].ContainsKey(playerId)
                                    ? buckets[b][playerId]
                                    : 0;

                                dataArray.Add(v);
                            }

                            // final json of player
                            finalSeries.Add(new
                            {
                                id = playerId,
                                label = playerName,
                                data = dataArray
                            });
                        }
                        // we return a reference to the last_id so that the next query can use it instead of having to call Get_Last_Damage_Row_Id()
                        object result = new { lastId = last_id, data = finalSeries };
                        return result;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
                return null;
            }
        }

        public static List<object> Get_AllDamages_GroupedByPlayers_BetweenUT(Int32 start_ut, Int32 end_ut)
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    using (SqliteCommand command = new SqliteCommand(@"
                        SELECT damages.playerid, damage, playername, ut
                        FROM damages
                        left join players on damages.playerid = players.playerid
                        where ut BETWEEN @start_ut AND @end_ut
                        ORDER BY ut;
                    ", connection))
                    {

                        command.Parameters.AddWithValue("@start_ut", start_ut);
                        command.Parameters.AddWithValue("@end_ut", end_ut);


                        var players = new Dictionary<long, string>();
                        var buckets = new Dictionary<long, Dictionary<long, double>>();

                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows == false) { return null; }
                            while (reader.Read())
                            {
                                long playerId = reader.GetInt64(reader.GetOrdinal("playerid"));
                                string playerName = reader.IsDBNull(reader.GetOrdinal("playername")) ? $"{playerId}" : reader.GetString(reader.GetOrdinal("playername"));
                                double dmg = reader.GetDouble(reader.GetOrdinal("damage"));
                                long ut = reader.GetInt64(reader.GetOrdinal("ut"));

                                long bucket = ut;

                                players[playerId] = playerName;

                                if (!buckets.ContainsKey(bucket))
                                    buckets[bucket] = new Dictionary<long, double>();

                                if (!buckets[bucket].ContainsKey(playerId))
                                    buckets[bucket][playerId] = 0;

                                buckets[bucket][playerId] += dmg;
                            }
                        }

                        var sortedBuckets = buckets.Keys.OrderBy(x => x).ToList();

                        var finalSeries = new List<object>();

                        // iterate through all the players building their json
                        foreach (var p in players)
                        {
                            long playerId = p.Key;
                            string playerName = p.Value;

                            var dataArray = new List<double>();

                            // Build data array for player
                            foreach (var b in sortedBuckets)
                            {
                                double v = buckets[b].ContainsKey(playerId)
                                    ? buckets[b][playerId]
                                    : 0;

                                dataArray.Add(v);
                            }

                            // final json of player
                            finalSeries.Add(new
                            {
                                id = playerId,
                                label = playerName,
                                data = dataArray
                            });
                        }

                        return finalSeries;
                    }

                }
            }
            catch
            {
                return null;
            }
        }

        //rewrite so this calls get_damage_groupedbypalyers_betweenUT
        public static List<object> Get_AggregatedDamage_GroupedByPlayers_BetweenUT(int start_ut, int end_ut)
        {
            try
            {
                using (var connection = new SqliteConnection(db_connection))
                {
                    connection.Open();

                    using (var command = new SqliteCommand(@"
                        SELECT damages.playerid, damage, playername, ut
                        FROM damages
                        left join players on damages.playerid = players.playerid
                        where ut BETWEEN @start_ut AND @end_ut
                        ORDER BY ut;
                    ", connection))
                    {
                        command.Parameters.AddWithValue("@start_ut", start_ut);
                        command.Parameters.AddWithValue("@end_ut", end_ut);

                        var players = new Dictionary<long, string>();
                        var buckets = new Dictionary<long, Dictionary<long, double>>();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                long playerId = reader.GetInt64(reader.GetOrdinal("playerid"));
                                string playerName = reader.IsDBNull(reader.GetOrdinal("playername")) ? $"{playerId}" : reader.GetString(reader.GetOrdinal("playername"));
                                double dmg = reader.GetDouble(reader.GetOrdinal("damage"));
                                long ut = reader.GetInt64(reader.GetOrdinal("ut"));

                                players[playerId] = playerName;

                                if (!buckets.ContainsKey(ut))
                                    buckets[ut] = new Dictionary<long, double>();

                                if (!buckets[ut].ContainsKey(playerId))
                                    buckets[ut][playerId] = 0;

                                buckets[ut][playerId] += dmg;
                            }
                        }

                        var cumulative = new Dictionary<long, double>(); // playerId -> cumulative damage

                        // We do this to fill in any holes where no damage was done.
                        var filledBuckets = new List<long>();
                        for (long ut = start_ut; ut <= end_ut; ut++) 
                        {
                            filledBuckets.Add(ut);
                            if (!buckets.ContainsKey(ut))
                                buckets[ut] = new Dictionary<long, double>();
                        }

                        var finalSeries = new List<object>();

                        foreach (var p in players)
                        {
                            long playerId = p.Key;
                            string playerName = p.Value;

                            cumulative[playerId] = 0; // initialize cumulative damage
                            var dataArray = new List<double>();

                            foreach (var b in filledBuckets)
                            {
                                // add damage for this bucket if present
                                if (buckets[b].ContainsKey(playerId))
                                    cumulative[playerId] += buckets[b][playerId];

                                // always push cumulative value
                                dataArray.Add(cumulative[playerId]);
                            }

                            finalSeries.Add(new
                            {
                                id = playerId,
                                label = playerName,
                                data = dataArray
                            });
                        }

                        return finalSeries;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        //add method to aggaragte damage from all players overtime

        public static List<Models.Recording_Simple> Get_Recordings()
        {
            List<Models.Recording_Simple> query_results = new List<Models.Recording_Simple>();
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand(@"
                    SELECT * FROM recordings
                    ", connection);
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows == false) { return null; }
                        while (reader.Read())
                        {
                            query_results.Add(new Models.Recording_Simple(reader.GetInt16(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3)));
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
            return query_results;
        }

        public static Damage_Simple Get_Biggest_BurstofDamage_InUT_BetweenTimes(int start_ut, int end_ut, int burst_timeframe)
        {
            return Get_ListOf_Distinct_Biggest_BurstofDamage_InUT_BetweenTimes(start_ut, end_ut, burst_timeframe, 1)[0];
        }

        /// <summary>
        /// returns back the largest burst over a time period. Unique per player id so if you pass a 3 count it will give you the 3 top bursts with no repeat playerids
        /// not completely accurate because depending on how the time gets chopped up a large burst could be split between 2 time sections. worst for reall small and really large timeframes
        /// </summary>
        /// <param name="start_ut"></param>
        /// <param name="end_ut"></param>
        /// <param name="burst_timeframe"></param>
        /// <returns>damage_simple.unix_timestamp marks the begining section of the burst</returns>
        public static List<Damage_Simple> Get_ListOf_Distinct_Biggest_BurstofDamage_InUT_BetweenTimes(int start_ut, int end_ut, int burst_timeframe, int count)
        {
            List<Damage_Simple> damages = new List<Damage_Simple>();
            try
            {
                using (var connection = new SqliteConnection(db_connection))
                {
                    connection.Open();

                    using (var command = new SqliteCommand(@"
                    select DISTINCT MAX(sum_dmg), plyr.playername, chunk_start, bigselect.playerid
                    FROM(
	                    select sum(damage) as sum_dmg, (ut/@burst_timeframe)*@burst_timeframe as chunk_start, playerid
	                    from damages
	                    where ut > @start_ut and ut < @end_ut
	                    group by playerid, chunk_start
	
	                    union select sum(damage) as sum_dmg, ((ut/@burst_timeframe)*@burst_timeframe)+(@burst_timeframe/4) as chunk_start, playerid
	                    from damages
	                    where ut > @start_ut and ut < @end_ut
	                    group by playerid, chunk_start
	
	                    union select sum(damage) as sum_dmg, ((ut/@burst_timeframe)*@burst_timeframe)+(@burst_timeframe/4)*2 as chunk_start, playerid
	                    from damages
	                    where ut > @start_ut and ut < @end_ut
	                    group by playerid, chunk_start
	
	                    union select sum(damage) as sum_dmg, ((ut/@burst_timeframe)*@burst_timeframe)+(@burst_timeframe/4)*3 as chunk_start, playerid
	                    from damages
	                    where ut > @start_ut and ut < @end_ut
	                    group by playerid, chunk_start
	                    ) as bigselect
                        left join players as plyr on bigselect.playerid = plyr.playerid
					    Group by bigselect.playerid
					    order by sum_dmg DESC
					    limit @count
                    ", connection))
                    {
                        command.Parameters.AddWithValue("@start_ut", start_ut);
                        command.Parameters.AddWithValue("@end_ut", end_ut);
                        command.Parameters.AddWithValue("@burst_timeframe", burst_timeframe);
                        command.Parameters.AddWithValue("@count", count);
                        Damage_Simple results;
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows == false) { return null; }
                            while (reader.Read())
                            {
                                if(reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3))
                                {
                                    continue;
                                }
                                results = new Damage_Simple(reader.GetDouble(0), reader.GetInt64(3), reader.GetString(1), reader.GetInt32(2));
                                damages.Add(results);
                            }
                            return damages;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static List<Damage_Simple> Get_Chunked_Damage_OverUT(int start_ut, int end_ut, int chunk_size)
        {
            List<Damage_Simple> results = new List<Damage_Simple>();
            try
            {
                using (var connection = new SqliteConnection(db_connection))
                {
                    connection.Open();

                    using (var command = new SqliteCommand(@"
                        select sum(damage) as sum_dmg, (ut/@chunk_size)*@chunk_size as chunk_start, dmgs.playerid, playername
                        from damages as dmgs
                        left join players as plyr on dmgs.playerid = plyr.playerid
                        where ut > @start_ut and ut < @end_ut
                        group by dmgs.playerid, chunk_start
                        order by chunk_start ASC", connection))
                    {
                        command.Parameters.AddWithValue("@start_ut", start_ut);
                        command.Parameters.AddWithValue("@end_ut", end_ut);
                        command.Parameters.AddWithValue("@chunk_size", chunk_size);
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read() == true)
                            {
                                results.Add(new Damage_Simple(reader.GetDouble(0), reader.GetInt64(2), reader.GetString(3), reader.GetInt32(1)));
                            }
                            return results;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static List<string> Get_Players_From_Recording(int start_ut, int end_ut)
        {
            List<string> results = new List<string>();
            try
            {
                using (var connection = new SqliteConnection(db_connection))
                {
                    connection.Open();

                    using (var command = new SqliteCommand(@"
                        select DISTINCT playername
                        from damages
                        left join players on damages.playerid = players.playerid
                        where damages.ut between @start_ut and @end_ut
                        group by damages.playerid", connection))
                    {
                        command.Parameters.AddWithValue("@start_ut", start_ut);
                        command.Parameters.AddWithValue("@end_ut", end_ut);
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read() == true)
                            {
                                results.Add(reader.GetString(0));
                            }
                            return results;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static void Clear_Damage_DB()
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand add_command = new SqliteCommand(@"
                    DELETE FROM damages
                    ", connection);
                    add_command.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                Debug.WriteLine("couldnt clear damage db");
            }
        }

        public static string Get_Local_Adapter()
        {
            string adapter = "";
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand(@"
                    select adapter from local_adapter order by id DESC limit 1
                    ", connection);
                    object results = command.ExecuteScalar();
                    if (results != DBNull.Value && (string)results != "" && results != null)
                    {
                        adapter = (string)results;
                    }
                }
            }
            catch
            {
                Debug.WriteLine("could not send sql command");
            }
            return adapter;
        }
        public static void Set_Local_Adapter(string adapter)
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand(@"
                        delete from local_adapter;
                        insert into local_adapter (adapter)
                            values(@adapter)
                    ", connection);
                    command.Parameters.AddWithValue("@adapter", adapter);
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                Debug.WriteLine("could not send sql command");
            }
        }

        /// <summary>
        /// Get skill statistics: usage count, avg/min/max damage per skill
        /// </summary>
        public static List<SkillStat> Get_Skill_Stats(int start_ut, int end_ut, long? playerid = null)
        {
            List<SkillStat> results = new List<SkillStat>();
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    string sql = @"
                        SELECT 
                            skill,
                            COUNT(*) as hit_count,
                            SUM(damage) as total_damage,
                            AVG(damage) as avg_damage,
                            MIN(damage) as min_damage,
                            MAX(damage) as max_damage
                        FROM damages
                        WHERE ut > @start_ut AND ut < @end_ut AND skill != 0";
                    
                    if (playerid.HasValue)
                    {
                        sql += " AND playerid = @playerid";
                    }
                    
                    sql += " GROUP BY skill ORDER BY total_damage DESC";
                    
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@start_ut", start_ut);
                        command.Parameters.AddWithValue("@end_ut", end_ut);
                        if (playerid.HasValue)
                        {
                            command.Parameters.AddWithValue("@playerid", playerid.Value);
                        }
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read() == true)
                            {
                                results.Add(new SkillStat
                                {
                                    skill_id = reader.GetInt32(0),
                                    hit_count = reader.GetInt32(1),
                                    total_damage = reader.GetDouble(2),
                                    avg_damage = reader.GetDouble(3),
                                    min_damage = reader.GetDouble(4),
                                    max_damage = reader.GetDouble(5)
                                });
                            }
                            return results;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting skill stats: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get combat duration statistics from recordings
        /// </summary>
        public static CombatStats Get_Combat_Stats(int start_ut, int end_ut)
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(@"
                        SELECT 
                            COUNT(*) as combat_count,
                            SUM(end_ut - start_ut) as total_duration
                        FROM recordings
                        WHERE start_ut >= @start_ut AND end_ut <= @end_ut AND end_ut IS NOT NULL
                    ", connection))
                    {
                        command.Parameters.AddWithValue("@start_ut", start_ut);
                        command.Parameters.AddWithValue("@end_ut", end_ut);
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read() == true)
                            {
                                int combatCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                long totalDuration = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
                                
                                return new CombatStats
                                {
                                    combat_count = combatCount,
                                    total_duration_seconds = (int)totalDuration,
                                    avg_combat_duration_seconds = combatCount > 0 ? (int)(totalDuration / combatCount) : 0
                                };
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting combat stats: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get unique players from damages table
        /// </summary>
        public static List<PlayerInfo> Get_Players(int start_ut, int end_ut)
        {
            List<PlayerInfo> results = new List<PlayerInfo>();
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(@"
                        SELECT DISTINCT d.playerid, COALESCE(p.playername, '') as playername
                        FROM damages d
                        LEFT JOIN players p ON d.playerid = p.playerid
                        WHERE d.ut > @start_ut AND d.ut < @end_ut
                        ORDER BY d.playerid
                    ", connection))
                    {
                        command.Parameters.AddWithValue("@start_ut", start_ut);
                        command.Parameters.AddWithValue("@end_ut", end_ut);
                        using (SqliteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read() == true)
                            {
                                results.Add(new PlayerInfo
                                {
                                    player_id = reader.GetInt64(0),
                                    player_name = reader.IsDBNull(1) ? "" : reader.GetString(1)
                                });
                            }
                            return results;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting players: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get player DPS stats for skill stats page
        /// </summary>
        public static List<PlayerDps> Get_PlayerDps(int start_ut, int end_ut)
        {
            List<PlayerDps> results = new List<PlayerDps>();
            try
            {
                using (SqliteConnection connection = new SqliteConnection(db_connection))
                {
                    connection.Open();
                    
                    // First get total damage
                    using (var totalCmd = new SqliteCommand(@"
                        SELECT SUM(damage) as total_damage
                        FROM damages
                        WHERE ut > @start_ut AND ut < @end_ut
                    ", connection))
                    {
                        totalCmd.Parameters.AddWithValue("@start_ut", start_ut);
                        totalCmd.Parameters.AddWithValue("@end_ut", end_ut);
                        var totalObj = totalCmd.ExecuteScalar();
                        double totalDamage = totalObj == DBNull.Value ? 0 : Convert.ToDouble(totalObj);
                        
                        // Now get player stats
                        using (var command = new SqliteCommand(@"
                            SELECT 
                                d.playerid,
                                COALESCE(p.playername, '') as playername,
                                SUM(d.damage) as total_damage
                            FROM damages d
                            LEFT JOIN players p ON d.playerid = p.playerid
                            WHERE d.ut > @start_ut AND d.ut < @end_ut
                            GROUP BY d.playerid
                            ORDER BY total_damage DESC
                        ", connection))
                        {
                            command.Parameters.AddWithValue("@start_ut", start_ut);
                            command.Parameters.AddWithValue("@end_ut", end_ut);
                            using (SqliteDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read() == true)
                                {
                                    double playerDamage = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                                    results.Add(new PlayerDps
                                    {
                                        player_id = reader.GetInt64(0),
                                        player_name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                        total_damage = playerDamage,
                                        dps_percentage = totalDamage > 0 ? (playerDamage / totalDamage) * 100 : 0
                                    });
                                }
                                return results;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting player DPS: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Skill statistics model
    /// </summary>
    public class SkillStat
    {
        public int skill_id { get; set; }
        public int hit_count { get; set; }
        public double total_damage { get; set; }
        public double avg_damage { get; set; }
        public double min_damage { get; set; }
        public double max_damage { get; set; }
    }

    /// <summary>
    /// Combat duration statistics model
    /// </summary>
    public class CombatStats
    {
        public int combat_count { get; set; }
        public int total_duration_seconds { get; set; }
        public int avg_combat_duration_seconds { get; set; }
    }

    /// <summary>
    /// Player information model
    /// </summary>
    public class PlayerInfo
    {
        public long player_id { get; set; }
        public string player_name { get; set; }
    }

    /// <summary>
    /// Player DPS statistics model
    /// </summary>
    public class PlayerDps
    {
        public long player_id { get; set; }
        public string player_name { get; set; }
        public double total_damage { get; set; }
        public double dps_percentage { get; set; }
    }
}