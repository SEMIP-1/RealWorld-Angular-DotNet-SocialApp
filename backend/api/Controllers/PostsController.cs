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
    public class PostsController : ControllerBase
    {
        private readonly PostService _postServices;
        private readonly IConfiguration _configuration;

        public PostsController(IConfiguration configuration, PostService postService) 
        { 
            _postServices=postService;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("createposts")]
        [Authorize]
        public async Task<IActionResult> CreatePosts([FromBody] CreateOrUpdatePostInterface body)
        {
            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if (userIdToken == null) return Unauthorized(new { Message = "Unauthorized user access" });
            if (body.title == null || body.message       == null) return BadRequest(new { Message = "Post data is required" });
            var newPost = new Post
            {
                title = body.title,
                message = body.message,
                creator = userIdToken,
                selectedFiles = body.selectedFiles,
            };

            await _postServices.CreateOnePostAsync(newPost);

            if (newPost._id == null) return BadRequest(new { Message = "Failed to create post" });

            return Ok(new { Message = "Post created successfully", newPost });
        }



        [HttpGet]
        [Route("{postId}")]
        [Authorize]
        public async Task<IActionResult> GetPost([FromRoute] string postId)
        {
            var userIdToken =User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new { Message = "Unauthorized user access" });

            var userPost= await _postServices.GetPostById(postId);
            if (userPost == null) return NotFound(new { Message = "Post not found" });
        
            return Ok(new { Message = "Post retrieved successfully", userPost });
        }
    }
}
