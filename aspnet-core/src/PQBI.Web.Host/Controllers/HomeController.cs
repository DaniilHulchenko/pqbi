﻿using Abp.Auditing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PQBI.Configuration;

namespace PQBI.Web.Controllers
{
    public class HomeController : PQBIControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        readonly IConfigurationRoot _appConfiguration;
        
        public HomeController(
            IWebHostEnvironment webHostEnvironment, 
            IAppConfigurationAccessor appConfigurationAccessor)
        {
            _webHostEnvironment = webHostEnvironment;
            _appConfiguration = appConfigurationAccessor.Configuration;
        }

        [DisableAuditing]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Ui");
            //if (_webHostEnvironment.IsDevelopment())
            //{
            //    return RedirectToAction("Index", "Ui");
            //}

            //var homePageUrl = _appConfiguration["App:HomePageUrl"];
            //if (string.IsNullOrEmpty(homePageUrl))
            //{
            //    return RedirectToAction("Index", "Ui");
            //}

            //return Redirect(homePageUrl);
        }
    }
}
