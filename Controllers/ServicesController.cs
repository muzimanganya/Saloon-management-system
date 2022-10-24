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

namespace Saloon.Controllers
{
    [AuthFilter]
    public class ServicesController : Controller
    {
        private Connection db = new Connection();

        // GET: Service
        public ActionResult Index()
        {
            var services = db.Services.Include(s => s.User).Include(s => s.User1);
            return View(services.ToList());
        }

        // GET: Service/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Service service = db.Services.Find(id);
            if (service == null)
            {
                return HttpNotFound();
            }
            return View(service);
        }

        // GET: Service/Create
        public ActionResult Create()
        { 
            return View();
        }

        // POST: Service/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,type,Amount,CreatedBy,UpdatedBy, Promotion")] Service service)
        {
            if (ModelState.IsValid)
            {
                service.CreatedOn = DateTime.Now;
                service.UpdatedOn = DateTime.Now;
                service.CreatedBy = SecurityHelper.GetLoginId();
                service.UpdatedBy = SecurityHelper.GetLoginId(); 

                db.Services.Add(service);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
             
            return View(service);
        }

        // GET: Service/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Service service = db.Services.Find(id);
            if (service == null)
            {
                return HttpNotFound();
            } 
            return View(service);
        }

        // POST: Service/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,type,Amount,UpdatedBy,Promotion")] Service service)
        {
            if (ModelState.IsValid)
            {
                var serviceFromDb = db.Services.Where(s => s.ID == service.ID).First();
                serviceFromDb.type = service.type;
                serviceFromDb.Amount = service.Amount;
                serviceFromDb.Promotion = service.Promotion;
                serviceFromDb.UpdatedOn = DateTime.Now;
                serviceFromDb.UpdatedBy = SecurityHelper.GetLoginId();

                db.Entry(serviceFromDb).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            } 
            return View(service);
        }

        // GET: Service/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Service service = db.Services.Find(id);
            if (service == null)
            {
                return HttpNotFound();
            }
            return View(service);
        }

        // POST: Service/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Service service = db.Services.Find(id);
            db.Services.Remove(service);
            db.SaveChanges();
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
