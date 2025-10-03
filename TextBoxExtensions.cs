using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GuildedDiscordMigrator
{
    public static class TextBoxExtensions
    {
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        public static void SetPlaceholder(this TextBox textBox, string placeholder)
        {
            if (textBox.IsHandleCreated)
            {
                SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholder);
            }
            else
            {
                textBox.HandleCreated += (s, e) => 
                {
                    SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholder);
                };
            }
        }
    }
}