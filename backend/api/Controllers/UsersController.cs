using api.Interfaces.UserInterface;
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
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserService _userService;

        public UsersController(IConfiguration configuration, UserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        #region signup
        [HttpPost]
        [Route("signup")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateUserInterface body)
        {
            var user = new User();
            if (body.FirstName == null || body.LastName == null || body.Email == null || body.Password == null)
            {
                return BadRequest(new { message = "Problem with provided body data." });
            }

            user.Username = body.FirstName + " " + body.LastName;
            user.Email = body.Email;
            user.Password = user.EncryptPasswordBase64(body.Password);

            //Check if email already exists before creating the account
            var CheckExistingUser = await _userService.GetUserByEmail(body.Email);
            if (CheckExistingUser != null)
            {
                return BadRequest(new { message = "Email already exists." });
            }

            await _userService.CreateUserAsync(user);
            //TODO Create a token and return it to the user
            var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub,user.Id??throw new InvalidOperationException()),
                    new Claim(ClaimTypes.Name,user.Username??throw new InvalidOperationException()),
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                };

            var tokenSecrete = _configuration.GetValue<string>("JwtSecret:Secret");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecrete ?? throw new IndexOutOfRangeException()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(1);

            var token = new JwtSecurityToken(
                issuer: "https://localhost:7089",
                audience: "https://localhost:7089",
                claims: claims,
                expires: expires,
                signingCredentials: creds
                );
            return Ok(new { result = user, token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
        #endregion

        #region signin
        [HttpPost]
        [Route("signin")]
        public async Task<IActionResult> login([FromBody] LoginInterface body)
        {
            if (body.Email == null || body.Password == null)
            {
                return BadRequest(new { message = "Problem with provided body data." });
            }

            var user = await _userService.GetUserByEmail(body.Email);
            if (user is null)
            {
                return NotFound();
            }
            var decryptedPassword = user.DecryptPasswordBase64(user.Password);

            if (decryptedPassword != body.Password)
            {
                return BadRequest(new { message = "Given Email or Password is invalid." });
            }
            else
            {
                //Sucessful login

                //Create a token and return it to the user
                var claims = new List<Claim> 
                {
                    new Claim(JwtRegisteredClaimNames.Sub,user.Id??throw new InvalidOperationException()),
                    new Claim(ClaimTypes.Name,user.Username??throw new InvalidOperationException()),
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                };

                var tokenSecrete = _configuration.GetValue<string>("JwtSecret:Secret");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecrete?? throw new IndexOutOfRangeException()));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.UtcNow.AddHours(1);
                
                var token = new JwtSecurityToken(
                    issuer: "https://localhost:7089",
                    audience: "https://localhost:7089",
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                    );

                return Ok(new { result = user, token = new JwtSecurityTokenHandler().WriteToken(token) });

            };      
        }
        #endregion

        #region Get User By Id
        [HttpGet]
        [Route("getUser/{id}")]
        public async Task<IActionResult> GetUserById([FromRoute] string id)
        {
            try
            {
                if (id == null)
                {
                    return BadRequest(new { message = "Problem with provided body data." });
                }

                var user = await _userService.GetUserById(id);
                if (user is null)
                {
                    return NotFound(new { message = "User with given id is not found" });
                }

                //TODO return Also the user posts

                return Ok(new { result = user, posts = Array.Empty<object>() });
            }
            catch
            {
                return BadRequest(new { message = "Something went wrong!." });
            }
        }
        #endregion

        #region Get Token
        [HttpGet]
        [Route("TestJWT"), Authorize]
        public IActionResult Test()
        {
            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(new { idToken = userIdToken });
        }
        #endregion

        #region update
        [HttpPatch]
        [Route("Update")]
        [Authorize]

        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserInterface body)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId is null)
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserById(userId);

            if (user is null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!string.IsNullOrEmpty(body.Name))
            {
                user.Username = body.Name;
            }
            if (!string.IsNullOrEmpty(body.Email))
            {
                user.Email = body.Email;
            }
            if (!string.IsNullOrEmpty(body.ImageUrl))
            {
                user.imageUrl = body.ImageUrl;
            }
            if (!string.IsNullOrEmpty(body.bio))
            {
                user.bio = body.bio;
            }

            var updatedUser = await _userService.UpdateUser(userId, user);

            return Ok(new
            {
                result = new
                {
                    updatedUser.Id,
                    updatedUser.Username,
                    updatedUser.Email,
                    updatedUser.imageUrl,
                    updatedUser.bio
                }
            }
            );
        }
        #endregion

        #region following
        [HttpPatch]
        [Route("{subUserId}/following")]
        [Authorize]
        public async Task<IActionResult> UserFollowing([FromRoute] string subUserId) 
        { 
            if(string.IsNullOrEmpty(subUserId)) { return NotFound(new { message = "User not found" }); }
            try 
            {
                //subUser is the user that is being followed and mainUser is the user that is following
                var subUser =await _userService.GetUserById(subUserId);
                if (subUser is null || subUser.Id is null ) {return NotFound(new { message = "User not found",Success = false }); }

                var mainUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(mainUserId)) { return Unauthorized(new { message = "problem with user token" }); }

                var mainUser = await _userService.GetUserById(mainUserId);
                if (mainUser is null || mainUser.Id is null) { return NotFound(new { message = "User not found", Success = false }); }
                if (mainUserId == subUserId){return BadRequest(new{message = "User cannot follow himself", Success = false});}

                mainUser.following ??= new HashSet<string>();
                subUser.followers ??= new HashSet<string>();

                var isAlreadyFollowing = mainUser.following.Contains(subUserId);
                if (isAlreadyFollowing)
                {
                    mainUser.following.Remove(subUserId);
                    subUser.followers.Remove(mainUserId);
                }
                else
                {
                    mainUser.following.Add(subUserId);
                    subUser.followers.Add(mainUserId);
                    //TO DO NOTIFY THE USER THAT HE HAS A NEW FOLLOWER
                }

                await _userService.UpdateUser(mainUserId, mainUser);
                await _userService.UpdateUser(subUserId, subUser);

                var action = isAlreadyFollowing? "Unfollowed": "Followed";

                return Ok(new {Success= true,message = $"{action} status updated successfully" });

            }
            catch (Exception ex)
            {

                return BadRequest(new { Message = ex.Message, Success = false });
            }
        }
        #endregion

        #region getSuggestedUsers
        [HttpGet]
        [Route("getSuggestedUsers")]
        [Authorize]
        public async Task<IActionResult> GetSuggestedUsers()
        {
            try
            {
                var mainUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(mainUserId)) { return Unauthorized(new { Message = "User is Not Found", Success = false }); }
                var mainUser = await _userService.GetUserById(mainUserId);
                if (mainUser == null) { return NotFound(new { Message = "Main user not found", Success = false }); }

                var mainUserFollowingId = mainUser.following;
                if (mainUserFollowingId == null) { return NotFound(new { Message = "Main user not following anyone", Success = false }); }

                var mainUserFollowing = new List<User>();

                foreach (var followingUserId in mainUserFollowingId)
                {
                    var followingUser = await _userService.GetUserById(followingUserId);
                    if (followingUser != null)
                    {
                        mainUserFollowing.Add(followingUser);
                    }
                }

                var suggestedUsers = new HashSet<string>();
                var finalSuggestedUsers = new List<User>();

                foreach (var followingUser in mainUserFollowing)
                {
                    if (followingUser.followers != null && followingUser.Id != null)
                    {
                        foreach (var subUserId in followingUser.followers)
                        {
                            if (suggestedUsers.Contains(subUserId) || subUserId == mainUserId|| mainUserFollowingId.Contains(subUserId)) { continue; }
                            var subUser = await _userService.GetUserById(subUserId);
                            if (subUser != null) { finalSuggestedUsers.Add(subUser); }
                            suggestedUsers.Add(subUserId);
                        }
                    }

                    if (followingUser.following != null && followingUser.Id != null)
                    {
                        foreach (var subUserId in followingUser.following)
                        {
                            if (suggestedUsers.Contains(subUserId) || subUserId == mainUserId|| mainUserFollowingId.Contains(subUserId)) { continue; }
                            var subUser = await _userService.GetUserById(subUserId);
                            if (subUser != null) { finalSuggestedUsers.Add(subUser); }
                            suggestedUsers.Add(subUserId);
                        }
                    }
                }

                return Ok(new { Users=finalSuggestedUsers,
                                Success = true,
                                Message="Suggested users retrieved successfully"});
            }
            catch (Exception ex)
            {

                return BadRequest(new { Message = ex.Message, Success = false });
            }
        } 
        #endregion



    }
}
