using Dalamud.Hooking;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TooltipMBData {
    public class Plugin : IDalamudPlugin {
        private bool disposedValue;

        public string Name => "Tooltip MB Data";

        public DalamudPluginInterface Interface { get; private set; }
        private GameFunctions Functions { get; set; }
        private readonly IntPtr alloc = Marshal.AllocHGlobal(4096);
        private Hook<TooltipDelegate> tooltipHook;

        private unsafe delegate IntPtr TooltipDelegate(IntPtr a1, uint** a2, byte*** a3);

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.Interface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");
            this.Functions = new GameFunctions(this);
            this.SetUpHook();
        }

        protected virtual void Dispose(bool disposing) {
            if (!this.disposedValue) {
                if (disposing) {
                    this.tooltipHook?.Dispose();
                    Marshal.FreeHGlobal(this.alloc);
                }

                this.disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SetUpHook() {
            IntPtr tooltipPtr = this.Interface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 50 48 8B 42 ??");
            if (tooltipPtr == IntPtr.Zero) {
                throw new ApplicationException("Could not set up tooltip hook because of null pointer");
            }
            unsafe {
                this.tooltipHook = new Hook<TooltipDelegate>(tooltipPtr, new TooltipDelegate(this.OnTooltip));
            }
            this.tooltipHook.Enable();
        }

        private unsafe IntPtr OnTooltip(IntPtr a1, uint** a2, byte*** a3) {
            // this can be replaced with a mid-func hook when reloaded hooks is in dalamud
            // but for now, do the same logic the func does and replace the text after
            uint* v3 = *(a2 + 4);
            uint v9 = *(v3 + 4);

            if ((v9 & 2) == 0) {
                goto Return;
            }

            ulong itemId = this.Interface.Framework.Gui.HoveredItem;
            if (itemId > 2_000_000) {
                goto Return;
            } else if (itemId > 1_000_000) {
                itemId -= 1_000_000;
            }

            //Item item = this.Interface.Data.GetExcelSheet<Item>().GetRow((uint)itemId);

            //if (item == null) {
            //    goto Return;
            //}

            // get the pointer to the text
            byte** startPtr = *(a3 + 4) + 13;
            // get the text pointer
            byte* start = *startPtr;

            // work around function being called twice
            if (start == (byte*)this.alloc) {
                goto Return;
            }

            string overwrite;

            //ItemAction action = item.ItemAction?.Value;

            //if (!ActionTypeExt.IsValidAction(action)) {
            //    goto Return;
            //}

            // get the text
            overwrite = ReadString(start);

            // generate our replacement text
            // TODO - ButchMonkey - Get data from Universallis api 
            // Example - https://universalis.app/api/Zodiark/14945

            //this.AppendIfAcquired(ref overwrite, item);
            this.AppendMarketPrice(ref overwrite, itemId);

            // write our replacement text into our own managed memory (4096 bytes)
            WriteString((byte*)this.alloc, overwrite, true);

            // overwrite the original pointer with our own
            *startPtr = (byte*)this.alloc;

        Return:
            return this.tooltipHook.Original(a1, a2, a3);
        }

        //private void AppendMarketPrice(ref string txt, Item item, ulong itemId) {
        private void AppendMarketPrice(ref string txt, ulong itemId) {
            string marketPrice;
            string lowest;
            string average;
            string highest;
            string seperator;
            switch (this.Interface.ClientState.ClientLanguage) {
                case Dalamud.ClientLanguage.English:
                default:
                    marketPrice = "Market Price";
                    lowest = "Lowest";
                    average = "Average";
                    highest = "Highest";
                    seperator = "|";
                    break;
                case Dalamud.ClientLanguage.French:
                    marketPrice = "Market Price";
                    lowest = "Lowest";
                    average = "Average";
                    highest = "Highest";
                    seperator = "|";
                    break;
                case Dalamud.ClientLanguage.German:
                    marketPrice = "Market Price";
                    lowest = "Lowest";
                    average = "Average";
                    highest = "Highest";
                    seperator = "|";
                    break;
                case Dalamud.ClientLanguage.Japanese:
                    marketPrice = "Market Price";
                    lowest = "Lowest";
                    average = "Average";
                    highest = "Highest";
                    seperator = "|";
                    break;
            }

            //string has = this.Functions.HasAcquired(item) ? yes : no;


            string priceLowest = "1g";
            string priceAverage = "5g";
            string priceHighest = "10g";

            txt = $"{txt}\n{marketPrice}: {lowest}: {priceLowest} {seperator} {average}: {priceAverage} {seperator} {highest}: {priceHighest} {seperator}\n ItemID: {itemId}";

            //if (name != null) {
            //    txt = $"{txt}\n{marketPrice}: {lowest}: {priceLowest} {seperator} {average}: {priceAverage} {seperator} {highest}: {priceHighest} {seperator}";
            //} else {
            //    txt = $"{txt}\n{acquired}{colon}{has}";
            //}
        }

        private unsafe static string ReadString(byte* ptr) {
            int offset = 0;
            while (true) {
                byte b = *(ptr + offset);
                if (b == 0) {
                    break;
                }
                offset += 1;
            }
            return Encoding.UTF8.GetString(ptr, offset);
        }

        private unsafe static void WriteString(byte* dst, string s, bool finalise = false) {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            for (int i = 0; i < bytes.Length; i++) {
                *(dst + i) = bytes[i];
            }
            if (finalise) {
                *(dst + bytes.Length) = 0;
            }
        }
    }
}
