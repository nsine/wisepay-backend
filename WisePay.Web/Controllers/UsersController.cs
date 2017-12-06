﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WisePay.Entities;
using WisePay.Web.Core.ClientInteraction;
using WisePay.Web.Internals;
using WisePay.Web.Teams;
using WisePay.Web.Users;

namespace WisePay.Web.Controllers
{
    [Produces("application/json")]
    [Route("api/users")]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly UsersService _usersService;
        private readonly TeamsService _teamsService;

        public UsersController(
            UsersService usersService,
            TeamsService teamsService,
            UserManager<User> userManager,
            IMapper mapper)
        {
            _userManager = userManager;
            _usersService = usersService;
            _teamsService = teamsService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<UserViewModel>> GetAll(string query)
        {
            var users = await _usersService.GetAllByQuery(query);
            return _mapper.Map<IEnumerable<UserViewModel>>(users);
        }

        [HttpGet("{id}")]
        public async Task<UserViewModel> Get(int id)
        {
            var user = await _usersService.GetById(id);

            if (user == null) throw new ApiException(404, "User not found", ErrorCode.NotFound);

            return _mapper.Map<UserViewModel>(user);
        }

        [HttpGet("me")]
        public async Task<CurrentUserViewModel> GetMe()
        {
            var me = await _userManager.GetUserAsync(User);
            return _mapper.Map<CurrentUserViewModel>(me);
        }

        [HttpGet("me/teams")]
        public async Task<IEnumerable<TeamShortInfoViewModel>> GetMyTeams()
        {
            var me = await _userManager.GetUserAsync(User);
            var teams = await _teamsService.GetUserTeams(me.Id);

            return _mapper.Map<IEnumerable<TeamShortInfoViewModel>>(teams);
        }
    }
}