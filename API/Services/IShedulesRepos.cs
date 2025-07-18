﻿using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IShedulesRepos
    {
        Task<List<Schedule>> GetAll(); 
        Task<List<Lichcodinh>> GetAllCoDinh();
        Task<SchedulesViewModel> GetById(int id);
        Task<List<SchedulesViewModel>> GetByStudent(Guid Id);
        Task AutogenerateSchedule();
        Task<Schedule> CreateSchedules(SchedulesDTO model);
        Task UpdateSchedules(SchedulesDTO model);
        Task<byte[]> ExportSchedules(List<SchedulesViewModel> model);
        Task DeleteSchedules(int Id);
    }
}
