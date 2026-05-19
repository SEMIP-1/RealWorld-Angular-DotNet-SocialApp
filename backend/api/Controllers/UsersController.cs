using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using api.Models;
using api.Services;
using api.Interfaces;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;

namespace api.Controllers
{
    [Route("api/[controller]")]
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

        [HttpPost]
        [Route("signup")]
        public async Task<IActionResult> CreateAccount([FromBody] UserInterface user) 
        { 
            var NewUser = new User();
            if (user.FirstName == null || user.LastName == null || user.Email == null || user.Password == null)
            {
                return BadRequest(new { message = "Problem with provided body data." });
            }

            NewUser.Username = user.FirstName+" "+user.LastName;
            NewUser.Email = user.Email;
            NewUser.Password = NewUser.EncryptPasswordBase64(user.Password);

            //TODO check if email already exists before creating the account

            await _userService.CreateUserAsync(NewUser);

            //TODO Create a token and return it to the user

            return Ok(new { result=NewUser});
        }
    }
}
