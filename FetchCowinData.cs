using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Cowin.UserData
{
     public class Session
    {
        public string session_id { get; set; }
        public string date { get; set; }
        public int available_capacity { get; set; }
        public int min_age_limit { get; set; }
        public string vaccine { get; set; }
        public List<string> slots { get; set; }
        public int available_capacity_dose1 { get; set; }
        public int available_capacity_dose2 { get; set; }
        
    }

    public class VaccineFee
    {
        public string vaccine { get; set; }
        public string fee { get; set; }
    }

    public class Center
    {
        public int center_id { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string state_name { get; set; }
        public string district_name { get; set; }
        public string block_name { get; set; }
        public int pincode { get; set; }
        public int lat { get; set; }
        public int @long { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string fee_type { get; set; }
        public List<Session> sessions { get; set; }
        public List<VaccineFee> vaccine_fees { get; set; }
    }

    public class Root
    {
        public List<Center> centers { get; set; }
    }
    public static class FetchCowinData
    {
        [FunctionName("FetchCowinData")]
        public static void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"FetchCowinData Timer trigger function executed at: {DateTime.Now}");
            CallCowinAPI(log);
            log.LogInformation($"FetchCowinData Timer trigger function execution Done");
        }

        public static async void CallCowinAPI(ILogger log)
        {
            // https://cdn-api.co-vin.in/api/v2/admin/location/districts/21
            // dotnet add package System.Collections --version 4.3.0
            // dotnet add package System.Net.Http --version 4.3.4  
            // dotnet add package  SendGrid
            
            StringBuilder _MailContent = new StringBuilder();

            string CowinURL = System.Environment.GetEnvironmentVariable("CowinURL");
            string CowinURL_Loop ="";
            
            string districtValues = System.Environment.GetEnvironmentVariable("district_id");
            string[] district_id = districtValues.Split(',');
            string _districtCode="";
            
            string _CurrentDt=System.DateTime.Now.ToString("dd-MM-yyyy"); 
            CowinURL=CowinURL.Replace("E_Date",_CurrentDt);

            bool _SendMail =false;

    _MailContent.Append("<html><head></head><body>");   
    _MailContent.Append("<p><a href='https://selfregistration.cowin.gov.in/' target='_blank'>Register - Cowin</a></p>");   
    for (int _dcount=0;_dcount<=2;_dcount++)
    {
        CowinURL_Loop="";
        CowinURL_Loop=CowinURL;

        _districtCode=district_id[_dcount];
        CowinURL_Loop=CowinURL_Loop.Replace("E_DistrictID",_districtCode);

        var client = new HttpClient();
        var response = await client.GetAsync(CowinURL_Loop);
        var content = await response.Content.ReadAsStringAsync();
        Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(content); 
        
        // (a) Centers
        int _CentersCount=myDeserializedClass.centers.Count;
        
        for (int i=0;i<=_CentersCount-1;i++)
        {
            //(b) Sessions
            int _SessionsCount=myDeserializedClass.centers[i].sessions.Count;
            for (int j=0;j<=_SessionsCount-1;j++)
            {
                var _sessionsVal=myDeserializedClass.centers[i].sessions[j];
                //(c) Available Session Details
                if(_sessionsVal.available_capacity > 0 && _sessionsVal.min_age_limit == 18 && myDeserializedClass.centers[i].sessions[j].available_capacity_dose2 >0)//
                {
                    _SendMail=true;    
                    int _SlotCount=myDeserializedClass.centers[i].sessions[j].slots.Count ;

                    _MailContent.Append("<tr style='background-color: lightblue;'>");   
                    _MailContent.Append("<td colspan=2 style='border: 1px solid black;'>District Name (Code) : "+myDeserializedClass.centers[i].district_name + " - "+_districtCode+"</td></tr>");   
                
                    _MailContent.Append("<tr><td style='padding: 4px;border: 1px solid black;'>Center Name : </td>");
                    _MailContent.Append("<td style='padding: 4px;border: 1px solid black;'>"+myDeserializedClass.centers[i].name+"</td></tr>");

                    _MailContent.Append("<tr><td style='padding: 4px;border: 1px solid black;'>Center Address : </td>");
                    _MailContent.Append("<td style='padding: 4px;border: 1px solid black;'>"+myDeserializedClass.centers[i].address+"</td></tr>");
                    
                    _MailContent.Append("<tr><td style='padding: 4px;border: 1px solid black;'>Vaccine : </td>");
                    _MailContent.Append("<td style='padding: 4px;border: 1px solid black;'>"+myDeserializedClass.centers[i].sessions[j].vaccine+"</td></tr>");
                    
                    _MailContent.Append("<tr><td style='padding: 4px;border: 1px solid black;'>Session Date : </td>");
                    _MailContent.Append("<td style='padding: 4px;border: 1px solid black;'>"+myDeserializedClass.centers[i].sessions[j].date+"</td></tr>");

                    _MailContent.Append("<tr><td style='padding: 4px;border: 1px solid black;'>Session Available Capacity : </td>");
                    _MailContent.Append("<td style='padding: 4px;border: 1px solid black;'>"+myDeserializedClass.centers[i].sessions[j].available_capacity+"</td></tr>");
                    
                    //_MailContent.Append("<tr><td style='padding: 4px;border: 1px solid black;'>Available Capacity Dose 1 : </td>");
                    //_MailContent.Append("<td style='padding: 4px;border: 1px solid black;'>"+myDeserializedClass.centers[i].sessions[j].available_capacity_dose1+"</td></tr>");
                    
                    _MailContent.Append("<tr><td style='padding: 4px;border: 1px solid black;'>Available Capacity Dose 2 : </td>");
                    _MailContent.Append("<td style='padding: 4px;border: 1px solid black;'>"+myDeserializedClass.centers[i].sessions[j].available_capacity_dose2+"</td></tr>");

                    _MailContent.Append("<tr><td style='padding: 4px;border: 1px solid black;'>Slots Count : </td>");
                    _MailContent.Append("<td style='padding: 4px;border: 1px solid black;'>"+_SlotCount+"</td></tr>");
                    
                    
                    //(d) Slot Details
                    for (int s=0;s<=_SlotCount-1;s++)
                    {
                        _MailContent.Append("<tr><td style='padding: 4px;border: 1px solid black;'>"+"("+(s+1)+")"+" Slot Details : </td>");
                        _MailContent.Append("<td style='padding: 4px;border: 1px solid black;'>"+myDeserializedClass.centers[i].sessions[j].slots[s]+"</td></tr>"); 
                    }
                      
                }
            }
        }
    }
             
             _MailContent.Append("</table></div><p><a href='https://selfregistration.cowin.gov.in/' target='_blank'>Register - Cowin</a></p></body></html>");   

            if(_SendMail)
           {
                log.LogInformation($"FetchCowinData Mail Sent");
                string _ApiKey = System.Environment.GetEnvironmentVariable("SENDGRID_KEY");
                string _FromEmailID = System.Environment.GetEnvironmentVariable("FromEmail");
                string _ToEmailID = System.Environment.GetEnvironmentVariable("ToEmail");
                var client_mail = new SendGridClient(_ApiKey);
                var from = new EmailAddress(_FromEmailID, "Elston");
                var subject = "Cowin Data Found";
                var to = new EmailAddress(_ToEmailID, "Elston");
                var plainTextContent = "";
                var htmlContent = _MailContent.ToString ();
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response_mail = await client_mail.SendEmailAsync(msg);     
           }    
          
        }

    }
}
