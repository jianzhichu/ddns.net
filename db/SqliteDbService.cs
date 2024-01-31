using ddns.net.dto;
using ddns.net.extension;
using ddns.net.model;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ddns.net
{
    public class SqliteDbService
    {

        private static readonly string ConnectionString =$"DataSource={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db", "ddns.sqlite")}" ;
        //private static readonly string ConnectionString = $"DataSource={Path.Combine(Directory.GetCurrentDirectory(), "db", "ddns.sqlite")}";

        /// <summary>
        /// 查询配置
        /// </summary>
        /// <returns></returns>
        public async Task<DomainConfigInfo> GetDomainConfig()
        {
            try
            {
                using SqliteConnection connection = new(ConnectionString);
                connection.Open();
                string selectSql = "SELECT * from domain_config_info limit 1;";
                using var command = new SqliteCommand(selectSql, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    DomainConfigInfo config = new()
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        AK = reader.GetString(reader.GetOrdinal("AK")),
                        SK = reader.GetString(reader.GetOrdinal("SK")),
                        CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                        Cron = reader.GetInt32(reader.GetOrdinal("Cron")),
                        Domain = reader.GetString(reader.GetOrdinal("Domain")),
                        RecordType = reader.GetString(reader.GetOrdinal("RecordType")),
                        SubDomain = reader.GetString(reader.GetOrdinal("SubDomain")),
                        DomainServer = reader.GetString(reader.GetOrdinal("DomainServer")),
                    };
                    int updateTimeOrdinal = reader.GetOrdinal("UpdateTime");
                    if (!reader.IsDBNull(updateTimeOrdinal))
                        config.UpdateTime = reader.GetDateTime(updateTimeOrdinal);
                    return config;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"sql get domain_config_info error,{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 查询用户
        /// </summary>
        /// <returns></returns>
        public async Task<LoginUserInfo> GetUser(string Id = "")
        {
            try
            {
                using SqliteConnection connection = new(ConnectionString);
                connection.Open();
                string selectSql = $"SELECT * from login_user_info {(!string.IsNullOrEmpty(Id) ? $"where Id='{Id}' OR UserName= '{Id}'" : " ")} limit 1;";
                using var command = new SqliteCommand(selectSql, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    LoginUserInfo user = new()
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                        Password = reader.GetString(reader.GetOrdinal("Password")),
                        UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    };

                    int AvatarOrdinal = reader.GetOrdinal("Avatar");
                    if (!reader.IsDBNull(AvatarOrdinal))
                        user.Avatar = reader.GetString(AvatarOrdinal);
                    int updateTimeOrdinal = reader.GetOrdinal("UpdateTime");
                    if (!reader.IsDBNull(updateTimeOrdinal))
                        user.UpdateTime = reader.GetDateTime(updateTimeOrdinal);
                    return user;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"sql get login_user_info error,{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 修改头像
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="avatar"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAvatar(string Id, string avatar)
        {
            var user = await GetUser(Id);
            if (user == null)
            {
                return false;
            }
            string SQL = $"update login_user_info set Avatar='{avatar}' where Id='{Id}'";
            using SqliteConnection connection = new(ConnectionString);
            connection.Open();
            using var command = new SqliteCommand(SQL, connection);
            var res = await command.ExecuteNonQueryAsync();

            return res > 0;

        }

        /// <summary>
        /// 获取stmp配置
        /// </summary>
        /// <returns></returns>
        public async Task<StmpConfigInfo> GetStmpConfig()
        {
            try
            {
                using SqliteConnection connection = new(ConnectionString);
                connection.Open();
                string selectSql = $"SELECT * from stmp_config_info  limit 1;";
                using var command = new SqliteCommand(selectSql, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    StmpConfigInfo stmp = new()
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Code = reader.GetString(reader.GetOrdinal("Code")),
                        From = reader.GetString(reader.GetOrdinal("From")),
                        To = reader.GetString(reader.GetOrdinal("To")),
                        Open = reader.GetBoolean(reader.GetOrdinal("Open")),
                        Port = reader.GetInt32(reader.GetOrdinal("Port")),
                        Stmp = reader.GetString(reader.GetOrdinal("Stmp"))
                    };

                    return stmp;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"sql get stmp_config_info error,{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 新增或者编辑stmpconfig
        /// </summary>
        /// <param name="stmp"></param>
        /// <returns></returns>
        public async Task<bool> SaveStmpConfig(StmpConfigInfo stmp)
        {
            if (stmp != null)
            {
                stmp.To = stmp.From;
            }

            string SQL;
            if (stmp.Id > 0)
            {

                SQL = $"UPDATE stmp_config_info SET Stmp='{stmp.Stmp}', Port='{stmp.Port}', Code='{stmp.Code}', 'From'='{stmp.From}', 'To'='{stmp.To}', Open='{(stmp.Open ? 1 : 0)}' WHERE (Id='{stmp.Id}');";
            }
            else
            {
                SQL = $"INSERT INTO stmp_config_info (Stmp, Port, Code, 'From', 'To', Open) VALUES ( '{stmp.Stmp}', '{stmp.Port}', '{stmp.Code}', '{stmp.From}', '{stmp.To}', {(stmp.Open ? 1 : 0)});";
            }


            using SqliteConnection connection = new(ConnectionString);
            connection.Open();
            using var command = new SqliteCommand(SQL, connection);
            var res = await command.ExecuteNonQueryAsync();
            return res > 0;
        }

        /// <summary>
        /// 重置所有表
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ResetAllConfigTable()
        {
            string[] tables = new string[] { "domain_config_info", "domain_record_id_info", "login_user_info", "stmp_config_info", "domain_record_info" };

            using SqliteConnection connection = new(ConnectionString);
            connection.Open();
            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                foreach (var table in tables)
                {

                    using var command = new SqliteCommand($"DELETE FROM {table} ", connection, transaction);
                    var res = await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"重置配置表失败,{ex.Message}");
                transaction.Rollback();
                return false;
            }
        }

        /// <summary>
        /// 获取记录recordids
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public async Task<List<DomainRecordIdInfo>> GetDomainRecordIds(string server)
        {
            List<DomainRecordIdInfo> recordIdInfos = new List<DomainRecordIdInfo>();
            try
            {
                using SqliteConnection connection = new(ConnectionString);
                connection.Open();
                string selectSql = $"SELECT * from domain_record_id_info where Server='{server}' limit 1;";
                using var command = new SqliteCommand(selectSql, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    DomainRecordIdInfo config = new()
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Domain = reader.GetString(reader.GetOrdinal("Domain")),
                        SubDomain = reader.GetString(reader.GetOrdinal("SubDomain")),
                        Server = reader.GetString(reader.GetOrdinal("Server")),

                    };

                    int DomainIdOrdinal = reader.GetOrdinal("DomainId");
                    if (!reader.IsDBNull(DomainIdOrdinal))
                        config.DomainId = reader.GetString(DomainIdOrdinal);

                    int RecodeIdOrdinal = reader.GetOrdinal("RecodeId");
                    if (!reader.IsDBNull(RecodeIdOrdinal))
                        config.RecodeId = reader.GetString(RecodeIdOrdinal);

                    int ZoneIdOrdinal = reader.GetOrdinal("ZoneId");
                    if (!reader.IsDBNull(ZoneIdOrdinal))
                        config.ZoneId = reader.GetString(ZoneIdOrdinal);
                    int RecordTypeOrdinal = reader.GetOrdinal("RecordType");
                    if (!reader.IsDBNull(RecordTypeOrdinal))
                        config.RecordType = reader.GetString(RecordTypeOrdinal);

                    recordIdInfos.Add(config);
                }
                return recordIdInfos;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"sql get domain_record_id_info error,{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="server"></param>
        /// <param name="domain"></param>
        /// <param name="subDomain"></param>
        /// <param name="recordType"></param>
        /// <returns></returns>
        public async Task<DomainRecordIdInfo> GetDomainRecordId(string server, string domain, string subDomain, string recordType = "A")
        {

            try
            {
                using SqliteConnection connection = new(ConnectionString);
                connection.Open();
                string selectSql = $"SELECT * from domain_record_id_info where Server='{server}'" +
                    $"and domain='{domain}' and subdomain='{subDomain}' and RecordType='{recordType}' limit 1;";
                using var command = new SqliteCommand(selectSql, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    DomainRecordIdInfo config = new()
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Domain = reader.GetString(reader.GetOrdinal("Domain")),
                        SubDomain = reader.GetString(reader.GetOrdinal("SubDomain")),
                        DomainId = reader.GetString(reader.GetOrdinal("DomainId")),
                        RecodeId = reader.GetString(reader.GetOrdinal("RecodeId")),
                        Server = reader.GetString(reader.GetOrdinal("Server")),
                        ZoneId = reader.GetString(reader.GetOrdinal("ZoneId")),
                        RecordType = reader.GetString(reader.GetOrdinal("RecordType"))
                    };

                    return config;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"sql get domain_record_id_info error,{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 配置记录ID
        /// </summary>
        /// <param name="idInfo"></param>
        /// <param name="colums"></param>
        /// <returns></returns>
        public async Task<bool> UpdateDomainRecordIdWithColums(DomainRecordIdInfo idInfo, string[] colums)
        {

            var config = await GetDomainRecordId(idInfo.Server, idInfo.Domain, idInfo.SubDomain, idInfo.RecordType);
            string SQL = string.Empty;
            if (config == null)
            {


                SQL = $"INSERT INTO domain_record_id_info (Domain, SubDomain, RecodeId, ZoneId, DomainId, Server) VALUES ( '{idInfo.Domain}', '{idInfo.SubDomain}', '{idInfo.RecodeId}', '{idInfo.ZoneId}', '{idInfo.DomainId}', '{idInfo.Server}');";
            }
            else
            {

                var updateCol = string.Empty;
                if (colums != null && colums.Any())
                {
                    var json = JsonConvert.SerializeObject(idInfo);

                    var jsonObject = JObject.Parse(json);

                    foreach (var colName in colums)
                    {
                        string colValue = (string)jsonObject[colName];

                        updateCol += $" {colName}='{colValue}' ,";
                    }
                    updateCol = updateCol.TrimEnd(',');
                    SQL = $"UPDATE domain_record_id_info SET  {updateCol}  WHERE Id = '{config.Id}'";
                }
            }
            if (string.IsNullOrEmpty(SQL))
                return false;
            using SqliteConnection connection = new(ConnectionString);
            connection.Open();
            using var command = new SqliteCommand(SQL, connection);
            var updateCount = await command.ExecuteNonQueryAsync();

            return updateCount > 0;
        }

        /// <summary>
        /// 新增或修改配置
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public async Task<(int code, string erro)> SaveDomainConfig(DomainConfigInfo c)
        {

            string SQL;
            if (c.Id > 0)
            {
                c.UpdateTime = DateTime.Now;
                SQL = $"UPDATE domain_config_info SET Domain='{c.Domain}', SubDomain='{c.SubDomain}', RecordType='{c.RecordType}', UpdateTime='{c.UpdateTime}', DomainServer='{c.DomainServer}', AK='{c.AK}', SK='{c.SK}', Cron='{c.Cron}' WHERE (Id='{c.Id}');";
                using SqliteConnection connection = new(ConnectionString);
                connection.Open();
                using var command = new SqliteCommand(SQL, connection);
                var res = await command.ExecuteNonQueryAsync();
                return (res > 0 ? 0 : -1, res > 0 ? "修改成功" : "修改失败");
            }
            else
            {
                //c.Id = IdGener.GetLong().ToString();
                c.CreateTime = DateTime.Now;


                SQL = $"INSERT INTO domain_config_info (Domain, SubDomain, RecordType, CreateTime, DomainServer, AK, SK, Cron)  " +
                    $"VALUES ( '{c.Domain}', '{c.SubDomain}', '{c.RecordType}', '{c.CreateTime}', '{c.DomainServer}', '{c.AK}', '{c.SK}', '{c.Cron}');";

                using SqliteConnection connection = new(ConnectionString);
                connection.Open();
                using var command = new SqliteCommand(SQL, connection);
                var res = await command.ExecuteNonQueryAsync();
                return (res > 0 ? 0 : -1, res > 0 ? "新增成功" : "新增失败");
            }
        }

        /// <summary>
        /// 查询配置
        /// </summary>
        /// <returns></returns>
        public async Task<DomainRecordInfo> GetLastDomainRecord()
        {
            try
            {
                using SqliteConnection connection = new(ConnectionString);
                connection.Open();
                string selectSql = "SELECT * from domain_record_info order by CreateTime DESC limit 1;";
                using var command = new SqliteCommand(selectSql, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    DomainRecordInfo config = new()
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Address = reader.GetString(reader.GetOrdinal("Address")),
                        CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                        Ip = reader.GetString(reader.GetOrdinal("Ip")),
                        IsDelete = reader.GetBoolean(reader.GetOrdinal("IsDelete")),
                        ISP = reader.GetString(reader.GetOrdinal("ISP")),
                        LastIp = reader.GetString(reader.GetOrdinal("LastIp")),
                        MainDomain = reader.GetString(reader.GetOrdinal("MainDomain")),
                        Servr = reader.GetString(reader.GetOrdinal("Servr")),
                    };

                    return config;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"sql get domain_record_info error,{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 删除解析记录 软删除
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DeleteDomainRecords()
        {
            string SQL = "update domain_record_info set IsDelete=1 where 1=1";
            using SqliteConnection connection = new(ConnectionString);
            connection.Open();
            using var command = new SqliteCommand(SQL, connection);
            var res = await command.ExecuteNonQueryAsync();

            return res > 0;
        }


        #region 新增解析记录
        /// <summary>
        /// 新增解析记录
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public async Task<bool> InsertDomainRecord(DomainRecordInfo d)
        {
            d.CreateTime = DateTime.Now;
            if (string.IsNullOrEmpty(d.LastIp))
                d.LastIp = "-";
            string SQL = $"INSERT INTO domain_record_info ( CreateTime, Address, ISP, Ip, LastIp, Servr, MainDomain, IsDelete) VALUES ( '{d.CreateTime}', '{d.Address}', '{d.ISP}', '{d.Ip}', '{d.LastIp}', '{d.Servr}', '{d.MainDomain}', '{(d.IsDelete ? 1 : 0)}');";
            using SqliteConnection connection = new(ConnectionString);
            connection.Open();
            using var command = new SqliteCommand(SQL, connection);
            var res = await command.ExecuteNonQueryAsync();
            return res > 0;
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PageDomainRecordInfo> DomainRecordPgaeList(RecordPageRequest request)
        {
            DateTime? start = null;
            DateTime? end = null;

            if (request.Dates != null && request.Dates.Any())
            {
                if (request.Dates.Count == 1)
                {
                    if (DateTime.TryParse(request.Dates[0], out DateTime start1))
                    {
                        start = start1;
                    }
                }
                if (request.Dates.Count == 2)
                {
                    if (DateTime.TryParse(request.Dates[0], out DateTime start1))
                    {
                        start = start1;
                    }

                    if (DateTime.TryParse(request.Dates[1], out DateTime end1))
                    {
                        end = end1;
                    }
                }
            }

            long totalCount = 0;
            string where = "where 1=1";

            if (start.HasValue)
            {
                where += " and  CreateTime >= @startTime";
            }
            if (end.HasValue)
            {

                where += " and  AND CreateTime <= @endTime";
            }

            string totalSQL = @$"SELECT count (id) from domain_record_info {where}";
            string query = @$"SELECT * FROM domain_record_info {where} ORDER BY CreateTime DESC  LIMIT @pageSize OFFSET @offset";


            List<DomainRecordInfo> list = new List<DomainRecordInfo>();

            using SqliteConnection connection = new SqliteConnection(ConnectionString);
            connection.Open();
            using SqliteTransaction transaction = connection.BeginTransaction();
            {
                try
                {

                    {
                        using SqliteCommand command = new SqliteCommand(totalSQL, connection, transaction);
                        totalCount = (long)await command.ExecuteScalarAsync();
                    }

                    {
                        using SqliteCommand command = new SqliteCommand(query, connection, transaction);
                        command.Parameters.AddWithValue("@startTime", request.pageSize);
                        command.Parameters.AddWithValue("@endTime", request.pageSize);
                        command.Parameters.AddWithValue("@pageSize", request.pageSize);
                        command.Parameters.AddWithValue("@offset", (request.pageIndex - 1) * request.pageSize);

                        using SqliteDataReader reader = await command.ExecuteReaderAsync();
                        while (reader.Read())
                        {
                            // 处理查询结果

                            DomainRecordInfo config = new()
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                                Ip = reader.GetString(reader.GetOrdinal("Ip")),
                                IsDelete = reader.GetBoolean(reader.GetOrdinal("IsDelete")),
                                ISP = reader.GetString(reader.GetOrdinal("ISP")),
                                MainDomain = reader.GetString(reader.GetOrdinal("MainDomain")),
                                Servr = reader.GetString(reader.GetOrdinal("Servr")),
                            };
                            var LastIpOrdinal = reader.GetOrdinal("LastIp");
                            if (!reader.IsDBNull(LastIpOrdinal))
                                config.LastIp = reader.GetString(LastIpOrdinal);

                            list.Add(config);
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {

                    Serilog.Log.Error($"sqlite query records error,{ex.Message}");
                    transaction.Rollback();
                }
            }


            PageDomainRecordInfo recordInfo = new PageDomainRecordInfo()
            {
                data = list,
                Total = totalCount,
                pageIndex = request.pageIndex,
                pageSize = request.pageSize
            };

            return recordInfo;
        }

        /// <summary>
        /// 注册用户
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public async Task<(int code, string erro)> InitUser(LoginUserInfo u)
        {
            string totalSQL = @$"SELECT count (id) from login_user_info";
            using SqliteConnection connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using SqliteCommand command = new SqliteCommand(totalSQL, connection);
            var totalCount = (long)await command.ExecuteScalarAsync();
            if (totalCount > 0)
            {
                return (-1, "该接口只允许第一次初始化的时候调用");
            }
            else
            {
                u.CreateTime = DateTime.Now;
                u.Password = Util.Md5(u.Password);
                string insertSQL = $"INSERT INTO login_user_info (UserName, Password, CreateTime) VALUES " +
                    $"('{u.UserName}', '{u.Password}', '{u.CreateTime}');";

                using var command2 = new SqliteCommand(insertSQL, connection);
                var res = await command2.ExecuteNonQueryAsync();

                return (res > 0 ? 0 : -1, res > 0 ? "初始用户成功" : "初始化用户失败");
            }

        }


        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sysinfo"></param>
        /// <returns></returns>
        public async Task<bool> Init(InitSystemInfo sysinfo)
        {
            try
            {
                await ResetAllConfigTable();

                await InitUser(new LoginUserInfo
                {
                    UserName = sysinfo.UserName,
                    Password = sysinfo.UswePwd
                });

                await SaveDomainConfig(new DomainConfigInfo
                {
                    AK = sysinfo.AK,
                    CreateTime = DateTime.Now,
                    Domain = sysinfo.domainName,
                    DomainServer = sysinfo.cloudName,
                    SK = sysinfo.SK,
                    RecordType = sysinfo.recordType,
                    SubDomain = sysinfo.domainRecord,
                    Cron = sysinfo.Cron
                });


                var subdomains = sysinfo.domainRecord.Split(";")?.Distinct().Where(x => !string.IsNullOrEmpty(x));
                string SQL = string.Empty;
                foreach (var item in subdomains)
                {
                    DomainRecordIdInfo info = new DomainRecordIdInfo();

                    SQL += $"INSERT INTO domain_record_id_info (Domain, SubDomain, RecodeId, ZoneId, DomainId, Server) VALUES ( '{info.Domain}', '{info.SubDomain}', '{info.RecodeId}', '{info.ZoneId}', '{info.DomainId}', '{info.Server}'); \r\n";

                }
                using SqliteConnection connection = new SqliteConnection(ConnectionString);
                connection.Open();
                using var command = new SqliteCommand(SQL, connection);
                var res = await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"init error  {ex.Message}");
                return false;
            }
        }



        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="loginUser"></param>
        /// <returns></returns>
        public async Task<(int code, string erro)> UpdatePwd(UpdatePwdRequest loginUser)
        {
            if (string.IsNullOrEmpty(loginUser.UserId))
            {
                return (-1, "用户不存在");
            }
            else
            {
                var user = await GetUser(loginUser.UserId);
                if (user != null)
                {
                    if (loginUser?.OldPassword?.Md5() != user.Password)
                    {
                        return (-1, "原密码错误");
                    }
                    else
                    {
                        var newpassword = loginUser.Password.Md5();
                        user.Password = newpassword;


                        string SQL = $"update login_user_info set passwor='{newpassword}' where id='{loginUser.UserId}'";
                        using SqliteConnection connection = new SqliteConnection(ConnectionString);
                        connection.Open();
                        using var command = new SqliteCommand(SQL, connection);
                        var res = await command.ExecuteNonQueryAsync();

                        return (res > 0 ? 0 : -1, res > 0 ? "更新成功" : "更新失败");
                    }
                }
                else
                {
                    return (-1, "用户不存在");
                }
            }
        }

    }



    public class PageInfo
    {
        public int pageIndex { get; set; } = 0;
        public int pageSize { get; set; } = 10;
    }

    public class PageDomainRecordInfo
    {
        public List<DomainRecordInfo> data { get; set; }
        public long Total { get; set; }
        public int pageIndex { get; set; } = 0;
        public int pageSize { get; set; } = 10;
    }

    public class RecordPageRequest : PageInfo
    {
        public List<string>? Dates { get; set; }
    }


    public class UpdatePwdRequest
    {
        public string UserId { get; set; }
        public string OldPassword { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

    }

    public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }


    //public class DataBaseConfigInfoRequest
    //{
    //    public SqlSugar.DbType DbType { get; set; }

    //    public string ConnString { get; set; }
    //}
}
