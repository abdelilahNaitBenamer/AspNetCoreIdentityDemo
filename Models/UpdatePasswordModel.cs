using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UsersManagement.Models
{
    public class UpdatePasswordModel
    {
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Token { get; set; }
        public string Email { get; set; }
    }
}