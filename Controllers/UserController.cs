using System;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using UsersManagement.Models;
using UsersManagement.Services;
using IEmailSender = UsersManagement.Services.IEmailSender;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace UsersManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private ITokenBuilder _tokenBuilder;
        private IEmailSender _emailSender;
        private IConfiguration Configuration { get; }

        public UserController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITokenBuilder tokenBuilder, IEmailSender sender, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenBuilder = tokenBuilder;
            _emailSender = sender;
            Configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<Object> RegisterUser([FromBody]ApplicationUserViewModel model)
        {
            if(model == null)
            {
                return BadRequest(new {
                    ErrorMessage ="Please Enter your informations"
                    });
            }

            ApplicationUser applicationUser = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(applicationUser, model.Password);

            if(result.Succeeded){
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
                var encodedToken = Encoding.UTF8.GetBytes(token);
                var validToken = WebEncoders.Base64UrlEncode(encodedToken);
                var param = new Dictionary<string, string>
                {
                    {"token", validToken },
                    {"email", applicationUser.Email }
                };
                var callbackUrl = QueryHelpers.AddQueryString(Configuration["ConfirmationClientURI"],param);
                await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
            }
            
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]LoginModel model)
        {
            if(model == null)
            {
                return BadRequest(new {
                    ErrorMessage ="Please set your credentials"
                    });
            }

            ApplicationUser dbUser = await _userManager.FindByNameAsync(model.UserName);

            if(dbUser == null)
            {
                return NotFound(new {ErrorMessage = "User not found"});
            }

            if(!dbUser.EmailConfirmed)
            {
                return Unauthorized(new {
                    ErrorMessage ="Please confirm your account"
                    });
            }   
             
            var result = await _userManager.CheckPasswordAsync(dbUser, model.Password);

            if (!result)
            {
                return NotFound(new {ErrorMessage = "Password incorrect."});
            }

            var token = _tokenBuilder.CreateToken(dbUser);

            return Ok(new { Token = token});
        }

        [HttpPost("emailconfirmation")]
        public async Task<Object> ConfirmEmail([FromBody] ConfirmationEmailModele modele)
        {
            if(modele.Email == null || modele.Token == null)
            {
                return BadRequest(new {
                    ErrorMessage ="Please set your email"
                    });
            }

            var user = await _userManager.FindByEmailAsync(modele.Email);

            if (user == null)
                return NotFound("User Not Found");

            var decodedToken = WebEncoders.Base64UrlDecode(modele.Token);
            string normalToken = Encoding.UTF8.GetString(decodedToken);
            var result = await _userManager.ConfirmEmailAsync(user, normalToken);

            if (result.Succeeded)
            {
                return Ok(result);
            }

            return BadRequest("Your account does not confirmed succesfully");
        }

        [HttpGet("profil")]
        [Authorize]
        public async Task<Object> GetUserProfil()
        {
            string userId = User.Claims.First(c => c.Type == "UserID").Value;
            ApplicationUser user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound("User Not Found");

            return new {
                user.UserName,
                user.FullName,
                user.Email
            };
        }

        [HttpPut("profil")]
        [Authorize]
        public async Task<Object> EditUserProfil([FromBody]ApplicationUser entity)
        {
            string userId = User.Claims.First(c => c.Type == "UserID").Value;

            if(entity == null){
                return BadRequest("Invalid Request");
            }

            ApplicationUser user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound("User Not Found");

            user.FullName = entity.FullName;
            var result = await _userManager.UpdateAsync(user);

            if(result.Succeeded){
                return Ok(user);
            }

            return BadRequest(new {ErrorMessage="Error occurred during updating the user"});
        }

        [HttpPost("forgetpassword")]
        public async Task<Object> ForgetPassword([FromBody]ResettingPasswordModel model)
        {   
            if(model == null)
            {
                return BadRequest(new {
                    ErrorMessage ="Please set your mail"
                    });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if(user == null)
                return NotFound(new { ErrorMessage = "No user associated with email"});

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Encoding.UTF8.GetBytes(token);
            var validToken = WebEncoders.Base64UrlEncode(encodedToken);
            var param = new Dictionary<string, string>
            {
                {"token", validToken },
                {"email", user.Email }
            };
            var callbackUrl = QueryHelpers.AddQueryString(Configuration["ResettingPasswordClientURI"],param);
            await _emailSender.SendEmailAsync(user.Email, "Reset Your Password",
                   $"To reset your password <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>click here</a>.");

            return Ok(new {SuccessMessage = "Reset password URL has been sent to the email successfully!"});
        }

        [HttpPost("resetpassword")]
        public async Task<Object> ResetPassword([FromBody]UpdatePasswordModel passwordModel)
        {
            if(passwordModel == null)
            {
                return BadRequest(new {
                    ErrorMessage ="Please set your informations"
                    });
            }

            if (passwordModel.Password != passwordModel.ConfirmPassword)
                return BadRequest(new {ErrorMessage = "Password doesn't match its confirmation"});

            var user = await _userManager.FindByEmailAsync(passwordModel.Email);

            if (user == null)
                return NotFound(new {ErrorMessage = "No user associated with email"});

            var decodedToken = WebEncoders.Base64UrlDecode(passwordModel.Token);
            string normalToken = Encoding.UTF8.GetString(decodedToken);

            var result = await _userManager.ResetPasswordAsync(user,normalToken,passwordModel.Password);

            if (result.Succeeded)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new {Errors = result.Errors.Select(e => e.Description)});
            }
        }
    }
}