using Car4You.DAL;
using Car4You.Helper;
using Car4You.Helpers;
using Car4You.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
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
    private readonly CarDbContext _context;

    public PhotoUploadHelper(IWebHostEnvironment environment, ILogger<PhotoUploadHelper> logger, CarDbContext context)
    {
        _environment = environment;
        _logger = logger;
        _context = context;
    }

    public static async Task<Stream> OverlayLogoStreamAsync(Stream inputStream, string logoPath)
    {
        inputStream.Position = 0;

        using var image = await Image.LoadAsync<Rgba32>(inputStream);
        using var logo = await Image.LoadAsync<Rgba32>(logoPath);

        // Skaluj logo do 20% krótszego boku (zamiast szerokości)
        int shorterSide = Math.Min(image.Width, image.Height);
        int logoWidth = (int)(shorterSide * 0.2);
        logo.Mutate(x => x.Resize(logoWidth, 0));

        // Zawsze prawy dolny róg
        int margin = 20;
        int x = image.Width - logo.Width - margin;
        int y = image.Height - logo.Height - margin;

        // Kontur logo
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
                        pixelRow[col] = new Rgba32(0, 0, 0, 180);
                    else
                        pixelRow[col] = new Rgba32(0, 0, 0, 0);
                }
            }
        });

        // Poświata
        using var glowLogo = outlineLogo.Clone();
        glowLogo.Mutate(x => x.GaussianBlur(3f));

        // Rysowanie
        image.Mutate(ctx =>
        {
            ctx.DrawImage(glowLogo, new Point(x, y), 0.25f);
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    if (offsetX == 0 && offsetY == 0) continue;
                    ctx.DrawImage(outlineLogo, new Point(x + offsetX, y + offsetY), 0.9f);
                }
            }
            ctx.DrawImage(logo, new Point(x, y), 1f);
        });

        var output = new MemoryStream();
        await image.SaveAsync(output, new JpegEncoder { Quality = 90 });
        output.Position = 0;
        return output;
    }


    public async Task CompressToMaxSizeAsync(Stream inputStream, string outputPath, int maxSizeBytes)
    {
        inputStream.Position = 0;

        using (var image = await Image.LoadAsync(inputStream))
        {
            int quality = 85;
            byte[] imageBytes;

            do
            {
                using (var outStream = new MemoryStream())
                {
                    var encoder = new JpegEncoder { Quality = quality };
                    await image.SaveAsync(outStream, encoder);
                    imageBytes = outStream.ToArray();
                }

                quality -= 5;
            }
            while (imageBytes.Length > maxSizeBytes && quality > 30);

            await File.WriteAllBytesAsync(outputPath, imageBytes);
        }
    }


}
