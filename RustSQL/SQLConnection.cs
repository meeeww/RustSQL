using System;
using System.IO;
using System.Text;
using Oxide.Core;
using Oxide.Core.Database;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("SQLConnection", "Zas", "0.0.2")]
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

        private bool IsValid(BasePlayer player)
        {
            return player != null && player.userID.IsSteamId();
        }

        // PVPSTATS
        private void OnPlayerRevive(BasePlayer reviver, BasePlayer player)
        {
            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);

                var queryReviverUpdate = $"UPDATE PlayerData SET revives = revives + 1 WHERE userId = @0";
                var queryDownedUpdate = $"UPDATE PlayerData SET revives = revives + 1 WHERE userId = @0";
                Sql updateReviverCommand = Sql.Builder.Append(queryReviverUpdate, reviver.userID);
                Sql updateDownedCommand = Sql.Builder.Append(queryDownedUpdate, player.userID);

                mySql.Insert(updateReviverCommand, connection);
                mySql.Insert(updateDownedCommand, connection);

                mySql.CloseDb(connection);
            }
            catch (Exception ex)
            {
                Puts(ex.ToString());
            }
        }

        private void CanBeWounded(BasePlayer player, HitInfo info)
        {
            //Puts(info);
        }

        private void CanHackCrate(BasePlayer player)
        {
            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);

                var queryUpdate = $"UPDATE PlayerData SET hackedCrates = hackedCrates + 1 WHERE userId = @0";
                Sql updateCommand = Sql.Builder.Append(queryUpdate, player.userID);

                mySql.Insert(updateCommand, connection);

                mySql.CloseDb(connection);
            }
            catch (Exception ex)
            {
                Puts(ex.ToString());
            }
        }

        object OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            Puts("OnPlayerAttack works!");
            return null;
        }

        //FARM STATS
        private object OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (!IsValid(player))
                return null;

            for (int i = 0; i < collectible.itemList.Length; i++)
            {
                ItemAmount itemAmount = collectible.itemList[i];
                try
                {
                    var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);
                    var selectQuery = "SELECT itemId FROM ItemsRecogidos WHERE itemId = @0 AND userId = @1";
                    Sql selectCommand = Sql.Builder.Append(selectQuery, itemAmount.itemid, player.userID);

                    mySql.Query(selectCommand, connection, results =>
                    {
                        if (results.Count == 0)
                        {
                            var queryInsert = $"INSERT INTO ItemsRecogidos (userId, shortName, itemId, quantity) VALUES (@0, @1, @2, @3)";
                            Sql insertCommand = Sql.Builder.Append(queryInsert, player.userID, itemAmount.itemDef.shortname, itemAmount.itemid, itemAmount.amount);

                            mySql.Insert(insertCommand, connection);

                            mySql.CloseDb(connection);
                        }
                        else
                        {
                            var queryUpdate = $"UPDATE ItemsRecogidos SET quantity = quantity + @0 WHERE itemId = @1 AND userId = @2";
                            Sql updateCommand = Sql.Builder.Append(queryUpdate, itemAmount.amount, itemAmount.itemid, player.userID);

                            mySql.Insert(updateCommand, connection);

                            mySql.CloseDb(connection);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Puts(ex.ToString());
                }
            }
            return null;
        }

        private object OnGrowableGathered(Item item, BasePlayer player)
        {
            if (!IsValid(player))
                return null;

            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);
                var selectQuery = "SELECT itemId FROM ItemsRecogidos WHERE itemId = @0 AND userId = @1";
                Sql selectCommand = Sql.Builder.Append(selectQuery, item.info.itemid, player.userID);

                mySql.Query(selectCommand, connection, results =>
                {
                    if (results.Count == 0)
                    {
                        var queryInsert = $"INSERT INTO ItemsRecogidos (userId, shortName, itemId, quantity) VALUES (@0, @1, @2, @3)";
                        Sql insertCommand = Sql.Builder.Append(queryInsert, player.userID, item.info.shortname, item.info.itemid, item.amount);

                        mySql.Insert(insertCommand, connection);

                        mySql.CloseDb(connection);
                    }
                    else
                    {
                        var queryUpdate = $"UPDATE ItemsRecogidos SET quantity = quantity + @0 WHERE itemId = @1 AND userId = @2";
                        Sql updateCommand = Sql.Builder.Append(queryUpdate, item.amount, item.info.itemid, player.userID);

                        mySql.Insert(updateCommand, connection);

                        mySql.CloseDb(connection);
                    }
                });
            }
            catch (Exception ex)
            {
                Puts(ex.ToString());
            }
            return null;
        }

        private object OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (!IsValid(player))
                return null;

            var amount = item.amount;
            var info = item.info;


            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);
                var selectQuery = "SELECT itemId FROM ItemsRecogidos WHERE itemId = @0 AND userId = @1";
                Sql selectCommand = Sql.Builder.Append(selectQuery, info.itemid, player.userID);

                mySql.Query(selectCommand, connection, results =>
                {
                    if (results.Count == 0)
                    {
                        var queryInsert = $"INSERT INTO ItemsRecogidos (userId, shortName, itemId, quantity) VALUES (@0, @1, @2, @3)";
                        Sql insertCommand = Sql.Builder.Append(queryInsert, player.userID, info.shortname, info.itemid, amount);

                        mySql.Insert(insertCommand, connection);

                        mySql.CloseDb(connection);
                    }
                    else
                    {
                        Puts(amount.ToString());
                        Puts(item.info.itemid.ToString());
                        Puts(player.userID.ToString());
                        var queryUpdate = $"UPDATE ItemsRecogidos SET quantity = quantity + @0 WHERE itemId = @1 AND userId = @2";
                        Sql updateCommand = Sql.Builder.Append(queryUpdate, amount, info.itemid, player.userID);

                        mySql.Insert(updateCommand, connection);

                        mySql.CloseDb(connection);
                    }
                });
            }
            catch (Exception ex)
            {
                Puts(ex.ToString());
            }
            return null;
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            OnDispenserGather(dispenser, player, item);
        }

        private object OnQuarryGather(MiningQuarry quarry, BasePlayer player, Item item)
        {
            if (!IsValid(player))
                return null;

            var amount = item.amount;
            var info = item.info;


            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);
                var selectQuery = "SELECT itemId FROM ItemsRecogidos WHERE itemId = @0 AND userId = @1";
                Sql selectCommand = Sql.Builder.Append(selectQuery, info.itemid, player.userID);

                mySql.Query(selectCommand, connection, results =>
                {
                    if (results.Count == 0)
                    {
                        var queryInsert = $"INSERT INTO ItemsRecogidos (userId, shortName, itemId, quantity) VALUES (@0, @1, @2, @3)";
                        Sql insertCommand = Sql.Builder.Append(queryInsert, player.userID, info.shortname, info.itemid, amount);

                        mySql.Insert(insertCommand, connection);

                        mySql.CloseDb(connection);
                    }
                    else
                    {
                        Puts(amount.ToString());
                        Puts(item.info.itemid.ToString());
                        Puts(player.userID.ToString());
                        var queryUpdate = $"UPDATE ItemsRecogidos SET quantity = quantity + @0 WHERE itemId = @1 AND userId = @2";
                        Sql updateCommand = Sql.Builder.Append(queryUpdate, amount, info.itemid, player.userID);

                        mySql.Insert(updateCommand, connection);

                        mySql.CloseDb(connection);
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