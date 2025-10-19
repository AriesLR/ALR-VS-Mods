using instruments;
using Vintagestory.API.Common;

namespace instrumentscovertpack
{
    public class instrumentscovertpackModSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass("banjo", typeof(BanjoItem));
            api.RegisterItemClass("bassguitar", typeof(BassGuitarItem));
            api.RegisterItemClass("brightpiano", typeof(BrightPianoItem));
            api.RegisterItemClass("electricbassguitar", typeof(ElectricBassGuitarItem));
            api.RegisterItemClass("flute", typeof(FluteItem));
            api.RegisterItemClass("leadguitar", typeof(LeadGuitarItem));
            api.RegisterItemClass("nylonguitar", typeof(NylonGuitarItem));
            api.RegisterItemClass("ocarina", typeof(OcarinaItem));
            api.RegisterItemClass("overdriveguitar", typeof(OverdriveGuitarItem));
            api.RegisterItemClass("squarewave", typeof(SquareWaveItem));
        }
    }

    public class BanjoItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = "banjo";
            animation = "holdbothhandslarge";
            Definitions.GetInstance().AddInstrumentType(instrument, animation);
        }
    }

    public class BassGuitarItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = "bassguitar";
            animation = "holdbothhandslarge";
            Definitions.GetInstance().AddInstrumentType(instrument, animation);
        }
    }

    public class BrightPianoItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = "brightpiano";
            animation = "holdbothhandslarge";
            Definitions.GetInstance().AddInstrumentType(instrument, animation);
        }
    }

    public class ElectricBassGuitarItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = "electricbassguitar";
            animation = "holdbothhandslarge";
            Definitions.GetInstance().AddInstrumentType(instrument, animation);
        }
    }

    public class FluteItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = "flute";
            animation = "holdbothhands";
            Definitions.GetInstance().AddInstrumentType(instrument, animation);
        }
    }

    public class LeadGuitarItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = "leadguitar";
            animation = "holdbothhandslarge";
            Definitions.GetInstance().AddInstrumentType(instrument, animation);
        }
    }

    public class NylonGuitarItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = "nylonguitar";
            animation = "holdbothhandslarge";
            Definitions.GetInstance().AddInstrumentType(instrument, animation);
        }
    }

    public class OcarinaItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = "ocarina";
            animation = "holdbothhands";
            Definitions.GetInstance().AddInstrumentType(instrument, animation);
        }
    }

    public class OverdriveGuitarItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = "overdriveguitar";
            animation = "holdbothhandslarge";
            Definitions.GetInstance().AddInstrumentType(instrument, animation);
        }
    }

    public class SquareWaveItem : InstrumentItem
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            instrument = "squarewave";
            animation = "holdbothhandslarge";
            Definitions.GetInstance().AddInstrumentType(instrument, animation);
        }
    }
}