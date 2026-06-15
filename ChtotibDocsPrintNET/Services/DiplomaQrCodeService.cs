using QRCoder;

namespace ChtotibDocsPrintNET.Services;

public static class DiplomaQrCodeService
{
    /// <summary>
    /// Пикселей на один модуль QR в PNG. Больше значение — плотнее растр при последующем масштабировании в PDF/превью, чётче границы модулей.
    /// </summary>
    private const int PixelsPerModule = 12;

    /// <summary>PNG (ч/б), тихая зона включена. ECC L и крупный растр — меньше модулей и проще сканеру.</summary>
    public static byte[]? RenderPng(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            using var gen = new QRCodeGenerator();
            using var data = CreateQrData(gen, text);
            var png = new PngByteQRCode(data);
            return png.GetGraphic(PixelsPerModule,
                new byte[] { 0, 0, 0, 255 },
                new byte[] { 255, 255, 255, 255 },
                drawQuietZones: true);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Уровень L: при той же длине полезной нагрузки матрица QR меньше, чем при Q/M — модули крупнее при той же физической стороне кода (удобнее камере).
    /// </summary>
    private static QRCodeData CreateQrData(QRCodeGenerator gen, string text)
    {
        try
        {
            return gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.L);
        }
        catch
        {
            var t = text.Length > 480 ? text[..480] : text;
            return gen.CreateQrCode(t, QRCodeGenerator.ECCLevel.L);
        }
    }
}
