using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JCBSystem.Login;
using Microsoft.IdentityModel.Tokens;



namespace JCBSystem.common
{
    public class JwtTokenHelper
    {
        public static string GetJWTToken(Dictionary<string, string> user)
        {
            string jwtKey = JCBSystem.Properties.Settings.Default.JwtKey;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user["Username"]),
                new Claim(ClaimTypes.Role, textInfo.ToTitleCase(user["UserLevel"].ToLower())),
            };

            // Gamitin ang tamang secret key mula sa configuration
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(10),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }


        public static bool IsTokenExpired(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
    }
}
