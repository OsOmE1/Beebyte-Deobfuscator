using System.Collections.Generic;
using System.Linq;

namespace Beebyte_Deobfuscator.Lookup
{
    struct LookupVertex
    {
        public LookupType Type { get; set; }
        public int StaticFieldCount { get; set; }
        public int LiteralFieldCount { get; set; }
        public int GenericFieldCount { get; set; }
        public int PropertyCount { get; set; }

    }
    class LookupMatrix
    {
        private List<LookupVertex> _matrix;
        public LookupMatrix()
        {
            _matrix = new List<LookupVertex>();
        }

        public void Insert(LookupVertex vertex)
        {
            _matrix.Add(vertex);
        }

        public void Insert(LookupType item)
        {
            Insert(new LookupVertex() { Type = item, StaticFieldCount = item.Fields.Count(f => f.IsStatic), LiteralFieldCount = item.Fields.Count(f => f.IsLiteral), GenericFieldCount = item.Fields.Count(f => !f.IsStatic && !f.IsLiteral), PropertyCount = item.Properties.Count });
        }

        public List<LookupVertex> GetVertices(LookupVertex vertex)
        {
            return _matrix.Where(l => l.StaticFieldCount == vertex.StaticFieldCount && l.GenericFieldCount== vertex.GenericFieldCount && l.LiteralFieldCount == vertex.LiteralFieldCount && l.PropertyCount == vertex.PropertyCount && l.Type.Namespace == vertex.Type.Namespace).ToList();
        }

        public List<LookupType> Get(LookupType item)
        {
            List<LookupVertex> vertexSet = GetVertices(new LookupVertex() { Type = item, StaticFieldCount = item.Fields.Count(f => f.IsStatic), LiteralFieldCount = item.Fields.Count(f => f.IsLiteral), GenericFieldCount = item.Fields.Count(f => !f.IsStatic && !f.IsLiteral), PropertyCount = item.Properties.Count });
            List<LookupType> typeList = new List<LookupType>();
            if (vertexSet == null || vertexSet.Count == 0) return typeList;

            typeList = vertexSet.Select(x => x.Type).ToList();
            return typeList;
        }
    }
}
