using api.Interfaces;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _notificationService;
        public NotificationsController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        #region Get user Notifications
        [HttpGet]
        [Route("{id}")]
        [Authorize]
        public async Task<IActionResult> GetNotification()
        {
            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new { Message = "User not authenticated" });

            var notification = await _notificationService.GetUserNotification(userIdToken);
            if (notification == null) return NotFound(new { Message = "Notification not found" });

            return Ok(new { notification });
        } 
        #endregion

        [HttpGet]
        [Route("markAsRead")]
        [Authorize]
        public async Task<IActionResult> MarkNotificationAsRead()
        {
            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new { Message = "User not authenticated" });

            await _notificationService.MarkNotificationAsRead(userIdToken);
            return Ok(new { Message = "Notification marked as read" });
        }
    }
}
