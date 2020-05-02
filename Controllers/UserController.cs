using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using UsersManagement.Models;
using UsersManagement.Services;
using IEmailSender = UsersManagement.Services.IEmailSender;

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

        public UserController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITokenBuilder tokenBuilder, IEmailSender sender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenBuilder = tokenBuilder;
            _emailSender = sender;
        }

        [HttpPost("register")]
        public async Task<Object> RegisterUser([FromBody]ApplicationUserViewModel model)
        {
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
                var callbackUrl = Url.Action(nameof(ConfirmEmail), "User", new { email = applicationUser.Email , validToken }, Request.Scheme);
                await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
            }
            
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]LoginModel model)
        {
            ApplicationUser dbUser = await _userManager.FindByNameAsync(model.UserName);

            if(!dbUser.EmailConfirmed)
            {
                return Unauthorized(new {
                    ErrorMessage ="Please confirm your account"
                    });
            }   
             
            var result = await _userManager.CheckPasswordAsync(dbUser, model.Password);

            if (dbUser == null || !result)
            {
                return NotFound(new {ErrorMessage = "User not found or password incorrect."});
            }

            var token = _tokenBuilder.CreateToken(dbUser);

            return Ok(new { Token = token});
        }

        [HttpGet]
        public async Task<Object> ConfirmEmail(string validToken, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return NotFound("User Not Found");

            var decodedToken = WebEncoders.Base64UrlDecode(validToken);
            string normalToken = Encoding.UTF8.GetString(decodedToken);
            var result = await _userManager.ConfirmEmailAsync(user, normalToken);

            if (result.Succeeded)
            {
                return Ok("Your account is confirmed succesfully");
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

        [HttpPost("forgetpassword")]
        public async Task<Object> ForgetPassword([FromBody]ResettingPasswordModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if(user == null)
                return NotFound(new { ErrorMessage = "No user associated with email"});

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Encoding.UTF8.GetBytes(token);
            var validToken = WebEncoders.Base64UrlEncode(encodedToken);

            var callbackUrl = Url.Action(nameof(ResetPassword), "User", new { email = user.Email, validToken }, Request.Scheme);
            await _emailSender.SendEmailAsync(user.Email, "Reset Password",
                   $"To reset your password <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>click here</a>.");

            return Ok(new {SuccessMessage = "Reset password URL has been sent to the email successfully!"});
        }

        [HttpPost("resetpassword")]
        public async Task<Object> ResetPassword(string email, string validToken, [FromBody]UpdatePasswordModel passwordModel)
        {

            if (passwordModel.Password != passwordModel.ConfirmPassword)
                return BadRequest(new {ErrorMessage = "Password doesn't match its confirmation"});

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return NotFound(new {ErrorMessage = "No user associated with email"});

            var decodedToken = WebEncoders.Base64UrlDecode(validToken);
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