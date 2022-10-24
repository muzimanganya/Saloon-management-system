using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Saloon.Models;
using Saloon.Helpers;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Saloon.Controllers
{  
    public class MobileController : ApiController
    {
        private Connection db = new Connection();

        // GET: api/Sell
        public HttpResponseMessage GetServices()
        {
            List<String> l = new List<String>();
            foreach(Service o in db.Services.OrderBy(o => o.type).ToList())
            {
                //l.Add(o.ID.ToString() + "-" + o.type.ToString().Trim() + " (" + o.Amount + ") ");
                l.Add(o.ID.ToString() + "-" + o.type.ToString().Trim());
            } 
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(String.Join("#", l));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            return response;
        }

        // POST: api/Mobile?username=123&password=1234
        [ResponseType(typeof(User))]
        public IHttpActionResult Login(string username, string password)
        {
            String hash = SecurityHelper.SHA256(password);
            db.Configuration.LazyLoadingEnabled = false;

            try
            {
                var user = db.Users.Select(o => new { ID = o.ID, Name = o.Name, Username = o.Username, Password = o.Password }).Where(x => x.Username == username && x.Password == hash).Single();
                return Ok(user);
            }
            catch(Exception e)
            { 
            } 
            
            return Ok(new User());   
        }


        [Route("api/Mobile/Sell")]
        [ResponseType(typeof(JToken))]
        public IHttpActionResult Post([FromBody]JToken json)
        {
            JObject o = (JObject)json;
            JObject response = null;
            Customer customerObj = null;
            int customerNumber = 0;
            int sellerId = 0;
            try
            {
                customerNumber = Int32.Parse(o.GetValue("Customer").ToString());
                sellerId = Int32.Parse(o.GetValue("Seller").ToString());
                //let this be last to give chance to others before it throws exception
                customerObj = db.Customers.Where(x => x.Mobile == customerNumber).Single();
            }
            catch (Exception e)
            {
                try
                {
                    String customerName = o.GetValue("CustomerName").ToString();
                    if (!String.IsNullOrEmpty(customerName))
                    {
                        customerObj = new Customer { Name = customerName, Mobile = customerNumber, CreatedBy = sellerId, UpdatedBy = sellerId, CreatedOn=DateTime.Now, UpdatedOn=DateTime.Now };
                        db.Customers.Add(customerObj);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    customerObj = null;
                }
                String s = "{\"error\":\"1\", \"message\":\"Customer Does not exists.\"}";
                response = JObject.Parse(s);
            }

            if (customerObj == null)
            {
                String s = "{\"error\":\"1\", \"message\":\"Customer Does not exists.\"}";
                response = JObject.Parse(s);
            }
            else
            {
                int cuid = Int32.Parse(o.GetValue("Service").ToString());
                Service serviceObj = db.Services.Where(x => x.ID == cuid).Single();

                int count = db.Transactions
                .Where(t => t.IsUsed == false && t.Customer == customerNumber).AsQueryable().Count();

                int promo = 0;
                if (count >= serviceObj.Promotion)
                    promo = 1;

                Transaction transaction = new Transaction
                {
                    Saloon = Int32.Parse(o.GetValue("Saloon").ToString()),
                    Worker = Int32.Parse(o.GetValue("Worker").ToString()),
                    Customer = customerNumber,
                    Service = serviceObj.ID ,
                    CreatedBy = sellerId,
                    UpdatedBy = sellerId,
                    CreatedOn = DateTime.Now,
                    UpdatedOn = DateTime.Now,
                    IsPromotion = promo
                };
                db.Transactions.Add(transaction);

                try
                {
                    if (db.SaveChanges() > 0)
                    {
                        //reset everything for this customer service if its promo
                        if(transaction.IsPromotion==1)
                        {
                            foreach (var trans in db.Transactions.Where(t=>t.IsUsed==false && t.Service==transaction.Service))
                            {
                                trans.IsUsed = true;
                            }
                            db.SaveChanges();
                        }

                        db.Entry(transaction).Reference(c => c.Service1).Load();
                        int ID = transaction.ID;
                        String saloon = db.Saloons.Where(x => x.ID == transaction.Saloon).Single().Name;
                        String worker = db.Users.Where(x => x.ID == transaction.Worker).Single().Name;
                        String counter = db.Users.Where(x => x.ID == transaction.CreatedBy).Single().Name;

                        String service = serviceObj.type.ToUpper();
                        int amount = serviceObj.Amount;
                        int points = db.Transactions.Where(t => t.IsUsed == false && t.Customer == customerNumber && t.Service == serviceObj.ID).AsQueryable().Count();

                        String s = "{" + String.Format("\"Ticket\":\"{0}\", \"Saloon\":\"{1}\", \"Worker\":\"{2}\", \"Counter\":\"{3}\", \"Service\":\"{4}\", \"Amount\":\"{5}\", \"Customer\":\"{6}\", \"IsPromotion\":\"{7}\", \"Points\":\"{8}\"", ID, saloon, worker, counter, service, amount, customerObj.Name, transaction.IsPromotion, points) + "}";
                        response = JObject.Parse(s);
                    }
                    else
                    {
                        String s = "{\"error\":\"1\", \"message\":\"System Error. Try again.\"}";
                        response = JObject.Parse(s);
                    }
                }
                catch (Exception e)
                {
                    String s = "{\"error\":\"1\", \"message\":\"Invalid Data. Try again.\"}";
                    response = JObject.Parse(s);
                }
            }

            return Ok(response);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool TransactionExists(int id)
        {
            return db.Transactions.Count(e => e.ID == id) > 0;
        }
    }
}