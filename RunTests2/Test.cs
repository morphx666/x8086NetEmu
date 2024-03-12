using System;

namespace RunTests2 {
    internal class Test {
        public string name { get; set; }
        public byte[] bytes { get; set; }
        public State initial { get; set; }
        public State final { get; set; }
        //public Cycle[][] cycles { get; set; }
        public string test_hash { get; set; }
    }

    internal class State {
        public Registers regs { get; set; }
        public int[][] ram { get; set; }
        public int[] queue { get; set; }
    }

    internal class Registers {
        public UInt16 ax { get; set; }
        public UInt16 bx { get; set; }
        public UInt16 cx { get; set; }
        public UInt16 dx { get; set; }
        public UInt16 si { get; set; }
        public UInt16 di { get; set; }
        public UInt16 bp { get; set; }
        public UInt16 sp { get; set; }
        public UInt16 ip { get; set; }
        public UInt16 cs { get; set; }
        public UInt16 ds { get; set; }
        public UInt16 es { get; set; }
        public UInt16 ss { get; set; }
        public UInt16 flags { get; set; }
    }

    public class Cycle {
        public string ALE { get; set; }
        public int AddressLatch { get; set; }
        public string SegmentStatus { get; set; }
        public string MemoryStatus { get; set; }
        public string IOStatus { get; set; }
        public int DataBus { get; set; }
        public string BusStatus { get; set; }
        public string TState { get; set; }
        public string QueueOperationStatus { get; set; }
        public int QueueByteRead { get; set; }
    }
}
