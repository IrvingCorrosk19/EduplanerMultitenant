namespace SchoolManager.Services.Interfaces;

public interface IInformeInstitucionalRazorRenderService
{
    Task<string> RenderViewToStringAsync<TModel>(string viewPath, TModel model);
}
