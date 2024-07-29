using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using vnMentor.Resources;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using vnMentor.Data;
using Microsoft.AspNetCore.Http;

namespace vnMentor.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private IWebHostEnvironment Environment;
        private DefaultDBContext db;
        private IConfiguration Configuration;
        public HomeController(IWebHostEnvironment environment, DefaultDBContext _db, IConfiguration _Configuration)
        {
            Environment = environment;
            db = _db;
            Configuration = _Configuration;
        }

        public IActionResult Index()
        {
            //if first time running the web application, run the sql to create tables
            string connection = Configuration.GetConnectionString("DefaultConnection");
            try
            {
                if (string.IsNullOrEmpty(connection))
                {
                    return View("ConfigurationError");
                }
                using (var con = new SqlConnection(connection))
                {
                    using (var cmd = new SqlCommand("select top(1)Id from aspnetusers", con))
                    {
                        cmd.CommandType = CommandType.Text;
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Invalid object name"))
                {
                    try
                    {
                        string fileContents = String.Empty;
                        var path = Path.Combine(this.Environment.WebRootPath, "SQL\\1.0.0.sql");
                        if (System.IO.File.Exists(path))
                        {
                            fileContents = System.IO.File.ReadAllText(path);
                            using (var con = new SqlConnection(connection))
                            {
                                con.Open();
                                foreach (var sql in SplitSqlStatements(fileContents))
                                {
                                    using (var cmd = new SqlCommand(sql, con))
                                    {
                                        cmd.CommandType = CommandType.Text;
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {

                    }
                }
            }

            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("index", "dashboard");
            }
            return View();
        }

        public IActionResult UnauthorizedAccess()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("UnauthorizedTwo");
            }
            return RedirectToAction("UnauthorizedOne");
        }

        public IActionResult UnauthorizedOne()
        {
            ViewBag.Message = Resource.YouDontHavePermissionToAccess;
            return View();
        }
        public IActionResult UnauthorizedTwo()
        {
            ViewBag.Message = Resource.YouDontHavePermissionToAccess;
            return View();
        }

        [AllowAnonymous]
        public IActionResult ChangeLanguage(string lang)
        {
            if (lang != null)
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(lang);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
            }
            else
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
                lang = "en";
            }

            Response.Cookies.Append("Language", lang);

            return Redirect(Request.GetTypedHeaders().Referer.ToString());
        }


        private static IEnumerable<string> SplitSqlStatements(string sqlScript)
        {
            // Make line endings standard to match RegexOptions.Multiline
            sqlScript = Regex.Replace(sqlScript, @"(\r\n|\n\r|\n|\r)", "\n");

            // Split by "GO" statements
            var statements = Regex.Split(
                    sqlScript,
                    @"^[\t ]*GO[\t ]*\d*[\t ]*(?:--.*)?$",
                    RegexOptions.Multiline |
                    RegexOptions.IgnorePatternWhitespace |
                    RegexOptions.IgnoreCase);

            // Remove empties, trim, and return
            return statements
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim(' ', '\n'));
        }

    }
}