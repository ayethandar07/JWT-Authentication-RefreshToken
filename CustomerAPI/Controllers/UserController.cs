using CustomerAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CustomerAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly Learn_DBContext context;
        private readonly JWTSetting setting;

        public UserController(Learn_DBContext learn_DB, IOptions<JWTSetting> options)
        {
            context = learn_DB;
            setting = options.Value;
        }

        [Route("Authenticate")]
        [HttpPost]
        public IActionResult Authenticate([FromBody] usercred user)
        {
            var _user = context.TblUser.FirstOrDefault( o => o.Userid == user.username && o.Password == user.password);
            if(_user == null )
            {
                return Unauthorized();
            }

            var tokenhandler = new JwtSecurityTokenHandler();
            var tokenkey = Encoding.UTF8.GetBytes(setting.securitykey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    new Claim[]
                    {
                        new Claim(ClaimTypes.Name, _user.Userid),
                    }
                ),
                Expires = DateTime.Now.AddMinutes(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenkey), SecurityAlgorithms.HmacSha256)
            };
            var token = tokenhandler.CreateToken(tokenDescriptor);
            string finaltoken = tokenhandler.WriteToken(token);

            return Ok(finaltoken);
        }
    }
}
