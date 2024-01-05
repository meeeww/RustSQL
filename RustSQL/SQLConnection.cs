using System;
using System.IO;
using System.Text;
using Oxide.Core;
using Oxide.Core.Database;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("SQLConnection", "Zas", "1.0.0")]
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
        private void OnPlayerDeath(BasePlayer victim, HitInfo info)
        {
            BasePlayer killer = info?.Initiator as BasePlayer;

            if (killer == null) return;
            if (victim.IsNpc || killer.IsNpc) return;

            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);

                if(killer == victim)
                {
                    var queryKillerUpdate = $"UPDATE PlayerData SET suicides = suicides + 1 WHERE userId = @0";
                    Sql updateKillerCommand = Sql.Builder.Append(queryKillerUpdate, killer.userID);

                    mySql.Insert(updateKillerCommand, connection);

                    mySql.CloseDb(connection);
                }
                else
                {
                    var queryKillerUpdate = $"UPDATE PlayerData SET kills = kills + 1 WHERE userId = @0";
                    if (info.isHeadshot)
                    {
                        queryKillerUpdate = $"UPDATE PlayerData SET kills = kills + 1, headshots = headshots + 1 WHERE userId = @0";
                    }
                    var queryVictimUpdate = $"UPDATE PlayerData SET deaths = deaths + 1 WHERE userId = @0";
                    Sql updateKillerCommand = Sql.Builder.Append(queryKillerUpdate, killer.userID);
                    Sql updateVictimCommand = Sql.Builder.Append(queryVictimUpdate, victim.userID);

                    mySql.Insert(updateKillerCommand, connection);
                    mySql.Insert(updateVictimCommand, connection);

                    mySql.CloseDb(connection);
                }
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

                var queryReviverUpdate = $"UPDATE PlayerData SET revives = revives + 1 WHERE userId = @0";
                var queryDownedUpdate = $"UPDATE PlayerData SET revived = revived + 1 WHERE userId = @0";
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

        // OBJECTIVES STATS
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

        private void OnEntityDeath(BaseCombatEntity target, HitInfo info)
        {
            if (info == null || target == null) return;
            if (info.InitiatorPlayer == null || info.InitiatorPlayer == target) return;

            if (target is ScientistNPC)
            {
                var player = info.InitiatorPlayer;
                var ID = player.UserIDString;

                var npcType = target.ShortPrefabName;

                try
                {
                    var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);
                    var selectQuery = "SELECT shortName FROM CientificosMatados WHERE shortName = @0 AND userId = @1";
                    Sql selectCommand = Sql.Builder.Append(selectQuery, npcType, player.userID);

                    mySql.Query(selectCommand, connection, results =>
                    {
                        if (results.Count == 0)
                        {
                            var queryInsert = $"INSERT INTO CientificosMatados (userId, shortName, quantity) VALUES (@0, @1, @2)";
                            Sql insertCommand = Sql.Builder.Append(queryInsert, player.userID, npcType, 1);

                            mySql.Insert(insertCommand, connection);

                            mySql.CloseDb(connection);
                        }
                        else
                        {
                            var queryUpdate = $"UPDATE CientificosMatados SET quantity = quantity + 1 WHERE shortName = @0 AND userId = @1";
                            Sql updateCommand = Sql.Builder.Append(queryUpdate, npcType, player.userID);

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
        }

        // RAID STATS
        void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);
                var selectQuery = "SELECT shortName FROM RaidData WHERE shortName = @0 AND userId = @1";
                Sql selectCommand = Sql.Builder.Append(selectQuery, item.ShortPrefabName, player.userID);

                mySql.Query(selectCommand, connection, results =>
                {
                    if (results.Count == 0)
                    {
                        var queryInsert = $"INSERT INTO RaidData (userId, shortName, quantity) VALUES (@0, @1, 1)";
                        Sql insertCommand = Sql.Builder.Append(queryInsert, player.userID, item.ShortPrefabName);

                        mySql.Insert(insertCommand, connection);

                        mySql.CloseDb(connection);
                    }
                    else
                    {
                        var queryUpdate = $"UPDATE RaidData SET quantity = quantity + 1 WHERE shortName = @0 AND userId = @1";
                        Sql updateCommand = Sql.Builder.Append(queryUpdate, item.ShortPrefabName, player.userID);

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

        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            try
            {
                var connection = mySql.OpenDb(Config["Host"].ToString(), 3306, Config["DBName"].ToString(), Config["Username"].ToString(), Config["Password"].ToString(), this);
                var selectQuery = "SELECT shortName FROM RaidData WHERE shortName = @0 AND userId = @1";
                Sql selectCommand = Sql.Builder.Append(selectQuery, entity.ShortPrefabName, player.userID);

                mySql.Query(selectCommand, connection, results =>
                {
                    if (results.Count == 0)
                    {
                        var queryInsert = $"INSERT INTO RaidData (userId, shortName, quantity) VALUES (@0, @1, 1)";
                        Sql insertCommand = Sql.Builder.Append(queryInsert, player.userID, entity.ShortPrefabName);

                        mySql.Insert(insertCommand, connection);

                        mySql.CloseDb(connection);
                    }
                    else
                    {
                        var queryUpdate = $"UPDATE RaidData SET quantity = quantity + 1 WHERE shortName = @0 AND userId = @1";
                        Sql updateCommand = Sql.Builder.Append(queryUpdate, entity.ShortPrefabName, player.userID);

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

        // OTHER STATS

    }
}