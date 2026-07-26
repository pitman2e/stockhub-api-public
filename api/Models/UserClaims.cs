//https://stackoverflow.com/questions/50580232/get-userid-from-jwt-on-all-controller-methods
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StockHub.Errors;
using StockHub.Interfaces;

namespace StockHub.Models;

public class UserClaims : IUserClaims
{
    private readonly IHttpContextAccessor _context;

    public UserClaims (IHttpContextAccessor context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    string IUserClaims.GetUid()
    {
        var httpContext = _context.HttpContext;
        if (httpContext == null)
        {
            throw new SHArgumentException("HttpContext is null (no active HttpContext)");
        }
        return httpContext.User.Claims.FirstOrDefault(i => i.Type == ClaimTypes.NameIdentifier)?.Value ?? "";
    }
}

/*
{
"name": "blahblah",
"picture": "https://lh5.googleusercontent.com/blahblah/photo.jpg",
"iss": "https://securetoken.google.com/stockhub-pm",
"aud": "stockhub-pm",
"auth_time": 1602500665,
"user_id": "6WDeqZuF1QSk3lblahblah",
"sub": "6WDeqZuF1QSk3lblahblah",
"iat": 1603451444,
"exp": 1603455044,
"email": "blahblah@blahblah.com",
"email_verified": true,
"firebase": {
"identities": {
  "google.com": [
    "111111167306182321725"
  ],
  "email": [
    "blahblah.com"
  ]
},
"sign_in_provider": "google.com"
}
}*/