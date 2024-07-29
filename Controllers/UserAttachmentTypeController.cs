﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using vnMentor.Models;
using vnMentor.Resources;
using vnMentor.Utils;
using vnMentor.Data;
using System.Threading.Tasks;
using System.Reflection;

namespace vnMentor.Controllers
{
    [Authorize(Roles = "System Admin")]
    public class UserAttachmentTypeController : Controller
    {
        private DefaultDBContext db;
        private Util util;
        private readonly UserManager<AspNetUsers> _userManager;
        private ErrorLoggingService _logger;

        public UserAttachmentTypeController(DefaultDBContext db, Util util, UserManager<AspNetUsers> userManager, ErrorLoggingService logger)
        {
            this.db = db;
            this.util = util;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetPartialViewUserAttachmentType(string sort, string search, int? pg, int? size)
        {
            try
            {
                List<ColumnHeader> headers = new List<ColumnHeader>();
                if (string.IsNullOrEmpty(sort))
                {
                    sort = UserAttachmentTypeListConfig.DefaultSortOrder;
                }
                headers = ListUtil.GetColumnHeaders(UserAttachmentTypeListConfig.DefaultColumnHeaders, sort);
                var list = ReadUserAttachmentTypes();
                string searchMessage = UserAttachmentTypeListConfig.SearchMessage;
                list = UserAttachmentTypeListConfig.PerformSearch(list, search);
                list = UserAttachmentTypeListConfig.PerformSort(list, sort);
                ViewData["CurrentSort"] = sort;
                ViewData["CurrentPage"] = pg ?? 1;
                ViewData["CurrentSearch"] = search;
                int? total = list.Count();
                int? defaultSize = UserAttachmentTypeListConfig.DefaultPageSize;
                size = size == 0 || size == null ? (defaultSize != -1 ? defaultSize : total) : size == -1 ? total : size;
                ViewData["CurrentSize"] = size;
                PaginatedList<GlobalOptionSetViewModel> result = await PaginatedList<GlobalOptionSetViewModel>.CreateAsync(list, pg ?? 1, size.Value, total.Value, headers, searchMessage);
                return PartialView("~/Views/UserAttachmentType/_MainList.cshtml", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{GetType().Name} Controller - {MethodBase.GetCurrentMethod().Name} Method");
            }
            return PartialView("~/Views/Shared/Error.cshtml", null);
        }

        public IQueryable<GlobalOptionSetViewModel> ReadUserAttachmentTypes()
        {
            var result = from t1 in db.GlobalOptionSets.AsNoTracking()
                         where t1.Type == "UserAttachment" && t1.Status == "Active"
                         select new GlobalOptionSetViewModel
                         {
                             Id = t1.Id,
                             Code = t1.Code,
                             DisplayName = t1.DisplayName,
                             OptionOrder = t1.OptionOrder,
                             SystemDefault = t1.SystemDefault
                         };
            return result;
        }

        public GlobalOptionSetViewModel GetViewModel(string Id, string type)
        {
            GlobalOptionSetViewModel model = new GlobalOptionSetViewModel();
            GlobalOptionSet globalOptionSet = db.GlobalOptionSets.Where(a => a.Id == Id).FirstOrDefault();
            model.Id = globalOptionSet.Id;
            model.Code = globalOptionSet.Code;
            model.DisplayName = globalOptionSet.DisplayName;
            model.Status = globalOptionSet.Status;
            model.OptionOrder = globalOptionSet.OptionOrder;
            model.SystemDefault = globalOptionSet.SystemDefault;
            model.IsoUtcCreatedOn = globalOptionSet.IsoUtcCreatedOn;
            model.IsoUtcModifiedOn = globalOptionSet.IsoUtcModifiedOn;
            if (type == "View")
            {
                model.CreatedAndModified = util.GetCreatedAndModified(globalOptionSet.CreatedBy, globalOptionSet.IsoUtcCreatedOn, globalOptionSet.ModifiedBy, globalOptionSet.IsoUtcModifiedOn);
            }
            return model;
        }

        public IActionResult Edit(string Id)
        {
            GlobalOptionSetViewModel model = new GlobalOptionSetViewModel();
            if (Id != null)
            {
                model = GetViewModel(Id, "Edit");
            }
            else
            {
                //display order
                int? maxOrder = db.GlobalOptionSets.Where(a => a.Type == "UserAttachment" && a.Status == "Active").Select(a => a.OptionOrder).OrderByDescending(a => a.Value).FirstOrDefault();
                model.OptionOrder = maxOrder + 1;
            }
            return View(model);
        }

        public IActionResult ViewRecord(string Id)
        {
            GlobalOptionSetViewModel model = new GlobalOptionSetViewModel();
            if (Id != null)
            {
                model = GetViewModel(Id, "View");
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult Edit(GlobalOptionSetViewModel model)
        {
            try
            {
                ValidateModel(model);

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                SaveRecord(model);
                TempData["NotifySuccess"] = Resource.RecordSavedSuccessfully;
            }
            catch (Exception ex)
            {
                TempData["NotifyFailed"] = Resource.FailedExceptionError;
                _logger.LogError(ex, $"{GetType().Name} Controller - {MethodBase.GetCurrentMethod().Name} Method");
            }
            return RedirectToAction("index");
        }

        public void ValidateModel(GlobalOptionSetViewModel model)
        {
            if (model != null)
            {
                bool duplicated = false;
                if (model.Id != null)
                {
                    duplicated = db.GlobalOptionSets.Where(a => a.DisplayName == model.DisplayName && a.Type == "UserAttachment" && a.Id != model.Id).Any();
                }
                else
                {
                    duplicated = db.GlobalOptionSets.Where(a => a.DisplayName == model.DisplayName && a.Type == "UserAttachment").Select(a => a.Id).Any();
                }

                if (duplicated == true)
                {
                    ModelState.AddModelError("DisplayName", Resource.UserAttachmentTypeNameAlreadyExist);
                }
            }
        }

        public void SaveRecord(GlobalOptionSetViewModel model)
        {
            if (model != null)
            {
                Regex sWhitespace = new Regex(@"\s+");
                //edit
                if (model.Id != null)
                {
                    GlobalOptionSet globalOptionSet = db.GlobalOptionSets.Where(a => a.Id == model.Id).FirstOrDefault();
                    globalOptionSet.Code = sWhitespace.Replace(model.DisplayName, "");
                    globalOptionSet.DisplayName = model.DisplayName;
                    globalOptionSet.OptionOrder = model.OptionOrder;
                    globalOptionSet.Type = "UserAttachment";
                    globalOptionSet.Status = "Active";
                    globalOptionSet.ModifiedBy = _userManager.GetUserId(User);
                    globalOptionSet.ModifiedOn = util.GetSystemTimeZoneDateTimeNow();
                    globalOptionSet.IsoUtcModifiedOn = util.GetIsoUtcNow();
                    db.Entry(globalOptionSet).State = EntityState.Modified;
                    db.SaveChanges();
                }
                //new record
                else
                {
                    GlobalOptionSet globalOptionSet = new GlobalOptionSet();
                    globalOptionSet.Id = Guid.NewGuid().ToString();
                    globalOptionSet.Code = sWhitespace.Replace(model.DisplayName, "");
                    globalOptionSet.DisplayName = model.DisplayName;
                    globalOptionSet.OptionOrder = model.OptionOrder;
                    globalOptionSet.Type = "UserAttachment";
                    globalOptionSet.Status = "Active";
                    globalOptionSet.CreatedBy = _userManager.GetUserId(User);
                    globalOptionSet.CreatedOn = util.GetSystemTimeZoneDateTimeNow();
                    globalOptionSet.IsoUtcCreatedOn = util.GetIsoUtcNow();
                    globalOptionSet.SystemDefault = false;
                    db.GlobalOptionSets.Add(globalOptionSet);
                    db.SaveChanges();
                }
            }
        }

        public IActionResult Delete(string Id)
        {
            try
            {
                if (Id != null)
                {
                    GlobalOptionSet globalOptionSet = db.GlobalOptionSets.Where(a => a.Id == Id).FirstOrDefault();
                    if (globalOptionSet != null)
                    {
                        db.GlobalOptionSets.Remove(globalOptionSet);
                        db.SaveChanges();
                    }
                }
                TempData["NotifySuccess"] = Resource.RecordDeletedSuccessfully;
            }
            catch (Exception ex)
            {
                GlobalOptionSet globalOptionSet = db.GlobalOptionSets.Where(a => a.Id == Id).FirstOrDefault();
                if (globalOptionSet == null)
                {
                    TempData["NotifySuccess"] = Resource.RecordDeletedSuccessfully;
                }
                else
                {
                    TempData["NotifyFailed"] = Resource.FailedExceptionError;
                    _logger.LogError(ex, $"{GetType().Name} Controller - {MethodBase.GetCurrentMethod().Name} Method");
                }
            }
            return RedirectToAction("index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (db != null)
                {
                    db.Dispose();

                }
                if (util != null)
                {
                    util.Dispose();

                }
            }

            base.Dispose(disposing);
        }
    }
}