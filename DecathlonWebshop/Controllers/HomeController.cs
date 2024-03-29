﻿using DecathlonWebshop.Contracts;
using DecathlonWebshop.Models;
using DecathlonWebshop.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DecathlonWebshop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepository;
        private readonly IStringLocalizer<HomeController> _stringLocalizer;

        public HomeController(ILogger<HomeController> logger, IProductRepository productRepository, IStringLocalizer<HomeController> stringLocalizer)
        {
            _logger = logger;
            _productRepository = productRepository;
            _stringLocalizer = stringLocalizer;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.PageTitle = _stringLocalizer["TXT_PageTitle"];
                        
            var homeViewModel = new HomeViewModel
            {
                ProductsOfTheWeek = await _productRepository.GetProductsOfTheWeekAsync()
            };
            return View(homeViewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }
    }
}
