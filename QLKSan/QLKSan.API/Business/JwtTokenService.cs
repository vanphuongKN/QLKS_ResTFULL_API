using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QLKSan.Common;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public JwtTokenService(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public LoginResponse CreateToken(TaiKhoan account)
        {
            var response = new LoginResponse();
            account.FillUserInfo(response);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.MaDangNhap),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(AppClaimTypes.MaDangNhap, account.MaDangNhap),
                new Claim(ClaimTypes.Name, account.TenDangNhap),
                new Claim(ClaimTypes.Role, account.VaiTro.ToString())
            };
            if (!string.IsNullOrEmpty(response.MaNhanVien)) claims.Add(new Claim(AppClaimTypes.MaNhanVien, response.MaNhanVien));
            if (!string.IsNullOrEmpty(response.MaKhachHang)) claims.Add(new Claim(AppClaimTypes.MaKhachHang, response.MaKhachHang));

            var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiresMinutes);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            response.AccessToken = new JwtSecurityTokenHandler().WriteToken(token);
            response.ExpiresAt = expiresAt;
            return response;
        }
    }
}
