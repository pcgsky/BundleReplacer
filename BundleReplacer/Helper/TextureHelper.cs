using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;

namespace BundleReplacer.Helper;

internal static class TextureHelper
{
    public static void GetResSTexture(TextureFile texFile, BundleFileInstance bundle)
    {
        var streamInfo = texFile.m_StreamData;
        var searchPath = streamInfo.path;
        if (string.IsNullOrWhiteSpace(searchPath))
        {
            return;
        }
        // if (searchPath.StartsWith("archive:/")) { searchPath = searchPath[9..]; }
        var fileName = Path.GetFileName(searchPath);
        foreach (var info in bundle.file.BlockAndDirInfo.DirectoryInfos)
        {
            if (info.Name == fileName)
            {
                var reader = bundle.file.DataReader;
                reader.Position = info.Offset + (long)streamInfo.offset;
                texFile.pictureData = reader.ReadBytes((int)streamInfo.size);
                texFile.m_StreamData.offset = 0;
                texFile.m_StreamData.size = 0;
                texFile.m_StreamData.path = "";
                return;
            }
        }
    }

    public static byte[]? GetPlatformBlob(AssetTypeValueField texBaseField)
    {
        var m_PlatformBlob = texBaseField["m_PlatformBlob"];
        return m_PlatformBlob.IsDummy ? null : m_PlatformBlob["Array"].AsByteArray;
    }

    public static TextureFormat GetCorrectedSwitchTextureFormat(TextureFormat format)
    {
        // in older versions of unity, rgb24 has a platformblob which shouldn't
        // be possible. it turns out in this case, the image is just rgba32.
        return format switch
        {
            TextureFormat.RGB24 => TextureFormat.RGBA32,
            TextureFormat.BGR24 => TextureFormat.BGRA32,
            _ => format,
        };
    }
    public static bool IsPo2(int n)
    {
        return n > 0 && ((n & (n - 1)) == 0);
    }
}
