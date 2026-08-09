using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Models;
using Microsoft.AspNetCore.Authorization;
using SchoolManager.Services.Interfaces;

[Authorize(Roles = "student,estudiante")]
public class StudentController : Controller
{
    private readonly IStudentService _studentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserService _userService;

    public StudentController(
        IStudentService studentService,
        ICurrentUserService currentUserService,
        IUserService userService)
    {
        _studentService = studentService;
        _currentUserService = currentUserService;
        _userService = userService;
    }

    /// <summary>
    /// Si no hay fila en <c>students</c> pero el usuario es estudiante autenticado, arma un modelo mínimo desde <c>users</c>
    /// (evita 404 en Details/Index en datos importados o E2E sin matrícula física en <c>students</c>).
    /// </summary>
    private async Task<Student?> ResolveSelfStudentAsync(Guid userId)
    {
        var student = await _studentService.GetByIdAsync(userId);
        if (student != null)
            return student;

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return null;

        var school = await _currentUserService.GetCurrentUserSchoolAsync();
        return new Student
        {
            Id = user.Id,
            Name = ($"{user.Name} {user.LastName}").Trim(),
            SchoolId = user.SchoolId,
            BirthDate = user.DateOfBirth.HasValue ? DateOnly.FromDateTime(user.DateOfBirth.Value) : null,
            School = school,
            CreatedAt = user.CreatedAt,
        };
    }

    public async Task<IActionResult> Index()
    {
        var me = await _currentUserService.GetCurrentUserIdAsync();
        if (me == null)
            return Unauthorized();
        var self = await ResolveSelfStudentAsync(me.Value);
        var students = self != null ? new List<Student> { self } : new List<Student>();
        return View(students);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var me = await _currentUserService.GetCurrentUserIdAsync();
        if (me == null || id != me.Value)
            return Forbid();
        var student = await ResolveSelfStudentAsync(id);
        if (student == null) return NotFound();
        return View(student);
    }

    public IActionResult Create() => Forbid();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Student student) => Forbid();

    public async Task<IActionResult> Edit(Guid id)
    {
        var me = await _currentUserService.GetCurrentUserIdAsync();
        if (me == null || id != me.Value)
            return Forbid();
        var student = await ResolveSelfStudentAsync(id);
        if (student == null) return NotFound();
        return View(student);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Student student)
    {
        var me = await _currentUserService.GetCurrentUserIdAsync();
        if (me == null || student.Id != me.Value)
            return Forbid();
        if (ModelState.IsValid)
        {
            await _studentService.UpdateAsync(student);
            return RedirectToAction(nameof(Index));
        }
        return View(student);
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var me = await _currentUserService.GetCurrentUserIdAsync();
        if (me == null || id != me.Value)
            return Forbid();
        var student = await ResolveSelfStudentAsync(id);
        if (student == null) return NotFound();
        return View(student);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var me = await _currentUserService.GetCurrentUserIdAsync();
        if (me == null || id != me.Value)
            return Forbid();
        await _studentService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult AccessPending()
    {
        return View();
    }
}
