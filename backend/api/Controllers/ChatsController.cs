using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using api.Interfaces;
using api.Models;
using api.Services;

namespace api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ChatsController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly IConfiguration _configuration;

        public ChatsController(IConfiguration configuration, ChatService chatService)
        {
            _chatService = chatService;
            _configuration = configuration;
        }

        #region Send a Message
        [HttpPost]
        [Route("sendmessage")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageInterface body)
        {
            if (body.senderId == null || body.receiverId == null || body.content == null)
            {
                return BadRequest(new { message = "Sender ID, Receiver ID, and Content are required." });
            }
            var senderIdToken = User.FindFirst(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(senderIdToken?.Value)) return Unauthorized("Sender ID not found in token.");
            if (body.senderId != senderIdToken.Value) return Unauthorized("Sender ID does not match the token.");

            var newMessage = new Message
            {
                Content = body.content,
                SenderId = body.senderId,
                ReceiverId = body.receiverId,
                Timestamp = DateTime.UtcNow
            };
            if(newMessage is null) return BadRequest(new{ message = "Message is null." });

            await _chatService.SendMessageAsync(newMessage);
            return Ok(newMessage);
        }
        #endregion

        #region get messages by nums
        [HttpGet]
        [Route("getmessagesbynums")]
        [Authorize]
        public async Task<IActionResult> GetMessagesByNumsBetweenTwoUsers([FromQuery] string num, [FromQuery] string senderId, [FromQuery] string receiverId)
        {
            if (string.IsNullOrEmpty(num) || string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId))
            {
                return BadRequest(new { message = "Sender ID, Receiver ID, and num are required." });
            }

            var senderIdToken = User.FindFirst(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(senderIdToken?.Value)) return Unauthorized("Sender ID not found in token.");
            if (senderId != senderIdToken.Value) return Unauthorized("Sender ID does not match the token.");

            List<Message> msgs=await _chatService.GetMessagesAsynByNums(int.Parse(num),senderId, receiverId);

            if (msgs is null) return BadRequest(new { message = "Message is null." });

            return Ok(new { msgs });
        }
        #endregion

        #region Get User Unread Messages
        [HttpGet]
        [Route("getuserunreadmessages")]
        [Authorize]
        public async Task<IActionResult> GetUserUnreadMessages([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { message = "User ID is required." });
            }
            var senderIdToken = User.FindFirst(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(senderIdToken?.Value)) return Unauthorized("Sender ID not found in token.");
            if (userId != senderIdToken.Value) return Unauthorized("User ID does not match the token.");

            var unReadedMessages = await _chatService.GetUserUnReadedMessagesAsync(userId);
            int totalUnreadMessages = unReadedMessages.Sum(m => m.NumOfUnReadedMessages);
            return Ok(new { unReadedMessages, totalUnreadMessages });
        }
        #endregion

        #region Mark Messages as Read
        [HttpGet]
        [Route("markmessagesasread")]
        [Authorize]
        public async Task<IActionResult> MarkMessagesAsRead([FromQuery] string senderId, [FromQuery] string receiverId)
        {
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId))
            {
                return BadRequest(new { message = "Sender ID and Receiver ID are required." });
            }
            var senderIdToken = User.FindFirst(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(senderIdToken?.Value)) return Unauthorized("Sender ID not found in token.");
            if (senderId != senderIdToken.Value) return Unauthorized("Sender ID does not match the token.");

            await _chatService.MarkMsgsAsReadedAsync(senderId, receiverId);
            return Ok(new { message = "Messages marked as read." });
        }
        #endregion
    }
}
