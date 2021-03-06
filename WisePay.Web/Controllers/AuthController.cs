using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WisePay.Entities;
using WisePay.Web.Auth;
using WisePay.Web.Core.ClientInteraction;
using WisePay.Web.Internals;
using WisePay.Web.Auth.Models;
using System.Linq;
using System.Collections.Generic;
using WisePay.Web.Avatars;
using WisePay.Web.Core.Helpers;
using AutoMapper;
using WisePay.Web.Users.Models;

namespace WisePay.Web.Controllers
{
    [Route("api")]
    public class AuthController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly AuthTokenService _tokenService;
        private readonly AvatarsService _avatarsService;
        private readonly IMapper _mapper;

        public AuthController(
            UserManager<User> userManager,
            AuthTokenService tokenService,
            AvatarsService avatarsService,
            IMapper mapper
        )
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _avatarsService = avatarsService;
            _mapper = mapper;
        }

        [HttpPost("sign_in")]
        public async Task<IActionResult> GenerateToken([FromBody]LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                throw new ApiException(400, "Invalid email", ErrorCode.InvalidCredentials);

            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordCorrect)
                throw new ApiException(400, "Invalid password", ErrorCode.InvalidCredentials);

            var token = await _tokenService.GenerateToken(user);

            var response = new
            {
                access_token = token,
                user = _mapper.Map<CurrentUserViewModel>(user)
            };

            return Json(response);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]RegisterModel registerModel)
        {
            if (registerModel.Password != registerModel.PasswordConfirmation)
                throw new ApiException(400, "Passwords don't match", ErrorCode.InvalidCredentials);

            var newUser = new User()
            {
                Email = registerModel.Email,
                UserName = registerModel.Email
            };

            var result = await _userManager.CreateAsync(newUser, registerModel.Password);
            ErrorResultsHandler.ThrowIfIdentityError(result);

            var response = new
            {
                access_token = await _tokenService.GenerateToken(newUser),
                user = _mapper.Map<CurrentUserViewModel>(newUser)
            };

            return Ok(response);
        }
    }
}
