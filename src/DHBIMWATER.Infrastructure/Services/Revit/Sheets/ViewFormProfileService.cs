using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB.ExtensibleStorage;


namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewFormProfileService
    {
        private readonly Document _doc;
        public ViewFormProfileService(Document doc) { _doc = doc; }
        private static readonly Guid FormSchemaGuid = new("3F9D1A7D-8A9A-4D9C-9B7B-5A6D31D4F211");
        private const string FieldName = "SheetForm";

        public void Apply(string viewId, string form)
        {
            if (!long.TryParse(viewId, out var vid)) return;
            var view = _doc.GetElement(new ElementId(vid)) as View;
            if (view == null) return;

            using var tx = new Transaction(_doc, "Apply View Form Profile");
            tx.Start();

            // 선택한 양식 저장(없음도 저장)
            SaveFormToView(view, form);

            // "없음"이면 저장만 하고 끝
            if (string.IsNullOrWhiteSpace(form) || form == "없음")
            {
                tx.Commit();
                return;
            }
            // 양식 적용 시 자르기 영역 보기는 항상 끔
            if (view.CropBoxActive)
            {
                view.CropBoxVisible = false;
            }

            var keep = GetKeepCategories(form);
            if (keep.Count == 0)
            {
                tx.Commit();
                return;
            }

            var hideAnnotations = GetHideAnnotationCategories(form);
            var hideAnnotationCategoriesByCategory = GetHideAnnotationCategoriesByCategory(form);
            

            // 모델 + 주석 카테고리 제어
            // 예전에 카테고리 숨김으로 꺼진 상태가 있으면 먼저 다시 켬
            foreach (Category cat in _doc.Settings.Categories)
            {
                if (cat == null) continue;
                if (!view.CanCategoryBeHidden(cat.Id)) continue;

                if (cat.CategoryType == CategoryType.Model ||
                    cat.CategoryType == CategoryType.Annotation)
                {
                    view.SetCategoryHidden(cat.Id, false);
                }

                var bic = (BuiltInCategory)cat.Id.Value;
                if (hideAnnotationCategoriesByCategory.Contains(bic))
                {
                    view.SetCategoryHidden(cat.Id, true);
                }
            }


            // 현재 뷰에 있는 요소만 골라서 숨김
            var hideIds = new List<ElementId>();

            var elemsInView = new FilteredElementCollector(_doc, view.Id)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var e in elemsInView)
            {
                if (e == null || e.Category == null) continue;
                if (!e.CanBeHidden(view)) continue;

                var cat = e.Category;
                var bic = (BuiltInCategory)cat.Id.Value;
                              

                // 가져온 CAD/가져온 카테고리는 전부 숨김
                if (e is ImportInstance)
                {
                    hideIds.Add(e.Id);
                    continue;
                }                

                // 숨기고 싶은 특정 주석/기호/참조 카테고리는 타입 상관없이 우선 숨김
                if (hideAnnotations.Contains(bic))
                {
                    hideIds.Add(e.Id);
                    continue;
                }


                // 모델 카테고리: keep에 없는 것만 숨김
                if (cat.CategoryType == CategoryType.Model)
                {
                    if (!keep.Contains(bic))
                        hideIds.Add(e.Id);
                    continue;
                }
            }
            if (hideIds.Count > 0)
            {
                view.HideElements(hideIds);
            }
            tx.Commit();
        }

        private static Schema GetOrCreateSchema()
        {
            var schema = Schema.Lookup(FormSchemaGuid);
            if (schema != null) return schema;

            var sb = new SchemaBuilder(FormSchemaGuid);
            sb.SetSchemaName("DHBIMWATER_ViewForm");
            sb.SetReadAccessLevel(AccessLevel.Public);
            sb.SetWriteAccessLevel(AccessLevel.Public);
            sb.AddSimpleField(FieldName, typeof(string));
            return sb.Finish();
        }
        private static void SaveFormToView(View view, string form)
        {
            var schema = GetOrCreateSchema();
            var field = schema.GetField(FieldName);
            var ent = new Entity(schema);
            ent.Set(field, form ?? "없음");
            view.SetEntity(ent);
        }

        private static HashSet<BuiltInCategory> GetKeepCategories(string form)
        {
            if (form == "KeyMap")
            {
                return new HashSet<BuiltInCategory>
                {
                    BuiltInCategory.OST_StructuralFoundation,
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_Columns,
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_StructuralFraming,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_Stairs,
                    BuiltInCategory.OST_Railings,
                    BuiltInCategory.OST_StairsRailing
                };
            }

            // 공통(일반도 기본)
            var baseSet = new HashSet<BuiltInCategory>
            {
                BuiltInCategory.OST_StructuralFoundation,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_Columns,
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Railings,
                BuiltInCategory.OST_StairsRailing,
                BuiltInCategory.OST_Stairs,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_Floors
            };

            if (form == "구조도")
            {
                baseSet.Add(BuiltInCategory.OST_Rebar);
                baseSet.Add(BuiltInCategory.OST_AreaRein);
                baseSet.Add(BuiltInCategory.OST_PathRein);
                baseSet.Add(BuiltInCategory.OST_FabricAreas);
                baseSet.Add(BuiltInCategory.OST_FabricReinforcement);
            }

            return baseSet;
        }

        private static HashSet<BuiltInCategory> GetHideAnnotationCategories(string form)
        {
            var set = new HashSet<BuiltInCategory>();

            if (form == "KeyMap")
            {
                set.Add(BuiltInCategory.OST_TextNotes);
                set.Add(BuiltInCategory.OST_DetailComponents);
                set.Add(BuiltInCategory.OST_Dimensions);
                return set;
            }

            set.Add(BuiltInCategory.OST_TextNotes);
            set.Add(BuiltInCategory.OST_DetailComponents);

            return set;
        }

        private static HashSet<BuiltInCategory> GetHideAnnotationCategoriesByCategory(string form)
        {
            if (form == "KeyMap")
            {
                return new HashSet<BuiltInCategory>
                {
                    BuiltInCategory.OST_Elev,
                    BuiltInCategory.OST_ElevationMarks,
                    BuiltInCategory.OST_CLines,
                    BuiltInCategory.OST_Levels,
                    BuiltInCategory.OST_Callouts
                };
            }

            return new HashSet<BuiltInCategory>
            {
                BuiltInCategory.OST_SectionHeads,
                BuiltInCategory.OST_Sections,
                BuiltInCategory.OST_Elev,
                BuiltInCategory.OST_ElevationMarks,
                BuiltInCategory.OST_CLines,
                BuiltInCategory.OST_Viewers,
                BuiltInCategory.OST_Views,
                BuiltInCategory.OST_Levels,
                BuiltInCategory.OST_Callouts
            };
        }
    }
}
