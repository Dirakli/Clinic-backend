using Clinic.Data;
using Clinic.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Clinic.models;
using System.Text.RegularExpressions;
namespace Clinic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private AUTH_PKG auth_pkg;
        private AuthHelper authHelper;
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration, AUTH_PKG auth_pkg, AuthHelper authHelper)
        {
            this._configuration = configuration;
            this.auth_pkg = auth_pkg;
            this.authHelper = authHelper;
        }

        [HttpPost]
        [Authorize]
        async public Task<IActionResult> RegisterDoctor([FromForm] RegisterRequest request) {
            var access_token = HttpContext.Request.Headers["Authorization"].ToString()?.Substring("Bearer ".Length).Trim();
            try
            {
                var verifiedToken = authHelper.VerifyJWTToken(access_token);
               
                if (verifiedToken.role != "admin") {
                    return BadRequest("You don't permission of making this request");
                }

                User findUser = auth_pkg.FindUser(request.email);
                if (findUser != null)
                {
                    return StatusCode(401, "This account is already created");
                }

                var regexPattern = @"^(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%^&*(),.?""':;{}|<>]).{8,16}$";
                if (!Regex.IsMatch(request.password, regexPattern))
                {
                    return BadRequest("Password not valid");
                }
                if (request.name.Length <= 5)
                {
                    return BadRequest("Name not valid");
                }
                if (request.role != "doctor")
                {
                    return BadRequest("Something went wrong");
                }

                string photoFileName = null;
                string resumeFileName = null;
                string PhotoAddress = _configuration["FolderAddress:PhotoAddress"];
                string ResumeAddress = _configuration["FolderAddress:ResumeAddress"];

                if (request.photo != null && request.photo.Length > 0)
                {
                    var uniquePhotoName = Guid.NewGuid().ToString();
                    var photoExtension = Path.GetExtension(request.photo.FileName);
                    photoFileName = $"user_photo_{uniquePhotoName}{photoExtension}";

                    var photoPath = Path.Combine(PhotoAddress, photoFileName);
                    Directory.CreateDirectory(PhotoAddress); // Ensure directory exists
                    using (var stream = new FileStream(photoPath, FileMode.Create))
                    {
                        await request.photo.CopyToAsync(stream);
                    }

                }
                else
                {
                    return BadRequest("Photo is required");
                }

                if (request.resume != null && request.resume.Length > 0)
                {
                    var uniqueResumeName = Guid.NewGuid().ToString();
                    var resumeExtension = Path.GetExtension(request.resume.FileName);
                    resumeFileName = $"user_resume_{uniqueResumeName}{resumeExtension}";

                    var resumePath = Path.Combine(ResumeAddress, resumeFileName);
                    Directory.CreateDirectory(ResumeAddress);
                    using (var stream = new FileStream(resumePath, FileMode.Create))
                    {
                        await request.resume.CopyToAsync(stream);
                    }

                    // Set the resume filename in the user object

                }
                else
                {
                    return BadRequest("resume is required");
                }

                User newUser = new User
                {
                    name = request.name,
                    surname = request.surname,
                    email = request.email,
                    password = request.password,
                    role = request.role,
                    private_number = request.private_number,
                    category = request.category,
                    photo = photoFileName,
                    resume = resumeFileName
                };

                User createUser = auth_pkg.AddUser(newUser);

                return StatusCode(200, new {success=true, user= createUser });
            }
            catch (Exception ex) {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
