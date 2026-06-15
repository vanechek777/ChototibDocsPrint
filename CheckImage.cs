using System;
using System.Drawing;

class Program {
    static void Main() {
        var img = new Bitmap("ChtotibDocsPrintNET/DocsTemplates/Diploma_Front.jpg");
        Console.WriteLine($"W: {img.Width}, H: {img.Height}");
        bool allWhite = true;
        for (int x = 0; x < img.Width; x++) {
            var p = img.GetPixel(x, img.Height - 1);
            if (p.R < 250 || p.G < 250 || p.B < 250) {
                allWhite = false;
                break;
            }
        }
        Console.WriteLine($"Bottom row all white: {allWhite}");

        allWhite = true;
        for (int x = 0; x < img.Width; x++) {
            var p = img.GetPixel(x, img.Height - 2);
            if (p.R < 250 || p.G < 250 || p.B < 250) {
                allWhite = false;
                break;
            }
        }
        Console.WriteLine($"Second to bottom row all white: {allWhite}");
        
        allWhite = true;
        for (int y = 0; y < img.Height; y++) {
            var p = img.GetPixel(img.Width - 1, y);
            if (p.R < 250 || p.G < 250 || p.B < 250) {
                allWhite = false;
                break;
            }
        }
        Console.WriteLine($"Rightmost column all white: {allWhite}");
    }
}