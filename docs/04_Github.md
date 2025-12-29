# 보기 - 터미널 선택하여 PowerShell 창 열기


1. 새 Feature- 브랜치 생성
	1) git checkout dev - dev 브랜치로 이동
	2) git pull origin dev - 원격 저장소의 최신 dev 브랜치 가져오기 (origin 은 원격 저장소 이름)
	3) git checkout -b feature/test - 새로운 feature/test 브랜치 생성 및 이동 (-b는 브랜치 생성 옵션)
	
	=> 또는 git checkout -b feature/기능명 dev (dev 브랜치를 기준으로 새 브랜치 생성)

1. 
2. 작업 후 커밋 및 푸시
	1) git status - 변경된 파일 확인
	2) git add . - 모든 변경 파일 스테이징 (스테이징은 커밋할 파일을 준비하는 단계)
	3) git commit -m "Add new feature" - 변경 사항 커밋 (메시지는 작업 내용에 맞게 작성)
	4) git push origin feature/test - 원격 저장소에 feature/test 브랜치 푸시 (origin 은 원격 저장소 이름)
	
	=>  git push (이후 브랜치 이름 생략 시 현재 브랜치가 푸시됨)