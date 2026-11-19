using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IspAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        public readonly IConfiguration configuration;
        public AccountController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        [HttpPost("login")]
        public ActionResult<string> Authenticate(AuthRequest authRequest)
        {
            var storedUsername = configuration["Authentication:Username"];
            var storedPassword = configuration["Authentication:Password"];

            if (authRequest.Username != storedUsername ||
                authRequest.Password != storedPassword)
            {
                return Unauthorized("Invalid username or password");
            }


            var secretKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["Authentication:SecretKey"]));
            var signingCred = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>();
            claims.Add(new Claim("Username", authRequest.Username));
            var securityToken = new JwtSecurityToken(
                configuration["Authentication:Issuer"],
                configuration["Authentication:Audience"],
                claims,
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(10),
                signingCred
               );
            var serilaizedToken = new JwtSecurityTokenHandler().WriteToken(securityToken);
            return Ok(serilaizedToken);
        }


    }
    public class AuthRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }


}
