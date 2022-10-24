using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Saloon.Helpers
{
    public class AuthFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpSessionStateBase  s = filterContext.HttpContext.Session;
            bool isLoggedIn = false;

            if(s!=null)
            {
                if (s["ID"] != null)
                    isLoggedIn = true;
            }

            if (!isLoggedIn)
            {
                //is authenticated
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary{{ "controller", "Home" },
                                        { "action", "Login" }
                });
            }
            else
            {
                //check permissions aka authorization
                if (s["Role"].ToString().Trim() != "admin")
                {
                    if (s["Role"].ToString().Trim() != "User")
                    {
                        filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary{{ "controller", "Home" },
                                        { "action", "Forbidden" }
                    });
                    }
                }
            }
        }
    }
}