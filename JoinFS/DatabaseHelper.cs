using JoinFS.CAVModels;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinFS
{
    public class DatabaseHelper
    {
        private string connectionString;

        public DatabaseHelper(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // Create a session
        public void CreateSession(JoinfsSession session)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO joinfs_sessions (id, nickname, callsign, last_updated) VALUES (@id, @nickname, @callsign, @last_updated)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", session.Id);
                cmd.Parameters.AddWithValue("@nickname", session.Nickname);
                cmd.Parameters.AddWithValue("@callsign", session.Callsign);
                cmd.Parameters.AddWithValue("@last_updated", session.LastUpdated);
                cmd.ExecuteNonQuery();
            }
        }

        // Read a session
        public JoinfsSession ReadSession(int id)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM joinfs_sessions WHERE id = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new JoinfsSession(
                            reader.GetString("nickname"),
                            reader.GetString("callsign"),
                            reader.GetDateTime("last_updated"),
                            reader.GetInt32("id")
                        );
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        // Update a session
        public void UpdateSession(JoinfsSession session)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE joinfs_sessions SET nickname = @nickname, callsign = @callsign, last_updated = @last_updated WHERE id = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nickname", session.Nickname);
                cmd.Parameters.AddWithValue("@callsign", session.Callsign);
                cmd.Parameters.AddWithValue("@last_updated", session.LastUpdated);
                cmd.Parameters.AddWithValue("@id", session.Id);
                cmd.ExecuteNonQuery();
            }
        }

        // Delete a session
        public void DeleteSession(int id)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM joinfs_sessions WHERE id = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // Similar CRUD methods can be created for JoinfsAircraft
        public void CreateAircraft(JoinfsAircraft aircraft)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO joinfs_aircrafts (id, callsign, owner, distance, heading, altitude, model) VALUES (@id, @callsign, @owner, @distance, @heading, @altitude, @model)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", aircraft.Id);
                cmd.Parameters.AddWithValue("@callsign", aircraft.Callsign);
                cmd.Parameters.AddWithValue("@owner", aircraft.Owner);
                cmd.Parameters.AddWithValue("@distance", aircraft.Distance);
                cmd.Parameters.AddWithValue("@heading", aircraft.Heading);
                cmd.Parameters.AddWithValue("@altitude", aircraft.Altitude);
                cmd.Parameters.AddWithValue("@model", aircraft.Model);
                cmd.ExecuteNonQuery();
            }
        }

        public JoinfsAircraft ReadAircraft(int id)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM joinfs_aircrafts WHERE id = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new JoinfsAircraft(
                            reader.GetInt32("id"),
                            reader.IsDBNull(reader.GetOrdinal("callsign")) ? null : reader.GetString("callsign"),
                            reader.IsDBNull(reader.GetOrdinal("owner")) ? null : reader.GetString("owner"),
                            reader.IsDBNull(reader.GetOrdinal("distance")) ? (float?)null : reader.GetFloat("distance"),
                            reader.IsDBNull(reader.GetOrdinal("heading")) ? (float?)null : reader.GetFloat("heading"),
                            reader.IsDBNull(reader.GetOrdinal("altitude")) ? (float?)null : reader.GetFloat("altitude"),
                            reader.IsDBNull(reader.GetOrdinal("model")) ? null : reader.GetString("model")
                        );
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public void UpdateAircraft(JoinfsAircraft aircraft)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE joinfs_aircrafts SET callsign = @callsign, owner = @owner, distance = @distance, heading = @heading, altitude = @altitude, model = @model WHERE id = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@callsign", aircraft.Callsign);
                cmd.Parameters.AddWithValue("@owner", aircraft.Owner);
                cmd.Parameters.AddWithValue("@distance", aircraft.Distance);
                cmd.Parameters.AddWithValue("@heading", aircraft.Heading);
                cmd.Parameters.AddWithValue("@altitude", aircraft.Altitude);
                cmd.Parameters.AddWithValue("@model", aircraft.Model);
                cmd.Parameters.AddWithValue("@id", aircraft.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteAircraft(int id)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM joinfs_aircrafts WHERE id = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
