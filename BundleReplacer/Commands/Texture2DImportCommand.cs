using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BundleReplacer.Helper;
using Mono.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using BundleHelper = BundleReplacer.Helper.BundleHelper;

namespace BundleReplacer.Commands;

public class Texture2DImportCommand : Command
{
    private string? bundlePath;
    private string? replaceDir;
    private string? outputPath;

    public Texture2DImportCommand() : base("import-texture-2ds", "Import Texture2Ds into a bundle file")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the original bundle file", v => bundlePath = v},
            {"r|replace=", "The path to the png files to replace", v => replaceDir = v},
            {"o|output=", "The output path", v => outputPath = v}
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrWhiteSpace(bundlePath) || string.IsNullOrWhiteSpace(replaceDir) || string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Missing required arguments");
        }
        ImportBundle(bundlePath, replaceDir, outputPath);

        return 0;
    }

    public static void ImportBundle(string bundlePath, string replaceDir, string outputPath)
    {
        var manager = new AssetsManager();
        var bundle = manager.LoadBundleFile(bundlePath);
        var asset = manager.LoadAssetsFileFromBundle(bundle, 0);

        var texture2Ds = asset.file.GetAssetsOfType(AssetClassID.Texture2D);
        if (texture2Ds.Count == 0) { return; }

        bool changed = false;

        foreach (var texture2D in texture2Ds)
        {
            var texture2DInfo = manager.GetBaseField(asset, texture2D);

            var name = texture2DInfo["m_Name"].AsString.Replace('|', '_');
            var id = texture2D.PathId;
            var pngPath = $"{replaceDir}/{name}-{id:X16}.png";

            if (!File.Exists(pngPath)) { continue; }
            using Image<Rgba32> image = Image.Load<Rgba32>(pngPath);

            var width = image.Width;
            var height = image.Height;

            var format = (TextureFormat)texture2DInfo["m_TextureFormat"].AsInt;
            var platformBlob = TextureHelper.GetPlatformBlob(texture2DInfo);
            var platform = asset.file.Metadata.TargetPlatform;

            int mips = texture2DInfo["m_MipCount"].IsDummy ? 1 : texture2DInfo["m_MipCount"].AsInt;
            byte[] encData = [];
            if (platform == 38 && platformBlob != null && platformBlob.Length != 0)
            {
                int paddedWidth, paddedHeight;

                format = TextureHelper.GetCorrectedSwitchTextureFormat(format);
                int gobsPerBlock = Texture2DSwitchDeswizzler.GetSwitchGobsPerBlock(platformBlob);
                Size blockSize = Texture2DSwitchDeswizzler.TextureFormatToBlockSize(format);
                Size newSize = Texture2DSwitchDeswizzler.GetPaddedTextureSize(width, height, blockSize.Width, blockSize.Height, gobsPerBlock);
                paddedWidth = newSize.Width;
                paddedHeight = newSize.Height;

                image.Mutate(i => i.Resize(new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.BottomLeft,
                    PadColor = Color.Fuchsia, // full alpha?
                    Size = newSize
                }).Flip(FlipMode.Vertical));

                Image<Rgba32> swizzledImage = Texture2DSwitchDeswizzler.SwitchSwizzle(image, blockSize, gobsPerBlock);

                encData = TextureEncoderDecoder.Encode(swizzledImage, paddedWidth, paddedHeight, format);
            }
            else
            {
                // can't make mipmaps from this image
                if (mips > 1 && (width != height || !TextureHelper.IsPo2(width)))
                {
                    mips = 1;
                }

                image.Mutate(i => i.Flip(FlipMode.Vertical));
                encData = TextureEncoderDecoder.Encode(image, width, height, format, 5, mips);
            }

            var m_StreamData = texture2DInfo["m_StreamData"];
            m_StreamData["offset"].AsInt = 0;
            m_StreamData["size"].AsInt = 0;
            m_StreamData["path"].AsString = "";

            if (!texture2DInfo["m_MipCount"].IsDummy) { texture2DInfo["m_MipCount"].AsInt = mips; }

            texture2DInfo["m_TextureFormat"].AsInt = (int)format;
            // todo: size for multi image textures
            texture2DInfo["m_CompleteImageSize"].AsInt = encData.Length;

            texture2DInfo["m_Width"].AsInt = width;
            texture2DInfo["m_Height"].AsInt = height;

            var image_data = texture2DInfo["image data"];
            image_data.Value.ValueType = AssetValueType.ByteArray;
            image_data.TemplateField.ValueType = AssetValueType.ByteArray;
            image_data.AsByteArray = encData;

            var bytes = texture2DInfo.WriteToByteArray();
            texture2D.Replacer = new ContentReplacerFromBuffer(bytes);

            changed = true;
        }

        if (!changed) { return; }

        bundle.file.BlockAndDirInfo.DirectoryInfos[0].SetNewData(asset.file);
        BundleHelper.CompressBundle(outputPath, manager, bundle);
    }
}
