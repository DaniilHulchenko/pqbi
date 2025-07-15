namespace PQSServiceMock.Helpers;

public class RecipeImporter
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public RecipeImporter(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }


    public async Task FileSaveAsync(IFormFile formFile, string targetDirectory)
    {
        var route = Path.Combine(_webHostEnvironment.WebRootPath, targetDirectory);

        if (!Directory.Exists(route))
        {
            Directory.CreateDirectory(route);
        }

        var fileName = formFile.FileName;
        //var fileName = $"{Guid.NewGuid()}{Path.GetExtension(formFile.FileName)}";
        var targetFilePath = Path.Combine(route, fileName);


        using (FileStream fs = File.Create(targetFilePath))
        {
            await formFile.OpenReadStream().CopyToAsync(fs);
        }

    }
}
