using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using UsersManagement.Models;

namespace UsersManagement.Services
{
    public class TokenBuilder : ITokenBuilder
    {
        public string CreateToken(ApplicationUser user){

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("placeholder-key-that-is-long-enough-for-sha256"));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor{
                Subject= new ClaimsIdentity(
                    new Claim[]{
                        new Claim("UserID", user.Id.ToString())
                    }),
                Expires= DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = signingCredentials
            };

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(descriptor);
            return tokenHandler.WriteToken(securityToken);
        }
    }
}
