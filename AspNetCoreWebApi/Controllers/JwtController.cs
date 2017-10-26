using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using AspNetCoreWebApi.Models;
using AspNetCoreWebApi.Options;
 

namespace AspNetCoreWebApi.Controllers
{
  [Route("api/[controller]")]
  public class JwtController : Controller
  {
    private readonly JwtIssuerOptions jwtOptions;
    private readonly ILogger logger;
    private readonly JsonSerializerSettings serializerSettings;

    public JwtController(IOptions<JwtIssuerOptions> jwtOptions, ILoggerFactory loggerFactory)
    {
      this.jwtOptions = jwtOptions.Value;
      ThrowIfInvalidOptions(this.jwtOptions);

      this.logger = loggerFactory.CreateLogger<JwtController>();

      this.serializerSettings = new JsonSerializerSettings
      {
        Formatting = Formatting.Indented
      };
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromBody] ApplicationUser applicationUser)
    {
      var identity = await GetClaimsIdentity(applicationUser);
      if (identity == null)
      {
        this.logger.LogInformation($"Invalid username ({applicationUser.Username}) or password ({applicationUser.Password})");
        return Unauthorized();
      }

      var claims = new[]
      {
        new Claim(JwtRegisteredClaimNames.Sub, applicationUser.Username),
        new Claim(JwtRegisteredClaimNames.Jti, await this.jwtOptions.JtiGenerator()),
        new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(this.jwtOptions.IssuedAt).ToString(), ClaimValueTypes.Integer64),
        identity.FindFirst("Role")
      };

      // Create the JWT security token and encode it.
      var jwt = new JwtSecurityToken(
          issuer: this.jwtOptions.Issuer,
          audience: this.jwtOptions.Audience,
          claims: claims,
          notBefore: this.jwtOptions.NotBefore,
          expires: this.jwtOptions.Expiration,
          signingCredentials: this.jwtOptions.SigningCredentials);

      var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

      // Serialize and return the response
      var response = new
      {
        accessToken = encodedJwt
        //,expiresIn = (int)this.jwtOptions.ValidFor.TotalSeconds
      };

      var json = JsonConvert.SerializeObject(response, this.serializerSettings);
      return new OkObjectResult(json);
    }

    private static void ThrowIfInvalidOptions(JwtIssuerOptions options)
    {
      if (options == null) throw new ArgumentNullException(nameof(options));

      if (options.ValidFor <= TimeSpan.Zero)
      {
        throw new ArgumentException("Must be a non-zero TimeSpan.", nameof(JwtIssuerOptions.ValidFor));
      }

      if (options.SigningCredentials == null)
      {
        throw new ArgumentNullException(nameof(JwtIssuerOptions.SigningCredentials));
      }

      if (options.JtiGenerator == null)
      {
        throw new ArgumentNullException(nameof(JwtIssuerOptions.JtiGenerator));
      }
    }

    /// <returns>Date converted to seconds since Unix epoch (Jan 1, 1970, midnight UTC).</returns>
    private static long ToUnixEpochDate(DateTime date)
      => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);

    /// <summary>
    /// IMAGINE BIG RED WARNING SIGNS HERE!
    /// You'd want to retrieve claims through your claims provider
    /// in whatever way suits you, the below is purely for demo purposes!
    /// </summary>
    private static Task<ClaimsIdentity> GetClaimsIdentity(ApplicationUser user)
    {
      if (user.Username == "admin@admin.com" &&
          user.Password == "admin123")
      {
        return Task.FromResult(new ClaimsIdentity(new GenericIdentity(user.Username, "accessToken"),
          new[]
          {
            new Claim("Role", "Admin")
          }));
      }

      if (user.Username == "user@user.com" &&
          user.Password == "user123")
      {
        return Task.FromResult(new ClaimsIdentity(new GenericIdentity(user.Username, "accessToken"),
          new[]
          {
              new Claim("Role","User")
          }));
      }

      // Credentials are invalid, or account doesn't exist
      return Task.FromResult<ClaimsIdentity>(null);
    }
  }
}