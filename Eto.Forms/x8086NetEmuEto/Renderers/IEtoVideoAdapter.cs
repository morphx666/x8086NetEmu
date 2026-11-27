namespace x8086NetEmuEto.Renderers {
    // Services the Eto video presenters expose to the rest of the frontend (text copy, tools)
    internal interface IEtoVideoAdapter {
        EtoVideoInputController InputController { get; }

        // The last presented frame as 32-bit pixels (only equality between values is meaningful)
        int[] CaptureFrame(out int width, out int height);
    }
}
