using Clinic.Data;
using Clinic.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Clinic.models;

namespace Clinic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private USER_PKG user_pkg { set; get; }
        private AuthHelper authHelper;
        public UsersController(USER_PKG user_pkg,AuthHelper authHelper) { 
          this.user_pkg = user_pkg;
          this.authHelper = authHelper;
        }

        [HttpGet("my-profile/{id}")]
        [Authorize]
        async public Task<IActionResult> MyProfile(int id)
        {
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var verifyToken = authHelper.VerifyJWTToken(accessToken);
            if(id!= verifyToken.userId)
            {
                return BadRequest("Something went wrong");
            }

            User user = user_pkg.FindUser(id);

            return Ok(user);
        }
    }
}
