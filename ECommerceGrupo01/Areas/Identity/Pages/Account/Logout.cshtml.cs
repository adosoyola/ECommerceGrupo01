// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ECommerce.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            

            // Log: qué había antes
    var before = HttpContext.Session.GetString("CartSession"); // <-- usa tu key real aquí
    _logger.LogInformation("Cart before logout: {Cart}", before ?? "<null>");

    await _signInManager.SignOutAsync();

    // Elimina la clave exacta que usas para guardar el carrito
    HttpContext.Session.Remove("CartSession"); // <-- o la key real
    // o para borrar todo:
    HttpContext.Session.Clear();

    var after = HttpContext.Session.GetString("CartSession");
    _logger.LogInformation("Cart after logout: {Cart}", after ?? "<null>");

    
            _logger.LogInformation("User logged out.");
           

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}
