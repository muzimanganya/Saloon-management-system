using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Saloon.Helpers
{
    public class SecurityHelper
    { 
        internal static string SHA256(string password)
        {
            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
            System.Text.StringBuilder hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password), 0, Encoding.UTF8.GetByteCount(password));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        internal static int GetLoginId()
        {
            int id = 0;
            var context = HttpContext.Current;
            if (context.Session["ID"] != null)
            {
                id = Int32.Parse(context.Session["ID"].ToString()); 
            }

            return id;
        }
    }
}