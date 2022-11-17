# Making an app from 'scratch' with skeleton in .net 6

## Add Nuget Packages
- NpgSql
- Autofac
- Autofac.Extensions.DependencyInjection
- Serilog
- ImageSharp
- Swashbuckle.AspNetCore
- Microsoft.Identity.Web

## Add npm packages
- `@mui/material`
- `@mui/icons-material`
- `@mui/lab` (used for date picker)
- `formik`
- `query-string`
- `react-router-dom`
- `@azure/msal-react` (for AAD auth)
- `date-fns`
- `debounce` (if you're using search) + types `@types/debounce`
- `@emotion/styled`
- `framer-motion` (because the built-in one in MUI had some problems)
- `react-error-boundary`
- `notistack`

Base Controller
```csharp
    public abstract class ControllerBase : Controller
    {
        protected ActionResult TranslateExceptionToResult(NpgsqlException ex)
        {
            if (ex is PostgresException pgEx)
            {
                if (pgEx.SqlState == "P0001")
                {
                    return BadRequest(pgEx.MessageText);
                }

                if (pgEx.SqlState == "23505")
                {
                    return BadRequest("Cannot add duplicate value");
                }

                if (pgEx.SqlState == "23503")
                {
                    var additionalInfo = string.Empty;

                    if (!string.IsNullOrEmpty(pgEx.ConstraintName))
                    {
                        additionalInfo = pgEx.ConstraintName.Replace("_fkey", "");
                    }

                    return BadRequest($"Related value does not exist. {additionalInfo}");
                }
            }

            return Problem(detail: ex.Message, title: $"Unexpected Error {ex.ErrorCode}");
        }
    }
```

User Service - this relies on a user repository operation called SelectByLogin
```csharp
public interface IUserService
    {
        int? GetUserId(ClaimsPrincipal user);
        
        int SystemUserId { get; }
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public int? GetUserId(ClaimsPrincipal user)
        {
            return GetUserByIdInternal(user, 0);
        }

        public int SystemUserId => 1;

        private int? GetUserByIdInternal(ClaimsPrincipal user, int tryCount)
        {
            try
            {
                if (!user?.Identity?.IsAuthenticated == true)
                {
                    return null;
                }
                
                var userName = GetUserName(user);
                var oid = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                
                var result = _repository.SelectByLogin(userName, oid);
                return result;
            }
            catch (PostgresException pgEx)
            {
                // 23505: duplicate key value violates unique constraint
                // this is a bit horrible because it makes the user service dependent on the repository implementation
                if (pgEx.SqlState == "23505")
                {
                    if (tryCount < 2)
                    {
                        Log.Warning("Concurrency error adding user {UserName} - re-try number {RetryCount}", user.Identity.Name, tryCount);
                        return GetUserByIdInternal(user, tryCount + 1);
                    }
                }

                throw;
            }
        }

        private string GetUserName(ClaimsPrincipal user)
        {
            if (!string.IsNullOrEmpty(user.Identity.Name))
            {
                return user.Identity.Name;
            }

            // user is an application, which doesn't have a name, but should have an app id
            return user.Claims.First(c => c.Type == "appid").Value;
        }
    }
```

Wire up autofac in `program.cs`

```csharp
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(b =>
{
    b.RegisterModule(new RepositoryModule());
    b.RegisterType<UserService>().As<IUserService>();
    b.RegisterType<AttachmentService>();
});
```

add swagger gen (also in `program.cs`)

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

add Roboto font to index.html
```html
<link
rel="stylesheet"
href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap"
/>
```
