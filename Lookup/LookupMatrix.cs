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
        private HashSet<LookupVertex>[,,,] _matrix;
        public LookupMatrix(int x, int y, int z, int w)
        {
            _matrix = new HashSet<LookupVertex>[x * 2 + 1, y * 2 + 1, z * 2 + 1, w * 2 + 1];
        }

        public void Insert(LookupVertex vertex)
        {
            if (_matrix[vertex.StaticFieldCount, vertex.LiteralFieldCount, vertex.GenericFieldCount, vertex.PropertyCount] == null)
            {
                _matrix[vertex.StaticFieldCount, vertex.LiteralFieldCount, vertex.GenericFieldCount, vertex.PropertyCount] = new HashSet<LookupVertex>() { vertex };
            }
            else
            {
                _matrix[vertex.StaticFieldCount, vertex.LiteralFieldCount, vertex.GenericFieldCount, vertex.PropertyCount].Add(vertex);
            }
        }

        public void Insert(LookupType item)
        {
            Insert(new LookupVertex() { Type = item, StaticFieldCount = item.Fields.Count(f => f.IsStatic), LiteralFieldCount = item.Fields.Count(f => f.IsLiteral), GenericFieldCount = item.Fields.Count(f => !f.IsStatic && !f.IsLiteral), PropertyCount = item.Properties.Count });
        }

        public HashSet<LookupVertex> GetVertices(LookupVertex vertex)
        {
            return _matrix[vertex.StaticFieldCount, vertex.LiteralFieldCount, vertex.GenericFieldCount, vertex.PropertyCount];
        }

        public List<LookupType> Get(LookupType item)
        {
            HashSet<LookupVertex> vertexSet = GetVertices(new LookupVertex() { Type = item, StaticFieldCount = item.Fields.Count(f => f.IsStatic), LiteralFieldCount = item.Fields.Count(f => f.IsLiteral), GenericFieldCount = item.Fields.Count(f => !f.IsStatic && !f.IsLiteral), PropertyCount = item.Properties.Count });
            List<LookupType> typeList = new List<LookupType>();
            if (vertexSet == null || vertexSet.Count == 0) return typeList;

            typeList.AddRange(vertexSet.Select(x => x.Type));
            return typeList;
        }
    }
}
