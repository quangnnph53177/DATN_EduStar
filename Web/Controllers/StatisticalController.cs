using API.ViewModel;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Web.ViewModels;

namespace Web.Controllers
{
    public class StatisticalController : Controller
    {
        private readonly HttpClient _client;
        public StatisticalController(HttpClient client)
        {
            _client = client;
            _client.BaseAddress = new Uri("https://localhost:7298/");
        }
        public async Task<IActionResult> DashBoard()
        {
            var gender = await _client.GetAsync("api/Statistical/StudentByGender");
            var address = await _client.GetAsync("api/Statistical/StudentByAddress");
            var classre = await _client.GetAsync("api/statistical/StudentByClass");
            var status = await _client.GetAsync("api/statistical/studentbystatus");
            if(!gender.IsSuccessStatusCode||!address.IsSuccessStatusCode
                || !classre.IsSuccessStatusCode || !status.IsSuccessStatusCode)
            {
                TempData["Message"] = "Lỗi rồi cha";
                return View(new DashboardViewModel());
            }
            var model = new DashboardViewModel()
            {
               studentByAddress = JsonConvert.DeserializeObject<List<StudentByAddressDTO>>
               (await address.Content.ReadAsStringAsync())?? new List<StudentByAddressDTO>(),
               studentByGender = JsonConvert.DeserializeObject<List<StudentByGenderDTO>>
               (await gender.Content.ReadAsStringAsync()) ?? new List<StudentByGenderDTO>(),
               studentByClass = JsonConvert.DeserializeObject<List<StudentByClassDTO>>
               (await classre.Content.ReadAsStringAsync()) ?? new List<StudentByClassDTO>(),
               studentByStatus = JsonConvert.DeserializeObject<List<StudentByStatusDTO>>
               (await status.Content.ReadAsStringAsync())?? new List<StudentByStatusDTO>()
            };
          
            return View(model);
        }

    }
}