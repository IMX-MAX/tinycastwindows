using Avalonia.Input.Platform;
using ComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Tinycast.Windows;

static class SelectedTextCapture
{
    public static async Task<string?> CaptureAsync(IClipboard? clipboard)
    {
        if (!OperatingSystem.IsWindows() || clipboard is null) return null;
        NativeMethods.OleGetClipboard(out ComDataObject? snapshot);
        var sequence = NativeMethods.GetClipboardSequenceNumber();
        try
        {
            NativeMethods.keybd_event(NativeMethods.VkControl, 0, 0, 0);
            NativeMethods.keybd_event(0x43, 0, 0, 0);
            NativeMethods.keybd_event(0x43, 0, NativeMethods.KeyeventfKeyup, 0);
            NativeMethods.keybd_event(
                NativeMethods.VkControl, 0, NativeMethods.KeyeventfKeyup, 0);
            await Task.Delay(90);
            if (NativeMethods.GetClipboardSequenceNumber() == sequence) return null;
            return await clipboard.GetTextAsync();
        }
        catch
        {
            return null;
        }
        finally
        {
            if (snapshot is not null)
            {
                NativeMethods.OleSetClipboard(snapshot);
                NativeMethods.OleFlushClipboard();
                if (System.Runtime.InteropServices.Marshal.IsComObject(snapshot))
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(snapshot);
            }
        }
    }
}
