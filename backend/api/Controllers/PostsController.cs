using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using api.Interfaces;
using api.Models;
using api.Services;
using System.Runtime.CompilerServices;


namespace api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly PostService _postServices;
        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;

        public PostsController(IConfiguration configuration, PostService postService, NotificationService notificationService)
        {
            _postServices = postService;
            _configuration = configuration;
            _notificationService = notificationService;
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

            if (newPost.id == null) return BadRequest(new { Message = "Failed to create post" });

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

            var mainUser = await _postServices.GetUserById(userIdToken);
            if (mainUser is null) return NotFound(new { Message = "User not found" });

            var post = await _postServices.GetPostById(postId);
            if (post == null) return NotFound(new { Message = "Post not found" });

            post.comments.Add(body.comment);
            var newPost = await _postServices.UpdatePost(postId, post);
            if (newPost == null) return BadRequest(new { Message = "Problem with values", Success = false });

            //to do notify the post creator 
            // Call Notification Start 
            var details = mainUser.Username + " commented on your post.";
            var us = new userIn { name = mainUser.Username ?? string.Empty, avatar = mainUser.imageUrl ?? string.Empty };
            var notification = new Notification
            {
                details = details,
                mainUserId = post.creator,
                targetId = mainUser.Id,
                user = us,
            };
            await _notificationService.CreateNotification(notification);
            //Call Notification end  

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

        #region Feed
        [HttpGet]
        [Route("")]
        [Authorize]
        public async Task<IActionResult> GetPostsPegenationAsync([FromQuery] int page)
        {
            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new { Message = "User is Unautharized" });

            var user = await _postServices.GetUserById(userIdToken);


            if (user is null) return NotFound(new { Message = "Not found" });
            var ids = user.following?.ToList() ?? new List<string>();

            ids.Add(user.Id.ToString());

            return Ok(await _postServices.Query(ids, page));
        }
        #endregion

        #region Update My Post
        [HttpPatch]
        [Route("{postId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost([FromRoute] string postId, [FromBody] CreateOrUpdatePostInterface updatedPost)
        {
            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new { Message = "Unauthorized user access" });

            if (string.IsNullOrEmpty(postId)) return BadRequest(new { Message = "Invalid post ID" });
            if (updatedPost is null) return BadRequest(new { Message = "Updated post data is required" });


            var post = await _postServices.GetPostById(postId);
            if (post is null) return NotFound(new { Message = "Post not found" });

            if (post.creator != userIdToken) return Unauthorized(new { Message = "You are not authorized to update this post" });

            post.title = updatedPost.title ?? post.title;
            post.message = updatedPost.message ?? post.message;
            post.selectedFiles = updatedPost.selectedFiles ?? post.selectedFiles;

            if (string.IsNullOrWhiteSpace(post.title) || string.IsNullOrWhiteSpace(post.message)) return BadRequest(new { Message = "Post data is required" });

            var updatedPostResult = await _postServices.UpdatePost(postId, post);
            if (updatedPostResult is null) return BadRequest(new { Message = "Failed to update post" });

            return Ok(new { Message = "Post updated successfully", result = updatedPostResult });
        }
        #endregion

        #region Post Likes
        [HttpPatch]
        [Route("{postId}/like")]
        [Authorize]
        public async Task<IActionResult> PostLikes([FromRoute]string postId)
        {
            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new { Message = "Unauthorized user access" });

            var mainUser = await _postServices.GetUserById(userIdToken);
            if (mainUser is null) return NotFound(new { Message = "User not found" });

            if (string.IsNullOrWhiteSpace(postId)) return BadRequest(new {Message="Post does not exist"});
            var post=await _postServices.GetPostById(postId);
            if (post is null) return NotFound(new { Message = "Post is not Found" });

            post.likes ??= new HashSet<string>();
            var isAlreadyLiked = post.likes.Contains(userIdToken);
            if (isAlreadyLiked)
            {
                post.likes.Remove(userIdToken);
            }
            else
            {
                post.likes.Add(userIdToken);
                //TO DO NOTIFY THE Post Creator THAT HE HAS A NEW like on post
                // Call Notification Start 
                var details = mainUser.Username + " liked your post.";
                var us = new userIn { name = mainUser.Username ?? string.Empty, avatar = mainUser.imageUrl ?? string.Empty };
                var notification = new Notification
                {
                    details = details,
                    mainUserId = post.creator,
                    targetId = userIdToken,
                    user = us,
                };
                await _notificationService.CreateNotification(notification);
                //Call Notification end  
            }

            await _postServices.UpdatePost(postId,post);
            var action = isAlreadyLiked ? "UnLiked" : "Liked";
            var newPost = new Post();

            return Ok(new { Success = true, message = $"{action} status updated successfully", newPost = post });
        }
        #endregion

        #region Delete Post
        [HttpDelete]
        [Route("{postId}")]
        [Authorize]
        public async Task<IActionResult> deletePost([FromRoute]string postId)
        {
            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdToken)) return Unauthorized(new {Message="User is Unauthorized"});

            if (string.IsNullOrEmpty(postId)) return BadRequest(new { Message = "Invalid post ID" });

            var post = await _postServices.GetPostById(postId);
            if (post is null) return NotFound(new { Message = "Post not found" });

            if (post.creator != userIdToken) return Unauthorized(new { Message = "You are not authorized to update this post" });

            await _postServices.DeletePostAsync(postId);    
            return Ok(new { Massage = "the post is deleted" });
        }
        #endregion
    }
}
