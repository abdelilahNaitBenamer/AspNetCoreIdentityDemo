using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UsersManagement.Models;

namespace UsersManagement.Services
{
    public interface ITokenBuilder
    {
        string CreateToken(ApplicationUser user);
    }
}
