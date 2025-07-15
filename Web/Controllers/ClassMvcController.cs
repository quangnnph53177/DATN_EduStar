using API.Data;
using API.Models;
using API.ViewModel;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using Web.ViewModels;

namespace Web.Controllers
{
   // [Authorize] //phân quyền đây 
    public class ClassMvcController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly AduDbcontext _context;
        public ClassMvcController(AduDbcontext context)
        {
            _context = context;
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
        public async Task<IActionResult> Create()
        {
            var SubjectList =  await _context.Subjects.ToListAsync();
            ViewBag.SubjectList = SubjectList.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.SubjectName,
            }).ToList();
            var TeacherList = await _context.Users.Where(r=>r.Roles.Any(c=> c.Id==2)).ToListAsync();
            ViewBag.TeacherList = TeacherList.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.UserName,
            });
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
            var SubjectList = await _context.Subjects.ToListAsync();
            ViewBag.SubjectList = SubjectList.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.SubjectName,
            }).ToList();
            var TeacherList = await _context.Users.Where(r => r.Roles.Any(c => c.Id == 2)).ToListAsync();
            ViewBag.TeacherList = TeacherList.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.UserName,
            });
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
                        TeacherName = classViewModel.TeacherName,
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
        public async Task<IActionResult> AssignStudent(int Id)
        {
            await LoadStudentListForClass(Id);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignStudent(AssignStudentsRequest request)
        {
            Console.WriteLine(">> [MVC] Nhận classId: " + request.ClassId);
            Console.WriteLine(">> [MVC] Số sinh viên chọn: " + (request.StudentIds?.Count ?? 0));

            if (request.StudentIds == null || !request.StudentIds.Any())
            {
                ViewBag.ErrorMessage = "Bạn chưa chọn sinh viên nào.";
                await LoadStudentListForClass(request.ClassId);
                return View();
            }

            var response = await _httpClient.PostAsJsonAsync($"{request.ClassId}/assignStudent/{request.StudentIds}", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Gán sinh viên thành công!";
                return RedirectToAction("Details", new { id = request.ClassId });
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            ViewBag.ErrorMessage = $"Lỗi khi gọi API: {errorContent}";
            await LoadStudentListForClass(request.ClassId);
            return View();
        }

        private async Task LoadStudentListForClass(int classId)
        {
            var cls = await _context.Classes.FindAsync(classId);

            var allStudents = await _context.StudentsInfors
                .Select(s => new StudentDTO
                {
                    id = s.UserId,
                    FullName = s.User.UserProfile.FullName,
                    Email = s.User.Email
                })
                .ToListAsync();

            var assignedStudentIds = await _context.Set<Dictionary<string, object>>("StudentInClass")
                .Where(sc => (int)sc["ClassId"] == classId)
                .Select(sc => (Guid)sc["StudentId"])
                .ToListAsync();

            var unassignedStudents = allStudents
                .Where(s => !assignedStudentIds.Contains(s.id))
                .ToList();

            ViewBag.ClassId = classId;
            ViewBag.NameClass = cls?.NameClass;
            ViewBag.StudentList = unassignedStudents;
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