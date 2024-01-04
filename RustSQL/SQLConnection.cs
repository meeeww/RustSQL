using System;
using System.IO;
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

        private void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        private void OnUserChat(IPlayer player, string message)
        {
            Puts("Player chatted");
            Puts($"{player.Name} said: {message}");
            Puts(Config["Host"].ToString());
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
                Puts("Error");
                Puts(ex.ToString());
            }
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

        object OnPlayerRevive(BasePlayer reviver, BasePlayer player)
        {
            try
            {
                Puts("Empezando configuracion");
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);
                var query = "SELECT userid FROM PlayerData WHERE ";
                Sql selectCommand = Sql.Builder.Append(query);

                mySql.Query(selectCommand, connection, results =>
                {
                    Puts(results.Count.ToString());
                    foreach (var item in results)
                    {
                        Puts(item["userid"].ToString());
                    }
                });
            }
            catch (Exception ex)
            {
                Puts(ex.ToString());
            }
            return null;
        }
    }
}