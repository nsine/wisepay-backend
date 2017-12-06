﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WisePay.Entities;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using WisePay.Web.Auth;
using Microsoft.IdentityModel.Tokens;
using WisePay.Web.Core.ClientInteraction;
using WisePay.Web.Internals;

namespace WisePay.Web.Controllers
{
    [Route("api/sign_in")]
    public class AuthTokenController : Controller
    {
        private UserManager<User> _userManager;
        private AuthTokenService _tokenService;

        public AuthTokenController(UserManager<User> userManager, AuthTokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateToken([FromBody]LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                throw new ApiException(400, "Invalid email", ErrorCode.InvalidCredentials);
            }

            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordCorrect)
            {
                throw new ApiException(400, "Invalid password", ErrorCode.InvalidCredentials);
            }

            var token = await _tokenService.GenerateToken(user);

            var response = new
            {
                access_token = token,
                email = user.Email
            };

            return Json(response);
        }
    }
}