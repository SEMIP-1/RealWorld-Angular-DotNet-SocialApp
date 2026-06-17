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

        #region Send a Message
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
    }
}
