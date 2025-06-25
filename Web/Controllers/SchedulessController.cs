
using API.Models;
using API.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class SchedulessController : Controller
    {
        private readonly HttpClient _client;
        private readonly API.Data.AduDbcontext _context;
        public SchedulessController(HttpClient client, API.Data.AduDbcontext context)
        {
            _context = context;
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
            await LoadSelectitem();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult>Create(SchedulesDTO dto)
        {
            var response = await _client.PostAsJsonAsync("api/schedules",dto);
            //var  result = response.Content.ReadAsStringAsync();
            await LoadSelectitem();
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            var response = await _client.GetStringAsync($"api/schedules/{Id}");
            var schedules = JsonConvert.DeserializeObject<SchedulesDTO>(response);
            await LoadSelectitem();
            return View(schedules); 
        }
        [HttpPost]
        public async Task<IActionResult> Edit(SchedulesDTO model)
        {
            var response = await _client.PutAsJsonAsync($"api/schedules/{model.Id}", model);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            var error =await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", error);
            await LoadSelectitem();
            return View(model);
        }
        
        public async Task<IActionResult> Delete(int Id)
        {
            var response = await _client.DeleteAsync($"api/Schedules/{Id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Xóa thành công!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Xóa thất bại!";
            }
      
            return RedirectToAction(nameof(Index));
        }
        public async Task LoadSelectitem()
        {
            var classList = await _context.Classes.ToListAsync();
            ViewBag.ClassList = classList.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.NameClass
            }).ToList();
            var roomlist = await _context.Rooms.ToListAsync();
            ViewBag.RoomList = roomlist.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.RoomCode
            }).ToList();
            var weekday = await _context.DayOfWeeks.ToListAsync();
            ViewBag.DayList = weekday.Select(w => new SelectListItem
            {
                Value = w.Id.ToString(),
                Text = w.Weekdays
            }).ToList();
            var studyShift = await _context.StudyShifts.ToListAsync();
            ViewBag.ShiftList = studyShift.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.StudyShiftName
            }).ToList();
        }
    }
}
