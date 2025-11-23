using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Journal.Databases.Identity;

public class SeedFactory
{
    public async Task SeedAdmins(IdentityContext context)
    {
        var existsInIdentity = await context.Users
            .AnyAsync(u => u.Id == "fdfa4136-ada3-41dc-b16e-8fd9556d4574"
                        || u.Email == "systemtester@journal.com");

        if (existsInIdentity)
        {
            Console.WriteLine("SystemTester already exists in Identity database. Skipping...");
            return;
        }

        var id = "fdfa4136-ada3-41dc-b16e-8fd9556d4574";
        var password = "NewPassword@1";

        var testAdmin = new IdentityUser()
        {
            Id = id,
            UserName = "systemtester",
            Email = "systemtester@journal.com",
            NormalizedEmail = "SYSTEMTESTER@JOURNAL.COM",
            EmailConfirmed = true,
            PhoneNumber = "0564330462",
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        var hasher = new PasswordHasher<IdentityUser>();
        testAdmin.PasswordHash = hasher.HashPassword(testAdmin, password);

        context.Users.Add(testAdmin);
        await context.SaveChangesAsync();
    }
}
