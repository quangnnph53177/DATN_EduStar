using API.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Web.Controllers
{
    [Authorize] //phân quyền đây 
    public class ClassMvcController : Controller
    {
        private readonly HttpClient _httpClient;

        public ClassMvcController()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7298/api/Class/");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<ClassViewModel> classes = new List<ClassViewModel>();
            try
            {
                var response = await _httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    classes = JsonConvert.DeserializeObject<List<ClassViewModel>>(data);
                }
                else
                {
                    ViewBag.ErrorMessage = "Error fetching classes: " + response.ReasonPhrase;
                }
            }
            catch (HttpRequestException e)
            {
                ViewBag.ErrorMessage = "Request error: " + e.Message;
            }
            catch (JsonException e)
            {
                ViewBag.ErrorMessage = "Data parsing error: " + e.Message;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An unexpected error occurred: " + ex.Message;
            }

            return View(classes);
        }

        public async Task<IActionResult> Details(int id)
        {
            ClassViewModel classViewModel = null;
            try
            {
                var response = await _httpClient.GetAsync($"{id}");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    classViewModel = JsonConvert.DeserializeObject<ClassViewModel>(data);
                }
                else
                {
                    ViewBag.ErrorMessage = "Error fetching class details.";
                }
            }
            catch (HttpRequestException e)
            {
                ViewBag.ErrorMessage = "Request error: " + e.Message;
            }
            catch (JsonException e)
            {
                ViewBag.ErrorMessage = "Data parsing error: " + e.Message;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An unexpected error occurred: " + ex.Message;
            }

            return View(classViewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClassCreateViewModel classCreateViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var content = new StringContent(JsonConvert.SerializeObject(classCreateViewModel), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync("", content);
                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        ViewBag.ErrorMessage = $"Error creating class: {response.ReasonPhrase}. Details: {errorContent}";
                    }
                }
                catch (HttpRequestException e)
                {
                    ViewBag.ErrorMessage = "Request error: " + e.Message;
                }
                catch (JsonException e)
                {
                    ViewBag.ErrorMessage = "Data parsing error: " + e.Message;
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An unexpected error occurred during creation: " + ex.Message;
                }
            }
            return View(classCreateViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ClassUpdateViewModel classUpdateViewModel = null;
            try
            {
                var response = await _httpClient.GetAsync($"{id}");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var classViewModel = JsonConvert.DeserializeObject<ClassViewModel>(data);

                    classUpdateViewModel = new ClassUpdateViewModel
                    {
                        ClassName = classViewModel.ClassName,
                        Semester = classViewModel.Semester,
                        YearSchool = classViewModel.YearSchool,
                    };
                }
                else
                {
                    ViewBag.ErrorMessage = "Error fetching class details for edit.";
                }
            }
            catch (HttpRequestException e)
            {
                ViewBag.ErrorMessage = "Request error: " + e.Message;
            }
            catch (JsonException e)
            {
                ViewBag.ErrorMessage = "Data parsing error: " + e.Message;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An unexpected error occurred during edit retrieval: " + ex.Message;
            }

            return View(classUpdateViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClassUpdateViewModel classUpdateViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var content = new StringContent(JsonConvert.SerializeObject(classUpdateViewModel), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"{id}", content);
                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction("Index");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        ViewBag.ErrorMessage = $"Class with ID {id} not found for update.";
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        ViewBag.ErrorMessage = $"Error updating class: {response.ReasonPhrase}. Details: {errorContent}";
                    }
                }
                catch (HttpRequestException e)
                {
                    ViewBag.ErrorMessage = "Request error: " + e.Message;
                }
                catch (JsonException e)
                {
                    ViewBag.ErrorMessage = "Data parsing error: " + e.Message;
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "An unexpected error occurred during update: " + ex.Message;
                }
            }
            return View(classUpdateViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            ClassViewModel classViewModel = null;
            try
            {
                var response = await _httpClient.GetAsync($"{id}");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    classViewModel = JsonConvert.DeserializeObject<ClassViewModel>(data);
                }
                else
                {
                    ViewBag.ErrorMessage = "Error fetching class details for delete.";
                }
            }
            catch (HttpRequestException e)
            {
                ViewBag.ErrorMessage = "Request error: " + e.Message;
            }
            catch (JsonException e)
            {
                ViewBag.ErrorMessage = "Data parsing error: " + e.Message;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An unexpected error occurred during delete retrieval: " + ex.Message;
            }

            return View(classViewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{id}");
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.ErrorMessage = "Error deleting class: " + response.ReasonPhrase;
                }
            }
            catch (HttpRequestException e)
            {
                ViewBag.ErrorMessage = "Request error: " + e.Message;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An unexpected error occurred during deletion: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult AssignStudent(int id)
        {
            ViewBag.ClassId = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignStudent(int classId, Guid studentId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{classId}/assignStudent/{studentId}", null);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Sinh viên đã được gán vào lớp thành công!";
                    return RedirectToAction("Details", new { id = classId });
                }
                else
                {
                    ViewBag.ClassId = classId;
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ViewBag.ErrorMessage = $"Error assigning student: {response.ReasonPhrase}. Details: {errorContent}";
                    return View();
                }
            }
            catch (HttpRequestException e)
            {
                ViewBag.ClassId = classId;
                ViewBag.ErrorMessage = "Request error: " + e.Message;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int classId, Guid studentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{classId}/removeStudent/{studentId}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Sinh viên đã được xóa khỏi lớp thành công!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Error removing student: {response.ReasonPhrase}. Details: {errorContent}";
                }
            }
            catch (HttpRequestException e)
            {
                TempData["ErrorMessage"] = "Request error: " + e.Message;
            }

            return RedirectToAction("Details", new { id = classId });
        }

        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            List<ClassChangeViewModel> history = new List<ClassChangeViewModel>();
            try
            {
                var response = await _httpClient.GetAsync($"{id}/history");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    history = JsonConvert.DeserializeObject<List<ClassChangeViewModel>>(data);
                }
                else
                {
                    ViewBag.ErrorMessage = "Error fetching class history: " + response.ReasonPhrase;
                }
            }
            catch (HttpRequestException e)
            {
                ViewBag.ErrorMessage = "Request error: " + e.Message;
            }

            return View(history);
        }
    }
}