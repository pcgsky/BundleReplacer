using AssetsTools.NET.Extra;

namespace BundleReplacer.Helper;

internal class BlankBlock(int start, int end)
{
    public int Start = start;
    public int End = end;

    public int Length => End - Start;
}

internal class StreamWrapper
{
    public MemoryStream Stream = new();
    public List<BlankBlock> BlankBlocks = [];

    public long Position
    {
        set => Stream.Position = value;
        get => Stream.Position;
    }

    public long Length => Stream.Length;

    public void InsertBlankBlock(int start, int end)
    {
        if (BlankBlocks.Count == 0)
        {
            BlankBlocks.Add(new BlankBlock(start, end));
            return;
        }

        int insertIndex = BinarySearchInsertPosition(start);

        bool mergeWithPrev = insertIndex > 0 && BlankBlocks[insertIndex - 1].End == start;
        bool mergeWithNext = insertIndex < BlankBlocks.Count && BlankBlocks[insertIndex].Start == end;

        if (mergeWithPrev && mergeWithNext)
        {
            BlankBlocks[insertIndex - 1].End = BlankBlocks[insertIndex].End;
            BlankBlocks.RemoveAt(insertIndex);
        }
        else if (mergeWithPrev)
        {
            BlankBlocks[insertIndex - 1].End = end;
        }
        else if (mergeWithNext)
        {
            BlankBlocks[insertIndex].Start = start;
        }
        else
        {
            BlankBlocks.Insert(insertIndex, new BlankBlock(start, end));
        }
    }

    public int InsertBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return (int)Stream.Position;

        int offset;
        var bytesLength = bytes.Length;

        for (int i = 0; i < BlankBlocks.Count; i++)
        {
            var block = BlankBlocks[i];
            if (block.Length >= bytesLength)
            {
                offset = block.Start;
                Stream.Position = offset;
                Stream.Write(bytes);

                if (block.Length == bytesLength)
                {
                    BlankBlocks.RemoveAt(i);
                }
                else
                {
                    block.Start += bytesLength;
                }
                return offset;
            }
        }

        offset = (int)Stream.Length;
        Stream.Position = offset;
        Stream.Write(bytes);
        return offset;
    }

    private int BinarySearchInsertPosition(int start)
    {
        int left = 0;
        int right = BlankBlocks.Count;

        while (left < right)
        {
            int mid = left + (right - left) / 2;
            if (BlankBlocks[mid].Start < start)
            {
                left = mid + 1;
            }
            else
            {
                right = mid;
            }
        }

        return left;
    }
}


internal static class ResourceReplaceHelper
{
    public static int Replace(string resourceFileName, BundleFileInstance bundle, Dictionary<string, StreamWrapper> streams, int originalSize, int originalOffset, byte[] newBytes)
    {
        if (!streams.TryGetValue(resourceFileName, out StreamWrapper? streamWrapper))
        {
            streamWrapper = new();
            foreach (var info in bundle.file.BlockAndDirInfo.DirectoryInfos)
            {
                if (info.Name != resourceFileName) { continue; }
                var reader = bundle.file.DataReader;
                reader.Position = info.Offset;
                streamWrapper.Stream.Write(reader.ReadBytes((int)info.DecompressedSize));
                break;
            }
            streams[resourceFileName] = streamWrapper;
        }

        streamWrapper.InsertBlankBlock(originalOffset, originalOffset + originalSize);
        return streamWrapper.InsertBytes(newBytes);
    }
}
