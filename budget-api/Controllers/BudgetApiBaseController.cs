using budget_api.Services.Responses;
using budget_api.Services.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;
using System.Security.Claims;

namespace budget_api.Controllers
{
    public abstract class BudgetApiBaseController : Controller
    {
        protected BudgetApiBaseController() : base() { }

        protected string? CurrentUserId { get { return User?.FindFirstValue(ClaimTypes.NameIdentifier); } }

        protected IActionResult ErrorPage(string message = "", HttpStatusCode? statusCode = null)
        {
            return Redirect($"/Error?message={message}&statusCode={statusCode}");
        }
        protected IActionResult ErrorPage<T>(ServiceResult<T> serviceResult)
        {
            return Redirect($"/Error?message={serviceResult.Error.Description}&statusCode={serviceResult.StatusCode}");
        }
        protected IActionResult ErrorPage(ServiceResult serviceResult)
        {
            return Redirect($"/Error?message={serviceResult.Error.Description}&statusCode={serviceResult.StatusCode}");
        }

        protected IActionResult HandleServiceResult<T>(ServiceResult<T> serviceResult, int? customStatusCode = null)
        {
            if (serviceResult.IsSuccess)
            {
                if (typeof(T) == typeof(FileMemoryStreamResponse))
                {
                    var fileData = serviceResult.Data as FileMemoryStreamResponse;
                    return File(fileData.MemoryStream, fileData.ContentType, fileData.FileDownloadName);
                }
                else if (typeof(T) == typeof(FileByteArrayResponse))
                {
                    var fileData = serviceResult.Data as FileByteArrayResponse;
                    return File(fileData.Bytes, fileData.ContentType, fileData.FileDownloadName);
                }
                return customStatusCode.HasValue ? StatusCode(customStatusCode.Value, serviceResult.Data) : Ok(serviceResult.Data);
            }

            var errors = new Dictionary<string, string[]>() { { "Model", serviceResult.Error.Errors.ToArray() } };

            var vpd = new ValidationProblemDetails(errors);
            vpd.Detail = serviceResult.Error.Description;
            vpd.Instance = $"{HttpContext.Request.Method} {HttpContext.Request.Path}";
            vpd.Type = $"Error {serviceResult.Error.Code}";
            vpd.Title = "Bad request";
            vpd.Status = (int)HttpStatusCode.BadRequest;
            vpd.Extensions.Add("traceId", HttpContext.TraceIdentifier);

            return ValidationProblem(vpd);
        }

        protected IActionResult HandleServiceResult(ServiceResult serviceResult, int? customStatusCode = null)
        {
            if (serviceResult.IsSuccess)
                return customStatusCode.HasValue ? StatusCode(customStatusCode.Value) : Ok();


            var errors = new Dictionary<string, string[]>() { { "Model", serviceResult.Error.Errors.ToArray() } };

            var vpd = new ValidationProblemDetails(errors);
            vpd.Detail = serviceResult.Error.Description;
            vpd.Instance = $"{HttpContext.Request.Method} {HttpContext.Request.Path}";
            vpd.Type = $"Error {serviceResult.Error.Code}";
            vpd.Title = "Bad request";
            vpd.Status = (int)HttpStatusCode.BadRequest;
            vpd.Extensions.Add("traceId", HttpContext.TraceIdentifier);

            return ValidationProblem(vpd);
        }
    }
}
