using Clinic.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Clinic.Helper;
using Clinic.models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Clinic.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using System;
using System.Text.Json;
using Org.BouncyCastle.Asn1.Ocsp;


namespace Clinic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private AUTH_PKG auth_pkg;
        private AuthHelper authHelper;
        private readonly IConfiguration _configuration;
        private EmailService emailService;
        private IRedisService redisService;

        public AuthController(AUTH_PKG auth, AuthHelper authHelper, IConfiguration configuration,EmailService emailService, IRedisService redisService)
        {
            this.auth_pkg = auth;
            this.authHelper = authHelper;
            _configuration = configuration;
            this.emailService = emailService;
            this.redisService = redisService;
        }

        [HttpPost("sign-up")]
        [Consumes("multipart/form-data")]
        async public Task<IActionResult> Register([FromForm] RegisterRequest request)
        {
            
            try
            {
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
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.password);
                request.password = passwordHash;
                string photoFileName = null;
                string resumeFileName = null;
                string PhotoAddress = _configuration["FolderAddress:PhotoAddress"];
                string ResumeAddress = _configuration["FolderAddress:ResumeAddress"];
                string userPhoto;
                string userResume;
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

                var token = authHelper.GenerateRegisterJWTToken(newUser);

                await emailService.SendEmailAsync(request.email, token, request.name);

                //User createUser = auth_pkg.AddUser(newUser);
                return StatusCode(200, new { user = "Check your mail" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }


        [HttpPost("sign-in")]
        async public Task <IActionResult> SignIn([FromBody] SignIn request)
        {
            try
            {
                User findUser = auth_pkg.FindUser(request.email);
                if (findUser == null)
                {
                    return StatusCode(401, "This mail is not registered");
                }

                bool verified = BCrypt.Net.BCrypt.Verify(request.password, findUser.password);
                if (verified !=true)
                {
                    return Unauthorized("Invalid password.");
                }

                var token = authHelper.GenerateJWTToken(findUser);

                return Ok(new { token,user=findUser, verified });

            }
            catch (Exception ex) {
                return StatusCode(500, ex.Message);
            }
      
        }
        [HttpGet("verify-token")]
        async public Task<IActionResult> VerifyToken([FromQuery] string token )
        {
            User user = authHelper.VerifyRegisterJWTToken(token);

            try
            {
                
                //if(user == null)
                //{
                //    return Unauthorized("Invalid token");
                //}

                User createUser = auth_pkg.AddUser(user);

                var newtoken = authHelper.GenerateJWTToken(createUser);

                return Redirect($"{_configuration["CLIENTURL:URL"]}/social-auth?token={token}&userId={createUser.id}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("forget-password")]
        async public Task<IActionResult> ForgetPassword(string email)
        {
            var user = auth_pkg.FindUser(email);
         

            try
            {
                if (user == null)
                {
                    return BadRequest("Email is not valid.");
                }

                var redisDb = redisService.GetDatabase();


                var value = await redisDb.StringGetAsync($"users_Code:{email}");

                if (value.HasValue) 
                {
                    await redisDb.KeyDeleteAsync($"users_Code:{email}"); 
                }

                Random random = new Random();
                int otp = random.Next(1000, 10000);

                var code = new { 
                 code=otp.ToString(),
                 verify=false
                };

                var jsonString = JsonSerializer.Serialize(code);

                await redisDb.StringSetAsync($"users_Code:{email}", jsonString, TimeSpan.FromMinutes(5));
                await emailService.SEndOtp(email, otp.ToString(), user.name);

                return Ok(new { message="Check your Email",email});

            }
            catch (Exception ex) {
                return StatusCode(500, ex.Message);
            }
          
        }

        [HttpPost("verify-code")]
        async public Task<IActionResult> VerifyCode(string email,string otp)
        {
            var redisDb = redisService.GetDatabase();
            try
            {
                
                var value = await redisDb.StringGetAsync($"users_Code:{email}");

                if (value.IsNull)
                {
                    return BadRequest("Time is up, Try again");
                }
                var messageObject = JsonSerializer.Deserialize<Dictionary<string, object>>(value.ToString());

                if (messageObject["code"].ToString() !=otp)
                {
                    return BadRequest("OTP is Incorrect");
                }
                await redisDb.KeyDeleteAsync($"users_Code:{email}");


                var code = new
                {
                    code = otp.ToString(),
                    verify = true
                };

                var jsonString = JsonSerializer.Serialize(code);
                await redisDb.StringSetAsync($"users_Code:{email}", jsonString, TimeSpan.FromMinutes(5));

                return Ok(new { message = "correctly", status=true,email });
            }
            catch (Exception ex) {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("change-password")]
        async public Task<IActionResult> ChangePassword(string email, string password)
        {
            var user = auth_pkg.FindUser(email);
            var redisDb = redisService.GetDatabase();
            try
            {
                if (user == null)
                {
                    return BadRequest("Email is not valid.");
                }

                var value = await redisDb.StringGetAsync($"users_Code:{email}");

                if (value.IsNull)
                {
                    return BadRequest("Time is up, Try again");
                }
                var messageObject = JsonSerializer.Deserialize<Dictionary<string, object>>(value.ToString());

                bool isVerified = messageObject.ContainsKey("verify") && messageObject["verify"] is bool verified && verified;

                if (isVerified)
                {
                    return BadRequest("OTP is Incorrect");
                }
                await redisDb.KeyDeleteAsync($"users_Code:{email}");

                var regexPattern = @"^(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%^&*(),.?""':;{}|<>]).{8,16}$";
                if (!Regex.IsMatch(password, regexPattern))
                {
                    return BadRequest("Password not valid");
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                auth_pkg.ChangePassword(email, passwordHash);


                return Ok(new { message="password changed correctly",status=true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}
