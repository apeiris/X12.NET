using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X12UtilsFRM
{
    public class CanvasSaveState
    {
        public string SourceSchemaFile { get; set; }
        public string TargetSchemaFile { get; set; }
        public List<CanvasFunctoidDto> Functoids { get; set; } = new List<CanvasFunctoidDto>();
        public List<CanvasConnectionDto> Wires { get; set; } = new List<CanvasConnectionDto>();
    }

    public class CanvasFunctoidDto
    {
        public string Id { get; set; }
        public string FunctoidName { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public string CustomScript { get; set; }
    }

    public class CanvasConnectionDto
    {
        public string SourceType { get; set; } // "SchemaNode" or "Functoid"
        public string SourceIdOrXPath { get; set; }

        public string TargetType { get; set; } // "SchemaNode" or "Functoid"
        public string TargetIdOrXPath { get; set; }
    }


}

