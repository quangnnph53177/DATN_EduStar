using API.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class ClassMvcController : Controller
    {
        public ClassMvcController()
        {

        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<ClassViewModel> classes = new List<ClassViewModel>();

            try
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetAsync("https://localhost:7298/api/Classes");

                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsStringAsync();
                        classes = JsonConvert.DeserializeObject<List<ClassViewModel>>(data);
                    }
                    else
                    {
                        // Handle error response
                        ViewBag.ErrorMessage = "Error fetching classes: " + response.ReasonPhrase;
                    }
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

            return View(classes);
        }
        public async Task<IActionResult> Details(int id)
        {
            ClassViewModel classViewModel = new ClassViewModel();
            using (var http = new HttpClient())
            {
                using (var response = await http.GetAsync($"https://localhost:7298/api/Classes/{id}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsStringAsync();
                        classViewModel = JsonConvert.DeserializeObject<ClassViewModel>(data);
                    }
                    else
                    {
                        // Handle error response
                        ViewBag.ErrorMessage = "Error fetching class details.";
                    }
                }
            }
            return View(classViewModel);
        }
        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClassViewModel classViewModel)
        {
            if (ModelState.IsValid)
            {
                using (var http = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(classViewModel), System.Text.Encoding.UTF8, "application/json");
                    using (var response = await http.PostAsync("https://localhost:7298/api/Classes", content))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            // Handle error response
                            ViewBag.ErrorMessage = "Error creating class.";
                        }
                    }
                }
            }
            return View(classViewModel);

        }
        public async Task<IActionResult> Edit(int id)
        {
            ClassViewModel classViewModel = new ClassViewModel();
            using (var http = new HttpClient())
            {
                using (var response = await http.GetAsync($"https://localhost:7298/api/Classes/{id}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsStringAsync();
                        classViewModel = JsonConvert.DeserializeObject<ClassViewModel>(data);
                    }
                    else
                    {
                        // Handle error response
                        ViewBag.ErrorMessage = "Error fetching class details for edit.";
                    }
                }
            }
            return View(classViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClassViewModel classViewModel)
        {
            if (ModelState.IsValid)
            {
                using (var http = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(classViewModel), System.Text.Encoding.UTF8, "application/json");
                    using (var response = await http.PutAsync($"https://localhost:7298/api/Classes/{id}", content))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            // Handle error response
                            ViewBag.ErrorMessage = "Error updating class.";
                        }
                    }
                }
            }
            return View(classViewModel);
        }
        public async Task<IActionResult> Delete(int id)
        {
            ClassViewModel classViewModel = new ClassViewModel();
            using (var http = new HttpClient())
            {
                using (var response = await http.GetAsync($"https://localhost:7298/api/Classes/{id}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsStringAsync();
                        classViewModel = JsonConvert.DeserializeObject<ClassViewModel>(data);
                    }
                    else
                    {
                        // Handle error response
                        ViewBag.ErrorMessage = "Error fetching class details for delete.";
                    }
                }
            }
            return View(classViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using (var http = new HttpClient())
            {
                using (var response = await http.DeleteAsync($"https://localhost:7298/api/Classes/{id}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        // Handle error response
                        ViewBag.ErrorMessage = "Error deleting class.";
                    }
                }
            }
            return RedirectToAction("Index");
        }
    }
}
