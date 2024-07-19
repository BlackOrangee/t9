using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace t9
{
    public class AlwaysFocusedTextBox : TextBox
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool HideCaret(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowCaret(IntPtr hWnd);

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            ShowCaret(this.Handle);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            HideCaret(this.Handle);
            this.Focus();
        }
    }
}
