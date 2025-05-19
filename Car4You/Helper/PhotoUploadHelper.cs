using Car4You.Helper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Car4You.Models;

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

        string tempFolder = Path.Combine(_environment.WebRootPath, "temp");
        string fallbackFolder = Path.Combine(_environment.ContentRootPath, "App_TempUploads");
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
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var fullPath = Path.Combine(tempFolder, fileName);

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

        public void ClearTemporaryFiles(List<TempPhoto> tempPhotos)
        {
            if (tempPhotos == null || tempPhotos.Count == 0) return;

            var wwwRootPath = _environment.WebRootPath;
            var contentRootPath = _environment.ContentRootPath;

            foreach (var photo in tempPhotos)
            {
                try
                {
                    var fileName = Path.GetFileName(photo.Src);

                    var pathInWwwroot = Path.Combine(wwwRootPath, "temp", fileName);
                    var pathInFallback = Path.Combine(contentRootPath, "App_TempUploads", fileName);

                    if (File.Exists(pathInWwwroot))
                    {
                        File.Delete(pathInWwwroot);
                        continue;
                    }

                    if (File.Exists(pathInFallback))
                    {
                        File.Delete(pathInFallback);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Błąd usuwania pliku tymczasowego: {photo.Src}");
                }
            }
        }
    }
