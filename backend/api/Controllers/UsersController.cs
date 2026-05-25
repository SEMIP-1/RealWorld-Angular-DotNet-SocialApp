using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using api.Models;
using api.Services;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using api.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;

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
                    return NotFound(new { massage = "User with given id is not found" });
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

        [HttpGet]
        [Route("test-open")]
        public IActionResult Open()
        {
            return Ok("Open endpoint works");
        }

        [HttpGet]
        [Route("test-auth")]
        [Authorize]
        public IActionResult Auth()
        {
            return Ok("Authorized works");
        }

        #endregion

        [HttpPatch]
        [Route("Update")]
        [Authorize]

        public async Task<IActionResult> UpdateUser( [FromBody] UpdateUserInterface body)
        {
            var userId =User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId is null) 
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserById(userId);

            if (user is null)
            {
                return NotFound(new{message = "User not found"});
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

            return Ok (new 
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
    }
}
