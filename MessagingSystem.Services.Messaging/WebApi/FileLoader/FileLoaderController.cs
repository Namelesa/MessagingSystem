using MessagingSystem.Services.Messaging.Application.FileLoader;
using MessagingSystem.Services.Messaging.WebApi.FileLoader.FileLoadContract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingSystem.Services.Messaging.WebApi.FileLoader;

[Authorize]
[ApiController]
[Route("api/file-loader")]
public class FileLoaderController(IFileLoaderOrchestrator fileLoaderOrchestrator) : ControllerBase
{
    [HttpPost("get-load-file-url")]
    public async Task<IActionResult> GetLoadFileUrlsAsync([FromForm] FileLoaderMessage files)
    {
        if (files.File == null || files.File.Length == 0 || files.File.Length >= 40)
            return BadRequest("Files are required");

        var urls = new List<object>();

        foreach (var file in files.File)
        {
            var key = $"{Guid.NewGuid()}_{file.FileName}";
            var url = await fileLoaderOrchestrator.GetLoadFileUrlAsync(key, file.ContentType);

            urls.Add(new {
                originalName = file.FileName, 
                uniqueFileName = key,
                url,
                uploadedAt = DateTime.UtcNow
            });
        }

        return Ok(urls);
    }
    
    [HttpGet("get-download-file-url")]
    public async Task<IActionResult> GetDownloadFileUrlsAsync([FromQuery]string[] fileNames)
    {
        var urls = new List<object>();
        
        foreach (var fileName in fileNames)
        {
            var url = await fileLoaderOrchestrator.GetDownloadFileUrlAsync(fileName);
            urls.Add(new { fileName, url });
        }
        return Ok(urls);
    }
    
    [HttpDelete("delete-file")]
    public async Task<IActionResult> DeleteFileAsync([FromQuery]string[] fileName)
    {
        foreach (var file in fileName)
        {
            await fileLoaderOrchestrator.DeleteFileAsync(file);
        }
        return NoContent();
    }
}