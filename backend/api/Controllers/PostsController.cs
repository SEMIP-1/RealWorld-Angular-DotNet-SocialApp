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
            _postServices = postService;
            _configuration = configuration;
        }

        #region CreatePosts
        [HttpPost]
        [Route("")]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromBody] CreateOrUpdatePostInterface body)
        {
            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier)?.ToString();
            if (userIdToken == null) return Unauthorized(new { Message = "Unauthorized user access" });
            if (body.title == null || body.message == null) return BadRequest(new { Message = "Post data is required" });
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
        #endregion

        #region GetPost
        [HttpGet]
        [Route("{postId}")]
        [Authorize]
        public async Task<IActionResult> GetPost([FromRoute] string postId)
        {
            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new { Message = "Unauthorized user access" });

            var userPost = await _postServices.GetPostById(postId);
            if (userPost == null) return NotFound(new { Message = "Post not found" });

            return Ok(new { Message = "Post retrieved successfully", userPost });
        }
        #endregion

        #region AddComment

        [HttpPost]
        [Route("{postId}/comments")]
        [Authorize]
        public async Task<IActionResult> AddComment([FromRoute] string postId, [FromBody] CommentBodyInterface body)
        {
            if (string.IsNullOrEmpty(postId)) return NotFound(new { Message = "Post Not Found" });
            if (string.IsNullOrWhiteSpace(body.comment) || body is null) return BadRequest(new { Message = "please Enter post comment" });

            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new { Message = "User is not authorized" });

            var post = await _postServices.GetPostById(postId);
            if (post == null) return NotFound(new { Message = "Post not found" });

            post.comments.Add(body.comment);
            var newPost = await _postServices.UpdatePost(postId, post);
            if (newPost == null) return BadRequest(new { Message = "Problem with values", Success = false });

            //to do notify the post creator 

            return Ok(new { Message = "Comment added successfully", result = newPost });
        }
        #endregion

        #region SearchforUsersPost
        [HttpGet]
        [Route("search")]
        [Authorize]
        public async Task<IActionResult> SearchforUsersPost([FromQuery] string query)
        {
            if (!string.IsNullOrEmpty(query)) return BadRequest(new { Message = "input query is in valid" });

            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new { Message = "User is not authorized" });

            var (posts, users) = await _postServices.Search(query);
            if (posts.Count == 0 && users.Count == 0) return NotFound(new { Message = "No posts or users found matching the search query" });

            return Ok(new { posts, users });
        }
        #endregion

        [HttpGet]
        [Route("")]
        [Authorize]
        public async Task<IActionResult> GetPostsPegenationAsync([FromQuery]int page) 
        {
            var userIdToken=User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new {Message="User is Unautharized"});

            var user = await _postServices.GetUserById(userIdToken);
            if(user is null) return NotFound(new {Message="Not found"});
            var ids = user.following.ToList()??new List<string>();
            
            ids.Add(user.Id.ToString());

            return Ok(await _postServices.Query(ids,page));
        }
    }
}
