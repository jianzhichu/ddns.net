using SqlSugar;
using System.Reflection;

namespace ddns.net.service
{
    public class SqlSugarFactory
    {
        //private  ISqlSugarClient _db;

        public ISqlSugarClient GetInstance(IConfiguration configuration)
        {
            if (!string.IsNullOrEmpty(configuration["dbtype"]))
            {
                DbType dbType = GetDBType(configuration);

                string connectionString = GetConnString(configuration, dbType);

                return GetDbInstance(dbType, connectionString);
            }
            else
            {
                //throw new Exception("dbconfig is null");
                return null;
            }
           
        }

        private static string GetConnString(IConfiguration configuration, DbType dbType)
        {
            var connectionString = configuration["dbconn"];
            if (dbType == DbType.Sqlite)
            {
                connectionString = CreateSqliteDBConn();
            }
            return connectionString;
        }

        private static string CreateSqliteDBConn()
        {
            var fileFloder = Path.Combine(Environment.CurrentDirectory, "db");
            if (!Directory.Exists(fileFloder))
            {
                Directory.CreateDirectory(fileFloder);
            }
            var filePath = Path.Combine(fileFloder, "ddns.sqlite");
            string conn = $"DataSource={filePath}";
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();//.Close();
            }

            return conn;
        }

        private static DbType GetDBType(IConfiguration configuration)
        {
            DbType dbType = DbType.Sqlite;
            var dbtypeString = configuration["dbtype"].ToLower();
            // 获取颜色枚举类型的所有枚举值
            var dbtypes = Enum.GetValues(typeof(DbType));
            foreach (DbType type in dbtypes)
            {
                if (type.ToString().ToLower() == dbtypeString)
                {
                    dbType = type;
                    break;
                }
            }

            return dbType;
        }

        public void Dispose()
        {
            //_db?.Dispose();
        }





        public ISqlSugarClient GetDbInstance(DbType dbType, string ConnString)
        {
          
            if (dbType == DbType.Sqlite)
            {
                ConnString = CreateSqliteDBConn();
            }
            if (!string.IsNullOrEmpty(ConnString))
            {

                var sqlSugar = new SqlSugarClient(new ConnectionConfig
                {
                    ConnectionString = ConnString,
                    InitKeyType = InitKeyType.Attribute,
                    DbType = dbType,
                    IsAutoCloseConnection = true // close connection after each operation (recommended)
                }, db =>
                {
                    //单例参数配置，所有上下文生效       
                    db.Aop.OnLogExecuting = (sql, pars) =>
                    {
                        //Serilog.Log.Debug(sql);//输出sql
                    };

                    db.Aop.OnError = (e) =>
                    {
                        Serilog.Log.Error(e.Message);
                        Serilog.Log.Error(e.Sql);
                    };

                    db.DbMaintenance.CreateDatabase(); // 检查数据库是否存在，如果不存在则创建
                    Assembly assembly = Assembly.GetExecutingAssembly(); // 获取当前程序集
                    Type[] types = assembly.GetTypes(); // 获取程序集中的所有类型

                    var targetClasses = types
                        .Where(t => t.Namespace != null && t.Namespace.StartsWith("ddns.net.model"))
                        .ToList();
                    db.CodeFirst.InitTables(targetClasses.ToArray());
                });
               
                return sqlSugar;
            }
            //}
            return null;
        }
    }

}
