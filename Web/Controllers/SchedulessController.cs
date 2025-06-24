using API.Models;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class SchedulessController : Controller
    {
        private readonly HttpClient _client;
        public SchedulessController(HttpClient client)
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
        public async Task<IActionResult> Auto(Schedule sc)
        {
            var response = await _client.PostAsJsonAsync("api/Schedules/arrangeschedules", sc);
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int Id)
        {
            var response = await _client.GetStringAsync($"api/Schedules/{Id}");
            var result = JsonConvert.DeserializeObject<SchedulesViewModel>(response);
            return View(result);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult>Create(SchedulesDTO dto)
        {
            var response = await _client.PostAsJsonAsync($"api/schedules",dto);
            return View(response);
        }
        public async Task<IActionResult>Update(SchedulesDTO dto, int id)
        {
            var response = await _client.PutAsJsonAsync($"api/schedules/{id}", dto);
            return View(response);
        }

    }
}
