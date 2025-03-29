using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BundleReplacer.Helper;
using Mono.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BundleReplacer.Commands;

public class Texture2DExportCommand : Command
{
    private string? bundlePath;
    private string? outputDir;

    public Texture2DExportCommand() : base("export-texture-2ds", "Export Texture2Ds from a bundle file")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the bundle file", v => bundlePath = v},
            {"o|output=", "The output directory", v => outputDir = v}
        };
    }


    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrWhiteSpace(bundlePath) || string.IsNullOrWhiteSpace(outputDir))
        {
            throw new ArgumentException("Missing required arguments");
        }
        ExtractBundle(bundlePath, outputDir);

        return 0;
    }

    public static void ExtractBundle(string bundlePath, string outputDir)
    {
        var manager = new AssetsManager();
        var bundle = manager.LoadBundleFile(bundlePath);
        var asset = manager.LoadAssetsFileFromBundle(bundle, 0);

        var texture2Ds = asset.file.GetAssetsOfType(AssetClassID.Texture2D);
        if (texture2Ds.Count == 0) { return; }

        Directory.CreateDirectory(outputDir);

        foreach (var texture2D in texture2Ds)
        {
            var texture2DInfo = manager.GetBaseField(asset, texture2D);

            var name = texture2DInfo["m_Name"].AsString.Replace('|', '_');
            var id = texture2D.PathId;
            var pngPath = $"{outputDir}/{name}-{id:X16}.png";

            var texFile = TextureFile.ReadTextureFile(texture2DInfo);
            if (texFile.m_Width == 0 && texFile.m_Height == 0)
            {
                continue;
            }
            TextureHelper.GetResSTexture(texFile, bundle);
            var bytes = texFile.pictureData;
            if (bytes is null || bytes.Length == 0)
            {
                continue;
            }

            var width = texFile.m_Width;
            var height = texFile.m_Height;
            var format = (TextureFormat)texFile.m_TextureFormat;

            var platformBlob = TextureHelper.GetPlatformBlob(texture2DInfo);
            var platform = asset.file.Metadata.TargetPlatform;
            if (platform == 38 && platformBlob is not null && platformBlob.Length != 0)
            {
                int originalWidth = width;
                int originalHeight = height;

                format = TextureHelper.GetCorrectedSwitchTextureFormat(format);
                int gobsPerBlock = Texture2DSwitchDeswizzler.GetSwitchGobsPerBlock(platformBlob);
                Size blockSize = Texture2DSwitchDeswizzler.TextureFormatToBlockSize(format);
                Size newSize = Texture2DSwitchDeswizzler.GetPaddedTextureSize(width, height, blockSize.Width, blockSize.Height, gobsPerBlock);
                width = newSize.Width;
                height = newSize.Height;

                byte[] decData = TextureEncoderDecoder.Decode(bytes, width, height, format);
                if (decData is null) { continue; }

                Image<Rgba32> image = Image.LoadPixelData<Rgba32>(decData, width, height);

                image = Texture2DSwitchDeswizzler.SwitchUnswizzle(image, blockSize, gobsPerBlock);
                if (originalWidth != width || originalHeight != height)
                {
                    image.Mutate(i => i.Crop(originalWidth, originalHeight));
                }
                image.Mutate(i => i.Flip(FlipMode.Vertical));
                image.SaveAsPng(pngPath);
            }
            else
            {
                byte[] decData = TextureEncoderDecoder.Decode(bytes, width, height, format);
                if (decData is null) { continue; }

                Image<Rgba32> image = Image.LoadPixelData<Rgba32>(decData, width, height);
                image.Mutate(i => i.Flip(FlipMode.Vertical));
                image.SaveAsPng(pngPath);
            }
        }
    }
}
