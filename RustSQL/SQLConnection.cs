using System;
using System.IO;
using System.Text;
using Oxide.Core;
using Oxide.Core.Database;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("SQLConnection", "Zas", "0.0.1")]
    internal class SQLConnection : CovalencePlugin
    {
        private Core.MySql.Libraries.MySql mySql = new Core.MySql.Libraries.MySql();
        private PluginConfig config;

        private class PluginConfig
        {
            public string Host;
            public string DBName;
            public string Username;
            public string Password;
        }

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        private void OnUserConnected(IPlayer player)
        {
            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);

                var selectQuery = "SELECT userid FROM PlayerData WHERE userId = @0";
                Sql selectCommand = Sql.Builder.Append(selectQuery, player.Id);

                mySql.Query(selectCommand, connection, results =>
                {
                    if (results.Count == 0)
                    {
                        Puts(results.Count.ToString());
                        var queryInsert = $"INSERT INTO PlayerData (userId, userName) VALUES (@0, @1)";
                        Sql insertCommand = Sql.Builder.Append(queryInsert, player.Id, player.Name);

                        mySql.Insert(insertCommand, connection);

                        mySql.CloseDb(connection);
                    }
                });
            }
            catch (Exception ex)
            {
                Puts(ex.ToString());
            }
        }

        private void OnPlayerRevive(BasePlayer reviver, BasePlayer player)
        {
            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);

                var queryReviverInsert = $"UPDATE PlayerData SET revives = revives + 1 WHERE userId = @0";
                var queryDownedInsert = $"UPDATE PlayerData SET revives = revives + 1 WHERE userId = @0";
                Sql insertReviverCommand = Sql.Builder.Append(queryReviverInsert, reviver.userID);
                Sql insertDownedCommand = Sql.Builder.Append(queryDownedInsert, player.userID);

                mySql.Insert(insertReviverCommand, connection);
                mySql.Insert(insertDownedCommand, connection);

                mySql.CloseDb(connection);
            }
            catch (Exception ex)
            {
                Puts(ex.ToString());
            }
        }

        private void CanBeWounded(BasePlayer player, HitInfo info)
        {
            Puts(info.);
        }

        private void CanHackCrate(BasePlayer player)
        {
            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);

                var queryInsert = $"UPDATE PlayerData SET hackedCrates = hackedCrates + 1 WHERE userId = @0";
                Sql inserCommand = Sql.Builder.Append(queryInsert, player.userID);

                mySql.Insert(inserCommand, connection);

                mySql.CloseDb(connection);
            }
            catch (Exception ex)
            {
                Puts(ex.ToString());
            }
        }
    }
}