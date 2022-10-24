using Saloon.Helpers;
using Saloon.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Configuration;
using System.Web.Mvc;
using PagedList;

namespace Saloon.Controllers
{
    [AuthFilter]
    public class SaloonsController : Controller
    {
        private Connection db = new Connection();
        private int total = 0;
        private int promotions = 0;
        private int pageSize = 30;

        // GET: Sales
        public ActionResult Sales(int id, int? page)
        {
            var transactions = db.Transactions.Include(t => t.Customer1).
                Include(t => t.Saloon1).Include(t => t.Service1).Include(t => t.User).
                Include(t => t.User1).Include(t => t.User2).
                Where(t => t.Saloon == id && t.CreatedOn.Day == DateTime.Now.Day && t.CreatedOn.Month == DateTime.Now.Month && t.CreatedOn.Year == DateTime.Now.Year).OrderByDescending(x => x.CreatedOn);
            
            CalculateSum(transactions.ToList()); 

            ViewBag.saloon = id;
            ViewBag.promotions = promotions;
            ViewBag.total = total;
             
            int pageNumber = (page ?? 1);
            return View(transactions.ToPagedList(pageNumber, pageSize)); 
        }

        // GET: Sales
        public ActionResult Month(int id, int saloon, int? page)
        {
            var transactions = db.Transactions.Include(t => t.Customer1).
                Include(t => t.Saloon1).Include(t => t.Service1).Include(t => t.User).
                Include(t => t.User1).Include(t => t.User2).
                Where(t => t.Saloon == saloon && t.CreatedOn.Month == id && t.CreatedOn.Year == DateTime.Now.Year).OrderByDescending(x => x.CreatedOn);

            ViewBag.saloon = saloon;

            CalculateSum(transactions.ToList());

            ViewBag.promotions = promotions;
            ViewBag.total = total;

            int pageNumber = (page ?? 1);
            return View("~/Views/Saloons/Sales.cshtml", transactions.ToPagedList(pageNumber, pageSize));  
        }

        // GET: Saloons
        public ActionResult Index()
        {
            var saloons = db.Saloons.
                OrderByDescending(x => x.CreatedOn).
                Include(s => s.User).
                Include(s => s.User1).ToList();
            return View(saloons);
        }

        // GET: Saloons
        public ActionResult SearchTicket(string searchString)
        {
            var transactions = db.Transactions.Include(t => t.Customer1).
            Include(t => t.Saloon1).Include(t => t.Service1).Include(t => t.User).
            Include(t => t.User1).Include(t => t.User2).
            Where(t => t.ID.ToString().Contains(searchString)).OrderByDescending(x => x.CreatedOn);

            return View(transactions);
        }


        // GET: Saloons/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Saloon.Models.Saloon saloon = db.Saloons.Find(id);
            if (saloon == null)
            {
                return HttpNotFound();
            }
            return View(saloon);
        }

        // GET: Saloons/Create
        public ActionResult Create()
        { 
            return View();
        }

        // POST: Saloons/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,Name,Location,CreatedBy,UpdatedBy")] Saloon.Models.Saloon saloon)
        {
            if (ModelState.IsValid)
            { 
                saloon.CreatedOn = DateTime.Now;
                saloon.UpdatedOn = DateTime.Now;
                saloon.CreatedBy = SecurityHelper.GetLoginId();
                saloon.UpdatedBy = SecurityHelper.GetLoginId(); 

                db.Saloons.Add(saloon);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
             
            return View(saloon);
        }

        // GET: Saloons/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Saloon.Models.Saloon saloon = db.Saloons.Find(id);
            if (saloon == null)
            {
                return HttpNotFound();
            } 
            return View(saloon);
        }

        // POST: Saloons/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,Name,Location,CreatedBy,UpdatedBy")] Saloon.Models.Saloon saloon)
        {
            if (ModelState.IsValid)
            {

                var saloonFromDb = db.Saloons.Where(s => s.ID == saloon.ID).First();
                saloonFromDb.Name = saloon.Name;
                saloonFromDb.Location = saloon.Location;
                saloonFromDb.UpdatedOn = DateTime.Now;
                saloonFromDb.UpdatedBy = SecurityHelper.GetLoginId();

                db.Entry(saloonFromDb).State = EntityState.Modified;
                db.SaveChanges(); 
                return RedirectToAction("Index");
            }
            ViewBag.CreatedBy = new SelectList(db.Users, "ID", "Name", saloon.CreatedBy);
            ViewBag.UpdatedBy = new SelectList(db.Users, "ID", "Name", saloon.UpdatedBy);
            return View(saloon);
        }

        // GET: Saloons/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Saloon.Models.Saloon saloon = db.Saloons.Find(id);
            if (saloon == null)
            {
                return HttpNotFound();
            }
            return View(saloon);
        }

        // POST: Saloons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Saloon.Models.Saloon saloon = db.Saloons.Find(id);
            db.Saloons.Remove(saloon);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public JsonResult GetSaleStats()
        {
            List<List<Dictionary<string, string>>> l = new List<List<Dictionary<string, string>>>();

            var builder = new EntityConnectionStringBuilder(WebConfigurationManager.ConnectionStrings["Connection"].ConnectionString);
            string providerConnectionString = builder.ProviderConnectionString;

            using (SqlConnection connection = new SqlConnection(providerConnectionString))
            {
                String sql = "SELECT sa.Name, ISNULL(SUM(se.Amount) , 0) As Revenue   FROM [Saloon].[dbo].[Transactions] t FULL JOIN [Saloon].[dbo].Saloons sa ON t.Saloon = sa.ID LEFT JOIN [Saloon].[dbo].[Services] se ON t.[Service] = se.ID WHERE CONVERT(DATE, t.CreatedOn) = CONVERT(DATE, GETDATE()) GROUP BY sa.Name";
                connection.Open(); using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader != null)
                        {
                            while (reader.Read())
                            {
                                List<Dictionary<string, string>> revenue = new List<Dictionary<string, string>>();

                                Dictionary<string, string> dk = new Dictionary<string, string>();
                                dk.Add("Name", reader["Name"].ToString());

                                Dictionary<string, string> dv = new Dictionary<string, string>();
                                dv.Add("Value", reader["Revenue"].ToString());

                                revenue.Add(dk);
                                revenue.Add(dv);
                                l.Add(revenue);
                            }
                        }
                    } // reader closed and disposed up here

                } // command disposed here

            } //connection closed and disposed here 
            return Json(l, JsonRequestBehavior.AllowGet);
        }
         

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        protected void CalculateSum(IEnumerable<Transaction> items)
        {
            promotions = 0;
            total = 0;

            foreach (var item in items)
            {
                if (item.IsPromotion == 1)
                {
                    promotions = promotions + item.Service1.Amount;
                }
                else
                {
                    total = total + item.Service1.Amount;
                }
            }
        }
    }
}
