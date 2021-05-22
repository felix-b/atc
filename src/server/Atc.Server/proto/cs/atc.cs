// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: atc.proto
// </auto-generated>

#region Designer generated code
#pragma warning disable CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
namespace AtcProto
{

    [global::ProtoBuf.ProtoContract()]
    public partial class ClientToServer : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"id")]
        public ulong Id { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"sent_at", DataFormat = global::ProtoBuf.DataFormat.WellKnown)]
        public global::System.DateTime? SentAt { get; set; }

        [global::ProtoBuf.ProtoMember(101)]
        public Connect connect
        {
            get => __pbn__payload.Is(101) ? ((Connect)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(101, value);
        }
        public bool ShouldSerializeconnect() => __pbn__payload.Is(101);
        public void Resetconnect() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 101);

        private global::ProtoBuf.DiscriminatedUnionObject __pbn__payload;

        [global::ProtoBuf.ProtoMember(102)]
        public QueryAirport query_airport
        {
            get => __pbn__payload.Is(102) ? ((QueryAirport)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(102, value);
        }
        public bool ShouldSerializequery_airport() => __pbn__payload.Is(102);
        public void Resetquery_airport() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 102);

        [global::ProtoBuf.ProtoMember(103)]
        public CreateAircraft create_aircraft
        {
            get => __pbn__payload.Is(103) ? ((CreateAircraft)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(103, value);
        }
        public bool ShouldSerializecreate_aircraft() => __pbn__payload.Is(103);
        public void Resetcreate_aircraft() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 103);

        [global::ProtoBuf.ProtoMember(104)]
        public UpdateAircraftSituation update_aircraft_situation
        {
            get => __pbn__payload.Is(104) ? ((UpdateAircraftSituation)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(104, value);
        }
        public bool ShouldSerializeupdate_aircraft_situation() => __pbn__payload.Is(104);
        public void Resetupdate_aircraft_situation() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 104);

        [global::ProtoBuf.ProtoMember(105)]
        public RemoveAircraft remove_aircraft
        {
            get => __pbn__payload.Is(105) ? ((RemoveAircraft)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(105, value);
        }
        public bool ShouldSerializeremove_aircraft() => __pbn__payload.Is(105);
        public void Resetremove_aircraft() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 105);

        [global::ProtoBuf.ProtoMember(106)]
        public QueryTaxiPath query_taxi_path
        {
            get => __pbn__payload.Is(106) ? ((QueryTaxiPath)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(106, value);
        }
        public bool ShouldSerializequery_taxi_path() => __pbn__payload.Is(106);
        public void Resetquery_taxi_path() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 106);

        [global::ProtoBuf.ProtoContract()]
        public partial class Connect : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"token")]
            [global::System.ComponentModel.DefaultValue("")]
            public string Token { get; set; } = "";

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class QueryAirport : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"icao_code")]
            [global::System.ComponentModel.DefaultValue("")]
            public string IcaoCode { get; set; } = "";

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class QueryTaxiPath : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"airport_icao")]
            [global::System.ComponentModel.DefaultValue("")]
            public string AirportIcao { get; set; } = "";

            [global::ProtoBuf.ProtoMember(2, Name = @"aircraft_model_icao")]
            [global::System.ComponentModel.DefaultValue("")]
            public string AircraftModelIcao { get; set; } = "";

            [global::ProtoBuf.ProtoMember(3, Name = @"from_point")]
            public GeoPoint FromPoint { get; set; }

            [global::ProtoBuf.ProtoMember(4, Name = @"to_point")]
            public GeoPoint ToPoint { get; set; }

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class CreateAircraft : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"aircraft")]
            public Aircraft Aircraft { get; set; }

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class UpdateAircraftSituation : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"aircraft_id")]
            public int AircraftId { get; set; }

            [global::ProtoBuf.ProtoMember(2, Name = @"situation")]
            public Aircraft.Situation Situation { get; set; }

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class RemoveAircraft : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"aircraft_id")]
            public int AircraftId { get; set; }

        }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class ServerToClient : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(2, Name = @"id")]
        public ulong Id { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"reply_to_request_id")]
        public ulong ReplyToRequestId { get; set; }

        [global::ProtoBuf.ProtoMember(4, Name = @"sent_at", DataFormat = global::ProtoBuf.DataFormat.WellKnown)]
        public global::System.DateTime? SentAt { get; set; }

        [global::ProtoBuf.ProtoMember(5, Name = @"request_sent_at", DataFormat = global::ProtoBuf.DataFormat.WellKnown)]
        public global::System.DateTime? RequestSentAt { get; set; }

        [global::ProtoBuf.ProtoMember(6, Name = @"request_received_at", DataFormat = global::ProtoBuf.DataFormat.WellKnown)]
        public global::System.DateTime? RequestReceivedAt { get; set; }

        [global::ProtoBuf.ProtoMember(1101)]
        public ReplyConnect reply_connect
        {
            get => __pbn__payload.Is(1101) ? ((ReplyConnect)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(1101, value);
        }
        public bool ShouldSerializereply_connect() => __pbn__payload.Is(1101);
        public void Resetreply_connect() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 1101);

        private global::ProtoBuf.DiscriminatedUnionObject __pbn__payload;

        [global::ProtoBuf.ProtoMember(1102)]
        public ReplyQueryAirport reply_query_airport
        {
            get => __pbn__payload.Is(1102) ? ((ReplyQueryAirport)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(1102, value);
        }
        public bool ShouldSerializereply_query_airport() => __pbn__payload.Is(1102);
        public void Resetreply_query_airport() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 1102);

        [global::ProtoBuf.ProtoMember(1103)]
        public ReplyCreateAircraft reply_create_aircraft
        {
            get => __pbn__payload.Is(1103) ? ((ReplyCreateAircraft)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(1103, value);
        }
        public bool ShouldSerializereply_create_aircraft() => __pbn__payload.Is(1103);
        public void Resetreply_create_aircraft() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 1103);

        [global::ProtoBuf.ProtoMember(1106)]
        public ReplyQueryTaxiPath reply_query_taxi_path
        {
            get => __pbn__payload.Is(1106) ? ((ReplyQueryTaxiPath)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(1106, value);
        }
        public bool ShouldSerializereply_query_taxi_path() => __pbn__payload.Is(1106);
        public void Resetreply_query_taxi_path() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 1106);

        [global::ProtoBuf.ProtoMember(201)]
        public NotifyAircraftCreated notify_aircraft_created
        {
            get => __pbn__payload.Is(201) ? ((NotifyAircraftCreated)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(201, value);
        }
        public bool ShouldSerializenotify_aircraft_created() => __pbn__payload.Is(201);
        public void Resetnotify_aircraft_created() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 201);

        [global::ProtoBuf.ProtoMember(202)]
        public NotifyAircraftSituationUpdated notify_aircraft_situation_updated
        {
            get => __pbn__payload.Is(202) ? ((NotifyAircraftSituationUpdated)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(202, value);
        }
        public bool ShouldSerializenotify_aircraft_situation_updated() => __pbn__payload.Is(202);
        public void Resetnotify_aircraft_situation_updated() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 202);

        [global::ProtoBuf.ProtoMember(203)]
        public NotifyAircraftRemoved notify_aircraft_removed
        {
            get => __pbn__payload.Is(203) ? ((NotifyAircraftRemoved)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(203, value);
        }
        public bool ShouldSerializenotify_aircraft_removed() => __pbn__payload.Is(203);
        public void Resetnotify_aircraft_removed() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 203);

        [global::ProtoBuf.ProtoMember(3001)]
        public FaultDeclined fault_declined
        {
            get => __pbn__payload.Is(3001) ? ((FaultDeclined)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(3001, value);
        }
        public bool ShouldSerializefault_declined() => __pbn__payload.Is(3001);
        public void Resetfault_declined() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 3001);

        [global::ProtoBuf.ProtoMember(3002)]
        public FaultNotFound fault_not_found
        {
            get => __pbn__payload.Is(3002) ? ((FaultNotFound)__pbn__payload.Object) : default;
            set => __pbn__payload = new global::ProtoBuf.DiscriminatedUnionObject(3002, value);
        }
        public bool ShouldSerializefault_not_found() => __pbn__payload.Is(3002);
        public void Resetfault_not_found() => global::ProtoBuf.DiscriminatedUnionObject.Reset(ref __pbn__payload, 3002);

        [global::ProtoBuf.ProtoContract()]
        public partial class FaultDeclined : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"message")]
            [global::System.ComponentModel.DefaultValue("")]
            public string Message { get; set; } = "";

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class FaultNotFound : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"message")]
            [global::System.ComponentModel.DefaultValue("")]
            public string Message { get; set; } = "";

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class ReplyConnect : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(2, Name = @"server_banner")]
            [global::System.ComponentModel.DefaultValue("")]
            public string ServerBanner { get; set; } = "";

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class ReplyCreateAircraft : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"created_aircraft_id")]
            public int CreatedAircraftId { get; set; }

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class ReplyQueryAirport : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"airport")]
            public Airport Airport { get; set; }

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class ReplyQueryTaxiPath : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"success")]
            public bool Success { get; set; }

            [global::ProtoBuf.ProtoMember(2, Name = @"taxi_path")]
            public TaxiPath TaxiPath { get; set; }

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class NotifyAircraftCreated : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"aircraft")]
            public Aircraft Aircraft { get; set; }

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class NotifyAircraftSituationUpdated : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"airctaft_id")]
            public int AirctaftId { get; set; }

            [global::ProtoBuf.ProtoMember(2, Name = @"situation")]
            public Aircraft.Situation Situation { get; set; }

        }

        [global::ProtoBuf.ProtoContract()]
        public partial class NotifyAircraftRemoved : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"airctaft_id")]
            public int AirctaftId { get; set; }

        }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class GeoPoint : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"lat")]
        public double Lat { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"lon")]
        public double Lon { get; set; }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class GeoPolygon : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"edges")]
        public global::System.Collections.Generic.List<GeoEdge> Edges { get; } = new global::System.Collections.Generic.List<GeoEdge>();

        [global::ProtoBuf.ProtoContract()]
        public partial class GeoEdge : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"type")]
            public GeoEdgeType Type { get; set; }

            [global::ProtoBuf.ProtoMember(2, Name = @"from_point")]
            public GeoPoint FromPoint { get; set; }

        }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class Vector3d : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"lat")]
        public double Lat { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"lon")]
        public double Lon { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"alt")]
        public double Alt { get; set; }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class Attitude : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"heading")]
        public float Heading { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"pitch")]
        public float Pitch { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"roll")]
        public float Roll { get; set; }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class Airport : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"icao")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Icao { get; set; } = "";

        [global::ProtoBuf.ProtoMember(2, Name = @"location")]
        public GeoPoint Location { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"runways")]
        public global::System.Collections.Generic.List<Runway> Runways { get; } = new global::System.Collections.Generic.List<Runway>();

        [global::ProtoBuf.ProtoMember(4, Name = @"parking_stands")]
        public global::System.Collections.Generic.List<ParkingStand> ParkingStands { get; } = new global::System.Collections.Generic.List<ParkingStand>();

        [global::ProtoBuf.ProtoMember(5, Name = @"taxi_nodes")]
        public global::System.Collections.Generic.List<TaxiNode> TaxiNodes { get; } = new global::System.Collections.Generic.List<TaxiNode>();

        [global::ProtoBuf.ProtoMember(6, Name = @"taxi_edges")]
        public global::System.Collections.Generic.List<TaxiEdge> TaxiEdges { get; } = new global::System.Collections.Generic.List<TaxiEdge>();

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class Runway : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"width_meters")]
        public float WidthMeters { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"length_meters")]
        public float LengthMeters { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"mask_bit")]
        public uint MaskBit { get; set; }

        [global::ProtoBuf.ProtoMember(4, Name = @"end_1")]
        public End End1 { get; set; }

        [global::ProtoBuf.ProtoMember(5, Name = @"end_2")]
        public End End2 { get; set; }

        [global::ProtoBuf.ProtoContract()]
        public partial class End : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"name")]
            [global::System.ComponentModel.DefaultValue("")]
            public string Name { get; set; } = "";

            [global::ProtoBuf.ProtoMember(2, Name = @"heading")]
            public float Heading { get; set; }

            [global::ProtoBuf.ProtoMember(3, Name = @"centerline_point")]
            public GeoPoint CenterlinePoint { get; set; }

            [global::ProtoBuf.ProtoMember(4, Name = @"displaced_threshold_meters")]
            public float DisplacedThresholdMeters { get; set; }

            [global::ProtoBuf.ProtoMember(5, Name = @"overrun_area_meters")]
            public float OverrunAreaMeters { get; set; }

        }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class TaxiNode : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"id")]
        public int Id { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"location")]
        public GeoPoint Location { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"is_junction")]
        public bool IsJunction { get; set; }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class TaxiEdge : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"id")]
        public int Id { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name { get; set; } = "";

        [global::ProtoBuf.ProtoMember(3, Name = @"node_id_1")]
        public int NodeId1 { get; set; }

        [global::ProtoBuf.ProtoMember(4, Name = @"node_id_2")]
        public int NodeId2 { get; set; }

        [global::ProtoBuf.ProtoMember(5, Name = @"type")]
        public TaxiEdgeType Type { get; set; }

        [global::ProtoBuf.ProtoMember(6, Name = @"is_one_way")]
        public bool IsOneWay { get; set; }

        [global::ProtoBuf.ProtoMember(7, Name = @"is_high_speed_exit")]
        public bool IsHighSpeedExit { get; set; }

        [global::ProtoBuf.ProtoMember(8, Name = @"length_meters")]
        public float LengthMeters { get; set; }

        [global::ProtoBuf.ProtoMember(9, Name = @"heading")]
        public float Heading { get; set; }

        [global::ProtoBuf.ProtoMember(10, Name = @"active_zones")]
        public ActiveZoneMatrix ActiveZones { get; set; }

        [global::ProtoBuf.ProtoContract()]
        public partial class ActiveZoneMatrix : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"departure")]
            public ulong Departure { get; set; }

            [global::ProtoBuf.ProtoMember(2, Name = @"arrival")]
            public ulong Arrival { get; set; }

            [global::ProtoBuf.ProtoMember(3, Name = @"ils")]
            public ulong Ils { get; set; }

        }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class ParkingStand : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"id")]
        public int Id { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name { get; set; } = "";

        [global::ProtoBuf.ProtoMember(3, Name = @"type")]
        public ParkingStandType Type { get; set; }

        [global::ProtoBuf.ProtoMember(4, Name = @"location")]
        public GeoPoint Location { get; set; }

        [global::ProtoBuf.ProtoMember(5, Name = @"heading")]
        public float Heading { get; set; }

        [global::ProtoBuf.ProtoMember(6, Name = @"width_code")]
        [global::System.ComponentModel.DefaultValue("")]
        public string WidthCode { get; set; } = "";

        [global::ProtoBuf.ProtoMember(7, Name = @"categories", IsPacked = true)]
        public global::System.Collections.Generic.List<AircraftCategory> Categories { get; } = new global::System.Collections.Generic.List<AircraftCategory>();

        [global::ProtoBuf.ProtoMember(8, Name = @"operation_types", IsPacked = true)]
        public global::System.Collections.Generic.List<OperationType> OperationTypes { get; } = new global::System.Collections.Generic.List<OperationType>();

        [global::ProtoBuf.ProtoMember(9, Name = @"airline_icaos")]
        public global::System.Collections.Generic.List<string> AirlineIcaos { get; } = new global::System.Collections.Generic.List<string>();

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class AirspaceGeometry : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"lateral_bounds")]
        public GeoPolygon LateralBounds { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"lower_bound_feet")]
        public float LowerBoundFeet { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"upper_bound_feet")]
        public float UpperBoundFeet { get; set; }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class Aircraft : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"id")]
        public int Id { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"model_icao")]
        [global::System.ComponentModel.DefaultValue("")]
        public string ModelIcao { get; set; } = "";

        [global::ProtoBuf.ProtoMember(3, Name = @"airline_icao")]
        [global::System.ComponentModel.DefaultValue("")]
        public string AirlineIcao { get; set; } = "";

        [global::ProtoBuf.ProtoMember(4, Name = @"tail_no")]
        [global::System.ComponentModel.DefaultValue("")]
        public string TailNo { get; set; } = "";

        [global::ProtoBuf.ProtoMember(5, Name = @"call_sign")]
        [global::System.ComponentModel.DefaultValue("")]
        public string CallSign { get; set; } = "";

        [global::ProtoBuf.ProtoMember(6)]
        public Situation situation { get; set; }

        [global::ProtoBuf.ProtoContract()]
        public partial class Situation : global::ProtoBuf.IExtensible
        {
            private global::ProtoBuf.IExtension __pbn__extensionData;
            global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
                => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

            [global::ProtoBuf.ProtoMember(1, Name = @"location")]
            public Vector3d Location { get; set; }

            [global::ProtoBuf.ProtoMember(2, Name = @"attitude")]
            public Attitude Attitude { get; set; }

            [global::ProtoBuf.ProtoMember(3, Name = @"velocity")]
            public Vector3d Velocity { get; set; }

            [global::ProtoBuf.ProtoMember(4, Name = @"acceleration")]
            public Vector3d Acceleration { get; set; }

            [global::ProtoBuf.ProtoMember(5, Name = @"is_on_ground")]
            public bool IsOnGround { get; set; }

            [global::ProtoBuf.ProtoMember(6, Name = @"flap_ratio")]
            public float FlapRatio { get; set; }

            [global::ProtoBuf.ProtoMember(7, Name = @"spoiler_ratio")]
            public float SpoilerRatio { get; set; }

            [global::ProtoBuf.ProtoMember(8, Name = @"gear_ratio")]
            public float GearRatio { get; set; }

            [global::ProtoBuf.ProtoMember(9, Name = @"nose_wheel_angle")]
            public float NoseWheelAngle { get; set; }

            [global::ProtoBuf.ProtoMember(10, Name = @"landing_lights")]
            public bool LandingLights { get; set; }

            [global::ProtoBuf.ProtoMember(11, Name = @"taxi_lights")]
            public bool TaxiLights { get; set; }

            [global::ProtoBuf.ProtoMember(12, Name = @"strobe_lights")]
            public bool StrobeLights { get; set; }

            [global::ProtoBuf.ProtoMember(13, Name = @"frequency_khz")]
            public int FrequencyKhz { get; set; }

            [global::ProtoBuf.ProtoMember(14, Name = @"squawk")]
            [global::System.ComponentModel.DefaultValue("")]
            public string Squawk { get; set; } = "";

            [global::ProtoBuf.ProtoMember(15, Name = @"mode_c")]
            public bool ModeC { get; set; }

            [global::ProtoBuf.ProtoMember(16, Name = @"mode_s")]
            public bool ModeS { get; set; }

        }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class TaxiPath : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"from_node_id")]
        public int FromNodeId { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"to_node_id")]
        public int ToNodeId { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"edge_ids", IsPacked = true)]
        public int[] EdgeIds { get; set; }

    }

    [global::ProtoBuf.ProtoContract()]
    public enum GeoEdgeType
    {
        [global::ProtoBuf.ProtoEnum(Name = @"GEO_EDGE_UNKNOWN")]
        GeoEdgeUnknown = 0,
        [global::ProtoBuf.ProtoEnum(Name = @"GEO_EDGE_ARC_BY_EDGE")]
        GeoEdgeArcByEdge = 1,
        [global::ProtoBuf.ProtoEnum(Name = @"GEO_EDGE_CIRCLE")]
        GeoEdgeCircle = 2,
        [global::ProtoBuf.ProtoEnum(Name = @"GEO_EDGE_GREAT_CIRCLE")]
        GeoEdgeGreatCircle = 3,
        [global::ProtoBuf.ProtoEnum(Name = @"GEO_EDGE_RHUMB_LINE")]
        GeoEdgeRhumbLine = 4,
        [global::ProtoBuf.ProtoEnum(Name = @"GEO_EDGE_CLOCKWISE_ARC")]
        GeoEdgeClockwiseArc = 5,
        [global::ProtoBuf.ProtoEnum(Name = @"GEO_EDGE_COUNTER_CLOCKWISE_ARC")]
        GeoEdgeCounterClockwiseArc = 6,
    }

    [global::ProtoBuf.ProtoContract()]
    public enum AircraftCategory
    {
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_CATEGORY_NONE")]
        AircraftCategoryNone = 0,
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_CATEGORY_HEAVY")]
        AircraftCategoryHeavy = 1,
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_CATEGORY_JET")]
        AircraftCategoryJet = 2,
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_CATEGORY_TURBOPROP")]
        AircraftCategoryTurboprop = 4,
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_CATEGORY_PROP")]
        AircraftCategoryProp = 8,
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_CATEGORY_LIGHT_PROP")]
        AircraftCategoryLightProp = 16,
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_CATEGORY_HELICPOTER")]
        AircraftCategoryHelicpoter = 32,
    }

    [global::ProtoBuf.ProtoContract()]
    public enum OperationType
    {
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_OPERATION_NONE")]
        AircraftOperationNone = 0,
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_OPERATION_GA")]
        AircraftOperationGa = 1,
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_OPERATION_AIRLINE")]
        AircraftOperationAirline = 2,
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_OPERATION_CARGO")]
        AircraftOperationCargo = 4,
        [global::ProtoBuf.ProtoEnum(Name = @"AIRCRAFT_OPERATION_MILITARY")]
        AircraftOperationMilitary = 8,
    }

    [global::ProtoBuf.ProtoContract()]
    public enum ParkingStandType
    {
        [global::ProtoBuf.ProtoEnum(Name = @"PARKING_UNKNOWN")]
        ParkingUnknown = 0,
        [global::ProtoBuf.ProtoEnum(Name = @"PARKING_GATE")]
        ParkingGate = 1,
        [global::ProtoBuf.ProtoEnum(Name = @"PARKING_REMOTE")]
        ParkingRemote = 2,
        [global::ProtoBuf.ProtoEnum(Name = @"PARKING_HANGAR")]
        ParkingHangar = 3,
    }

    [global::ProtoBuf.ProtoContract()]
    public enum TaxiEdgeType
    {
        [global::ProtoBuf.ProtoEnum(Name = @"TAXI_EDGE_GROUNDWAY")]
        TaxiEdgeGroundway = 0,
        [global::ProtoBuf.ProtoEnum(Name = @"TAXI_EDGE_TAXIWAY")]
        TaxiEdgeTaxiway = 1,
        [global::ProtoBuf.ProtoEnum(Name = @"TAXI_EDGE_RUNWAY")]
        TaxiEdgeRunway = 2,
    }

}

#pragma warning restore CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
#endregion
