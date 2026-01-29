using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiTUIO.Provider;

namespace WiiTUIO.Output.Handlers
{
    // Wrapper: usa MouseHandler (SendInput) pero SOLO para "fpsmouse"
    public class FpsMouseWindowsHandler : ICursorHandler
    {
        private readonly MouseHandler inner = new MouseHandler();

        public bool setPosition(string key, CursorPos cursorPos)
        {
            if (!key.Equals("fpsmouse"))
                return false;

            return inner.setPosition(key, cursorPos);
        }

        public bool connect() => inner.connect();
        public bool disconnect() => inner.disconnect();
        public bool reset() => inner.reset();
        public bool startUpdate() => inner.startUpdate();
        public bool endUpdate() => inner.endUpdate();
    }
}
