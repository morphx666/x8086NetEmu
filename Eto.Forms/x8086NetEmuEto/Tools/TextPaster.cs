using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using x8086NetEmu;
using K = x8086NetEmu.Adapter.XEventArgs.Keys;

namespace x8086NetEmuEto.Tools {
    // Types host text into the guest through its keyboard, so it works in any video mode and with any program
    // that reads the keyboard. Characters a US keyboard can't type directly but that exist in code page 437
    // (accents, box drawing, ...) are entered with the BIOS Alt+keypad method.
    internal static class TextPaster {
        private static int busy;

        private static readonly Dictionary<char, (K Key, bool Shift)> keys = BuildKeyMap();

        private static Dictionary<char, (K, bool)> BuildKeyMap() {
            Dictionary<char, (K, bool)> m = [];
            for(char c = 'a'; c <= 'z'; c++) m[c] = (K.A + (c - 'a'), false);
            for(char c = 'A'; c <= 'Z'; c++) m[c] = (K.A + (c - 'A'), true);
            for(char c = '0'; c <= '9'; c++) m[c] = (K.D0 + (c - '0'), false);

            const string shiftedDigits = ")!@#$%^&*(";
            for(int i = 0; i < 10; i++) m[shiftedDigits[i]] = (K.D0 + i, true);

            void Pair(K key, char normal, char shifted) {
                m[normal] = (key, false);
                m[shifted] = (key, true);
            }
            Pair(K.OemMinus, '-', '_');
            Pair(K.Oemplus, '=', '+');
            Pair(K.OemOpenBrackets, '[', '{');
            Pair(K.OemCloseBrackets, ']', '}');
            Pair(K.OemPipe, '\\', '|');
            Pair(K.OemSemicolon, ';', ':');
            Pair(K.OemQuotes, '\'', '"');
            Pair(K.Oemtilde, '`', '~');
            Pair(K.Oemcomma, ',', '<');
            Pair(K.OemPeriod, '.', '>');
            Pair(K.OemQuestion, '/', '?');

            m[' '] = (K.Space, false);
            m['\t'] = (K.Tab, false);
            m['\n'] = (K.Enter, false);
            return m;
        }

        public static bool IsBusy => Volatile.Read(ref busy) == 1;

        public static void Paste(X8086 cpu, string text) {
            if(string.IsNullOrEmpty(text) || cpu?.VideoAdapter is not CGAAdapter adapter) return;
            if(Interlocked.Exchange(ref busy, 1) == 1) return; // One paste at a time

            text = text.Replace("\r\n", "\n").Replace('\r', '\n');

            Task.Run(() => {
                try {
                    foreach(char c in text) {
                        if(X8086.IsClosing || cpu.VideoAdapter != adapter) break;
                        WaitForKeyboardBuffer(cpu);

                        if(keys.TryGetValue(c, out (K Key, bool Shift) k)) {
                            if(k.Shift) Down(adapter, K.ShiftKey);
                            Down(adapter, k.Key);
                            Up(adapter, k.Key);
                            if(k.Shift) Up(adapter, K.ShiftKey);
                        } else if(CP437.TryFromUnicode(c, out byte code) && code != 0) {
                            // Alt + decimal code on the numeric keypad
                            Down(adapter, K.Menu);
                            foreach(char d in ((int)code).ToString()) {
                                K pad = K.NumPad0 + (d - '0');
                                Down(adapter, pad);
                                Up(adapter, pad);
                            }
                            Up(adapter, K.Menu);
                        } else {
                            continue; // Not representable in the guest
                        }

                        WaitForScancodes(cpu);
                    }
                } finally {
                    Volatile.Write(ref busy, 0);
                }
            });
        }

        private static void Down(CGAAdapter adapter, K key) {
            adapter.HandleKeyDown(adapter, new Adapter.XKeyEventArgs((int)key, 0));
            Thread.Sleep(2);
        }

        private static void Up(CGAAdapter adapter, K key) {
            adapter.HandleKeyUp(adapter, new Adapter.XKeyEventArgs((int)key, 0));
            Thread.Sleep(2);
        }

        // The keyboard controller only buffers 16 scancodes: let the guest consume a character's scancodes before
        // sending the next one (bounded, in case the guest has the keyboard interrupt masked)
        private static void WaitForScancodes(X8086 cpu) {
            Thread.Sleep(5); // Let the scheduler move the queued key events into the controller
            for(int i = 0; i < 50; i++) {
                if(cpu.PPI == null || cpu.PPI.PendingKeyData == 0 || cpu.IsPaused || cpu.DebugMode) return;
                Thread.Sleep(5);
            }
        }

        // Don't overrun the BIOS type-ahead buffer (16 entries at 40:1E, head at 40:1A, tail at 40:1C). Programs
        // that read the keyboard hardware directly never fill it, so the wait is bounded.
        private static void WaitForKeyboardBuffer(X8086 cpu) {
            byte[] mem = cpu.Memory;
            for(int i = 0; i < 100; i++) {
                int head = mem[0x41A] | (mem[0x41B] << 8);
                int tail = mem[0x41C] | (mem[0x41D] << 8);
                int pending = ((tail - head) & 0x1F) / 2;
                if(pending < 8 || cpu.IsPaused || cpu.DebugMode) return;
                Thread.Sleep(10);
            }
        }
    }
}
