﻿using System;
using System.Threading.Tasks;
using Messaging.Contract.Models;
using Messaging.Contract.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messaging.Api.Controllers
{
    /// <summary>
    /// User management API
    /// </summary>
    [Route("api/{appId}/[controller]")]
    public class UserController : Controller
    {
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor for the dependency injection.
        /// </summary>
        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// Retrieves metadata for user with given <paramref name="email"/>.
        /// </summary>
        /// <param name="appId">Application ID</param>
        /// <param name="email">User's email address</param>
        /// <response code="200">Returns the retrieved user metadata.</response>
        /// <response code="400">If user with given <paramref name="email"/> doesn't exist.</response>
        [Authorize]
        [HttpGet("{email}")]
        [ProducesResponseType(typeof(User), 200)]
        public async Task<IActionResult> Get(Guid appId, string email)
        {
            var user = await _userRepository.Get(appId, email);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        /// <summary>
        /// Registers a new user with given <paramref name="email"/> in specified application.
        /// </summary>
        /// <param name="appId">Application ID</param>
        /// <param name="email">Email to identify the user.</param>
        /// <param name="customData">Custom user metadata</param>
        /// <response code="201">Returns the newly registered user.</response>
        /// <response code="400">User with given email already exists.</response>
        [HttpPost]
        public async Task<IActionResult> Register(Guid appId, [FromBody] string email, [FromBody] string customData)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _userRepository.Get(appId, email);
            if (existing != null)
                return BadRequest("Email already used.");

            var user = new User
            {
                Email = email,
                CustomData = customData
            };
            var result = await _userRepository.Upsert(appId, user);

            return Created($"api/{email}", result);
        }

        /// <summary>
        /// Updates the metadata of the specified user.
        /// </summary>
        /// <param name="appId">Application ID</param>
        /// <param name="email">Email to identify the user.</param>
        /// <param name="customData">Custom user metadata</param>
        /// <response code="200">Returns the updated user.</response>
        /// <response code="403">When attempting to update someone else.</response>
        /// <response code="404">When the specified user doesn't exist.</response>
        [Authorize]
        [HttpPut("{email}")]
        public async Task<IActionResult> Update(Guid appId, string email, [FromBody]string customData)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!String.Equals(email, HttpContext.GetCurrentUserId(), StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var user = await _userRepository.Get(appId, email);
            if (user == null)
                return NotFound();

            user.CustomData = customData;

            var result = await _userRepository.Upsert(appId, user);

            return Ok(result);
        }
    }
}
