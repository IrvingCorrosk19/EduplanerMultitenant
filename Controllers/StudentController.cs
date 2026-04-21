using Microsoft.AspNetCore.Mvc;
using SchoolManager.Models;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "student,estudiante")]
public class StudentController : Controller
{
    private readonly IStudentService _studentService;

    public StudentController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    public async Task<IActionResult> Index()
    {
        var students = await _studentService.GetAllAsync();
        return View(students);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var student = await _studentService.GetByIdAsync(id);
        if (student == null) return NotFound();
        return View(student);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(Student student)
    {
        if (ModelState.IsValid)
        {
            await _studentService.CreateAsync(student);
            return RedirectToAction(nameof(Index));
        }
        return View(student);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var student = await _studentService.GetByIdAsync(id);
        if (student == null) return NotFound();
        return View(student);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Student student)
    {
        if (ModelState.IsValid)
        {
            await _studentService.UpdateAsync(student);
            return RedirectToAction(nameof(Index));
        }
        return View(student);
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var student = await _studentService.GetByIdAsync(id);
        if (student == null) return NotFound();
        return View(student);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _studentService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Vista informativa cuando el estudiante no tiene acceso a la plataforma (PlatformAccessStatus = Pendiente). Ruta excluida del PlatformAccessGuardFilter.</summary>
    [HttpGet]
    public IActionResult AccessPending()
    {
        return View();
    }
}
