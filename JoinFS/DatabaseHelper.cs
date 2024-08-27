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
                string query = "INSERT INTO joinfs_sessions (id, node, nickname, callsign, simulator, connected, last_updated) VALUES (@id, @node, @nickname, @callsign, @simulator, @connected, @last_updated)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", session.Id);
                cmd.Parameters.AddWithValue("@node", session.Node);
                cmd.Parameters.AddWithValue("@nickname", session.Nickname);
                cmd.Parameters.AddWithValue("@callsign", session.Callsign);
                cmd.Parameters.AddWithValue("@simulator", session.Simulator);
                cmd.Parameters.AddWithValue("@connected", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@last_updated", DateTime.UtcNow);
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
                            reader.GetString("node"),
                            reader.GetString("nickname"),
                            reader.GetString("callsign"),
                            reader.GetString("simulator"),
                            reader.GetDateTime("connected"),
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

        public JoinfsSession ReadSessionByNode(string node)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM joinfs_sessions WHERE node = @node LIMIT 1";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@node", node);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new JoinfsSession(
                            reader.GetString("node"),
                            reader.GetString("nickname"),
                            reader.GetString("callsign"),
                            reader.GetString("simulator"),
                            reader.GetDateTime("connected"),
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
                string query = "UPDATE joinfs_sessions SET node = @node, nickname = @nickname, callsign = @callsign, simulator = @simulator, connected = @connected, last_updated = @last_updated WHERE id = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@node", session.Node);
                cmd.Parameters.AddWithValue("@nickname", session.Nickname);
                cmd.Parameters.AddWithValue("@callsign", session.Callsign);
                cmd.Parameters.AddWithValue("@simulator", session.Simulator);
                cmd.Parameters.AddWithValue("@connected", session.Connected);
                cmd.Parameters.AddWithValue("@last_updated", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@id", session.Id);
                cmd.ExecuteNonQuery();
            }
        }

        // Update a session
        public void UpdateSessionForNode(JoinfsSession session, string node)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE joinfs_sessions SET node = @node, nickname = @nickname, callsign = @callsign, simulator = @simulator, connected = @connected, last_updated = @last_updated WHERE node = @node";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@node", session.Node);
                cmd.Parameters.AddWithValue("@nickname", session.Nickname);
                cmd.Parameters.AddWithValue("@callsign", session.Callsign);
                cmd.Parameters.AddWithValue("@simulator", session.Simulator);
                cmd.Parameters.AddWithValue("@connected", session.Connected);
                cmd.Parameters.AddWithValue("@last_updated", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@node", node);
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

        public void DeleteSessionByNode(string node)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM joinfs_sessions WHERE node = @node";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@node", node);
                cmd.ExecuteNonQuery();
            }
        }

        // Similar CRUD methods can be created for JoinfsAircraft
        public void CreateAircraft(JoinfsAircraft aircraft)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO joinfs_aircrafts (id, callsign, owner, ownernode, distance, heading, altitude, latitude, longitude, model, last_updated) VALUES (@id, @callsign, @owner, @ownernode, @distance, @heading, @altitude, @latitude, @longitude, @model, @lastupdated)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", aircraft.Id);
                cmd.Parameters.AddWithValue("@callsign", aircraft.Callsign);
                cmd.Parameters.AddWithValue("@owner", aircraft.Owner);
                cmd.Parameters.AddWithValue("@ownernode", aircraft.OwnerNode);
                cmd.Parameters.AddWithValue("@distance", aircraft.Distance);
                cmd.Parameters.AddWithValue("@heading", aircraft.Heading);
                cmd.Parameters.AddWithValue("@altitude", aircraft.Altitude);
                cmd.Parameters.AddWithValue("@latitude", aircraft.Latitude);
                cmd.Parameters.AddWithValue("@longitude", aircraft.Longitude);
                cmd.Parameters.AddWithValue("@model", aircraft.Model);
                cmd.Parameters.AddWithValue("@lastupdated", DateTime.UtcNow);
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
                            reader.IsDBNull(reader.GetOrdinal("callsign")) ? null : reader.GetString("callsign"),
                            reader.IsDBNull(reader.GetOrdinal("owner")) ? null : reader.GetString("owner"),
                            reader.IsDBNull(reader.GetOrdinal("ownernode")) ? null : reader.GetString("ownernode"),
                            reader.IsDBNull(reader.GetOrdinal("distance")) ? (float?)null : reader.GetFloat("distance"),
                            reader.IsDBNull(reader.GetOrdinal("heading")) ? (float?)null : reader.GetFloat("heading"),
                            reader.IsDBNull(reader.GetOrdinal("altitude")) ? (float?)null : reader.GetFloat("altitude"),
                            reader.IsDBNull(reader.GetOrdinal("latitude")) ? (float?)null : reader.GetFloat("latitude"),
                            reader.IsDBNull(reader.GetOrdinal("longitude")) ? (float?)null : reader.GetFloat("longitude"),
                            reader.IsDBNull(reader.GetOrdinal("model")) ? null : reader.GetString("model"),
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

        public JoinfsAircraft ReadAircraftForNode(string ownernode)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM joinfs_aircrafts WHERE ownernode = @ownernode LIMIT 1";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ownernode", ownernode);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new JoinfsAircraft(
                            reader.IsDBNull(reader.GetOrdinal("callsign")) ? null : reader.GetString("callsign"),
                            reader.IsDBNull(reader.GetOrdinal("owner")) ? null : reader.GetString("owner"),
                            reader.IsDBNull(reader.GetOrdinal("ownernode")) ? null : reader.GetString("ownernode"),
                            reader.IsDBNull(reader.GetOrdinal("distance")) ? (float?)null : reader.GetFloat("distance"),
                            reader.IsDBNull(reader.GetOrdinal("heading")) ? (float?)null : reader.GetFloat("heading"),
                            reader.IsDBNull(reader.GetOrdinal("altitude")) ? (float?)null : reader.GetFloat("altitude"),
                            reader.IsDBNull(reader.GetOrdinal("latitude")) ? (float?)null : reader.GetFloat("latitude"),
                            reader.IsDBNull(reader.GetOrdinal("longitude")) ? (float?)null : reader.GetFloat("longitude"),
                            reader.IsDBNull(reader.GetOrdinal("model")) ? null : reader.GetString("model"),
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

        public void UpdateAircraft(JoinfsAircraft aircraft)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE joinfs_aircrafts SET callsign = @callsign, owner = @owner, ownernode = @ownernode, distance = @distance, heading = @heading, altitude = @altitude, latitude = @latitude, longitude = @longitude, model = @model, last_updated = @last_updated WHERE id = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@callsign", aircraft.Callsign);
                cmd.Parameters.AddWithValue("@owner", aircraft.Owner);
                cmd.Parameters.AddWithValue("@ownernode", aircraft.OwnerNode);
                cmd.Parameters.AddWithValue("@distance", aircraft.Distance);
                cmd.Parameters.AddWithValue("@heading", aircraft.Heading);
                cmd.Parameters.AddWithValue("@altitude", aircraft.Altitude);
                cmd.Parameters.AddWithValue("@latitude", aircraft.Latitude);
                cmd.Parameters.AddWithValue("@longitude", aircraft.Longitude);
                cmd.Parameters.AddWithValue("@model", aircraft.Model);
                cmd.Parameters.AddWithValue("@last_updated", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@id", aircraft.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateAircraftForOwnernode(JoinfsAircraft aircraft)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE joinfs_aircrafts SET callsign = @callsign, owner = @owner, ownernode = @ownernode, distance = @distance, heading = @heading, altitude = @altitude, latitude = @latitude, longitude = @longitude, model = @model, last_updated = @last_updated WHERE ownernode = @ownernode";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@callsign", aircraft.Callsign);
                cmd.Parameters.AddWithValue("@owner", aircraft.Owner);
                cmd.Parameters.AddWithValue("@ownernode", aircraft.OwnerNode);
                cmd.Parameters.AddWithValue("@distance", aircraft.Distance);
                cmd.Parameters.AddWithValue("@heading", aircraft.Heading);
                cmd.Parameters.AddWithValue("@altitude", aircraft.Altitude);
                cmd.Parameters.AddWithValue("@latitude", aircraft.Latitude);
                cmd.Parameters.AddWithValue("@longitude", aircraft.Longitude);
                cmd.Parameters.AddWithValue("@model", aircraft.Model);
                cmd.Parameters.AddWithValue("@last_updated", DateTime.UtcNow);
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

        public void DeleteAircraftForNode(string ownernode)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM joinfs_aircrafts WHERE ownernode = @ownernode";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ownernode", ownernode);
                cmd.ExecuteNonQuery();
            }
        }

        public void CleanupDatabase()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM joinfs_aircrafts ";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();
                string query2 = "DELETE FROM joinfs_sessions ";
                MySqlCommand cmd2 = new MySqlCommand(query2, conn);
                cmd2.ExecuteNonQuery();
            }
        }
    }
}
