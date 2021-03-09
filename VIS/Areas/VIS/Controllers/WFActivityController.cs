﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VAdvantage.Utility;
using VIS.Filters;
using VIS.Models;

namespace VIS.Controllers
{
    [AjaxValidateAntiForgeryToken] // validate antiforgery token 
    public class WFActivityController : Controller
    {
        [AjaxAuthorizeAttribute]
        [AjaxSessionFilterAttribute]
        public ActionResult Index()
        {
            return View();
        }
        [AjaxAuthorizeAttribute]
        [AjaxSessionFilterAttribute]
        public JsonResult GetActivities(int pageNo, int pageSize, bool refresh, string searchText, int VAF_Screen_ID, DateTime? dateFrom, DateTime? dateTo,int VAF_Node_ID)
        {
            WFActivityModel model = new WFActivityModel();
            Ctx ctx = Session["ctx"] as Ctx;
            return Json(new { result = model.GetActivities(ctx, ctx.GetVAF_UserContact_ID(), ctx.GetVAF_Client_ID(), pageNo, pageSize, refresh, searchText, VAF_Screen_ID, dateFrom, dateTo, VAF_Node_ID) }, JsonRequestBehavior.AllowGet);
        }
        [AjaxAuthorizeAttribute]
        [AjaxSessionFilterAttribute]
        public JsonResult GetActivityInfo(int activityID, int nodeID, int wfProcessID)
        {
            WFActivityModel model = new WFActivityModel();
            Ctx ctx = Session["ctx"] as Ctx;
            return Json(new { result = model.GetActivityInfo(activityID, nodeID, wfProcessID, ctx) }, JsonRequestBehavior.AllowGet);
        }
        /// <summary>
        /// This function will approve, or forward workfow activies.
        /// </summary>
        /// <param name="activityID"> Can containes one activity ID or more than one comma separated activity IDs</param>
        /// <param name="nodeID">Will Contain Node ID</param>
        /// <param name="txtMsg">Any Text message written while approving or forwarding activity</param>
        /// <param name="fwd"> VAF_UserContact_ID of users to whome workflow is forwarded</param>
        /// <param name="answer"></param>
        /// <returns></returns>
        [AjaxAuthorizeAttribute]
        [AjaxSessionFilterAttribute]
        public JsonResult ApproveIt(string activityID, int nodeID, string txtMsg, object fwd, object answer, int VAF_Screen_ID)
        {
            WFActivityModel model = new WFActivityModel();
            Ctx ctx = Session["ctx"] as Ctx;
            return Json(new { result = model.ApproveIt(nodeID, activityID, txtMsg, fwd, answer, ctx, VAF_Screen_ID) }, JsonRequestBehavior.AllowGet);
        }
        [AjaxAuthorizeAttribute]
        [AjaxSessionFilterAttribute]
        public JsonResult GetRelativeData(int activityID)
        {
            WFActivityModel model = new WFActivityModel();
            Ctx ctx = Session["ctx"] as Ctx;
            return Json(new { result = model.GetRelativeData(ctx, activityID) }, JsonRequestBehavior.AllowGet);
        }

        [AjaxAuthorizeAttribute]
        [AjaxSessionFilterAttribute]
        [HttpPost]
        public JsonResult GetWorkflowWindows()
        {
            WFActivityModel model = new WFActivityModel();
            Ctx ctx = Session["ctx"] as Ctx;
            return Json(JsonConvert.SerializeObject(model.GetWorkflowWindows(ctx)), JsonRequestBehavior.AllowGet);
        }

    }
}