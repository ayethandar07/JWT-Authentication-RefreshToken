﻿using CustomerAPI.Models;
using CustomerAPI.Repositories;
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
        private readonly IRefreshTokenGenerator tokenGenerator;
        private readonly JWTSetting setting;

        public UserController(Learn_DBContext learn_DB, IOptions<JWTSetting> options, IRefreshTokenGenerator refreshToken)
        {
            context = learn_DB;
            tokenGenerator = refreshToken;
            setting = options.Value;
        }

        public TokenResponse Authenticate(string username, Claim[] claims)
        {
            TokenResponse response = new TokenResponse();
            var tokenkey = Encoding.UTF8.GetBytes(setting.securitykey);

            var tokenhandler = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(2),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(tokenkey), SecurityAlgorithms.HmacSha256));

            response.JWTToken = new JwtSecurityTokenHandler().WriteToken(tokenhandler);
            response.RefreshToken = tokenGenerator.GenerateToken(username);

            return response;
        }

        [Route("Authenticate")]
        [HttpPost]
        public IActionResult Authenticate([FromBody] usercred user)
        {
            TokenResponse tokenResponse = new TokenResponse();

            var _user = context.TblUser.FirstOrDefault(o => o.Userid == user.username && o.Password == user.password);
            if (_user == null)
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

            tokenResponse.JWTToken = finaltoken;
            tokenResponse.RefreshToken = tokenGenerator.GenerateToken(user.username);

            return Ok(tokenResponse);
        }

        [Route("Refresh")]
        [HttpPost]
        public IActionResult Refresh ([FromBody] TokenResponse token)
        {
            var tokenhandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenhandler.ValidateToken(token.JWTToken, new TokenValidationParameters 
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(setting.securitykey)),
                ValidateIssuer = false,
                ValidateAudience = false
            }, out securityToken);

            var _token = securityToken as JwtSecurityToken;

            if (_token != null && !_token.Header.Alg.Equals(SecurityAlgorithms.HmacSha256))
            {
                return Unauthorized();
            }

            var username = principal.Identity.Name;
            var _reftable = context.TblRefreshtoken.FirstOrDefault(o => o.UserId == username && o.RefreshToken == token.RefreshToken);
            if(_reftable == null)
            {
                return Unauthorized();
            }

            TokenResponse _result = Authenticate(username, principal.Claims.ToArray());

            return Ok(_result);
        }
    }
}
