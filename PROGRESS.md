# PROGRESS

> 작업이 시작되거나 완료될 때마다 이 파일을 업데이트한다.
> 현재 브랜치: `feature/wTank`

---

## 완료된 작업

### 인프라 / 공통
- [x] Clean Architecture 계층 구조 설계 및 프로젝트 셋업
- [x] DI Container 구성 (`ServiceContainer`, `Func<Document?>` 패턴)
- [x] Revit Transaction 추상화 (`ITransactionContext`, `RevitTransactionContext`)
- [x] `Result<T>` 공통 결과 타입

### 수량산출 (Quantity)
- [x] `IQuantityExtractor` 인터페이스 정의
- [x] Extractor 구현: Beam, Column, Floor, Foundation, GenericModel, Rebar, Stairs, Wall, Railing, DirectShape
- [x] `CalculateQuantityUseCase` — Extractor 순회 → DataStorage 저장 → 반환
- [x] `ExportQuantityUseCase` — 수량 Excel 내보내기
- [x] `IElementQuantityRepo` / `ElementQuantityRepo` — Revit DataStorage 저장
- [x] `IManualQuantityRepo` / `ManualQuantityRepo` — 수동 수량 DataStorage 저장
  - DI 오류 수정: `Document` 직접 주입 → `Func<Document?>` 패턴으로 변경 (2026-06-22)
- [x] `ManualQuantityViewModel` — 수동 수량 입력 (New / Edit 모드, 산출식 미리보기)
- [x] `QuantityRequestHandler` — ExternalEvent 기반 비동기 처리

#### 수량산출 Rule Engine 전환 (2026-06-22 ~ 06-23) — 상세 로그 정리됨
- [x] Wall/Column/Beam/Floor/Foundation/Rebar 를 `IQuantityExtractor` → `IElementMeasurementExtractor` + `QuantityRuleEngine` 경로로 전환
- [x] 핵심 타입 신규: `RevitCategory`, `ElementMeasurements`, `QuantityRule`/`RuleFilter`/`RuleSet`, `DefaultRuleSet`, `QuantityRuleEngine`
- [x] `FormworkType` enum(거푸집 규격), `FaceType.OpeningSide`(EdgeLoop 기반 오프닝 측면 판별) 추가
  > 상세 구조·규칙·키 정의는 [docs/05_수량산출로직.md](docs/05_수량산출로직.md) 참조. 세부 변경 이력은 git log.

#### 동바리 수량 산출 + 슬래브 하부 거푸집 보정 (2026-06-25)
- [x] `RevitIntersectingElementFinder` — `GetTargetCategories(OST_Floors)`에서 `OST_StructuralFraming` 제거
  - 슬래브의 모든 면(Bottom/Side/OpeningSide)에서 보 접촉 공제 없음
  - 보 하부면이 슬래브 하부 거푸집 면적에 자연스럽게 포함됨
- [x] `DefaultRuleSet` — 보 하부 거푸집 규칙 제거 (`Fw(Plywood4, "A_bottom_net", Framing)`)
  - 보 하부면 거푸집은 슬래브 하부 거푸집에 통합, 보에서 별도 산출 안 함
- [x] `RevitFloorMeasurementExtractor` — `CalcShoringHeight()` / `CalcShoringRange()` (2026-06-26)
  - 하부 슬래브 탐색: `OfClass(Floor)`, BBox.Max.Z < 현재 슬래브 하면 Z + XY BBox 겹침 확인 → 가장 가까운 것 선택
  - XY 겹침 없는 슬래브는 후보에서 제외 (평면상 무관한 슬래브가 하부로 잘못 선택되는 버그 수정)
  - 높이(m) = 현재 슬래브 BBox.Min.Z − 하부 슬래브 BBox.Max.Z
  - `ShoringHeightThresholdM = 4.2` 상수 유지 (강관/시스템 분기 기준)
  - Values: `H_shoring`, `ShoringFactor(0.9)`
  - Parameters: `ShoringRange` — `"강관_2.5"` / `"강관_3.5"` / `"강관_4.2"` / `"시스템_5"` / `"시스템_10"` / `"시스템_20"` / `"시스템_30"` (높이 0 이하 → 빈 문자열)
- [x] `DefaultRuleSet` — 동바리 규칙 7개로 세분화 (2026-06-26)
  - WorkType `"동바리"` → `"강관동바리"` / `"시스템동바리"` 분리
  - 강관동바리 (m²): `H≤3.5m` / `3.5m<H≤4.2m`
  - 시스템동바리 (공m³): `H≤5m` / `5m<H≤10m` / `10m<H≤20m` / `20m<H≤30m`
  - 팩토리 헬퍼: `SteelShoring()` / `SystemShoring()` 추가

### 자동 모델링
- [x] `CreatePumpingStationUseCase` — 펌프장 자동 생성
- [x] `ParsePumpManufacturerSpecsUseCase` / `ParseValveExtensionUseCase` — Excel 파싱
- [x] `CreateReservoirUseCase` — 배수지(저수조) 자동 생성 ✅ 2026-06-24
  - DH_Revit_test/RequestHandler.cs 지오메트리 계산식 이식 → `ReservoirGeometryCalculator`
  - 레벨(6개: 구조 4 + LWL/HWL) / 슬래브(B1·L1·B2·L2·B4·L4·MS1·S1·S2·TC1-3) / 선형벽체(W1-10·B3) / 기둥(C1) / 보(G1·H1·H2)
  - LWL/HWL 레벨은 plan view 미생성 (수위 참조용 레벨만)
  - `RevitSlabCommandRepo` 버그 수정: SubPoints 빈 배열일 때 빈 CurveLoop 추가 → profile 오류 발생 ✅ 2026-06-24
  - 기둥·보 유형 선택 방식으로 변경 ✅ 2026-06-24
    - Cw/Cd/Gw/Gh 치수 입력 제거 → `ColumnTypeName`/`BeamTypeName` (Revit 패밀리 타입명)
    - `WaterTankViewModel`: `IElementTypeQueryRepo` 주입, 로드 시 타입 목록 초기화
    - UI: TextBox 4개 → ComboBox 2개 (기둥 유형 / 보 유형)
    - `BeamDefinition.TypeName` 추가 (Width/Height는 PumpingStation 호환용 유지)
  - 사용자 입력 단위 전환: LWL·HWL → m, 나머지 치수 → mm, He → 자동계산(HWL-LWL)
  - `ColumnDefinition` + `IColumnCommandRepo` + `RevitColumnCommandRepo` 신규 추가
  - TODO: 오프닝 배치, 단면뷰 작성 (Calculator 메서드 추가 후 구현)

#### 시스템 Stair(계단) 작성 코드 신규 추가 (2026-07-01)
- [x] `StairsDefinition` / `StairsRunDefinition` / `StairsLandingDefinition` (`Core/Structures`) — Run/Landing 분리 구조
  - Run: `StartPoint`/`EndPoint`/`Justification`(Left/Center/Right)
  - Landing: `BoundaryPoints`(폐곡선, Z=층계참 높이)
  - 컨테이너(`StairsDefinition`): `BaseLevelName`/`TopLevelName`/`BaseOffset`/`TopOffset`/`TypeName` + `Runs`/`Landings` + 공통 메타데이터(Category/ElementCode/Zone/Part)
- [x] `IStairCommandRepo` (`Application/Interfaces`) — `CreateStair(StairsDefinition)`
- [x] `RevitStairCommandRepo` (`Infrastructure/Repositories/Revit/Modeling`)
  - `StairsEditScope.Start()` → 내부 Transaction에서 Run별 `StairsRun.CreateStraightRun()` + Landing별 `StairsLanding.CreateSketchedLanding()` → `stairsEditScope.Commit()`
  - RevitAPI.dll 리플렉션으로 실제 시그니처 확인 후 구현 (`StairsRunJustification`은 Left/Center/Right, `LeftJustification` 아님 등)
  - StairsType은 이름으로 기존 타입 탐색만 (FindOrCreate 미구현, 없으면 실패)
  - DH_ElementCode/DH_Addin/DH_Category/DH_Part/DH_Zone 파라미터 설정
- [x] DI 등록: `ServiceCollectionExtensions.cs`에 `IStairCommandRepo → RevitStairCommandRepo` 추가 (Mock 미등록 — 요청 범위 제외)
- [x] 빌드 확인 완료 (오류 0개)
- TODO: `BaseOffset`/`TopOffset`(mm) 이 Repo에 미반영 — 계단 인스턴스 파라미터 반영 로직 확인 필요
- TODO: `StairsEditScope`는 Revit API 제약상 열려있는 Transaction 내부에서 Start/Commit 불가하나, 사용자 확인에 따라 Repo에서 별도 처리 안 함 → 호출하는 UseCase(예: `CreatePumpingStationUseCase`류)가 이미 트랜잭션을 연 상태에서 호출한다는 전제. 실제 Revit에서 예외 발생 시 트랜잭션 경계 재검토 필요
- TODO: `CreateStair`를 사용하는 UseCase/ViewModel/Ribbon 연동 미작성 (이번 요청 범위 밖)

### 도면 (Sheets)
- [x] `SheetUseCase` — 시트 생성/관리
- [x] `WaterReservoirUseCase` — 저수조 도면 배치
- [x] `PumpingStationUseCase` — 펌프장 도면 배치

### 파라미터
- [x] `ExportParamsUseCase` / `ImportParamsUseCase` — 공유 파라미터 내보내기/가져오기

### UI
- [x] 리본 메뉴 구성 (`RibbonBuilder`, `ModelingRibbonModule`, `QuantityRibbonModule` 등)
- [x] 외부 패밀리 라이브러리 (`WebFamilyLibraryCommand`)
- [x] 부재별 탭 아이템 정렬 — `공종 → 규격1 → 규격2` 순으로 변경 ✅ 2026-06-29
  - `QuantityView.xaml` `GroupByMembers` SortDescriptions: `ElementCode/ElementId` → `WorkType/Specification/SubSpecification`

### 문서
- [x] Quantity Extractor 폴더 구조 문서화 → `docs/05_수량산출로직.md` 3장에 통합 ✅ 2026-06-30
  - 16개 파일, 2개 인터페이스 계열, DI 등록 상태, 레거시 미사용 6개 파일, 신규 추가 절차 정리
  - 기존 3-1 표의 `RevitRebarExtractor`(미등록) 활성 오기 → DI 등록 기준으로 정정
  - 처음엔 별도 `06_Quantity_Extractor_구조.md`로 작성했다가 05와 중복되어 **05에 병합 후 06 삭제**
  - `CLAUDE.md` Quantity Extractor 섹션 + 신규 `AGENTS.md`에서 05로 링크 연결
  - `AGENTS.md` 주요 문서 표에 [3장 — 두 가지 산출 경로](docs/05_수량산출로직.md#3-두-가지-산출-경로) 앵커 링크 추가

---

## ⚠️ 의사결정 대기

### Stair 샘플 코드 구현 방식 (2026-07-01)
- 요청: "0,0,0에서 시작하는 계단 작성하는 샘플 코드" 필요 (`IStairCommandRepo`/`RevitStairCommandRepo` 호출 예시)
- 선택지 2가지 중 미결정:
  1. **UseCase에서 직접 생성 (권장 검토)** — `StairsDefinition`을 UseCase 안에서 인라인으로 조립 후 바로 Repo 호출. 단순 샘플/테스트 목적이라 Calculator 계층 생략. 추후 파라메트릭 로직 필요해지면 그때 Calculator로 분리.
  2. **Calculator + UseCase 둘 다 생성** — `PumpingStationGeometryCalculator`처럼 `StairsCalculator`를 별도로 만들어 `StairsDefinition` 계산을 담당시키고, UseCase는 트랜잭션/Repo 호출만 담당. 지금은 로직이 단순해도 기존 패턴과 통일성 유지 목적.
- 다음 작업 시 사용자에게 위 선택지 확인 후 진행할 것.

---

## 진행 중인 작업

#### 수동 수량 입력 — 변수 길이/면적 측정 연동 구현 완료 (2026-07-03)
- [x] 목업 `docs/manual_quantity_view_v2.html` 기반 UI를 실제 WPF 변수 카드에 반영
- [x] `IMeasurePickService` / `MeasureKind` / `MeasureResult` 추가 (`Application/Interfaces/Quantity`)
- [x] `RevitMeasurePickService` 추가 (`Revit/Commands/Quantity`)
  - `ExternalEvent` + `TaskCompletionSource` 기반으로 길이/면적 측정 요청 처리
  - 길이 측정은 `Selection.PickPoint()` 반복 + ESC 종료 방식으로 구현
  - 면적 측정은 `Selection.PickObject(ObjectType.Face)` + `face.Area` → `m²` 변환
- [x] `ManualQuantityViewModel` 확장
  - 선택적 측정 서비스 주입, `MeasureLengthCommand` / `MeasureAreaCommand`, `VariableInput.IsMeasuring` 추가
  - 측정 성공 시 변수 값 `F3` 포맷 반영 + 변수 단위(`m`/`m²`) 동적 세팅, 취소 시 값 유지
- [x] `ManualQuantityView` / `QuantityView` 모드리스 전환
  - `DialogResult` 제거, `CloseRequested` 콜백으로 부모 창에서 `AddItem` / `ReplaceItem` 처리
  - 변수 카드 5열 구조(`변수명 | 값 | 단위 | 길이 | 면적`) + 측정 중 강조 UI 추가
- [x] `QuantityCommand` / `QuantityViewModel` 배선 완료
  - `QuantityCommand`에서 `RevitMeasurePickService` 생성 후 `QuantityViewModel.SetMeasureService()`로 전달
- [x] 자동 테스트 추가
  - `tests/DHBIMWATER.UI.Tests` 신규 생성
  - `ManualQuantityViewModelTests`: 측정 성공, 취소 유지, 서비스 미주입 시 비활성 검증
- [x] 빌드/테스트 확인
  - `dotnet test tests/DHBIMWATER.UI.Tests/DHBIMWATER.UI.Tests.csproj -c Release` 통과
  - `dotnet build src/DHBIMWATER.Revit/DHBIMWATER.Revit.csproj -c Release` 오류 0
- TODO: 실제 Revit 런타임에서 길이 측정 UX(`PickPoint` 반복 + ESC 종료)가 사용자 기대와 맞는지 확인 필요
- TODO: 길이/면적 버튼 아이콘은 임시 텍스트(`↔`, `▱`) 사용 중 — 추후 프로젝트 아이콘 스타일로 교체 검토
#### PumpingStationViewModel 초기값 B7/NS1 미반영 버그 수정 (2026-07-01)
- [x] `InitializeDerivedValues()` — `_h7` 계산 직후 `UpdateNS1()` 호출 추가
  - 기존엔 생성자에서 필드를 직접 대입(`_d = 800.0` 등)해서 `D`/`H6`/`HS1` 프로퍼티 setter의 `UpdateH7Calculation → UpdateNS1 → ApplyB7Final` 체인이 한 번도 실행되지 않음
  - 그 결과 Type1 기본값 로드 시 `NS1=0`, `B7`이 필드 기본값 `3000`에 멈춰있었음 (정상 계산 시 `NS1=11`, `B7=4300`이어야 함)
  - `UpdateNS1()`이 내부에서 `ApplyB7Final()`까지 연쇄 호출하므로 별도 호출 불필요
  - `NS1`/`B7` setter가 값 변경 시 자체적으로 `OnPropertyChanged` 호출하므로 수동 알림 추가 안 함
  - TODO: NS1 산출식(`ROUNDDOWN`/`MOD`)이 원본 엑셀과 정확히 일치하는지는 엑셀 원본 재확인 필요 — 나머지 0일 때 계단 1단 빼는 로직으로 구현되어 있음

#### PumpingStationViewModel B7 100mm 올림 (2026-06-30)
- [x] `ApplyB7Final` — B7 최종값에 100mm 올림 적용
  - 기존 `effective` 계산을 `baseValue`로 분리 후 `Math.Ceiling(baseValue / 100.0) * 100`
  - Type1(밸브/계단 `Math.Max` 결과)·Type2·3(`_b7Base`) 모두 최종 B7이 100mm 단위로 올림됨

#### 펌프장 θ(기초 경사부 기울기) 힌트 연동 (2026-06-30)
- [x] `PumpingStationView.xaml` — θ ComboBox에 `Tag="θ"` 추가
- [x] `PumpingStationView.xaml.cs` `OnParameterFocused` — 시각 트리를 따라 올라가며 string `Tag` 탐색
  - TextBox만 처리하던 것을 ComboBox 등 모든 컨트롤 호환으로 확장 (기존 TextBox 동작 유지)
- [x] `PumpingStationViewModel._paramHints` — `["θ"]` 힌트 항목 추가 (설계기준 30°/45° 권고 문구)

#### 철근 HostElementId 파이프라인 추가 (2026-06-29)
- [x] `ElementMeasurements.HostElementId` (`long?`) 필드 추가
- [x] `QuantityRuleEngine` — `QuantityItem` 생성 시 `HostElementId` 전달
- [x] `RevitRebarMeasurementExtractor` — `rebar.GetHostId()` 로 `HostElementId` 세팅
  - 구버전 `RevitRebarExtractor` 대비 누락된 유일한 필드였음

#### RevitStairsExtractor 재료 판별 수정 (2026-06-29)
- [x] `Stairs` 객체 자체에는 재료 없음 → `GetStairsMaterialClass()` 헬퍼 추가
  - `stair.GetMaterialIds(false)` 직접 호출로 재료 취득 (Run/Landing 순환 불필요)
  - `STRUCTURAL_MATERIAL_PARAM` 기반 `FamilyInstanceHelper` 미사용
  - Concrete → "철근콘크리트", Metal → "강재", Generic → "기타", null → "미분류"

#### GenericModel / DirectShape 콘크리트 수량 산출 (2026-06-29)
- [x] `FamilyInstanceHelper.IsConcreteMaterial()` 추가
  - `StructuralAssetClass.Concrete` 인 경우만 true, 이름 폴백 없음
  - StructuralAsset 미설정 → false
- [x] `RevitGenericModelExtractor` — 재료 판별 + 귀속 철근 체크
  - 콘크리트 재료가 아니면 수량 없음
  - `GetRebarHostIds()` 캐시 HashSet → `rebar.GetHostId()` 1회 수집 후 O(1) 조회
  - 철근 있으면 철근콘크리트, 없으면 무근콘크리트 (기본값 무근)
- [x] `RevitDirectShapeExtractor` — 동일 로직
  - `ds.GetMaterialIds(false)` 로 재료 취득 (FamilyInstance와 다른 경로)
  - 클래스 기반 수집(`OfClass(typeof(DirectShape))`) 유지 → Floor 카테고리 DirectShape와 Floor 시스템 패밀리 간 충돌 없음
  - 동일한 `GetRebarHostIds()` 캐시 패턴 적용

### WaterTank 기능 (`feature/wTank`)
- [x] 수조(Water Tank) 자동 모델링 로직 ✅ 2026-06-24
- [ ] `WaterTankCommand` 구현 (ExternalEvent 연결)
- [ ] Ribbon 연동 (`ModelingRibbonModule` 수정)

### Rule Engine 검증 (벽체 / 기둥 / 보 / 슬래브 / 기초)
- [ ] Revit에서 수량산출 실행 후 QuantityItem 생성 확인
- [ ] 벽체: 콘크리트(A x Thk), 거푸집(외/내벽 Euroform, 마구리 합판3회), 스페이서 항목 검증
- [ ] 기둥: 직사각형(B x D x L), 원형(PI x R^2 x L), 이형(A_cs x L) 분기 검증
- [ ] 보: 직사각형(B x D x L), 이형(A_cs x L) 분기 검증
- [ ] 슬래브: 콘크리트(A x Thk), 거푸집(하부 합판4회 — 보 공제 없음, 측면 합판3회/합판6회), 스페이서 검증
- [ ] 슬래브: 동바리 — 강관동바리(3구간 m²) / 시스템동바리(4구간 공m³) 높이별 구간 분류 검증
- [ ] 독립기초: 콘크리트(Vol), 거푸집(측면 합판4회/합판6회) 검증

---

## 🔧 미비점 / 개선 필요 (2026-06-30 코드 점검)

- [ ] **레거시 미사용 Extractor 6개 정리 검토** — `RevitWallExtractor` / `RevitColumnExtractor` /
  `RevitBeamExtractor` / `RevitFloorExtractor` / `RevitFoundationExtractor` / `RevitRebarExtractor`
  는 `IQuantityExtractor` 구현이지만 **DI 미등록 + 코드 미참조** (dead code, `*MeasurementExtractor`로 대체됨).
  - 잘못된 using 다수 포함: `DocumentFormat.OpenXml.*`, `System.Windows.Controls`,
    `System.ComponentModel.DataAnnotations`, `System.Data.Common`, `System.Reflection.Metadata.Ecma335`
  - 삭제 여부 결정 필요 (CLAUDE.md: 기존 dead code 임의 삭제 금지 → **컨펌 후 진행**)
- [ ] **`CanExtract(long)` 인터페이스 메서드 전면 미사용** — `IQuantityExtractor` /
  `IElementMeasurementExtractor` 양쪽에 선언되고 모든 Extractor가 구현하지만 **호출부 없음**
  (산출 경로는 `CollectElementIds()` + `Extract()`만 사용). → 단일 요소 재산출 용도로 살리거나 인터페이스에서 제거 검토.
- [ ] **`CalculateQuantityUseCase` `CollectElementIds()` 이중 열거** — `ids.Any()` 체크 후
  `foreach (ids)` 로 lazy `IEnumerable`(`FilteredElementCollector`)를 2회 실행. 양 경로 모두 `.ToList()` 1회 캐싱 권장.
- [ ] **활성 `RevitRailingExtractor` 의 `using System.Diagnostics;`** — 사용 여부 확인 후 미사용이면 정리 (빌드 경고).
- [ ] **거푸집 Specification 하드코딩 잔존** — `DefaultRuleSet` 거푸집 규격이 `FormworkType` enum이지만
  여전히 코드 하드코딩 상태 (CLAUDE.md: Specification 하드코딩 금지). → 아래 "거푸집 종류 설정창 연동" 작업으로 해소 예정.

### QuantityView 상세목록 버튼(삭제/복사/수정) 활성화 지연 (2026-07-01 원인 파악)
- [ ] **`RelayCommand`가 `CommandManager.RequerySuggested`에만 의존** (`RelayCommand.cs:29-33`)
  — `CommandManager.InvalidateRequerySuggested()`(`QuantityViewModel.cs:48` 등)는 Background 우선순위로
  디스패처 큐에 들어가 UI 스레드가 바쁘면 버튼 `IsEnabled` 갱신이 지연됨.
  → `RelayCommand`가 자체 `event` 필드로 `CanExecuteChanged`를 보관하고 동기적으로 `Invoke`하도록 수정 검토.
- [ ] **DataGrid `ColumnWidth="Auto"`가 행 가상화 무력화** (`DataGrids.xaml:61` `QuantityDataGridStyle`,
  `QuantityView.xaml` 상세 목록 컬럼들 `Width="Auto"`) — Auto 폭 계산을 위해 전체 행을 실측해야 해서
  `VirtualizationMode="Recycling"` 지정에도 사실상 가상화 무력화. 항목 수가 많을수록 위 지연 체감 증폭.
  → 고정 픽셀 폭 또는 `*`로 변경 검토.

---

## 예정된 작업

### 비계 수량 산출
- [ ] 건물 외피(envelope)를 하나의 연속 면으로 취급하여 비계 면적 산출
  - 외벽 면 + 슬래브 측면(외벽 연장선상) 포함
  - 예: [1층 벽체 외면] + [슬래브 측면] + [2층 벽체 외면] = 연속 외피

**채택 방향: 외벽 외부 Face 엣지 수집 → 폴리라인 조립**
1. `DH_IsExterior=1` 벽체의 외부 면에서 하단 수평 엣지 수집
2. 슬래브 측면 중 외벽 연장선상에 있는 면의 엣지도 동일하게 수집
3. 끝점이 맞닿는 엣지끼리 연결 → 외곽 폴리라인 조립
4. 폴리라인 세그먼트별 높이 × 길이 = 비계 면적 합산

**검토한 다른 방법**
- 외벽 LocationCurve 끝점 그래프 순회: 중심선 기준이라 실제 외면과 오프셋 차이 있음
- Solid Union 후 외곽 추출: Boolean 연산 비용 크고 실패 가능성 있음



### OpeningBottom / OpeningTop FaceType 추가 (다음 작업)
- [ ] `FaceType.OpeningBottom`, `FaceType.OpeningTop` 추가
  - Wall 오프닝의 헤드(soffit) / 실(sill) 면 분리
  - EdgeLoop 기반: Left/Right 내부 루프 엣지를 수평면도 공유하면 Opening{Bottom/Top}
- [ ] `RevitWallMeasurementExtractor` — `A_opening_bottom_gross/net`, `A_opening_top_gross/net` 키 추가
- [ ] `DefaultRuleSet` — Wall 헤드/실 거푸집 규칙 추가 (실 여부 결정 후 적용)

### Rule Engine 확장
- [x] `IQuantityRuleRepository` DataStorage 구현체 (`RevitQuantityRuleRepo`) ✅ 2026-06-24
  - `QuantityRuleStorageSchema` 신규 (`Infrastructure/Storage/Schemas/`)
  - `RevitQuantityRuleRepo` 신규 (`Infrastructure/Repositories/Revit/Storage/`)
  - DI 등록 완료
  - `CalculateQuantityUseCase`: DefaultRuleSet(공통 공종) + ProjectRuleSet(프로젝트 특화 공종) 병합 적용
    - DefaultRuleSet: 콘크리트·거푸집·철근 등 모든 구조물 공통
    - ProjectRuleSet: 지수판 등 프로젝트별 추가 공종 (없으면 빈 리스트로 처리)

### 거푸집 종류 설정창 연동
- [ ] 설정창 UI — 부재별·FaceType별 거푸집 종류 선택
- [ ] `IFormworkSettingRepo` 인터페이스 + DataStorage 구현체
- [ ] 설정값 → 프로젝트 RuleSet의 거푸집 Specification에 반영

### 프로젝트 RuleSet
- [ ] 지수판 길이 등 프로젝트 특화 공종 정의
- [ ] Rule CRUD 설정창 UI

### 부재별 탭 ElementCode 순 그룹 정렬 (계획 문서 작성됨 2026-06-30)
- 계획 문서: `docs/부재별탭_ElementCode_정렬_계획.md`
- [x] `QuantityView.xaml` `GroupByMembers` SortDescriptions를 `Category → ElementCode → ElementId → WorkType → Specification → SubSpecification` 순으로 변경
  - 그룹 표시 순서를 ElementCode 순으로 (단순 문자열 정렬, `B1, B10, B2` 순 허용)
- [x] `QuantityItem.ElementGroupLabel` — `WorkType == "철근"`이면 ElementId 없이 ElementCode로만 그룹화
- 주의: 기존 line 161 "공종 → 규격1 → 규격2" 정렬을 ElementCode 우선으로 되돌리는 변경

### 기타
- [ ] 배수지 오프닝 배치 — `ReservoirGeometryCalculator.CalculateOpenings()` 추가 후 UseCase 연결
- [ ] 배수지 단면뷰 작성 — `ReservoirGeometryCalculator.CalculateSectionViews()` 추가 후 UseCase 연결
- [ ] 수량산출 결과 검증 로직 보강

