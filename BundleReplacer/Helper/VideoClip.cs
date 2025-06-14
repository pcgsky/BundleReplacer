using AssetsTools.NET;
using AssetsTools.NET.Extra;
using MediaInfo;

namespace BundleReplacer.Helper;

internal static class VideoClip
{
    public static bool Export(string outputDir, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo videoClip)
    {

        var videoClipInfo = manager.GetBaseField(asset, videoClip);
        var moviePath = $"{outputDir}/{GetFileName(videoClip, videoClipInfo)}";

        var resource = videoClipInfo["m_ExternalResources"];
        var resourcePath = resource["m_Source"].AsString;
        var offset = resource["m_Offset"].AsULong;
        var size = resource["m_Size"].AsULong;

        var resourceFileName = Path.GetFileName(resourcePath);
        byte[] movieBytes = [];
        foreach (var info in bundle.file.BlockAndDirInfo.DirectoryInfos)
        {
            if (info.Name != resourceFileName) { continue; }
            var reader = bundle.file.DataReader;
            reader.Position = info.Offset + (long)offset;
            movieBytes = reader.ReadBytes((int)size);
            break;
        }
        if (movieBytes.Length == 0) { return false; }

        Directory.CreateDirectory(outputDir);
        File.WriteAllBytes(moviePath, movieBytes);

        return true;
    }

    public static bool Import(string replaceDir, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo videoClip, Dictionary<string, StreamWrapper> resourceStreams)
    {
        var videoClipInfo = manager.GetBaseField(asset, videoClip);
        var moviePath = $"{replaceDir}/{GetFileName(videoClip, videoClipInfo)}";

        if (!File.Exists(moviePath)) { return false; }
        using (var movieInfo = new MediaInfo.MediaInfo())
        {
            movieInfo.Open(moviePath);

            videoClipInfo["Width"].AsUInt = uint.Parse(movieInfo.Get(StreamKind.Video, 0, "Width"));
            videoClipInfo["Height"].AsUInt = uint.Parse(movieInfo.Get(StreamKind.Video, 0, "Height"));
            videoClipInfo["m_FrameRate"].AsDouble = double.Parse(movieInfo.Get(StreamKind.Video, 0, "FrameRate"));
            videoClipInfo["m_FrameCount"].AsULong = ulong.Parse(movieInfo.Get(StreamKind.Video, 0, "FrameCount"));
        }

        var resource = videoClipInfo["m_ExternalResources"];
        var resourcePath = resource["m_Source"].AsString;

        var resourceFileName = Path.GetFileName(resourcePath);
        var movieBytes = File.ReadAllBytes(moviePath);

        var offset = ResourceReplaceHelper.Replace(resourceFileName, bundle, resourceStreams, (int)resource["m_Size"].AsULong, (int)resource["m_Offset"].AsULong, movieBytes);
        resource["m_Offset"].AsULong = (ulong)offset;
        resource["m_Size"].AsULong = (ulong)movieBytes.Length;

        var bytes = videoClipInfo.WriteToByteArray();
        videoClip.Replacer = new ContentReplacerFromBuffer(bytes);

        return true;
    }

    public static string GetFileName(AssetFileInfo videoClip, AssetTypeValueField videoClipInfo)
    {
        var name = videoClipInfo["m_Name"].AsString;

        var id = videoClip.PathId;
        var ext = Path.GetExtension(videoClipInfo["m_OriginalPath"].AsString);
        var moviePath = $"{BundleReplaceHelper.EscapeFileName(name)}-{id:X16}{ext}";

        return moviePath;
    }
}
