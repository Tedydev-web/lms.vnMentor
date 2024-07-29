using vnMentor.Data;
using vnMentor.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace vnMentor.Utils
{
    public class ErrorLoggingService
    {
        private readonly DefaultDBContext _dbContext;
        private Util _util;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ErrorLoggingService(DefaultDBContext dbContext, Util util, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _util = util;
            _httpContextAccessor = httpContextAccessor;
        }

        public void LogError(Exception exception, string errorMessage)
        {
            var error = new ErrorLog
            {
                Id = Guid.NewGuid().ToString(),
                ErrorMessage = errorMessage,
                ErrorDetails = exception?.ToString(),
                ErrorDate = _util.GetSystemTimeZoneDateTimeNow()
            };
            string userId = "";
            if (_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
            {
                userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            error.UserId = userId;
            _dbContext.ErrorLogs.Add(error);
            _dbContext.SaveChanges();
        }
    }

}
