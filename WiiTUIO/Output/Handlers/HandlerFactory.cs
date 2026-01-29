using Nefarius.ViGEm.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiTUIO.Output;
using WiiTUIO.Output.Handlers.Xinput;
using VMultiDllWrapper;

namespace WiiTUIO.Output.Handlers
{
    public class HandlerFactory
    {
        private Dictionary<long, List<IOutputHandler>> outputHandlers;

        public HandlerFactory()
        {
            outputHandlers = new Dictionary<long, List<IOutputHandler>>();
        }

        private List<IOutputHandler> createOutputHandlers(long id)
        {
            VMulti vmulti = new VMulti();

            List<IOutputHandler> all = new List<IOutputHandler>();

            IOutputHandler keyboardHandler = new KeyboardHandler();
            IOutputHandler mouseHandler = new MouseHandler();
            bool vmultiOk = vmulti.connect((int)id);
            if (vmultiOk)
            {
                keyboardHandler = new VmultiKeyboardHandler(vmulti);
                mouseHandler = new VmultiMouseHandler(vmulti);
            }

            all.Add(keyboardHandler);

            // Si VMulti está activo, inserta ANTES el handler de FPS por Windows
            if (vmultiOk)
            {
                all.Add(new FpsMouseWindowsHandler());
            }

            all.Add(mouseHandler);

            ViGEmHandler gamepadHandler = new ViGEmHandler(id);
            if (gamepadHandler.isAvailable) all.Add(gamepadHandler);
            all.Add(new WiimoteHandler());
            all.Add(new CursorHandler(id));

            return all;
        }

        public List<IOutputHandler> getOutputHandlers(long id)
        {
            List<IOutputHandler> handlerList;
            if (outputHandlers.TryGetValue(id, out handlerList))
            {
                return handlerList;
            }
            else
            {
                handlerList = this.createOutputHandlers(id);
                return handlerList;
            }
        }

    }
}
