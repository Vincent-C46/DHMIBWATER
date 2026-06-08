using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Core.Parameters;
using DHBIMWATER.Infrastructure.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using App = Autodesk.Revit.ApplicationServices.Application;

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
            if (definitions == null || definitions.Count == 0) return;

            Document? doc = _doc();
            var app = doc.Application;

            var version = app.VersionNumber;
            string sharedParameterFileName = "DHBIMWATER_sharedParameters.txt";
            string sharedParameterFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $@"Autodesk\Revit\Addins\{version}\{sharedParameterFileName}");

            if (doc == null) return;

            string previousFilePath = app.SharedParametersFilename;

            if (definitions.Count == 0) return;

            // GUID 하드코딩 - 변경 금지 ❌
            var guidDict = new Dictionary<string, Guid>()
                                {
                                    { "DH_Addin",       new Guid("f0ff9795-a26f-4a2f-869c-532d2c418fac") },
                                    { "DH_ElementCode", new Guid("280f0a33-1456-4c43-9f01-ae6acf80769b") },
                                    { "DH_Class",       new Guid("5b93b43c-abd7-435b-81b1-6b7ff51419c8") },
                                    { "DH_Category",    new Guid("7e539f5b-80e4-4dd9-a7e1-ac070dbcaa24") },
                                    { "DH_Zone",        new Guid("b60a47f3-3ede-45db-8491-b8f70de329a4") },
                                    { "DH_Part",        new Guid("47c74ae0-3fc8-4a06-9c4e-80713b564b0c") },
                                    // 형상 치수 정보
                                    { "L1",             new Guid("24ef3fdc-96cb-4e32-bb49-c8ecacd92a58") },
                                    { "W1",             new Guid("a6f6385b-2364-4288-a239-813d49e4a572") },
                                    { "L2",             new Guid("180771e8-b65a-4f97-a8e4-d00a67fa4823") },
                                    { "W2",             new Guid("f65c9ace-ffde-4b4f-a01c-b7dd303b0836") },
                                    { "L3",             new Guid("f074ebb2-5306-4082-888f-99d53b4f6f9e") },
                                    { "W3",             new Guid("e8fced45-637e-42bd-b9a9-e29f319e9f39") },
                                    { "H",              new Guid("10337573-871a-40b7-8a3d-4dc3637e9349") },
                                    { "ETC",            new Guid("5ef1d2b7-8766-482f-a81c-22fdcba1e72a") },
                                    // 뷰 관리용
                                    { "DH_뷰 카테고리",  new Guid("D65D0DD9-6E59-419C-B8A1-613C9C8E89CA") },
                                    { "DH_뷰 타입",      new Guid("D1A2EC3E-16A2-4E42-AC1B-7897E1DC43BA") },
                                    //{ "DH_RowNum",    new Guid("98122773-6f3c-49dc-a817-5bfb065d94a1") },
                                    //{ "DH_ColNum",    new Guid("a207e5bc-87dd-4062-973f-149777f98762") },
                                };

            try
            {
                string header = "*META\tVERSION\tMINVERSION\r\nMETA\t2\t1\r\n*GROUP\tID\tNAME\r\n*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\tHIDEWHENNOVALUE\r\n";
                File.WriteAllText(sharedParameterFilePath, header); // 생성 or 덮어쓰기

                app.SharedParametersFilename = sharedParameterFilePath; // 경로 지정
                DefinitionFile defFile = app.OpenSharedParameterFile();
                BindingMap bindingMap = doc.ParameterBindings;

                foreach (var def in definitions)
                {
                    #region 공유 매개변수 생성
                    DefinitionGroups? groups = defFile.Groups;
                    var groupNames = groups.Select(g => g.Name);
                    DefinitionGroup? group;

                    if (!groupNames.Contains(def.GroupName))
                    {
                        group = groups.Create(def.GroupName);   // 그룹이 없으면 새로 생성
                    }
                    else
                        group = groups.get_Item(def.GroupName); // 그룹이 이미 존재하면 가져오기

                    ForgeTypeId specType = RevitParameterMapper.ToSpecTypeId(def.SpecType);
                    ForgeTypeId groupType = RevitParameterMapper.ToGroupTypeId(def.GroupType);

                    ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(def.Name, specType);

                    options.GUID = guidDict.TryGetValue(def.Name, out var guid) ? guid : Guid.NewGuid();  // 지정된 GUID가 없으면 새로 생성
                    options.UserModifiable = def.UserModifiable;
                    options.Visible = true;

                    // 공유 매개변수 생성
                    Definition sharedDef = group.Definitions.Create(options);
                    //if (sharedDef == null) continue;
                    #endregion

                    #region 프로젝트 매개변수 생성
                    CategorySet categorySet = RevitParameterMapper.ToCategorySet(def.Categories, doc);
                    Binding binding = RevitParameterMapper.ToBinding(def.BindingType, categorySet, app);

                    if (!IsAlreadyBound(doc, def))
                        bindingMap.Insert(sharedDef, binding, groupType);
                    #endregion
                }
            }
            finally
            {
                // 기존 공유매개변수 복원
                app.SharedParametersFilename = previousFilePath;
            }
        }

        private static bool IsAlreadyBound(Document doc, SharedParameterDefinition def)
        {
            DefinitionBindingMapIterator iterator = doc.ParameterBindings.ForwardIterator();
            while (iterator.MoveNext())
            {
                if (iterator.Key is Definition existing && existing.Name == def.Name)
                    return true;
            }
            return false;
        }

    }
}
