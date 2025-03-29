using System.Runtime.InteropServices;

namespace BundleReplacer.Helper;

internal class PInvoke
{
    [DllImport("dll/TexToolWrap.dll")]
    public static extern uint DecodeByCrunchUnity(IntPtr data, IntPtr buf, int mode, uint width, uint height, uint byteSize);

    [DllImport("dll/TexToolWrap.dll")]
    public static extern uint DecodeByPVRTexLib(IntPtr data, IntPtr buf, int mode, uint width, uint height);

    [DllImport("dll/TexToolWrap.dll")]
    public static extern uint EncodeByCrunchUnity(IntPtr data, ref int checkoutId, int mode, int level, uint width, uint height, uint ver, int mips);

    [DllImport("dll/TexToolWrap.dll")]
    public static extern bool PickUpAndFree(IntPtr outBuf, uint size, int id);

    [DllImport("dll/TexToolWrap.dll")]
    public static extern uint EncodeByPVRTexLib(IntPtr data, IntPtr buf, int mode, int level, uint width, uint height);

    [DllImport("dll/TexToolWrap.dll")]
    public static extern uint EncodeByISPC(IntPtr data, IntPtr buf, int mode, int level, uint width, uint height);
}
