using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using SixLabors.Fonts;
using SixLabors.Shapes;
using SixLabors.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Overlays;
using SixLabors.ImageSharp.Processing.Transforms;
using Microsoft.AspNetCore.Mvc;

namespace corecolor.Controllers
{
    [Route("[controller]")]
    public class ImageController : Controller
    {
        [HttpGet("[action]/{*url}")]
        public async Task<IActionResult> CoreColor(string url) {

            if (url.Substring(0, 4) != "http") {
                url = "http://" + url;
            }

            Image<Rgba32> sourceImage = await this.LoadImageFromUrl(url);
            Dictionary<string, int> colors = new Dictionary<string, int>();

            for (int x = 1; x < (sourceImage.Width - 2); x++) {
                for (int y = 1; y < (sourceImage.Height - 2 ); y++) {
                    string pixel1 = sourceImage[x-1, y+1].ToHex();
                    string pixel2 = sourceImage[x, y+1].ToHex();
                    string pixel3 = sourceImage[x+1, y+1].ToHex();
                    string pixel4 = sourceImage[x-1, y].ToHex();
                    string pixel5 = sourceImage[x, y].ToHex();
                    string pixel6 = sourceImage[x+1, y].ToHex();
                    string pixel7 = sourceImage[x-1, y-1].ToHex();
                    string pixel8 = sourceImage[x, y-1].ToHex();
                    string pixel9 = sourceImage[x+1, y-1].ToHex();
                    if (pixel5 == pixel1 && pixel5 == pixel2 && pixel5 == pixel3 && pixel5 == pixel4 && pixel5 == pixel6 && pixel5 == pixel7 && pixel5 == pixel8 && pixel5 == pixel9) {
                        int value;
                        if (colors.TryGetValue(pixel5, out value)) {
                            colors[pixel5] += 1;
                        } else {
                            colors[pixel5] = 1;
                        }
                    }
                }
            }

            Image<Rgba32> img = new Image<Rgba32>(325, 1000);
            MemoryStream output = new MemoryStream();

            using (img) {
                int n = 0;
                var font = SystemFonts.CreateFont("Consolas", 16, FontStyle.Regular);

                foreach (KeyValuePair<string, int> entry in colors) {
                    img.Mutate(x => {
                        if (Rgba32.FromHex(entry.Key).A > 0.9f) {
                            x.Fill(Rgba32.FromHex(entry.Key), new Rectangle(200, 25*n, 50, 25));
                            x.DrawText("#" + entry.Key.Substring(0,6), font, Rgba32.White, new PointF(257, 25*n-1));
                            n++;
                        }
                    });
                }

                int imageSize = 25 * n + 20;
                if (imageSize < 120) { imageSize = 120; }

                font = SystemFonts.CreateFont("Consolas", 14, FontStyle.Regular);

                using (sourceImage) {
                    sourceImage.Mutate(x => x.Resize(190, 0));
                    img.Mutate(x => {
                        x.BackgroundColor(Rgba32.FromHex("62727b"));
                        x.Fill(Rgba32.White, new Rectangle(0, 0, 200, imageSize-20));
                        x.DrawImage(sourceImage, 1, new Point(5, ((imageSize-20)/2) - (sourceImage.Height/2) ));
                        x.Fill(Rgba32.FromHex("102027"), new Rectangle(0, imageSize-20, 500, 20));
                        x.DrawText("Made with CoreColor", font, Rgba32.White, new PointF(173, imageSize-22));
                    });
                }

                img.Mutate(x => x.Crop(325, imageSize));
                img.SaveAsPng(output);
                output.Seek(0, SeekOrigin.Begin);
            }

            return File(output, "image/png");

        }

        private async Task<Image<Rgba32>> LoadImageFromUrl(string url)
        {
            Image<Rgba32> image = null;

            try {
                HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = await httpClient.GetAsync(url);
                Stream inputStream = await response.Content.ReadAsStreamAsync();

                image = Image.Load(inputStream);
            } catch {
                
            }

            return image;
        }

    }
}