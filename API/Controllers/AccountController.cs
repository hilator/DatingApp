using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace API.Controllers
{
    public class AccountController:BaseApiController
    {
        private readonly DataContext _dataContext;
        private readonly ITokenService _tokenService ;
        public AccountController(DataContext dataContext,ITokenService tokenService)
        {
            _tokenService = tokenService;
            _dataContext = dataContext;
            
        }
        [HttpPost("register")] //account/register
        public async Task <ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if(await UserExist(registerDto.Username))return BadRequest("UserName is taken");
            using var hmac = new HMACSHA512();
            var user = new AppUser
            {
                UserName = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSolt =hmac.Key
        
            };
            _dataContext.Users.Add(user);
            await _dataContext.SaveChangesAsync();
            return new UserDto{
                Username = user.UserName,
                Token =_tokenService.CreateToken(user)

            };
        }
             [HttpPost("register")] //account/register
        public async Task <ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _dataContext.Users.SingleOrDefaultAsync
                       (x=>x.UserName ==loginDto.Username); 
            if(user==null)return Unauthorized("invalid username");
            using var hmac = new HMACSHA512(user.PasswordSolt);
            var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
            for(int i =0;i<computeHash.Length;i++)
            {
                    if(computeHash[i]!=user.PasswordHash[i])
                        return Unauthorized("invalid password");

            }
                   return new UserDto{
                Username = user.UserName,
                Token =_tokenService.CreateToken(user)

            };
        }
        
        private async Task <bool> UserExist(string username)
        {
            return await _dataContext.Users.AnyAsync(x=>x.UserName ==username.ToLower() );
        }
    }
}