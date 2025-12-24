using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SecurityTokenService;

public static class VerifyCodeHelper
{
    public static string GenerateCode(int length)
    {
        // 范围是10^(n-1)到10^n,如果是4,则生成的范围是1000到9999,以此类推
        var code = RandomNumberGenerator.GetInt32((int)Math.Pow(10, length - 1), (int)Math.Pow(10, length))
            .ToString();
        return code;
    }

    public static byte[] GetVerifyCode(string code)
    {
        // 验证码图片尺寸
        const int codeW = 100;
        const int codeH = 34;
        const int fontSize = 16;

        // 颜色列表（用于验证码、噪线）
        Color[] colors = { Color.Black, Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Brown, Color.DarkBlue };

        Random rnd = new Random();

        // 创建ImageSharp图片对象
        using Image<Rgba32> image = new Image<Rgba32>(codeW, codeH);
        // 清空画布为白色
        image.Mutate(ctx => ctx.BackgroundColor(Color.Beige));

        // 1. 绘制噪线
        for (int i = 0; i < 5; i++)
        {
            // 随机起点和终点
            int x1 = rnd.Next(codeW);
            int y1 = rnd.Next(codeH);
            int x2 = rnd.Next(codeW);
            int y2 = rnd.Next(codeH);

            // 随机颜色
            Color lineColor = colors[rnd.Next(colors.Length)];

            // 绘制线条
            image.Mutate(ctx => ctx.DrawLine(
                Pens.Solid(lineColor, 1),
                new PointF(x1, y1),
                new PointF(x2, y2)));
        }

        // 2. 加载字体（自动查找系统中的 Times New Roman 字体）
        if (!SystemFonts.TryGet("Times New Roman", out var fontFamily))
        {
            if (!SystemFonts.Families.Any())
            {
                throw new ArgumentException("No font family found.");
            }

            // 降级使用默认字体（防止字体不存在）
            fontFamily = SystemFonts.Families.First();
        }

        Font font = fontFamily.CreateFont(fontSize, FontStyle.Regular);

        // 3. 绘制验证码字符
        var startX = 6;
        for (int i = 0; i < code.Length; i++)
        {
            // 绘制单个字符
            var i1 = i;
            // 随机字符颜色
            Color textColor = colors[rnd.Next(colors.Length)];

            // 字符位置（每个字符间隔18像素）
            float x = startX + i * 16;
            float y = 7;
            image.Mutate(ctx => ctx.DrawText(
                text: code[i1].ToString(),
                font: font,
                color: textColor,
                location: new PointF(x, y)));
        }

        // 4. 将图片写入内存流
        using MemoryStream ms = new MemoryStream();
        // 保存为PNG格式
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
