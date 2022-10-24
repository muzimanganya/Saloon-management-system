using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Saloon.Models;
using Saloon.Helpers;
using System.Data.SqlClient;

namespace Saloon.Controllers
{
     [AuthFilter]
    public class PaymentsController : Controller
    {
        private Connection db = new Connection();

        // GET: Payments
        public ActionResult Index()
        {
            var payments = db.Payments.Include(p => p.User).OrderByDescending(u=>u.PayDate);
            return View(payments.ToList());
        } 
          
        public ActionResult Create()
        { 
            String date = DateTime.Now.ToString("dd/M/yyyy");
            date = date.Replace("-", "/");

            db.PaySalaries(date, SecurityHelper.GetLoginId().ToString());
            return RedirectToAction("Index");
        } 
         

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
