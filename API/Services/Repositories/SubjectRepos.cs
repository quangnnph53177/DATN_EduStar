 using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;

namespace API.Services.Repositories
{
    public class SubjectRepos : ISubject
    {
        private readonly AduDbcontext _context;
        public SubjectRepos(AduDbcontext context)
        {
            _context = context;
        }

        public async Task<Subject> CreateSubject(SubjectViewModel sub)
        {
            var su = new Subject()
            {
                Id = sub.Id,
                SubjectName = sub.SubjectName,
                Subjectcode = sub.subjectCode,
                Description= sub.Description,
                NumberOfCredits = sub.NumberOfCredits,
                SemesterId = sub.SemesterId, 
                Status = sub.Status,
            };
            _context.Subjects.Add(su);
            await _context.SaveChangesAsync();
            return su;
        }

        public async Task<bool> DeleteSubject(int Id)
        {
            var del =await _context.Subjects.FirstOrDefaultAsync(s=>s.Id==Id);
            if (del.Status == false)
            {
                _context.Subjects.Remove(del);
                await _context.SaveChangesAsync();
            }
            return true;
           
        }     

        public async Task<List<SubjectViewModel>> Getall()
        {
            var sub = await _context.Subjects
                .ToListAsync();
            var item = sub.Select(c => new SubjectViewModel
            {
                Id = c.Id,
                SubjectName = c.SubjectName,
                subjectCode= c.Subjectcode,
                NumberOfCredits = c.NumberOfCredits,
                Description = c.Description,
                SemesterId = c.SemesterId,
                Status = c.Status,
            }).ToList();
            return item;
        }

        public async Task<SubjectViewModel> GetById(int id)
        {
            var details =await _context.Subjects.FirstOrDefaultAsync(c => c.Id == id);
            var item = new SubjectViewModel
            {
                Id= id,
                SubjectName=details.SubjectName,
                subjectCode= details.Subjectcode,
                NumberOfCredits=details.NumberOfCredits,
                Description=details.Description,
                SemesterId = details.SemesterId,
                Status = details.Status,
            };
            return item;
        }

        public async Task<bool> OpenAndClose(int Id)
        {
            var su =await _context.Subjects.FindAsync(Id);
            if (su == null) return false;
            su.Status = !(su.Status ?? true);
            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<List<SubjectViewModel>> Search(string? subjectName, int? numberofCredit, string? subcode, bool? status, int? semesterId)
        {
            
            var query = _context.Subjects.Include(c => c.Classes).AsSplitQuery();
            if (!string.IsNullOrWhiteSpace(subjectName))
            {
                query = query.Where(c => c.SubjectName.ToLower().Contains(subjectName));
            }
            if (!string.IsNullOrWhiteSpace(subcode))
            {
                query = query.Where(c => c.Subjectcode.ToLower().Contains(subcode));
            }
            if (numberofCredit.HasValue)
            {
                query = query.Where(c => c.NumberOfCredits == numberofCredit);
            }
            if (semesterId.HasValue)
            {
                query = query.Where(c => c.SemesterId == semesterId);
            }
            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status);  
            }
            var model = await query.OrderBy(c=>c.Subjectcode).Select(c => new SubjectViewModel
            {
                Id = c.Id,
                SubjectName=c.SubjectName,
                subjectCode = c.Subjectcode,
                NumberOfCredits=c.NumberOfCredits,
                Description=c.Description,
                SemesterId = c.SemesterId,
                Status =c.Status,
            }).ToListAsync();
            return model;
        
        }

        public async Task<bool> UpdateSubject(SubjectViewModel subject)
        {
            var con = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == subject.Id);

            if (con == null)
                return false; // Không tìm thấy subject cần cập nhật

            // Cập nhật thông tin
            con.SubjectName = subject.SubjectName;
            con.Subjectcode = subject.subjectCode;
            con.NumberOfCredits = subject.NumberOfCredits;
            con.Description = subject.Description;
            con.SemesterId = subject.SemesterId;
            con.Status = subject.Status;

            // Không cần gọi Update(con) nếu con đang được tracked bởi EF
            await _context.SaveChangesAsync();
            return true;
        }

    }
}
