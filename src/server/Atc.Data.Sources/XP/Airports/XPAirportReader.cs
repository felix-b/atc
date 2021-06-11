using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zero.Serialization.Buffers;
using Atc.Data.Airports;
using Atc.Data.Control;
using Atc.Data.Navigation;
using Atc.Data.Primitives;
using Atc.Data.Traffic;
using Atc.Math;
using AtcDistance = Atc.Data.Primitives.Distance;

namespace Atc.Data.Sources.XP.Airports
{
    public class XPAirportReader
    {
        public delegate bool ContextualParser(int lineCode);
        public delegate bool FilterAirportCallback(in AirportData.HeaderData header);
        public delegate ZRef<ControlledAirspaceData> QueryAirspaceCallback (in AirportData.HeaderData header);

        private readonly IBufferContext _output;
        private readonly QueryAirspaceCallback? m_onQueryAirspace;
        private readonly FilterAirportCallback? m_onFilterAirport;

        private readonly HashSet<int> m_parsedFrequencyKhz = new();
        private readonly HashSet<int> m_parsedFrequencyLineCodes = new();

        private readonly AirportAssemblyParts _parts = new AirportAssemblyParts();
        
        bool m_headerWasRead;
        bool m_filterWasQueried;
        bool m_isLandAirport;
        bool m_skippingAirport;
        int m_unparsedLineCode;
        int m_nextEdgeId;
        int m_nextParkingStandId;
        string m_icao = string.Empty;
        string m_name = string.Empty;
        float m_elevation;
        double? m_datumLatitude = null;
        double? m_datumLongitude = null;

        public XPAirportReader(
            IBufferContext output, 
            XPAptDatReader.ILogger logger,
            QueryAirspaceCallback? onQueryAirspace = null, 
            FilterAirportCallback? onFilterAirport = null,
            int unparsedLineCode = -1)
        {
            _logger = logger;
            _output = output;
            
            m_onQueryAirspace = onQueryAirspace;
            m_onFilterAirport = onFilterAirport;
            m_unparsedLineCode = unparsedLineCode;
            m_isLandAirport = false;
        }

        public void ReadAirport(Stream aptDatStream)
        {
            ReadAirport(XPAptDatReader.CreateAptDatStreamReader(aptDatStream));
        }

        public void ReadAirport(StreamReader aptDatStreamReader)
        {
            readAptDatInContext(aptDatStreamReader, (int lineCode) => {
                return rootContextParser(lineCode, aptDatStreamReader);
            });
        }

        public ZRef<AirportData>? GetAirport()
        {
            if (!m_skippingAirport)
            {
                try
                {
                    return assembleAirportOrThrow();
                }
                catch (Exception e)
                {
                    _logger.FailedToAssembleAirport(icao: m_icao, message: e.Message);
                }
            }

            return null;
        }

        public int UnparsedLineCode => m_unparsedLineCode;

        public string Icao => m_icao;

        public bool IsLandAirport => m_isLandAirport;

        private void FindTaxiways()
        {
            var edgesGroupedByName = _parts
                .TaxiEdgeById.Values
                .GroupBy(edge => edge.Get().Name);

            foreach (var group in edgesGroupedByName)
            {
                var nameRef = group.Key;
                var taxiwayData = CreateTaxiwayData(nameRef, group);
                if (taxiwayData.HasValue)
                {
                    _parts.TaxiwayByName.Add(nameRef.GetValueNonCached(), taxiwayData.Value);
                }
            }

            ZRef<TaxiwayData>? CreateTaxiwayData(ZStringRef nameRef, IEnumerable<ZRef<TaxiEdgeData>> edges)
            {
                var endNodes = new List<ZRef<TaxiNodeData>>();
                
                foreach (var edgeRef in edges)
                {
                    ref var edge = ref edgeRef.Get();
                    if (IsTaxiwayEndNode(nameRef, edge.Node1Ref()))
                    {
                        endNodes.Add(edge.Node1Ref());
                    }
                    if (IsTaxiwayEndNode(nameRef, edge.Node2Ref()))
                    {
                        endNodes.Add(edge.Node2Ref());
                    }
                }

                if (endNodes.Count != 2 || endNodes[0] == endNodes[1])
                {
                    return null;
                }

                var location0 = endNodes[0].Get().Location;
                var location1 = endNodes[1].Get().Location;
                var startNode = location0 < location1 ? endNodes[0] : endNodes[1];
                var endNode = location0 < location1 ? endNodes[1] : endNodes[0];
                var linedUpEdges = LineUpTaxiwayEdges(nameRef, startNode, endNode);
                var forwardVector = _output.AllocateVector(linedUpEdges.ToArray());
                linedUpEdges.Reverse();
                var backwardVector = _output.AllocateVector(linedUpEdges
                    .Select(e => e.Get().ReverseEdgeRef())
                    .Where(e => e.HasValue)
                    .Select(e => e!.Value)
                    .ToArray()
                );
                
                var taxiwayData = _output.AllocateRecord(new TaxiwayData() {
                    Name = nameRef,
                    Edges12 = forwardVector,
                    Edges21 = backwardVector,
                });
                return taxiwayData;
            }

            List<ZRef<TaxiEdgeData>> LineUpTaxiwayEdges(
                ZStringRef nameRef,
                ZRef<TaxiNodeData> startNodeRef,
                ZRef<TaxiNodeData> endNodeRef)
            {
                var result = new List<ZRef<TaxiEdgeData>>(capacity: 16);
                
                for (var nodeRef = startNodeRef ; nodeRef != endNodeRef && !nodeRef.IsNull ; )
                {
                    ref var node = ref nodeRef.Get();
                    var nextEdgeRef = node.EdgesOut.FirstOrDefault(e => e.Get().Get().Name == nameRef);
                    if (nextEdgeRef.IsNull)
                    {
                        throw new InvalidDataException(
                            $"Taxiway '{nameRef.GetValueNonCached()}': cannot find next edge from node id {node.Id}");
                    }
                    nodeRef = nextEdgeRef.Get().Get().Node2Ref(); 
                    result.Add(nextEdgeRef.Get());
                }

                return result;
            }
            
            bool IsTaxiwayEndNode(ZStringRef nameRef, ZRef<TaxiNodeData> nodeRef)
            {
                //TODO: current implementation assumes two-way edges; TODO - handle one-way edges as well.
                
                var inEdges = nodeRef.Get().EdgesIn.Where(e => EdgeHasName(e, nameRef)).ToArray();
                if (inEdges.Length != 1)
                {
                    return false;
                }

                var outEdges = nodeRef.Get().EdgesOut.Where(e => EdgeHasName(e, nameRef)).ToArray();
                if (outEdges.Length != 1)
                {
                    return false;
                }
                
                var isInOutSameEdge = inEdges[0].Get().Get().ReverseEdge == outEdges[0].Get();
                return isInOutSameEdge;
            }

            bool EdgeHasName(ZCursor<ZRef<TaxiEdgeData>> edgeCur, ZStringRef name)
            {
                var edgeRef = edgeCur.Get();
                ref var edge = ref edgeRef.Get();
                return (edge.Name == name);
            }
        }
        
        void CompleteTaxiNet()
        {
            FixUpEdgesAndNodes();
            FindTaxiways();
            
            void FixUpEdgesAndNodes()
            {
                foreach (var edgeRef in _parts.TaxiEdgeById.Values)
                {
                    FixUpEdge(edgeRef);
                }
            }

            ZRef<TaxiNodeData> FindNodeRefById(int edgeId, int nodeId)
            {
                if (_parts.TaxiNodeById.TryGetValue(nodeId, out var nodeRef))
                {
                    return nodeRef;
                }
                throw new InvalidDataException($"Taxi edge id {edgeId} refers to node id {nodeId}, but the node was not found");
            }

            void FixUpEdge(ZRef<TaxiEdgeData> edgeRef)
            {
                ref var edge = ref edgeRef.Get();
                edge.Node1 = FindNodeRefById(edge.Id, edge.Node1.ByteIndex);
                edge.Node2 = FindNodeRefById(edge.Id, edge.Node2.ByteIndex);
                ref var node1 = ref edge.Node1.GetAs<TaxiNodeData>();
                ref var node2 = ref edge.Node2.GetAs<TaxiNodeData>();
                
                GeoMath.CalculateGreatCircleLine(node1.Location, node2.Location, out var geoLine);
                edge.Length = geoLine.Length;
                edge.Heading = geoLine.Bearing12;
                
                node1.EdgesOut.Add(edgeRef);
                node2.EdgesIn.Add(edgeRef);
                
                // if (edge.ReverseEdge.HasValue)
                // {
                //     ref var reverseEdge = ref edge.ReverseEdge.Value.GetAs<TaxiEdgeData>();
                //     reverseEdge.Node1 = edge.Node2;
                //     reverseEdge.Node2 = edge.Node1;
                //     reverseEdge.Length = geoLine.Length;
                //     reverseEdge.Heading = geoLine.Bearing21;
                //     node2.EdgesOut.Add(edge.ReverseEdge.Value.AsZRef<TaxiEdgeData>());
                //     node1.EdgesIn.Add(edge.ReverseEdge.Value.AsZRef<TaxiEdgeData>());
                // }
            }
        }
        
        ZRef<AirportData> assembleAirportOrThrow()
        {
            CompleteTaxiNet();
            
            var datum = new GeoPoint(
                m_datumLatitude.GetValueOrDefault(0),
                m_datumLongitude.GetValueOrDefault(0));

            var header = new AirportData.HeaderData() {
                Icao = _output.AllocateString(m_icao),
                Name = _output.AllocateString(m_name),
                Datum = datum,
                Elevation = Altitude.FromFeetMsl(m_elevation)
            };
            
            _parts.Header = header;
            _parts.Airpsace = m_onQueryAirspace?.Invoke(in header);

            var assembler = new AirportAssembler(_output, _parts);
            var airportRef = assembler.AssembleAirport();
            return airportRef;
        }
        
        private void readAptDatInContext(StreamReader input, ContextualParser parser)
        {
            while (!input.FastEndOfStream())
            {
                int saveLineCode = m_unparsedLineCode;
                long saveInputPosition = input.BaseStream.Position;

                try
                {
                    if (!readAptDatLineInContext(input, parser))
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    string diagnostic = formatDiagnosticMessage(input, saveInputPosition, saveLineCode, e);
                    _logger.FailedToLoadAirportSkipping(icao: m_icao, diagnostic);
                    m_skippingAirport = true;
                }
            }
        }

        string formatDiagnosticMessage(StreamReader input, long position, int extractedLineCode, Exception error)
        {
            input.BaseStream.Position = position;
            input.DiscardBufferedData();

            var message =
                $"[{error.Message}] line [" +
                (extractedLineCode >= 0 ? $"code[{extractedLineCode}] > " : string.Empty) +
                input.ReadLine() ?? string.Empty;

            return message;
        }
        
        private bool readAptDatLineInContext(StreamReader input, ContextualParser parser)
        {
            int lineCode = m_unparsedLineCode >= 0
                ? m_unparsedLineCode
                : extractNextLineCode(input);

            if (lineCode < 0)
            {
                return false;
            }

            m_unparsedLineCode = -1;

            bool accepted = parser(lineCode);
            if (!accepted)
            {
                m_unparsedLineCode = lineCode;
                return false;
            }

            return true;
        } 
        
        private int extractNextLineCode(StreamReader input)
        {
            input.ExtractWhitespace(includeCrlf: true);

            if (input.FastEndOfStream())
            {
                return -1;
            }
            
            input.Extract(out int lineCode);
            return lineCode;
        }
        
        private bool rootContextParser(int lineCode, StreamReader input)
        {
            bool isAirportHeaderLine = IsAirportHeaderLineCode(lineCode);

            if (m_skippingAirport)
            {
                if (isAirportHeaderLine)
                {
                    return false;
                }
                input.SkipToNextLine();
                return true;
            }

            if (m_headerWasRead)
            {
                if (isAirportHeaderLine)
                {
                    return false; // we're at the beginning of the next airport
                }
                if (lineCode != 1302 && !m_filterWasQueried)
                {
                    m_skippingAirport = !invokeFilterCallback();
                    m_filterWasQueried = true;
                    if (m_skippingAirport)
                    {
                        _logger.SkippingAirportByFilter(icao: m_icao);
                    }
                }
            }

            switch (lineCode)
            {
                case 1:
                    parseHeader1(input);
                    m_headerWasRead = true;
                    m_isLandAirport = true;
                    break;
                case 16:
                case 17:
                    m_isLandAirport = false;
                    m_skippingAirport = true;
                    input.SkipToNextLine();
                    break;
                case 100:
                    parseRunway100(input);
                    break;
                case 1201:
                    parseTaxiNode1201(input);
                    break;
                case 1202:
                    parseTaxiEdge1202(input);
                    break;
                case 1206:
                    parseGroundEdge1206(input);
                    break;
                case 1300:
                    parseStartupLocation1300(input);
                    break;
                case 1302:
                    parseMetadata1302(input);
                    break;
                default:
                    if (isControlFrequencyLine(lineCode))
                    {
                        parseControlFrequency(lineCode, input);
                    }
                    else
                    {
                        input.SkipToNextLine();
                    }
                    break;
            }

            return true;
        }
        
        private bool invokeFilterCallback()
        {
            var header = new AirportData.HeaderData  {
                Icao = _output.AllocateString(m_icao),
                Name = _output.AllocateString(m_name),
                Datum = new GeoPoint(m_datumLatitude ?? 0, m_datumLongitude ?? 0),
                Elevation = new Altitude(m_elevation, AltitudeUnit.Feet, AltitudeType.Msl)
            };
            return m_onFilterAirport?.Invoke(header) ?? true;
        }
        
        void parseHeader1(StreamReader input)
        {
            int deprecated;
            input.Extract(out m_elevation, out deprecated, out deprecated, out m_icao);
            m_name = input.ReadToEndOfLine();
        }
        
        void parseRunway100(StreamReader input)
        {
            RunwayEndData parseEnd(out string name) 
            {
                int unusedInt;

                input.Extract(out name, out double centerlineLat, out double centerlineLon);
                input.Extract(out float displasedThresholdMeters, out float overrunAreaMeters);
                input.Extract(out unusedInt, out unusedInt, out unusedInt, out unusedInt);

                return new RunwayEndData {
                    Name = _output.AllocateString(name),
                    CenterlinePoint = new GeoPoint(centerlineLat, centerlineLon),
                    DisplacedThresholdLength = AtcDistance.FromMeters(displasedThresholdMeters),
                    OverrunAreaLength = AtcDistance.FromMeters(overrunAreaMeters)
                };
            }

            float widthMeters;
            int unusedInt;
            float unusedFloat;
            
            input.Extract(out widthMeters, out unusedInt, out unusedInt);
            input.Extract(out unusedFloat, out unusedInt, out unusedInt, out unusedInt);
            
            var end1 = parseEnd(out var end1Name);
            var end2 = parseEnd(out var end2Name);

            GeoMath.CalculateGreatCircleLine(in end1.CenterlinePoint, in end2.CenterlinePoint, out var centerLine);
            end1.Heading = centerLine.Bearing12;
            end2.Heading = centerLine.Bearing21;

            var fullName12 = $"{end1Name}-{end2Name}";
            var fullName21 = $"{end2Name}-{end1Name}";
            var runwayRef = _output.AllocateRecord<RunwayData>(new RunwayData {
                Name = _output.AllocateString(fullName12),
                End1 = end1, 
                End2 = end2, 
                Length = centerLine.Length, 
                Width = Distance.FromMeters(widthMeters),
                BitmaskFlag = 1UL << _parts.Runways.Count
            });
            
            _parts.Runways.Add(runwayRef);
            _parts.RunwayByName.Add(end1Name, runwayRef);
            _parts.RunwayByName.Add(end2Name, runwayRef);
            _parts.RunwayByName.Add(fullName12, runwayRef);
            _parts.RunwayByName.Add(fullName21, runwayRef);
        }

        void parseTaxiNode1201(StreamReader input)
        {
            double latitude;
            double longitude;
            string usage;
            int id;
            string name;

            input.Extract(out latitude, out longitude, out usage, out id);
            name = input.ReadToEndOfLine();

            var nodeRef = _output.AllocateRecord<TaxiNodeData>(new TaxiNodeData {
                Id = id,
                Location = new GeoPoint(latitude, longitude),
                Name = _output.AllocateString(name),
                EdgesIn = _output.AllocateVector<ZRef<TaxiEdgeData>>(initialCapacity: 3),
                EdgesOut = _output.AllocateVector<ZRef<TaxiEdgeData>>(initialCapacity: 3),
            });
            
            _parts.TaxiNodeById.Add(id, nodeRef);
        }

        void parseTaxiEdge1202(StreamReader input)
        {
            input.Extract(out int nodeId1, out int nodeId2, out string direction, out string typeString);
            var name = input.ReadToEndOfLine();

            TaxiEdgeType type = typeString.StartsWith("runway")
                ? TaxiEdgeType.Runway 
                : TaxiEdgeType.Taxiway;

            char widthCode = typeString.StartsWith("taxiway_") && typeString.Length == 9
                ? typeString[8]
                : '\x0';
            
            var edgeRef = ParseTaxiOrGroundEdge(type, name, direction, widthCode, nodeId1, nodeId2);

            readAptDatInContext(input, lineCode => {
                if (lineCode == 1204)
                {
                    parseRunwayActiveZone1204(input, edgeRef);
                    return true;
                }
                return false;
            });
        }

        void parseGroundEdge1206(StreamReader input)
        {
            input.Extract(out int nodeId1, out int nodeId2, out string direction);
            var name = input.ReadToEndOfLine();
            char widthCode = '\x0'; 
            ParseTaxiOrGroundEdge(TaxiEdgeType.Groundway, name, direction, widthCode, nodeId1, nodeId2);
        }

        private ZRef<TaxiEdgeData> ParseTaxiOrGroundEdge(
            TaxiEdgeType type,
            string name, 
            string direction, 
            char widthCode,
            int nodeId1,
            int nodeId2)
        {
            bool isOneWay = direction == "oneway";
            var nameRef = _output.AllocateString(name);

            ZRef<TaxiEdgeData> AllocateTaxiEdgeRecord(int assignId, int assignNodeId1, int assignNodeId2, ZRefAny? assignReverse)
            {
                return _output.AllocateRecord<TaxiEdgeData>(new TaxiEdgeData() {
                    Id = assignId, 
                    Name = nameRef, 
                    Type = type,
                    WidthCode = widthCode,
                    IsOneWay = isOneWay,
                    // temporary, we store node ids not the actual pointers 
                    // because the nodes may not be parsed yet
                    // in the end we run FixupTaxiEdgeNodeIds() to replace the ids with pointers
                    Node1 = new ZRefAny(assignNodeId1), 
                    Node2 = new ZRefAny(assignNodeId2),
                    Length = Distance.FromMeters(0),      // to ba calculated by AirportBuilder
                    Heading = Bearing.FromTrueDegrees(0), // to ba calculated by AirportBuilder
                    ReverseEdge = assignReverse
                });
            }
            
            int edgeId = m_nextEdgeId++;
            var edgeRef = AllocateTaxiEdgeRecord(edgeId, nodeId1, nodeId2, null); 
            _parts.TaxiEdgeById.Add(edgeId, edgeRef);

            if (!isOneWay)
            {
                var reverseEdgeId = m_nextEdgeId++; 
                var reverseEdgeRef = AllocateTaxiEdgeRecord(reverseEdgeId, nodeId2, nodeId1, edgeRef);
                edgeRef.Get().ReverseEdge = reverseEdgeRef;
                _parts.TaxiEdgeById.Add(reverseEdgeId, reverseEdgeRef);
            }
            
            return edgeRef;
        }

        void parseRunwayActiveZone1204(StreamReader input, ZRef<TaxiEdgeData> edgeRef)
        {
            input.Extract(out string classification, out string runwayNamesString);

            var zoneType = classification switch {
                "arrival" => ActiveZoneTypes.Arrival,
                "departure" => ActiveZoneTypes.Departure,
                "ils" => ActiveZoneTypes.Ils,
                _ => ActiveZoneTypes.None
            };

            var runwayNamesArray = runwayNamesString.Split(_commaListSeparators, StringSplitOptions.RemoveEmptyEntries);
            ref var edge = ref edgeRef.Get();

            for (int i = 0; i < runwayNamesArray.Length; i++)
            {
                var runwayRef = _parts.RunwayByName[runwayNamesArray[i]];
                edge.ActiveZones.AddZoneTypes(runwayRef, zoneType);                
            }
        }

        void parseStartupLocation1300(StreamReader input)
        {
            string widthCode = string.Empty;
            string operationTypesText = string.Empty;
            string airlinesText = string.Empty;

            input.Extract(
                out double latitude, 
                out double longitude, 
                out float headingDegrees, 
                out string typeText, 
                out string categoriesText);
            
            var name = input.ReadToEndOfLine();
            var uniqueName = _parts.ParkingStandByName.MakeMinimalUniqueStringKey(name, minSuffix: 2, maxSuffix: 200);

            readAptDatInContext(input, lineCode => {
                if (lineCode == 1301)
                {
                    input.Extract(out widthCode, out operationTypesText);
                    airlinesText = input.ReadToEndOfLine();
                    return true;
                }
                return false;
            });

            var type = _parkingStandTypeLookup[typeText];
            var categories = AircraftCategories.None;
            var operationTypes = OperationTypes.None;

            ParseSeparatedList(categoriesText, _arbitraryListSeparators, item => {
                categories |= _aircraftCategoryLookup[item];
            });
            ParseSeparatedList(operationTypesText, _arbitraryListSeparators, item => {
                operationTypes |= _aircraftOperationTypeLookup[item];
            });

            var airlinesVectorRef = _output.AllocateVector<ZRef<AirlineData>>(); 
            var parkingStandRef = _output.AllocateRecord<ParkingStandData>(new ParkingStandData() {
                Id = m_nextParkingStandId++, 
                Name = _output.AllocateString(uniqueName),
                Type = type,
                Location = new GeoPoint(latitude, longitude),
                Direction = Bearing.FromTrueDegrees(headingDegrees),
                WidthCode = widthCode.Length > 0 ? widthCode[0] : '\x0',
                Categories = categories,
                Operations = operationTypes,
                Airlines = airlinesVectorRef
            });

            ParseSeparatedList(airlinesText, _arbitraryListSeparators, item => {
                ref var world = ref _output.GetWorldData();
                if (world.AirlineByIcao.TryGetValue(item, out var airlineRef))
                {
                    airlinesVectorRef.Add(airlineRef);
                }
                else
                {
                    //_logger.WarnGateAirlineNotFound(icao: item);
                }
            });
            
            _parts.ParkingStandByName.Add(uniqueName, parkingStandRef);
        }

        void parseMetadata1302(StreamReader input)
        {
            input.Extract(out string fieldName);

            if (fieldName == "datum_lat")
            {
                if (input.TryExtract(out double lat))
                {
                    m_datumLatitude = lat;
                }
            }
            else if (fieldName == "datum_lon")
            {
                if (input.TryExtract(out double lon))
                {
                    m_datumLongitude = lon;
                }
            }
            else if (fieldName == "icao_code")
            {
                if (input.TryExtract(out string icao) && !string.IsNullOrWhiteSpace(icao))
                {
                    m_icao = icao;
                }
            }
            else
            {
                input.ReadToEndOfLine();
            }
        }

        bool isControlFrequencyLine(int lineCode)
        {
            return ((lineCode >= 50 && lineCode <= 56) || (lineCode >= 1050 && lineCode <= 1056));
        }

        void parseControlFrequency(int lineCode, StreamReader input)
        {
            if (m_parsedFrequencyLineCodes.Contains(lineCode))
            {
                return;
            }

            ControllerPositionType getPositionType()
            {
                switch (lineCode)
                {
                    case 52:
                    case 1052:
                        return ControllerPositionType.ClearanceDelivery;
                    case 53:                         
                    case 1053:                       
                        return ControllerPositionType.Ground;
                    case 54:                         
                    case 1054:                       
                        return ControllerPositionType.Local;
                    case 55:                         
                    case 1055:                       
                        return ControllerPositionType.Approach;
                    case 56:                         
                    case 1056:                       
                        return ControllerPositionType.Departure;
                    default:                         
                        return ControllerPositionType.Unknown;
                }
            };

            var positionType = getPositionType();
            if (positionType != ControllerPositionType.Unknown)
            {
                input.Extract(out int khz);
                string callSign = input.ReadToEndOfLine();
                
                if (m_parsedFrequencyKhz.Add(khz))
                {
                    var position = new ControlFacilityBuilder.ControllerPositionHeader {
                        Type = positionType,
                        Frequency = Frequency.FromKhz(khz),
                        Boundary = new GeoPolygon(),
                        CallSign = callSign
                    };
                    _parts.ControllerPositions.Add(position);
                    m_parsedFrequencyLineCodes.Add(lineCode);
                }
            }
        }

        private static readonly Dictionary<string, ParkingStandType> _parkingStandTypeLookup = new() {
            {"gate",     ParkingStandType.Gate},
            {"hangar",   ParkingStandType.Hangar},
            {"tie_down", ParkingStandType.Remote},
            {"misc",     ParkingStandType.Unknown},
        };

        private static readonly Dictionary<string, AircraftCategories> _aircraftCategoryLookup = new() {
            {"heavy",      AircraftCategories.Heavy},
            {"jets",       AircraftCategories.Jet},
            {"turboprops", AircraftCategories.Turboprop},
            {"props",      AircraftCategories.Prop},
            {"helos",      AircraftCategories.Helicopter},
            {"fighters",   AircraftCategories.Fighter},
            {"all",        AircraftCategories.All},
        };

        private static readonly Dictionary<string, OperationTypes> _aircraftOperationTypeLookup = new() {
            {"none",             OperationTypes.None},
            {"general_aviation", OperationTypes.GA},
            {"airline",          OperationTypes.Airline},
            {"cargo",            OperationTypes.Cargo},
            {"military",         OperationTypes.Military},
        };

        private static readonly char[] _commaListSeparators = new char[] {',', '\x20'};

        private static readonly string _arbitraryListSeparators = ",\x20;:|\t";
        private readonly XPAptDatReader.ILogger _logger;

        public static bool IsAirportHeaderLineCode(int lineCode)
        {
            return (lineCode == 1 || lineCode == 16 || lineCode == 17);
        }

        private static void ParseSeparatedList(
            string listText, 
            string delimiters, 
            Action<string> parseItem)
        {
            //TODO: replace with listText.Split() ?
            
            int lastDelimiterIndex = -1;
    
            for (int i = 0 ; i < listText.Length; i++)
            {
                char c = listText[i];
                bool isDelimiter = delimiters.Contains(c);
                if (isDelimiter)
                {
                    if (i > lastDelimiterIndex + 1)
                    {
                        string itemText = listText.Substring(lastDelimiterIndex + 1, i - lastDelimiterIndex - 1);
                        parseItem(itemText);
                    }
                    lastDelimiterIndex = i;
                }
            }

            if (lastDelimiterIndex < (int)listText.Length - 1)
            {
                string itemText = listText.Substring(lastDelimiterIndex + 1, listText.Length - lastDelimiterIndex - 1);
                parseItem(itemText);
            }
        }

        private class AirportAssemblyParts
        {
            public AirportData.HeaderData Header { get; set; }
            public List<ZRef<RunwayData>> Runways { get; } = new();
            public Dictionary<string, ZRef<RunwayData>> RunwayByName { get; } = new();
            public Dictionary<int, ZRef<TaxiNodeData>> TaxiNodeById { get; } = new();
            public Dictionary<int, ZRef<TaxiEdgeData>> TaxiEdgeById { get; } = new();
            public Dictionary<string, ZRef<TaxiwayData>> TaxiwayByName { get; } = new();
            public Dictionary<string, ZRef<ParkingStandData>> ParkingStandByName { get; } = new();
            public ZRef<ControlledAirspaceData>? Airpsace { get; set; } = null;
            public List<ControlFacilityBuilder.ControllerPositionHeader> ControllerPositions { get; } = new();
        }

        private class AirportAssembler
        {
            private readonly IBufferContext _output;
            private readonly AirportAssemblyParts _parts;
            private ZRef<AirportData> _airportRef;

            public AirportAssembler(IBufferContext output, AirportAssemblyParts parts)
            {
                _output = output;
                _parts = parts;
            }

            public ZRef<AirportData> AssembleAirport()
            {
                _airportRef = _output.AllocateRecord(new AirportData() {
                    Header = _parts.Header,
                    Runways = _output.AllocateVector<ZRef<RunwayData>>(),
                    RunwayByName = _output.AllocateStringMap<ZRef<RunwayData>>(GetBucketCount(_parts.RunwayByName.Count, 64)),
                    TaxiwayByName = _output.AllocateStringMap<ZRef<TaxiwayData>>(GetBucketCount(_parts.TaxiwayByName.Count, 64)),
                    TaxiNodeById = _output.AllocateIntMap<ZRef<TaxiNodeData>>(GetBucketCount(_parts.TaxiNodeById.Count, 1024)),
                    ParkingStandByName = _output.AllocateStringMap<ZRef<ParkingStandData>>(GetBucketCount(_parts.ParkingStandByName.Count, 512)),
                    Tower = null
                });

                ref var airport = ref _airportRef.Get();
                
                PopulateStringMap(_parts.RunwayByName, airport.RunwayByName);
                PopulateStringMap(_parts.ParkingStandByName, airport.ParkingStandByName);
                PopulateStringMap(_parts.TaxiwayByName, airport.TaxiwayByName);
                PopulateIntMap(_parts.TaxiNodeById, airport.TaxiNodeById);
                PopulateVector(_parts.Runways, airport.Runways);

                if (_parts.Airpsace.HasValue)
                {
                    airport.Tower = AssembleTowerFacility();
                }

                return _airportRef;
            }

            int GetBucketCount(int keyCount, int maxBuckets)
            {
                return System.Math.Min(System.Math.Max(2, keyCount), maxBuckets);
            }

            ZRef<ControlFacilityData> AssembleTowerFacility()
            {
                //TODO
                return ZRef<ControlFacilityData>.Null;
            }
            
            private void PopulateStringMap<T>(Dictionary<string, ZRef<T>> source, ZStringMapRef<ZRef<T>> target)
                where T : unmanaged
            {
                foreach (var pair in source)
                {
                    target.Add(_output.AllocateString(pair.Key), pair.Value);
                }
            }

            private void PopulateIntMap<T>(Dictionary<int, ZRef<T>> source, ZIntMapRef<ZRef<T>> target)
                where T : unmanaged
            {
                foreach (var pair in source)
                {
                    target.Add(pair.Key, pair.Value);
                }
            }

            private void PopulateVector<T>(List<T> source, ZVectorRef<T> target)
                where T : unmanaged
            {
                foreach (var value in source)
                {
                    target.Add(value);
                }
            }
        }
    }
}