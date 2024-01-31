//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc.Filters;
//using Microsoft.Extensions.Primitives;

//namespace ddns.net.auth
//{


//    public class CustomAuthorizationHandler : AuthorizationHandler<CustomRequirement>
//    {
//        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CustomRequirement requirement)
//        {
//            // 假设 permissionName 是授权要求的名称
//            //string permissionName = requirement.PermissionName;

//            // 在这里执行自定义的授权逻辑
//            if (HasPermission(context))
//            {
//                context.Succeed(requirement);
//            }
//            else
//            {
//                context.Fail();
//            }

//            return Task.CompletedTask;
//        }

//        private bool HasPermission(AuthorizationHandlerContext context)
//        {
//            // 根据用户名和权限名从数据库或其他存储中查询用户的权限信息，并判断是否拥有指定的权限
//            // 返回 true 表示有权限，返回 false 表示无权限
//            AuthorizationFilterContext filterContext = context.Resource as AuthorizationFilterContext;
//            HttpContext httpContext = filterContext.HttpContext;
//            httpContext.Request.Headers.TryGetValue("Authorization", out StringValues Authorization);
//            var token=  Authorization.ToString();
//            token = token.Split(" ")[1];
//            // 这里只是一个示例，实际情况需要根据你的业务逻辑来实现
//            if (username == "admin" && permissionName == "canAccessAdminPanel")
//            {
//                return true;
//            }

//            return false;
//        }
//    }

//    public class CustomRequirement : IAuthorizationRequirement
//    {
//        //public string PermissionName { get; }

//        //public CustomRequirement(string permissionName)
//        //{
//        //    PermissionName = permissionName;
//        //}
//    }

//}
