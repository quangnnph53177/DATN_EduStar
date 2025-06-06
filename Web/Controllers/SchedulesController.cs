using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class SchedulesController : Controller
    {
        private readonly HttpClient _client;
        public SchedulesController(HttpClient client)
        {
            _client = client;
            _client.BaseAddress = _client.BaseAddress = new Uri("https://localhost:7298/");
        }
        public async Task<IActionResult> Index()
        {
            var response =await _client.GetStringAsync("api/Schedules");
            var result = JsonConvert.DeserializeObject<List<SchedulesViewModel>>(response);
            return View(result);
        }
        public async Task<IActionResult> Auto()
        {
            var response = await _client.GetAsync("api/Schedules/arrangeschedules");
            return View();
        }
    }
}
