# DHBIMWATER 수량산출 저장 구조 설계

## 1. 저장 전략

| 데이터 | 저장 위치 | 이유 |
|---|---|---|
| Auto 수량 (`Status = Auto`) | **Element의 ExtensibleStorage** | 객체에 종속, 객체 삭제 시 함께 소멸 |
| Manual 수량 (`Status = Manual`) | **전용 DataStorage 엔티티** | 특정 Element에 귀속되지 않음 |
| 설정값 | **전용 DataStorage 엔티티** (별도 Schema) | 기존 계획대로 |

---

## 2. Schema 구조

```
Schemas
├── QuantityElementSchema       // Element에 부착 (Auto 수량)
│   └── Field: "Items" → string (JSON array of QuantityItem)
│
├── ManualQuantityStorageSchema // DataStorage에 부착 (Manual 수량)
│   └── Field: "Items" → string (JSON array of QuantityItem)
│
└── QuantitySettingsSchema      // DataStorage에 부착 (설정값)
    └── Field: "Settings" → string (JSON)
```

- Schema당 `string` 필드 하나에 JSON 직렬화
- Revit의 `MapField` / `ArrayField`는 타입 제약이 많아 사용하지 않음

---

## 3. 프로젝트 레이어 구조

```
Core/
├── Entities/
│   ├── QuantityItem.cs
│   ├── FaceDeduction.cs        // 거푸집 공제 데이터
│   └── FormulaContext.cs       // 수식 생성에 필요한 컨텍스트
└── Services/
    └── FormulaBuilder.cs       // FormulaContext → RawFormula / RenderedFormula 생성

Infrastructure/Storage/
├── Schemas/
│   ├── QuantityElementSchema.cs
│   ├── ManualQuantityStorageSchema.cs
│   └── QuantitySettingsSchema.cs
├── Repositories/
│   ├── IElementQuantityRepository.cs   // 인터페이스 (Core)
│   ├── IManualQuantityRepository.cs    // 인터페이스 (Core)
│   ├── ElementQuantityRepository.cs    // Element ExtensibleStorage 구현
│   └── ManualQuantityRepository.cs     // DataStorage 구현
└── Helpers/
    └── DataStorageHelper.cs            // DataStorage 싱글턴 조회/생성
```

---

## 4. 핵심 엔티티

### FaceDeduction
거푸집 공제 정보. 수식 변수(`D1`, `D2`)의 실제 데이터 원본.

```csharp
public record FaceDeduction(
    long     SourceElementId,  // 공제 대상 부재 ElementId
    long     TargetElementId,  // 공제를 발생시킨 인접 부재 ElementId
    FaceType FaceType,         // 공제 면 방향
    double   Area              // 공제 면적
);
```

### FormulaContext
`FormulaBuilder`에 전달하는 입력 데이터.

```csharp
public record FormulaContext(
    double GrossArea,                        // 총 면적 (변수 A)
    IReadOnlyList<FaceDeduction> Deductions  // 공제 목록 (변수 D1, D2, ...)
);
```

### QuantityItem (변경사항)
`Deductions` 필드 추가. `RawFormula` / `RenderedFormula`는 `FormulaBuilder`가 생성.

```csharp
public record QuantityItem {
    // ... 기존 필드 동일
    public IReadOnlyList<FaceDeduction> Deductions { get; init; } = [];
}
```

---

## 5. FormulaBuilder

`FormulaContext`를 받아 수식 문자열과 변수 맵을 반환.

```
입력:  GrossArea = 200.0, Deductions = [Area=15.5, Area=12.3]
출력:
  RawFormula      = "A - D1 - D2"
  RenderedFormula = "200.0(A) - 15.5(D1) - 12.3(D2)"
  VariableMap     = { A: 200.0, D1: 15.5, D2: 12.3 }
```

- `D1`, `D2`는 수식에서만 사용하는 Alias (별칭)
- 실제 데이터(`ElementId`, `FaceType` 등)는 `FaceDeduction` 객체에 보존
- 변수명에 ElementId를 인코딩하지 않음 (`A1_ID_719812` 방식 사용 안 함)

---

## 6. Repository 패턴

### Schema 정의 (공통 패턴)

```csharp
internal static class QuantityElementSchema
{
    public static readonly Guid SchemaGuid = new("YOUR-GUID-HERE");
    public const string SchemaName = "DH_QuantityElement";
    public const string FieldItems = "Items";

    public static Schema GetOrCreate() =>
        Schema.Lookup(SchemaGuid) ?? new SchemaBuilder(SchemaGuid)
            .SetSchemaName(SchemaName)
            .SetReadAccessLevel(AccessLevel.Public)
            .SetWriteAccessLevel(AccessLevel.Public)
            .AddSimpleField(FieldItems, typeof(string))
            .Finish();
}
```

### DataStorageHelper

```csharp
// Schema GUID 기준으로 전용 DataStorage를 찾거나 생성
internal static class DataStorageHelper
{
    public static DataStorage GetOrCreate(Document doc, Guid schemaGuid) { ... }
}
```

---

## 7. 트랜잭션 규칙

- Repository의 `Save` 메서드 내부에서 Transaction을 열고 닫음
- **반드시 `IExternalEventHandler.Execute()` 안에서 호출**

```csharp
class SaveQuantityEventHandler : IExternalEventHandler
{
    public void Execute(UIApplication app)
    {
        var repo = new ElementQuantityRepository(app.ActiveUIDocument.Document);
        repo.Save(_elementId, _items);
    }
}
```

---

## 8. 미구현 항목 (이 문서 기준)

- [ ] `FaceDeduction.cs` 생성
- [ ] `FormulaContext.cs` 생성
- [ ] `FormulaBuilder.cs` 구현
- [ ] `QuantityItem`에 `Deductions` 필드 추가
- [ ] `QuantityElementSchema.cs` 구현
- [ ] `ManualQuantityStorageSchema.cs` 구현
- [ ] `ElementQuantityRepository.cs` 구현
- [ ] `ManualQuantityRepository.cs` 구현
- [ ] `DataStorageHelper.cs` 구현
