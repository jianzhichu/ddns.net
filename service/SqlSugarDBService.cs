//using ClockSnowFlake;
//using SqlSugar;
//using ddns.net.extension;
//using ddns.net.model;

//namespace ddns.net.service
//{
//    public class SqlSugarDBService
//    {
//        private readonly ISqlSugarClient sugarClient;

//        public static DomainConfigInfo domainConfig;

//        public SqlSugarDBService(SqlSugarFactory  sqlsugarFactory,IConfiguration configuration)
//        {
//            sugarClient = sqlsugarFactory.GetInstance(configuration);
//        }
//        /// <summary>
//        /// 查询配置
//        /// </summary>
//        /// <param name="refresh">是否查询最新的-即从数据库查询</param>
//        /// <returns></returns>
//        public DomainConfigInfo GetDomainConfig(bool refresh = false)
//        {
//            if (sugarClient == null)
//                return null;
//            if (domainConfig == null || refresh) 
//            {
//                domainConfig = sugarClient.Queryable<DomainConfigInfo>().First();
//            }
//            return domainConfig;
//        }
//        ///// <summary>
//        ///// 查询解析recordId
//        ///// </summary>
//        ///// <param name="domain"></param>
//        ///// <param name="subDomain"></param>
//        ///// <returns></returns>
//        //public async Task<DomainRecordIdInfo> GetDomainRecordId(string domain, string subDomain)
//        //{
//        //    return await sugarClient.Queryable<DomainRecordIdInfo>()
//        //        .Where(x => x.Domain == domain && x.SubDomain == subDomain).FirstAsync();
//        //}

//        /// <summary>
//        /// 查询解析RECORDID-列表
//        /// </summary>
//        /// <returns></returns>
//        public async Task<List<DomainRecordIdInfo>> GetDomainRecordIds(string server)
//        {
//            return await sugarClient.Queryable<DomainRecordIdInfo>().Where(x => x.Server == server).ToListAsync();
//        }

//        /// <summary>
//        /// 更新record记录
//        /// </summary>
//        /// <param name="idInfo"></param>
//        /// <returns></returns>
//        public async Task<bool> UpdateDomainRecordIdWithColums(DomainRecordIdInfo idInfo, string[] colums)
//        {
//            var idinfox = await sugarClient.Queryable<DomainRecordIdInfo>().Where(x => x.Domain == idInfo.Domain
//                && x.SubDomain == idInfo.SubDomain
//                && x.Server == idInfo.Server).FirstAsync();
//            if (idinfox == null)
//            {
//                idInfo.Id = IdGener.GetLong().ToString();
//                var insert = await sugarClient.Insertable(idInfo).ExecuteCommandAsync();
//                return insert > 0;
//            }
//            else
//            {
//                var update = await sugarClient.Updateable(idInfo).UpdateColumns(colums)
//                .Where(x => x.Domain == idInfo.Domain
//                && x.SubDomain == idInfo.SubDomain
//                && x.Server == idInfo.Server).ExecuteCommandAsync();
//                return update > 0;
//            }
//        }

     
//        /// <summary>
//        /// 更新配置
//        /// </summary>
//        /// <param name="domainConfigInfo"></param>
//        /// <returns></returns>
//        public async Task<(int code, string erro)> SaveDomainConfig(DomainConfigInfo domainConfigInfo)
//        {
//            if (!string.IsNullOrEmpty(domainConfigInfo.Id))
//            {
//                domainConfigInfo.UpdateTime = DateTime.Now;
//                var res = await sugarClient.Updateable(domainConfigInfo).IgnoreColumns(it => new { it.CreateTime, it.Id }).Where(d=>d.Id==domainConfig.Id).ExecuteCommandAsync();
//                return (res > 0 ? 0 : -1, res > 0 ? "修改成功" : "修改失败");
//            }
//            else
//            {
//                domainConfigInfo.Id = IdGener.GetLong().ToString();
//                domainConfigInfo.CreateTime = DateTime.Now;
//                var res = await sugarClient.Insertable(domainConfigInfo).ExecuteCommandAsync();
//                return (res > 0 ? 0 : -1, res > 0 ? "新增成功" : "新增失败");
//            }
//        }

//        /// <summary>
//        /// 指定字段更新
//        /// </summary>
//        /// <param name="domainConfigInfo"></param>
//        /// <param name="colums"></param>
//        /// <returns></returns>
//        public async Task<bool> UpdateConfigWithColumns(DomainConfigInfo domainConfigInfo,string[] colums)
//        {
//            var res = await sugarClient.Updateable(domainConfigInfo)
//                   .UpdateColumns(colums).Where(x => x.Id == domainConfigInfo.Id).ExecuteCommandAsync();
//            return res > 0;
//        }
//        /// <summary>
//        /// 新增解析记录
//        /// </summary>
//        /// <param name="domainRecordInfo"></param>
//        /// <returns></returns>
//        public async Task<bool> InsertDomainRecord(DomainRecordInfo domainRecordInfo)
//        {
//            domainRecordInfo.Id = IdGener.GetLong().ToString();
//            domainRecordInfo.CreateTime = DateTime.Now;
//            var res = await sugarClient.Insertable(domainRecordInfo).ExecuteCommandAsync();
//            return res > 0;
//        }

//        /// <summary>
//        /// 查询最新一条解析记录
//        /// </summary>
//        /// <returns></returns>
//        public async Task<DomainRecordInfo> GetLastDomainRecord()
//        {
//          return  await sugarClient.Queryable<DomainRecordInfo>().Where(x=>!x.IsDelete).OrderByDescending(x => x.CreateTime).FirstAsync();
//        }

//        /// <summary>
//        /// 删除解析记录
//        /// </summary>
//        /// <returns></returns>
//        public async Task<bool> DeleteDomainRecords()
//        {
//            var delete = await sugarClient.Updateable<DomainRecordInfo>().SetColumns(x => x.IsDelete == true).Where(x=>1==1).ExecuteCommandAsync() ;
//            return delete > 0;
//        }

//        /// <summary>
//        /// 分页查询
//        /// </summary>
//        /// <param name="request"></param>
//        /// <returns></returns>
//        public async Task<PageDomainRecordInfo> DomainRecordPgaeList(RecordPageRequest request)
//        {

//            DateTime? start=null;
//            DateTime? end=null;

//            if(request.Dates!=null && request.Dates.Any())
//            {
//                if(request.Dates.Count==1) {
//                     if(DateTime.TryParse(request.Dates[0], out DateTime start1))
//                    {
//                        start = start1;
//                    }
//                }
//                if (request.Dates.Count == 2)
//                {
//                    if (DateTime.TryParse(request.Dates[0], out DateTime start1))
//                    {
//                        start = start1;
//                    }

//                    if (DateTime.TryParse(request.Dates[1], out DateTime end1))
//                    {
//                        end = end1;
//                    }
//                }
//            }

//            var totalCount = 0;
//            var data = sugarClient.Queryable<DomainRecordInfo>()
//                .WhereIF(start.HasValue, x => x.CreateTime >= start.Value)
//                .WhereIF(end.HasValue, x => x.CreateTime <= end.Value)
//                .OrderByDescending(it => it.Id).ToPageList(request.pageIndex, request.pageSize, ref totalCount);
//            return  new PageDomainRecordInfo {  data= data, Total=totalCount, pageIndex=request.pageIndex, pageSize=request.pageSize };
//        }

//        /// <summary>
//        /// 初始化系统管理员
//        /// </summary>
//        /// <param name="userInfo"></param>
//        /// <returns></returns>
//        public async Task<(int code,string erro)> InitUser(LoginUserInfo userInfo)
//        {
//            var isInit = await sugarClient.Queryable<LoginUserInfo>().AnyAsync();
//            if (isInit)
//            {
//                return (-1, "该接口只允许第一次初始化的时候调用");
//            }
//            else
//            {
//                userInfo.Id = IdGener.GetLong().ToString();
//                userInfo.CreateTime = DateTime.Now;
//                userInfo.Password = Util.Md5(userInfo.Password);

//                var res = await sugarClient.Insertable(userInfo).ExecuteCommandAsync();
//                return (res > 0?0:-1, res > 0 ? "初始用户成功" : "初始化用户失败");
//            }
//        }

//        /// <summary>
//        /// 修改密码
//        /// </summary>
//        /// <param name="loginUser"></param>
//        /// <returns></returns>
//        public async Task<(int code, string erro)> UpdatePwd(UpdatePwdRequest loginUser)
//        {
//            if (string.IsNullOrEmpty(loginUser.UserId))
//            {
//                return (-1, "用户不存在");
//            }
//            else
//            {
//                var user = await sugarClient.Queryable<LoginUserInfo>().Where(x => x.Id == loginUser.UserId).FirstAsync();
//                if (user != null)
//                {
//                    if (loginUser?.OldPassword?.Md5() != user.Password)
//                    {
//                        return (-1, "原密码错误");
//                    }
//                    else
//                    {
//                        var newpassword = loginUser.Password.Md5();
//                        user.Password = newpassword;
//                        var res = await sugarClient.Updateable(user)
//                            .UpdateColumns(u => u.Password).Where(u => u.Id == loginUser.UserId).ExecuteCommandAsync();

//                        return (res > 0 ? 0 : -1, res > 0 ? "更新成功" : "更新失败");
//                    }
//                }
//                else
//                {
//                    return (-1, "用户不存在");
//                }
//            }
//        }
//        ///// <summary>
//        ///// 检查是否已经初始化过用户
//        ///// </summary>
//        ///// <returns></returns>
//        //public async Task<(int code, string erro)> Check()
//        //{
//        //    var isInit = await sugarClient.Queryable<LoginUserInfo>().AnyAsync();
//        //    if (isInit)
//        //    {
//        //        return (0, "");
//        //    }
//        //    else
//        //    {
//        //        return (-1, "尚未初始化用户");
//        //    }
//        //}
//        /// <summary>
//        /// 查询用户-系统里面只会有一个用户
//        /// </summary>
//        /// <param name="userName"></param>
//        /// <returns></returns>
//        public async Task<LoginUserInfo> GetUser(string userName=null)
//        {
//            return await sugarClient.Queryable<LoginUserInfo>().WhereIF(!string.IsNullOrEmpty(userName),x => x.UserName == userName).FirstAsync();
//        }
//        /// <summary>
//        /// 修改头像
//        /// </summary>
//        /// <param name="Id"></param>
//        /// <param name="avatar"></param>
//        /// <returns></returns>
//        public async Task<bool> UpdateAvatar(string Id,string avatar)
//        {
//            //var update=    await sqlSugarClient.Updateable<LoginUserInfo>()
//            //.SetColumns(p => new LoginUserInfo { Avatar = avatar })
//            //.Where(p => p.Id == Id).ExecuteCommandAsync();
//            //return update > 0;

//            var user = await sugarClient.Queryable<LoginUserInfo>().Where(p => p.Id == Id).FirstAsync();
//            if (user is null)
//            {
//                return false;
//            }
//            else
//            {
//                user.Avatar = avatar;
//                var update = await sugarClient.Updateable(user)
//                        .UpdateColumns(p => p.Avatar).Where(p => p.Id == Id).ExecuteCommandAsync();
//                return update > 0;
//            }
//        }

//        /// <summary>
//        /// 获取邮件配置
//        /// </summary>
//        /// <returns></returns>
//        public async Task<StmpConfigInfo> GetStmpConfig()
//        {
//            try
//            {
//                return await sugarClient.Queryable<StmpConfigInfo>().Where(x => x.Open).FirstAsync();
//            }
//            catch (Exception ex)
//            {
//                Serilog.Log.Error($"stmp config get error,{ex.Message}");
//                return null;
//            }
//        }

//        /// <summary>
//        /// 新增或者编辑stmpconfig
//        /// </summary>
//        /// <param name="stmpConfigInfo"></param>
//        /// <returns></returns>
//        public async Task<bool> SaveStmpConfig(StmpConfigInfo stmpConfigInfo)
//        {
//            if (stmpConfigInfo != null)
//            {
//                stmpConfigInfo.To = stmpConfigInfo.From;
//            }
//            if (!string.IsNullOrEmpty(stmpConfigInfo.Id))
//            {
//                var res = await sugarClient.Updateable(stmpConfigInfo).ExecuteCommandAsync();
//                return res > 0;
//            }
//            else
//            {
//                stmpConfigInfo.Id = IdGener.GetLong().ToString();
//                var res= await sugarClient.Insertable(stmpConfigInfo).ExecuteCommandAsync();
//                return res > 0;
//            }
//        }


//        /// <summary>
//        /// 重置配置表-谨慎使用
//        /// </summary>
//        /// <returns></returns>
//        public bool ResetAllConfigTable()
//        {
//            try
//            {
//                sugarClient.Ado.BeginTran();
//                sugarClient.Deleteable<DomainConfigInfo>().ExecuteCommand();
//                sugarClient.Deleteable<DomainRecordIdInfo>().ExecuteCommand();
//                sugarClient.Deleteable<DataBaseConfigInfo>().ExecuteCommand();
//                sugarClient.Deleteable<LoginUserInfo>().ExecuteCommand();
//                sugarClient.Deleteable<StmpConfigInfo>().ExecuteCommand();
//                sugarClient.Ado.CommitTran();
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Serilog.Log.Error($"重置配置表失败,{ex.Message}");
//                return false;
//            }

//        }
//    }



//}
