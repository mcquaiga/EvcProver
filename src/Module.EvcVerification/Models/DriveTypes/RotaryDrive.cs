namespace Module.EvcVerification.Models.DriveTypes
{
    public class RotaryDrive : IDriveType
    {
        public RotaryDrive(EvcVerification instrument)
        {
            Instrument = instrument;
            Meter = new MeterTest(Instrument);
        }

        public EvcVerification Instrument { get; set; }
        public MeterTest Meter { get; set; }

        public string Discriminator => Drives.Rotary;

        public bool HasPassed => Meter.MeterDisplacementHasPassed;

        public Energy Energy => null;

        public decimal? UnCorrectedInputVolume(decimal appliedInput)
        {
            return Meter.MeterDisplacement * appliedInput;
        }

        public int MaxUncorrectedPulses()
        {
            if (Instrument.UnCorrectedMultiplier() == 10)
                return Meter.MeterIndex.UnCorPulsesX10;

            if (Instrument.UnCorrectedMultiplier() == 100)
                return Meter.MeterIndex.UnCorPulsesX100;

            return 10; //Low standard number if we can't find anything
        }
    }
}