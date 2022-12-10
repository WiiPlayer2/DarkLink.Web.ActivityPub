using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Demo.Server
{
    public class OpenIdContext : IdentityDbContext<IdentityUser>
    {
        public OpenIdContext(DbContextOptions<OpenIdContext> options) : base(options) { }
    }
}
