namespace WebApplication.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;
 
    /// <summary>
    /// This controller will receive the skype messages and handle them to the EchoBot service. 
    /// </summary>
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        
        IMemoryCache memoryCache;
 
        
        BotCredentials botCredentials;
 
        
        public MessagesController(IMemoryCache memoryCache, IOptions<BotCredentials> botCredentials)
        {
            this.memoryCache = memoryCache;
            this.botCredentials = botCredentials.Value;
        }
 
        
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody] dynamic activity)
        {
            var conversationId = activity.from.id.ToString();
 
             
            string token = await this.GetBotApiToken();
            using (var client = new HttpClient())
            {
                
                dynamic message = new ExpandoObject();
                message.type = "message/text";
                message.text = activity.text;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PostAsJsonAsync<ExpandoObject>(
                    $"https://api.skype.net/v3/conversations/{conversationId}/activities",
                    message as ExpandoObject);
            }
 
            return Created(Url.Content("~/"), string.Empty);
        }
        private async Task<string> GetBotApiToken()
        {
            
            string token = memoryCache.Get("token")?.ToString();
            if (string.IsNullOrEmpty(token))
            {
                
                using (var client = new HttpClient())
                {
                    
                    var parameters = new Dictionary<string, string>
                    {
                        {"client_id", this.botCredentials.ClientId },
                        {"client_secret", this.botCredentials.ClientSecret },
                        {"scope", "https://graph.microsoft.com/.default" },
                        {"grant_type", "client_credentials" }
                    };
                    var content = new FormUrlEncodedContent(parameters);
                    var response = await client.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", content);
                    var tokenResponse = await response.Content.ReadAsAsync<TokenResponse>();
 
                    token = tokenResponse.access_token;
                    memoryCache.Set(
                        "token",
                        token,
                        new DateTimeOffset(DateTime.Now.AddMinutes(15)));
                }
            }
 
            return token;
        }
    }
}