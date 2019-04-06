using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SOC2GitHubProcedureScheduler
{
    public class GitHub
    {
        public static bool SendTaskToGit(string oAuthToken, string repo, string gituser, SOC2Procedure procedure)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(
                "https://api.github.com/repos/" +
                gituser +
                "/" +
                repo +
                "/issues");
            byte[] bytes = Encoding.UTF8.GetBytes(procedure.Json);

            httpWebRequest.Method = "POST";
            httpWebRequest.ContentLength = bytes.Length;
            httpWebRequest.ContentType = "application/json; encoding='utf-8'";
            httpWebRequest.UserAgent = "Virvent Syslog Server";
            httpWebRequest.Headers.Add("Authorization", string.Format("Token {0}", oAuthToken));

            ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate
            {
                return true;
            };

            var objRequestStream = httpWebRequest.GetRequestStream();
            objRequestStream.Write(bytes, 0, bytes.Length);
            objRequestStream.Close();
            objRequestStream.Dispose();
            try
            {
                var objHttpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                objHttpWebResponse.Close();
                objHttpWebResponse.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
            return true;
        }
    }
}
