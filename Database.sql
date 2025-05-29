CREATE DATABASE Eghitvebinh3;
GO
USE Eghitvebinh3;
GO

-- 1. Roles
CREATE TABLE Roles (
    Id INT PRIMARY KEY identity(1,1),
    RoleName NVARCHAR(28) NOT NULL
);
GO
create table Permission
(
	Id int primary key identity(1,1),
	PermissionName Nvarchar(Max)
);
create table RolePermission(
	RoleId int foreign key references Roles (Id) ON DELETE CASCADE,
	PermissionId int foreign key references Permission (Id) ON DELETE CASCADE
	primary key (RoleId,PermissionId)
);
-- 2. Users
CREATE TABLE [User] (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserName NVARCHAR(50) NOT NULL,
    PassWordHash NVARCHAR(90) NOT NULL,
    Email NVARCHAR(90),
    PhoneNumber NVARCHAR(12),
    Statuss BIT,
    CreateAt DATETIME DEFAULT GETDATE()
);
GO
create table UserRole
(
	Userid UNIQUEIDENTIFIER foreign key references [User](Id) ON DELETE CASCADE,
	Roleid int foreign key references Roles (Id),
	primary key(Userid, RoleId)
);
-- 3. UserProfiles
CREATE TABLE UserProfiles (
    UserId UNIQUEIDENTIFIER PRIMARY KEY FOREIGN KEY REFERENCES [User](Id) ON DELETE CASCADE,
    FullName NVARCHAR(90),
	UserCode NVARCHAR(50),
    Gender BIT,
    Avatar NVARCHAR(255),
    Address NVARCHAR(225),
    DOB DATE
);
GO
create table auditlog(
	Id int identity(1, 1) primary key,
	Userid UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [User](Id) ON DELETE SET NULL,
	Active nvarchar(Max),
	Descriptions nvarchar(max),
	Timestamp datetime default getdate()
);
-- 4. Subjects
CREATE TABLE Subjects (
    Id INT PRIMARY KEY,
    SubjectName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    NumberOfCredits INT
);
GO

-- 5. Classes
CREATE TABLE Classes (
    Id INT PRIMARY KEY identity(1,1),
    NameClass NVARCHAR(90),
    SubjectId INT FOREIGN KEY REFERENCES Subjects(Id),
    Semester NVARCHAR(10),
    YearSchool INT
);
GO

-- 6. StudentsInfor
CREATE TABLE StudentsInfor (
    UserId UNIQUEIDENTIFIER PRIMARY KEY FOREIGN KEY REFERENCES [User](Id) ON DELETE CASCADE,
    StudentsCode NVARCHAR(50),
    ClassCode NVARCHAR(90)
);
GO

-- 7. StudentInClass
CREATE TABLE StudentInClass (
    ClassId INT FOREIGN KEY REFERENCES Classes(Id) ON DELETE CASCADE,
    StudentId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES StudentsInfor(UserId) ON DELETE CASCADE,
    PRIMARY KEY (ClassId, StudentId)
);
GO

-- 8. Rooms
CREATE TABLE Rooms (
    Id INT PRIMARY KEY identity(1,1),
    RoomCode NVARCHAR(50),
    Capacity INT,
    Device NVARCHAR(90)
);
GO

-- 9. StudyShifts
CREATE TABLE StudyShifts (
    Id INT PRIMARY KEY identity(1,1),
    StudyShiftName NVARCHAR(90),
    StartTime TIME,
    EndTime TIME
);
GO

-- 10. DayOfWeeks
CREATE TABLE DayOfWeeks (
    Id INT PRIMARY KEY identity(1,1),
    Weekdays NVARCHAR(10)
);
GO

-- 11. Schedules
CREATE TABLE Schedules (
    Id INT PRIMARY KEY identity(1,1),
    ClassId INT FOREIGN KEY REFERENCES Classes(Id) ON DELETE CASCADE,
    RoomId INT FOREIGN KEY REFERENCES Rooms(Id) ON DELETE SET NULL,
    DayId INT FOREIGN KEY REFERENCES DayOfWeeks(Id) ON DELETE SET NULL,
    StudyShiftId INT FOREIGN KEY REFERENCES StudyShifts(Id)ON DELETE SET NULL
);
GO

-- 12. Attendance
CREATE TABLE Attendance (
    Id INT PRIMARY KEY identity(1,1),
    SchedulesId INT FOREIGN KEY REFERENCES Schedules(Id) ON DELETE CASCADE,
    UserId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [User](Id) ON DELETE SET NULL,
    CreateAt DATETIME DEFAULT GETDATE()
);
GO

-- 13. AttendanceDetails
CREATE TABLE AttendanceDetails (
    Id INT PRIMARY KEY identity(1,1),
    StudentId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES StudentsInfor(UserId),
    AttendanceId INT FOREIGN KEY REFERENCES Attendance(Id) ON DELETE CASCADE,
    Statuss NVARCHAR(20),
    Description NVARCHAR(MAX)
);
GO

-- 14. Complaints
CREATE TABLE Complaints (
    Id INT PRIMARY KEY identity(1,1),
    StudentId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [User](Id),
    ComplaintType NVARCHAR(50),
    Statuss NVARCHAR(20),
    Reason NVARCHAR(MAX),
    ProofUrl NVARCHAR(255),
    CreateAt DATETIME DEFAULT GETDATE(),
    ProcessedAt DATETIME,
    ProcessedBy UNIQUEIDENTIFIER FOREIGN KEY REFERENCES [User](Id),
    ResponseNote NVARCHAR(MAX)
);
GO

-- 15. AttendanceDetailsComplaints
CREATE TABLE AttendanceDetailsComplaints (
    ComplaintId INT PRIMARY KEY FOREIGN KEY REFERENCES Complaints(Id) ON DELETE CASCADE,
    SchedulesId INT FOREIGN KEY REFERENCES Schedules(Id)ON DELETE SET NULL
);
GO

-- 16. ClassChange
CREATE TABLE ClassChange (
    ComplaintId INT PRIMARY KEY FOREIGN KEY REFERENCES Complaints(Id ) ON DELETE CASCADE,
    CurrentClassId INT FOREIGN KEY REFERENCES Classes(Id),
    RequestedClassId INT FOREIGN KEY REFERENCES Classes(Id)
);
GO
insert into Roles (RoleName)
values ('Admin'),('Teacher'),('Student')
go
insert into Permission(PermissionName)
values ('Create'),('Edit'),('Detail')
go
SELECT * FROM [User];
SELECT * FROM UserProfiles;
SELECT * FROM UserRole;
SELECT * FROM StudentsInfor;

SELECT * FROM Roles;
SELECT * FROM Permission;
SELECT * FROM RolePermission
-- Lấy Id của Roles và Permissions để gán
DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE RoleName = 'Admin');
DECLARE @TeacherRoleId INT = (SELECT Id FROM Roles WHERE RoleName = 'Teacher');
DECLARE @StudentRoleId INT = (SELECT Id FROM Roles WHERE RoleName = 'Student');


DECLARE @CreatePermId INT = (SELECT Id FROM Permission WHERE PermissionName = 'Create');
SELECT @CreatePermId;
DECLARE @EditPermId INT = (SELECT Id FROM Permission WHERE PermissionName = 'Edit');
DECLARE @DetailPermId INT = (SELECT Id FROM Permission WHERE PermissionName = 'Detail');

-- Gán Permission cho Role Admin (Create, Edit, Delete)

INSERT INTO RolePermission(RoleId, PermissionId) VALUES
(@AdminRoleId, @CreatePermId),
(@AdminRoleId, @EditPermId),
(@AdminRoleId, @DetailPermId);

INSERT INTO RolePermission(RoleId, PermissionId) VALUES
(@StudentRoleId, @EditPermId),
(@StudentRoleId, @DetailPermId);

INSERT INTO RolePermission(RoleId, PermissionId) VALUES
(@TeacherRoleId, @EditPermId),
(@TeacherRoleId, @DetailPermId);
Select*from RolePermission;
Select*from Permission;
delete from [User] where Id like '11FC2482-A153-4781-BF7E-2969D5B562F3';
select * from [UserRole] where Roleid like 2;
select * from [User]