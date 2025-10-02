using Car4You.Helper;
using Car4You.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced; // Dodano brakującą dyrektywę using
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing; // ← ważne dla DrawImage z GraphicsOptions
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;



public class PhotoUploadHelper
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PhotoUploadHelper> _logger;

    public PhotoUploadHelper(IWebHostEnvironment environment, ILogger<PhotoUploadHelper> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<List<TempPhoto>> ProcessUploadedPhotosAsync(
        IList<IFormFile> uploadedPhotos,
        int mainPhotoIndex)
    {
        var tempPhotos = new List<TempPhoto>();

        if (uploadedPhotos == null || uploadedPhotos.Count == 0)
            return tempPhotos;

        string tempFolder = System.IO.Path.Combine(_environment.WebRootPath, "temp");
        string fallbackFolder = System.IO.Path.Combine(_environment.ContentRootPath, "App_TempUploads");
        bool useFallback = false;

        try
        {
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Brak dostępu do /wwwroot/temp. Używam folderu zapasowego.");
            tempFolder = fallbackFolder;
            useFallback = true;

            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);
        }

        for (int i = 0; i < uploadedPhotos.Count; i++)
        {
            var file = uploadedPhotos[i];
            var fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(file.FileName);
            var fullPath = System.IO.Path.Combine(tempFolder, fileName);

            try
            {
                using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                var photoSrc = useFallback
                    ? "/admin/temp-preview?file=" + fileName
                    : "/temp/" + fileName;

                tempPhotos.Add(new TempPhoto
                {
                    Src = photoSrc,
                    IsMain = (i == mainPhotoIndex)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd przy zapisie pliku: {fullPath}");
                throw;
            }
        }

        return tempPhotos;
    }

    public List<TempPhoto> RestoreFromTempData(ITempDataDictionary tempData)
    {
        if (tempData["TempPhotos"] is string json)
        {
            return JsonConvert.DeserializeObject<List<TempPhoto>>(json);
        }

        return new List<TempPhoto>();
    }

    public void SaveToTempData(ITempDataDictionary tempData, List<TempPhoto> photos)
    {
        tempData["TempPhotos"] = JsonConvert.SerializeObject(photos);
    }

    public void ClearTemp()
    {
        // Czyść folder temp (tymczasowe zdjęcia)
        var tempPath = System.IO.Path.Combine(_environment.WebRootPath, "temp");
        ClearingFolder(tempPath);
        tempPath = System.IO.Path.Combine(_environment.WebRootPath, "App_TempUploads");
        ClearingFolder(tempPath);
        return;
    }

    private void ClearingFolder(string tempPath)
    {
        if (Directory.Exists(tempPath))
        {
            foreach (var file in Directory.GetFiles(tempPath))
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Nie udało się usunąć pliku tymczasowego: {file}");
                }
            }
        }
    }

    public static async Task OverlayLogoAsync(string sourcePath, string logoPath, string targetPath)
    {
        using var image = await Image.LoadAsync<Rgba32>(sourcePath);
        using var logo = await Image.LoadAsync<Rgba32>(logoPath);

        // Skaluj logo
        int logoWidth = image.Width / 5;
        logo.Mutate(x => x.Resize(logoWidth, 0));

        int x = image.Width - logo.Width - 20;
        int y = image.Height - logo.Height - 20;

        // Stwórz kontur logo
        using var outlineLogo = logo.Clone();
        outlineLogo.ProcessPixelRows(accessor =>
        {
            for (int row = 0; row < accessor.Height; row++)
            {
                var pixelRow = accessor.GetRowSpan(row);
                for (int col = 0; col < pixelRow.Length; col++)
                {
                    var px = pixelRow[col];
                    if (px.A > 0)
                        pixelRow[col] = new Rgba32(0, 0, 0, 180); // ciemny kontur
                    else
                        pixelRow[col] = new Rgba32(0, 0, 0, 0);
                }
            }
        });

        // Stwórz poświatę z rozmyciem
        using var glowLogo = outlineLogo.Clone();
        glowLogo.Mutate(x => x.GaussianBlur(8f)); // wartość 5 = moc poświaty

        image.Mutate(ctx =>
        {
            // Rysuj poświatę pod spodem
            ctx.DrawImage(glowLogo, new Point(x, y), 0.2f); // lekko przezroczysta

            // Rysuj kontur logo wokół
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    if (offsetX == 0 && offsetY == 0) continue;
                    ctx.DrawImage(outlineLogo, new Point(x + offsetX, y + offsetY), 1f);
                }
            }

            // Nałóż oryginalne logo na środek
            ctx.DrawImage(logo, new Point(x, y), 1f);
        });

        await image.SaveAsync(targetPath, new JpegEncoder { Quality = 90 });
    }

}
