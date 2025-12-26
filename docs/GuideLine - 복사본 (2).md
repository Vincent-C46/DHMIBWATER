1. Revit 및 SandBox 버튼이미지 등록
	1) DHBIMWATER.UI.Resources.Icons 폴더에 Revit(예시 : revit.png) 버튼이미지 추가
	2) revit.png 속성에서 빌드 작업을 리소스로 설정
	3) DHBIMWATER.UI.Sandbox 프로젝트의 MainWindow.xaml 파일에 fluent:RibbonGroupBox에 버튼을 추가하고 이미지 할당
	4) LargeIcon="pack://application:,,,/DHBIMWATER.UI;component/Resources/Icons/revit.png"
	5) SandBox 실행 후 Revit 버튼 확인

2. View(.xaml) / ViewModel(.cs) 추가 , 서비스 등록, DataContext 설정
	1) DHBIMWATER.UI.Views 폴더에 폴더를 만들어서 View(WPF 창) 추가 - 예시 : GuideLineView.xaml
	2) DHBIMWATER.UI.ViewModels 폴더에 폴더를 만들어서 ViewModel(C# 클래스) 추가 - 예시 : GuideLineViewModel.cs - ViewModelBase.cs 상속 / Public으로 구현
	3) DHBIMWATER.UI.DependencyInjection.ServiceCollectionExtensions 클래스에 View와 ViewModel을 서비스로 등록
	4) GuideLineView.xaml.cs (비하인드 코드)에서 ViewModel을 주입받아 DataContext로 설정)

3. XAML 네임스페이스 및 디자인 타임 설정
	1) xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks" 추가 - 아이콘팩 사용 시
	2) xmlns:controls="clr-namespace:DHBIMWATER.UI.Controls" - 커스텀 컨트롤 사용 시 - 타이틀 바 추가해야하므로 반드시 선언
	3) xmlns:vm="clr-namespace:DHBIMWATER.UI.ViewModels.Modeling" - ViewModel을 바인딩 하기 위해서 선언
	4) d:DataContext="{d:DesignInstance vm:Modeling1ViewModel, IsDesignTimeCreatable=False}" - 디자인 타임에 ViewModel 바인딩하기 위해서 선언

4. XAML 기본 레이아웃 구성
	1) Window 기본 속성 설정
		Title="GuideLineWindow" 
        Height="700" Width="1100"
        WindowStartupLocation="CenterScreen"
        Background="#F4F7F9" 
        AllowsTransparency="False" 
        WindowStyle="None"
        FontFamily="Segoe UI, Malgun Gothic">

	2) Window.Resources에 Generic.xaml 리소스 딕셔너리 병합 - GuideLineView.xaml 참고
		<Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/DHBIMWATER.UI;component/Styles/Generic.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>

	3) 기본 Grid 레이아웃 구성 및 타이틀 바 추가 - GuideLineView.xaml 참고
		<Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="40"/>
        </Grid.RowDefinitions>

        <controls:TitleBar Grid.Row="0"/>
    </Grid>

	4) WindowChrome 설정 - GuideLineView.xaml 참고
		<WindowChrome.WindowChrome>
        <WindowChrome CaptionHeight="0" CornerRadius="15" GlassFrameThickness="1" UseAeroCaptionButtons="False"/>
        </WindowChrome.WindowChrome>

5. SandBox에서 View 호출 및 테스트
	1) DHBIMWATER.UI.Sandbox.MainViewModel 클래스로 이동하여 ICommand 속성 추가 : public ICommand OpenGuideLineViewCommand { get; }
	2) 생성자에서 OpenGuideLineViewCommand = new RelayCommand(OpenGuideLineView); 메서드 바인딩
	3) OpenGuideLineView() 메서드 구현 - 서비스로 뷰를 주입받아 Show() 호출
	4) DHBIMWATER.UI.Sandbox.MainView.xaml 파일에서 버튼에 커맨드 바인딩 - Command="{Binding OpenGuideLineViewCommand}"

6. 버튼 등록
	1) DHBIMWATER.Revit.UI.Modules 폴더에서 패널 생성 및 버튼 추가 - 예시 : ModelingRibbonModule.cs / IRibbonModule 상속
	2) Build() 메서드에서 패널과 버튼 생성 코드 구현 - ModelingRibbonModule.cs 참고
	3) 버튼 이미지 설정 btn4.LargeImage = RibbonButtonImages.GetIcon("revit.png")

7. Revit Command 구현 및 테스트
	1) DHBIMWATER.REVIT.Commands 폴더에 Revit Command 클래스 추가 - 예시 : GuideLineCommand.cs - Public으로 구현 / CommandBase 상속
	2) 서비스 프로바이더에서 뷰를 주입받아 ShowDialog() 호출

8. 솔루션 빌드 및 Revit 플러그인 설치, 테스트 
	1) 솔루션 빌드 시 - C:\BuildOutput\DHBIMWATER 폴더에 .addin 및 .dll 파일 생성 확인
	2) 솔루션 installer 폴더에 있는 DHBIMWATER_2025.nsi를 실행하여 플러그인 설치 파일 생성
	3) C:\BuildOutput\DHBIMWATER 폴더에 설치 파일 생성 확인 및 설치
	4) Revit 2025 실행 후 DHBIMWATER 탭에서 GuideLine 버튼 클릭하여 뷰가 정상적으로 열리는지 확인
