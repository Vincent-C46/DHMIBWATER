using System.Collections.Generic;
using DHBIMWATER.Core.Parameters;
using DHBIMWATER.Infrastructure.Converters;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    public class RevitSharedParameterRepository : ISharedParameterRepository
    {
        private readonly Func<Document?> _doc;

        public RevitSharedParameterRepository(Func<Document?> doc)
        {
            _doc = doc;
        }

        public void EnsureParameters(IReadOnlyList<SharedParameterDefinition> definitions)
        {
            Document? doc = _doc();

            if (definitions == null || definitions.Count == 0) return;
            foreach (var def in definitions)
            {
                ForgeTypeId groupType = RevitParameterMapper.ToGroupTypeId(def.GroupType);
                ForgeTypeId specType = RevitParameterMapper.ToSpecTypeId(def.SpecType);
                CategorySet categorySet = RevitParameterMapper.ToCategorySet(def.Categories, doc);
            }
        }
    }
}